namespace AgendamentoPro.Core.Interfaces.Services
{
    public interface IPasswordHasher
    {
        string Hash(string senha);
        bool Verify(string senha, string hash);
    }
}
