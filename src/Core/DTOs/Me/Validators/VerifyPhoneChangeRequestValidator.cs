using FluentValidation;

namespace Core.DTOs.Me.Validators;

public class VerifyPhoneChangeRequestValidator : AbstractValidator<VerifyPhoneChangeRequest>
{
    public VerifyPhoneChangeRequestValidator()
    {
        RuleFor(x => x.NewPhone).NotEmpty().Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Invalid phone number format.");
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
