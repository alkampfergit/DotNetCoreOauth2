using NSubstitute;
using System.Net;
using System.Net.Http.Headers;

namespace DotNetCoreOAuth2.Tests
{
    [TestFixture]
    public class WellKnownConfigurationHandlerTests
    {
        private IHttpClientFactory _httpClientFactory;
        private WellKnownConfigurationHandler _sut;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _httpClientFactory = Substitute.For<IHttpClientFactory>();
            //we work with a real http client, this test will perform a 
            _httpClientFactory.CreateClient("default").Returns(_ => new HttpClient(new WellKnownHandlerStub(GenerateResponse)));
            _sut = new WellKnownConfigurationHandler(_httpClientFactory, "default");
        }

        private Task<HttpResponseMessage> GenerateResponse(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response.Content = new StringContent(@"{
""token_endpoint"": ""https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/oauth2/token"",
""token_endpoint_auth_methods_supported"": [
""client_secret_post"",
""private_key_jwt"",
""client_secret_basic""
],
""jwks_uri"": ""https://login.microsoftonline.com/common/discovery/keys"",
""response_modes_supported"": [
""query"",
""fragment"",
""form_post""
],
""subject_types_supported"": [
""pairwise""
],
""id_token_signing_alg_values_supported"": [
""RS256""
],
""response_types_supported"": [
""code"",
""id_token"",
""code id_token"",
""token id_token"",
""token""
],
""scopes_supported"": [
""openid""
],
""issuer"": ""https://sts.windows.net/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/"",
""microsoft_multi_refresh_token"": true,
""authorization_endpoint"": ""https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/oauth2/authorize"",
""device_authorization_endpoint"": ""https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/oauth2/devicecode"",
""http_logout_supported"": true,
""frontchannel_logout_supported"": true,
""end_session_endpoint"": ""https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/oauth2/logout"",
""claims_supported"": [
""sub"",
""iss"",
""cloud_instance_name"",
""cloud_instance_host_name"",
""cloud_graph_host_name"",
""msgraph_host"",
""aud"",
""exp"",
""iat"",
""auth_time"",
""acr"",
""amr"",
""nonce"",
""email"",
""given_name"",
""family_name"",
""nickname""
],
""check_session_iframe"": ""https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/oauth2/checksession"",
""userinfo_endpoint"": ""https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/openid/userinfo"",
""kerberos_endpoint"": ""https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/kerberos"",
""tenant_region_scope"": ""EU"",
""cloud_instance_name"": ""microsoftonline.com"",
""cloud_graph_host_name"": ""graph.windows.net"",
""msgraph_host"": ""graph.microsoft.com"",
""rbac_url"": ""https://pas.windows.net""
}");
            return Task.FromResult(response);
        }

        public class WellKnownHandlerStub : DelegatingHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

            public WellKnownHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
            {
                _handlerFunc = handlerFunc;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handlerFunc(request, cancellationToken);
            }
        }

        [Test]
        public async Task Can_find_authorize_endpoint()
        {
            var url = await _sut.GetAuthorizationUrlAsync("https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69");
            Assert.That(url, Is.EqualTo("https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/oauth2/authorize"));
        }

        [Test]
        public async Task Can_find_token_endpoint()
        {
            var url = await _sut.GetTokenUrlAsync("https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69");
            Assert.That(url, Is.EqualTo("https://login.microsoftonline.com/2aaa4b86-cc65-43c7-b348-89f4cb2b5c69/oauth2/token"));
        }
    }
}
