using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Rnwood.Smtp4dev.Server.Pop3;
using Rnwood.Smtp4dev.Server.Pop3.CommandHandlers;
using Rnwood.Smtp4dev.Tests.TestHelpers;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Data;
using System.Linq;
using Xunit;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Tests.Server.Pop3
{
    public class CommandHandlersTests
    {
        private (Pop3SessionContext ctx, MemoryStream ms) CreateContext(TestMessagesRepository repo = null, ServerOptions options = null)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            var reader = new StreamReader(ms, Encoding.ASCII);
            repo ??= new TestMessagesRepository();
            options ??= new ServerOptions();
            var loggerFactory = LoggerFactory.Create(b => { });

            var ctx = new Pop3SessionContext
            {
                Stream = ms,
                Writer = writer,
                Reader = reader,
                MessagesRepository = repo,
                Logger = loggerFactory.CreateLogger<Pop3SessionContext>(),
                Options = options,
                CancellationToken = CancellationToken.None
            };

            return (ctx, ms);
        }

        private string ReadOutput(MemoryStream ms)
        {
            // Return output currently in stream as ASCII string
            var bytes = ms.ToArray();
            return Encoding.ASCII.GetString(bytes);
        }

        [Fact]
        public async Task UserCommand_Accepts_Username()
        {
            var (ctx, ms) = CreateContext();
            var handler = new UserCommand();

            await handler.ExecuteAsync(ctx, "alice", CancellationToken.None);

            var outStr = ReadOutput(ms);
            Assert.StartsWith("+OK", outStr);
            Assert.Equal("alice", ctx.Username);
        }

        [Fact]
        public async Task PassCommand_Requires_User_And_Password_Then_Authenticates()
        {
            var (ctx, ms) = CreateContext();
            var handler = new PassCommand();

            // Without username
            await handler.ExecuteAsync(ctx, "pass", CancellationToken.None);
            Assert.StartsWith("-ERR", ReadOutput(ms));

            // Clear stream
            ms.SetLength(0);

            ctx.Username = "bob";
            await handler.ExecuteAsync(ctx, "secret", CancellationToken.None);
            var outStr = ReadOutput(ms);
            Assert.Contains("+OK", outStr);
            Assert.True(ctx.Authenticated);
        }

        [Fact]
        public async Task StatCommand_Requires_Auth_And_Shows_Count_And_Size()
        {
            var repo = new TestMessagesRepository();
            var msg = new Message { Id = Guid.NewGuid(), Data = Encoding.ASCII.GetBytes("Hello\r\n") };
            await repo.AddMessage(msg);

            var (ctx, ms) = CreateContext(repo);
            var handler = new StatCommand();

            // Not authenticated
            await handler.ExecuteAsync(ctx, null, CancellationToken.None);
            Assert.StartsWith("-ERR", ReadOutput(ms));

            ms.SetLength(0);
            ctx.Authenticated = true;
            await handler.ExecuteAsync(ctx, null, CancellationToken.None);
            var outStr = ReadOutput(ms);
            Assert.StartsWith("+OK", outStr);
            Assert.Contains("1", outStr);
        }

        [Fact]
        public async Task Retr_Writes_BinarySafe_And_DotStuffed()
        {
            var repo = new TestMessagesRepository();
            var data = Encoding.ASCII.GetBytes("Hello\r\n." + "startsDot\r\nBinary\0\r\n");
            var message = new Message { Id = Guid.NewGuid(), Data = data };
            await repo.AddMessage(message);

            var (ctx, ms) = CreateContext(repo);
            ctx.Authenticated = true;
            var handler = new RetrCommand();

            await handler.ExecuteAsync(ctx, "1", CancellationToken.None);
            await ctx.Writer.FlushAsync();

            var all = ms.ToArray();
            // Find first CRLF (end of +OK header)
            var headerEnd = Array.IndexOf(all, (byte)0x0A); // LF index (we will adjust)
            // Better search for header CRLF (\r\n)
            int headerCr = -1;
            for (int i = 0; i + 1 < all.Length; i++)
            {
                if (all[i] == 0x0D && all[i + 1] == 0x0A) { headerCr = i; break; }
            }
            Assert.True(headerCr >= 0, "Header CRLF not found");
            int bodyStart = headerCr + 2;

            // Find terminator sequence \r\n.\r\n
            byte[] term = new byte[] { 0x0D, 0x0A, 0x2E, 0x0D, 0x0A };
            int termIndex = -1;
            for (int i = bodyStart; i + term.Length <= all.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < term.Length; j++)
                {
                    if (all[i + j] != term[j]) { match = false; break; }
                }
                if (match) { termIndex = i; break; }
            }

            Assert.True(termIndex > bodyStart, "Terminator not found");

            var bodyBytes = new ArraySegment<byte>(all, bodyStart, termIndex - bodyStart).ToArray();

            // Undo dot-stuffing
            var unstuffed = new MemoryStream();
            int p = 0;
            // If body starts with ".." then reduce to "."
            if (bodyBytes.Length >= 2 && bodyBytes[0] == (byte)'.' && bodyBytes[1] == (byte)'.')
            {
                unstuffed.WriteByte((byte)'.');
                p = 2;
            }
            while (p < bodyBytes.Length)
            {
                // Look for CRLF followed by ".."
                if (p + 3 <= bodyBytes.Length && bodyBytes[p] == 0x0D && bodyBytes[p + 1] == 0x0A && bodyBytes[p + 2] == (byte)'.' && p + 3 < bodyBytes.Length && bodyBytes[p + 3] == (byte)'.')
                {
                    // copy CRLF and single dot
                    unstuffed.WriteByte(0x0D);
                    unstuffed.WriteByte(0x0A);
                    unstuffed.WriteByte((byte)'.');
                    p += 4;
                    continue;
                }

                unstuffed.WriteByte(bodyBytes[p]);
                p++;
            }

            var result = unstuffed.ToArray();
            if (!result.SequenceEqual(data))
            {
                // Allow server to omit a final CRLF in the stored message when returning it — accept match without a trailing CRLF
                if (data.Length >= 2 && data[data.Length - 2] == (byte)'\r' && data[data.Length - 1] == (byte)'\n' && result.Length == data.Length - 2 && result.SequenceEqual(data.Take(data.Length - 2)))
                {
                    // acceptable
                }
                else
                {
                    Assert.Equal(data, result); // rethrow with helpful diff
                }
            }
        }

        [Fact]
        public async Task Uidl_Lists_Uids()
        {
            var repo = new TestMessagesRepository();
            var m1 = new Message { Id = Guid.NewGuid(), Data = Encoding.ASCII.GetBytes("a") };
            var m2 = new Message { Id = Guid.NewGuid(), Data = Encoding.ASCII.GetBytes("b") };
            await repo.AddMessage(m1);
            await repo.AddMessage(m2);

            var (ctx, ms) = CreateContext(repo);
            ctx.Authenticated = true;
            var handler = new UidlCommand();

            await handler.ExecuteAsync(ctx, null, CancellationToken.None);
            var outStr = ReadOutput(ms);
            Assert.Contains("+OK", outStr);
            Assert.Contains("1 ", outStr);
            Assert.Contains("2 ", outStr);
            Assert.Contains(".\r\n", outStr);
        }

        [Fact]
        public async Task Dele_Deletes_Message()
        {
            var repo = new TestMessagesRepository();
            var m = new Message { Id = Guid.NewGuid(), Data = Encoding.ASCII.GetBytes("x") };
            await repo.AddMessage(m);

            var (ctx, ms) = CreateContext(repo);
            ctx.Authenticated = true;
            var handler = new DeleCommand();

            await handler.ExecuteAsync(ctx, "1", CancellationToken.None);
            var outStr = ReadOutput(ms);
            Assert.Contains("+OK", outStr);
            var remaining = repo.GetAllMessages().ToList();
            Assert.Empty(remaining);
        }

        [Fact]
        public async Task Rset_Returns_OK()
        {
            var (ctx, ms) = CreateContext();
            ctx.Authenticated = true;
            var handler = new RsetCommand();

            await handler.ExecuteAsync(ctx, null, CancellationToken.None);
            var outStr = ReadOutput(ms);
            Assert.Contains("+OK", outStr);
        }

        [Fact]
        public async Task Quit_Sends_Goodbye_And_Ends()
        {
            var (ctx, ms) = CreateContext();
            var handler = new QuitCommand();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await handler.ExecuteAsync(ctx, null, CancellationToken.None));
            var outStr = ReadOutput(ms);
            Assert.Contains("+OK", outStr);
        }

        [Fact]
        public async Task Capa_Advertises_Stls_Based_On_Options()
        {
            var (ctx, ms) = CreateContext(options: new ServerOptions { Pop3TlsMode = TlsMode.StartTls });
            ctx.Authenticated = false;
            var handler = new CapaCommand();

            await handler.ExecuteAsync(ctx, null, CancellationToken.None);
            await ctx.Writer.FlushAsync();
            var outStr = ReadOutput(ms);
            Assert.Contains("UIDL", outStr);
            Assert.Contains("STLS", outStr);

            // Without TLS — create a fresh context and read from its stream
            var (ctx2, ms2) = CreateContext(options: new ServerOptions { Pop3TlsMode = TlsMode.None });
            await new CapaCommand().ExecuteAsync(ctx2, null, CancellationToken.None);
            await ctx2.Writer.FlushAsync();
            var outStr2 = ReadOutput(ms2);
            Assert.Contains("UIDL", outStr2);
            Assert.DoesNotContain("STLS", outStr2);
        }

        [Fact]
        public async Task Stls_IsRejected_When_Pop3Mode_Is_ImplicitTls()
        {
            var (ctx, ms) = CreateContext(options: new ServerOptions { Pop3TlsMode = TlsMode.ImplicitTls });
            var handler = new StlsCommand();

            await handler.ExecuteAsync(ctx, null, CancellationToken.None);
            await ctx.Writer.FlushAsync();

            var outStr = ReadOutput(ms);
            Assert.StartsWith("-ERR", outStr);
            Assert.Contains("STLS not supported", outStr);
        }

        [Fact]
        public async Task Capa_DoesNotAdvertise_Stls_When_Pop3Mode_Is_ImplicitTls()
        {
            var (ctx, ms) = CreateContext(options: new ServerOptions { Pop3TlsMode = TlsMode.ImplicitTls });
            ctx.Authenticated = false;
            var handler = new CapaCommand();

            await handler.ExecuteAsync(ctx, null, CancellationToken.None);
            await ctx.Writer.FlushAsync();
            var outStr = ReadOutput(ms);

            Assert.Contains("UIDL", outStr);
            Assert.DoesNotContain("STLS", outStr);
        }
    }
}
