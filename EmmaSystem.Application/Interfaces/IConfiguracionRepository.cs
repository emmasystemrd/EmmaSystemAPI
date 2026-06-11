using EmmaSystem.Application.DTOs.Configuracion;

namespace EmmaSystem.Application.Interfaces;

public interface IConfiguracionRepository
{
    // Facturación Electrónica
    Task<ConfFacturacionElectronicaDto?> GetFacturacionElectronicaAsync(int idEmpresa, CancellationToken ct = default);
    Task UpdateFacturacionElectronicaAsync(ConfFacturacionElectronicaSaveDto dto, int idEmpresa, CancellationToken ct = default);

    // Proveedor/Cliente
    Task<ConfProveedorClienteDto?> GetProveedorClienteAsync(int idEmpresa, CancellationToken ct = default);
    Task UpdateProveedorClienteAsync(ConfProveedorClienteDto dto, int idEmpresa, CancellationToken ct = default);

    // Capital
    Task<(string? Capital, string? Cuenta_Cierre)> GetCapitalAsync(int idEmpresa, CancellationToken ct = default);
    Task UpdateCapitalAsync(string capital, string cuentaCierre, int idEmpresa, CancellationToken ct = default);

    // Impuestos
    Task<ConfImpuestoDto?> GetImpuestosAsync(int idEmpresa, CancellationToken ct = default);

    // Empleados
    Task<ConfEmpleadoDto?> GetEmpleadoAsync(int idEmpresa, CancellationToken ct = default);

    // TSS
    Task<ConfTssDto?> GetTssAsync(int idEmpresa, CancellationToken ct = default);

    // Facturación (impresión)
    Task<ConfFacturacionDto?> GetFacturacionAsync(int idEmpresa, CancellationToken ct = default);
    Task UpdateFacturacionAsync(ConfFacturacionDto dto, CancellationToken ct = default);
}