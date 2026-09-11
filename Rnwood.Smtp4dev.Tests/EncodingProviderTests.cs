using System.Text;
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
}
