namespace RavenBot.Core.Templating
{
    public class StringProvider : IStringProvider
    {
        public string Get(string key)
        {
            return key;
        }

        public void Override(string oldValue, string newValue)
        {
        }
    }
}