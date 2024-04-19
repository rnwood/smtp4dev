using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth;

public class AuthExtensionProcessorTests
{
    [Fact]
    public async Task GetEHLOKeywords_ReturnsIdentifiers()
    {
        TestMocks mocks = new TestMocks();
        mocks.ServerOptions.Setup(sb => sb.IsAuthMechanismEnabled(
            It.IsAny<IConnection>(),
            It.IsAny<IAuthMechanism>()
        )).Returns<IConnection, IAuthMechanism>((c, m) =>
            Task.FromResult(m.Identifier == "LOGIN" || m.Identifier == "PLAIN"));
        AuthExtensionProcessor authExtensionProcessor = new AuthExtensionProcessor(mocks.Connection.Object);
        string[] keywords = await authExtensionProcessor.GetEHLOKeywords();

        Assert.Equal(2, keywords.Length);
        Assert.Contains(keywords, k => k.StartsWith("AUTH "));
        Assert.Contains(keywords, k => k.StartsWith("AUTH="));

        foreach (string keyword in keywords)
        {
            string[] ids = keyword.Split(new[] { ' ', '=' }, StringSplitOptions.None).Skip(1).ToArray();
            Assert.Equal(2, ids.Length);
            Assert.Contains(ids, id => id == "LOGIN");
            Assert.Contains(ids, id => id == "PLAIN");
        }
    }
}
