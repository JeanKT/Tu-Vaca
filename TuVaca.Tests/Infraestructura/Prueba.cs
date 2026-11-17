using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using TuVaca.Seguridad;

namespace TuVaca.Tests.Infraestructura
{
    /// <summary>
    /// Ayudas para las pruebas: base de datos en memoria con datos de ejemplo
    /// y controladores con un usuario que "inició sesión".
    /// </summary>
    public static class Prueba
    {
        public const int ProductorId = 1;
        public const int CompradorId = 2;
        public const int AdminId = 3;
        public const int OtroProductorId = 4;

        public const int DepartamentoId = 10;
        public const int OtroDepartamentoId = 11;
        public const int MunicipioId = 100;
        public const int OtroMunicipioId = 101;

        public const string ClaveValida = "Clave123!";

        /// <summary>Base nueva en memoria (una por prueba) con roles, estados, catálogos y 4 usuarios.</summary>
        public static TuVacaDbContext CrearDb()
        {
            var opciones = new DbContextOptionsBuilder<TuVacaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var db = new TuVacaDbContext(opciones);
            Sembrar(db);
            return db;
        }

        private static void Sembrar(TuVacaDbContext db)
        {
            db.Roles.AddRange(
                new Roles { RolId = 1, NombreRol = Catalogos.RolProductor },
                new Roles { RolId = 2, NombreRol = Catalogos.RolComprador },
                new Roles { RolId = 3, NombreRol = Catalogos.RolAdministrador });

            (int id, string nombre, string ambito)[] estados =
            {
                (1, "Activo", "USUARIO"), (2, "Inactivo", "USUARIO"),
                (10, "Viva", "VACA"), (11, "Fallecida", "VACA"), (12, "Vendida en plataforma", "VACA"), (13, "Vendida externamente", "VACA"),
                (20, "Activa", "PUBLICACION"), (21, "Pausada", "PUBLICACION"), (22, "Vendida", "PUBLICACION"),
                (23, "Arrendada", "PUBLICACION"), (24, "Rechazada", "PUBLICACION"), (25, "Cancelada", "PUBLICACION"),
                (26, "Eliminada", "PUBLICACION"), (30, "Activo", "REGISTRO_SANITARIO"), (31, "Anulado", "REGISTRO_SANITARIO"),
                (40, "No leída", "NOTIFICACION"), (41, "Leída", "NOTIFICACION")
            };
            foreach (var e in estados)
            {
                db.Estados.Add(new Estados { EstadoId = e.id, Nombre = e.nombre, Ambito = e.ambito });
            }

            db.Departamentos.AddRange(
                new Departamentos { DepartamentoId = DepartamentoId, CodigoDane = "25", Nombre = "Cundinamarca" },
                new Departamentos { DepartamentoId = OtroDepartamentoId, CodigoDane = "05", Nombre = "Antioquia" });
            db.Municipios.AddRange(
                new Municipios { MunicipioId = MunicipioId, CodigoDane = "25269", Nombre = "Facatativá", DepartamentoId = DepartamentoId },
                new Municipios { MunicipioId = OtroMunicipioId, CodigoDane = "05001", Nombre = "Medellín", DepartamentoId = OtroDepartamentoId });

            db.TiposRegistroSanitario.AddRange(
                new TiposRegistroSanitario { TipoRegistroId = 1, Nombre = "Vacuna" },
                new TiposRegistroSanitario { TipoRegistroId = 2, Nombre = "Tratamiento" });

            for (var i = 1; i <= 7; i++)
            {
                db.TiposNotificacion.Add(new TiposNotificacion { TipoNotificacionId = i, Nombre = $"Tipo {i}" });
            }

            var hash = Contrasenas.Hashear(ClaveValida);
            db.Usuarios.AddRange(
                Usuario(ProductorId, "Jean Productor", "1001", "productor@tuvaca.com", 1, hash, "3001234567"),
                Usuario(CompradorId, "Carla Compradora", "1002", "comprador@tuvaca.com", 2, hash, null),
                Usuario(AdminId, "Ana Admin", "1003", "admin@tuvaca.com", 3, hash, null),
                Usuario(OtroProductorId, "Pedro Ganadero", "1004", "pedro@tuvaca.com", 1, hash, "3109876543"));

            db.SaveChanges();
        }

        private static Usuarios Usuario(int id, string nombre, string doc, string correo, int rol, string hash, string? tel) => new()
        {
            UsuarioId = id,
            NombreCompleto = nombre,
            NumeroDocumento = doc,
            Correo = correo,
            ContrasenaHash = hash,
            Telefono = tel,
            RolId = rol,
            EstadoId = Catalogos.UsuarioActivo,
            FechaRegistro = DateTime.Now.AddDays(-10)
        };

        public static Vacas AgregarVaca(TuVacaDbContext db, int usuarioId = ProductorId, string identificacion = "CO-001",
            string raza = "Holstein", string sexo = "Hembra", int estadoId = Catalogos.VacaViva,
            DateOnly? nacimiento = null, string? foto = "/uploads/vacas/vaca.jpg", decimal peso = 450)
        {
            var vaca = new Vacas
            {
                UsuarioId = usuarioId,
                Identificacion = identificacion,
                Raza = raza,
                Sexo = sexo,
                FechaNacimiento = nacimiento ?? DateOnly.FromDateTime(DateTime.Today.AddYears(-3)),
                Peso = peso,
                FotoUrl = foto,
                EstadoId = estadoId,
                FechaRegistro = DateTime.Now
            };
            db.Vacas.Add(vaca);
            db.SaveChanges();
            return vaca;
        }

        public static PublicacionesVaca AgregarPublicacion(TuVacaDbContext db, Vacas vaca, decimal precio = 3_000_000,
            int estadoId = Catalogos.PublicacionActiva, int municipioId = MunicipioId,
            string descripcion = "Vaca lechera muy mansa", DateTime? fecha = null)
        {
            var publicacion = new PublicacionesVaca
            {
                VacaId = vaca.VacaId,
                Precio = precio,
                Descripcion = descripcion,
                MunicipioId = municipioId,
                FotoUrl = vaca.FotoUrl ?? "/uploads/publicaciones/p.jpg",
                EstadoId = estadoId,
                FechaPublicacion = fecha ?? DateTime.Now
            };
            db.PublicacionesVaca.Add(publicacion);
            db.SaveChanges();
            return publicacion;
        }

        /// <summary>Deja el controlador listo: HttpContext, usuario con sesión (si se indica), TempData y Url.</summary>
        public static T Preparar<T>(T controlador, int? usuarioId = null, string? rol = null, string nombre = "Usuario de prueba")
            where T : Controller
        {
            var servicios = new ServiceCollection();
            servicios.AddSingleton<IAuthenticationService, AutenticacionFalsa>();
            var http = new DefaultHttpContext { RequestServices = servicios.BuildServiceProvider() };

            if (usuarioId != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, usuarioId.Value.ToString()),
                    new(ClaimTypes.Name, nombre),
                    new(ClaimTypes.Role, rol ?? Catalogos.RolProductor),
                    new(UsuarioActual.ClaimSesion, "token-prueba")
                };
                http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
            }

            controlador.ControllerContext = new ControllerContext { HttpContext = http };
            controlador.TempData = new TempDataDictionary(http, new TempDataFalso());
            controlador.Url = new UrlFalsa();
            return controlador;
        }

        public static IConfiguration ConfiguracionVacia() => new ConfigurationBuilder().Build();

        public static FakeEntorno Entorno() => new();

        /// <summary>Archivo subido de mentiras (por ejemplo una foto).</summary>
        public static IFormFile Archivo(string nombre, string tipo, int bytes = 1024)
        {
            var flujo = new MemoryStream(new byte[bytes]);
            return new FormFile(flujo, 0, bytes, "Foto", nombre)
            {
                Headers = new HeaderDictionary(),
                ContentType = tipo
            };
        }
    }

    public class AutenticacionFalsa : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }

    public class TempDataFalso : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    public class UrlFalsa : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();
        public string? Action(UrlActionContext actionContext) => $"/{actionContext.Controller}/{actionContext.Action}";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//");
        public string? Link(string? routeName, object? values) => "http://localhost/";
        public string? RouteUrl(UrlRouteContext routeContext) => "/";
    }

    public class FakeEntorno : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "tuvaca-pruebas", Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "TUVACA";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
    }
}
