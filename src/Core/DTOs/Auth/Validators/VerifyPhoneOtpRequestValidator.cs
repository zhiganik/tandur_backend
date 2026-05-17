using FluentValidation;

namespace Core.DTOs.Auth.Validators;

public class VerifyPhoneOtpRequestValidator : AbstractValidator<VerifyPhoneOtpRequest>
{
    public VerifyPhoneOtpRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{6,14}$");
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
