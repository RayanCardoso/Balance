using System.Globalization;
using Balance.Application.UseCases.Debts.Register;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.Debts;

public class RegisterDebtValidatorTest
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly CultureInfo PtBr = new("pt-BR");

    private static void WithCulture(CultureInfo culture, Action action)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;

        try
        {
            action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void Success_Scheduled()
    {
        var result = new RegisterDebtValidator().Validate(RequestRegisterDebtJsonBuilder.BuildScheduled());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_OpenEnded()
    {
        var result = new RegisterDebtValidator().Validate(RequestRegisterDebtJsonBuilder.BuildOpenEnded());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Total_Amount_Equals_Principal_Amount()
    {
        var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
        request.PrincipalAmount = 1500;
        request.TotalAmount = 1500;

        var result = new RegisterDebtValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Name_Empty_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.Name = string.Empty;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "Name is required.");
        });
    }

    [Fact]
    public void Error_Name_Empty_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.Name = string.Empty;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "O nome é obrigatório.");
        });
    }

    [Fact]
    public void Error_Principal_Amount_Not_Greater_Than_Zero_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.PrincipalAmount = 0;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "The amount must be greater than zero.");
        });
    }

    [Fact]
    public void Error_Principal_Amount_Not_Greater_Than_Zero_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.PrincipalAmount = 0;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "O valor deve ser maior que zero.");
        });
    }

    [Fact]
    public void Error_Total_Amount_Not_Greater_Than_Zero_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.PrincipalAmount = 0;
            request.TotalAmount = 0;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "The amount must be greater than zero.");
        });
    }

    [Fact]
    public void Error_Total_Amount_Not_Greater_Than_Zero_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.PrincipalAmount = 0;
            request.TotalAmount = 0;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "O valor deve ser maior que zero.");
        });
    }

    [Fact]
    public void Error_Total_Less_Than_Principal_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.PrincipalAmount = 1000;
            request.TotalAmount = 999.99m;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage == "The total amount must not be less than the principal.");
        });
    }

    [Fact]
    public void Error_Total_Less_Than_Principal_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.PrincipalAmount = 1000;
            request.TotalAmount = 999.99m;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage == "O valor total não pode ser menor que o principal.");
        });
    }

    [Fact]
    public void Error_Scheduled_Missing_Schedule_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.InstallmentCount = null;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage == "A scheduled debt requires a due day and a number of installments.");
        });
    }

    [Fact]
    public void Error_Scheduled_Missing_Schedule_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.DueDay = null;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage == "Uma dívida programada exige um dia de vencimento e um número de parcelas.");
        });
    }

    [Fact]
    public void Error_Scheduled_Installment_Count_Invalid_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.InstallmentCount = 0;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage == "The number of installments must be at least 2.");
        });
    }

    [Fact]
    public void Error_Scheduled_Installment_Count_Invalid_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.InstallmentCount = 0;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage == "O número de parcelas deve ser ao menos 2.");
        });
    }

    [Fact]
    public void Error_Scheduled_Due_Day_Out_Of_Range_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.DueDay = 32;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "The day must be between 1 and 31.");
        });
    }

    [Fact]
    public void Error_Scheduled_Due_Day_Out_Of_Range_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.DueDay = 0;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "O dia deve estar entre 1 e 31.");
        });
    }

    [Fact]
    public void Error_OpenEnded_Schedule_Not_Allowed_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildOpenEnded();
            request.DueDay = 10;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage == "An open-ended debt must not have a due day or a number of installments.");
        });
    }

    [Fact]
    public void Error_OpenEnded_Schedule_Not_Allowed_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtJsonBuilder.BuildOpenEnded();
            request.InstallmentCount = 5;

            var result = new RegisterDebtValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage ==
                "Uma dívida sem prazo não deve ter dia de vencimento nem número de parcelas.");
        });
    }
}
