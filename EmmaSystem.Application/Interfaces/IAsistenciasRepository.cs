using EmmaSystem.Application.DTOs.Asistencia;

namespace EmmaSystem.Application.Interfaces;

public interface IAsistenciasRepository
{
    Task<List<EstudianteAsistenciaDto>> GetEstudiantesParaAsistenciaAsync(
        int idAsistencia, DateTime fecha, int idCurso, int? idDetalleCurso, int idInstructor, CancellationToken ct = default);

    Task<int?> GetExistingAsistenciaIdAsync(
        DateTime fecha, int idCurso, int? idDetalleCurso, int idInstructor, CancellationToken ct = default);

    Task<int> SaveAsistenciaAsync(AsistenciaSaveDto dto, CancellationToken ct = default);

    Task<List<AsistenciaMatrixDto>> GetMatrixAsistenciaAsync(
        DateTime fecha1, DateTime fecha2, int idCurso, int? idDetalleCurso, int idInstructor, CancellationToken ct = default);

    Task<AsistenciaFormularioDto> GetAsistenciaSemanalAsync(
DateTime fecha1, DateTime fecha2, int idCurso, int? idDetalleCurso, int idInstructor,
CancellationToken ct = default);
}