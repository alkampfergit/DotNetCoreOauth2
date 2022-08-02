﻿using Microsoft.AspNetCore.WebUtilities;
using System.Security;

namespace DotNetCoreOAuth2
{
    public class OAuth2Client
    {
        private readonly CodeFlowHelper _codeFlowHelper;
        private readonly WellKnownConfigurationHandler _wellKnownConfigurationHandler;
        private readonly string _clientId;
        private readonly string _authority;
        private readonly string _redirectUrl;

        public OAuth2Client(
            CodeFlowHelper _codeFlowHelper,
            WellKnownConfigurationHandler wellKnownConfigurationHandler,
            string clientId,
            string authority,
            string redirectUrl)
        {
            this._codeFlowHelper = _codeFlowHelper;
            _wellKnownConfigurationHandler = wellKnownConfigurationHandler;
            _clientId = clientId;
            _authority = authority;
            _redirectUrl = redirectUrl;
        }

        /// <summary>
        /// Generate the token request httpmessage, then the client can send
        /// with simple HttpClient
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="clientSecret">Client secret if provided</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<HttpRequestMessage> GenerateTokenRequestAsync(string queryString, string clientSecret)
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
            if (!String.IsNullOrEmpty(clientSecret)) 
            {
                parameters["client_secret"] = clientSecret;
            }
            var content = new FormUrlEncodedContent(parameters);
            var tokenRequestUrl = await _wellKnownConfigurationHandler.GetTokenUrlAsync(requestState.Authority);
            var request = new HttpRequestMessage(HttpMethod.Post, tokenRequestUrl);

            request.Content = content;
            return request;
        }

        public async Task<Uri> GenerateUrlForCodeFlowAsync(
            string scope,
            IDictionary<string, string>? customParameters)
        {
            var requestData = _codeFlowHelper.GenerateNewRequestData();
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                ["client_id"] = _clientId,
                ["redirect_uri"] = _redirectUrl,
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

            requestData.AddRequestData(_authority, _redirectUrl);
            return new Uri(url);
        }
    }
}