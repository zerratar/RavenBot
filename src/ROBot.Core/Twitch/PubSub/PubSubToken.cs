using System;

namespace ROBot.Core.Twitch
{
    public class PubSubToken
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }
        public string UnverifiedToken { get; set; }
        public bool BadAuth { get; set; }

        public event EventHandler OnUpdated;
        public void Update()
        {
            OnUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
