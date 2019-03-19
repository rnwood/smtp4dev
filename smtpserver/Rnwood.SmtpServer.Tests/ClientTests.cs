// <copyright file="ClientTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using System.Text;
    using System.Threading.Tasks;
    using MailKit.Net.Smtp;
    using MimeKit;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Defines the <see cref="ClientTests" />
    /// </summary>
    public partial class ClientTests
    {
        /// <summary>
        /// Defines the output
        /// </summary>
        private ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientTests"/> class.
        /// </summary>
        /// <param name="output">The output<see cref="ITestOutputHelper"/></param>
        public ClientTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// The MailKit_NonSSL
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task MailKit_SmtpUtf8()
        {
            using (DefaultServer server = new DefaultServer(false, StandardSmtpPort.AssignAutomatically))
            {
                ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

                server.MessageReceivedEventHandler += (o, ea) =>
                {
                    messages.Add(ea.Message);
                    return Task.CompletedTask;
                };
                server.Start();

                await this.SendMessage_MailKit_Async(server, "ظػؿقط@to.com", "ظػؿقط@from.com").WithTimeout("sending message").ConfigureAwait(false);

                Assert.Single(messages);
                Assert.Equal("ظػؿقط@from.com", messages.First().From);
            }
        }

        /// <summary>
        /// The MailKit_NonSSL
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task MailKit_NonSSL()
        {
            using (DefaultServer server = new DefaultServer(false, StandardSmtpPort.AssignAutomatically))
            {
                ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

                server.MessageReceivedEventHandler += (o, ea) =>
                {
                    messages.Add(ea.Message);
                    return Task.CompletedTask;
                };
                server.Start();

                await this.SendMessage_MailKit_Async(server, "to@to.com").WithTimeout("sending message").ConfigureAwait(false);

                Assert.Single(messages);
                Assert.Equal("from@from.com", messages.First().From);
            }
        }

        [Fact]
        public async Task SmtpClient_UTF8Address()
        {
            using (DefaultServer server = new DefaultServer(false, StandardSmtpPort.AssignAutomatically))
            {
                try
                {
                    ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

                    server.MessageReceivedEventHandler += (o, ea) =>
                    {
                        messages.Add(ea.Message);
                        return Task.CompletedTask;
                    };
                    server.SessionCompletedEventHandler += async (o, ea) =>
                    {
                        using (TextReader logReader = await ea.Session.GetLog())
                        {
                            output.WriteLine(logReader.ReadToEnd());
                        }
                    };
                    server.Start();

                    await SendMessage_SmtpClient_Async(server, "квіточка@пошта.укр", "квіточка@пошта.укр").WithTimeout("sending message");

                    server.Stop();

                    Assert.Single(messages);
                    Assert.Equal("квіточка@пошта.укр", messages.First().Recipients.SingleOrDefault());

                }
                finally
                {
                    server.Stop(true);
                }

            }

            
        }

        [Fact]
        public async Task MailKit_UTF8Address()
        {
            using (DefaultServer server = new DefaultServer(false, StandardSmtpPort.AssignAutomatically))
            {
                ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

                server.MessageReceivedEventHandler += async (o, ea) =>
                {
                    messages.Add(ea.Message);
                };
                server.Start();

                await SendMessage_MailKit_Async(server, "квіточка@пошта.укр").WithTimeout("sending message");

                Assert.Single(messages);
                Assert.Equal("квіточка@пошта.укр", messages.First().Recipients.SingleOrDefault());
            }
        }

        /// <summary>
        /// The MailKit_NonSSL_StressTest
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task MailKit_NonSSL_StressTest()
        {
            using (DefaultServer server = new DefaultServer(false, StandardSmtpPort.AssignAutomatically))
            {
                ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

                server.MessageReceivedEventHandler += (o, ea) =>
                {
                    messages.Add(ea.Message);
                    return Task.CompletedTask;
                };
                server.Start();

                List<Task> sendingTasks = new List<Task>();

                int numberOfThreads = 10;
                int numberOfMessagesPerThread = 50;

                for (int threadId = 0; threadId < numberOfThreads; threadId++)
                {
                    int localThreadId = threadId;

                    sendingTasks.Add(Task.Run(async () =>
                    {
                        using (MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient())
                        {
                            await client.ConnectAsync("localhost", server.PortNumber).ConfigureAwait(false);

                            for (int i = 0; i < numberOfMessagesPerThread; i++)
                            {
                                MimeMessage message = NewMessage(i + "@" + localThreadId, "from@from.com");

                                await client.SendAsync(message).ConfigureAwait(false);
                                ;
                            }

                            await client.DisconnectAsync(true).ConfigureAwait(false);
                        }
                    }));
                }

                await Task.WhenAll(sendingTasks).WithTimeout(120, "sending messages").ConfigureAwait(false);
                Assert.Equal(numberOfMessagesPerThread * numberOfThreads, messages.Count);

                for (int threadId = 0; threadId < numberOfThreads; threadId++)
                {
                    for (int i = 0; i < numberOfMessagesPerThread; i++)
                    {
                        Assert.Contains(messages, m => m.Recipients.Any(t => t == i + "@" + threadId));
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="toAddress">The toAddress<see cref="string"/></param>
        /// <returns>The <see cref="MimeMessage"/></returns>
        private static MimeMessage NewMessage(string toAddress, string fromAddress)
        {
            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("", fromAddress));
            message.To.Add(new MailboxAddress("", toAddress));
            message.Subject = "subject";
            message.Body = new TextPart("plain")
            {
                Text = "body"
            };
            return message;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="server">The server<see cref="DefaultServer"/></param>
        /// <param name="toAddress">The toAddress<see cref="string"/></param>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        private async Task SendMessage_MailKit_Async(DefaultServer server, string toAddress, string fromAddress = "from@from.com")
        {
            MimeMessage message = NewMessage(toAddress, fromAddress);

            using (MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient(new SmtpClientLogger(this.output)))
            {
                await client.ConnectAsync("localhost", server.PortNumber).ConfigureAwait(false);
                await client.SendAsync(new FormatOptions { International = true }, message).ConfigureAwait(false);
                await client.DisconnectAsync(true).ConfigureAwait(false);
            }
        }

        private async Task SendMessage_SmtpClient_Async(DefaultServer server, string toAddress, string fromAddress)
        {
            MailMessage message = new System.Net.Mail.MailMessage();
            message.Subject = "Test";
            message.To.Add(new MailAddress(toAddress, toAddress, Encoding.UTF8));
            message.From = new MailAddress(fromAddress, fromAddress, Encoding.UTF8);
            message.Body = "Blah blah blah";

            using (System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("localhost", server.PortNumber))
            {
                client.DeliveryFormat = SmtpDeliveryFormat.International;
                await client.SendMailAsync(message);
            }

        }
    }
}
