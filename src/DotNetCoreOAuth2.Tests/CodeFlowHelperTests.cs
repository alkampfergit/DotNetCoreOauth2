namespace DotNetCoreOAuth2.Tests
{
    [TestFixture]
    public class CodeFlowHelperTests
    {
        private CodeFlowHelper _sut;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _sut = new CodeFlowHelper();
        }

        [Test]
        public void Can_generate_pkce()
        {
            var requestData = _sut.GenerateNewRequestData();
            Assert.That(requestData.Pkce.Length, Is.EqualTo(64));
            Assert.That(requestData.PkceHashed.Length, Is.EqualTo(43));
        }
    }
}
