using ContractStudentService.DTOs.Contract;
using FluentValidation;

namespace ContractStudentService.Validators;

public class UpdateContractDtoValidator : AbstractValidator<UpdateContractDto>
{
    public UpdateContractDtoValidator()
    {
        RuleFor(x => x.ContractCode)
            .NotEmpty();

        RuleFor(x => x.StudentId)
            .GreaterThan(0);

        RuleFor(x => x.RoomId)
            .GreaterThan(0);

        RuleFor(x => x.DepositAmount)
            .GreaterThan(0);

        RuleFor(x => x.MonthlyFee)
            .GreaterThan(0);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate);
    }
}