using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

public class DefaultServerBehaviourTests
{
    private readonly Mock<IConnection> connectionMock;

    public DefaultServerBehaviourTests() => connectionMock = new Mock<IConnection>();

    [Fact]
    public void CanSetAuthMechanismsViaSmtpServer()
    {
        DefaultServer smtpServer = new DefaultServer(true);
        smtpServer.Behaviour.EnabledAuthMechanisms.Clear();
        smtpServer.Behaviour.EnabledAuthMechanisms.Add(new LoginMechanism());
        smtpServer.Behaviour.EnabledAuthMechanisms.Count.Should().Be(1);
    }

    [Theory]
    [ClassData(typeof(AuthMechanismData))]
    public async Task EnsureDefaultBehaviourIsAllAUthMechanismsAreEnabled(IAuthMechanism authMechanism)
    {
        DefaultServerBehaviour defaultServerBehaviour = new DefaultServerBehaviour(true);
        defaultServerBehaviour.EnabledAuthMechanisms.Should().NotBeNull();
        bool enabled = await defaultServerBehaviour.IsAuthMechanismEnabled(connectionMock.Object, authMechanism)
            ;
        enabled.Should().BeTrue();
    }

    [Theory]
    [ClassData(typeof(AuthMechanismData))]
    public async Task WhenASupportedAuthMechanismIdentifierIsConfiguredThenVerifyOnlyTheyAreEnabled(
        IAuthMechanism authMechanism)
    {
        DefaultServerBehaviour defaultServerBehaviour = new DefaultServerBehaviour(true);
        PlainMechanism enabledMechanism = new PlainMechanism();
        defaultServerBehaviour.EnabledAuthMechanisms.Clear();
        defaultServerBehaviour.EnabledAuthMechanisms.Add(enabledMechanism);

        bool enabled = await defaultServerBehaviour.IsAuthMechanismEnabled(connectionMock.Object, authMechanism)
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
