using System;
namespace Rnwood.Smtp4dev
{
    interface IServerSettings
    {
        bool CauseTimeout { get; set; }
        bool DefaultTo8Bit { get; set; }
        string DomainName { get; set; }
        bool Enable8BITMIME { get; set; }
        bool EnableAUTH { get; set; }
        bool EnableSIZE { get; set; }
        bool EnableSSL { get; set; }
        bool EnableSTARTTLS { get; set; }
        bool FailAuthentication { get; set; }
        string IPAddress { get; set; }
        long MaximumMessageSize { get; set; }
        int MaxMessages { get; set; }
        string CustomMessageFolder { get; set; }
        bool OnlyAllowClearTextAuthOverSecureConnection { get; set; }
        int PortNumber { get; set; }
        int ReceiveTimeout { get; set; }
        bool RejectMessages { get; set; }
        bool RejectRecipients { get; set; }
        bool RequireAuthentication { get; set; }
        bool RequireSecureConnection { get; set; }
        string SSLCertificatePassword { get; set; }
        string SSLCertificatePath { get; set; }
    }
}
