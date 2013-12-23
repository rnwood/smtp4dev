using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rnwood.Smtp4dev.Service
{
    public class ServiceSettings : IServerSettings
    {
        public ServiceSettings()
        {
            baseKey = Registry.LocalMachine.CreateSubKey("Software").CreateSubKey("Smtp4dev").CreateSubKey("ServiceSettings");
        }

        private RegistryKey baseKey;

        public bool CauseTimeout
        {
            get
            {
                return bool.Parse((string) baseKey.GetValue("CauseTimeout", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("CauseTimeout", value.ToString());
            }
        }

        public bool DefaultTo8Bit
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("DefaultTo8Bit", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("DefaultTo8Bit", value.ToString());
            }
        }

        public string DomainName
        {
            get
            {
                return (string)baseKey.GetValue("DomainName", Environment.MachineName);
            }
            set
            {
                baseKey.SetValue("DomainName", value);
            }
        }

        public bool Enable8BITMIME
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("Enable8BITMIME", bool.TrueString));
            }
            set
            {
                baseKey.SetValue("Enable8BITMIME", value.ToString());
            }
        }

        public bool EnableAUTH
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("EnableAUTH", bool.TrueString));
            }
            set
            {
                baseKey.SetValue("EnableAUTH", value.ToString());
            }
        }

        public bool EnableSIZE
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("EnableSIZE", bool.TrueString));
            }
            set
            {
                baseKey.SetValue("EnableSIZE", value.ToString());
            }
        }

        public bool EnableSSL
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("EnableSSL", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("EnableSSL", value.ToString());
            }
        }

        public bool EnableSTARTTLS
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("EnableSTARTTLS", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("EnableSTARTTLS", value.ToString());
            }
        }

        public bool FailAuthentication
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("FailAuthentication", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("FailAuthentication", value.ToString());
            }
        }

        public string IPAddress
        {
            get
            {
                return (string)baseKey.GetValue("IPAddress", System.Net.IPAddress.Any.ToString());
            }
            set
            {
                baseKey.SetValue("IPAddress", value);
            }
        }

        public long MaximumMessageSize
        {
            get
            {
                return (long)baseKey.GetValue("MaximumMessageSize", 0);
            }
            set
            {
                baseKey.SetValue("MaximumMessageSize", value);
            }
        }

        public int MaxMessages
        {
            get
            {
                return (int)baseKey.GetValue("MaxMessages", 100);
            }
            set
            {
                baseKey.SetValue("MaxMessages", value);
            }
        }

        public string CustomMessageFolder
        {
            get
            {
                return (string)baseKey.GetValue("CustomMessageFolder", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                     "smtp4dev\\Messages"));
            }
            set
            {
                baseKey.SetValue("CustomMessageFolder", value);
            }
        }

        public bool OnlyAllowClearTextAuthOverSecureConnection
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("OnlyAllowClearTextAuthOverSecureConnection", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("OnlyAllowClearTextAuthOverSecureConnection", value.ToString());
            }
        }

        public int PortNumber
        {
            get
            {
                return (int)baseKey.GetValue("PortNumber", 25);
            }
            set
            {
                baseKey.SetValue("PortNumber", value);
            }
        }

        public int ReceiveTimeout
        {
            get
            {
                return (int) baseKey.GetValue("ReceiveTimeout", 30000);
            }
            set
            {
                baseKey.SetValue("ReceiveTimeout", value);
            }
        }

        public bool RejectMessages
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("RejectMessages", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("RejectMessages", value.ToString());
            }
        }

        public bool RejectRecipients
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("RejectRecipients", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("RejectRecipients", value.ToString());
            }
        }

        public bool RequireAuthentication
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("RequireAuthentication", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("RequireAuthentication", value.ToString());
            }
        }

        public bool RequireSecureConnection
        {
            get
            {
                return bool.Parse((string)baseKey.GetValue("RequireSecureConnection", bool.FalseString));
            }
            set
            {
                baseKey.SetValue("RequireSecureConnection", value.ToString());
            }
        }

        public string SSLCertificatePassword
        {
            get
            {
                return (string)baseKey.GetValue("SSLCertificatePassword", null);
            }
            set
            {
                baseKey.SetValue("SSLCertificatePassword", value);
            }
        }

        public string SSLCertificatePath
        {
            get
            {
                return (string)baseKey.GetValue("SSLCertificatePath", null);
            }
            set
            {
                baseKey.SetValue("SSLCertificatePath", value);
            }
        }
    }
}
