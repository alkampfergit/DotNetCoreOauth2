using System.Security.Cryptography;
using System.Text;

namespace DotNetCoreOAuth2
{
    internal static class RandomGenerator
    {
        private static StringBuilder charPool;

        static RandomGenerator()
        {
            charPool = new StringBuilder();
            for (char i = 'a'; i <= 'z'; i++)
            {
                charPool.Append(i);
                charPool.Append(char.ToUpper(i));
            }

            for (char i = '0'; i <= '9'; i++)
            {
                charPool.Append(i);
            }

            charPool.Append("_-.~");
        }

        public static string GenerateRandomString(int stringLength)
        {
            StringBuilder password = new StringBuilder();
            for (int i = 0; i < stringLength; i++)
            {
                password.Append(charPool[RandomNumberGenerator.GetInt32(charPool.Length)]);
            }
            return password.ToString();
        }
    }
}
