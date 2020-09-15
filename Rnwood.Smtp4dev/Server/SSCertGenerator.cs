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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    internal class SSCertGenerator
    {
        
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateSelfSignedCertificate(string hostname)
        {
            CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
            SecureRandom random = new SecureRandom(randomGenerator);

            X509V3CertificateGenerator certGenerator = new X509V3CertificateGenerator();
            certGenerator.SetSubjectDN(new X509Name("CN=" + hostname));
            certGenerator.SetIssuerDN(new X509Name("CN=" + hostname));

            BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certGenerator.SetSerialNumber(serialNumber);
            certGenerator.SetSignatureAlgorithm("SHA256WithRSA");

            certGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(10));

            KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, 2048);

            RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            AsymmetricCipherKeyPair keypair = keyPairGenerator.GenerateKeyPair();
            certGenerator.SetPublicKey(keypair.Public);

            var cert = certGenerator.Generate(keypair.Private, random);


            Pkcs12Store store = new Pkcs12Store();
            var certificateEntry = new X509CertificateEntry(cert);
            store.SetCertificateEntry("cert", certificateEntry);
            store.SetKeyEntry("cert", new AsymmetricKeyEntry(keypair.Private), new[] { certificateEntry });
            var stream = new MemoryStream();
            store.Save(stream, "".ToCharArray(), random);

            return new X509Certificate2(
                stream.ToArray(), "",
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        }
    }
}
