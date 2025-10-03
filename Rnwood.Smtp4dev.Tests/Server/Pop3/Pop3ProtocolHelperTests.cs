using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.Server;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Server.Pop3
{
    public class Pop3ProtocolHelperTests
    {
        private static async Task RunWithTimeout(Func<Task> inner, TimeSpan timeout)
        {
            var task = inner();
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                throw new TimeoutException($"Test did not complete within {timeout}");
            }
            await task;
        }

        [Fact]
        public async Task WriteDotStuffedMessageAsync_DotLinesAndBinary_PreservesBytesAndDotStuffs()
        {
            await RunWithTimeout(async () =>
            {
                // Arrange - create a message containing a line that starts with '.' and binary 0x00
                var original = Encoding.ASCII.GetBytes("first line\r\n." + "dotline\r\n" + "binary:\0\r\n");

                using var ms = new MemoryStream();

                // Act
                await Pop3ProtocolHelper.WriteDotStuffedMessageAsync(ms, original);
                ms.Position = 0;
                var resultBytes = ms.ToArray();

                // The result should contain the dot-stuffed form: lines starting with '.' must be prefixed by another '.'
                var resultStr = Encoding.ASCII.GetString(resultBytes);
                Assert.Contains("..dotline\r\n", resultStr);

                // Now un-stuff locally and compare to original content up to final CRLF (tests that unstuffing recovers original bytes)
                using var input = new MemoryStream(resultBytes);
                using var reader = new StreamReader(input, Encoding.ASCII);
                using var outMs = new MemoryStream();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == ".") break; // end marker
                    // Undo dot-stuffing
                    if (line.StartsWith("..")) line = line.Substring(1);
                    var bytes = Encoding.ASCII.GetBytes(line + "\r\n");
                    outMs.Write(bytes, 0, bytes.Length);
                }

                var recovered = outMs.ToArray();
                Assert.Equal(original, recovered);
            }, TimeSpan.FromSeconds(5));
        }
    }
}
