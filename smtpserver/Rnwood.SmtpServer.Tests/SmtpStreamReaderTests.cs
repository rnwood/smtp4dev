using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

public class SmtpStreamReaderTests
{
    [Fact]
    public async Task ReadLine_WithFallbackChars() =>
        await Test("MAIL FROM <robért@rnwood.co.uk>\r\n", Encoding.GetEncoding("iso-8859-1"),
            new[] { "MAIL FROM <robért@rnwood.co.uk>" });

    [Fact]
    public async Task ReadLine_WithAnsiChars() =>
        await Test("MAIL FROM <quáestionem@rnwood.co.uk>\r\n", Encoding.GetEncoding("iso-8859-1"),
            new[] { "MAIL FROM <quáestionem@rnwood.co.uk>" });

    [Fact]
    public async Task ReadLine_WithUtf8Chars() =>
        await Test("MAIL FROM <ظػؿقط <rob@rnwood.co.uk>>\r\n", Encoding.UTF8,
            new[] { "MAIL FROM <ظػؿقط <rob@rnwood.co.uk>>" });


    [Fact]
    public async Task ReadLine_MutipleLinesInBuffer() =>
        await Test("aaa\r\nbbb\r\nccc\r\n", Encoding.UTF8, new[] { "aaa", "bbb", "ccc" });

    private async Task Test(string data, Encoding encoding, string[] expectedLines)
    {
        byte[] dataBytes = encoding.GetBytes(data);
        using (Stream stream = new MemoryStream(dataBytes))
        {
            using (SmtpStreamReader ssr = new SmtpStreamReader(stream, Encoding.GetEncoding("iso-8859-1"), false))
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    List<string> receivedLines = new List<string>();

                    string receivedLine;

                    while ((receivedLine = await ssr.ReadLineAsync(cts.Token)) != null)
                    {
                        receivedLines.Add(receivedLine);
                    }

                    byte[] receivedBytes = encoding.GetBytes(string.Join("\r\n", receivedLines));
                    byte[] expectedBytes = encoding.GetBytes(string.Join("\r\n", expectedLines));
                    Assert.Equal(expectedBytes, receivedBytes);
                }
            }
        }
    }
}
