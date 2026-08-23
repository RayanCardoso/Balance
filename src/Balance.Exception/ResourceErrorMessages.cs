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

    public static string PERSON_NOT_FOUND => Get(nameof(PERSON_NOT_FOUND));
    public static string AMOUNT_GREATER_THAN_ZERO => Get(nameof(AMOUNT_GREATER_THAN_ZERO));
    public static string ACCOUNT_REQUIRED_FOR_CREDIT => Get(nameof(ACCOUNT_REQUIRED_FOR_CREDIT));
    public static string EXPECTED_DAY_OUT_OF_RANGE => Get(nameof(EXPECTED_DAY_OUT_OF_RANGE));
    public static string VARIABLE_SOURCE_HAS_NO_VERSION => Get(nameof(VARIABLE_SOURCE_HAS_NO_VERSION));
    public static string INCOME_SOURCE_NOT_FOUND => Get(nameof(INCOME_SOURCE_NOT_FOUND));
    public static string INCOME_SOURCE_ARCHIVED => Get(nameof(INCOME_SOURCE_ARCHIVED));
    public static string NO_VERSION_IN_EFFECT => Get(nameof(NO_VERSION_IN_EFFECT));
    public static string CHANGE_REASON_REQUIRED => Get(nameof(CHANGE_REASON_REQUIRED));
    public static string VALIDITY_START_MUST_BE_LATER => Get(nameof(VALIDITY_START_MUST_BE_LATER));
    public static string REFERENCE_MONTH_INVALID => Get(nameof(REFERENCE_MONTH_INVALID));

    public static string DAY_OUT_OF_RANGE => Get(nameof(DAY_OUT_OF_RANGE));
    public static string INSTALLMENT_COUNT_INVALID => Get(nameof(INSTALLMENT_COUNT_INVALID));
    public static string PAYMENT_ALREADY_RECORDED => Get(nameof(PAYMENT_ALREADY_RECORDED));
    public static string RECURRING_EXPENSE_ARCHIVED => Get(nameof(RECURRING_EXPENSE_ARCHIVED));
    public static string CATEGORY_NOT_FOUND => Get(nameof(CATEGORY_NOT_FOUND));
    public static string ACCOUNT_NOT_FOUND => Get(nameof(ACCOUNT_NOT_FOUND));
    public static string RECURRING_EXPENSE_NOT_FOUND => Get(nameof(RECURRING_EXPENSE_NOT_FOUND));
    public static string RECURRING_EXPENSE_PAYMENT_NOT_FOUND => Get(nameof(RECURRING_EXPENSE_PAYMENT_NOT_FOUND));

    private static string Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
