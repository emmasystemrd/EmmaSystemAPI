namespace EmmaSystem.Application.DTOs.Asistencia;

public class AsistenciaSaveDto
{
    public int? IdAsistencia { get; set; } // Si es null o 0, se crea nueva. Si tiene valor, se actualizan detalles.
    public DateTime Fecha { get; set; }
    public int Idcurso { get; set; }
    public int? Iddetalle_Curso { get; set; }
    public int Idlogin { get; set; }
    public int Idinstructor { get; set; }

    // Lista de estudiantes con su estado de asistencia (P, A, E, T, M, N)
    public List<DetalleAsistenciaSaveDto> Detalles { get; set; } = new();
}

public class DetalleAsistenciaSaveDto
{
    public int Iddetalle { get; set; } // Si es > 0, se actualiza. Si es 0, se inserta.
    public int Idestudiante { get; set; }
    public string Asistencia { get; set; } = string.Empty; // P, A, E, T, M, N
}