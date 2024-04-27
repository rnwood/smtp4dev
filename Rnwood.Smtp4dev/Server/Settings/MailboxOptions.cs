using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Mail;

namespace Rnwood.Smtp4dev.Server.Settings
{
    [TypeConverter(typeof(MailboxFromStringConverter))]
    public record MailboxOptions
    {
        public string Name { get; set; }
        public string Recipients { get; set; }

        internal const string DEFAULTNAME = "Default";
    }

    public class MailboxFromStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string stringValue =  value as string;

            string[] values = stringValue.Split('=', 2);

            if (values.Length != 2)
            {
                throw new FormatException("Mailbox must be in format \"Name:Recipients\"");
            }

            return new MailboxOptions { Name = values[0], Recipients = values[1]};
        }
    }
}
