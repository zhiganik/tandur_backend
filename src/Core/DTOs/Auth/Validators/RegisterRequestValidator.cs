using FluentValidation;

namespace Core.DTOs.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.SessionToken).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2);
    }
}
