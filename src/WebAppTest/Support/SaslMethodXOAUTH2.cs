using MailKit.Security;
using System.Net;
using System.Text;

namespace WebAppTest.Support
{
    public class SaslMethodXOAUTH2 : SaslMechanism
    {
        const string AuthBearer = "auth=Bearer ";
        const string UserEquals = "user=";

        /// <summary>
        /// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismOAuth2"/> class.
        /// </summary>
        /// <remarks>
        /// Creates a new XOAUTH2 SASL context.
        /// </remarks>
        /// <param name="credentials">The user's credentials.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="credentials"/> is <c>null</c>.
        /// </exception>
        public SaslMethodXOAUTH2(NetworkCredential credentials) : base(credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismOAuth2"/> class.
        /// </summary>
        /// <remarks>
        /// Creates a new XOAUTH2 SASL context.
        /// </remarks>
        /// <example>
        /// <code language="c#" source="Examples\OAuth2GMailExample.cs"/>
        /// <code language="c#" source="Examples\OAuth2ExchangeExample.cs"/>
        /// </example>
        /// <param name="userName">The user name.</param>
        /// <param name="auth_token">The auth token.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <para><paramref name="userName"/> is <c>null</c>.</para>
        /// <para>-or-</para>
        /// <para><paramref name="auth_token"/> is <c>null</c>.</para>
        /// </exception>
        public SaslMethodXOAUTH2(string userName, string auth_token) : base(userName, auth_token)
        {
        }

        /// <summary>
        /// Get the name of the SASL mechanism.
        /// </summary>
        /// <remarks>
        /// Gets the name of the SASL mechanism.
        /// </remarks>
        /// <value>The name of the SASL mechanism.</value>
        public override string MechanismName
        {
            get { return "XOAUTH2"; }
        }

        /// <summary>
        /// Get whether or not the mechanism supports an initial response (SASL-IR).
        /// </summary>
        /// <remarks>
        /// <para>Gets whether or not the mechanism supports an initial response (SASL-IR).</para>
        /// <para>SASL mechanisms that support sending an initial client response to the server
        /// should return <value>true</value>.</para>
        /// </remarks>
        /// <value><c>true</c> if the mechanism supports an initial response; otherwise, <c>false</c>.</value>
        public override bool SupportsInitialResponse
        {
            get { return true; }
        }

        public String XOAUTH2 { get; private set; }

        /// <summary>
        /// Parse the server's challenge token and return the next challenge response.
        /// </summary>
        /// <remarks>
        /// Parses the server's challenge token and returns the next challenge response.
        /// </remarks>
        /// <returns>The next challenge response.</returns>
        /// <param name="token">The server's challenge token.</param>
        /// <param name="startIndex">The index into the token specifying where the server's challenge begins.</param>
        /// <param name="length">The length of the server's challenge.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.NotSupportedException">
        /// The SASL mechanism does not support SASL-IR.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The operation was canceled via the cancellation token.
        /// </exception>
        /// <exception cref="SaslException">
        /// An error has occurred while parsing the server's challenge token.
        /// </exception>
        protected override byte[] Challenge(byte[] token, int startIndex, int length, CancellationToken cancellationToken)
        {
            if (IsAuthenticated)
                return null;

            var authToken = Credentials.Password;
            var userName = Credentials.UserName;
            int index = 0;

            var buf = new byte[UserEquals.Length + userName.Length + AuthBearer.Length + authToken.Length + 3];
            for (int i = 0; i < UserEquals.Length; i++)
                buf[index++] = (byte)UserEquals[i];
            for (int i = 0; i < userName.Length; i++)
                buf[index++] = (byte)userName[i];
            buf[index++] = 1;
            for (int i = 0; i < AuthBearer.Length; i++)
                buf[index++] = (byte)AuthBearer[i];
            for (int i = 0; i < authToken.Length; i++)
                buf[index++] = (byte)authToken[i];
            buf[index++] = 1;
            buf[index++] = 1;

            IsAuthenticated = true;

            XOAUTH2 = Convert.ToBase64String(buf);

            return buf;
        }
    }
}