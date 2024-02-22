using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto.Operators;

[assembly: InternalsVisibleTo("Rnwood.Smtp4dev.Tests")]
namespace Rnwood.Smtp4dev.Server
{
    internal static class SSCertGenerator
    {
        
        public static X509Certificate2 CreateSelfSignedCertificate(string hostname)
        {
            CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
            SecureRandom random = new SecureRandom(randomGenerator);

            X509V3CertificateGenerator certGenerator = new X509V3CertificateGenerator();
            certGenerator.SetSubjectDN(new X509Name("CN=" + hostname));
            certGenerator.SetIssuerDN(new X509Name("CN=" + hostname));

            BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certGenerator.SetSerialNumber(serialNumber);

            certGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(10));

            KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, 2048);

            RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();
            certGenerator.SetPublicKey(keyPair.Public);

            var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private, random);
            var cert = certGenerator.Generate(signatureFactory);


            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            var certificateEntry = new X509CertificateEntry(cert);
            store.SetCertificateEntry("cert", certificateEntry);
            store.SetKeyEntry("cert", new AsymmetricKeyEntry(keyPair.Private), new[] { certificateEntry });
            var stream = new MemoryStream();
            store.Save(stream, "".ToCharArray(), random);

            return new X509Certificate2(
                stream.ToArray(), "",
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        }
    }
}
