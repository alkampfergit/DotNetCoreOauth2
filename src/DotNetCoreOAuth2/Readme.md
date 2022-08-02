# Raw test authentication on IMAP

openssl s_client -connect imap-mail.outlook.com:993 -crlf

openssl s_client -connect outlook.office365.com:993 -crlf

A00001 CAPABILITY
A00002 AUTHENTICATE XOAUTH2 token

## Resources

[Base page on how to connect to imap](https://docs.microsoft.com/en-us/exchange/client-developer/legacy-protocols/how-to-authenticate-an-imap-pop-smtp-application-by-using-oauth)

## Various links

https://docs.microsoft.com/en-us/exchange/client-developer/legacy-protocols/how-to-authenticate-an-imap-pop-smtp-application-by-using-oauth

https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-device-code

https://docs.microsoft.com/en-us/samples/azure-samples/active-directory-dotnetcore-daemon-v2/ms-identity-daemon/

Questi sono gli esempi
https://docs.microsoft.com/en-us/azure/active-directory/develop/sample-v2-code
https://github.com/Azure-Samples/ms-identity-dotnet-advanced-token-cache/blob/master/1-Integrated-Cache/1-2-WebAPI-BgWorker/README.md


http://localhost:44351/api/oauth2/signin-oauth2

https://picturestore.codewrecks.com/api/tokens/get-token

https://github.com/MicrosoftDocs/office-developer-exchange-docs/issues/87
https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow

