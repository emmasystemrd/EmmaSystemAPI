using EmmaSystem.Application.Interfaces;

namespace EmmaSystem.Infrastructure.Data;

public class TenantContext : ITenantContext
{
    public int ClienteId { get; private set; }
    public int EmpresaId { get; private set; }
    public string NombreEmpresa { get; private set; } = string.Empty;
    public string RncCedula { get; private set; } = string.Empty;
    public byte Ambiente { get; private set; } = 1;

    public void Establecer(int clienteId, int empresaId, string nombreEmpresa,
                           string rncCedula, byte ambiente)
    {
        ClienteId = clienteId;
        EmpresaId = empresaId;
        NombreEmpresa = nombreEmpresa;
        RncCedula = rncCedula;
        Ambiente = ambiente;
    }
}