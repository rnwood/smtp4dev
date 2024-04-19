using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Rnwood.Smtp4dev.Server.Settings;
using Serilog;

namespace Rnwood.Smtp4dev.Server
{
    internal static class CertificateHelper
    {
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
        
           public static X509Certificate2 GetTlsCertificate(ServerOptions options, ILogger logger)
        {
            X509Certificate2 cert = null;

            logger.Information("TLS mode: {TLSMode}", options.TlsMode);

            if (options.TlsMode != TlsMode.None)
            {
                if (!string.IsNullOrEmpty(options.TlsCertificate))
                {
                    var pfxPassword = options.TlsCertificatePassword ?? "";

                    if (string.IsNullOrEmpty(options.TlsCertificatePrivateKey))
                    {
                        cert = CertificateHelper.LoadCertificate(options.TlsCertificate, pfxPassword);
                    }
                    else
                    {
                        cert = CertificateHelper.LoadCertificateWithKey(options.TlsCertificate,
                            options.TlsCertificatePrivateKey, pfxPassword);
                    }

                    logger.Information("Using provided certificate with Subject {SubjectName}, expiry {ExpiryDate}", cert.SubjectName.Name,
                        cert.GetExpirationDateString());
                }
                else
                {
                    string pfxPath = Path.GetFullPath("selfsigned-certificate.pfx");
                    string cerPath = Path.GetFullPath("selfsigned-certificate.cer");

                    if (File.Exists(pfxPath))
                    {
                        cert = new X509Certificate2(File.ReadAllBytes(pfxPath), "",
                            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                        if (cert.Subject != $"CN={options.HostName}" ||
                            DateTime.Parse(cert.GetExpirationDateString()) < DateTime.Now.AddDays(30))
                        {
                            cert = null;
                        }
                        else
                        {
                            logger.Information(
                                "Using existing self-signed certificate with subject name {Hostname} and expiry date {ExpirationDate}",
                                options.HostName,
                                cert.GetExpirationDateString());
                        }
                    }

                    if (cert == null)
                    {
                        cert = SSCertGenerator.CreateSelfSignedCertificate(options.HostName);
                        File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pkcs12));
                        File.WriteAllBytes(cerPath, cert.Export(X509ContentType.Cert));
                        logger.Information("Generated new self-signed certificate with subject name '{Hostname} and expiry date {ExpirationDate}",
                            options.HostName,
                            cert.GetExpirationDateString());
                    }


                    logger.Information(
                        "Ensure that the hostname you enter into clients and '{Hostname}' from ServerOptions:HostName configuration match exactly and trust the issuer certificate at {cerPath} in your client/OS to avoid certificate validation errors.",
                        options.HostName, cerPath);
                }
            }

            return cert;
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