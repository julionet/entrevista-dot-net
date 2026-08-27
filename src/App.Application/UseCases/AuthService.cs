using App.Application.DTOs;
using App.Application.Ports.Input;
using App.Application.Ports.Output;
using App.Domain.Entities;
using App.Domain.Exceptions;

namespace App.Application.UseCases;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
            throw new DomainException($"Já existe um usuário cadastrado com o email '{email}'.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(Guid.NewGuid(), email, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterResponse(user.Id, user.Email);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Mensagem genérica de propósito: não revelar se o email existe ou não.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new DomainException("Email ou senha inválidos.");

        if (!user.IsActive)
            throw new DomainException("Usuário inativo.");

        return CreateTokenResponse(user);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _jwtTokenGenerator.ValidateRefreshToken(request.RefreshToken)
            ?? throw new DomainException("Refresh token inválido ou expirado.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Refresh token inválido ou expirado.");

        if (!user.IsActive)
            throw new DomainException("Usuário inativo.");

        return CreateTokenResponse(user);
    }

    private TokenResponse CreateTokenResponse(User user)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user);
        return new TokenResponse(accessToken, refreshToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
