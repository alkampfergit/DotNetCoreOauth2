using Microsoft.Identity.Client;
using System.Security.Cryptography;

namespace WebAppTest.Support;

public static class EncryptedMsalTokenCacheHelper
{
    public static void EnableSerialization(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(args => BeforeAccessNotification(args));
        tokenCache.SetAfterAccess(args => AfterAccessNotification(args));
    }

    private static string GetFile()
    {
        return Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "msal_tokens_encrypted.bin");
    }

    private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
    {
        var tokenFile = GetFile();
        if (File.Exists(tokenFile))
        {
            var encryptedContent = File.ReadAllBytes(tokenFile);
            var decryptedContent = ProtectedData.Unprotect(
                encryptedContent, 
                null, 
                DataProtectionScope.CurrentUser);
            args.TokenCache.DeserializeMsalV3(decryptedContent);
        }
    }

    private static void AfterAccessNotification(TokenCacheNotificationArgs args)
    {
        if (args.HasStateChanged)
        {
            var tokenFile = GetFile();
            var data = args.TokenCache.SerializeMsalV3();
            var encryptedData = ProtectedData.Protect(
                data, 
                null, 
                DataProtectionScope.CurrentUser);
            
            using var ms = new FileStream(tokenFile, FileMode.OpenOrCreate, FileAccess.Write);
            ms.SetLength(0);
            ms.Write(encryptedData, 0, encryptedData.Length);
        }
    }
}