namespace DotNetCoreOAuth2
{
    public static class Base64UrlEncoder
    {
        /// <summary>
        /// https://base64.guru/standards/base64url
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Encode(byte[] data)
        {
            var base64Raw = Convert.ToBase64String(data);
            var trimmed = base64Raw.TrimEnd('='); //trim equal padding chars
            return trimmed
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
