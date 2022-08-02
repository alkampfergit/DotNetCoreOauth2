using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace DotNetCoreOAuth2
{
    public class WellKnownConfigurationHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _clientName;

        private ConcurrentDictionary<string, WellKnownEndpoint> _cache = new ConcurrentDictionary<string, WellKnownEndpoint>();

        public WellKnownConfigurationHandler(IHttpClientFactory httpClientFactory, string clientName)
        {
            _httpClientFactory = httpClientFactory;
            _clientName = clientName;
        }

        public void AddKnownServices(string authority, string autorizeUrl, string tokenEndpointUrl) 
        {
            _cache[authority.TrimEnd('/')] = new WellKnownEndpoint(autorizeUrl, tokenEndpointUrl);
        }

        public async Task<string> GetAuthorizationUrlAsync(string authority)
        {
            authority = authority.TrimEnd('/');
            WellKnownEndpoint endpoints = await GetEndpointForAuthorityAsync(authority);
            return endpoints.AutorizeUrl;
        }

        public async Task<string> GetTokenUrlAsync(string authority)
        {
            authority = authority.TrimEnd('/');
            WellKnownEndpoint endpoints = await GetEndpointForAuthorityAsync(authority);
            return endpoints.TokenEndpointUrl;
        }

        private async Task<WellKnownEndpoint> GetEndpointForAuthorityAsync(string authority)
        {
            if (!_cache.TryGetValue(authority, out var endpoints))
            {
                endpoints = await DownloadEndpointsForAuthorityAsync(authority);
                _cache[authority.TrimEnd('/')] = endpoints;
            }

            return endpoints;
        }

        private async Task<WellKnownEndpoint> DownloadEndpointsForAuthorityAsync(string authority)
        {
            var client = _httpClientFactory.CreateClient(_clientName);
            var url = $"{authority}/.well-known/openid-configuration";
            var response = await client.GetStringAsync(url);
            var responseJson = JsonConvert.DeserializeAnonymousType(response, new 
            {
                token_endpoint = "",
                authorization_endpoint = "",
            });
            return new WellKnownEndpoint(responseJson.authorization_endpoint, responseJson.token_endpoint);
        }

        private record WellKnownEndpoint
        {
            public WellKnownEndpoint(string autorizeUrl, string tokenEndpointUrl)
            {
                AutorizeUrl = autorizeUrl;
                TokenEndpointUrl = tokenEndpointUrl;
            }

            public string AutorizeUrl { get; }
            public string TokenEndpointUrl { get; }
        }
    }
}
