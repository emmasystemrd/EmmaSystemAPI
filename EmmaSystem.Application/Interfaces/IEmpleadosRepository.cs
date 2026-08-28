using EmmaSystem.Application.DTOs.Empleado;

namespace EmmaSystem.Application.Interfaces;

public interface IEmpleadosRepository
{
    Task<IReadOnlyList<InstructorDto>> GetInstructoresActivosAsync(CancellationToken ct = default);
}