using TuVaca.Controllers;
using TuVaca.Models.Produccion;
using TuVaca.Models.Sanidad;
using TuVaca.Servicios;

namespace TuVaca.Tests
{
    /// <summary>CU-07 y CU-08 (vacunas) y CU-11 / CU-22 (producción de leche).</summary>
    public class SanidadYProduccionTests
    {
        private static SanidadController Sanidad(TuVacaDbContext db, int usuario = Prueba.ProductorId) =>
            Prueba.Preparar(new SanidadController(db, new NotificacionService(db), NullLogger<SanidadController>.Instance), usuario);

        private static ProduccionController Produccion(TuVacaDbContext db, int usuario = Prueba.ProductorId) =>
            Prueba.Preparar(new ProduccionController(db, NullLogger<ProduccionController>.Instance), usuario);

        private static RegistrarSanitarioViewModel Vacuna(int vacaId) => new()
        {
            VacaId = vacaId,
            TipoRegistroId = 1,
            Nombre = "Aftosa",
            FechaAplicacion = DateTime.Today,
            Responsable = "Dr. Pérez"
        };

        // ------------------------------ CU-08 ------------------------------

        [Fact]
        public async Task Historial_VacaAjena_NoSeMuestra_RN1()
        {
            var db = Prueba.CrearDb();
            var ajena = Prueba.AgregarVaca(db, Prueba.OtroProductorId);
            Assert.IsType<NotFoundResult>(await Sanidad(db).Historial(ajena.VacaId, null, null, null));
        }

        [Fact]
        public async Task Historial_OrdenadoDelMasRecienteYConFiltros()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            db.RegistrosSanitarios.AddRange(
                new RegistrosSanitarios { VacaId = vaca.VacaId, TipoRegistroId = 1, Nombre = "Vieja", FechaAplicacion = hoy.AddDays(-30), Responsable = "X", EstadoId = Catalogos.RegistroSanitarioActivo },
                new RegistrosSanitarios { VacaId = vaca.VacaId, TipoRegistroId = 2, Nombre = "Nueva", FechaAplicacion = hoy, Responsable = "Y", EstadoId = Catalogos.RegistroSanitarioAnulado });
            db.SaveChanges();

            var todos = (HistorialSanitarioViewModel)((ViewResult)await Sanidad(db).Historial(vaca.VacaId, null, null, null)).Model!;
            Assert.Equal("Nueva", todos.Registros[0].Nombre); // RN-2
            Assert.True(todos.Registros[0].Anulado);          // RN-3

            var vacunas = (HistorialSanitarioViewModel)((ViewResult)await Sanidad(db).Historial(vaca.VacaId, 1, null, null)).Model!;
            Assert.Single(vacunas.Registros);

            var rango = (HistorialSanitarioViewModel)((ViewResult)await Sanidad(db)
                .Historial(vaca.VacaId, null, DateTime.Today.AddDays(-5), DateTime.Today)).Model!;
            Assert.Single(rango.Registros);
        }

        // ------------------------------ CU-07 ------------------------------

        [Fact]
        public async Task Registrar_Get_VacaViva_MuestraFormulario()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            Assert.IsType<ViewResult>(await Sanidad(db).Registrar(vaca.VacaId));
        }

        [Fact]
        public async Task Registrar_Get_VacaNoViva_Redirige()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db, estadoId: Catalogos.VacaVendidaExterna);
            Assert.IsType<RedirectToActionResult>(await Sanidad(db).Registrar(vaca.VacaId));
            Assert.IsType<NotFoundResult>(await Sanidad(db).Registrar(9999));
        }

        [Fact]
        public async Task Registrar_Post_Valido_ConProximaDosis_ProgramaRecordatorio()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var modelo = Vacuna(vaca.VacaId);
            modelo.ProximaDosis = DateTime.Today.AddMonths(6);

            var resultado = await Sanidad(db).Registrar(modelo);

            Assert.Equal("Historial", Assert.IsType<RedirectToActionResult>(resultado).ActionName);
            Assert.Single(db.RegistrosSanitarios);
            var recordatorio = Assert.Single(db.Notificaciones);           // FA-2
            Assert.Equal(TiposNotificacionId.RecordatorioDosis, recordatorio.TipoNotificacionId);
            Assert.True(recordatorio.FechaProgramada > DateTime.Now);
        }

        [Fact]
        public async Task Registrar_Post_FechaFutura_FE2_RN3()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var modelo = Vacuna(vaca.VacaId);
            modelo.FechaAplicacion = DateTime.Today.AddDays(1);

            Assert.IsType<ViewResult>(await Sanidad(db).Registrar(modelo));
            Assert.Empty(db.RegistrosSanitarios);
        }

        [Fact]
        public async Task Registrar_Post_ProximaDosisAntesDeLaAplicacion_Error()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var modelo = Vacuna(vaca.VacaId);
            modelo.ProximaDosis = DateTime.Today.AddDays(-1);
            modelo.TipoRegistroId = 99; // tipo que no existe

            var controlador = Sanidad(db);
            Assert.IsType<ViewResult>(await controlador.Registrar(modelo));
            Assert.True(controlador.ModelState.ContainsKey(nameof(modelo.ProximaDosis)));
            Assert.True(controlador.ModelState.ContainsKey(nameof(modelo.TipoRegistroId)));
        }

        [Fact]
        public async Task Registrar_Post_VacaAjenaONoViva()
        {
            var db = Prueba.CrearDb();
            var ajena = Prueba.AgregarVaca(db, Prueba.OtroProductorId, "Z1");
            var muerta = Prueba.AgregarVaca(db, identificacion: "Z2", estadoId: Catalogos.VacaVendidaExterna);

            Assert.IsType<NotFoundResult>(await Sanidad(db).Registrar(Vacuna(ajena.VacaId)));
            Assert.IsType<ViewResult>(await Sanidad(db).Registrar(Vacuna(muerta.VacaId)));
            Assert.Empty(db.RegistrosSanitarios);
        }

        // ------------------------------ CU-11 ------------------------------

        [Fact]
        public async Task Produccion_Registrar_Nuevo()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            Assert.IsType<ViewResult>(await Produccion(db).Registrar(vaca.VacaId));

            var resultado = await Produccion(db).Registrar(new RegistrarProduccionViewModel
            {
                VacaId = vaca.VacaId,
                Fecha = DateTime.Today,
                CantidadLitros = 18.5m
            });

            Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal(18.5m, Assert.Single(db.ProduccionLeche).CantidadLitros);
        }

        [Fact]
        public async Task Produccion_MismoDia_PreguntaYLuegoActualiza_RN5_FA2()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            db.ProduccionLeche.Add(new ProduccionLeche { VacaId = vaca.VacaId, Fecha = DateOnly.FromDateTime(DateTime.Today), CantidadLitros = 10 });
            db.SaveChanges();

            var pregunta = await Produccion(db).Registrar(new RegistrarProduccionViewModel { VacaId = vaca.VacaId, Fecha = DateTime.Today, CantidadLitros = 12 });
            var modelo = Assert.IsType<RegistrarProduccionViewModel>(Assert.IsType<ViewResult>(pregunta).Model);
            Assert.True(modelo.YaExiste);
            Assert.Equal(10, modelo.CantidadAnterior);

            await Produccion(db).Registrar(new RegistrarProduccionViewModel { VacaId = vaca.VacaId, Fecha = DateTime.Today, CantidadLitros = 12, ConfirmarActualizacion = true });
            Assert.Equal(12, Assert.Single(db.ProduccionLeche).CantidadLitros);
        }

        [Fact]
        public async Task Produccion_FechaFutura_O_VacaNoViva()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var muerta = Prueba.AgregarVaca(db, identificacion: "M1", estadoId: Catalogos.VacaVendidaExterna);

            Assert.IsType<ViewResult>(await Produccion(db).Registrar(new RegistrarProduccionViewModel { VacaId = vaca.VacaId, Fecha = DateTime.Today.AddDays(2), CantidadLitros = 5 }));
            Assert.IsType<ViewResult>(await Produccion(db).Registrar(new RegistrarProduccionViewModel { VacaId = muerta.VacaId, Fecha = DateTime.Today, CantidadLitros = 5 }));
            Assert.IsType<RedirectToActionResult>(await Produccion(db).Registrar(muerta.VacaId));
            Assert.IsType<NotFoundResult>(await Produccion(db).Registrar(new RegistrarProduccionViewModel { VacaId = 9999, Fecha = DateTime.Today, CantidadLitros = 5 }));
            Assert.Empty(db.ProduccionLeche);
        }

        [Fact]
        public async Task Produccion_HistorialYResumen_TotalesYPromedios_RN6()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            Prueba.AgregarVaca(db, identificacion: "TORO", sexo: "Macho");
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            db.ProduccionLeche.AddRange(
                new ProduccionLeche { VacaId = vaca.VacaId, Fecha = hoy, CantidadLitros = 20 },
                new ProduccionLeche { VacaId = vaca.VacaId, Fecha = hoy.AddDays(-1), CantidadLitros = 10 },
                new ProduccionLeche { VacaId = vaca.VacaId, Fecha = hoy.AddDays(-60), CantidadLitros = 99 });
            db.SaveChanges();

            var historial = (HistorialProduccionViewModel)((ViewResult)await Produccion(db).Historial(vaca.VacaId, null, null)).Model!;
            Assert.Equal(30, historial.Total);   // últimos 30 días por defecto
            Assert.Equal(15, historial.Promedio);

            var resumen = (ResumenProduccionViewModel)((ViewResult)await Produccion(db).Index(DateTime.Today, DateTime.Today.AddDays(-90))).Model!;
            var item = Assert.Single(resumen.Vacas); // solo hembras
            Assert.Equal(129, item.Total);
            Assert.Equal(129, resumen.Total);

            Assert.IsType<NotFoundResult>(await Produccion(db, Prueba.OtroProductorId).Historial(vaca.VacaId, null, null));
        }

        [Fact]
        public async Task Produccion_Eliminar_CU22()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var registro = new ProduccionLeche { VacaId = vaca.VacaId, Fecha = DateOnly.FromDateTime(DateTime.Today), CantidadLitros = 8 };
            db.ProduccionLeche.Add(registro);
            db.SaveChanges();

            Assert.IsType<NotFoundResult>(await Produccion(db, Prueba.OtroProductorId).Eliminar(registro.ProduccionId));
            Assert.IsType<RedirectToActionResult>(await Produccion(db).Eliminar(registro.ProduccionId));
            Assert.Empty(db.ProduccionLeche);
        }
    }
}
