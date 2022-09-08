using DotNetCoreOAuth2;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Text;
using WebAppTest.Controllers.Models;

namespace WebAppTest.Controllers
{
    [Route("sample-oauth2")]
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
                "openid email offline_access https://outlook.office.com/IMAP.AccessAsUser.All https://outlook.office.com/SMTP.Send",
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

        [Route("imap-client-flow")]
        [HttpPost]
        public async Task<IActionResult> ClientFlow(ClientFlowDto dto)
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();
            var request = await oAuth2Client.GenerateTokenRequestForClientFlowAsync(
                _oauth2Settings.CurrentValue.Authority,
                "https://outlook.office365.com/.default",
                _oauth2Settings.CurrentValue.ClientSecret);

            var model = new SampleIMAPModel();
            await DumpTokenRequest(request, model);
            
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

            model.AccessToken = obj.access_token;

            await ConnectToImap(model, dto.Email);
            return View("Index", model);
        }

        [Route("smtp-client-flow")]
        [HttpPost]
        public async Task<IActionResult> SmtpClientFlow(TestSmtpDto dto)
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();
            var request = await oAuth2Client.GenerateTokenRequestForClientFlowAsync(
                _oauth2Settings.CurrentValue.Authority,
                "https://outlook.office.com/.default",
                _oauth2Settings.CurrentValue.ClientSecret);

            var model = new SampleIMAPModel();
            await DumpTokenRequest(request, model);

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

            model.AccessToken = obj.access_token;

            await TrySendTestMail(dto.From, dto.To, model).ConfigureAwait(false);

            return View("Index", model);
        }

        private static async Task TrySendTestMail(string from, string to, SampleIMAPModel model)
        {
            //now we want to send the email
            try
            {
                var oauth2 = new SaslMechanismOAuth2(from, model.AccessToken);
                using var smtpclient = new MailKit.Net.Smtp.SmtpClient(new ProtocolLogger(Console.OpenStandardOutput()));
                await smtpclient.ConnectAsync(
                    "smtp.office365.com",
                    587,
                    SecureSocketOptions.Auto);
                await smtpclient.AuthenticateAsync(oauth2);

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(from));

                message.To.Add(new MailboxAddress(to, to));

                message.Subject = "Test email - Please no reply";

                message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = "Test sending email"
                };
                await smtpclient.SendAsync(message).ConfigureAwait(false);
                model.TestResult = "OK Mail Sent";
            }
            catch (Exception ex)
            {
                model.TestResult = $"Error sending Mail: {ex.ToString()}";
            }
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

            await DumpTokenRequest(tokenRequest.Request, model);

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

            JwtSecurityTokenHandler h = new JwtSecurityTokenHandler();
            var jwtToken = h.ReadJwtToken(model.IdToken);
            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");
            if (emailClaim == null)
            {
                model.TestResult = "ERROR: Received claim does not contains email";
            }
            else
            {
                model.EmailAddress = emailClaim.Value;
            }

            return View("Index", model);
        }

        private static async Task DumpTokenRequest(HttpRequestMessage  request, SampleIMAPModel model)
        {
            var content = await request.Content.ReadAsStringAsync();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Request to:\n {request.RequestUri}");
            var queryStringParsed = QueryHelpers.ParseQuery(content);
            foreach (var element in queryStringParsed)
            {
                var value = element.Value;
                if (element.Key.Contains("secret"))
                {
                    value = new String('*', element.Value.Single().Length);
                }
                sb.AppendLine($"{element.Key} = {value}");
            }
            model.RequestTokenData = sb.ToString();
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

        [Route("test-smtp")]
        [HttpPost]
        public async Task<IActionResult> TestSmtp(TestSmtpDto dto)
        {
            var model = InMemoryDb[dto.State];

            await TrySendTestMail(model.EmailAddress, dto.To, model).ConfigureAwait(false);

            return View("Index", model);
        }

        private async Task<IActionResult> TestImapConnection(SampleIMAPModel model)
        {
            await ConnectToImap(model);
            return View("Index", model);
        }
        
        private static async Task ConnectToImap(SampleIMAPModel model, string email = null)
        {
            email ??= model.EmailAddress;
            var oauth2_1 = new SaslMechanismOAuth2(email, model.AccessToken);
            model.EmailAddress = email;
            try
            {
                using (var newClient = new ImapClient(new ProtocolLogger(Console.OpenStandardOutput())))
                {
                    await newClient.ConnectAsync("outlook.office365.com", 993, SecureSocketOptions.SslOnConnect);
                    await newClient.AuthenticateAsync(oauth2_1);
                }
                model.TestResult = "IMAP Login OK";
            }
            catch (Exception ex)
            {
                model.TestResult = $"ERROR: {ex.Message}";
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
                var value = element.Value;
                if (element.Key.Contains("secret"))
                {
                    value = new String('*', element.Value.Single().Length);
                }
                sb.AppendLine($"{element.Key} = {value}");
            }

            return sb.ToString();
        }
    }
}
