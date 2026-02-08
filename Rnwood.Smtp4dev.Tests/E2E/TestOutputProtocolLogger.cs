using System;
using System.Text;
using MailKit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    /// <summary>
    /// Protocol logger that writes MailKit SMTP/IMAP client protocol logs to xUnit test output.
    /// This helps diagnose connection and protocol issues in E2E tests.
    /// </summary>
    public class TestOutputProtocolLogger : IProtocolLogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _prefix;
        private readonly object _lock = new object();

        public TestOutputProtocolLogger(ITestOutputHelper output, string prefix = "")
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _prefix = prefix;
        }

        public IAuthenticationSecretDetector AuthenticationSecretDetector { get; set; }

        public void LogConnect(Uri uri)
        {
            lock (_lock)
            {
                _output.WriteLine($"  [{_prefix}] CONNECT: {uri}");
            }
        }

        public void LogClient(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                var text = Encoding.UTF8.GetString(buffer, offset, count).TrimEnd('\r', '\n');
                if (!string.IsNullOrWhiteSpace(text))
                {
                    foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _output.WriteLine($"  [{_prefix}] C: {line}");
                    }
                }
            }
        }

        public void LogServer(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                var text = Encoding.UTF8.GetString(buffer, offset, count).TrimEnd('\r', '\n');
                if (!string.IsNullOrWhiteSpace(text))
                {
                    foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _output.WriteLine($"  [{_prefix}] S: {line}");
                    }
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
