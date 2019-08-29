using TwitchLib.Client.Models;

namespace RavenBot.Core
{
    public interface IConnectionCredentialsProvider
    {
        ConnectionCredentials Get();
    }
}