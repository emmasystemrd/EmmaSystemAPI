namespace EmmaSystem.Application.Interfaces;

public interface ITenantContext
{
    int ClienteId { get; }
    int EmpresaId { get; }
    string NombreEmpresa { get; }
    string RncCedula { get; }   // Nuevo
    byte Ambiente { get; }      // Nuevo (1=Prueba, 2=Cert, 3=Prod)

    void Establecer(int clienteId, int empresaId, string nombreEmpresa,
                    string rncCedula, byte ambiente);
}