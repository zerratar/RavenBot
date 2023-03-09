namespace Shinobytes.Core.RuleEngine
{
    public interface IGambitGenerator
    {
        IGambit<TKnowledgeBase> CreateEngine<TKnowledgeBase>();
    }
}