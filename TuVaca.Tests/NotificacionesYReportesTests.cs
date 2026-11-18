using TuVaca.Controllers;
using TuVaca.Models.Reportes;
using TuVaca.Servicios;

namespace TuVaca.Tests
{
    /// <summary>Campanita de notificaciones, controladores pequeños y reportes PDF/Excel.</summary>
    public class NotificacionesYReportesTests
    {
        static NotificacionesYReportesTests()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        // --------------------------- Notificaciones ---------------------------

        [Fact]
        public async Task Servicio_CrearContarListarYMarcar()
        {
            var db = Prueba.CrearDb();
            var servicio = new NotificacionService(db);
            await servicio.CrearAsync(Prueba.ProductorId, TiposNotificacionId.MensajeDeComprador, "Hola", "/Contactos/Recibidos");
            await servicio.CrearAsync(Prueba.ProductorId, TiposNotificacionId.VentaConfirmada, "Otra");
            await servicio.CrearAsync(Prueba.ProductorId, TiposNotificacionId.RecordatorioDosis, "Futura", null, DateTime.Now.AddDays(5));

            Assert.Equal(2, await servicio.ContarNoLeidasAsync(Prueba.ProductorId)); // la futura todavía no se ve
            Assert.Single(await servicio.ListarAsync(Prueba.ProductorId, 1));

            var primera = db.Notificaciones.First();
            Assert.NotNull(await servicio.MarcarLeidaAsync(primera.NotificacionId, Prueba.ProductorId));
            Assert.Null(await servicio.MarcarLeidaAsync(primera.NotificacionId, Prueba.CompradorId)); // no es suya
            Assert.Equal(1, await servicio.ContarNoLeidasAsync(Prueba.ProductorId));

            await servicio.MarcarTodasLeidasAsync(Prueba.ProductorId);
            Assert.Equal(0, await servicio.ContarNoLeidasAsync(Prueba.ProductorId));
        }

        [Fact]
        public async Task Controlador_Notificaciones()
        {
            var db = Prueba.CrearDb();
            var servicio = new NotificacionService(db);
            await servicio.CrearAsync(Prueba.ProductorId, TiposNotificacionId.MensajeDeComprador, "Hola", "/Contactos/Recibidos");
            await servicio.CrearAsync(Prueba.ProductorId, TiposNotificacionId.MensajeDeComprador, "Sin enlace");
            NotificacionesController C() => Prueba.Preparar(new NotificacionesController(servicio, Prueba.Entorno()), Prueba.ProductorId);

            Assert.IsType<List<NotificacionVista>>(Assert.IsType<ViewResult>(await C().Index()).Model);

            var conEnlace = db.Notificaciones.First(n => n.Enlace != null);
            var sinEnlace = db.Notificaciones.First(n => n.Enlace == null);
            Assert.Equal("/Contactos/Recibidos", Assert.IsType<LocalRedirectResult>(await C().Abrir(conEnlace.NotificacionId)).Url);
            Assert.IsType<RedirectToActionResult>(await C().Abrir(sinEnlace.NotificacionId));
            Assert.IsType<NotFoundResult>(await C().Abrir(9999));

            Assert.IsType<LocalRedirectResult>(await C().MarcarTodas("/Vacas"));
            Assert.IsType<RedirectToActionResult>(await C().MarcarTodas());
            Assert.IsType<RedirectToActionResult>(await C().Prueba());
            Assert.Equal(3, db.Notificaciones.Count());

            var produccion = Prueba.Preparar(new NotificacionesController(servicio, new FakeEntorno { EnvironmentName = "Production" }), Prueba.ProductorId);
            Assert.IsType<NotFoundResult>(await produccion.Prueba());
        }

        // ----------------------- Controladores pequeños -----------------------

        [Fact]
        public async Task Ubicaciones_PruebaBd_Home()
        {
            var db = Prueba.CrearDb();
            Assert.IsType<JsonResult>(await Prueba.Preparar(new UbicacionesController(db)).Municipios(Prueba.DepartamentoId));
            Assert.IsType<JsonResult>(await Prueba.Preparar(new PruebaBdController(db)).Index());
            Assert.IsType<ViewResult>(Prueba.Preparar(new TUVACA.Controllers.HomeController()).Index());
        }

        // ------------------------------ Reportes ------------------------------

        private static List<ReporteProduccionFila> FilasProduccion() => new()
        {
            new() { VacaID = 1, Identificacion = "A1", Raza = "Holstein", Estado = "Viva", DiasRegistrados = 2, TotalLitros = 30, PromedioLitros = 15, MinimoLitros = 10, MaximoLitros = 20 },
            new() { VacaID = 2, Identificacion = "A2", Raza = "Jersey", Estado = "Viva", DiasRegistrados = 0 }
        };

        [Fact]
        public void ReporteService_GeneraExcelValido()
        {
            var servicio = new ReporteService();
            var produccion = servicio.ProduccionExcel(FilasProduccion(), DateTime.Today.AddDays(-7), DateTime.Today, "Jean");
            var usuarios = servicio.UsuariosExcel(new List<UsuarioReporteFila>
            {
                new() { NombreCompleto = "Jean", NumeroDocumento = "1", Correo = "j@x.com", Rol = "Administrador", Estado = "Activo", FechaRegistro = DateTime.Now }
            });

            // Un .xlsx es un zip: empieza por "PK"
            Assert.Equal((byte)'P', produccion[0]);
            Assert.Equal((byte)'K', produccion[1]);
            Assert.Equal((byte)'P', usuarios[0]);
        }

        [Fact]
        public void ReporteService_GeneraPdfValido()
        {
            var servicio = new ReporteService();
            var produccion = servicio.ProduccionPdf(FilasProduccion(), DateTime.Today.AddDays(-7), DateTime.Today, "Jean");
            var vacio = servicio.ProduccionPdf(new List<ReporteProduccionFila>(), DateTime.Today, DateTime.Today, "Jean");
            var sanidad = servicio.SanidadPdf(new List<HistorialSanitarioFila>
            {
                new() { Tipo = "Vacuna", Nombre = "Aftosa", FechaAplicacion = DateOnly.FromDateTime(DateTime.Today), Responsable = "Dr", Estado = "Activo", ProximaDosis = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), EstadoProximaDosis = "Pendiente" },
                new() { Tipo = "Tratamiento", Nombre = "Ivermectina", FechaAplicacion = DateOnly.FromDateTime(DateTime.Today), Responsable = "Dr", Estado = "Anulado" }
            }, "A1", "Jean");
            var sanidadVacia = servicio.SanidadPdf(new List<HistorialSanitarioFila>(), "A1", "Jean");

            foreach (var pdf in new[] { produccion, vacio, sanidad, sanidadVacia })
            {
                Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
            }
        }

        [Fact]
        public async Task ReportesController_UsuariosExcel_Y_ManejoDeErrores()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            ReportesController C(int usuario, string rol) =>
                Prueba.Preparar(new ReportesController(db, new ReporteService(), NullLogger<ReportesController>.Instance), usuario, rol);

            var excel = Assert.IsType<FileContentResult>(await C(Prueba.AdminId, Catalogos.RolAdministrador).UsuariosExcel());
            Assert.EndsWith(".xlsx", excel.FileDownloadName);

            Assert.IsType<NotFoundResult>(await C(Prueba.OtroProductorId, Catalogos.RolProductor).SanidadPdf(vaca.VacaId));

            // La base en memoria no ejecuta procedimientos almacenados: se prueba que el error se maneje (FE-1)
            Assert.IsType<RedirectToActionResult>(await C(Prueba.ProductorId, Catalogos.RolProductor).ProduccionExcel(null, null));
            Assert.IsType<RedirectToActionResult>(await C(Prueba.ProductorId, Catalogos.RolProductor).ProduccionPdf(DateTime.Today, DateTime.Today.AddDays(-3)));
            Assert.IsType<RedirectToActionResult>(await C(Prueba.ProductorId, Catalogos.RolProductor).SanidadPdf(vaca.VacaId));
        }
    }
}
