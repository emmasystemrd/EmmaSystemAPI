namespace EmmaSystem.Application.Interfaces;

/// <summary>
/// Contexto del tenant actual (cliente y empresa) en el request
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// ID del cliente autenticado
    /// </summary>
    int ClienteId { get; }

    /// <summary>
    /// ID de la empresa seleccionada
    /// </summary>
    int EmpresaId { get; }

    /// <summary>
    /// Nombre de la empresa actual
    /// </summary>
    string NombreEmpresa { get; }

    /// <summary>
    /// Establece el contexto del tenant para el request actual
    /// </summary>
    /// <param name="clienteId">ID del cliente</param>
    /// <param name="empresaId">ID de la empresa</param>
    /// <param name="nombreEmpresa">Nombre de la empresa</param>
    void Establecer(int clienteId, int empresaId, string nombreEmpresa);
}