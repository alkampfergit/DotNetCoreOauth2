using DotNetCoreOAuth2;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using WebAppTest.Support;

namespace WebAppTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OAuth2Controller : ControllerBase
    {
        private static Oauth2Token _lastToken = null;

        private readonly CodeFlowHelper _codeFlowHelper;
        private readonly WellKnownConfigurationHandler _wellKnownConfigurationHandler;
        private readonly IOptionsMonitor<OAuth2Settings> _oauth2Settings;
        private readonly IHttpClientFactory _httpClientFactory;

        static OAuth2Controller()
        {
            if (System.IO.File.Exists("lasttoken.txt"))
            {
                var text = System.IO.File.ReadAllText("lasttoken.txt");
                _lastToken = JsonConvert.DeserializeObject<Oauth2Token>(text);
            }
        }

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

            await UpdateTokenFromResponse(response);
            return Ok(_lastToken);
        }

        private static async Task UpdateTokenFromResponse(HttpResponseMessage response)
        {
            var stringResponse = await response.Content.ReadAsStringAsync();
            _lastToken = Oauth2Token.DeserializeFromTokenResponse(stringResponse);
            await System.IO.File.WriteAllTextAsync("lasttoken.txt", JsonConvert.SerializeObject(_lastToken));
        }

        [HttpGet]
        [Route("trigger-login")]
        public async Task<IActionResult> TriggerLogin()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();

            var loginMessage = await oAuth2Client.GenerateUrlForCodeFlowAsync(
                //"openid profile offline_access email https://graph.microsoft.com/IMAP.AccessAsUser.All",
                //"openid profile offline_access email https://outlook.office.com/IMAP.AccessAsUser.All",
                "openid offline_access https://outlook.office.com/IMAP.AccessAsUser.All",
                //"openid email offline_access https://graph.microsoft.com/.default",
                new Dictionary<string, string>());

            return Ok(loginMessage);
        }

        [HttpGet]
        [Route("office-365-get-mail")]
        public async Task<IActionResult> GetMail(string emailAddress)
        {
            if (_lastToken == null)
            {
                return StatusCode(500, "No token available");
            }

            if (_lastToken.ExpireAtUtc < DateTime.UtcNow.AddMinutes(2))
            {
                //we need to refresh token
                OAuth2Client oAuth2Client = CreateOAuth2Client();
                var request = await oAuth2Client.GenerateTokenRefreshRequestAsync(
                    _oauth2Settings.CurrentValue.Authority,
                    _lastToken,
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
                    return StatusCode(500, $"Internal error Refreshing token: {error}");
                }

                await UpdateTokenFromResponse(response);
            }

            //var oauth2_1 = new SaslMechanismOAuth2("worker@jarvisdemo.onmicrosoft.com", _lastToken.AccessToken);
            var oauth2_1 = new SaslMethodXOAUTH2(emailAddress, _lastToken.AccessToken);

            using (var newClient = new ImapClient(new ProtocolLogger(Console.OpenStandardOutput())))
            {
                await newClient.ConnectAsync("outlook.office365.com", 993, SecureSocketOptions.SslOnConnect);
                await newClient.AuthenticateAsync(oauth2_1);

                var folder = newClient.GetFolder("archive-to-jarvis");
                folder.Open(MailKit.FolderAccess.ReadWrite);
                var query = SearchQuery.NotSeen;
                var uidList = folder.Search(query)
                    .Take(1000).ToList();
            }

            using var ms = new MemoryStream(_lastToken.RefreshToken.Length + 200);
            using var bw = new BinaryWriter(ms);
            bw.Write(Encoding.ASCII.GetBytes("user="));
            bw.Write(Encoding.ASCII.GetBytes(emailAddress));
            bw.Write((byte)1);
            bw.Write(Encoding.ASCII.GetBytes("auth=Bearer "));
            bw.Write(Encoding.ASCII.GetBytes(_lastToken.AccessToken));
            bw.Write((byte)1);
            bw.Write((byte)1);

            var rawXOAUTH2 = Convert.ToBase64String(ms.ToArray());

            return Ok(new { AuthString = BitConverter.ToString(ms.ToArray()), RawXOAUTH2 = rawXOAUTH2 });
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