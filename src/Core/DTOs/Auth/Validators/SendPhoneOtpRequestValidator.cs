using FluentValidation;

namespace Core.DTOs.Auth.Validators;

public class SendPhoneOtpRequestValidator : AbstractValidator<SendPhoneOtpRequest>
{
    public SendPhoneOtpRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{6,14}$");
    }
}
