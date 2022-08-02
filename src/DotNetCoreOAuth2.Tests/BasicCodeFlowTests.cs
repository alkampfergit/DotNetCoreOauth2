using Microsoft.AspNetCore.WebUtilities;
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
            _sut = new OAuth2Client(_cfh, "aaaa", "https://foo.com", "https://my-app.com/get-token");
        }

        [Test]
        public void Basic_url_generation()
        {
            var url = _sut.GenerateUrlForCodeFlow("offline_access test", null);
            Assert.That(url.AbsoluteUri.Contains("client_id=aaaa"));
            Assert.That(url.AbsoluteUri.Contains("scope=offline_access%20test"));
            Assert.That(url.AbsoluteUri.Contains($"redirect_uri={WebUtility.UrlEncode("https://my-app.com/get-token")}"));
        }

        [Test]
        public void Verify_base_state_generation()
        {
            var url = _sut.GenerateUrlForCodeFlow("offline_access test", null);
            var parameters = QueryHelpers.ParseQuery(url.Query);
            var state = parameters["state"].Single();
            var requestData = _cfh.GetRequestData(state)!;

            Assert.That(url.AbsoluteUri.Contains($"state={requestData.State}"));
        }

        [Test]
        public void Verify_Pkce()
        {
            var url = _sut.GenerateUrlForCodeFlow("offline_access test", null);
            Assert.That(url.AbsoluteUri.Contains("code_challenge_method=S256"));

            var parameters = QueryHelpers.ParseQuery(url.Query);
            var state = parameters["state"].Single();
            var requestData = _cfh.GetRequestData(state)!;

            int codeChallenge = parameters["code_challenge"].Single().Length;
            Assert.That(codeChallenge, Is.EqualTo(43));
            Assert.That(url.AbsoluteUri.Contains($"code_challenge={WebUtility.UrlEncode(requestData.PkceHashed)}"));
        }
    }
}