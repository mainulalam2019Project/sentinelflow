using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SentinelFlow.Domain;
using SentinelFlow.Infrastructure;

public static class SentinelFlowSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SentinelFlowDbContext>();
        await db.Database.MigrateAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Admin", "User" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        async Task EnsureUser(string email, string name, string role, string password)
        {
            if (await userManager.FindByEmailAsync(email) is null)
            {
                var u = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
                await userManager.CreateAsync(u, password);
                await userManager.AddToRoleAsync(u, role);
            }
        }

        await EnsureUser("admin@sentinelflow.dev", "Admin User", "Admin", "Admin@123");
        await EnsureUser("user@sentinelflow.dev", "Regular User", "User", "User@123");

        if (!await db.Services.AnyAsync())
        {
            var payment = new MonitoredService { Name = "Payment API", Description = "Handles payment processing", Environment = SentinelFlow.Domain.Environment.Production, BaseUrl = "https://payment.example.com", Owner = "payments-team", HealthCheckPath = "/health", Criticality = Criticality.Critical, SlaTarget = 99.95, Health = ServiceHealth.Healthy };
            var checkout = new MonitoredService { Name = "Checkout API", Description = "Checkout flow", Environment = SentinelFlow.Domain.Environment.Production, BaseUrl = "https://checkout.example.com", Owner = "commerce-team", HealthCheckPath = "/health", Criticality = Criticality.High, SlaTarget = 99.9, Health = ServiceHealth.Healthy };
            var dbService = new MonitoredService { Name = "Payment Database", Description = "Primary PostgreSQL", Environment = SentinelFlow.Domain.Environment.Production, BaseUrl = "postgres://db.internal:5432", Owner = "platform-team", HealthCheckPath = "/health", Criticality = Criticality.Critical, SlaTarget = 99.99, Health = ServiceHealth.Healthy };
            db.Services.AddRange(payment, checkout, dbService);
            await db.SaveChangesAsync();
            db.Dependencies.AddRange(
                new Dependency { ServiceId = checkout.Id, DependsOnServiceId = payment.Id },
                new Dependency { ServiceId = payment.Id, DependsOnServiceId = dbService.Id });
            await db.SaveChangesAsync();
        }
    }
}
