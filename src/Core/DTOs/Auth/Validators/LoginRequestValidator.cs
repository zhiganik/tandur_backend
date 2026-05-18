using FluentValidation;

namespace Core.DTOs.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.SessionToken).NotEmpty();
    }
}
