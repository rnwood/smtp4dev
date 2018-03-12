using MimeKit;
using Rnwood.Smtp4dev.DbModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    public class MessageConverter
    {
        public async Task<Message> ConvertAsync(Stream messageData, string from, string to)
        {
            string subject = "";
            string mimeParseError = null;

            byte[] data = new byte[messageData.Length];
            await messageData.ReadAsync(data, 0, data.Length);


            bool foundHeaders = false;
            bool foundSeparator = false;
            using (StreamReader dataReader = new StreamReader(new MemoryStream(data)))
            {
                while (!dataReader.EndOfStream)
                {
                    if (dataReader.ReadLine().Length != 0)
                    {
                        foundHeaders = true;
                    } else
                    {
                        foundSeparator = true;
                        break;
                    }
                }   
            }

            if (!foundHeaders || !foundSeparator)
            {
                mimeParseError = "Malformed MIME message. No headers found";
            } else {

                messageData.Seek(0, SeekOrigin.Begin);
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(10));
                    MimeMessage mime = await MimeMessage.LoadAsync(messageData, true, cts.Token).ConfigureAwait(false);
                    subject = mime.Subject;
                }
                catch (OperationCanceledException e)
                {
                    mimeParseError = e.Message;
                }
                catch (FormatException e)
                {
                    mimeParseError = e.Message;
                }
            }


            Message message = new Message()
            {
                Id = Guid.NewGuid(),

                From = from,
                To = to,
                ReceivedDate = DateTime.Now,
                Subject = subject,
                Data = data,
                MimeParseError = mimeParseError
            };

            return message;
        }
    }
}
