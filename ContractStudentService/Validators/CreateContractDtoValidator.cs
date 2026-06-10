using ContractStudentService.DTOs.Contract;
using FluentValidation;

namespace ContractStudentService.Validators;

public class CreateContractDtoValidator : AbstractValidator<CreateContractDto>
{
    public CreateContractDtoValidator()
    {
        RuleFor(x => x.ContractCode)
            .NotEmpty()
            .WithMessage("Contract Code không được để trống.");

        RuleFor(x => x.StudentId)
            .GreaterThan(0)
            .WithMessage("StudentId không hợp lệ.");

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .WithMessage("RoomId không hợp lệ.");

        RuleFor(x => x.DepositAmount)
            .GreaterThan(0)
            .WithMessage("Deposit Amount phải lớn hơn 0.");

        RuleFor(x => x.MonthlyFee)
            .GreaterThan(0)
            .WithMessage("Monthly Fee phải lớn hơn 0.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End Date phải lớn hơn Start Date.");
    }
}