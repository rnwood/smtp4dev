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
        mocks.Connection.Setup(c => c.MailVerb).Returns(new MailVerb());
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

    [Fact]
    public async Task MailFrom_AllowsAuthParameterWithValue()
    {
        TestMocks mocks = new TestMocks();
        mocks.Connection.Setup(c => c.MailVerb).Returns(new MailVerb());
        mocks.Connection.Setup(c => c.NewMessage()).ReturnsAsync(() => {
            var message = new MemoryMessageBuilder();
            mocks.Connection.Setup(c => c.CurrentMessage).Returns(message);
            return message;
        });


        AuthExtensionProcessor authExtensionProcessor = new AuthExtensionProcessor(mocks.Connection.Object);
        await mocks.Connection.Object.MailVerb.FromSubVerb.Process(mocks.Connection.Object, new SmtpCommand("MAIL FROM:<e=mc2@example.com> AUTH=e+3Dmc2@example.com"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
    }

    [Fact]
    public async Task MailFrom_AllowsAuthParameterWithEmptyPlaceholderValue()
    {
        TestMocks mocks = new TestMocks();
        mocks.Connection.Setup(c => c.MailVerb).Returns(new MailVerb());
        mocks.Connection.Setup(c => c.NewMessage()).ReturnsAsync(() => {
            var message = new MemoryMessageBuilder();
            mocks.Connection.Setup(c => c.CurrentMessage).Returns(message);
            return message;
        });


        AuthExtensionProcessor authExtensionProcessor = new AuthExtensionProcessor(mocks.Connection.Object);
        await mocks.Connection.Object.MailVerb.FromSubVerb.Process(mocks.Connection.Object, new SmtpCommand("MAIL FROM:<e=mc2@example.com> AUTH=<>"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
    }

    [Fact]
    public async Task MailFrom_DoesNotAllowAuthParameterWithEmptyValue()
    {
        TestMocks mocks = new TestMocks();
        mocks.Connection.Setup(c => c.MailVerb).Returns(new MailVerb());
        mocks.Connection.Setup(c => c.NewMessage()).ReturnsAsync(() => {
            var message = new MemoryMessageBuilder();
            mocks.Connection.Setup(c => c.CurrentMessage).Returns(message);
            return message;
        });


        AuthExtensionProcessor authExtensionProcessor = new AuthExtensionProcessor(mocks.Connection.Object);

        var exception = await Assert.ThrowsAsync<SmtpServerException>(async () =>
        {
            await mocks.Connection.Object.MailVerb.FromSubVerb.Process(mocks.Connection.Object, new SmtpCommand("MAIL FROM:<e=mc2@example.com> AUTH="));
        });
        Assert.Equal((int) StandardSmtpResponseCode.SyntaxErrorInCommandArguments, exception.SmtpResponse.Code);
    }
}
