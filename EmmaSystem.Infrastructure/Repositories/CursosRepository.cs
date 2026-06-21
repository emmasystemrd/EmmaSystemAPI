using Dapper;
using EmmaSystem.Application.DTOs.Curso;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class CursosRepository : ICursosRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public CursosRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    // ═══ LISTADOS ═══

    public async Task<IReadOnlyList<CursoListadoDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<CursoListadoDto>(
            new CommandDefinition("[dbo].[MostrarCurso]",
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<CursoListadoDto>> SearchAsync(string texto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<CursoListadoDto>(
            new CommandDefinition("[dbo].[BuscarCurso]",
                new { TextoBuscar = texto },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<CursoListadoDto>> SearchActivosAsync(string texto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<CursoListadoDto>(
            new CommandDefinition("[dbo].[BuscarCurso_Activo]",
                new { TextoBuscar = texto },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<CursoListadoDto?> GetByCodigoAsync(string codigo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        return await conn.QueryFirstOrDefaultAsync<CursoListadoDto>(
            new CommandDefinition("[dbo].[BuscarCurso_Codigo]",
                new { TextoBuscar = codigo },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    // ═══ DETALLE COMPLETO ═══

    public async Task<CursoDetalleDto?> GetByIdAsync(int idCurso, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var curso = await conn.QueryFirstOrDefaultAsync(
            "SELECT Idcurso, Codigo, Curso, Descripcion, Nivel, Horario, Valor_Inscripcion, Valor_Mensual, " +
            "Tipo_Duracion, Duracion, Cupo_Maximo, Idinstructor, Estado " +
            "FROM Curso WHERE Idcurso = @Idcurso AND Estado != 'E'",
            new { Idcurso = idCurso },
            commandType: CommandType.Text);

        if (curso == null) return null;

        var detalles = await conn.QueryAsync<CursoDetalleItemDto>(
            new CommandDefinition("[dbo].[MostrarDetalleCurso]",
                new { Idcurso = idCurso },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return new CursoDetalleDto
        {
            Idcurso = (int)curso.Idcurso,
            Codigo = (string?)curso.Codigo ?? "",
            Curso = (string?)curso.Curso ?? "",
            Descripcion = (string?)curso.Descripcion ?? "",
            Nivel = (string?)curso.Nivel ?? "",
            Horario = (string?)curso.Horario ?? "",
            Valor_Inscripcion = (decimal)curso.Valor_Inscripcion,
            Valor_Mensual = (decimal)curso.Valor_Mensual,
            Tipo_Duracion = (string?)curso.Tipo_Duracion ?? "",
            Duracion = (int)curso.Duracion,
            Cupo_Maximo = (int)curso.Cupo_Maximo,
            Idinstructor = (int)curso.Idinstructor,
            Estado = (string?)curso.Estado ?? "A",
            Detalles = detalles.ToList()
        };
    }

    // ═══ CRUD ═══

    public async Task<int> InsertAsync(CursoSaveDto curso, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            var p = new DynamicParameters();
            p.Add("@Idcurso", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@Codigo", curso.Codigo);
            p.Add("@Curso", curso.Curso);
            p.Add("@Descripcion", curso.Descripcion);
            p.Add("@Nivel", curso.Nivel);
            p.Add("@Horario", curso.Horario);
            p.Add("@Valor_Inscripcion", curso.Valor_Inscripcion);
            p.Add("@Valor_Mensual", curso.Valor_Mensual);
            p.Add("@Tipo_Duracion", curso.Tipo_Duracion);
            p.Add("@Duracion", curso.Duracion);
            p.Add("@Cupo_Maximo", curso.Cupo_Maximo);
            p.Add("@Idinstructor", curso.Idinstructor);
            p.Add("@Estado", curso.Estado);

            await conn.ExecuteAsync(
                new CommandDefinition("[dbo].[InsertarCurso]", p,
                    transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));

            var idCurso = p.Get<int>("@Idcurso");

            if (curso.Detalles != null && curso.Detalles.Any())
            {
                foreach (var detalle in curso.Detalles)
                {
                    await conn.ExecuteAsync(
                        new CommandDefinition("[dbo].[InsertarDetalleCurso]",
                            new { Idcurso = idCurso, Nombre = detalle.Nombre },
                            transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                }
            }

            transaction.Commit();
            return idCurso;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(int idCurso, CursoSaveDto curso, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            var p = new DynamicParameters();
            p.Add("@Idcurso", idCurso);
            p.Add("@Codigo", curso.Codigo);
            p.Add("@Curso", curso.Curso);
            p.Add("@Descripcion", curso.Descripcion);
            p.Add("@Nivel", curso.Nivel);
            p.Add("@Horario", curso.Horario);
            p.Add("@Valor_Inscripcion", curso.Valor_Inscripcion);
            p.Add("@Valor_Mensual", curso.Valor_Mensual);
            p.Add("@Tipo_Duracion", curso.Tipo_Duracion);
            p.Add("@Duracion", curso.Duracion);
            p.Add("@Cupo_Maximo", curso.Cupo_Maximo);
            p.Add("@Idinstructor", curso.Idinstructor);
            p.Add("@Estado", curso.Estado);

            await conn.ExecuteAsync(
                new CommandDefinition("[dbo].[ModificarCurso]", p,
                    transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));

            await conn.ExecuteAsync(
                "DELETE FROM Detalle_Curso WHERE Idcurso = @Idcurso",
                new { Idcurso = idCurso }, transaction: transaction);

            if (curso.Detalles != null && curso.Detalles.Any())
            {
                foreach (var detalle in curso.Detalles)
                {
                    await conn.ExecuteAsync(
                        new CommandDefinition("[dbo].[InsertarDetalleCurso]",
                            new { Idcurso = idCurso, Nombre = detalle.Nombre },
                            transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteAsync(int idCurso, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[EliminarCurso]",
                new { Idcurso = idCurso },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<int> GetNextSecuenciaAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition("[dbo].[SecuenciaCurso]",
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    // ═══ DETALLES ═══

    public async Task<IReadOnlyList<CursoDetalleItemDto>> GetDetallesAsync(int idCurso, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<CursoDetalleItemDto>(
            new CommandDefinition("[dbo].[MostrarDetalleCurso]",
                new { Idcurso = idCurso },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }
}