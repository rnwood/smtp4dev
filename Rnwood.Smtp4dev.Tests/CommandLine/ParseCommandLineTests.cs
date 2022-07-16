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
        [InlineData(0)]
        [InlineData(67000)]
        public void SmtpPortMustBeValidTcpPort(int portNumber)
        {
            // TCP port range 1-65535 is valid
            var commandLineOptions = CommandLineParser.TryParseCommandLine(new[] { $"{RelaySmtpPortParam}={portNumber}" }, false);
            var cmdLineOptions = new CommandLineOptions();
            Action act = () => new ConfigurationBuilder().AddCommandLineOptions(commandLineOptions).Build().Bind(cmdLineOptions);
            act.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentOutOfRangeException>();
        }
    }
}