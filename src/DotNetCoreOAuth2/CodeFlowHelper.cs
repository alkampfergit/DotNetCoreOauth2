using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace DotNetCoreOAuth2
{
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc7636
    /// https://datatracker.ietf.org/doc/html/rfc6749#section-4.1.3
    /// </summary>
    public class CodeFlowHelper
    {
        private ConcurrentDictionary<string, RequestData> _requestData = new ConcurrentDictionary<string, RequestData>();

        private Timer _timer;

        public CodeFlowHelper()
        {
            _timer = new Timer(CleanUpCallback, null, 0, 1000 * 60 * 10);
        }

        private void CleanUpCallback(object state)
        {
            var expired = _requestData
                .Values
                .Where(v => v.IssueDateUtc < DateTime.UtcNow.AddMinutes(10))
                .Select(v => v.State)
                .ToList();
            foreach (var expiredState in expired)
            {
                Clear(expiredState);
            }
        }

        public RequestData GenerateNewRequestData()
        {
            var id = RandomGenerator.GenerateRandomString(64);
            var pkce = RandomGenerator.GenerateRandomString(64);

            var rd = new RequestData(id, pkce);

            _requestData[rd.State] = rd;

            return rd;
        }

        public RequestData? GetRequestData(string state)
        {
            if (_requestData.TryGetValue(state, out var requestData))
            {
                return requestData;
            }

            return null;
        }

        public void Clear(string state)
        {
            _requestData.TryRemove(state, out var _);
        }

        public record RequestData
        {
            public string State { get; }
            public string Pkce { get; }
            public string PkceHashed { get; }
            public string RedirectUrl { get; set; }
            public string Authority { get; set; }
            public string CustomState { get; set; }

            public DateTime IssueDateUtc { get; set; }

            public RequestData(string state, string pkce)
            {
                IssueDateUtc = DateTime.UtcNow;
                using var sha = SHA256.Create();

                State = state;
                Pkce = pkce;
                PkceHashed = Base64UrlEncoder.Encode(sha.ComputeHash(Encoding.ASCII.GetBytes(pkce)));
            }

            public void AddRequestData(string authority, string redirectUrl, string customState)
            {
                RedirectUrl = redirectUrl;
                Authority = authority;
                CustomState = customState;
            }
        }
    }
}
