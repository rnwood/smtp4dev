using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Rnwood.Smtp4dev.Server
{
    public static class CertificateHelper
    {
        /// <summary>
        /// Load certificate and private key
        /// </summary>
        /// <param name="certificatePath"></param>
        /// <param name="certificateKeyPath"></param>
        /// <returns>Exported x509 Certificate</returns>
        public static X509Certificate2 LoadCertificateWithKey(string certificatePath, string certificateKeyPath, string password)
        {
            using var rsa = RSA.Create();
            var keyPem = File.ReadAllText(certificateKeyPath);
            var keyDer = CertificateHelper.UnPem(keyPem);
            rsa.ImportPkcs8PrivateKey(keyDer, out _);
            var certNoKey = new X509Certificate2(certificatePath);
            var pfxData = certNoKey.CopyWithPrivateKey(rsa).Export(X509ContentType
                            .Pfx);

            if (string.IsNullOrEmpty(password))
            {
                return new X509Certificate2(pfxData);
            }
            else
            {
                return new X509Certificate2(pfxData, password);
            }
        }

        public static X509Certificate2 LoadCertificate(string certificatePath, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new X509Certificate2(certificatePath);
            } else
            {

                return new X509Certificate2(certificatePath, password);
            }
        }

        /// <summary>
        /// This is a shortcut that assumes valid PEM
        /// -----BEGIN words-----\r\nbase64\r\n-----END words-----
        /// </summary>
        /// <param name="pem"></param>
        /// <returns></returns>
        public static byte[] UnPem(string pem)
        {
            const string dashes = "-----";
            const string newLine = "\r\n";
            pem = NormalizeLineEndings(pem);
            var index0 = pem.IndexOf(dashes, StringComparison.Ordinal);
            var index1 = pem.IndexOf(newLine, index0 + dashes.Length, StringComparison.Ordinal) + newLine.Length;
            var index2 = pem.IndexOf(dashes, index1, StringComparison.Ordinal) - newLine.Length; //TODO: /n
            return Convert.FromBase64String(pem.Substring(index1, index2 - index1));
        }

        private static string NormalizeLineEndings(string val)
        {
            return Regex.Replace(val, @"\r\n|\n\r|\n|\r", "\r\n");
        }
    }
}