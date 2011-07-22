#region

using System.Text;

#endregion

namespace Rnwood.SmtpServer
{
    public class SmtpResponse
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
    }

    public enum StandardSmtpResponseCode
    {
        //Errors
        SyntaxErrorCommandUnrecognised = 500,
        SyntaxErrorInCommandArguments = 501,
        CommandNotImplemented = 502,
        BadSequenceOfCommands = 503,
        CommandParameterNotImplemented = 504,
        ExceededStorageAllocation = 552,
        AuthenticationFailure = 535,
        AuthenticationRequired = 530,

        SystemStatusOrHelpReply = 211,
        HelpMessage = 214,
        ServiceReady = 220,
        ClosingTransmissionChannel = 221,
        OK = 250,
        UserNotLocalWillForwardTo = 251,
        StartMailInputEndWithDot = 354,
        AuthenticationContinue = 334,
        AuthenitcationOK = 235
    }
}