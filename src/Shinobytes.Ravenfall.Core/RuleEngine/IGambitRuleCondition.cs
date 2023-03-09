namespace Shinobytes.Core.RuleEngine
{
    public interface IGambitRuleCondition<TKnowledgeBase>
    {
        bool TestCondition(TKnowledgeBase fact);
    }
}
