using MailKit.Net.Pop3;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.Pop3
{
    [Collection("E2E")]
    public class E2ETests_Pop3_RetrieveAnd_Delete : E2ETests
    {
        private readonly ITestOutputHelper output;

        public E2ETests_Pop3_RetrieveAnd_Delete(ITestOutputHelper output) : base(output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("None")]
        [InlineData("StartTls")]
        [InlineData("ImplicitTls")]
        public void RetrieveThenDeleteMessage(string pop3Mode)
        {
            var previousArgs = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_ARGS");
            try
            {
                // Tell the server to use the specific POP3 TLS mode for this run
                // Append to existing args if present (e.g., Docker args), otherwise set new value
                var existingArgs = previousArgs ?? "";
                var newArgs = string.IsNullOrEmpty(existingArgs) 
                    ? $"--pop3tlsmode={pop3Mode}"
                    : $"{existingArgs}\n--pop3tlsmode={pop3Mode}";
                Environment.SetEnvironmentVariable("SMTP4DEV_E2E_ARGS", newArgs);

                RunE2ETest(context =>
                {
                    // Send a test message via SMTP
                    string subject = $"POP3 E2E {pop3Mode} " + Guid.NewGuid().ToString();

                    var msg = new MimeMessage();
                    msg.From.Add(new MailboxAddress("sender", "from@from.com"));
                    msg.To.Add(new MailboxAddress("recipient", "to@to.com"));
                    msg.Subject = subject;
                    msg.Body = new TextPart("plain") { Text = "POP3 E2E Body" };

                    using (var smtp = CreateSmtpClientWithLogging())
                    {
                        smtp.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                        smtp.CheckCertificateRevocation = false;
                        smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                        smtp.Connect("localhost", context.SmtpPortNumber, SecureSocketOptions.StartTls, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                        smtp.Send(msg);
                        smtp.Disconnect(true);
                    }

                    // Map the string mode to MailKit SecureSocketOptions for POP3 connect
                    SecureSocketOptions popConnectOption = pop3Mode switch
                    {
                        "None" => SecureSocketOptions.None,
                        "StartTls" => SecureSocketOptions.StartTls,
                        "ImplicitTls" => SecureSocketOptions.SslOnConnect,
                        _ => SecureSocketOptions.None
                    };

                    // Helper to try connecting to multiple candidate hosts to avoid IPv4/IPv6 issues on test environments
                    void ConnectWithFallback(MailKit.Net.Pop3.Pop3Client client, string host, int port, MailKit.Security.SecureSocketOptions options)
                    {
                        var candidates = new[] { host, "127.0.0.1", "::1" };
                        Exception lastEx = null;
                        foreach (var candidate in candidates)
                        {
                            try
                            {
                                output.WriteLine($"Attempting POP3 connect to {candidate}:{port} using {options}");
                                client.Connect(candidate, port, options, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                                output.WriteLine($"Connected to POP3 {candidate}:{port}");
                                return;
                            }
                            catch (Exception ex)
                            {
                                lastEx = ex;
                                output.WriteLine($"POP3 connect to {candidate}:{port} failed: {ex.Message}");
                            }
                        }

                        throw new AggregateException("All POP3 connect attempts failed", lastEx ?? new Exception("Unknown"));
                    }

                    // Connect to POP3 and retrieve the message
                    using (var pop = CreatePop3ClientWithLogging())
                    {
                        pop.CheckCertificateRevocation = false;
                        pop.ServerCertificateValidationCallback = (s, c, h, e) => true;

                        ConnectWithFallback(pop, context.Pop3Host, context.Pop3PortNumber, popConnectOption);
                        // Authenticate - server currently accepts any creds when AuthenticationRequired is false
                        pop.Authenticate("user", "password");

                        Assert.True(pop.Count >= 1, "POP3 should have at least one message");

                        var retrieved = pop.GetMessage(0);
                        Assert.Equal(subject, retrieved.Subject);

                        // Mark for deletion and commit (QUIT)
                        pop.DeleteMessage(0);
                        pop.Disconnect(true);
                    }

                    // Reconnect to POP3 to ensure message was removed
                    using (var pop2 = new Pop3Client())
                    {
                        pop2.CheckCertificateRevocation = false;
                        pop2.ServerCertificateValidationCallback = (s, c, h, e) => true;

                        ConnectWithFallback(pop2, context.Pop3Host, context.Pop3PortNumber, popConnectOption);
                        pop2.Authenticate("user", "password");

                        // There should be zero messages after deletion
                        Assert.True(pop2.Count == 0, "Message should have been deleted by previous session");

                        pop2.Disconnect(true);
                    }
                });
            }
            finally
            {
                // Restore previous environment variable state (may be null)
                Environment.SetEnvironmentVariable("SMTP4DEV_E2E_ARGS", previousArgs);
            }
        }
    }
}
