using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Departamento;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/departamento")]
[Authorize] // Exige Token JWT válido
public sealed class DepartamentoController : ControllerBase
{
    private readonly IDepartamentoRepository _repo;

    public DepartamentoController(IDepartamentoRepository repo) => _repo = repo;

    // Helper para extraer la empresa del Token (Seguridad Multi-tenant)
    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")!.Value);

    /// <summary>
    /// Obtiene todos los departamentos activos de la empresa.
    /// </summary>
    [HttpGet]
    [Permission(Modules.Articulo, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<DepartamentoDto>>> GetAll(CancellationToken ct)
    {
        var result = await _repo.GetAllAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Busca departamentos por nombre o descripción.
    /// </summary>
    [HttpGet("buscar")]
    [Permission(Modules.Articulo, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<DepartamentoDto>>> Search([FromQuery] string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto)) return BadRequest("El texto de búsqueda es requerido.");
        var result = await _repo.SearchAsync(texto, GetIdEmpresa(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo departamento.
    /// </summary>
    [HttpPost]
    [Permission(Modules.Articulo, Operations.Crear)]
    public async Task<IActionResult> Create([FromBody] DepartamentoCreateDto dto, CancellationToken ct)
    {
        await _repo.CreateAsync(dto, GetIdEmpresa(), ct);
        return Ok(new { message = "Departamento creado exitosamente." });
    }

    /// <summary>
    /// Actualiza un departamento existente.
    /// </summary>
    [HttpPut("{id:int}")]
    [Permission(Modules.Articulo, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] DepartamentoUpdateDto dto, CancellationToken ct)
    {
        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Departamento actualizado exitosamente." });
    }

    /// <summary>
    /// Elimina (Soft Delete) un departamento.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Permission(Modules.Articulo, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Departamento eliminado exitosamente." });
    }
}