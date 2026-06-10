using ContractStudentService.DTOs.Student;
using FluentValidation;

namespace ContractStudentService.Validators;

public class UpdateStudentDtoValidator : AbstractValidator<UpdateStudentDto>
{
    public UpdateStudentDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.SchoolName)
            .NotEmpty();

        RuleFor(x => x.ClassName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.FacultyName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
