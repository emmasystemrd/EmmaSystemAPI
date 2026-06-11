using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Catalogos;
using EmmaSystem.Application.DTOs.Cliente;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/cliente")]
[Authorize]
public sealed class ClienteController : ControllerBase
{
    private readonly IClienteRepository _repo;

    public ClienteController(IClienteRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")!.Value);
    // En EmmaSystem, el Idusuario del JWT es el mismo que se usa como Idlogin en los SPs
    private int GetIdLogin() => int.Parse(User.FindFirst("Idusuario")!.Value);

    [HttpGet]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> GetAll(CancellationToken ct)
    {
        var result = await _repo.GetAllAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    [HttpGet("buscar/{texto}")]
    [Permission(Modules.Cliente, Operations.Consultar)]
    public async Task<ActionResult<ClienteDetalleDto>> GetByCodigoOrDocumento(string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest("El texto de búsqueda es requerido.");

        var result = await _repo.GetByCodigoOrDocumentoAsync(texto.Trim(), GetIdEmpresa(), ct);

        if (result is null)
            return NotFound(new { message = $"No se encontró ningún cliente activo con código o documento '{texto}'." });

        return Ok(result);
    }

    /// <summary>
    /// Busca clientes activos por texto. Si el texto está vacío, devuelve todos los clientes activos.
    /// </summary>
    [HttpGet("buscar-activos")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> BuscarActivos(
        [FromQuery] string? texto = "",
        CancellationToken ct = default)
    {
        var result = await _repo.BuscarClientesActivosAsync(texto ?? "", GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un cliente específico por ID
    /// </summary>
    [HttpGet("{id:int}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<ClienteDto>> GetById(
        int id,
        CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(id, GetIdEmpresa(), ct);

        if (result is null)
            return NotFound(new { message = $"Cliente con ID {id} no encontrado." });

        return Ok(result);
    }

    [HttpPost]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<IActionResult> Create([FromBody] ClienteSaveDto dto, CancellationToken ct)
    {
        await _repo.CreateAsync(dto, GetIdEmpresa(), GetIdLogin(), ct);
        return Ok(new { message = "Cliente creado exitosamente." });
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] ClienteSaveDto dto, CancellationToken ct)
    {
        await _repo.UpdateAsync(id, dto, GetIdLogin(), ct);
        return Ok(new { message = "Cliente actualizado exitosamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, GetIdLogin(), ct);
        return Ok(new { message = "Cliente eliminado exitosamente." });
    }


    //private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")?.Value ?? "1");

    // ═══════════════════════════════════════════════════════════════
    // TIPOS DE COMPROBANTES FISCALES (NCF)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene los tipos de comprobantes fiscales disponibles.
    /// </summary>
    /// <param name="esVenta">
    /// true = tipos para ventas (01, 02, 12, 14, 15, 16, 31, 32, 44, 45, 46)
    /// false = tipos para compras (11, 13, 17, 41, 43, 47)
    /// </param>
    [HttpGet("tipos-comprobante")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<TipoComprobanteDto>>> GetTiposComprobante(
        [FromQuery] bool esVenta = true,
        CancellationToken ct = default)
    {
        var result = await _repo.GetTiposComprobanteAsync(esVenta, ct);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // RETENCIONES (ITBIS / ISR)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las retenciones activas de la empresa.
    /// </summary>
    /// <param name="tipo">
    /// true = retenciones de ITBIS
    /// false = retenciones de ISR
    /// </param>
    [HttpGet("retenciones")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<RetencionDto>>> GetRetenciones(
        [FromQuery] bool tipo = true,
        CancellationToken ct = default)
    {
        var result = await _repo.GetRetencionesAsync(GetIdEmpresa(), tipo, ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene las retenciones de ITBIS de la empresa.
    /// </summary>
    [HttpGet("retenciones/itbis")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<RetencionDto>>> GetRetencionesItbis(CancellationToken ct = default)
    {
        var result = await _repo.GetRetencionesAsync(GetIdEmpresa(), false, ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene las retenciones de ISR de la empresa.
    /// </summary>
    [HttpGet("retenciones/isr")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<RetencionDto>>> GetRetencionesIsr(CancellationToken ct = default)
    {
        var result = await _repo.GetRetencionesAsync(GetIdEmpresa(), true, ct);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // CUENTAS CONTABLES (CATÁLOGO)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Busca cuentas contables de tipo 'D' (Detalle) por texto.
    /// Busca en: Num_Cuenta, Nombre, Descripcion y Grupo.
    /// </summary>
    /// <param name="texto">Texto a buscar (mínimo 1 carácter)</param>
    [HttpGet("cuentas")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CuentaContableDto>>> BuscarCuentas(
        [FromQuery] string texto,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest(new { message = "El parámetro 'texto' es requerido." });

        var result = await _repo.BuscarCuentasContablesAsync(texto.Trim(), GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene todas las cuentas contables de tipo 'D' (Detalle) de la empresa.
    /// </summary>
    [HttpGet("cuentas/todas")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CuentaContableDto>>> GetAllCuentas(CancellationToken ct = default)
    {
        // Usamos un texto vacío para traer todas
        var result = await _repo.BuscarCuentasContablesAsync("", GetIdEmpresa(), ct);
        return Ok(result);
    }
}