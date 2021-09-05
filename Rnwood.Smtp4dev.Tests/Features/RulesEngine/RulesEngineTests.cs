using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.RulesEngine;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Features.RulesEngine
{
    public class RulesEngineTests
    {
        [Fact]
        public async Task CanLoadRulesConfig()
        {
            var logger = Substitute.For<ILogger<RelayMessageRulesEngine>>();
            var rulesRepository = Substitute.For<IRulesRepository>();
            var rules = ResourceHelper.LoadRules();

            rulesRepository.GetWorkflowRulesAsJsonString(WorkflowType.RelayMessages).Returns(rules);
            var engine = new RelayMessageRulesEngine(rulesRepository, logger);
            var ruleResults = await engine.Execute(new Message { From = "Barry@gmail.com", To = "user@gmail.com" });
            ruleResults.First().IsSuccess.Should().BeTrue();
        }
    }
}