using AgendamentoPro.Core.Interfaces.Services;

namespace AgendamentoPro.Infrastructure.Services.Auth
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);
        public bool Verify(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
