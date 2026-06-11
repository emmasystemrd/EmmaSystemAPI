using System.Security.Cryptography;
using System.Text;

namespace EmmaSystem.Infrastructure.Security;

/// <summary>
/// Helper de encriptación que replica la lógica legacy de Windows Forms.
/// 
/// IMPORTANTE: 
/// - El NOMBRE DE USUARIO se encripta con TripleDES + MD5 (ECB, PKCS7) usando la llave "Diosesbueno@777".
/// - La CLAVE (password) se encripta/desencripta en SQL Server con ENCRYPTBYPASSPHRASE/DECRYPTBYPASSPHRASE('Diosesbueno@7').
/// 
/// Son dos mecanismos DIFERENTES con llaves DIFERENTES por diseño histórico del sistema.
/// </summary>
public static class CryptoHelper
{
    // Llave usada en C# para encriptar el NOMBRE DE USUARIO (TripleDES)
    private const string UsernameKey = "Diosesbueno@777";

    // NOTA: La passphrase 'Diosesbueno@7' se usa SOLO en SQL Server para la CLAVE (password).
    // No se usa en C# porque la desencriptación de la contraseña la hace el SP [dbo].[splogin].
    // public const string SqlPasswordPassphrase = "Diosesbueno@7";

    /// <summary>
    /// Encripta el nombre de usuario con TripleDES (ECB + PKCS7) usando MD5 hash de la llave.
    /// El resultado es compatible con lo que ya está guardado en la columna Usuario.Nombre.
    /// </summary>
    public static string EncryptUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return username;

        try
        {
            byte[] keyArray;
            byte[] arrayToEncrypt = Encoding.UTF8.GetBytes(username);

            // Hash MD5 de la llave (igual que en Windows Forms)
            using var md5 = MD5.Create();
            keyArray = md5.ComputeHash(Encoding.UTF8.GetBytes(UsernameKey));

            // TripleDES con modo ECB y padding PKCS7
#pragma warning disable SYSLIB0021 // TripleDES es legacy, pero lo necesitamos por compatibilidad
            using var tdes = TripleDES.Create();
#pragma warning restore SYSLIB0021
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            using var cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(arrayToEncrypt, 0, arrayToEncrypt.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al encriptar el nombre de usuario.", ex);
        }
    }

    /// <summary>
    /// Desencripta el nombre de usuario (útil para logs o auditoría, no para el login).
    /// </summary>
    public static string DecryptUsername(string encryptedUsername)
    {
        if (string.IsNullOrEmpty(encryptedUsername))
            return encryptedUsername;

        try
        {
            byte[] keyArray;
            byte[] arrayToDecrypt = Convert.FromBase64String(encryptedUsername);

            using var md5 = MD5.Create();
            keyArray = md5.ComputeHash(Encoding.UTF8.GetBytes(UsernameKey));

#pragma warning disable SYSLIB0021
            using var tdes = TripleDES.Create();
#pragma warning restore SYSLIB0021
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            using var cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(arrayToDecrypt, 0, arrayToDecrypt.Length);

            return Encoding.UTF8.GetString(resultArray);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al desencriptar el nombre de usuario.", ex);
        }
    }
}