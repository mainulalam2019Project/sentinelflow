using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SentinelFlow.Domain;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _config;
    private readonly SentinelFlowDbContext _db;

    public AuthController(UserManager<ApplicationUser> users, IConfiguration config, SentinelFlowDbContext db)
        => (_users, _config, _db) = (users, config, db);

    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string FullName, string Email, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var user = new ApplicationUser { UserName = req.Email, Email = req.Email, FullName = req.FullName, EmailConfirmed = true };
        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        await _users.AddToRoleAsync(user, "User");
        _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "UserRegistered", EntityType = nameof(ApplicationUser), EntityId = user.Id });
        await _db.SaveChangesAsync();
        return Created("", new { user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { error = "Invalid credentials" });

        var roles = await _users.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "super-secret-dev-key-change-me-32chars!!"));
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "sentinelflow",
            audience: _config["Jwt:Audience"] ?? "sentinelflow-clients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), user = new { user.FullName, user.Email, roles } });
    }
}
