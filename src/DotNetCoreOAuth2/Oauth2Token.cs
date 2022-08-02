using Newtonsoft.Json;

namespace DotNetCoreOAuth2
{
    public class Oauth2Token
    {
        public string Type { get; private set; }

        public string Scope { get; private set; }

        public DateTime ExpireAtUtc { get; private set; }

        public string AccessToken { get; private set; }

        public string RefreshToken { get; private set; }

        public static Oauth2Token DeserializeFromTokenResponse(string json)
        {
            var obj = JsonConvert.DeserializeAnonymousType(json, new
            {
                token_type = "",
                scope = "",
                expires_in = 0,
                access_token = "",
                refresh_token = "",
            });

            return new Oauth2Token()
            {
                AccessToken = obj.access_token,
                ExpireAtUtc = DateTime.UtcNow.AddSeconds(obj.expires_in),
                RefreshToken = obj.refresh_token,
                Scope = obj.scope,
                Type = obj.token_type,
            };
        }
    }
}
