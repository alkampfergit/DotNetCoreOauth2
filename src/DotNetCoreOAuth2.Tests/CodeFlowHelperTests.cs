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

        [Test]
        public void Can_request_by_state()
        {
            var original = _sut.GenerateNewRequestData();
            var requestData = _sut.GetRequestData(original.State)!;

            Assert.That(requestData.Pkce.Length, Is.EqualTo(64));
            Assert.That(requestData.PkceHashed.Length, Is.EqualTo(43));
        }

        [Test]
        public void Can_clear_state()
        {
            var original = _sut.GenerateNewRequestData();
            _sut.Clear(original.State);
            Assert.IsNull(_sut.GetRequestData(original.State));
        }
    }
}
