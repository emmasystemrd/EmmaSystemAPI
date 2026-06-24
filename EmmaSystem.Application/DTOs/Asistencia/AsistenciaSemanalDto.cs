namespace EmmaSystem.Application.DTOs.Asistencia;

public class AsistenciaSemanalDto
{
    public int No { get; set; }
    public string ALUMNO { get; set; } = string.Empty;
    public int EDAD { get; set; }
    public string GENERO { get; set; } = string.Empty;

    // Semana 1
    public int S1_P { get; set; }
    public int S1_A { get; set; }
    public int S1_E { get; set; }
    public int S1_T { get; set; }

    // Semana 2
    public int S2_P { get; set; }
    public int S2_A { get; set; }
    public int S2_E { get; set; }
    public int S2_T { get; set; }

    // Semana 3
    public int S3_P { get; set; }
    public int S3_A { get; set; }
    public int S3_E { get; set; }
    public int S3_T { get; set; }

    // Semana 4
    public int S4_P { get; set; }
    public int S4_A { get; set; }
    public int S4_E { get; set; }
    public int S4_T { get; set; }

    // Semana 5
    public int S5_P { get; set; }
    public int S5_A { get; set; }
    public int S5_E { get; set; }
    public int S5_T { get; set; }

    // Total
    public int Total_P { get; set; }
}

public class AsistenciaFormularioDto
{
    public string Institucion { get; set; } = string.Empty;
    public string LogoBase64 { get; set; } = string.Empty;
    public string Disciplina { get; set; } = string.Empty;
    public string Docente { get; set; } = string.Empty;
    public string Dias { get; set; } = string.Empty;
    public string Horario { get; set; } = string.Empty;
    public string Mes { get; set; } = string.Empty;
    public int Año { get; set; }
    public List<string> FechasSemanas { get; set; } = new();
    public List<AsistenciaSemanalDto> Estudiantes { get; set; } = new();
}