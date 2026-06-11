using EmmaSystem.API.Filters;
using EmmaSystem.API.Middleware;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Application.Services;
using EmmaSystem.Infrastructure.Data;
using EmmaSystem.Infrastructure.Repositories;
using EmmaSystem.Infrastructure.Services;
using EmmaSystem.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
namespace EmmaSystem.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ──────────────────────────────────────────────
            // 1) Servicios Core (Controllers y API Explorer)
            // ──────────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // ──────────────────────────────────────────────
            // 2) Dapper: Factoría de conexiones Scoped
            //    (Cada request HTTP obtiene su propia SqlConnection)
            // ──────────────────────────────────────────────
            builder.Services.AddScoped<SqlConnectionFactory>();

            // ──────────────────────────────────────────────
            // 3) Inyección de Repositorios y Servicios
            // ──────────────────────────────────────────────
            builder.Services.AddScoped<AuthRepository>();
            builder.Services.AddScoped<PermissionRepository>();
            builder.Services.AddScoped<EmpresaRepository>();
            builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();

            // DEPARTAMENTO
            builder.Services.AddScoped<DepartamentoRepository>();
            builder.Services.AddScoped<IDepartamentoRepository, DepartamentoRepository>();

            // CLIENTE
            builder.Services.AddScoped<ClienteRepository>();
            builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

            // PROVEEDOR
            builder.Services.AddScoped<ProveedorRepository>();
            builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();

            // ARTICULO
            builder.Services.AddScoped<ArticuloRepository>();
            builder.Services.AddScoped<IArticuloRepository, ArticuloRepository>();
            
            //DETALLE PRODUCTO
            builder.Services.AddScoped<DetalleProductoRepository>();
            builder.Services.AddScoped<IDetalleProductoRepository, DetalleProductoRepository>();

            //MEDIDA
            builder.Services.AddScoped<MedidaRepository>();
            builder.Services.AddScoped<IMedidaRepository, MedidaRepository>();

            //CATEGORIA
            builder.Services.AddScoped<CategoriaRepository>();
            builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();

            //TERMINO
            builder.Services.AddScoped<TerminoRepository>();
            builder.Services.AddScoped<ITerminoRepository, TerminoRepository>();

            //COTIZACION
            builder.Services.AddScoped<CotizacionRepository>();
            builder.Services.AddScoped<ICotizacionRepository, CotizacionRepository>();

            // VENTAS
            builder.Services.AddScoped<VentaRepository>();
            builder.Services.AddScoped<IVentaRepository, VentaRepository>();

            // UBICACION
            builder.Services.AddScoped<UbicacionRepository>();
            builder.Services.AddScoped<IUbicacionRepository, UbicacionRepository>();

            //AGREGAR NCF
            builder.Services.AddScoped<EnCFRepository>();
            builder.Services.AddScoped<IEnCFRepository, EnCFRepository>();

            //CONFIGURACION
            builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
            //CURSOS
            builder.Services.AddScoped<ICursosRepository, CursosRepository>();
            //Estudantes
            builder.Services.AddScoped<IEstudiantesRepository, EstudiantesRepository>();
            //Inscripciones
            builder.Services.AddScoped<IInscripcionesRepository, InscripcionesRepository>();
            builder.Services.AddScoped<IAsistenciasRepository, AsistenciasRepository>();

            builder.Services.AddScoped<ICobrosRepository, CobrosRepository>();

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPermissionService, PermissionService>();

            // Registrar la configuración de encriptación
            builder.Services.Configure<EncryptionSettings>(
                builder.Configuration.GetSection("Encryption"));

            // 1️⃣ Agrega esta línea para habilitar el caché en memoria
            builder.Services.AddMemoryCache();
            // ──────────────────────────────────────────────
            // 4) Filtro global de permisos (se usa con [Permission])
            // ──────────────────────────────────────────────
            builder.Services.AddScoped<PermissionAuthorizationFilter>();

            builder.Services.AddScoped<IDgiiService, DgiiService>();

            // ──────────────────────────────────────────────
            // 5) Configuración de Autenticación JWT
            // ──────────────────────────────────────────────
            var jwt = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false; // Cambiar a true en producción
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt["Issuer"],
                        ValidAudience = jwt["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            builder.Services.AddAuthorization();

            // ──────────────────────────────────────────────
            // 6) Swagger con soporte para JWT Bearer Token
            // ──────────────────────────────────────────────
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "EmmaSystem Web API",
                    Version = "v1",
                    Description = "ERP contable dominicano — backend REST sobre SQL Server + Dapper."
                });

                // Configuración del botón "Authorize" en Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Ingrese el token JWT. Ejemplo: eyJhbGci..."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
            });

            // ──────────────────────────────────────────────
            // 7) CORS (Para que React (Vite/Node) pueda consumir la API)
            // ──────────────────────────────────────────────
            builder.Services.AddCors(o => o.AddPolicy("ReactDev", p =>
                p.WithOrigins("http://localhost:5173", "http://localhost:3000")
                 .AllowAnyHeader()
                 .AllowAnyMethod()));

            var app = builder.Build();

            // ──────────────────────────────────────────────
            // Pipeline de Middleware (El orden es CRÍTICO)
            // ──────────────────────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // El middleware de SQL debe ir ANTES de la autenticación para capturar errores globales
            app.UseMiddleware<SqlExceptionMiddleware>();

            app.UseHttpsRedirection();
            app.UseCors("ReactDev");

            app.UseAuthentication(); // 1ro: ¿Quién eres? (JWT)
            app.UseAuthorization();  // 2do: ¿Qué puedes hacer? (Permisos)

            app.MapControllers();
            // ============================================
            // 6. ENDPOINT DE PRUEBA DE CONEXIÓN (HEALTH CHECK)
            // ============================================
            app.MapGet("/api/health/db", async (SqlConnectionFactory factory) =>
            {
                try
                {
                    using var connection = factory.CreateConnection();
                    await connection.OpenAsync();

                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT GETDATE() AS ServerTime, DB_NAME() AS DatabaseName";

                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return Results.Ok(new
                        {
                            Status = "✅ Conexión exitosa",
                            ServerTime = reader["ServerTime"],
                            DatabaseName = reader["DatabaseName"],
                            Message = "La base de datos está accesible y los triggers/SPs están intactos."
                        });
                    }

                    return Results.Problem("No se pudo leer la respuesta de la base de datos");
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "❌ Error de conexión a SQL Server",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithTags("Health Check"); // 👈 Solo dejamos WithTags, eliminamos WithOpenApi()
            app.Run();
        }
    }
}
