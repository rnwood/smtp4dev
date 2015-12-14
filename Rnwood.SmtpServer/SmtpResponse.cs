#region

using System;
using System.Text;

#endregion

namespace Rnwood.SmtpServer
{
    public class SmtpResponse : IEquatable<SmtpResponse>
    {
        public SmtpResponse(int code, string message, params object[] args)
        {
            Code = code;
            Message = string.Format(message, args);
        }

        public SmtpResponse(StandardSmtpResponseCode code, string message, params object[] args)
            : this((int) code, message, args)
        {
        }

        public int Code { get; private set; }

        public string Message { get; private set; }

        public bool IsError
        {
            get { return Code >= 500 && Code <= 599; }
        }

        public bool IsSuccess
        {
            get { return Code >= 200 && Code <= 299; }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the response.
        /// </summary>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            string[] lines = Message.Split(new string[]{"\r\n"}, System.StringSplitOptions.None);

            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l];

                if (l == lines.Length - 1)
                {
                    result.AppendLine(Code + " " + line);
                }
                else
                {
                    result.AppendLine(Code + "-" + line);
                }
            }

            return result.ToString();
        }

        public bool Equals(SmtpResponse other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Code == Code && Equals(other.Message, Message);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (SmtpResponse)) return false;
            return Equals((SmtpResponse) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Code*397) ^ (Message != null ? Message.GetHashCode() : 0);
            }
        }
    }
}