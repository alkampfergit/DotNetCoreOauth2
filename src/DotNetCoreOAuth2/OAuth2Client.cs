using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCoreOAuth2
{
    public class OAuth2Client
    {
        private readonly string _clientId;
        private readonly string _authority;
        private readonly string _redirectUrl;

        public OAuth2Client(
            string clientId,
            string authority,
            string redirectUrl)
        {
            _clientId = clientId;
            _authority = authority;
            _redirectUrl = redirectUrl;
        }

        public HttpRequestMessage GenerateMessageForCodeFlow()
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "https://accounts.google.com/o/oauth2/v2/auth");
            message.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "client_id", _clientId },
                { "redirect_uri", _redirectUrl },
                { "response_type", "code" },
                { "scope", _authority },
                { "access_type", "offline" },
                { "include_granted_scopes", "true" },
                { "state", "state" },
                {"code_verifier", ""}
            });

            return message; ;
        }
    }
}