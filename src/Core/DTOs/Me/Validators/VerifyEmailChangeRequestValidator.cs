using FluentValidation;

namespace Core.DTOs.Me.Validators;

public class VerifyEmailChangeRequestValidator : AbstractValidator<VerifyEmailChangeRequest>
{
    public VerifyEmailChangeRequestValidator()
    {
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
