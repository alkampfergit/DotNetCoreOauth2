using Microsoft.Identity.Client;

namespace WebAppTest.Support;

public static class MsalTokenCacheHelper
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
            "msal_tokens.json");
    }

    private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
    {
        var tokenFile = GetFile();
        if (File.Exists(tokenFile))
        {
            var content = File.ReadAllBytes(tokenFile);
            args.TokenCache.DeserializeMsalV3(content);
        }
    }

    private static void AfterAccessNotification(TokenCacheNotificationArgs args)
    {
        if (args.HasStateChanged)
        {
            var tokenFile = GetFile();
            var data = args.TokenCache.SerializeMsalV3();
            using var ms = new FileStream(tokenFile, FileMode.OpenOrCreate, FileAccess.Write);
            ms.SetLength(0); // Clear the file before writing
            ms.Write(data, 0, data.Length);
        }
    }
}
