# DotNetCoreOauth2

## Library to help implementing OAuth2 authentication in .NET Core

## WebAppTest 

This application is created to help use the library and to test various scenarios for OAUTH2, like accessing an IMAP Office 365 folder using XOAUTH2.

To use the application you need to configure the OAuth2 section in appsettings.json file specifying Authority client id and secret, or you can put the very same information in a file called **DotNetCoreOauth2.json that is located in one of the parent folder of the project**. This technique allow you to avoid including sensitive information in the source code of the project.

### Office 365 IMAP scenario

Create the application in Azure, then navigate to **https://{urlofapplication}/Oauth2/trigger-login** and the app will generate the URL to trigger a code flow to get the access token. You can put a breakpoint in the GetToken method if you want to debug **how the server ask for a real token from the code received in querystring**.

The application is really simple, it **stores in memory the latest token received**, and if you want to check if that token can be used to access your IMAP folder, you can navigate to **https://{urlofapplication}/Oauth2/office-365-get-mail?emailAddress=your-email-address**. If you do not get an error MimeKit was able to autenticate, and you can use the token received to try to authenticate directly with the IMAP server with openssl.



