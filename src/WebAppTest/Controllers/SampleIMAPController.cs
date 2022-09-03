using DotNetCoreOAuth2;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Text;
using WebAppTest.Controllers.Models;

namespace WebAppTest.Controllers
{
    [Route("sample-imap")]
    public class SampleIMAPController : Controller
    {
        private readonly CodeFlowHelper _codeFlowHelper;
        private readonly WellKnownConfigurationHandler _wellKnownConfigurationHandler;
        private readonly IOptionsMonitor<OAuth2Settings> _oauth2Settings;
        private readonly IHttpClientFactory _httpClientFactory;

        private static Dictionary<string, SampleIMAPModel> InMemoryDb = new Dictionary<string, SampleIMAPModel>();

        public SampleIMAPController(
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
        public IActionResult Index()
        {
            return View(new SampleIMAPModel());
        }

        [Route("code-flow")]
        [HttpPost]
        public async Task<IActionResult> CodeFlow()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();

            var relativeUrl = Url.Action("GetToken", "SampleIMAP")!;
            var redirectUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/{relativeUrl.TrimStart('/')}";
            var customState = Guid.NewGuid().ToString();
            var codeChallengeUrl = await oAuth2Client.GenerateUrlForCodeFlowAsync(
                "openid email offline_access https://outlook.office.com/IMAP.AccessAsUser.All",
                redirectUrl,
                new Dictionary<string, string>(),
                customState: customState);

            // In a real world, this will return a redirect to the code challenge url so that
            // the user will be immediately prompted with a login page.
            var model = new SampleIMAPModel();
            model.State = customState;
            model.LoginLink = codeChallengeUrl.AbsoluteUri;
            model.DebugLoginLink = DumpUrl(model.LoginLink);

            InMemoryDb[customState] = model;
            return View("Index", model);
        }

        [Route("client-flow")]
        [HttpPost]
        public async Task<IActionResult> ClientFlow(ClientFlowDto dto)
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();
            var request = await oAuth2Client.GenerateTokenRequestForClientFlowAsync(
                _oauth2Settings.CurrentValue.Authority,
                "https://outlook.office365.com/.default",
                _oauth2Settings.CurrentValue.ClientSecret);

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
            var obj = JsonConvert.DeserializeAnonymousType(stringResponse, new
            {
                access_token = ""
            })!;

            var model = new SampleIMAPModel();
            model.AccessToken = obj.access_token;
            await ConnectToImap(model, dto.Email);
            return View("Index", model);
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
            var model = InMemoryDb[dto.State];

            return await TestImapConnection(model);
        }

        private async Task<IActionResult> TestImapConnection(SampleIMAPModel model)
        {
            JwtSecurityTokenHandler h = new JwtSecurityTokenHandler();
            var jwtToken = h.ReadJwtToken(model.IdToken);

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");
            if (emailClaim == null)
            {
                model.ImapResult = "ERROR: Received claim does not contains email";
            }
            else
            {
                await ConnectToImap(model, emailClaim.Value);
            }

            return View("Index", model);
        }

        private static async Task ConnectToImap(SampleIMAPModel model, string email)
        {
            var oauth2_1 = new SaslMechanismOAuth2(email, model.AccessToken);
            model.EmailAddress = email;
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
    }
}
