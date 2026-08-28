using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Empleado;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/empleados")]
[Authorize]
public sealed class EmpleadosController : ControllerBase
{
    private readonly IEmpleadosRepository _repo;

    public EmpleadosController(IEmpleadosRepository repo) => _repo = repo;

    [HttpGet("instructores")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<InstructorDto>>> GetInstructoresActivos(CancellationToken ct)
    {
        var result = await _repo.GetInstructoresActivosAsync(ct);
        return Ok(result);
    }
}