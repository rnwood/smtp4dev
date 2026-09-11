using System.Text;
using System.IO;
using System.Linq;
using MimeKit;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev;
using Xunit;

namespace Rnwood.Smtp4dev.Tests;

public class EncodingProviderTests
{
    public EncodingProviderTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.RegisterProvider(new Utf7EncodingProvider());
    }

    [Theory]
    [InlineData("iso-8859-1", "é")]
    [InlineData("iso-8859-8", "א")]
    [InlineData("windows-1252", "€")]
    public void LegacyCodePageEncoding_RoundTrips(string name, string value)
    {
        var encoding = Encoding.GetEncoding(name);

        Assert.Equal(value, encoding.GetString(encoding.GetBytes(value)));
    }

    [Theory]
    [InlineData("utf-7")]
    [InlineData("utf7")]
    [InlineData("unicode-1-1-utf-7")]
    [InlineData("csunicode11utf7")]
    public void Utf7EncodingProvider_ResolvesAliases(string name)
    {
        var encoding = Encoding.GetEncoding(name);
        const string value = "héllo";

        Assert.Equal(65002, encoding.CodePage);
        Assert.Equal(value, encoding.GetString(encoding.GetBytes(value)));
        Assert.Same(encoding, Encoding.GetEncoding(65002));
    }

    [Theory]
    [InlineData("iso-8859-1", "Héllo")]
    [InlineData("iso-8859-8", "שלום")]
    [InlineData("iso-8859-8-i", "שלום")]
    [InlineData("windows-1252", "H€llo")]
    [InlineData("utf-7", "Héllo")]
    public void MimeMessage_UsesRegisteredEncodingForTextBody(string charset, string expectedBody)
    {
        var encoding = Encoding.GetEncoding(charset);
        var headers = "From: sender@example.com\r\n"
            + "To: recipient@example.com\r\n"
            + $"Content-Type: text/plain; charset=\"{charset}\"\r\n"
            + "Content-Transfer-Encoding: 8bit\r\n\r\n";
        var messageBytes = Encoding.ASCII.GetBytes(headers)
            .Concat(encoding.GetBytes(expectedBody + "\r\n"))
            .ToArray();

        var message = MimeMessage.Load(new MemoryStream(messageBytes));

        Assert.Equal(expectedBody, message.TextBody.TrimEnd());
        Assert.Equal(expectedBody, new MimeProcessingService().ExtractBodyText(message).Trim());
    }
}
