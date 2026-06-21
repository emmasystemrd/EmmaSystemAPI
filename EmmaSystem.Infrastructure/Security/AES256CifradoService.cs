using System.Security.Cryptography;
using System.Text;
using EmmaSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EmmaSystem.Infrastructure.Security;

/// <summary>
/// Servicio de cifrado AES-256-CBC usando System.Security.Cryptography nativo.
/// Compatible con campos VARBINARY de SQL Server.
/// </summary>
public class AES256CifradoService : ICifradoService
{
    private readonly byte[] _claveMaestra;
    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int Iterations = 10000;

    public AES256CifradoService(IConfiguration configuration)
    {
        // 1. Intentar variable de entorno
        var claveEnv = Environment.GetEnvironmentVariable("EMMA_ENCRYPTION_KEY");

        // 2. Fallback a appsettings.json
        if (string.IsNullOrWhiteSpace(claveEnv))
            claveEnv = configuration["Encryption:AesKey"];

        if (string.IsNullOrWhiteSpace(claveEnv) || claveEnv.Length < 32)
            throw new InvalidOperationException(
                "La clave EMMA_ENCRYPTION_KEY no está configurada o tiene menos de 32 caracteres. " +
                "Configure la variable de entorno o Encryption:AesKey en appsettings.json.");

        _claveMaestra = Encoding.UTF8.GetBytes(claveEnv);
    }

    /// <inheritdoc/>
    public byte[] Cifrar(string textoPlano, byte[] salt, out byte[] iv)
    {
        if (string.IsNullOrEmpty(textoPlano))
        {
            iv = Array.Empty<byte>();
            return Array.Empty<byte>();
        }

        if (salt is null || salt.Length == 0)
            throw new ArgumentException("El salt no puede estar vacío", nameof(salt));

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = DerivarClave(salt);
        aes.GenerateIV();
        iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            var bytesTexto = Encoding.UTF8.GetBytes(textoPlano);
            cs.Write(bytesTexto, 0, bytesTexto.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    /// <inheritdoc/>
    public string Descifrar(byte[] datosCifrados, byte[] salt, byte[] iv)
    {
        if (datosCifrados is null || datosCifrados.Length == 0)
            return string.Empty;

        if (salt is null || salt.Length == 0)
            throw new ArgumentException("El salt no puede estar vacío", nameof(salt));

        if (iv is null || iv.Length == 0)
            throw new ArgumentException("El IV no puede estar vacío", nameof(iv));

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = DerivarClave(salt);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(datosCifrados);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Deriva una clave de 256 bits usando PBKDF2 con la clave maestra y el salt del cliente
    /// </summary>
    private byte[] DerivarClave(byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            _claveMaestra,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(32); // 256 bits = 32 bytes
    }
}