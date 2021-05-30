using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RavenBot.Twitch
{
    public class TwitchOAuthTokenGenerator : IDisposable
    {
        private const string TwitchClientID = "757vrtjoawg2rtquprnfb35nqah1w4";
        private const string TwitchRedirectUri = "https://id.twitch.tv/oauth2/authorize";

        public event EventHandler<TwitchOAuthResult> AccessTokenReceived;
        private HttpListener listener;
        private bool disposed;

        private readonly Random random = new Random();

        public async Task StartAuthenticationProcess(Action<string> onAuthUrlAvailable, Action onError)
        {
            var validationToken = GenerateValidationToken();
            var authUrl = GetAccessTokenRequestUrl(validationToken);
            TwitchOAuthResult accessToken = null;
            TaskCompletionSource<TwitchOAuthResult> tokenResult
                = new TaskCompletionSource<TwitchOAuthResult>();

            GetAccessToken(authUrl, (token, user, userId) =>
            {
                accessToken = new TwitchOAuthResult();
                accessToken.AccessToken = token;
                accessToken.User = user;
                accessToken.UserID = userId;
                tokenResult.SetResult(accessToken);
            }, onError);

            if (onAuthUrlAvailable != null)
            {
                onAuthUrlAvailable(authUrl);
            }

            await tokenResult.Task;

            AccessTokenReceived?.Invoke(this, accessToken);
        }

        private string GetAccessTokenRequestUrl(string validationToken)
        {
            return
                TwitchRedirectUri + "?response_type=code" +
                $"&client_id={TwitchClientID}" +
                $"&redirect_uri=https://www.ravenfall.stream/api/twitch/authorize" +
                //$"&redirect_uri=https://localhost:5001/api/twitch/authorize" +
                $"&scope=user_read+channel:moderate+chat:edit+chat:read" +
                $"&state={validationToken}&force_verify=true";
        }

        private string GenerateValidationToken()
        {
            return Convert.ToBase64String(Enumerable.Range(0, 20).Select(x =>
            (byte)((byte)(random.NextDouble() * ((byte)'z' - (byte)'a')) + (byte)'a')).ToArray());
        }

        private void GetAccessToken(string redirectUrl, Action<string, string, string> onTokenReceived, Action onError)
        {
            new Thread(async () =>
            {
                try
                {
                    if (listener == null || !listener.IsListening)
                    {
                        listener = new HttpListener();
                        listener.Prefixes.Add("http://*:8182/");
                        listener.Start();
                    }

                    try
                    {
                        while (true)
                        {
                            var context = listener.GetContext();
                            var req = context.Request;
                            var res = context.Response;

                            if (req.Url.ToString().Contains("twitchredirect"))
                            {
                                res.Redirect(redirectUrl);
                                res.Close();
                            }
                            else
                            {
                                var state = req.QueryString["state"];
                                var accessToken = req.QueryString["token"];

                                var user = req.QueryString["user"];
                                var userId = req.QueryString["id"];

                                if (string.IsNullOrEmpty(user))
                                {
                                    var result = await ValidateOAuthAsync(accessToken);
                                    if (result != null)
                                    {
                                        user = result.Login;
                                        userId = result.UserID;
                                    }
                                }

                                onTokenReceived(accessToken, user, userId);
                                res.StatusCode = 200;
                                res.Close();
                                return;
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine(exc);
                        onError?.Invoke();

                        Console.WriteLine("You need to run RavenBot.exe as administrator.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    onError?.Invoke();

                    Console.WriteLine("You need to run RavenBot.exe as administrator.");
                }
            }).Start();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (listener != null)
            {
                try { listener.Abort(); } catch { }
                try { listener.Close(); } catch { }
                try { listener.Stop(); } catch { }
            }
        }


        private async Task<TwitchValidateResponse> ValidateOAuthAsync(string access_token)
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("https://id.twitch.tv/oauth2/validate");
            req.Method = "GET";
            req.Accept = "application/vnd.twitchtv.v5+json";
            req.Headers["Authorization"] = $"OAuth {access_token}";
            req.Headers["Client-ID"] = TwitchClientID;

            try
            {
                using (var res = await req.GetResponseAsync())
                using (var stream = res.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<TwitchValidateResponse>(
                            await reader.ReadToEndAsync());
                }
            }
            catch (WebException we)
            {
                var resp = we.Response as HttpWebResponse;
                if (resp != null)
                {
                    using (var stream = resp.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        var errorText = await reader.ReadToEndAsync();
                        Console.WriteLine(errorText);

                    }
                }
                return null;
            }
        }
    }

    public class TwitchValidateResponse
    {
        [JsonProperty("client_id")]
        public string ClientID { get; set; }
        public string Login { get; set; }
        public string[] Scopes { get; set; }
        [JsonProperty("user_id")]
        public string UserID { get; set; }
    }

    public class TwitchOAuthResult
    {
        public string AccessToken { get; set; }
        public string UserID { get; set; }
        public string User { get; set; }
    }
}
