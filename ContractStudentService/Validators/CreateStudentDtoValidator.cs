using ContractStudentService.DTOs.Student;
using FluentValidation;

namespace ContractStudentService.Validators;

public class CreateStudentDtoValidator : AbstractValidator<CreateStudentDto>
{
    public CreateStudentDtoValidator()
    {
        RuleFor(x => x.StudentCode)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.CCCD)
            .NotEmpty()
            .Length(12);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.ClassName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.FacultyName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ResidenceHistory)
            .MaximumLength(1000);
    }
}
