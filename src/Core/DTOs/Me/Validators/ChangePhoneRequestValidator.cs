using FluentValidation;

namespace Core.DTOs.Me.Validators;

public class ChangePhoneRequestValidator : AbstractValidator<ChangePhoneRequest>
{
    public ChangePhoneRequestValidator()
    {
        RuleFor(x => x.NewPhone).NotEmpty().Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Invalid phone number format.");
    }
}
