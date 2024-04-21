using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Rnwood.Smtp4dev.Server.Settings
{
    [TypeConverter(typeof(UserFromStringConverter))]
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserFromStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string stringValue =  value as string;

            string[] values = stringValue.Split('=', 2);

            return new User { Username = values[0], Password = values.Last() ?? "" };
        }
    }
}
