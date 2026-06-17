namespace EmmaSystem.Infrastructure.Settings;

/// <summary>
/// Configuración del sistema EmmaSystem cargada desde appsettings.json
/// </summary>
public class EmmaSystemSettings
{
    /// <summary>
    /// Cadena de conexión a la base de datos central EmmaSystemCentral
    /// </summary>
    public string CadenaConexionCentral { get; set; } = string.Empty;

    /// <summary>
    /// Minutos de cache para conexiones de empresas
    /// </summary>
    public int MinutosCache { get; set; } = 10;
}