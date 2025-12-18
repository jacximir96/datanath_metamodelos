using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using DataNath.ApiMetadatos.Configuration;

namespace DataNath.ApiMetadatos.Services;

public class EncryptionService : IEncryptionService
{
    private readonly string _encryptionKey;

    public EncryptionService(IOptions<EncryptionSettings> settings)
    {
        _encryptionKey = settings.Value.Key;
    }

    public string Encrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));

            // Generar IV aleatorio para cada encriptación
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // Escribir el IV al inicio del stream (primeros 16 bytes)
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(text);
            }

            // Retornar IV + texto encriptado en Base64
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al encriptar: {ex.Message}", ex);
        }
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        // PRIMERO intentar con formato antiguo (IV estático) para compatibilidad
        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16]; // IV de ceros para datos antiguos

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var buffer = Convert.FromBase64String(encryptedText);

            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            // Si falla formato antiguo, intentar con formato nuevo (IV incluido)
            try
            {
                var buffer = Convert.FromBase64String(encryptedText);

                // Verificar que hay suficientes bytes para IV (16 bytes) + datos
                if (buffer.Length < 16)
                    return encryptedText; // Texto plano

                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));

                // Extraer el IV de los primeros 16 bytes
                var iv = new byte[16];
                Array.Copy(buffer, 0, iv, 0, 16);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                // Desencriptar desde el byte 16 en adelante (saltando el IV)
                using var ms = new MemoryStream(buffer, 16, buffer.Length - 16);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch
            {
                // Si falla todo, asumir que el texto está en claro
                return encryptedText;
            }
        }
    }
}
