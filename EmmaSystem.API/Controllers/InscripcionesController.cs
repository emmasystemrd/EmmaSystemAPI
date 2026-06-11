using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Inscripcion;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/inscripciones")]
[Authorize]
public sealed class InscripcionesController : ControllerBase
{
    private readonly IInscripcionesRepository _repo;

    public InscripcionesController(IInscripcionesRepository repo) => _repo = repo;

    private int GetIdLogin() => int.Parse(User.FindFirst("Idusuario")?.Value ?? "1"); // Ajusta "Idusuario" al claim real de tu JWT

    [HttpGet]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<InscripcionListadoDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _repo.GetAllAsync(ct));
    }

    [HttpGet("buscar")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<InscripcionListadoDto>>> Search(
        [FromQuery] DateTime? fecha1,
        [FromQuery] DateTime? fecha2,
        [FromQuery] bool isFecha = false,
        [FromQuery] string texto = "",
        [FromQuery] string columna = "e.Nombres",
        CancellationToken ct = default)
    {
        return Ok(await _repo.SearchAsync(fecha1, fecha2, isFecha, texto, columna, ct));
    }

    [HttpGet("{id:int}")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<InscripcionDetalleDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(id, ct);
        if (result is null) return NotFound(new { message = "Inscripción no encontrada." });
        return Ok(result);
    }

    [HttpGet("{id:int}/imprimir")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<InscripcionImpresionDto>> GetPrintData(int id, CancellationToken ct)
    {
        var result = await _repo.GetPrintDataAsync(id, ct);
        if (result is null) return NotFound(new { message = "Datos de impresión no encontrados." });

        // Convertir foto a base64 si existe para el frontend
        if (result.Foto != null && result.Foto.Length > 0)
        {
            // El frontend ya espera el base64, o podemos devolverlo así:
            // result.FotoBase64 = Convert.ToBase64String(result.Foto);
        }
        return Ok(result);
    }

    [HttpGet("estudiante/{texto}")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult> GetEstudianteByCodigo(string texto, CancellationToken ct)
    {
        var result = await _repo.GetEstudianteByCodigoAsync(texto, ct);
        if (result is null) return NotFound(new { message = "Estudiante no encontrado con ese código o documento." });
        return Ok(result);
    }

    [HttpPost]
    [Permission(Modules.Educacion, Operations.Crear)]
    public async Task<ActionResult<int>> Create([FromBody] InscripcionSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Asignar el usuario que realiza la inscripción desde el Token JWT
        dto.Idlogin = GetIdLogin();

        var id = await _repo.InsertAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = id }, id);
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Educacion, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] InscripcionSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Inscripción actualizada correctamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Educacion, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Inscripción eliminada correctamente." });
    }
}