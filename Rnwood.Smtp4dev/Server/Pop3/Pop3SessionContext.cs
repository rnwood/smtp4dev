namespace Rnwood.Smtp4dev.Server.Pop3
{
	using System.IO;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Logging;
	using Rnwood.Smtp4dev.Data;
	using Rnwood.Smtp4dev.Server.Settings;

	internal class Pop3SessionContext
	{
		public Stream Stream { get; set; }
		public StreamWriter Writer { get; set; }
		public StreamReader Reader { get; set; }
		public string Username { get; set; }
		public bool Authenticated { get; set; }
		public IMessagesRepository MessagesRepository { get; set; }
		public ILogger Logger { get; set; }
		public ServerOptions Options { get; set; }
		public CancellationToken CancellationToken { get; set; }

		public Task WriteLineAsync(string line)
		{
			Writer.Write(line);
			Writer.Write("\r\n");
			return Writer.FlushAsync();
		}

		public Task WriteAsync(string text)
		{
			Writer.Write(text);
			return Writer.FlushAsync();
		}

		public async Task<string> ReadLineAsync()
		{
			var ms = new MemoryStream();
			int prev = -1;
			var buffer = new byte[1];
			while (!CancellationToken.IsCancellationRequested)
			{
				int read = await Stream.ReadAsync(buffer, 0, 1, CancellationToken).ConfigureAwait(false);
				if (read == 0) return null;
				ms.WriteByte(buffer[0]);
				if (prev == '\r' && buffer[0] == '\n') break;
				prev = buffer[0];
			}
			var bytes = ms.ToArray();
			return System.Text.Encoding.ASCII.GetString(bytes).TrimEnd('\r', '\n');
		}

		public Pop3SessionContext()
		{
			// defaults
			Username = string.Empty;
			Authenticated = false;
		}
	}
}
