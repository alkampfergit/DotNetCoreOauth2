using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace DotNetCoreOAuth2
{
    public class CodeFlowHelper
    {
        private ConcurrentDictionary<string, RequestData> _requestData = new ConcurrentDictionary<string, RequestData>();

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

        public record RequestData
        {
            public string State { get; init; }
            public string Pkce { get; init; }
            public string PkceHashed { get; init; }

            public RequestData(string state, string pkce)
            {
                State = state;
                Pkce = pkce;
                PkceHashed = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(pkce))).TrimEnd('=');
            }
        }
    }
}
