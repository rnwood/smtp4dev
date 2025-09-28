using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    internal static class Pop3ProtocolHelper
    {
        // Writes the raw message bytes to the output stream performing POP3 dot-stuffing in a binary safe manner.
        public static async Task WriteDotStuffedMessageAsync(Stream outputStream, byte[] data, CancellationToken cancellationToken = default)
        {
            if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));
            if (data == null) data = Array.Empty<byte>();

            // Ensure we write via a buffer for performance
            using var writer = new BinaryWriter(outputStream, System.Text.Encoding.ASCII, leaveOpen: true);

            bool atLineStart = true;
            for (int i = 0; i < data.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                byte b = data[i];

                if (atLineStart && b == (byte)'.')
                {
                    // Write an extra dot for dot-stuffing
                    writer.Write((byte)'.');
                }

                writer.Write(b);

                // Track line starts. Consider '\n' as line terminator (handles both LF and CRLF)
                atLineStart = (b == (byte)'\n');
            }

            // Ensure final CRLF before end marker
            bool endsWithCRLF = data.Length >= 2 && data[data.Length - 2] == (byte)'\r' && data[data.Length - 1] == (byte)'\n';
            if (!endsWithCRLF)
            {
                writer.Write((byte)'\r');
                writer.Write((byte)'\n');
            }

            // Write terminating dot on its own line
            writer.Write((byte)'.');
            writer.Write((byte)'\r');
            writer.Write((byte)'\n');

            await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
