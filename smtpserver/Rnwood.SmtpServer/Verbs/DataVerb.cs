#region

using Rnwood.SmtpServer.Verbs;
using System;
using System.IO;
using System.Threading.Tasks;

#endregion

namespace Rnwood.SmtpServer
{
    public class DataVerb : IVerb
    {
        public async virtual Task ProcessAsync(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage == null)
            {
                await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "Bad sequence of commands"));
                return;
            }

            connection.CurrentMessage.SecureConnection = connection.Session.SecureConnection;
            await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.StartMailInputEndWithDot,
                                                               "End message with period"));

            using (StreamWriter writer = new StreamWriter(connection.CurrentMessage.WriteData(), connection.ReaderEncoding))
            {
                bool firstLine = true;

                do
                {
                    string line = await connection.ReadLineAsync();

                    if (line != ".")
                    {
                        line = ProcessLine(line);

                        if (!firstLine)
                            writer.Write(Environment.NewLine);

                        writer.Write(line);
                    }
                    else
                    {
                        break;
                    }

                    firstLine = false;
                } while (true);

                writer.Flush();
                long? maxMessageSize =
                    connection.Server.Behaviour.GetMaximumMessageSize(connection);

                if (maxMessageSize.HasValue && writer.BaseStream.Length > maxMessageSize.Value)
                {
                    await connection.WriteResponseAsync(
                        new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation,
                                         "Message exceeds fixed size limit"));
                }
                else
                {
                    writer.Dispose();
                    connection.Server.Behaviour.OnMessageCompleted(connection);
                    await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.OK, "Mail accepted"));
                    connection.CommitMessage();
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