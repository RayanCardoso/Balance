using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Debts.GetMonthly;

public interface IGetMonthlyDebtUseCase
{
    Task<ResponseMonthlyDebtJson> Execute(int year, int month);
}
