using DotNetCoreOAuth2;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MailKit.Security;
using System.Text;
using WebAppTest.Support;
using Newtonsoft.Json;

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

            var stringResponse = await response.Content.ReadAsStringAsync();

            _lastToken = Oauth2Token.DeserializeFromTokenResponse(stringResponse);
            System.IO.File.WriteAllText("lasttoken.txt", JsonConvert.SerializeObject(_lastToken));
            return Ok(_lastToken);
        }

        [HttpGet]
        [Route("trigger-login")]
        public async Task<IActionResult> TriggerLogin()
        {
            OAuth2Client oAuth2Client = CreateOAuth2Client();

            var loginMessage = await oAuth2Client.GenerateUrlForCodeFlowAsync(
                //"openid profile offline_access email https://graph.microsoft.com/IMAP.AccessAsUser.All",
                "openid profile offline_access email https://outlook.office.com/IMAP.AccessAsUser.All",
                new Dictionary<string, string>());

            return Ok(loginMessage);
        }

        [HttpGet]
        [Route("office-365-get-mail")]
        public async Task<IActionResult> GetMail()
        {
            if (_lastToken == null) 
            {
                return StatusCode(500, "No token available");
            }
            var client = new ImapClient(new ProtocolLogger(Console.OpenStandardOutput()));

            client.Connect("outlook.office365.com", 993, true);
            
            //client.AuthenticationMechanisms.Clear();
            //client.AuthenticationMechanisms.Add("XOAUTH2");
            //var oauth2 = new SaslMechanismOAuth2("worker@jarvisdemo.onmicrosoft.com", _lastToken.AccessToken);
            //var oauth2 = new SaslMethodXOAUTH2("worker@jarvisdemo.onmicrosoft.com", _lastToken.AccessToken);

            var oauth2 = new SaslMethodXOAUTH2("worker@jarvisdemo.onmicrosoft.com", _lastToken.AccessToken);
            var authString = $"user=worker@jarvisdemo.onmicrosoft.com^Aauth=Bearer {_lastToken.AccessToken}^A^A";
            var rawXOAUTH2 = Convert.ToBase64String(Encoding.ASCII.GetBytes(authString));

            return Ok(new { AuthString = authString, RawXOAUTH2 = rawXOAUTH2});
            //client.Authenticate(oauth2);
            //var folder = client.GetFolder("archive-to-jarvis");
            //folder.Open(MailKit.FolderAccess.ReadWrite);
            //var query = SearchQuery.NotSeen;
            //var uidList = folder.Search(query)
            //    .Take(1000).ToList();

            //var infos = folder.Fetch(uidList, MessageSummaryItems.All);
            //if (uidList.Count > 0)
            //{
            //    foreach (var info in infos)
            //    {
            //        var message = folder.GetMessage(info.UniqueId);
            //        folder.AddFlags(info.UniqueId, MessageFlags.Seen, true, CancellationToken.None);
            //        message.WriteTo("c:\\temp\\email.eml");
            //    }
            //}

            //return Ok("OK");
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