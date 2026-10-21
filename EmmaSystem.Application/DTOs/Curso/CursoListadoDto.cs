namespace EmmaSystem.Application.DTOs.Curso;

public class CursoListadoDto
{
    public int Idcurso { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Horario { get; set; } = string.Empty;
    public decimal Valor_Inscripcion { get; set; }
    public decimal Valor_Mensual { get; set; }

    public DateTime? Fecha_Inicio { get; set; }
    public DateTime? Fecha_Final { get; set; }

    public int Cupo_Maximo { get; set; }
    public int Idinstructor { get; set; }
    public string NombreInstructor { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}