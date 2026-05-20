using FluentValidation;

namespace Core.DTOs.Orders.Validators;

public class OrderQueryValidator : AbstractValidator<OrderQuery>
{
    public OrderQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.MinTotal).GreaterThanOrEqualTo(0).When(x => x.MinTotal.HasValue);
        RuleFor(x => x.MaxTotal).GreaterThanOrEqualTo(0).When(x => x.MaxTotal.HasValue);
        RuleFor(x => x)
            .Must(x => x.MinTotal is null || x.MaxTotal is null || x.MinTotal <= x.MaxTotal)
            .WithMessage("MinTotal must be less than or equal to MaxTotal.");
        RuleFor(x => x.Sort)
            .Must(s => s is "asc" or "desc")
            .WithMessage("Sort must be 'asc' or 'desc'.");
    }
}
