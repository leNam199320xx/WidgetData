using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        IConfiguration config, ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
        _context = context;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "Invalid credentials" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials" });

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateToken(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Expires = DateTime.UtcNow.AddHours(GetExpirationHours()),
            UserId = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Roles = roles
        });
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            EmailConfirmed = true,
            IsActive = true
        };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, "Viewer");
        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? dto)
    {
        if (dto != null && !string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            var stored = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken && !r.IsRevoked);
            if (stored != null)
            {
                stored.IsRevoked = true;
                stored.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        return Ok(new { message = "Logged out" });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var stored = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken && !r.IsRevoked);

        if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        var user = stored.User;
        if (!user.IsActive)
            return Unauthorized(new { message = "User account is inactive" });

        // Revoke old refresh token (token rotation)
        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;

        var roles = await _userManager.GetRolesAsync(user);
        var newToken = GenerateToken(user, roles);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

        return Ok(new AuthResponseDto
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            Expires = DateTime.UtcNow.AddHours(GetExpirationHours()),
            UserId = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Roles = roles
        });
    }

    private async Task<string> CreateRefreshTokenAsync(string userId)
    {
        // Clean up old tokens for this user
        var oldTokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && (r.IsRevoked || r.ExpiresAt < DateTime.UtcNow))
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(oldTokens);

        var token = new RefreshToken
        {
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
        return token.Token;
    }

    private int GetExpirationHours()
    {
        var hours = _config["JwtSettings:ExpirationHours"];
        return int.TryParse(hours, out var h) ? h : 24;
    }

    private string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var secret = _config["JwtSettings:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JwtSettings:Secret is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(GetExpirationHours()),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

