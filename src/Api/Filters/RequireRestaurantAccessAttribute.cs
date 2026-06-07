using System.Security.Claims;
using Core.Domain.Constants;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRestaurantAccessAttribute(IRestaurantService restaurantService) : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.IsInRole(TandurRoles.SuperAdmin))
        {
            await next();
            return;
        }

        var adminId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!context.ActionArguments.TryGetValue("restaurantId", out var arg) || arg is not Guid restaurantId)
        {
            context.Result = new ForbidResult();
            return;
        }

        var summaries = await restaurantService.GetSummariesForAdminAsync(adminId);
        if (summaries.All(r => r.Id != restaurantId))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
