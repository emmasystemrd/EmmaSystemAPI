using Dapper;
using EmmaSystem.Application.DTOs.Estudiante;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class EstudiantesRepository : IEstudiantesRepository
{
    private readonly SqlConnectionFactory _factory;

    public EstudiantesRepository(SqlConnectionFactory factory) => _factory = factory;

    // ═══ LISTADOS ═══

    public async Task<IReadOnlyList<EstudianteListadoDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryAsync<EstudianteListadoDto>(
            new CommandDefinition(
                "[dbo].[MostrarEstudiante1]",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<EstudianteListadoDto>> SearchAsync(string texto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        // ✅ Usar clase tipada para evitar problemas con dynamic
        var result = await conn.QueryAsync<EstudianteSqlRow>(
            new CommandDefinition(
                "[dbo].[BuscarEstudiante]",
                new { TextoBuscar = texto },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        // ✅ Ahora las propiedades tienen tipos definidos, no dynamic
        return result.Select(r => new EstudianteListadoDto
        {
            IdEstudiante = r.Idestudiante,
            Codigo = r.Codigo ?? "",
            Estudiante = r.Estudiante ?? "",
            Num_Documento = r.Num_Documento ?? "",
            Fecha_Nacimiento = r.Fecha_Nacimiento, // ✅ Ya es DateTime?, no necesita conversión
            Telefono = r.Telefono ?? "",
            Estado = r.Estado ?? ""
        }).ToList();
    }

    // ═══ DETALLE COMPLETO ═══

    public async Task<EstudianteDetalleDto?> GetByIdAsync(int idEstudiante, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        // Usamos clase tipada para evitar problemas con byte[] y DBNull
        var row = await conn.QueryFirstOrDefaultAsync<EstudianteSqlRow>(
            @"SELECT 
                e.Idestudiante, e.Codigo, e.Nombres, e.Apellido1, e.Apellido2,
                (e.Nombres + ' ' + e.Apellido1 + ' ' + e.Apellido2) as Estudiante,
                e.Sexo, e.Foto, e.Tipo_Doc, e.Num_Documento, e.Lugar_Nacimiento,
                e.Telefono, e.Celular, e.Nacionalidad, e.Fecha_Nacimiento,
                e.Tipo_Sangre, e.Email, e.Direccion, e.Provincia, e.Municipio,
                e.Sector, e.Alergico, e.Medicamentos_que_usa,
                c.Codigo as Codigo_Padre, e.Parentesco, e.Estado
              FROM Estudiante e
              LEFT JOIN Cliente1 c ON c.Idcliente = e.Idcliente
              WHERE e.Idestudiante = @IdEstudiante AND e.Estado != 'E'",
            new { IdEstudiante = idEstudiante },
            commandType: CommandType.Text);

        if (row == null) return null;

        return new EstudianteDetalleDto
        {
            IdEstudiante = row.Idestudiante,
            Codigo = row.Codigo ?? "",
            Nombres = row.Nombres ?? "",
            Apellido1 = row.Apellido1 ?? "",
            Apellido2 = row.Apellido2 ?? "",
            Estudiante = row.Estudiante ?? "",
            Sexo = row.Sexo ?? "",
            FotoBase64 = null, // La foto se carga por separado
            TieneFoto = row.Foto != null && row.Foto.Length > 0,
            Tipo_Doc = row.Tipo_Doc,
            Num_Documento = row.Num_Documento ?? "",
            Lugar_Nacimiento = row.Lugar_Nacimiento ?? "",
            Telefono = row.Telefono ?? "",
            Celular = row.Celular ?? "",
            Nacionalidad = row.Nacionalidad,
            Fecha_Nacimiento = row.Fecha_Nacimiento,
            Tipo_Sangre = row.Tipo_Sangre ?? "",
            Email = row.Email ?? "",
            Direccion = row.Direccion ?? "",
            Provincia = row.Provincia,
            Municipio = row.Municipio,
            Sector = row.Sector,
            Alergico = row.Alergico ?? "",
            Medicamentos_que_usa = row.Medicamentos_que_usa ?? "",
            Codigo_Padre = row.Codigo_Padre ?? "",
            Parentesco = row.Parentesco,
            Estado = row.Estado ?? "A"
        };
    }

    // ═══ FOTO ═══

    public async Task<byte[]?> GetFotoAsync(int idEstudiante, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<byte[]?>(
            "SELECT Foto FROM Estudiante WHERE Idestudiante = @Id",
            new { Id = idEstudiante },
            commandType: CommandType.Text);
    }

    // ═══ CRUD ═══

    public async Task<int> InsertAsync(EstudianteSaveDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Codigo", dto.Codigo);
        p.Add("@Nombres", dto.Nombres);
        p.Add("@Apellido1", dto.Apellido1);
        p.Add("@Apellido2", dto.Apellido2);
        p.Add("@Sexo", dto.Sexo);
        p.Add("@Foto", dto.Foto ?? (object)DBNull.Value, DbType.Binary);
        p.Add("@Tipo_Doc", dto.Tipo_Doc);
        p.Add("@Num_Documento", dto.Num_Documento);
        p.Add("@Lugar_Nacimiento", dto.Lugar_Nacimiento);
        p.Add("@Telefono", dto.Telefono);
        p.Add("@Celular", dto.Celular);
        p.Add("@Nacionalidad", dto.Nacionalidad ?? (object)DBNull.Value);
        p.Add("@Fecha_Nacimiento", dto.Fecha_Nacimiento ?? (object)DBNull.Value, DbType.Date);
        p.Add("@Tipo_Sangre", dto.Tipo_Sangre);
        p.Add("@Email", dto.Email);
        p.Add("@Direccion", dto.Direccion);
        p.Add("@Provincia", dto.Provincia ?? (object)DBNull.Value);
        p.Add("@Municipio", dto.Municipio ?? (object)DBNull.Value);
        p.Add("@Sector", dto.Sector ?? (object)DBNull.Value);
        p.Add("@Alergico", dto.Alergico);
        p.Add("@Medicamentos_que_usa", dto.Medicamentos_que_usa);
        p.Add("@Idcliente", dto.Idcliente ?? (object)DBNull.Value);
        p.Add("@Parentesco", dto.Parentesco ?? (object)DBNull.Value);
        p.Add("@Estado", dto.Estado);

        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[InsertarEstudiante]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        // Obtener el ID recién insertado
        var id = await conn.ExecuteScalarAsync<int>(
            "SELECT SCOPE_IDENTITY()",
            commandType: CommandType.Text);

        return id;
    }

    public async Task UpdateAsync(int idEstudiante, EstudianteSaveDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        // Si no se envía foto nueva, conservar la actual
        byte[]? fotoFinal = dto.Foto;
        if (fotoFinal == null)
        {
            fotoFinal = await GetFotoAsync(idEstudiante, ct);
        }

        var p = new DynamicParameters();
        p.Add("@IdEstudiante", idEstudiante);
        p.Add("@Codigo", dto.Codigo);
        p.Add("@Nombres", dto.Nombres);
        p.Add("@Apellido1", dto.Apellido1);
        p.Add("@Apellido2", dto.Apellido2);
        p.Add("@Sexo", dto.Sexo);
        p.Add("@Foto", fotoFinal ?? (object)DBNull.Value, DbType.Binary);
        p.Add("@Tipo_Doc", dto.Tipo_Doc);
        p.Add("@Num_Documento", dto.Num_Documento);
        p.Add("@Lugar_Nacimiento", dto.Lugar_Nacimiento);
        p.Add("@Telefono", dto.Telefono);
        p.Add("@Celular", dto.Celular);
        p.Add("@Nacionalidad", dto.Nacionalidad ?? (object)DBNull.Value);
        p.Add("@Fecha_Nacimiento", dto.Fecha_Nacimiento ?? (object)DBNull.Value, DbType.Date);
        p.Add("@Tipo_Sangre", dto.Tipo_Sangre);
        p.Add("@Email", dto.Email);
        p.Add("@Direccion", dto.Direccion);
        p.Add("@Provincia", dto.Provincia ?? (object)DBNull.Value);
        p.Add("@Municipio", dto.Municipio ?? (object)DBNull.Value);
        p.Add("@Sector", dto.Sector ?? (object)DBNull.Value);
        p.Add("@Alergico", dto.Alergico);
        p.Add("@Medicamentos_que_usa", dto.Medicamentos_que_usa);
        p.Add("@Idcliente", dto.Idcliente ?? (object)DBNull.Value);
        p.Add("@Parentesco", dto.Parentesco ?? (object)DBNull.Value);
        p.Add("@Estado", dto.Estado);

        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[ModificarEstudiante]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task DeleteAsync(int idEstudiante, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[EliminarEstudiante]",
                new { Idestudiante = idEstudiante },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
}

// ═══ Clase interna para mapeo seguro de SQL ═══
internal class EstudianteSqlRow
{
    public int Idestudiante { get; set; }
    public string? Codigo { get; set; }
    public string? Nombres { get; set; }
    public string? Apellido1 { get; set; }
    public string? Apellido2 { get; set; }
    public string? Estudiante { get; set; }
    public string? Sexo { get; set; }
    public byte[]? Foto { get; set; }
    public int Tipo_Doc { get; set; }
    public string? Num_Documento { get; set; }
    public string? Lugar_Nacimiento { get; set; }
    public string? Telefono { get; set; }
    public string? Celular { get; set; }
    public int? Nacionalidad { get; set; }
    public DateTime? Fecha_Nacimiento { get; set; }
    public string? Tipo_Sangre { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public int? Provincia { get; set; }
    public int? Municipio { get; set; }
    public long? Sector { get; set; }
    public string? Alergico { get; set; }
    public string? Medicamentos_que_usa { get; set; }
    public string? Codigo_Padre { get; set; }
    public int? Parentesco { get; set; }
    public string? Estado { get; set; }
}