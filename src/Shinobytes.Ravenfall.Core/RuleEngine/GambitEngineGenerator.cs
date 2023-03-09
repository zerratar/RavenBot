﻿namespace Shinobytes.Core.RuleEngine
{
    public class GambitGenerator : IGambitGenerator
    {
        public IGambit<TKnowledgeBase> CreateEngine<TKnowledgeBase>()
        {
            return new Gambit<TKnowledgeBase>();
        }
    }
}
