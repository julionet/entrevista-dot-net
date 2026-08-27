using App.Domain.Exceptions;

namespace App.Domain.Entities;

public sealed class User
{
    public Guid Id { get; }
    public string Email { get; }
    public string PasswordHash { get; }
    public bool IsActive { get; private set; }

    public User(Guid id, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O email é obrigatório.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("A senha é obrigatória.");

        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
    }
}
