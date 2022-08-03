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
            var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[stringLength];
            rng.GetBytes(bytes);
            StringBuilder password = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                password.Append(charPool[bytes[i] % charPool.Length]);
            }
            return password.ToString();
        }
    }
}
