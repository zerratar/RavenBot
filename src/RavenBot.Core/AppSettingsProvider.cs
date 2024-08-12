using Newtonsoft.Json;

namespace RavenBot.Core
{
    public class AppSettingsProvider
    {
        public IAppSettings Get()
        {
            if (System.IO.File.Exists("settings.json"))
            {
                var text = System.IO.File.ReadAllText("settings.json");
                return JsonConvert.DeserializeObject<AppSettings>(text);
            }

            return new AppSettings(null, null, null, null, 4040);
        }
    }
}