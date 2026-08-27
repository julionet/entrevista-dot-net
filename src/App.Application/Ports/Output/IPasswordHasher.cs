namespace App.Application.Ports.Output;

/// <summary>
/// Porta de saída (driven port): contrato que a Application exige da infraestrutura
/// para gerar e validar hashes de senha, sem conhecer o algoritmo usado.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}
