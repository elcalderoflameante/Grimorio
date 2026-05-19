using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;

namespace Grimorio.Infrastructure.Services.Sri;

// Firma XML de factura con XAdES-BES segun la especificacion tecnica del SRI Ecuador.
// SHA-1 y RSA-SHA1 son obligatorios para este formato.
public static class SriXmlSigner
{
    private const string EtsiNs = "http://uri.etsi.org/01903/v1.3.2#";
    private const string XmlDsNs = "http://www.w3.org/2000/09/xmldsig#";
    private const string SignedPropertiesType = "http://uri.etsi.org/01903#SignedProperties";

    public static string Sign(string unsignedXml, byte[] p12Bytes, string p12Password)
    {
        var store = new Pkcs12StoreBuilder().Build();
        store.Load(new MemoryStream(p12Bytes), p12Password.ToCharArray());
        var alias = store.Aliases.Cast<string>().First(a => store.IsKeyEntry(a));

        var bCert = store.GetCertificate(alias).Certificate;
        var bKey = (RsaPrivateCrtKeyParameters)store.GetKey(alias).Key;
        using var x509 = X509CertificateLoader.LoadCertificate(bCert.GetEncoded());

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = bKey.Modulus.ToByteArrayUnsigned(),
            Exponent = bKey.PublicExponent.ToByteArrayUnsigned(),
            D = bKey.Exponent.ToByteArrayUnsigned(),
            P = bKey.P.ToByteArrayUnsigned(),
            Q = bKey.Q.ToByteArrayUnsigned(),
            DP = bKey.DP.ToByteArrayUnsigned(),
            DQ = bKey.DQ.ToByteArrayUnsigned(),
            InverseQ = bKey.QInv.ToByteArrayUnsigned(),
        });

        var rnd = new Random().Next(100000, 999999).ToString();
        var sigId = $"Signature{rnd}";
        var signedPropsId = $"Signature{rnd}-SignedProperties{rnd}";
        var keyInfoId = $"Signature{rnd}-KeyInfo";
        var refId = $"Reference-ID-{rnd}";
        var objId = $"Signature{rnd}-Object{rnd}";

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(unsignedXml);

        var keyInfo = BuildKeyInfo(x509, rsa, keyInfoId);
        var signedXml = new SriSignedXml(doc)
        {
            SigningKey = rsa,
            KeyInfo = keyInfo
        };
        signedXml.Signature.Id = sigId;
        signedXml.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

        var qualProps = BuildQualifyingProperties(doc, sigId, signedPropsId, refId, DateTime.UtcNow, bCert);
        var objectData = doc.CreateDocumentFragment();
        objectData.AppendChild(qualProps.CloneNode(true));
        signedXml.AddObject(new DataObject
        {
            Id = objId,
            Data = objectData.ChildNodes
        });

        // signatureContext provee contexto de DOM para KeyInfo y SignedProperties.
        // NO se añade al documento para que el digest de #comprobante se calcule
        // sobre <factura> sin hijos Signature — igual que lo que verificará el SRI.
        var signatureContext = BuildSignatureContext(doc, sigId, keyInfo, objId, qualProps);
        signedXml.RegisterId(keyInfoId, (XmlElement)signatureContext.GetElementsByTagName("KeyInfo", XmlDsNs)[0]!);
        signedXml.RegisterId(signedPropsId, (XmlElement)signatureContext.GetElementsByTagName("SignedProperties", EtsiNs)[0]!);

        // El SRI espera estas referencias en orden: SignedProperties, KeyInfo, comprobante.
        signedXml.AddReference(MakeReference("#" + signedPropsId, type: SignedPropertiesType));
        signedXml.AddReference(MakeReference("#" + keyInfoId));
        signedXml.AddReference(MakeReference("#comprobante", refId, new XmlDsigEnvelopedSignatureTransform()));

        signedXml.ComputeSignature();

        doc.DocumentElement!.AppendChild(doc.ImportNode(signedXml.GetXml(), true));

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
               { Encoding = new UTF8Encoding(false), Indent = false }))
        {
            doc.WriteTo(writer);
        }

        return new UTF8Encoding(false).GetString(ms.ToArray());
    }

    private static KeyInfo BuildKeyInfo(X509Certificate2 cert, RSA rsa, string keyInfoId)
    {
        var keyInfo = new KeyInfo { Id = keyInfoId };
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        keyInfo.AddClause(new RSAKeyValue(rsa));
        return keyInfo;
    }

    private static XmlElement BuildQualifyingProperties(
        XmlDocument doc,
        string sigId,
        string signedPropsId,
        string refId,
        DateTime signingTime,
        Org.BouncyCastle.X509.X509Certificate cert)
    {
        var certHashB64 = Convert.ToBase64String(SHA1.HashData(cert.GetEncoded()));
        var issuer = cert.IssuerDN.ToString();
        var serial = cert.SerialNumber.ToString();

        var qp = doc.CreateElement("etsi", "QualifyingProperties", EtsiNs);
        qp.SetAttribute("Target", "#" + sigId);

        var sp = doc.CreateElement("etsi", "SignedProperties", EtsiNs);
        sp.SetAttribute("Id", signedPropsId);

        var ssp = doc.CreateElement("etsi", "SignedSignatureProperties", EtsiNs);
        var st = doc.CreateElement("etsi", "SigningTime", EtsiNs);
        st.InnerText = signingTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        ssp.AppendChild(st);

        var sc = doc.CreateElement("etsi", "SigningCertificate", EtsiNs);
        var xc = doc.CreateElement("etsi", "Cert", EtsiNs);
        var cd = doc.CreateElement("etsi", "CertDigest", EtsiNs);
        var dm = doc.CreateElement("ds", "DigestMethod", XmlDsNs);
        dm.SetAttribute("Algorithm", SignedXml.XmlDsigSHA1Url);
        var dv = doc.CreateElement("ds", "DigestValue", XmlDsNs);
        dv.InnerText = certHashB64;
        cd.AppendChild(dm);
        cd.AppendChild(dv);
        xc.AppendChild(cd);

        var isx = doc.CreateElement("etsi", "IssuerSerial", EtsiNs);
        var xin = doc.CreateElement("ds", "X509IssuerName", XmlDsNs);
        xin.InnerText = issuer;
        var xsn = doc.CreateElement("ds", "X509SerialNumber", XmlDsNs);
        xsn.InnerText = serial;
        isx.AppendChild(xin);
        isx.AppendChild(xsn);
        xc.AppendChild(isx);
        sc.AppendChild(xc);
        ssp.AppendChild(sc);
        sp.AppendChild(ssp);

        var sdop = doc.CreateElement("etsi", "SignedDataObjectProperties", EtsiNs);
        var dof = doc.CreateElement("etsi", "DataObjectFormat", EtsiNs);
        dof.SetAttribute("ObjectReference", "#" + refId);
        var desc = doc.CreateElement("etsi", "Description", EtsiNs);
        desc.InnerText = "contenido comprobante";
        var mime = doc.CreateElement("etsi", "MimeType", EtsiNs);
        mime.InnerText = "text/xml";
        dof.AppendChild(desc);
        dof.AppendChild(mime);
        sdop.AppendChild(dof);
        sp.AppendChild(sdop);

        qp.AppendChild(sp);
        return qp;
    }

    private static Reference MakeReference(
        string uri,
        string? id = null,
        Transform? transform = null,
        string? type = null)
    {
        var reference = new Reference(uri)
        {
            Id = id,
            Type = type,
            DigestMethod = SignedXml.XmlDsigSHA1Url
        };

        if (transform != null)
            reference.AddTransform(transform);

        return reference;
    }

    private static XmlElement BuildSignatureContext(
        XmlDocument doc,
        string sigId,
        KeyInfo keyInfo,
        string objectId,
        XmlElement qualifyingProperties)
    {
        var signature = doc.CreateElement("Signature", XmlDsNs);
        signature.SetAttribute("Id", sigId);
        signature.AppendChild(doc.ImportNode(keyInfo.GetXml(), true));

        var obj = doc.CreateElement("Object", XmlDsNs);
        obj.SetAttribute("Id", objectId);
        obj.AppendChild(qualifyingProperties);
        signature.AppendChild(obj);
        return signature;
    }

    private sealed class SriSignedXml : SignedXml
    {
        private readonly Dictionary<string, XmlElement> _knownElementsById = new();

        public SriSignedXml(XmlDocument document) : base(document) { }

        public void RegisterId(string id, XmlElement element)
        {
            _knownElementsById[id] = element;
        }

        public override XmlElement? GetIdElement(XmlDocument? document, string idValue)
        {
            if (_knownElementsById.TryGetValue(idValue, out var knownElement))
                return knownElement;

            if (document == null)
                return null;

            var element = base.GetIdElement(document, idValue);
            if (element != null)
                return element;

            element = document.SelectSingleNode(BuildIdXPath(idValue)) as XmlElement;
            return element;
        }

        private static string BuildIdXPath(string idValue)
        {
            var escaped = idValue.Replace("'", "&apos;");
            return $"//*[@Id='{escaped}' or @id='{escaped}' or @ID='{escaped}']";
        }
    }
}
