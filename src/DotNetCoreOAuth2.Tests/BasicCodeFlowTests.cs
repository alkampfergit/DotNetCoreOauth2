using Microsoft.AspNetCore.WebUtilities;
using NSubstitute;
using System.Net;

namespace DotNetCoreOAuth2.Tests
{
    public class Tests
    {
        private CodeFlowHelper _cfh;
        private OAuth2Client _sut;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _cfh = new CodeFlowHelper();
            var wkch = new WellKnownConfigurationHandler(Substitute.For<IHttpClientFactory>(), "test");
            wkch.AddKnownServices("https://foo.com", "https://foo.com/authorize", "https://foo.co/token");
            _sut = new OAuth2Client(_cfh, wkch, "aaaa", "https://foo.com", "https://my-app.com/get-token");
        }

        [Test]
        public async Task Basic_url_generation()
        {
            var url = await _sut.GenerateUrlForCodeFlowAsync("offline_access test", null);
            Assert.That(url.AbsoluteUri.Contains("client_id=aaaa"));
            Assert.That(url.AbsoluteUri.Contains("scope=offline_access%20test"));
            Assert.That(url.AbsoluteUri.Contains($"redirect_uri={WebUtility.UrlEncode("https://my-app.com/get-token")}"));
        }

        [Test]
        public async Task Uses_well_known_configuration_handler()
        {
            var url = await _sut.GenerateUrlForCodeFlowAsync("offline_access test", null);
            Assert.That(url.AbsoluteUri.StartsWith("https://foo.com/authorize"));
        }

        [Test]
        public async Task Verify_base_state_generation()
        {
            var url = await _sut.GenerateUrlForCodeFlowAsync("offline_access test", null);
            var parameters = QueryHelpers.ParseQuery(url.Query);
            var state = parameters["state"].Single();
            var requestData = _cfh.GetRequestData(state)!;

            Assert.That(url.AbsoluteUri.Contains($"state={requestData.State}"));
        }

        [Test]
        public async Task Verify_Pkce()
        {
            var url = await _sut.GenerateUrlForCodeFlowAsync("offline_access test", null);
            Assert.That(url.AbsoluteUri.Contains("code_challenge_method=S256"));

            var parameters = QueryHelpers.ParseQuery(url.Query);
            var state = parameters["state"].Single();
            var requestData = _cfh.GetRequestData(state)!;

            int codeChallenge = parameters["code_challenge"].Single().Length;
            Assert.That(codeChallenge, Is.EqualTo(43));
            Assert.That(url.AbsoluteUri.Contains($"code_challenge={WebUtility.UrlEncode(requestData.PkceHashed)}"));
        }

        [Test]
        public async Task State_managed_correctly()
        {
            var url = await _sut.GenerateUrlForCodeFlowAsync("offline_access test", null);
            Assert.That(url.AbsoluteUri.Contains("code_challenge_method=S256"));

            var parameters = QueryHelpers.ParseQuery(url.Query);
            var state = parameters["state"].Single();
            var requestData = _cfh.GetRequestData(state)!;

            Assert.That(requestData.RedirectUrl, Is.EqualTo(parameters["redirect_uri"]));
            Assert.That(requestData.Authority, Is.EqualTo("https://foo.com"));
        }

        [Test]
        public async Task Generate_request_for_token_base() 
        {
            var url = await _sut.GenerateUrlForCodeFlowAsync("offline_access test", null);
            Assert.That(url.AbsoluteUri.Contains("code_challenge_method=S256"));

            var parameters = QueryHelpers.ParseQuery(url.Query);
            var state = parameters["state"].Single();
            var requestData = _cfh.GetRequestData(state)!;

            var request = await _sut.GenerateTokenRequestAsync(
                $"?code=0.AS8AhkuqKmXMx0OzSIn0yytcaah02mKoF4ZNn16aI1UYTReUAP4.AgABAAIAAAD--DLA3VO7QrddgJg7WevrAgDs_wQA9P9vsedBSblHPaaaKuly8Tqo2tKeOFbPnQG4rIX3OioDIn9Ge-NgoPxpRsIRVAxEKxX3c2ErJgldAOtyOuwvc9X384DtZaIMiLe4z6MZVJ4KxzlHRvBAyTOXoqatEfnaF9x5XhvVvnx612Hj9JUwLW12VaLuIo7sv45m4un84Tltrgjf0LeQqUOwFPlVsxTpUY96roah8RSQMXRjMg0ccpwWwXeSHHH1546rbYO-brTsnS3wGBzrzfqJThj6Pmkqz3yuCKpgrX43GszfhGKpYMapkXFblVVPh5Ay_9uMbXk-P9wAPMNHvnh9s4U-sAIbkS0iZBRvoiIw6MrOsy5-CItvgBbBT4hlPPHiSJT6ML-17FN2wazJbXsCePWvLRD-gMzESD_eIZzGeOsXBI81WNd7EBek8RNMEDBA7bMn4mVT5oTrdOFZpFzHHQeyVeHCdVyLez8RxNe_YjvqTjOqbyO_EwSCb8scNAR5vhemZtOfb9cDSk2W1xkqbueIx6Sp1vrSyLPJ-YRdEf9KXRWpq7q9KsIFTtxlg9yjSOaDTBjxlyZpu9tgVqECjLNRb9QSfmU1ijJ8iRqqpRwp2ujTyQum1hR3L1w1bnfC6l9nsE7UNarciy8z8IbX2pv_nehB1g3AKA9NXuZw1bCwo-ttj_pd0nqif-_uqTzZFrBRJG4tnDPpvtVOO1rCX1-BeufNvP8U-YrAPLYm_TaMaOi45i0FntphG2mhdWbwuotnMNo-RXe7KO9_GgQwnIIz&state={state}&session_state=ace9f8b3-b3a3-4304-a18c-7fe0c0f7d69e",
                "secret");
            
            Assert.That(request, Is.Not.Null);
            var realContent = await request.Content!.ReadAsStringAsync();
            Assert.That(realContent.Contains("client_id=aaaa"));
            Assert.That(realContent.Contains("client_secret=secret"));
            Assert.That(realContent.Contains($"code_verifier={requestData.Pkce}"));
            Assert.That(realContent.Contains($"redirect_uri={WebUtility.UrlEncode(requestData.RedirectUrl)}"));
        }
    }
}