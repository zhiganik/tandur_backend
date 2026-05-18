using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Swagger;

public class TagOrderDocumentFilter : IDocumentFilter
{
    private static readonly Dictionary<string, string> Tags = new()
    {
        ["Auth › Session"]        = "Anonymous OTP session flow — phone and email verification before login or register",
        ["Auth"]                  = "Client authentication — login, register, token refresh",
        ["Me"]                    = "Current user profile — shared by mobile and admin users",
        ["Restaurants"]           = "Browse restaurants (mobile, read-only)",
        ["Categories"]            = "Browse menu categories (mobile, read-only)",
        ["Menu"]                  = "Browse menu items (mobile, read-only)",
        ["Admin › Auth"]          = "Admin panel authentication — email + password",
        ["Admin › Restaurants"]   = "Manage restaurants (admin CRUD)",
        ["Admin › Categories"]    = "Manage menu categories (admin CRUD)",
        ["Admin › Menu"]          = "Manage menu items (admin CRUD)",
        ["Admin › Users"]         = "Manage users (admin)",
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = Tags
            .Select(kv => new OpenApiTag { Name = kv.Key, Description = kv.Value })
            .ToList();
    }
}
