namespace ArenaStorm.WebAPI.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArenaStorm.WebAPI.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Service responsible for generating and validating JSON Web Tokens (JWT).
/// </summary>
public class JwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly double _expiryHours;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtService"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration for retrieving JWT settings.</param>
    public JwtService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"] ?? "ArenaStorm_SuperSecret_JWT_Key_2026_Change_In_Production!";
        _issuer = configuration["Jwt:Issuer"] ?? "ArenaStorm";
        _audience = configuration["Jwt:Audience"] ?? "ArenaStormPlayers";
        _expiryHours = double.TryParse(configuration["Jwt:ExpiryHours"], out var hours) ? hours : 24;
    }

    /// <summary>
    /// Generates a signed JWT token for the specified user.
    /// </summary>
    /// <param name="user">The user entity containing identity claims.</param>
    /// <returns>A signed JWT token string.</returns>
    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("username", user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("displayName", user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT token and extracts its claims principal.
    /// </summary>
    /// <param name="token">The JWT token string to validate.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> if valid; otherwise, null.</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
