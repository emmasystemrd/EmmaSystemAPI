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
    public string Tipo_Duracion { get; set; } = string.Empty;
    public int Duracion { get; set; }
    public int Cupo_Maximo { get; set; }
    public string Idinstructor { get; set; } = string.Empty; // Num_Doc del empleado
    public string Estado { get; set; } = string.Empty;
}