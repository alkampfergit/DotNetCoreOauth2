using DotNetCoreOAuth2;
using MailKit.Net.Imap;
using MailKit.Security;
using MailKit;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using WebAppTest.Controllers.Models;
using System.IdentityModel.Tokens.Jwt;

namespace WebAppTest.Controllers
{
    [Route("sample-imap-code-flow")]
    public class SampleIMAPCodeFlowController : Controller
    {
        private CodeFlowHelper _codeFlowHelper;
        private WellKnownConfigurationHandler _wellKnownConfigurationHandler;
        private IOptionsMonitor<OAuth2Settings> _oauth2Settings;
        private IHttpClientFactory _httpClientFactory;

        public SampleIMAPCodeFlowController(
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
        public async Task<IActionResult> Index()
        {
            return View(new SampleIMAPCodeFlowModel());
        }

        private static Dictionary<string, SampleIMAPCodeFlowModel> InMemoryDb = new Dictionary<string, SampleIMAPCodeFlowModel>();

        [Route("Flow")]
        [HttpPost]
        public async Task<IActionResult> Flow()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();

            var relativeUrl = Url.Action("GetToken", "SampleIMAPCodeFlow")!;
            var redirectUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/{relativeUrl.TrimStart('/')}";
            var customState = Guid.NewGuid().ToString();
            var codeChallengeUrl = await oAuth2Client.GenerateUrlForCodeFlowAsync(
                "openid email offline_access https://outlook.office.com/IMAP.AccessAsUser.All",
                redirectUrl,
                new Dictionary<string, string>(),
                customState: customState);

            // In a real world, this will return a redirect to the code challenge url so that
            // the user will be immediately prompted with a login page.
            var model = new SampleIMAPCodeFlowModel();
            model.State = customState;
            model.LoginLink = codeChallengeUrl.AbsoluteUri;
            model.DebugLoginLink = DumpUrl(model.LoginLink);

            InMemoryDb[customState] = model;
            return View("Index", model);
        }

        private static string DumpUrl(string stringUri)
        {
            var uri = new Uri(stringUri);
            var queryStringParsed = QueryHelpers.ParseQuery(uri.Query);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Request to = {uri.AbsoluteUri.Split('?')[0]}");
            foreach (var element in queryStringParsed)
            {
                sb.AppendLine($"{element.Key} = {element.Value}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Standard handler that will receive code_challenge data in querystring
        /// and will perform POST request to obtain token.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("get-token")]
        public async Task<IActionResult> GetToken()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();
            var tokenRequest = await oAuth2Client.GenerateTokenRequestAsync(Request.QueryString.Value!, _oauth2Settings.CurrentValue.ClientSecret);
            var model = InMemoryDb[tokenRequest.CustomState];
            model.IdpResponseLink = Request.GetEncodedUrl();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Request to:\n {tokenRequest.Request.RequestUri}");
            var content = await tokenRequest.Request.Content.ReadAsStringAsync();
            var queryStringParsed = QueryHelpers.ParseQuery(content);
            foreach (var element in queryStringParsed)
            {
                sb.AppendLine($"{element.Key} = {element.Value}");
            }
            model.RequestTokenData = sb.ToString();

            var client = _httpClientFactory.CreateClient("default");
            var response = await client.SendAsync(tokenRequest.Request);
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
            model.RefreshToken = token.RefreshToken;
            model.IdToken = token.IdToken;
            model.AccessToken = token.AccessToken;

            return View("Index", model);
        }

        /// <summary>
        /// Standard handler that will receive code_challenge data in querystring
        /// and will perform POST request to obtain token.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("test-imap")]
        public async Task<IActionResult> TestImap(TestImapDto dto)
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();
            var model = InMemoryDb[dto.State];
           
            JwtSecurityTokenHandler h = new JwtSecurityTokenHandler();
            var jwtToken = h.ReadJwtToken(model.IdToken);

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");
            if (emailClaim == null)
            {
                model.ImapResult = "ERROR: Received claim does not contains email";
            }

            var oauth2_1 = new SaslMechanismOAuth2(emailClaim.Value, model.AccessToken);

            try
            {
                using (var newClient = new ImapClient(new ProtocolLogger(Console.OpenStandardOutput())))
                {
                    await newClient.ConnectAsync("outlook.office365.com", 993, SecureSocketOptions.SslOnConnect);
                    await newClient.AuthenticateAsync(oauth2_1);
                }
                model.ImapResult = "IMAP Login OK";
            }
            catch (Exception ex)
            {
                model.ImapResult = $"ERROR: {ex.Message}";
            }

            return View("Index", model);
        }

        private OAuth2Client CreateOAuth2Client()
        {
            OAuth2Client oAuth2Client = new OAuth2Client(
                _codeFlowHelper,
                _wellKnownConfigurationHandler,
                _oauth2Settings.CurrentValue.Authority,
                _oauth2Settings.CurrentValue.ClientId);
            return oAuth2Client;
        }
    }
}
