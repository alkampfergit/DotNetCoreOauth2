//https://github.com/jstedfast/MailKit/blob/master/ExchangeOAuth2.md

using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Identity.Client;

var clientId = "<clientId>";
var tenantId = "<tenantId>";

//var options = new PublicClientApplicationOptions
//{
//    ClientId = "7dc85a6b-127b-45d2-ba1d-837f95aba0c7",
//    TenantId = "2aaa4b86-cc65-43c7-b348-89f4cb2b5c69",
//    RedirectUri = "https://picturestore.codewrecks.com/OAuth2/get-token"
//};

//var publicClientApplication = PublicClientApplicationBuilder
//    .CreateWithApplicationOptions(options)
//    .Build();

//var scopes = new string[] {
//    "email",
//    "offline_access",
//    "https://outlook.office.com/IMAP.AccessAsUser.All", // Only needed for IMAP
//    //"https://outlook.office.com/POP.AccessAsUser.All",  // Only needed for POP
//    //"https://outlook.office.com/SMTP.Send", // Only needed for SMTP
//};

//var authToken = await publicClientApplication.AcquireTokenInteractive(scopes).ExecuteAsync();

//var oauth2 = new SaslMechanismOAuth2(authToken.Account.Username, authToken.AccessToken);

//using (var client = new ImapClient())
//{
//    await client.ConnectAsync("outlook.office365.com", 993, SecureSocketOptions.SslOnConnect);
//    await client.AuthenticateAsync(oauth2);
//    await client.DisconnectAsync(true);
//}

var scopes = new string[] {
                //"email",
                //"offline_access",
	        "https://outlook.office365.com/.default",
                //"https://outlook.office.com/IMAP.AccessAsUser.All", // Only needed for IMAP
		//"https://outlook.office.com/POP.AccessAsUser.All",  // Only needed for POP
		//"https://outlook.office.com/SMTP.Send", // Only needed for SMTP
                //"https://graph.microsoft.com/.default",
            };

var confidentialClientApplication = ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithClientSecret("<client-secret>")
        .WithAuthority(new Uri("https://login.microsoftonline.com/" + tenantId + "/v2.0"))
        .Build();

var authenticationResult = await confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync();

var authToken = authenticationResult;
var oauth2 = new SaslMechanismOAuth2("<email address to access>", authToken.AccessToken);

using (var client = new ImapClient(new ProtocolLogger("imapLog.txt")))
{
    client.Connect("outlook.office365.com", 993, SecureSocketOptions.SslOnConnect);
    //client.AuthenticationMechanisms.Remove("XOAUTH2");
    client.Authenticate(oauth2);
    var inbox = client.Inbox;
    inbox.Open(MailKit.FolderAccess.ReadOnly);
    Console.WriteLine("Total messages: {0}", inbox.Count);
    Console.WriteLine("Recent messages: {0}", inbox.Recent);
    client.Disconnect(true);
}

Console.ReadKey();
