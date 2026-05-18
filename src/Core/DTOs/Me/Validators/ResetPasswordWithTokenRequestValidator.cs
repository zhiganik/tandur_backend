using FluentValidation;

namespace Core.DTOs.Me.Validators;

public class ResetPasswordWithTokenRequestValidator : AbstractValidator<ResetPasswordWithTokenRequest>
{
    public ResetPasswordWithTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
