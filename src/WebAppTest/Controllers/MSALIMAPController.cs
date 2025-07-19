using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using MimeKit;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using WebAppTest.Controllers.Models;

namespace WebAppTest.Controllers
{
    [Route("msal-oauth2")]
    public class MSALIMAPController : Controller
    {
        private readonly IOptionsMonitor<OAuth2Settings> _oauth2Settings;
        private readonly IConfidentialClientApplication _confidentialClientApplication;
        private static Dictionary<string, MSALIMAPModel> InMemoryDb = new();

        private static string[] _scopes = 
            "openid email offline_access https://outlook.office.com/IMAP.AccessAsUser.All https://outlook.office.com/SMTP.Send"
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        public MSALIMAPController(
            IOptionsMonitor<OAuth2Settings> oauth2Settings,
            IConfidentialClientApplication confidentialClientApplication)
        {
            _oauth2Settings = oauth2Settings;
            _confidentialClientApplication = confidentialClientApplication;
        }

        private async Task<MSALIMAPModel> CreateModelAsync()
        {
            var model = new MSALIMAPModel();
            var accounts = await _confidentialClientApplication.GetAccountsAsync();
            model.MSALHasAccount = accounts.Any();
            return model;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await CreateModelAsync());
        }

        [Route("code-flow")]
        [HttpPost]
        public async Task<IActionResult> CodeFlow(CodeFlowDto dto)
        {   
            var redirectUri = $"{Request.Scheme}://{Request.Host}/msal-oauth2/get-token";
            var customState = Guid.NewGuid().ToString();
            var redirectAuthUrlBuilder = _confidentialClientApplication
                .GetAuthorizationRequestUrl(_scopes);

            var authUrl = await redirectAuthUrlBuilder
                .ExecuteAsync();
            // Build the authorization URL manually for MSAL
            var authorityUri = new Uri(_oauth2Settings.CurrentValue.Authority);
            var model = await CreateModelAsync();
            model.State = customState;
            model.LoginLink = authUrl.AbsoluteUri;
            model.DebugLoginLink = DumpUrl(model.LoginLink);

            InMemoryDb[customState] = model;
            return View("Index", model);
        }

        [HttpGet]
        [Route("get-token")]
        public async Task<IActionResult> GetToken(string code, string state, string? error = null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest($"OAuth error: {error}");
            }

            var model = await CreateModelAsync();
            try
            {
                var result = await _confidentialClientApplication.AcquireTokenByAuthorizationCode(_scopes, code)
                    .ExecuteAsync();

                ParseTokenIntoModel(model, result);
            }
            catch (Exception ex)
            {
                model.TestResult = $"Error acquiring token: {ex.Message}";
            }

            return View("Index", model);
        }

        private static void ParseTokenIntoModel(MSALIMAPModel model, AuthenticationResult result)
        {
            model.AccessToken = result.AccessToken;
            model.IdToken = result.IdToken;
            model.RequestTokenData = $"MSAL Authorization Code Flow\nScopes: {string.Join(", ", _scopes)}\nToken expires: {result.ExpiresOn}";
            if (!string.IsNullOrEmpty(result.IdToken))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(result.IdToken);
                var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");
                if (emailClaim != null)
                {
                    model.EmailAddress = emailClaim.Value;
                }
                else
                {
                    model.TestResult = "ERROR: Received claim does not contain email";
                }
            }
        }

        [Route("test-smtp")]
        [HttpPost]
        public async Task<IActionResult> TestSmtp(TestSmtpDto dto)
        {
            var model = await CreateModelAsync();
            var accounts = await _confidentialClientApplication.GetAccountsAsync();
            var account = accounts.FirstOrDefault();
            var token = await _confidentialClientApplication.AcquireTokenSilent(
                _scopes,
                account)
                .ExecuteAsync();

            ParseTokenIntoModel(model, token);

            return View("Index", model);
        }

        private static string DumpUrl(string stringUri)
        {
            var uri = new Uri(stringUri);
            var queryStringParsed = QueryHelpers.ParseQuery(uri.Query);
            var sb = new StringBuilder();
            sb.AppendLine($"Request to = {uri.AbsoluteUri.Split('?')[0]}");

            foreach (var element in queryStringParsed)
            {
                var value = element.Value;
                if (element.Key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                {
                    value = new string('*', element.Value.Single().Length);
                }
                sb.AppendLine($"{element.Key} = {value}");
            }

            return sb.ToString();
        }
    }
}