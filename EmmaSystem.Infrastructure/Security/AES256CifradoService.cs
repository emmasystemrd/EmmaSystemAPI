using System.Security.Cryptography;
using System.Text;
using EmmaSystem.Application.Interfaces;

namespace EmmaSystem.Infrastructure.Security;

/// <summary>
/// Servicio de cifrado AES-256-CBC usando System.Security.Cryptography nativo
/// </summary>
public class AES256CifradoService : ICifradoService
{
    private readonly byte[] _claveMaestra;
    private const int KeySize = 256;
    private const int IVSize = 128;
    private const int Iterations = 10000;

    public AES256CifradoService()
    {
        var claveMaestraEnv = Environment.GetEnvironmentVariable("EMMA_ENCRYPTION_KEY")
            ?? throw new InvalidOperationException(
                "La variable de entorno EMMA_ENCRYPTION_KEY no está configurada");

        if (string.IsNullOrWhiteSpace(claveMaestraEnv) || claveMaestraEnv.Length < 32)
        {
            throw new InvalidOperationException(
                "La clave maestra EMMA_ENCRYPTION_KEY debe tener al menos 32 caracteres");
        }

        _claveMaestra = Encoding.UTF8.GetBytes(claveMaestraEnv);
    }

    /// <inheritdoc/>
    public string Cifrar(string texto, string salt)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        if (string.IsNullOrEmpty(salt))
            throw new ArgumentException("El salt no puede estar vacío", nameof(salt));

        using var aes = CrearAlgoritmo(salt, out byte[] iv);

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // Escribir el IV al inicio del stream (16 bytes)
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            var bytesTexto = Encoding.UTF8.GetBytes(texto);
            cs.Write(bytesTexto, 0, bytesTexto.Length);
            cs.FlushFinalBlock();
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <inheritdoc/>
    public string Descifrar(string textoCifrado, string salt, string iv)
    {
        if (string.IsNullOrEmpty(textoCifrado))
            return string.Empty;

        if (string.IsNullOrEmpty(salt))
            throw new ArgumentException("El salt no puede estar vacío", nameof(salt));

        if (string.IsNullOrEmpty(iv))
            throw new ArgumentException("El IV no puede estar vacío", nameof(iv));

        var bytesCifrados = Convert.FromBase64String(textoCifrado);
        var bytesIV = Convert.FromBase64String(iv);

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = IVSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        aes.Key = DerivarClave(salt);
        aes.IV = bytesIV;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(bytesCifrados);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    /// <inheritdoc/>
    public (string cifrado, string iv) CifrarConIV(string texto, string salt)
    {
        if (string.IsNullOrEmpty(texto))
            return (string.Empty, string.Empty);

        if (string.IsNullOrEmpty(salt))
            throw new ArgumentException("El salt no puede estar vacío", nameof(salt));

        using var aes = CrearAlgoritmo(salt, out byte[] iv);

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

        var bytesTexto = Encoding.UTF8.GetBytes(texto);
        cs.Write(bytesTexto, 0, bytesTexto.Length);
        cs.FlushFinalBlock();

        return (Convert.ToBase64String(ms.ToArray()), Convert.ToBase64String(iv));
    }

    /// <summary>
    /// Crea una instancia de AES con la clave derivada del salt
    /// </summary>
    private Aes CrearAlgoritmo(string salt, out byte[] iv)
    {
        var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = IVSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Derivar clave única combinando clave maestra + salt del cliente
        aes.Key = DerivarClave(salt);

        // Generar IV aleatorio para cada cifrado
        aes.GenerateIV();
        iv = aes.IV;

        return aes;
    }

    /// <summary>
    /// Deriva una clave de 256 bits usando PBKDF2 con la clave maestra y el salt
    /// </summary>
    private byte[] DerivarClave(string salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            _claveMaestra,
            Encoding.UTF8.GetBytes(salt),
            Iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(32); // 256 bits = 32 bytes
    }
}