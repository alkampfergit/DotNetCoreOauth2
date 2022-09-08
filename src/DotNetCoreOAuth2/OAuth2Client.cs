using Microsoft.AspNetCore.WebUtilities;
using System.Security;

namespace DotNetCoreOAuth2
{
    public class OAuth2Client
    {
        private readonly CodeFlowHelper _codeFlowHelper;
        private readonly WellKnownConfigurationHandler _wellKnownConfigurationHandler;
        private readonly string _clientId;
        private readonly string _authority;

        public OAuth2Client(
            CodeFlowHelper _codeFlowHelper,
            WellKnownConfigurationHandler wellKnownConfigurationHandler,
            string authority,
            string clientId)
        {
            this._codeFlowHelper = _codeFlowHelper;
            _wellKnownConfigurationHandler = wellKnownConfigurationHandler;
            _clientId = clientId;
            _authority = authority;
        }

        /// <summary>
        /// Generate the token request httpmessage for a simple client flow.
        /// </summary>
        /// <param name="clientSecret">Client secret if provided</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<HttpRequestMessage> GenerateTokenRequestForClientFlowAsync(string authority, string scope, string clientSecret)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = scope,
                ["client_secret "] = clientSecret,
                ["client_id"] = _clientId
            };

            var content = new FormUrlEncodedContent(parameters);
            var tokenRequestUrl = await _wellKnownConfigurationHandler.GetTokenUrlAsync(authority);
            var request = new HttpRequestMessage(HttpMethod.Post, tokenRequestUrl);

            request.Content = content;
            return request;
        }

        /// <summary>
        /// Generate the token request httpmessage, then the client can send
        /// with simple HttpClient
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="clientSecret">Client secret if provided</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<(HttpRequestMessage Request, string CustomState)> GenerateTokenRequestAsync(
            string queryString,
            string clientSecret)
        {
            var queryParameters = QueryHelpers.ParseQuery(queryString);
            var state = queryParameters["state"];
            var requestState = _codeFlowHelper.GetRequestData(state);
            if (requestState == null)
            {
                throw new SecurityException("unexpected state");
            }

            _codeFlowHelper.Clear(state);
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                ["grant_type"] = "authorization_code",
                ["code"] = queryParameters["code"],
                ["redirect_uri"] = requestState.RedirectUrl,
                ["code_verifier"] = requestState.Pkce,
                ["client_id"] = _clientId
            };
            if (!string.IsNullOrEmpty(clientSecret))
            {
                parameters["client_secret"] = clientSecret;
            }
            var content = new FormUrlEncodedContent(parameters);
            var tokenRequestUrl = await _wellKnownConfigurationHandler.GetTokenUrlAsync(requestState.Authority);
            var request = new HttpRequestMessage(HttpMethod.Post, tokenRequestUrl);

            request.Content = content;
            return (request, requestState.CustomState);
        }

        /// <summary>
        /// Generates code for code flow.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="redirectUrl"></param>
        /// <param name="customParameters"></param>
        /// <param name="customState"></param>
        /// <returns></returns>
        public async Task<Uri> GenerateUrlForCodeFlowAsync(
            string scope,
            string redirectUrl,
            IDictionary<string, string>? customParameters,
            string customState = null)
        {
            var requestData = _codeFlowHelper.GenerateNewRequestData();
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                ["client_id"] = _clientId,
                ["redirect_uri"] = redirectUrl,
                ["response_type"] = "code",
                ["scope"] = scope,
                ["access_type"] = "offline",
                ["include_granted_scopes"] = "true",
                ["state"] = requestData.State,
                ["code_challenge"] = requestData.PkceHashed,
                ["code_challenge_method"] = "S256"
                //["code_challenge"] = requestData.Pkce,
                //["code_challenge_method"] = "plain"
            };
            if (customParameters != null)
            {
                foreach (var param in customParameters)
                {
                    parameters[param.Key] = param.Value;
                }
            }

            var authUrl = await _wellKnownConfigurationHandler.GetAuthorizationUrlAsync(_authority);
            var url = QueryHelpers.AddQueryString(authUrl, parameters);

            requestData.AddRequestData(_authority, redirectUrl, customState);
            return new Uri(url);
        }

        public async Task<HttpRequestMessage> GenerateTokenRefreshRequestAsync(
            Oauth2Token oauth2Token,
            string clientSecret)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = oauth2Token.RefreshToken,
                ["client_id"] = _clientId,
            };
            if (!string.IsNullOrEmpty(clientSecret))
            {
                parameters["client_secret"] = clientSecret;
            }
            var content = new FormUrlEncodedContent(parameters);
            var tokenRequestUrl = await _wellKnownConfigurationHandler.GetTokenUrlAsync(_authority);
            var request = new HttpRequestMessage(HttpMethod.Post, tokenRequestUrl);

            request.Content = content;
            return request;
        }

        public bool ValidateToken(string idToken)
        {
            return true;
            //JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            //var validationParameters = new TokenValidationParameters()
            //{
            //    ValidateLifetime = false,
            //    ValidateAudience = false,
            //    ValidateIssuer = true,
            //    ValidIssuer = _authority,
            //    ValidAudience = "Sample",
            //    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) // The same key as the one that generate the token
            //};
            //jwtSecurityTokenHandler.ValidateToken(token.IdToken)
        }
    }
}