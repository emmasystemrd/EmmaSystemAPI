using EmmaSystem.Application.Interfaces;

namespace EmmaSystem.Infrastructure.Data;

/// <summary>
/// Contexto del tenant actual. Scoped para tener una instancia por request HTTP.
/// </summary>
public class TenantContext : ITenantContext
{
    public int ClienteId { get; private set; }
    public int EmpresaId { get; private set; }
    public string NombreEmpresa { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public void Establecer(int clienteId, int empresaId, string nombreEmpresa)
    {
        if (clienteId <= 0)
            throw new ArgumentException("El ID de cliente debe ser mayor a 0", nameof(clienteId));

        if (empresaId <= 0)
            throw new ArgumentException("El ID de empresa debe ser mayor a 0", nameof(empresaId));

        if (string.IsNullOrWhiteSpace(nombreEmpresa))
            throw new ArgumentException("El nombre de empresa no puede estar vacío", nameof(nombreEmpresa));

        ClienteId = clienteId;
        EmpresaId = empresaId;
        NombreEmpresa = nombreEmpresa;
    }
}