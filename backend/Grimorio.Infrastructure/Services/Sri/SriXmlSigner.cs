using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Grimorio.Infrastructure.Services.Sri;

// Firma el XML de factura con XAdES-BES según el estándar del SRI Ecuador.
// Usa SHA-1 y RSA tal como exige la especificación técnica del SRI.
public static class SriXmlSigner
{
    private const string XadesNs = "http://uri.etsi.org/01903/v1.3.2#";
    private const string XmlDsigNs = "http://www.w3.org/2000/09/xmldsig#";

    public static string Sign(string unsignedXml, byte[] p12Bytes, string p12Password)
    {
        // ── Cargar certificado desde .p12 ─────────────────────────────────────
        var store = new Pkcs12StoreBuilder().Build();
        store.Load(new MemoryStream(p12Bytes), p12Password.ToCharArray());
        var alias = store.Aliases.Cast<string>().First(a => store.IsKeyEntry(a));

        var bouncyCert = store.GetCertificate(alias).Certificate;
        var bouncyKey = (RsaPrivateCrtKeyParameters)store.GetKey(alias).Key;

        // Convertir a tipos .NET para operaciones criptográficas
        var rsaParams = new RSAParameters
        {
            Modulus = bouncyKey.Modulus.ToByteArrayUnsigned(),
            Exponent = bouncyKey.PublicExponent.ToByteArrayUnsigned(),
            D = bouncyKey.Exponent.ToByteArrayUnsigned(),
            P = bouncyKey.P.ToByteArrayUnsigned(),
            Q = bouncyKey.Q.ToByteArrayUnsigned(),
            DP = bouncyKey.DP.ToByteArrayUnsigned(),
            DQ = bouncyKey.DQ.ToByteArrayUnsigned(),
            InverseQ = bouncyKey.QInv.ToByteArrayUnsigned(),
        };
        using var rsa = RSA.Create();
        rsa.ImportParameters(rsaParams);

        var certDer = bouncyCert.GetEncoded();
        using var dotnetCert = new X509Certificate2(certDer);
        var certB64 = Convert.ToBase64String(certDer);

        // ── Cargar XML y preparar el documento ────────────────────────────────
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(unsignedXml);

        var signingTime = DateTime.UtcNow;

        // ── IDs únicos para los nodos de la firma ─────────────────────────────
        var sigId = "Signature";
        var certId = "Certificate1";
        var signedPropsId = "SignedProperties";
        var keyInfoId = "KeyInfoId-Signature";

        // ── Construir KeyInfo ─────────────────────────────────────────────────
        var keyInfo = BuildKeyInfo(doc, certB64, rsa, keyInfoId);
        doc.DocumentElement!.AppendChild(keyInfo);

        // ── Construir QualifyingProperties (XAdES) ───────────────────────────
        var qualProps = BuildQualifyingProperties(doc, sigId, signedPropsId, signingTime, bouncyCert, certB64);
        var objNode = doc.CreateElement("ds", "Object", XmlDsigNs);
        objNode.SetAttribute("Id", "QualifyingProperties");
        objNode.AppendChild(qualProps);
        // Añadir temporalmente para calcular digest
        doc.DocumentElement.AppendChild(objNode);

        // ── Calcular digests ──────────────────────────────────────────────────
        string digestDoc = ComputeDigest(doc, "#comprobante");
        string digestKeyInfo = ComputeDigest(doc, "#" + keyInfoId);
        string digestSignedProps = ComputeDigest(doc, "#" + signedPropsId);

        // Remover nodos temporales del doc original
        doc.DocumentElement.RemoveChild(keyInfo);
        doc.DocumentElement.RemoveChild(objNode);

        // ── Construir SignedInfo ──────────────────────────────────────────────
        var signedInfo = BuildSignedInfo(doc, sigId, digestDoc, digestKeyInfo, digestSignedProps, keyInfoId, signedPropsId);

        // ── Firmar el SignedInfo ──────────────────────────────────────────────
        // Canonicalizar SignedInfo antes de firmar
        string c14nSignedInfo = Canonicalize(signedInfo);
        byte[] signedInfoBytes = Encoding.UTF8.GetBytes(c14nSignedInfo);
        byte[] signature = rsa.SignData(signedInfoBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        string signatureB64 = Convert.ToBase64String(signature);

        // ── Ensamblar el elemento <ds:Signature> completo ────────────────────
        var sigElement = doc.CreateElement("ds", "Signature", XmlDsigNs);
        sigElement.SetAttribute("Id", sigId);

        sigElement.AppendChild(signedInfo);

        var sigValue = doc.CreateElement("ds", "SignatureValue", XmlDsigNs);
        sigValue.SetAttribute("Id", "SignatureValue");
        sigValue.InnerText = signatureB64;
        sigElement.AppendChild(sigValue);

        sigElement.AppendChild(keyInfo);

        var obj2 = doc.CreateElement("ds", "Object", XmlDsigNs);
        obj2.SetAttribute("Id", "QualifyingProperties");
        obj2.AppendChild(qualProps);
        sigElement.AppendChild(obj2);

        doc.DocumentElement.AppendChild(sigElement);

        // ── Serializar XML firmado ────────────────────────────────────────────
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true });
        doc.WriteTo(writer);
        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static XmlElement BuildKeyInfo(XmlDocument doc, string certB64, RSA rsa, string keyInfoId)
    {
        var keyInfo = doc.CreateElement("ds", "KeyInfo", XmlDsigNs);
        keyInfo.SetAttribute("Id", keyInfoId);

        var x509Data = doc.CreateElement("ds", "X509Data", XmlDsigNs);
        var x509Cert = doc.CreateElement("ds", "X509Certificate", XmlDsigNs);
        x509Cert.InnerText = certB64;
        x509Data.AppendChild(x509Cert);
        keyInfo.AppendChild(x509Data);

        var keyValue = doc.CreateElement("ds", "KeyValue", XmlDsigNs);
        var rsaKeyValue = doc.CreateElement("ds", "RSAKeyValue", XmlDsigNs);
        var rsaParams = rsa.ExportParameters(false);
        var modulus = doc.CreateElement("ds", "Modulus", XmlDsigNs);
        modulus.InnerText = Convert.ToBase64String(rsaParams.Modulus!);
        var exponent = doc.CreateElement("ds", "Exponent", XmlDsigNs);
        exponent.InnerText = Convert.ToBase64String(rsaParams.Exponent!);
        rsaKeyValue.AppendChild(modulus);
        rsaKeyValue.AppendChild(exponent);
        keyValue.AppendChild(rsaKeyValue);
        keyInfo.AppendChild(keyValue);

        return keyInfo;
    }

    private static XmlElement BuildQualifyingProperties(
        XmlDocument doc, string sigId, string signedPropsId,
        DateTime signingTime, Org.BouncyCastle.X509.X509Certificate cert, string certB64)
    {
        var certHash = SHA1.HashData(cert.GetEncoded());
        var certHashB64 = Convert.ToBase64String(certHash);

        var issuer = cert.IssuerDN.ToString();
        var serial = cert.SerialNumber.ToString();

        var qp = doc.CreateElement("xades", "QualifyingProperties", XadesNs);
        qp.SetAttribute("Target", "#" + sigId);

        var sp = doc.CreateElement("xades", "SignedProperties", XadesNs);
        sp.SetAttribute("Id", signedPropsId);

        var ssp = doc.CreateElement("xades", "SignedSignatureProperties", XadesNs);

        var st = doc.CreateElement("xades", "SigningTime", XadesNs);
        st.InnerText = signingTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        ssp.AppendChild(st);

        var sc = doc.CreateElement("xades", "SigningCertificate", XadesNs);
        var xcert = doc.CreateElement("xades", "Cert", XadesNs);
        var cd = doc.CreateElement("xades", "CertDigest", XadesNs);
        var dm = doc.CreateElement("ds", "DigestMethod", XmlDsigNs);
        dm.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1");
        var dv = doc.CreateElement("ds", "DigestValue", XmlDsigNs);
        dv.InnerText = certHashB64;
        cd.AppendChild(dm); cd.AppendChild(dv);
        xcert.AppendChild(cd);

        var isx = doc.CreateElement("xades", "IssuerSerial", XadesNs);
        var xin = doc.CreateElement("ds", "X509IssuerName", XmlDsigNs);
        xin.InnerText = issuer;
        var xsn = doc.CreateElement("ds", "X509SerialNumber", XmlDsigNs);
        xsn.InnerText = serial;
        isx.AppendChild(xin); isx.AppendChild(xsn);
        xcert.AppendChild(isx);
        sc.AppendChild(xcert);
        ssp.AppendChild(sc);
        sp.AppendChild(ssp);
        qp.AppendChild(sp);
        return qp;
    }

    private static XmlElement BuildSignedInfo(
        XmlDocument doc, string sigId,
        string digestDoc, string digestKeyInfo, string digestSignedProps,
        string keyInfoId, string signedPropsId)
    {
        var si = doc.CreateElement("ds", "SignedInfo", XmlDsigNs);

        var cm = doc.CreateElement("ds", "CanonicalizationMethod", XmlDsigNs);
        cm.SetAttribute("Algorithm", "http://www.w3.org/TR/2001/REC-xml-c14n-20010315");
        si.AppendChild(cm);

        var sm = doc.CreateElement("ds", "SignatureMethod", XmlDsigNs);
        sm.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#rsa-sha1");
        si.AppendChild(sm);

        si.AppendChild(BuildReference(doc, "#comprobante", "comprobante", digestDoc, enveloped: true));
        si.AppendChild(BuildReference(doc, "#" + keyInfoId, null, digestKeyInfo));
        si.AppendChild(BuildReference(doc, "#" + signedPropsId, "SignedPropertiesId", digestSignedProps,
            type: "http://uri.etsi.org/01903#SignedProperties"));

        return si;
    }

    private static XmlElement BuildReference(
        XmlDocument doc, string uri, string? id, string digestValue,
        bool enveloped = false, string? type = null)
    {
        var r = doc.CreateElement("ds", "Reference", XmlDsigNs);
        r.SetAttribute("URI", uri);
        if (id != null) r.SetAttribute("Id", id);
        if (type != null) r.SetAttribute("Type", type);

        if (enveloped)
        {
            var transforms = doc.CreateElement("ds", "Transforms", XmlDsigNs);
            var t = doc.CreateElement("ds", "Transform", XmlDsigNs);
            t.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#enveloped-signature");
            transforms.AppendChild(t);
            r.AppendChild(transforms);
        }

        var dm = doc.CreateElement("ds", "DigestMethod", XmlDsigNs);
        dm.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1");
        r.AppendChild(dm);

        var dv = doc.CreateElement("ds", "DigestValue", XmlDsigNs);
        dv.InnerText = digestValue;
        r.AppendChild(dv);

        return r;
    }

    private static string ComputeDigest(XmlDocument doc, string xpath)
    {
        // XmlDsigC14NTransform solo acepta XmlDocument, XmlNodeList o Stream — no XmlNode directo
        var xpathExpr = xpath.StartsWith('#')
            ? $"//*[@Id='{xpath[1..]}' or @id='{xpath[1..]}']"
            : xpath;

        var nodeList = doc.SelectNodes(xpathExpr);
        if (nodeList == null || nodeList.Count == 0)
            throw new InvalidOperationException($"Nodo {xpath} no encontrado en el XML.");

        using var ms = new MemoryStream();
        var c14n = new XmlDsigC14NTransform();
        c14n.LoadInput(nodeList);
        var output = (Stream)c14n.GetOutput(typeof(Stream));
        output.CopyTo(ms);

        var hash = SHA1.HashData(ms.ToArray());
        return Convert.ToBase64String(hash);
    }

    private static string Canonicalize(XmlElement element)
    {
        // Importar el elemento a un documento temporal para poder pasarlo como XmlDocument
        var tmpDoc = new XmlDocument();
        tmpDoc.AppendChild(tmpDoc.ImportNode(element, true));

        using var ms = new MemoryStream();
        var c14n = new XmlDsigC14NTransform();
        c14n.LoadInput(tmpDoc);
        var output = (Stream)c14n.GetOutput(typeof(Stream));
        output.CopyTo(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
