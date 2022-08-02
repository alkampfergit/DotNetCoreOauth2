using Microsoft.AspNetCore.WebUtilities;
using static System.Formats.Asn1.AsnWriter;

namespace DotNetCoreOAuth2
{
    public class OAuth2Client
    {
        private readonly CodeFlowHelper _codeFlowHelper;
        private readonly string _clientId;
        private readonly string _authority;
        private readonly string _redirectUrl;

        public OAuth2Client(
            CodeFlowHelper _codeFlowHelper,
            string clientId,
            string authority,
            string redirectUrl)
        {
            this._codeFlowHelper = _codeFlowHelper;
            _clientId = clientId;
            _authority = authority;
            _redirectUrl = redirectUrl;
        }

        /// <summary>
        /// Generate the token request httpmessage, then the client can send
        /// with simple HttpClient
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public HttpRequestMessage GenerateTokenRequest(Object request)
        {
            var requestData = _codeFlowHelper.GenerateNewRequestData();
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                { "client_id", _clientId },
                { "redirect_uri", _redirectUrl },
                { "response_type", "code" },
                { "scope", scope },
                { "access_type", "offline" },
                { "include_granted_scopes", "true" },
                { "state", requestData.State },
                { "code_challenge", requestData.PkceHashed},
                { "code_challenge_method", "S256"}
            };
            if (customParameters != null)
            {
                foreach (var param in customParameters)
                {
                    parameters[param.Key] = param.Value;
                }
            }
            var url = QueryHelpers.AddQueryString(_authority, parameters);

            return new Uri(url);
        }

        public Uri GenerateUrlForCodeFlow(
            string scope,
            IDictionary<string, string>? customParameters)
        {
            var requestData = _codeFlowHelper.GenerateNewRequestData();
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                { "client_id", _clientId },
                { "redirect_uri", _redirectUrl },
                { "response_type", "code" },
                { "scope", scope },
                { "access_type", "offline" },
                { "include_granted_scopes", "true" },
                { "state", requestData.State },
                { "code_challenge", requestData.PkceHashed},
                { "code_challenge_method", "S256"}
            };
            if (customParameters != null)
            {
                foreach (var param in customParameters)
                {
                    parameters[param.Key] = param.Value;
                }
            }
            var url = QueryHelpers.AddQueryString(_authority, parameters);

            return new Uri(url);
        }
    }
}