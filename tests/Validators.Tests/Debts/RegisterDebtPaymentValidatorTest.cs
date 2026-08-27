using System.Globalization;
using Balance.Application.UseCases.Debts.RegisterPayment;
using Balance.Communication.Enums;
using CommonTestUtilities.Requests;
using Shouldly;

namespace Validators.Tests.Debts;

public class RegisterDebtPaymentValidatorTest
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
    public void Success()
    {
        var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());

        var result = new RegisterDebtPaymentValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Error_Amount_Zero_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());
            request.AmountPaid = 0;

            var result = new RegisterDebtPaymentValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "The amount must be greater than zero.");
        });
    }

    [Fact]
    public void Error_Amount_Zero_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());
            request.AmountPaid = 0;

            var result = new RegisterDebtPaymentValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "O valor deve ser maior que zero.");
        });
    }

    [Fact]
    public void Error_Amount_Negative_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());
            request.AmountPaid = -100;

            var result = new RegisterDebtPaymentValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "The amount must be greater than zero.");
        });
    }

    [Fact]
    public void Error_Amount_Negative_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());
            request.AmountPaid = -100;

            var result = new RegisterDebtPaymentValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "O valor deve ser maior que zero.");
        });
    }

    [Fact]
    public void Error_Credit_Without_Account_Invariant_Culture()
    {
        WithCulture(Invariant, () =>
        {
            var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());
            request.Type = ExpenseType.Credit;
            request.AccountId = null;

            var result = new RegisterDebtPaymentValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error => error.ErrorMessage == "An account is required for a credit expense.");
        });
    }

    [Fact]
    public void Error_Credit_Without_Account_PtBr_Culture()
    {
        WithCulture(PtBr, () =>
        {
            var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());
            request.Type = ExpenseType.Credit;
            request.AccountId = null;

            var result = new RegisterDebtPaymentValidator().Validate(request);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(error =>
                error.ErrorMessage == "Escolha a conta ou cartão de uma despesa no crédito.");
        });
    }

    [Fact]
    public void Success_Debit_Without_Account()
    {
        var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());
        request.Type = ExpenseType.Debit;
        request.AccountId = null;

        var result = new RegisterDebtPaymentValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Success_Pix_Without_Account()
    {
        var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());
        request.Type = ExpenseType.Pix;
        request.AccountId = null;

        var result = new RegisterDebtPaymentValidator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }
}
