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
using WebAppTest.Controllers.Models;

namespace WebAppTest.Controllers
{
    [Route("msal-oauth2")]
    public class MSALIMAPController : Controller
    {
        private readonly IOptionsMonitor<OAuth2Settings> _oauth2Settings;
        private readonly IConfidentialClientApplication _confidentialClientApp;
        private readonly IPublicClientApplication _publicClientApp;

        private static Dictionary<string, MSALIMAPModel> InMemoryDb = new();

        public MSALIMAPController(IOptionsMonitor<OAuth2Settings> oauth2Settings)
        {
            _oauth2Settings = oauth2Settings;
            
            // Initialize MSAL Confidential Client for client credentials flow
            _confidentialClientApp = ConfidentialClientApplicationBuilder
                .Create(_oauth2Settings.CurrentValue.ClientId)
                .WithClientSecret(_oauth2Settings.CurrentValue.ClientSecret)
                .WithAuthority(new Uri(_oauth2Settings.CurrentValue.Authority))
                .Build();

            // Initialize MSAL Public Client for authorization code flow
            _publicClientApp = PublicClientApplicationBuilder
                .Create(_oauth2Settings.CurrentValue.ClientId)
                .WithAuthority(_oauth2Settings.CurrentValue.Authority)
                .Build();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new MSALIMAPModel());
        }

        [Route("code-flow")]
        [HttpPost]
        public IActionResult CodeFlow(CodeFlowDto dto)
        {
            var clientScope = dto?.Scope ?? "openid email offline_access https://outlook.office.com/IMAP.AccessAsUser.All https://outlook.office.com/SMTP.Send";
            var scopes = clientScope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var redirectUri = $"{Request.Scheme}://{Request.Host}/msal-oauth2/get-token";
            var customState = Guid.NewGuid().ToString();

            // Build the authorization URL manually for MSAL
            var authorityUri = new Uri(_oauth2Settings.CurrentValue.Authority);
            var authUrl = $"{authorityUri}authorize?" +
                         $"client_id={Uri.EscapeDataString(_oauth2Settings.CurrentValue.ClientId)}&" +
                         $"response_type=code&" +
                         $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                         $"scope={Uri.EscapeDataString(string.Join(" ", scopes))}&" +
                         $"state={Uri.EscapeDataString(customState)}&" +
                         $"response_mode=query";

            var model = new MSALIMAPModel();
            model.State = customState;
            model.LoginLink = authUrl;
            model.DebugLoginLink = DumpUrl(model.LoginLink);

            InMemoryDb[customState] = model;
            return View("Index", model);
        }

        [Route("imap-client-flow")]
        [HttpPost]
        public async Task<IActionResult> ClientFlow(ClientFlowDto dto)
        {
            var model = new MSALIMAPModel();

            try
            {
                string[] scopes = { "https://outlook.office365.com/.default" };
                
                var result = await _confidentialClientApp.AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                model.AccessToken = result.AccessToken;
                model.RequestTokenData = $"MSAL Client Credentials Flow\nScopes: {string.Join(", ", scopes)}\nToken expires: {result.ExpiresOn}";

                await ConnectToImap(model, dto.Email);
            }
            catch (Exception ex)
            {
                model.TestResult = $"Error acquiring token: {ex.Message}";
            }

            return View("Index", model);
        }

        [Route("smtp-client-flow")]
        [HttpPost]
        public async Task<IActionResult> SmtpClientFlow(TestSmtpDto dto)
        {
            var model = new MSALIMAPModel();

            try
            {
                string[] scopes = { "https://outlook.office365.com/.default" };
                
                var result = await _confidentialClientApp.AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                model.AccessToken = result.AccessToken;
                model.RequestTokenData = $"MSAL Client Credentials Flow\nScopes: {string.Join(", ", scopes)}\nToken expires: {result.ExpiresOn}";

                await TrySendTestMail(dto.From, dto.To, model);
            }
            catch (Exception ex)
            {
                model.TestResult = $"Error acquiring token: {ex.Message}";
            }

            return View("Index", model);
        }

        private static async Task TrySendTestMail(string from, string to, MSALIMAPModel model)
        {
            try
            {
                var oauth2 = new SaslMechanismOAuth2(from, model.AccessToken);
                using var smtpclient = new MailKit.Net.Smtp.SmtpClient(new ProtocolLogger(Console.OpenStandardOutput()));
                await smtpclient.ConnectAsync("smtp.office365.com", 587, SecureSocketOptions.Auto);
                await smtpclient.AuthenticateAsync(oauth2);

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(from));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = "Test email - Please no reply (MSAL)";
                message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = "Test sending email using MSAL authentication"
                };
                
                await smtpclient.SendAsync(message);
                model.TestResult = "OK Mail Sent (MSAL)";
            }
            catch (Exception ex)
            {
                model.TestResult = $"Error sending Mail: {ex}";
            }
        }

        [HttpGet]
        [Route("get-token")]
        public async Task<IActionResult> GetToken(string code, string state, string? error = null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest($"OAuth error: {error}");
            }

            if (!InMemoryDb.ContainsKey(state))
            {
                return BadRequest("Invalid state parameter");
            }

            var model = InMemoryDb[state];
            model.IdpResponseLink = Request.QueryString.Value ?? string.Empty;

            try
            {
                string[] scopes = { "openid", "email", "offline_access", "https://outlook.office.com/IMAP.AccessAsUser.All", "https://outlook.office.com/SMTP.Send" };
                
                var result = await _confidentialClientApp.AcquireTokenByAuthorizationCode(scopes, code)
                    .ExecuteAsync();

                model.AccessToken = result.AccessToken;
                model.IdToken = result.IdToken;
                model.RequestTokenData = $"MSAL Authorization Code Flow\nScopes: {string.Join(", ", scopes)}\nToken expires: {result.ExpiresOn}";

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
            catch (Exception ex)
            {
                model.TestResult = $"Error acquiring token: {ex.Message}";
            }

            return View("Index", model);
        }

        [HttpPost]
        [Route("test-imap")]
        public async Task<IActionResult> TestImap(TestImapDto dto)
        {
            var model = InMemoryDb[dto.State];
            return await TestImapConnection(model);
        }

        [Route("test-smtp")]
        [HttpPost]
        public async Task<IActionResult> TestSmtp(TestSmtpDto dto)
        {
            var model = InMemoryDb[dto.State];
            await TrySendTestMail(model.EmailAddress, dto.To, model);
            return View("Index", model);
        }

        private async Task<IActionResult> TestImapConnection(MSALIMAPModel model)
        {
            await ConnectToImap(model);
            return View("Index", model);
        }

        private static async Task ConnectToImap(MSALIMAPModel model, string? email = null)
        {
            email ??= model.EmailAddress;
            var oauth2 = new SaslMechanismOAuth2(email, model.AccessToken);
            model.EmailAddress = email;
            
            try
            {
                using var newClient = new ImapClient(new ProtocolLogger(Console.OpenStandardOutput()));
                await newClient.ConnectAsync("outlook.office365.com", 993, SecureSocketOptions.SslOnConnect);
                await newClient.AuthenticateAsync(oauth2);
                model.TestResult = "IMAP Login OK (MSAL)";
            }
            catch (Exception ex)
            {
                model.TestResult = $"ERROR: {ex.Message}";
            }
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