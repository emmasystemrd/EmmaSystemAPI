using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Proveedor;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EmmaSystem.API.Controllers;
[ApiController]
[Route("api/proveedor")]
[Authorize]
public sealed class ProveedorController : ControllerBase
{
    private readonly IProveedorRepository _repo;
    public ProveedorController(IProveedorRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")!.Value);
    private int GetIdLogin() => int.Parse(User.FindFirst("Idusuario")!.Value);

    [HttpGet]
    [Permission(Modules.Proveedor, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<ProveedorDto>>> GetAll(CancellationToken ct)
    {
        var result = await _repo.GetAllAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Permission(Modules.Proveedor, Operations.Crear)]
    public async Task<IActionResult> Create([FromBody] ProveedorSaveDto dto, CancellationToken ct)
    {
        await _repo.CreateAsync(dto, GetIdEmpresa(), GetIdLogin(), ct);
        return Ok(new { message = "Proveedor creado exitosamente." });
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Proveedor, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] ProveedorSaveDto dto, CancellationToken ct)
    {
        await _repo.UpdateAsync(id, dto, GetIdLogin(), ct);
        return Ok(new { message = "Proveedor actualizado exitosamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Proveedor, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, GetIdLogin(), ct);
        return Ok(new { message = "Proveedor eliminado exitosamente." });
    }
}