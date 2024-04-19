using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

public class SmtpServerOptionsTests
{
    private readonly Mock<IConnection> connectionMock;

    public SmtpServerOptionsTests() => connectionMock = new Mock<IConnection>();

    [Fact]
    public void CanSetAuthMechanismsViaSmtpServer()
    {
        SmtpServer smtpServer = new SmtpServer( new Rnwood.SmtpServer.ServerOptions(true, true));
        smtpServer.Options.EnabledAuthMechanisms.Clear();
        smtpServer.Options.EnabledAuthMechanisms.Add(new LoginMechanism());
        smtpServer.Options.EnabledAuthMechanisms.Count.Should().Be(1);
    }

    [Theory]
    [ClassData(typeof(AuthMechanismData))]
    public async Task EnsureDefaultOptionsIsAllAUthMechanismsAreEnabled(IAuthMechanism authMechanism)
    {
        ServerOptions SmtpServerOptions = new Rnwood.SmtpServer.ServerOptions(true, true);
        SmtpServerOptions.EnabledAuthMechanisms.Should().NotBeNull();
        bool enabled = await SmtpServerOptions.IsAuthMechanismEnabled(connectionMock.Object, authMechanism)
            ;
        enabled.Should().BeTrue();
    }

    [Theory]
    [ClassData(typeof(AuthMechanismData))]
    public async Task WhenASupportedAuthMechanismIdentifierIsConfiguredThenVerifyOnlyTheyAreEnabled(
        IAuthMechanism authMechanism)
    {
        ServerOptions SmtpServerOptions = new Rnwood.SmtpServer.ServerOptions(true, true);
        PlainMechanism enabledMechanism = new PlainMechanism();
        SmtpServerOptions.EnabledAuthMechanisms.Clear();
        SmtpServerOptions.EnabledAuthMechanisms.Add(enabledMechanism);

        bool enabled = await SmtpServerOptions.IsAuthMechanismEnabled(connectionMock.Object, authMechanism)
            ;
        if (authMechanism.Identifier == enabledMechanism.Identifier)
        {
            enabled.Should().BeTrue();
        }
        else
        {
            enabled.Should().BeFalse();
        }
    }
}

public class AuthMechanismData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { new CramMd5Mechanism() };
        yield return new object[] { new AnonymousMechanism() };
        yield return new object[] { new LoginMechanism() };
        yield return new object[] { new PlainMechanism() };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
