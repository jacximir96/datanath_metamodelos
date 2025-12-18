namespace DataNath.ApiMetadatos.Services;

public interface IEncryptionService
{
    string Encrypt(string text);
    string Decrypt(string encryptedText);
}
