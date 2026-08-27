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

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Mensagem genérica de propósito: não revelar se o email existe ou não.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new DomainException("Email ou senha inválidos.");

        if (!user.IsActive)
            throw new DomainException("Usuário inativo.");

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new LoginResponse(token);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
