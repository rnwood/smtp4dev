using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using MimeKit.Cryptography;
using Rnwood.Smtp4dev.Server.Settings;
using Serilog;

namespace Rnwood.Smtp4dev.Server
{
    internal static class CertificateHelper
    {
        public static X509Certificate2 LoadCertificateWithKey(string certificatePath, string certificateKeyPath, string password)
        {
            var certNoKey = new X509Certificate2(certificatePath);
            string keyAlgo = certNoKey.GetKeyAlgorithm();

            byte[] pfxData;

            switch (keyAlgo)
            {
                case "1.2.840.113549.1.1.1":
                    {
                        using var rsa = RSA.Create();
                        rsa.ImportFromPem(File.ReadAllText(certificateKeyPath));
                        pfxData = certNoKey.CopyWithPrivateKey(rsa).Export(X509ContentType
                                        .Pfx);
                        break;
                    }
                case "1.2.840.10040.4.1":
                    {
                        using var dsa = DSA.Create();
                        dsa.ImportFromPem(File.ReadAllText(certificateKeyPath));
                        pfxData = certNoKey.CopyWithPrivateKey(dsa).Export(X509ContentType
                                        .Pfx);
                        break;
                    }
                case "1.2.840.10045.2.1":
                    {
                        using var ecdsa = ECDsa.Create();
                        ecdsa.ImportFromPem(File.ReadAllText(certificateKeyPath));
                        pfxData = certNoKey.CopyWithPrivateKey(ecdsa).Export(X509ContentType
                                        .Pfx);
                        break;
                    }
                default:
                    throw new Exception($"Unsupported cert key algo {keyAlgo}. Supported: RSA, DSA, ECDsa");
            }

            

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

            if (!string.IsNullOrEmpty(options.SslProtocols))
            {
                logger.Information("SSL protocols: {SslProtocols}", options.SslProtocols);
            }

            if (!string.IsNullOrEmpty(options.TlsCipherSuites))
            {
                logger.Information("TLS cipher suites: {TlsCipherSuites}", options.TlsCipherSuites);
            }

            if (options.TlsMode != TlsMode.None)
            {
                if (!string.IsNullOrEmpty(options.TlsCertificateStoreThumbprint))
                {
                    cert = CertificateHelper.LoadCertificateWithThumbprint(options.TlsCertificateStoreThumbprint);

                    if (cert == null)
                    {
                        throw new Exception($"No certificate found on local machine for thumbprint {options.TlsCertificateStoreThumbprint}");
                    }
                    logger.Information("Using provided certificate from Store with Subject {SubjectName}, expiry {ExpiryDate}", cert.SubjectName.Name,
                        cert.GetExpirationDateString());
                }
                else if (!string.IsNullOrEmpty(options.TlsCertificate))
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

                    if (!cert.HasPrivateKey)
                    {
                        throw new Exception($"Certificate at '{options.TlsCertificate}' has no private key.");
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
                            DateTime.Parse(cert.GetExpirationDateString()) < DateTime.Now.AddDays(30)
                            || !cert.GetSubjectDnsNames().Contains(options.HostName))
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
            }
            else
            {

                return new X509Certificate2(certificatePath, password);
            }
        }

        public static X509Certificate2 LoadCertificateWithThumbprint(string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            return store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, false)
                .OfType<X509Certificate2>()
                .FirstOrDefault();
        }
    }
}