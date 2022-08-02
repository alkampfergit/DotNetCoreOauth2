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
        private readonly IOptionsMonitor<OAuth2Settings> _oauth2Settings;

        public OAuth2Controller(
            CodeFlowHelper codeFlowHelper,
            IOptionsMonitor<OAuth2Settings> oauth2Settings)
        {
            _codeFlowHelper = codeFlowHelper;
            _oauth2Settings = oauth2Settings;
        }

        [HttpGet]
        [Route("get-token")]
        public IActionResult GetToken()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();
            var request = oAuth2Client.GenerateTokenRequest(Request);
            return Ok("Hello World");
        }

        [HttpGet]
        [Route("trigger-login")]
        public IActionResult TriggerLogin()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();

            var loginMessage = oAuth2Client.GenerateUrlForCodeFlow(
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
                _oauth2Settings.CurrentValue.ClientId,
                _oauth2Settings.CurrentValue.Authority,
                redirectUrl);
            return oAuth2Client;
        }
    }
}