using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rnwood.Smtp4dev.DbModel;
using RulesEngine.Models;

namespace Rnwood.Smtp4dev.RulesEngine
{
    public interface IRelayMessageRulesEngine
    {
        Task<List<RuleResultTree>> Execute(Message email);
    }

    public class RelayMessageRulesEngine : IRelayMessageRulesEngine
    {
        private readonly IRulesRepository rulesRepository;
        private readonly ILogger<RelayMessageRulesEngine> logger;
        private global::RulesEngine.RulesEngine re;
        private bool initialised = false;

        public RelayMessageRulesEngine(IRulesRepository rulesRepository, ILogger<RelayMessageRulesEngine> logger)
        {
            this.rulesRepository = rulesRepository;
            this.logger = logger;
        }

        public async Task<List<RuleResultTree>> Execute(Message email)
        {
            if (!initialised)
                Init();
            return await re.ExecuteAllRulesAsync(WorkflowType.RelayMessages, email);
        }

        private void Init()
        {
            var rules = rulesRepository.GetWorkflowRulesAsJsonString(WorkflowType.RelayMessages);
            var settings = new ReSettings { CustomTypes = new[] { typeof(Helpers) } };
            re = new global::RulesEngine.RulesEngine(new[] { rules }, logger, settings);
            this.initialised = true;
        }
    }
}