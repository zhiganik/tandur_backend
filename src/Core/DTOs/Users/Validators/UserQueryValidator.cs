using Core.Domain.Constants;
using FluentValidation;

namespace Core.DTOs.Users.Validators;

public class UserQueryValidator : AbstractValidator<UserQuery>
{
    private static readonly HashSet<string> ValidRoles =
        [TandurRoles.User, TandurRoles.Admin, TandurRoles.SuperAdmin];

    public UserQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        RuleForEach(x => x.Roles)
            .Must(r => ValidRoles.Contains(r))
            .WithMessage($"Each role must be one of: {string.Join(", ", ValidRoles)}.");
        RuleFor(x => x.Sort)
            .Must(s => s is "asc" or "desc")
            .WithMessage("Sort must be 'asc' or 'desc'.");
    }
}
