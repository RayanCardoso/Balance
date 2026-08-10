using System.Globalization;
using System.Resources;

namespace Balance.Exception;

public static class ResourceErrorMessages
{
    public static ResourceManager ResourceManager { get; } = new(
        baseName: "Balance.Exception.ResourceErrorMessages",
        assembly: typeof(ResourceErrorMessages).Assembly);

    public static string UNKNOWN_ERROR => Get(nameof(UNKNOWN_ERROR));

    public static string NAME_REQUIRED => Get(nameof(NAME_REQUIRED));
    public static string EMAIL_REQUIRED => Get(nameof(EMAIL_REQUIRED));
    public static string EMAIL_INVALID => Get(nameof(EMAIL_INVALID));
    public static string EMAIL_ALREADY_REGISTERED => Get(nameof(EMAIL_ALREADY_REGISTERED));
    public static string PASSWORD_REQUIRED => Get(nameof(PASSWORD_REQUIRED));
    public static string PASSWORD_TOO_SHORT => Get(nameof(PASSWORD_TOO_SHORT));
    public static string EMAIL_OR_PASSWORD_INVALID => Get(nameof(EMAIL_OR_PASSWORD_INVALID));

    private static string Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
