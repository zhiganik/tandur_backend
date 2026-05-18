using FluentValidation;

namespace Core.DTOs.Me.Validators;

public class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequest>
{
    public UpdateMeRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        When(x => x.DateOfBirth.HasValue, () =>
        {
            RuleFor(x => x.DateOfBirth!.Value)
                .LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past.")
                .GreaterThan(DateTime.UtcNow.AddYears(-120)).WithMessage("Date of birth is unrealistic.");
        });
    }
}
