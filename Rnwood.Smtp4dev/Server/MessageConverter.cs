using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Server
{
    public class MessageConverter
    {
        public async Task<Message> ConvertAsync(Stream messageData, string envelopeFrom, string envelopeTo)
        {
            var subject = "";
            string mimeParseError = null;

            var data = new byte[messageData.Length];
            await messageData.ReadAsync(data, 0, data.Length);


            var foundHeaders = false;
            var foundSeparator = false;
            using (var dataReader = new StreamReader(new MemoryStream(data)))
            {
                while (!dataReader.EndOfStream)
                    if (dataReader.ReadLine().Length != 0)
                    {
                        foundHeaders = true;
                    }
                    else
                    {
                        foundSeparator = true;
                        break;
                    }
            }

            if (!foundHeaders || !foundSeparator)
            {
                mimeParseError = "Malformed MIME message. No headers found";
            }
            else
            {
                messageData.Seek(0, SeekOrigin.Begin);
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(10));
                    var mime = await MimeMessage.LoadAsync(messageData, true, cts.Token).ConfigureAwait(false);
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


            var message = new Message
            {
                Id = Guid.NewGuid(),

                From = envelopeFrom,
                To = envelopeTo,
                ReceivedDate = DateTime.Now,
                Subject = subject,
                Data = data,
                MimeParseError = mimeParseError
            };

            return message;
        }
    }
}