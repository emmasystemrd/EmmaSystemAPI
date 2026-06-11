using EmmaSystem.Application.DTOs.Catalogos;
using EmmaSystem.Application.DTOs.Cliente;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmmaSystem.Application.Interfaces;

public interface IClienteRepository
{
    Task<IReadOnlyList<ClienteDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default);

    Task<ClienteDetalleDto?> GetByCodigoOrDocumentoAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default);

    // ✅ NUEVOS MÉTODOS
    Task<IReadOnlyList<ClienteDto>> BuscarClientesActivosAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default);
    Task<ClienteDto?> GetByIdAsync(int idCliente, int idEmpresa, CancellationToken ct = default);

    Task CreateAsync(ClienteSaveDto dto, int idEmpresa, int idLogin, CancellationToken ct = default);
    Task UpdateAsync(int idCliente, ClienteSaveDto dto, int idLogin, CancellationToken ct = default);
    Task DeleteAsync(int idCliente, int idLogin, CancellationToken ct = default);

    /// <summary>
    /// Obtiene los tipos de comprobantes fiscales (NCF) disponibles.
    /// Ejecuta [dbo].[spcargar_ecf].
    /// </summary>
    /// <param name="esVenta">true = tipos para ventas, false = tipos para compras</param>
    Task<IReadOnlyList<TipoComprobanteDto>> GetTiposComprobanteAsync(bool esVenta, CancellationToken ct = default);

    /// <summary>
    /// Obtiene las retenciones activas de una empresa.
    /// Ejecuta [dbo].[spcargar_retencion].
    /// </summary>
    /// <param name="idEmpresa">ID de la empresa</param>
    /// <param name="tipo">true = ITBIS, false = ISR (según configuración del SP)</param>
    Task<IReadOnlyList<RetencionDto>> GetRetencionesAsync(int idEmpresa, bool tipo, CancellationToken ct = default);

    /// <summary>
    /// Busca cuentas contables de tipo 'D' (Detalle) por texto.
    /// Ejecuta [dbo].[spbuscar_cuenta_detalle].
    /// </summary>
    /// <param name="textoBuscar">Texto a buscar en Num_Cuenta, Nombre, Descripcion o Grupo</param>
    /// <param name="idEmpresa">ID de la empresa</param>
    Task<IReadOnlyList<CuentaContableDto>> BuscarCuentasContablesAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default);
}