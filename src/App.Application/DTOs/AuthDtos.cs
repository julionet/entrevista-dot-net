using System.Text.Json.Serialization;

namespace App.Application.DTOs;

public sealed record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse(Guid Id, string Email);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(
    [property: JsonPropertyName("refresh_token")] string RefreshToken);

public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken);
