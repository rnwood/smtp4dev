using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public class E2ETests_ImapAppendProtocol : E2ETests
    {
        public E2ETests_ImapAppendProtocol(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void AppendToSentFolder_ProtocolLevel_ShouldWork()
        {
            RunE2ETest(context => 
            {
                // Test APPEND at the protocol level to verify our implementation works
                using (var client = new TcpClient("localhost", context.ImapPortNumber))
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Read greeting
                    var greeting = reader.ReadLine();
                    Assert.StartsWith("* OK", greeting);
                    
                    // Login
                    writer.WriteLine("A001 LOGIN user password");
                    var loginResponse = reader.ReadLine();
                    Assert.Equal("A001 OK LOGIN completed.", loginResponse);
                    
                    // Prepare test message
                    var testMessage = $"From: sender@example.com\r\nTo: recipient@example.com\r\nSubject: Test APPEND to Sent {Guid.NewGuid()}\r\n\r\nThis is a test message.\r\n";
                    
                    // Send APPEND command to Sent folder
                    writer.WriteLine($"A002 APPEND Sent (\\Seen) {{{testMessage.Length}}}");
                    
                    // Should get continuation response
                    var continuation = reader.ReadLine();
                    Assert.Equal("+ Ready for literal data.", continuation);
                    
                    // Send message data
                    writer.Write(testMessage);
                    writer.WriteLine(); // CRLF to terminate
                    
                    // Should get success response
                    var appendResponse = reader.ReadLine();
                    Assert.StartsWith("A002 OK APPEND completed", appendResponse);
                    Assert.Contains("APPENDUID", appendResponse);
                    
                    // Logout
                    writer.WriteLine("A003 LOGOUT");
                    var logoutResponse = reader.ReadLine();
                    Assert.StartsWith("* BYE", logoutResponse);
                }
            });
        }

        [Fact]
        public void AppendToInboxFolder_ProtocolLevel_ShouldWork()
        {
            RunE2ETest(context => 
            {
                // Test APPEND to INBOX at the protocol level
                using (var client = new TcpClient("localhost", context.ImapPortNumber))
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Read greeting
                    var greeting = reader.ReadLine();
                    Assert.StartsWith("* OK", greeting);
                    
                    // Login
                    writer.WriteLine("A001 LOGIN user password");
                    var loginResponse = reader.ReadLine();
                    Assert.Equal("A001 OK LOGIN completed.", loginResponse);
                    
                    // Prepare test message
                    var testMessage = $"From: sender@example.com\r\nTo: recipient@example.com\r\nSubject: Test APPEND to INBOX {Guid.NewGuid()}\r\n\r\nThis is a test message.\r\n";
                    
                    // Send APPEND command to INBOX folder
                    writer.WriteLine($"A002 APPEND INBOX {{{testMessage.Length}}}");
                    
                    // Should get continuation response
                    var continuation = reader.ReadLine();
                    Assert.Equal("+ Ready for literal data.", continuation);
                    
                    // Send message data
                    writer.Write(testMessage);
                    writer.WriteLine(); // CRLF to terminate
                    
                    // Should get success response
                    var appendResponse = reader.ReadLine();
                    Assert.StartsWith("A002 OK APPEND completed", appendResponse);
                    Assert.Contains("APPENDUID", appendResponse);
                    
                    // Logout
                    writer.WriteLine("A003 LOGOUT");
                    var logoutResponse = reader.ReadLine();
                    Assert.StartsWith("* BYE", logoutResponse);
                }
            });
        }

        [Fact]
        public void AppendToUnsupportedFolder_ProtocolLevel_ShouldRejectCorrectly()
        {
            RunE2ETest(context => 
            {
                // Test APPEND to unsupported folder
                using (var client = new TcpClient("localhost", context.ImapPortNumber))
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Read greeting
                    var greeting = reader.ReadLine();
                    Assert.StartsWith("* OK", greeting);
                    
                    // Login
                    writer.WriteLine("A001 LOGIN user password");
                    var loginResponse = reader.ReadLine();
                    Assert.Equal("A001 OK LOGIN completed.", loginResponse);
                    
                    // Prepare test message
                    var testMessage = $"From: sender@example.com\r\nTo: recipient@example.com\r\nSubject: Test APPEND to Drafts {Guid.NewGuid()}\r\n\r\nThis is a test message.\r\n";
                    
                    // Send APPEND command to unsupported folder
                    writer.WriteLine($"A002 APPEND Drafts {{{testMessage.Length}}}");
                    
                    // Should get rejection response, not continuation
                    var response = reader.ReadLine();
                    Assert.StartsWith("A002 NO", response);
                    Assert.Contains("not supported", response);
                    
                    // Logout
                    writer.WriteLine("A003 LOGOUT");
                    var logoutResponse = reader.ReadLine();
                    Assert.StartsWith("* BYE", logoutResponse);
                }
            });
        }
    }
}