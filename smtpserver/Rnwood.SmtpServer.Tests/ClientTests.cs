// <copyright file="ClientTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using Xunit;
using Xunit.Abstractions;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="ClientTests" />
/// </summary>
public partial class ClientTests
{
    /// <summary>
    ///     Defines the output
    /// </summary>
    private readonly ITestOutputHelper output;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ClientTests" /> class.
    /// </summary>
    /// <param name="output">The output<see cref="ITestOutputHelper" /></param>
    public ClientTests(ITestOutputHelper output) => this.output = output;

    /// <summary>
    ///     The MailKit_NonSSL
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task MailKit_SmtpUtf8()
    {
        using (SmtpServer server = new SmtpServer(new Rnwood.SmtpServer.ServerOptions(false, false, "test", (int)StandardSmtpPort.AssignAutomatically, false, [], [], null, null)))
        {
            ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

            server.MessageReceivedEventHandler += (o, ea) =>
            {
                messages.Add(ea.Message);
                return Task.CompletedTask;
            };
            server.Start();

            await SendMessage_MailKit_Async(server, "ظػؿقط@to.com", "ظػؿقط@from.com").WithTimeout("sending message")
                ;

            Assert.Single(messages);
            Assert.Equal("ظػؿقط@from.com", messages.First().From);

            Assert.Equal("ظػؿقط@to.com", messages.First().Recipients.Single());
        }
    }

    /// <summary>
    ///     The MailKit_NonSSL
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task MailKit_NonSSL()
    {
        using (SmtpServer server = new SmtpServer(new Rnwood.SmtpServer.ServerOptions(false, false, "test",(int)StandardSmtpPort.AssignAutomatically, false, [], [], null, null)))
        {
            ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

            server.MessageReceivedEventHandler += (o, ea) =>
            {
                messages.Add(ea.Message);
                return Task.CompletedTask;
            };
            server.Start();

            await SendMessage_MailKit_Async(server, "to@to.com").WithTimeout("sending message")
                ;

            Assert.Single(messages);
            Assert.Equal("from@from.com", messages.First().From);
        }
    }

    /// <summary>
    ///     Tests mailkit connecting using STARTTLS
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task MailKit_StartTLS()
    {
        using (SmtpServer server = new SmtpServer(new Rnwood.SmtpServer.ServerOptions( false,false, Dns.GetHostName(),
                   (int)StandardSmtpPort.AssignAutomatically, false, [], [],
                   null, CreateSelfSignedCertificate())))
        {
            ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

            server.MessageReceivedEventHandler += (o, ea) =>
            {
                messages.Add(ea.Message);
                return Task.CompletedTask;
            };
            server.Start();

            await SendMessage_MailKit_Async(server, "to@to.com", secureSocketOptions: SecureSocketOptions.StartTls)
                .WithTimeout("sending message");

            Assert.Single(messages);
            Assert.Equal("from@from.com", messages.First().From);
        }
    }

    /// <summary>
    ///     Tests mailkit connecting using implicit TLS
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task MailKit_ImplicitTLS()
    {
        using (SmtpServer server = new SmtpServer(new Rnwood.SmtpServer.ServerOptions( false,false, Dns.GetHostName(),
                   (int)StandardSmtpPort.AssignAutomatically, false, [], [],
                   CreateSelfSignedCertificate(), null)))
        {
            ConcurrentBag<IMessage> messages = new ConcurrentBag<IMessage>();

            server.MessageReceivedEventHandler += (o, ea) =>
            {
                messages.Add(ea.Message);
                return Task.CompletedTask;
            };
            server.Start();

            await SendMessage_MailKit_Async(server, "to@to.com",
                    secureSocketOptions: SecureSocketOptions.SslOnConnect).WithTimeout("sending message")
                ;

            Assert.Single(messages);
            Assert.Equal("from@from.com", messages.First().From);
        }
    }

    /// <summary>
    ///     The MailKit_NonSSL_StressTest
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task MailKit_NonSSL_StressTest()
    {
        using (SmtpServer server = new SmtpServer(new Rnwood.SmtpServer.ServerOptions( false, false, "test", (int) StandardSmtpPort.AssignAutomatically, false, [], [], null, null)))
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
                    using (SmtpClient client = new SmtpClient())
                    {
                        await client.ConnectAsync("localhost", server.ListeningEndpoints.First().Port);

                        for (int i = 0; i < numberOfMessagesPerThread; i++)
                        {
                            MimeMessage message = NewMessage(i + "@" + localThreadId, "from@from.com");

                            await client.SendAsync(message);
                        }

                        await client.DisconnectAsync(true);
                    }
                }));
            }

            await Task.WhenAll(sendingTasks).WithTimeout(120, "sending messages");
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
    /// </summary>
    /// <param name="toAddress">The toAddress<see cref="string" /></param>
    /// <returns>The <see cref="MimeMessage" /></returns>
    private static MimeMessage NewMessage(string toAddress, string fromAddress)
    {
        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress("", fromAddress));
        message.To.Add(new MailboxAddress("", toAddress));
        message.Subject = "subject";
        message.Body = new TextPart("plain") { Text = "body" };
        return message;
    }

    /// <summary>
    /// </summary>
    /// <param name="server">The server<see cref="SmtpServer" /></param>
    /// <param name="toAddress">The toAddress<see cref="string" /></param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    private async Task SendMessage_MailKit_Async(SmtpServer server, string toAddress,
        string fromAddress = "from@from.com", SecureSocketOptions secureSocketOptions = SecureSocketOptions.None)
    {
        MimeMessage message = NewMessage(toAddress, fromAddress);

        using (SmtpClient client = new SmtpClient(new SmtpClientLogger(output)))
        {
            client.CheckCertificateRevocation = false;
            client.ServerCertificateValidationCallback = (mysender, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };
            client.SslProtocols = SslProtocols.Tls12;
            await client.ConnectAsync("localhost", server.ListeningEndpoints.First().Port, secureSocketOptions);
            await client.SendAsync(new FormatOptions { International = true }, message);
            await client.DisconnectAsync(true);
        }
    }

    private X509Certificate2 CreateSelfSignedCertificate()
    {
        CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
        SecureRandom random = new SecureRandom(randomGenerator);

        X509V3CertificateGenerator certGenerator = new X509V3CertificateGenerator();
        certGenerator.SetSubjectDN(new X509Name("CN=localhost"));
        certGenerator.SetIssuerDN(new X509Name("CN=localhost"));

        BigInteger serialNumber =
            BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
        certGenerator.SetSerialNumber(serialNumber);

        certGenerator.SetNotBefore(DateTime.UtcNow.Date);
        certGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(10));

        KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, 2048);

        RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(keyGenerationParameters);
        AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();
        certGenerator.SetPublicKey(keyPair.Public);

        Asn1SignatureFactory signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private, random);
        X509Certificate cert = certGenerator.Generate(signatureFactory);

        Pkcs12Store store = new Pkcs12StoreBuilder().Build();
        X509CertificateEntry certificateEntry = new X509CertificateEntry(cert);
        store.SetCertificateEntry("cert", certificateEntry);
        store.SetKeyEntry("cert", new AsymmetricKeyEntry(keyPair.Private), new[] { certificateEntry });
        MemoryStream stream = new MemoryStream();
        store.Save(stream, "".ToCharArray(), random);

        return new X509Certificate2(
            stream.ToArray(), "",
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
    }
}
