using MimeKit;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.UI.Messages
{
    public class ViewMessageViewModel
    {
        internal ViewMessageViewModel(ISmtp4devMessage message)
        {
            try
            {
                using (Stream data = message.GetData())
                {
                    MimeMessage mimeMessage = MimeMessage.Load(data);
                    Subject = mimeMessage.Subject;
                    Headers = mimeMessage.Headers.ToDictionary(h => h.Field, h => h.Value).ToArray();
                }
            }
            catch (FormatException)
            {
            }
        }

        public string Subject { get; private set; }

        public KeyValuePair<string, string>[] Headers { get; private set; }
    }
}