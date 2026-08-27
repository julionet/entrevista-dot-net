using Microsoft.AspNetCore.Identity;
using App.Application.Ports.Output;
using App.Domain.Entities;

namespace App.Infrastructure.Security;

/// <summary>
/// Adaptador driven: usa o PasswordHasher&lt;TUser&gt; do ASP.NET Core Identity (PBKDF2).
/// A instância de User passada para HashPassword/VerifyHashedPassword é ignorada pela
/// implementação padrão (ela não faz "salting" por usuário além do embutido no hash),
/// então passar null é seguro aqui.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _identityHasher = new();

    public string Hash(string password)
        => _identityHasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string providedPassword)
        => _identityHasher.VerifyHashedPassword(null!, passwordHash, providedPassword) != PasswordVerificationResult.Failed;
}
