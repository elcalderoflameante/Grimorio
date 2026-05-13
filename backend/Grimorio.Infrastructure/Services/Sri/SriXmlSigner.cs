using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Grimorio.Infrastructure.Services.Sri;

// Firma XML de factura con XAdES-BES según la especificación técnica del SRI Ecuador.
// SHA-1 y RSA-SHA1 son obligatorios según dicha especificación.
public static class SriXmlSigner
{
    private const string EtsiNs  = "http://uri.etsi.org/01903/v1.3.2#";
    private const string XmlDsNs = "http://www.w3.org/2000/09/xmldsig#";

    public static string Sign(string unsignedXml, byte[] p12Bytes, string p12Password)
    {
        // ── Cargar certificado y clave privada desde .p12 ─────────────────────
        var store = new Pkcs12StoreBuilder().Build();
        store.Load(new MemoryStream(p12Bytes), p12Password.ToCharArray());
        var alias = store.Aliases.Cast<string>().First(a => store.IsKeyEntry(a));

        var bCert = store.GetCertificate(alias).Certificate;
        var bKey  = (RsaPrivateCrtKeyParameters)store.GetKey(alias).Key;

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus  = bKey.Modulus.ToByteArrayUnsigned(),
            Exponent = bKey.PublicExponent.ToByteArrayUnsigned(),
            D        = bKey.Exponent.ToByteArrayUnsigned(),
            P        = bKey.P.ToByteArrayUnsigned(),
            Q        = bKey.Q.ToByteArrayUnsigned(),
            DP       = bKey.DP.ToByteArrayUnsigned(),
            DQ       = bKey.DQ.ToByteArrayUnsigned(),
            InverseQ = bKey.QInv.ToByteArrayUnsigned(),
        });
        var certB64 = Convert.ToBase64String(bCert.GetEncoded());

        // ── IDs únicos para nodos de firma ────────────────────────────────────
        var rnd           = new Random().Next(100000, 999999).ToString();
        var sigId         = $"Signature{rnd}";
        var signedPropsId = $"Signature{rnd}-SignedProperties{rnd}";
        var keyInfoId     = $"Signature{rnd}-KeyInfo";
        var refId         = $"Reference-ID-{rnd}";
        var objId         = $"Signature{rnd}-Object{rnd}";

        // ── Cargar documento XML ──────────────────────────────────────────────
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(unsignedXml);

        // ── PASO 1: digest del comprobante ANTES de agregar nodos de firma ────
        // Si se calcula después, el contenido del elemento <factura> cambiaría.
        string digestDoc = ComputeDigestById(doc, "comprobante");

        var signingTime = DateTime.UtcNow;

        // ── PASO 2: construir ds:Signature con KeyInfo y Object ───────────────
        var sigEl = doc.CreateElement("ds", "Signature", XmlDsNs);
        sigEl.SetAttribute("Id", sigId);

        var keyInfo = BuildKeyInfo(doc, certB64, rsa, keyInfoId);
        sigEl.AppendChild(keyInfo);

        var qualProps = BuildQualifyingProperties(doc, sigId, signedPropsId, refId,
                                                  signingTime, bCert, certB64);
        var objEl = doc.CreateElement("ds", "Object", XmlDsNs);
        objEl.SetAttribute("Id", objId);
        objEl.AppendChild(qualProps);
        sigEl.AppendChild(objEl);

        // ── PASO 3: añadir sigEl al doc para establecer contexto de namespaces ─
        // Es necesario para que C14N calcule xmlns:ds como heredado del padre,
        // igual que ocurrirá cuando el verificador del SRI valide los digests.
        doc.DocumentElement!.AppendChild(sigEl);

        // ── PASO 4: calcular digests con el contexto correcto de NS ──────────
        string digestKeyInfo     = ComputeDigestById(doc, keyInfoId);
        string digestSignedProps = ComputeDigestById(doc, signedPropsId);

        // ── PASO 5: construir SignedInfo e insertarlo como primer hijo de sigEl
        // Orden de referencias según especificación XAdES-BES del SRI:
        //   SignedProperties → KeyInfo → comprobante
        var signedInfo = BuildSignedInfo(doc, digestDoc, digestKeyInfo, digestSignedProps,
                                         keyInfoId, signedPropsId, refId);
        sigEl.InsertBefore(signedInfo, sigEl.FirstChild);

        // ── PASO 6: canonicalizar SignedInfo desde su posición en el documento ─
        // Usar SelectNodes desde sigEl asegura que xmlns:ds se herede del padre
        // y no aparezca redundantemente en ds:SignedInfo — igual que hará el SRI.
        var ns     = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("ds", XmlDsNs);
        var siList = sigEl.SelectNodes("ds:SignedInfo", ns)!;

        using var siMs = new MemoryStream();
        var siC14n = new XmlDsigC14NTransform();
        siC14n.LoadInput(siList);
        ((Stream)siC14n.GetOutput(typeof(Stream))).CopyTo(siMs);

        // ── PASO 7: firmar ────────────────────────────────────────────────────
        byte[] sigBytes = rsa.SignData(siMs.ToArray(), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        string sigB64   = Convert.ToBase64String(sigBytes);

        // ── PASO 8: insertar SignatureValue entre SignedInfo y KeyInfo ─────────
        var sigVal = doc.CreateElement("ds", "SignatureValue", XmlDsNs);
        sigVal.SetAttribute("Id", $"SignatureValue{rnd}");
        sigVal.InnerText = sigB64;
        sigEl.InsertBefore(sigVal, keyInfo);
        // Estructura final: SignedInfo → SignatureValue → KeyInfo → Object

        // ── Serializar XML firmado en UTF-8 real ──────────────────────────────
        // Indent=false es crítico: el indentado agregaría nodos de texto con
        // espacios dentro de ds:SignedInfo que no estaban al momento de firmar,
        // haciendo que la canonicalización del SRI difiera de la nuestra.
        using var ms = new MemoryStream();
        using (var w = XmlWriter.Create(ms, new XmlWriterSettings
               { Encoding = new UTF8Encoding(false), Indent = false }))
        {
            doc.WriteTo(w);
        }
        return new UTF8Encoding(false).GetString(ms.ToArray());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static XmlElement BuildKeyInfo(XmlDocument doc, string certB64, RSA rsa, string keyInfoId)
    {
        var ki = doc.CreateElement("ds", "KeyInfo", XmlDsNs);
        ki.SetAttribute("Id", keyInfoId);

        var x509d = doc.CreateElement("ds", "X509Data", XmlDsNs);
        var x509c = doc.CreateElement("ds", "X509Certificate", XmlDsNs);
        x509c.InnerText = certB64;
        x509d.AppendChild(x509c);
        ki.AppendChild(x509d);

        var kv    = doc.CreateElement("ds", "KeyValue", XmlDsNs);
        var rsakv = doc.CreateElement("ds", "RSAKeyValue", XmlDsNs);
        var p     = rsa.ExportParameters(false);
        var mod   = doc.CreateElement("ds", "Modulus", XmlDsNs);
        mod.InnerText = Convert.ToBase64String(p.Modulus!);
        var exp = doc.CreateElement("ds", "Exponent", XmlDsNs);
        exp.InnerText = Convert.ToBase64String(p.Exponent!);
        rsakv.AppendChild(mod);
        rsakv.AppendChild(exp);
        kv.AppendChild(rsakv);
        ki.AppendChild(kv);

        return ki;
    }

    private static XmlElement BuildQualifyingProperties(
        XmlDocument doc, string sigId, string signedPropsId, string refId,
        DateTime signingTime, Org.BouncyCastle.X509.X509Certificate cert, string certB64)
    {
        var certHashB64 = Convert.ToBase64String(SHA1.HashData(cert.GetEncoded()));
        var issuer      = cert.IssuerDN.ToString();
        var serial      = cert.SerialNumber.ToString();

        var qp = doc.CreateElement("etsi", "QualifyingProperties", EtsiNs);
        qp.SetAttribute("Target", "#" + sigId);

        var sp = doc.CreateElement("etsi", "SignedProperties", EtsiNs);
        sp.SetAttribute("Id", signedPropsId);

        // ── SignedSignatureProperties ─────────────────────────────────────────
        var ssp = doc.CreateElement("etsi", "SignedSignatureProperties", EtsiNs);

        var st = doc.CreateElement("etsi", "SigningTime", EtsiNs);
        st.InnerText = signingTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        ssp.AppendChild(st);

        var sc  = doc.CreateElement("etsi", "SigningCertificate", EtsiNs);
        var xc  = doc.CreateElement("etsi", "Cert", EtsiNs);
        var cd  = doc.CreateElement("etsi", "CertDigest", EtsiNs);
        var dm  = doc.CreateElement("ds", "DigestMethod", XmlDsNs);
        dm.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1");
        var dv = doc.CreateElement("ds", "DigestValue", XmlDsNs);
        dv.InnerText = certHashB64;
        cd.AppendChild(dm); cd.AppendChild(dv);
        xc.AppendChild(cd);

        var isx = doc.CreateElement("etsi", "IssuerSerial", EtsiNs);
        var xin = doc.CreateElement("ds", "X509IssuerName", XmlDsNs);
        xin.InnerText = issuer;
        var xsn = doc.CreateElement("ds", "X509SerialNumber", XmlDsNs);
        xsn.InnerText = serial;
        isx.AppendChild(xin); isx.AppendChild(xsn);
        xc.AppendChild(isx);
        sc.AppendChild(xc);
        ssp.AppendChild(sc);
        sp.AppendChild(ssp);

        // ── SignedDataObjectProperties ────────────────────────────────────────
        var sdop = doc.CreateElement("etsi", "SignedDataObjectProperties", EtsiNs);
        var dof  = doc.CreateElement("etsi", "DataObjectFormat", EtsiNs);
        dof.SetAttribute("ObjectReference", "#" + refId);
        var desc = doc.CreateElement("etsi", "Description", EtsiNs);
        desc.InnerText = "contenido comprobante";
        var mime = doc.CreateElement("etsi", "MimeType", EtsiNs);
        mime.InnerText = "text/xml";
        dof.AppendChild(desc); dof.AppendChild(mime);
        sdop.AppendChild(dof);
        sp.AppendChild(sdop);

        qp.AppendChild(sp);
        return qp;
    }

    private static XmlElement BuildSignedInfo(
        XmlDocument doc,
        string digestDoc, string digestKeyInfo, string digestSignedProps,
        string keyInfoId, string signedPropsId, string refId)
    {
        var si = doc.CreateElement("ds", "SignedInfo", XmlDsNs);

        var cm = doc.CreateElement("ds", "CanonicalizationMethod", XmlDsNs);
        cm.SetAttribute("Algorithm", "http://www.w3.org/TR/2001/REC-xml-c14n-20010315");
        si.AppendChild(cm);

        var sm = doc.CreateElement("ds", "SignatureMethod", XmlDsNs);
        sm.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#rsa-sha1");
        si.AppendChild(sm);

        // Orden obligatorio según especificación SRI: SignedProperties → KeyInfo → comprobante
        si.AppendChild(MakeRef(doc, "#" + signedPropsId, null, digestSignedProps,
            type: "http://uri.etsi.org/01903#SignedProperties"));
        si.AppendChild(MakeRef(doc, "#" + keyInfoId, null, digestKeyInfo));
        si.AppendChild(MakeRef(doc, "#comprobante", refId, digestDoc, enveloped: true));

        return si;
    }

    private static XmlElement MakeRef(XmlDocument doc, string uri, string? id,
        string digestValue, bool enveloped = false, string? type = null)
    {
        var r = doc.CreateElement("ds", "Reference", XmlDsNs);
        r.SetAttribute("URI", uri);
        if (id   != null) r.SetAttribute("Id",   id);
        if (type != null) r.SetAttribute("Type", type);

        if (enveloped)
        {
            var xf = doc.CreateElement("ds", "Transforms", XmlDsNs);
            var t  = doc.CreateElement("ds", "Transform",  XmlDsNs);
            t.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#enveloped-signature");
            xf.AppendChild(t);
            r.AppendChild(xf);
        }

        var dm = doc.CreateElement("ds", "DigestMethod", XmlDsNs);
        dm.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1");
        r.AppendChild(dm);

        var dv = doc.CreateElement("ds", "DigestValue", XmlDsNs);
        dv.InnerText = digestValue;
        r.AppendChild(dv);

        return r;
    }

    private static string ComputeDigestById(XmlDocument doc, string elementId)
    {
        var nodeList = doc.SelectNodes($"//*[@Id='{elementId}' or @id='{elementId}']");
        if (nodeList == null || nodeList.Count == 0)
            throw new InvalidOperationException($"Elemento con Id='{elementId}' no encontrado en el XML.");

        using var ms = new MemoryStream();
        var c14n     = new XmlDsigC14NTransform();
        c14n.LoadInput(nodeList);
        ((Stream)c14n.GetOutput(typeof(Stream))).CopyTo(ms);
        return Convert.ToBase64String(SHA1.HashData(ms.ToArray()));
    }
}
