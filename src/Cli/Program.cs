using System.Security.Cryptography;
using System.Text;
using Core.Domain.Constants;
using Core.Domain.Entities;
using dotenv.net;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var envFile = FindEnvFile();
if (envFile is not null)
    DotEnv.Load(new DotEnvOptions(envFilePaths: [envFile]));

if (args.Length < 3 || args[0] != "create-admin")
{
    Console.WriteLine("Usage: dotnet run --project src/Cli -- create-admin <email> <username> [--super]");
    return 1;
}

var email = args[1];
var userName = args[2];
var isSuperAdmin = args.Contains("--super");

var services = new ServiceCollection();

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is not set.");

services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

services.AddLogging();

var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

foreach (var r in new[] { TandurRoles.User, TandurRoles.Admin, TandurRoles.SuperAdmin })
{
    if (!await roleManager.RoleExistsAsync(r))
        await roleManager.CreateAsync(new IdentityRole(r));
}

if (await userManager.FindByEmailAsync(email) is not null)
{
    Console.WriteLine($"Error: A user with email '{email}' already exists.");
    return 1;
}

var password = GeneratePassword();
var user = new AppUser
{
    UserName = userName,
    Email = email,
    FirstName = userName,
    LastName = string.Empty,
    EmailConfirmed = false,
    MustChangePassword = false,
};

var result = await userManager.CreateAsync(user, password);
if (!result.Succeeded)
{
    foreach (var error in result.Errors)
        Console.WriteLine($"Error: {error.Description}");
    return 1;
}

var assignedRole = isSuperAdmin ? TandurRoles.SuperAdmin : TandurRoles.Admin;
await userManager.AddToRoleAsync(user, assignedRole);

Console.WriteLine();
Console.WriteLine($"{assignedRole} created successfully.");
Console.WriteLine($"  Username: {userName}");
Console.WriteLine($"  Email   : {email}");
Console.WriteLine($"  Password: {password}");
Console.WriteLine();
Console.WriteLine($"The {assignedRole.ToLower()} must change their password on first login.");

return 0;

static string? FindEnvFile()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var path = Path.Combine(dir.FullName, ".env");
        if (File.Exists(path)) return path;
        dir = dir.Parent;
    }
    return null;
}

static string GeneratePassword()
{
    const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string lower = "abcdefghijklmnopqrstuvwxyz";
    const string digits = "0123456789";
    const string all = upper + lower + digits;

    var bytes = RandomNumberGenerator.GetBytes(12);
    var sb = new StringBuilder();
    sb.Append(upper[bytes[0] % upper.Length]);
    sb.Append(lower[bytes[1] % lower.Length]);
    sb.Append(digits[bytes[2] % digits.Length]);
    for (int i = 3; i < 12; i++)
        sb.Append(all[bytes[i] % all.Length]);

    return new string(sb.ToString().OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
}
