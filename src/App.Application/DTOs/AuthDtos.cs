namespace App.Application.DTOs;

public sealed record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse(Guid Id, string Email);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string Token);
