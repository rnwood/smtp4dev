using System;
using System.Reflection;
using CommandLiners;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.CommandLine
{
    public class ParseCommandLineTests
    {
        private const string RelaySmtpPortParam = "--relaysmtpport";

        [Theory]
        [InlineData(500)]
        public void CanParseRelaySmtpPort(int portNumber)
        {
            var commandLineOptions = CommandLineParser.TryParseCommandLine(new[] { $"{RelaySmtpPortParam}={portNumber}" }, false);
            var cmdLineOptions = new CommandLineOptions();
            new ConfigurationBuilder().AddCommandLineOptions(commandLineOptions).Build().Bind(cmdLineOptions);
            cmdLineOptions.RelayOptions.SmtpPort.Should().Be(portNumber);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(67000)]
        public void SmtpPortMustBeValidTcpPort(int portNumber)
        {
            // TCP port range 0-65535 is valid
            var commandLineOptions = CommandLineParser.TryParseCommandLine(new[] { $"{RelaySmtpPortParam}={portNumber}" }, false);
            var cmdLineOptions = new CommandLineOptions();
            Action act = () => new ConfigurationBuilder().AddCommandLineOptions(commandLineOptions).Build().Bind(cmdLineOptions);
            act.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void CanParseMultipleUsers()
        {
            var commandLineOptions = CommandLineParser.TryParseCommandLine(new[] { $"--user:u1=p1", "--user:u2=p2" }, false);
            var cmdLineOptions = new CommandLineOptions();
            new ConfigurationBuilder().AddCommandLineOptions(commandLineOptions).Build().Bind(cmdLineOptions);
            cmdLineOptions.ServerOptions.Users.Length.Should().Be(2);
            cmdLineOptions.ServerOptions.Users[0].Username.Should().Be("u1");
            cmdLineOptions.ServerOptions.Users[0].Password.Should().Be("p1");
            cmdLineOptions.ServerOptions.Users[1].Username.Should().Be("u2");
            cmdLineOptions.ServerOptions.Users[1].Password.Should().Be("p2");
        }
    }
}