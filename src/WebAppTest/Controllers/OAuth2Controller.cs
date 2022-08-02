using DotNetCoreOAuth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace WebAppTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OAuth2Controller : ControllerBase
    {
        private readonly CodeFlowHelper _codeFlowHelper;
        private readonly WellKnownConfigurationHandler _wellKnownConfigurationHandler;
        private readonly IOptionsMonitor<OAuth2Settings> _oauth2Settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public OAuth2Controller(
            CodeFlowHelper codeFlowHelper,
            WellKnownConfigurationHandler wellKnownConfigurationHandler,
            IOptionsMonitor<OAuth2Settings> oauth2Settings,
            IHttpClientFactory httpClientFactory)
        {
            _codeFlowHelper = codeFlowHelper;
            _wellKnownConfigurationHandler = wellKnownConfigurationHandler;
            _oauth2Settings = oauth2Settings;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        [Route("get-token")]
        public async Task<IActionResult> GetToken()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();
            var request = await oAuth2Client.GenerateTokenRequestAsync(Request.QueryString.Value!, _oauth2Settings.CurrentValue.ClientSecret);

            var client = _httpClientFactory.CreateClient("default");
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) 
            {
                string error = "";
                if (response.Content != null) 
                {
                    error = (await response.Content.ReadAsStringAsync()) ?? "";
                }
                return StatusCode(500, $"Internal error: {error}");
            }

            var stringResponse = await response.Content.ReadAsStringAsync();

            var token = Oauth2Token.DeserializeFromTokenResponse(stringResponse);
            return Ok(token);
        }

        [HttpGet]
        [Route("trigger-login")]
        public async Task<IActionResult> TriggerLogin()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();

            var loginMessage = await oAuth2Client.GenerateUrlForCodeFlowAsync(
                "openid profile offline_access",
                new Dictionary<string, string>());

            return Ok(loginMessage);
        }

        private OAuth2Client CreateOAuth2Client()
        {
            var relativeUrl = Url.Action("GetToken", "OAuth2")!;
            var redirectUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/{relativeUrl.TrimStart('/')}";
            OAuth2Client oAuth2Client = new OAuth2Client(
                _codeFlowHelper,
                _wellKnownConfigurationHandler,
                _oauth2Settings.CurrentValue.ClientId,
                _oauth2Settings.CurrentValue.Authority,
                redirectUrl);
            return oAuth2Client;
        }
    }
}