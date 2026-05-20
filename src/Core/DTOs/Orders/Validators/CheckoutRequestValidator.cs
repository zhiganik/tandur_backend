using FluentValidation;

namespace Core.DTOs.Orders.Validators;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(i => i.MenuItemId).NotEmpty();
            line.RuleFor(i => i.Quantity).InclusiveBetween(1, 50);
        });
    }
}
