namespace EmmaSystem.Application.Interfaces;

/// <summary>
/// Servicio de cifrado AES-256 para datos sensibles
/// </summary>
public interface ICifradoService
{
    /// <summary>
    /// Cifra un texto usando AES-256-CBC. El IV se almacena junto con el texto cifrado.
    /// </summary>
    /// <param name="texto">Texto plano a cifrar</param>
    /// <param name="salt">Salt único del cliente para derivar la clave</param>
    /// <returns>Texto cifrado en Base64 (incluye el IV al inicio)</returns>
    string Cifrar(string texto, string salt);

    /// <summary>
    /// Descifra un texto que fue cifrado con CifrarConIV
    /// </summary>
    /// <param name="textoCifrado">Texto cifrado en Base64</param>
    /// <param name="salt">Salt del cliente usado durante el cifrado</param>
    /// <param name="iv">IV usado durante el cifrado (en Base64)</param>
    /// <returns>Texto plano descifrado</returns>
    string Descifrar(string textoCifrado, string salt, string iv);

    /// <summary>
    /// Cifra un texto y retorna tanto el texto cifrado como el IV por separado
    /// </summary>
    /// <param name="texto">Texto plano a cifrar</param>
    /// <param name="salt">Salt único del cliente para derivar la clave</param>
    /// <returns>Tupla con el texto cifrado y el IV (ambos en Base64)</returns>
    (string cifrado, string iv) CifrarConIV(string texto, string salt);
}