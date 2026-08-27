using App.Application.DTOs;
using App.Application.Ports.Output;
using App.Application.UseCases;
using App.Domain.Entities;
using App.Domain.Exceptions;
using Moq;
using Xunit;

namespace App.Application.Tests;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepositoryMock.Object, _passwordHasherMock.Object, _jwtTokenGeneratorMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ComEmailNovo_DevePersistirUsuarioComSenhaHasheada()
    {
        var request = new RegisterRequest("user@test.com", "Senha123!");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(h => h.Hash(request.Password)).Returns("hashed-password");

        var result = await _sut.RegisterAsync(request);

        Assert.Equal("user@test.com", result.Email);
        _userRepositoryMock.Verify(r => r.AddAsync(
            It.Is<User>(u => u.Email == "user@test.com" && u.PasswordHash == "hashed-password"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ComEmailJaCadastrado_DeveLancarDomainException()
    {
        var request = new RegisterRequest("user@test.com", "Senha123!");
        var existingUser = new User(Guid.NewGuid(), "user@test.com", "hash");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(request));
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ComCredenciaisValidas_DeveRetornarToken()
    {
        var user = new User(Guid.NewGuid(), "user@test.com", "hashed-password");
        var request = new LoginRequest("user@test.com", "Senha123!");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("hashed-password", request.Password)).Returns(true);
        _jwtTokenGeneratorMock.Setup(j => j.GenerateToken(user)).Returns("fake-jwt-token");

        var result = await _sut.LoginAsync(request);

        Assert.Equal("fake-jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_ComEmailInexistente_DeveLancarDomainException()
    {
        var request = new LoginRequest("naoexiste@test.com", "Senha123!");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("naoexiste@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<DomainException>(() => _sut.LoginAsync(request));
        _jwtTokenGeneratorMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ComSenhaInvalida_DeveLancarDomainException()
    {
        var user = new User(Guid.NewGuid(), "user@test.com", "hashed-password");
        var request = new LoginRequest("user@test.com", "senha-errada");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("hashed-password", request.Password)).Returns(false);

        await Assert.ThrowsAsync<DomainException>(() => _sut.LoginAsync(request));
        _jwtTokenGeneratorMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

}
