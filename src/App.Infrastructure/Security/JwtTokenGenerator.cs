using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using App.Application.Ports.Output;
using App.Domain.Entities;

namespace App.Infrastructure.Security;

/// <summary>
/// Access token e refresh token são ambos JWTs, mas emitidos com audiences diferentes
/// (Audience vs RefreshAudience). Isso impede que um refresh token seja aceito como Bearer
/// token nas rotas protegidas: o middleware de autenticação só valida a Audience de access token.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateAccessToken(User user)
        => GenerateToken(user, _settings.Audience, TimeSpan.FromMinutes(_settings.ExpirationMinutes));

    public string GenerateRefreshToken(User user)
        => GenerateToken(user, _settings.RefreshAudience, TimeSpan.FromDays(_settings.RefreshTokenExpirationDays));

    public Guid? ValidateRefreshToken(string refreshToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.RefreshAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            // MapInboundClaims = false: sem isso, o handler remapeia "sub"/"email" para URIs
            // longas (ClaimTypes.NameIdentifier/Email), e FindFirst(JwtRegisteredClaimNames.Sub) não acha nada.
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(refreshToken, validationParameters, out _);
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            // Token expirado/assinatura inválida (SecurityTokenException) ou malformado (ArgumentException).
            return null;
        }
    }

    private string GenerateToken(User user, string audience, TimeSpan lifetime)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
