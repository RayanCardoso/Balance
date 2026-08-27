using Balance.Application.AutoMapper;
using Balance.Application.UseCases.Accounts.GetAll;
using Balance.Application.UseCases.Accounts.Register;
using Balance.Application.UseCases.Dashboard.GetMonthly;
using Balance.Application.UseCases.Categories.GetAll;
using Balance.Application.UseCases.Categories.Register;
using Balance.Application.UseCases.Creditors.Archive;
using Balance.Application.UseCases.Creditors.GetAll;
using Balance.Application.UseCases.Creditors.GetSummary;
using Balance.Application.UseCases.Creditors.Register;
using Balance.Application.UseCases.Debts.Archive;
using Balance.Application.UseCases.Debts.GetAll;
using Balance.Application.UseCases.Debts.GetById;
using Balance.Application.UseCases.Debts.Register;
using Balance.Application.UseCases.Debts.RegisterPayment;
using Balance.Application.UseCases.Debts.UpdatePayment;
using Balance.Application.UseCases.Expenses.GetMonthly;
using Balance.Application.UseCases.Expenses.Register;
using Balance.Application.UseCases.Expenses.RegisterInstallmentPlan;
using Balance.Application.UseCases.Incomes.ChangeValue;
using Balance.Application.UseCases.Incomes.GetMonthly;
using Balance.Application.UseCases.Incomes.Register;
using Balance.Application.UseCases.Incomes.RegisterPayment;
using Balance.Application.UseCases.Login.DoLogin;
using Balance.Application.UseCases.People.GetAll;
using Balance.Application.UseCases.RecurringExpenses.Archive;
using Balance.Application.UseCases.RecurringExpenses.ChangeValue;
using Balance.Application.UseCases.RecurringExpenses.GetAll;
using Balance.Application.UseCases.RecurringExpenses.Register;
using Balance.Application.UseCases.RecurringExpenses.RegisterPayment;
using Balance.Application.UseCases.RecurringExpenses.UpdatePayment;
using Balance.Application.UseCases.People.Register;
using Balance.Application.UseCases.Users.Register;
using Microsoft.Extensions.DependencyInjection;

namespace Balance.Application;

public static class DependencyInjectionExtension
{
    public static void AddApplication(this IServiceCollection services)
    {
        AddUseCases(services);
        AddAutoMapper(services);
    }

    private static void AddAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapping>());
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IDoLoginUseCase, DoLoginUseCase>();
        services.AddScoped<IRegisterPersonUseCase, RegisterPersonUseCase>();
        services.AddScoped<IGetAllPeopleUseCase, GetAllPeopleUseCase>();
        services.AddScoped<IRegisterIncomeSourceUseCase, RegisterIncomeSourceUseCase>();
        services.AddScoped<IRegisterIncomePaymentUseCase, RegisterIncomePaymentUseCase>();
        services.AddScoped<IChangeIncomeSourceValueUseCase, ChangeIncomeSourceValueUseCase>();
        services.AddScoped<IGetMonthlyIncomeUseCase, GetMonthlyIncomeUseCase>();
        services.AddScoped<IRegisterCategoryUseCase, RegisterCategoryUseCase>();
        services.AddScoped<IGetAllCategoriesUseCase, GetAllCategoriesUseCase>();
        services.AddScoped<IRegisterCreditorUseCase, RegisterCreditorUseCase>();
        services.AddScoped<IGetAllCreditorsUseCase, GetAllCreditorsUseCase>();
        services.AddScoped<IArchiveCreditorUseCase, ArchiveCreditorUseCase>();
        services.AddScoped<IGetCreditorSummaryUseCase, GetCreditorSummaryUseCase>();
        services.AddScoped<IRegisterDebtUseCase, RegisterDebtUseCase>();
        services.AddScoped<IRegisterDebtPaymentUseCase, RegisterDebtPaymentUseCase>();
        services.AddScoped<IUpdateDebtPaymentUseCase, UpdateDebtPaymentUseCase>();
        services.AddScoped<IGetDebtByIdUseCase, GetDebtByIdUseCase>();
        services.AddScoped<IGetAllDebtsUseCase, GetAllDebtsUseCase>();
        services.AddScoped<IArchiveDebtUseCase, ArchiveDebtUseCase>();
        services.AddScoped<IRegisterAccountUseCase, RegisterAccountUseCase>();
        services.AddScoped<IGetAllAccountsUseCase, GetAllAccountsUseCase>();
        services.AddScoped<IRegisterExpenseUseCase, RegisterExpenseUseCase>();
        services.AddScoped<IGetMonthlyExpenseUseCase, GetMonthlyExpenseUseCase>();
        services.AddScoped<IGetMonthlyDashboardUseCase, GetMonthlyDashboardUseCase>();
        services.AddScoped<IRegisterInstallmentPlanUseCase, RegisterInstallmentPlanUseCase>();
        services.AddScoped<IRegisterRecurringExpenseUseCase, RegisterRecurringExpenseUseCase>();
        services.AddScoped<IGetAllRecurringExpensesUseCase, GetAllRecurringExpensesUseCase>();
        services.AddScoped<IChangeRecurringExpenseValueUseCase, ChangeRecurringExpenseValueUseCase>();
        services.AddScoped<IArchiveRecurringExpenseUseCase, ArchiveRecurringExpenseUseCase>();
        services.AddScoped<IRegisterRecurringExpensePaymentUseCase, RegisterRecurringExpensePaymentUseCase>();
        services.AddScoped<IUpdateRecurringExpensePaymentUseCase, UpdateRecurringExpensePaymentUseCase>();
    }
}
