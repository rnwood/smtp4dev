using System;
using AwesomeAssertions;
using CommandLiners;
using Microsoft.Extensions.Configuration;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class FakeGatewaysOptionsTests
    {
        [Fact]
        public void Defaults_AreDisabled()
        {
            var options = new ConfigurationBuilder().Build().GetSection("FakeGateways").Get<FakeGatewaysOptions>() ?? new FakeGatewaysOptions();

            options.Enabled.Should().BeFalse();
            options.MaxStoredItems.Should().Be(1000);
            options.Konzulta.RefuseNumbers.Should().BeEmpty();
            options.Targito.UpstreamUrl.Should().BeNull();
            options.Targito.TemplateNames.Should().BeEmpty();
        }

        [Fact]
        public void BindsFromConfiguration()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
            {
                ["FakeGateways:Enabled"] = "true",
                ["FakeGateways:MaxStoredItems"] = "5",
                ["FakeGateways:Konzulta:RefuseNumbers:0"] = "+420700000099",
                ["FakeGateways:Targito:UpstreamUrl"] = "https://upstream.example.com/JsonRpc",
                ["FakeGateways:Targito:TemplateNames:t1"] = "Name 1"
            }).Build();

            var options = config.GetSection("FakeGateways").Get<FakeGatewaysOptions>();

            options.Enabled.Should().BeTrue();
            options.MaxStoredItems.Should().Be(5);
            options.Konzulta.RefuseNumbers.Should().Equal("+420700000099");
            options.Targito.UpstreamUrl.Should().Be("https://upstream.example.com/JsonRpc");
            options.Targito.TemplateNames["t1"].Should().Be("Name 1");
        }

        [Fact]
        public void BindsFromEnvironmentVariableStyleKey()
        {
            Environment.SetEnvironmentVariable("FakeGateways__Enabled", "true");
            try
            {
                var options = new ConfigurationBuilder().AddEnvironmentVariables().Build().GetSection("FakeGateways").Get<FakeGatewaysOptions>();
                options.Enabled.Should().BeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable("FakeGateways__Enabled", null);
            }
        }

        [Fact]
        public void CommandLineSwitchEnablesFakeGateways()
        {
            var map = CommandLineParser.TryParseCommandLine(new[] { "--fakegateways" }, false);

            var cmdLineOptions = new CommandLineOptions();
            var config = new ConfigurationBuilder().AddCommandLineOptions(map).Build();
            config.Bind(cmdLineOptions);

            cmdLineOptions.FakeGateways.Enabled.Should().BeTrue();
            config.GetSection("FakeGateways").Get<FakeGatewaysOptions>().Enabled.Should().BeTrue();
        }

        [Fact]
        public void WithoutCommandLineSwitchNothingIsSet()
        {
            var map = CommandLineParser.TryParseCommandLine(new string[0], false);

            var config = new ConfigurationBuilder().AddCommandLineOptions(map).Build();

            config["FakeGateways:Enabled"].Should().BeNull();
        }
    }
}
