using System.Globalization;
using FluentValidation;

namespace Core.DTOs.Restaurants.Validators;

public class CreateRestaurantRequestValidator : AbstractValidator<CreateRestaurantRequest>
{
    public CreateRestaurantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Must(CurrencyValidation.IsValidIso4217)
            .WithMessage("'{PropertyName}' must be a valid ISO 4217 currency code (e.g. USD, EUR, KZT).");
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(100);
    }
}

public class UpdateRestaurantRequestValidator : AbstractValidator<UpdateRestaurantRequest>
{
    public UpdateRestaurantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Must(CurrencyValidation.IsValidIso4217)
            .WithMessage("'{PropertyName}' must be a valid ISO 4217 currency code (e.g. USD, EUR, KZT).");
    }
}

file static class CurrencyValidation
{
    private static readonly HashSet<string> ValidCodes = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(c => { try { return new RegionInfo(c.Name).ISOCurrencySymbol; } catch { return null; } })
        .Where(c => c is not null)
        .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

    internal static bool IsValidIso4217(string? code) =>
        code is not null && ValidCodes.Contains(code);
}