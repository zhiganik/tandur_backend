using FluentValidation;

namespace Core.DTOs.Auth.Validators;

public class VerifyEmailOtpRequestValidator : AbstractValidator<VerifyEmailOtpRequest>
{
    public VerifyEmailOtpRequestValidator()
    {
        RuleFor(x => x.SessionToken).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
