using Newtonsoft.Json;

namespace DotNetCoreOAuth2
{
    public class Oauth2Token
    {
        public string Type { get; set; }

        /// <summary>
        /// Email associated to a token, it is important for code that monitors
        /// email IMAP folder or send data with SMTP
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Oid claim.
        /// </summary>
        public string Sub { get; set; }

        public string Scope { get; set; }

        public DateTime ExpireAtUtc { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public string IdToken { get; set; }

        public static Oauth2Token DeserializeFromTokenResponse(string json)
        {
            var obj = JsonConvert.DeserializeAnonymousType(json, new
            {
                token_type = "",
                scope = "",
                expires_in = 0,
                access_token = "",
                refresh_token = "",
                id_token = "",
            });

            return new Oauth2Token()
            {
                AccessToken = obj.access_token,
                ExpireAtUtc = DateTime.UtcNow.AddSeconds(obj.expires_in),
                RefreshToken = obj.refresh_token,
                IdToken = obj.id_token,
                Scope = obj.scope,
                Type = obj.token_type,
            };
        }

        internal void Refresh(Oauth2Token refreshedToken)
        {
            AccessToken = refreshedToken.AccessToken;
            ExpireAtUtc = refreshedToken.ExpireAtUtc;
            if (!string.IsNullOrEmpty(refreshedToken.RefreshToken) && RefreshToken != refreshedToken.RefreshToken)
            {
                RefreshToken = refreshedToken.RefreshToken;
            }
        }
    }
}
