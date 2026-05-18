using FluentValidation;

namespace Core.DTOs.Auth.Validators;

public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
