namespace WebAppTest.Controllers.Models
{
    public class SampleIMAPCodeFlowModel
    {
        public string State { get; set; }
        
        public string LoginLink { get; set; }

        public string DebugLoginLink { get; set; }

        public string IdpResponseLink { get; set; }

        public string RequestTokenData { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public string IdToken { get; set; }

        public string ImapResult { get; set; }
        
        public string SmtpResult { get; set; }
    }
}
