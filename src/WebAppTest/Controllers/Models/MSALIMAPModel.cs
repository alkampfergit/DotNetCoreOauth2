namespace WebAppTest.Controllers.Models
{
    public class MSALIMAPModel
    {
        public string State { get; set; } = string.Empty;
        
        public string LoginLink { get; set; } = string.Empty;

        public string DebugLoginLink { get; set; } = string.Empty;

        public string IdpResponseLink { get; set; } = string.Empty;

        public string RequestTokenData { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public bool MSALHasAccount { get; set; } = false;

        public string RefreshToken { get; set; } = string.Empty;

        public string IdToken { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;
        
        public string TestResult { get; set; } = string.Empty;
    }
}