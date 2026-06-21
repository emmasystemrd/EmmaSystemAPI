namespace EmmaSystem.Application.Interfaces;

public interface ICifradoService
{
    /// <summary>
    /// Cifra texto plano usando AES-256-CBC con salt y clave maestra.
    /// Retorna los bytes cifrados y genera un IV aleatorio.
    /// </summary>
    byte[] Cifrar(string textoPlano, byte[] salt, out byte[] iv);

    /// <summary>
    /// Descifra bytes cifrados usando AES-256-CBC con salt, IV y clave maestra.
    /// </summary>
    string Descifrar(byte[] datosCifrados, byte[] salt, byte[] iv);
}