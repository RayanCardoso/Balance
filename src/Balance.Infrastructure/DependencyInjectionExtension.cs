using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Accounts;
using Balance.Domain.Repositories.Categories;
using Balance.Domain.Repositories.Creditors;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Repositories.Expenses;
using Balance.Domain.Repositories.Incomes;
using Balance.Domain.Repositories.People;
using Balance.Domain.Repositories.RecurringExpenses;
using Balance.Domain.Repositories.Users;
using Balance.Domain.Security.Cryptography;
using Balance.Domain.Security.Tokens;
using Balance.Domain.Services.LoggedUser;
using Balance.Infrastructure.DataAccess;
using Balance.Infrastructure.DataAccess.Repositories.Accounts;
using Balance.Infrastructure.DataAccess.Repositories.Categories;
using Balance.Infrastructure.DataAccess.Repositories.Creditors;
using Balance.Infrastructure.DataAccess.Repositories.Debts;
using Balance.Infrastructure.DataAccess.Repositories.Expenses;
using Balance.Infrastructure.DataAccess.Repositories.Incomes;
using Balance.Infrastructure.DataAccess.Repositories.People;
using Balance.Infrastructure.DataAccess.Repositories.RecurringExpenses;
using Balance.Infrastructure.DataAccess.Repositories.Users;
using Balance.Infrastructure.Extensions;
using Balance.Infrastructure.Security.Cryptography;
using Balance.Infrastructure.Security.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Balance.Infrastructure;

public static class DependencyInjectionExtension
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPasswordEncripter, PasswordEncripter>();
        services.AddScoped<ILoggedUser, Services.LoggedUser.LoggedUser>();

        AddToken(services, configuration);

        AddRepositories(services);

        if (configuration.IsTestEnvironment() == false)
        {
            AddDbContext(services, configuration);
        }
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserReadOnlyRepository, UserRepository>();
        services.AddScoped<IUserWriteOnlyRepository, UserRepository>();
        services.AddScoped<IPersonReadOnlyRepository, PersonRepository>();
        services.AddScoped<IPersonWriteOnlyRepository, PersonRepository>();
        services.AddScoped<IIncomeSourceReadOnlyRepository, IncomeSourceRepository>();
        services.AddScoped<IIncomeSourceWriteOnlyRepository, IncomeSourceRepository>();
        services.AddScoped<IIncomeSourceUpdateOnlyRepository, IncomeSourceRepository>();
        services.AddScoped<IIncomePaymentWriteOnlyRepository, IncomePaymentRepository>();
        services.AddScoped<ICategoryReadOnlyRepository, CategoryRepository>();
        services.AddScoped<ICategoryWriteOnlyRepository, CategoryRepository>();
        services.AddScoped<ICreditorReadOnlyRepository, CreditorRepository>();
        services.AddScoped<ICreditorWriteOnlyRepository, CreditorRepository>();
        services.AddScoped<ICreditorUpdateOnlyRepository, CreditorRepository>();
        services.AddScoped<IDebtReadOnlyRepository, DebtRepository>();
        services.AddScoped<IDebtWriteOnlyRepository, DebtRepository>();
        services.AddScoped<IDebtUpdateOnlyRepository, DebtRepository>();
        services.AddScoped<IDebtInstallmentWriteOnlyRepository, DebtRepository>();
        services.AddScoped<IDebtPaymentRepository, DebtPaymentRepository>();
        services.AddScoped<IAccountReadOnlyRepository, AccountRepository>();
        services.AddScoped<IAccountWriteOnlyRepository, AccountRepository>();
        services.AddScoped<IExpenseReadOnlyRepository, ExpenseRepository>();
        services.AddScoped<IExpenseWriteOnlyRepository, ExpenseRepository>();
        services.AddScoped<IInstallmentPlanWriteOnlyRepository, InstallmentPlanRepository>();
        services.AddScoped<IRecurringExpenseReadOnlyRepository, RecurringExpenseRepository>();
        services.AddScoped<IRecurringExpenseWriteOnlyRepository, RecurringExpenseRepository>();
        services.AddScoped<IRecurringExpenseUpdateOnlyRepository, RecurringExpenseRepository>();
        services.AddScoped<IRecurringExpensePaymentRepository, RecurringExpensePaymentRepository>();
    }

    private static void AddToken(IServiceCollection services, IConfiguration configuration)
    {
        var expirationTimeMinutes = configuration.GetValue<uint>("Settings:Jwt:ExpiresMinutes");
        var signingKey = configuration.GetValue<string>("Settings:Jwt:SigningKey");

        services.AddScoped<IAccessTokenGenerator>(_ => new JwtTokenGenerator(expirationTimeMinutes, signingKey!));
    }

    private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Connection");

        services.AddDbContext<BalanceDbContext>(config => config.UseNpgsql(connectionString));
    }
}
