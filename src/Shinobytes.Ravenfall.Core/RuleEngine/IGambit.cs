using System.Collections.Generic;

namespace Shinobytes.Core.RuleEngine
{
    public interface IGambit<TKnowledgeBase>
    {
        void AddRule(IGambitRule<TKnowledgeBase> rule);
        void AddRules(IEnumerable<IGambitRule<TKnowledgeBase>> ruleCollection);
        void RemoveRule(IGambitRule<TKnowledgeBase> rule);
        bool ProcessRules(TKnowledgeBase fact);
    }
}
