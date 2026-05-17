using FluentValidation;

namespace Core.DTOs.Auth.Validators;

public class SendEmailOtpRequestValidator : AbstractValidator<SendEmailOtpRequest>
{
    public SendEmailOtpRequestValidator()
    {
        RuleFor(x => x.SessionToken).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
