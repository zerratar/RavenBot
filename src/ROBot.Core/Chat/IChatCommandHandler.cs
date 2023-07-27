using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat
{
    public interface IChatCommandHandler : IDisposable
    {
        IReadOnlyList<ChatCommandInput> Inputs { get; }
        string Description { get; }
        string UsageExample { get; }
        string Category { get; }
        bool RequiresBroadcaster { get; set; }
        Task HandleAsync(IBotServer game, IChatCommandClient twitch, ICommand cmd);
    }

    public class ChatCommandInput
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string[] Choices { get; set; }
        public ChatCommandInput[] Options { get; set; }
        public bool IsRequired { get; set; }

        public static ChatCommandInput Create(string name, string description)
        {
            return new ChatCommandInput { Name = name, Description = description };
        }

        public static ChatCommandInput Create(string name, string description, params string[] options)
        {
            return new ChatCommandInput { Name = name, Description = description, Choices = options };
        }

        public ChatCommandInput WithOptions(params ChatCommandInput[] options)
        {
            this.Options = options;
            return this;
        }

        public ChatCommandInput NotRequired()
        {
            IsRequired = false;
            return this;
        }

        public ChatCommandInput Required()
        {
            IsRequired = true;
            return this;
        }
    }
}
