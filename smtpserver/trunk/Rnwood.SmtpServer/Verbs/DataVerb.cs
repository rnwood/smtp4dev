#region

using System.IO;
using System.Text;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class DataVerb : IVerb
    {
        public virtual void Process(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage == null)
            {
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "Bad sequence of commands"));
                return;
            }

            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.StartMailInputEndWithDot,
                                                               "End message with period"));
            using (MemoryStream dataStream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(dataStream, Encoding.Default))
                {
                    do
                    {
                        string line = connection.ReadLine();

                        if (line != ".")
                        {
                            line = ProcessLine(line);
                            writer.WriteLine(line);
                        }
                        else
                        {
                            break;
                        }
                    } while (true);

                    writer.Flush();
                    long? maxMessageSize =
                        connection.Server.Behaviour.GetMaximumMessageSize(connection);

                    if (maxMessageSize.HasValue && dataStream.Length > maxMessageSize.Value)
                    {
                        connection.WriteResponse(
                            new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation,
                                             "Message exceeds fixed size limit"));
                    }
                    else
                    {
                        connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Mail accepted"));
                        connection.CurrentMessage.Data = dataStream.ToArray();
                        connection.CommitMessage();
                    }
                }
            }
        }

        protected virtual string ProcessLine(string line)
        {
            //Remove escaping of end of message character
            if (line.StartsWith("."))
            {
                line = line.Remove(0, 1);
            }
            return line;
        }
    }
}