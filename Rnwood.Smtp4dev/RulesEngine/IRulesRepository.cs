namespace Rnwood.Smtp4dev.RulesEngine
{
    public interface IRulesRepository
    {
        string GetWorkflowRulesAsJsonString(string workflowName);
    }
}