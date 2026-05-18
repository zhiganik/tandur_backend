using FluentValidation;

namespace Core.DTOs.Auth.Validators;

public class ChangePasswordWithOtpRequestValidator : AbstractValidator<ChangePasswordWithOtpRequest>
{
    public ChangePasswordWithOtpRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
