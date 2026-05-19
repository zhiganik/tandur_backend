using FluentValidation;

namespace Core.DTOs.Schedules.Validators;

public class TimeSlotRequestValidator : AbstractValidator<TimeSlotRequest>
{
    public TimeSlotRequestValidator()
    {
        RuleFor(x => x.From).LessThan(x => x.To)
            .WithMessage("Slot 'from' must be before 'to'.");
    }
}

public class UpdateScheduleDayRequestValidator : AbstractValidator<UpdateScheduleDayRequest>
{
    public UpdateScheduleDayRequestValidator()
    {
        RuleForEach(x => x.TimeSlots).SetValidator(new TimeSlotRequestValidator());
        RuleFor(x => x.TimeSlots).Must(NoOverlaps)
            .WithMessage("Time slots must not overlap.");
    }

    private static bool NoOverlaps(List<TimeSlotRequest> slots)
    {
        var sorted = slots.OrderBy(s => s.From).ToList();
        for (var i = 1; i < sorted.Count; i++)
            if (sorted[i].From < sorted[i - 1].To) return false;
        return true;
    }
}

public class UpdateFullScheduleRequestValidator : AbstractValidator<UpdateFullScheduleRequest>
{
    public UpdateFullScheduleRequestValidator()
    {
        RuleFor(x => x.Days).Must(d => d.Select(x => x.DayOfWeek).Distinct().Count() == 7)
            .WithMessage("All 7 days of the week must be included.");
        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleForEach(d => d.TimeSlots).SetValidator(new TimeSlotRequestValidator());
            day.RuleFor(d => d.TimeSlots).Must(NoOverlaps)
                .WithMessage("Time slots must not overlap.");
        });
    }

    private static bool NoOverlaps(List<TimeSlotRequest> slots)
    {
        var sorted = slots.OrderBy(s => s.From).ToList();
        for (var i = 1; i < sorted.Count; i++)
            if (sorted[i].From < sorted[i - 1].To) return false;
        return true;
    }
}

public class CreateOverrideRequestValidator : AbstractValidator<CreateOverrideRequest>
{
    public CreateOverrideRequestValidator()
    {
        RuleFor(x => x.Date).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Override date must be today or in the future.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.TimeSlots).SetValidator(new TimeSlotRequestValidator());
        RuleFor(x => x.TimeSlots).Must(NoOverlaps).WithMessage("Time slots must not overlap.");
    }

    private static bool NoOverlaps(List<TimeSlotRequest> slots)
    {
        var sorted = slots.OrderBy(s => s.From).ToList();
        for (var i = 1; i < sorted.Count; i++)
            if (sorted[i].From < sorted[i - 1].To) return false;
        return true;
    }
}

public class UpdateOverrideRequestValidator : AbstractValidator<UpdateOverrideRequest>
{
    public UpdateOverrideRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.TimeSlots).SetValidator(new TimeSlotRequestValidator());
        RuleFor(x => x.TimeSlots).Must(NoOverlaps).WithMessage("Time slots must not overlap.");
    }

    private static bool NoOverlaps(List<TimeSlotRequest> slots)
    {
        var sorted = slots.OrderBy(s => s.From).ToList();
        for (var i = 1; i < sorted.Count; i++)
            if (sorted[i].From < sorted[i - 1].To) return false;
        return true;
    }
}

public class InstantCloseRequestValidator : AbstractValidator<InstantCloseRequest>
{
    public InstantCloseRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.TimeSlots).SetValidator(new TimeSlotRequestValidator());
        RuleFor(x => x.TimeSlots).Must(NoOverlaps).WithMessage("Time slots must not overlap.");
    }

    private static bool NoOverlaps(List<TimeSlotRequest> slots)
    {
        var sorted = slots.OrderBy(s => s.From).ToList();
        for (var i = 1; i < sorted.Count; i++)
            if (sorted[i].From < sorted[i - 1].To) return false;
        return true;
    }
}
