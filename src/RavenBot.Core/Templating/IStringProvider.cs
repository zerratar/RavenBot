namespace RavenBot.Core
{
    public interface IStringProvider
    {
        string Get(string key);

        /// <summary>
        /// Overrides the value the <see cref="Get(string)"/> method returns.
        /// If the <paramref name="newValue"/> is set to <see cref="null"/> the value is reset.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void Override(string oldValue, string newValue);
    }
}