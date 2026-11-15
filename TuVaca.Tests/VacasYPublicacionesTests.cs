using TuVaca.Controllers;
using TuVaca.Models.Hato;
using TuVaca.Models.Publicaciones;

namespace TuVaca.Tests
{
    /// <summary>CU-02, CU-12 (vacas) y CU-03, CU-16, CU-17 (publicaciones).</summary>
    public class VacasYPublicacionesTests
    {
        private static VacasController Vacas(TuVacaDbContext db, int usuario = Prueba.ProductorId) =>
            Prueba.Preparar(new VacasController(db, Prueba.Entorno(), NullLogger<VacasController>.Instance), usuario);

        private static PublicacionesController Publicaciones(TuVacaDbContext db, int usuario = Prueba.ProductorId) =>
            Prueba.Preparar(new PublicacionesController(db, Prueba.Entorno(), NullLogger<PublicacionesController>.Instance), usuario);

        private static RegistrarVacaViewModel NuevaVaca(string id = "NUEVA-1") => new()
        {
            Identificacion = id,
            Raza = "Jersey",
            Sexo = "Hembra",
            EdadMeses = 30,
            Peso = 380
        };

        // ------------------------------ CU-02 ------------------------------

        [Fact]
        public async Task MisVacas_SoloLasDelUsuario_RN3()
        {
            var db = Prueba.CrearDb();
            Prueba.AgregarVaca(db, identificacion: "MIA");
            Prueba.AgregarVaca(db, Prueba.OtroProductorId, "AJENA");

            var lista = Assert.IsType<List<VacaListadoItem>>(Assert.IsType<ViewResult>(await Vacas(db).Index()).Model);
            Assert.Equal("MIA", Assert.Single(lista).Identificacion);
        }

        [Fact]
        public async Task Registrar_ConEdadAproximada_YFoto()
        {
            var db = Prueba.CrearDb();
            Assert.IsType<ViewResult>(await Vacas(db).Registrar());
            var modelo = NuevaVaca();
            modelo.Foto = Prueba.Archivo("vaca.jpg", "image/jpeg");
            modelo.MunicipioOrigenId = Prueba.MunicipioId;

            var resultado = await Vacas(db).Registrar(modelo);

            Assert.IsType<RedirectToActionResult>(resultado);
            var vaca = Assert.Single(db.Vacas);
            Assert.Equal(Prueba.ProductorId, vaca.UsuarioId);
            Assert.Equal(Catalogos.VacaViva, vaca.EstadoId);
            Assert.StartsWith("/uploads/vacas/", vaca.FotoUrl);
        }

        [Fact]
        public async Task Registrar_IdentificacionDuplicada_FE3()
        {
            var db = Prueba.CrearDb();
            Prueba.AgregarVaca(db, identificacion: "NUEVA-1");
            var controlador = Vacas(db);

            Assert.IsType<ViewResult>(await controlador.Registrar(NuevaVaca()));
            Assert.True(controlador.ModelState.ContainsKey(nameof(RegistrarVacaViewModel.Identificacion)));
            Assert.Single(db.Vacas);
        }

        [Fact]
        public async Task Registrar_DatosInvalidos_FE2()
        {
            var db = Prueba.CrearDb();
            var sinEdad = NuevaVaca();
            sinEdad.EdadMeses = null;
            var futura = NuevaVaca();
            futura.FechaNacimiento = DateTime.Today.AddDays(3);
            var fotoMala = NuevaVaca();
            fotoMala.Foto = Prueba.Archivo("virus.exe", "application/octet-stream");
            fotoMala.MunicipioOrigenId = 55555;

            Assert.IsType<ViewResult>(await Vacas(db).Registrar(sinEdad));
            Assert.IsType<ViewResult>(await Vacas(db).Registrar(futura));
            Assert.IsType<ViewResult>(await Vacas(db).Registrar(fotoMala));
            Assert.Empty(db.Vacas);
        }

        // ------------------------------ CU-12 ------------------------------

        [Fact]
        public async Task Editar_Get_PrecargaDatos_SinIdentificacionEditable()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            db.Vacas.First().MunicipioOrigenId = Prueba.MunicipioId;
            db.SaveChanges();

            var modelo = Assert.IsType<EditarVacaViewModel>(Assert.IsType<ViewResult>(await Vacas(db).Editar(vaca.VacaId)).Model);
            Assert.Equal("CO-001", modelo.Identificacion);
            Assert.Equal(Prueba.DepartamentoId, modelo.DepartamentoId);

            Assert.IsType<NotFoundResult>(await Vacas(db, Prueba.OtroProductorId).Editar(vaca.VacaId)); // RN-1
        }

        [Fact]
        public async Task Editar_Post_ActualizaYCambiaFotoDeLaPublicacion_RN4()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var publicacion = Prueba.AgregarPublicacion(db, vaca);

            var resultado = await Vacas(db).Editar(new EditarVacaViewModel
            {
                VacaId = vaca.VacaId,
                Raza = "Normando",
                Sexo = "Hembra",
                FechaNacimiento = DateTime.Today.AddYears(-4),
                Peso = 500,
                Foto = Prueba.Archivo("nueva.png", "image/png")
            });

            Assert.IsType<RedirectToActionResult>(resultado);
            var editada = db.Vacas.Single();
            Assert.Equal("Normando", editada.Raza);
            Assert.Equal("CO-001", editada.Identificacion); // RN-2
            Assert.NotNull(editada.FechaActualizacion);     // RN-5
            Assert.Equal(editada.FotoUrl, db.PublicacionesVaca.Single(p => p.PublicacionVacaId == publicacion.PublicacionVacaId).FotoUrl);
        }

        [Fact]
        public async Task Editar_VacaNoViva_O_DatosInvalidos()
        {
            var db = Prueba.CrearDb();
            var vendida = Prueba.AgregarVaca(db, estadoId: Catalogos.VacaVendidaExterna);
            var viva = Prueba.AgregarVaca(db, identificacion: "V2");

            Assert.IsType<RedirectToActionResult>(await Vacas(db).Editar(vendida.VacaId));
            Assert.IsType<ViewResult>(await Vacas(db).Editar(new EditarVacaViewModel
            {
                VacaId = viva.VacaId, Raza = "X", Sexo = "Hembra", FechaNacimiento = DateTime.Today.AddDays(5), Peso = 300, MunicipioOrigenId = 777
            }));
            Assert.IsType<NotFoundResult>(await Vacas(db).Editar(new EditarVacaViewModel { VacaId = 9999 }));
        }

        // ------------------------------ CU-03 ------------------------------

        [Fact]
        public async Task Publicar_Get_PrecargaLaVaca()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            vaca.MunicipioOrigenId = Prueba.MunicipioId;
            db.SaveChanges();

            var modelo = Assert.IsType<PublicarVacaViewModel>(Assert.IsType<ViewResult>(await Publicaciones(db).Crear(vaca.VacaId)).Model);
            Assert.Equal("CO-001", modelo.Identificacion);
            Assert.True(modelo.UsarFotoDeLaVaca);
            Assert.Equal(Prueba.DepartamentoId, modelo.DepartamentoId);
        }

        [Fact]
        public async Task Publicar_Post_ConLaFotoDeLaVaca_QuedaActiva()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);

            var resultado = await Publicaciones(db).Crear(new PublicarVacaViewModel
            {
                VacaId = vaca.VacaId,
                Precio = 4_000_000,
                Descripcion = "Vaca de buen temperamento",
                DepartamentoId = Prueba.DepartamentoId,
                MunicipioId = Prueba.MunicipioId,
                UsarFotoDeLaVaca = true
            });

            Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal(Catalogos.PublicacionActiva, Assert.Single(db.PublicacionesVaca).EstadoId);
        }

        [Fact]
        public async Task Publicar_YaPublicada_O_SinFoto_FE2_FE3()
        {
            var db = Prueba.CrearDb();
            var publicada = Prueba.AgregarVaca(db);
            Prueba.AgregarPublicacion(db, publicada);
            var sinFoto = Prueba.AgregarVaca(db, identificacion: "SF", foto: null);

            Assert.IsType<RedirectToActionResult>(await Publicaciones(db).Crear(publicada.VacaId));
            Assert.IsType<NotFoundResult>(await Publicaciones(db, Prueba.OtroProductorId).Crear(publicada.VacaId));

            var modelo = new PublicarVacaViewModel
            {
                VacaId = sinFoto.VacaId, Precio = 1000, Descripcion = "Descripción suficiente", DepartamentoId = Prueba.DepartamentoId, MunicipioId = 4242
            };
            var controlador = Publicaciones(db);
            Assert.IsType<ViewResult>(await controlador.Crear(modelo));
            Assert.True(controlador.ModelState.ContainsKey(nameof(modelo.FotoNueva)));
            Assert.True(controlador.ModelState.ContainsKey(nameof(modelo.MunicipioId)));
        }

        [Fact]
        public async Task MisPublicaciones_PausarYReanudar()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var p = Prueba.AgregarPublicacion(db, vaca);

            var lista = Assert.IsType<List<PublicacionListadoItem>>(Assert.IsType<ViewResult>(await Publicaciones(db).Index()).Model);
            Assert.Single(lista);

            await Publicaciones(db).Pausar(p.PublicacionVacaId);
            Assert.Equal(Catalogos.PublicacionPausada, db.PublicacionesVaca.Single().EstadoId);
            await Publicaciones(db).Pausar(p.PublicacionVacaId); // ya no está activa: no cambia
            await Publicaciones(db).Reanudar(p.PublicacionVacaId);
            Assert.Equal(Catalogos.PublicacionActiva, db.PublicacionesVaca.Single().EstadoId);
            await Publicaciones(db).Reanudar(p.PublicacionVacaId); // ya está activa: no cambia

            Assert.IsType<NotFoundResult>(await Publicaciones(db, Prueba.OtroProductorId).Pausar(p.PublicacionVacaId));
            Assert.IsType<NotFoundResult>(await Publicaciones(db, Prueba.OtroProductorId).Reanudar(p.PublicacionVacaId));
        }

        // --------------------------- CU-16 y CU-17 ---------------------------

        [Fact]
        public async Task EditarPublicacion_CambiaPrecioYDescripcion()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var p = Prueba.AgregarPublicacion(db, vaca);

            var get = Assert.IsType<EditarPublicacionViewModel>(Assert.IsType<ViewResult>(await Publicaciones(db).Editar(p.PublicacionVacaId)).Model);
            Assert.Equal(Prueba.DepartamentoId, get.DepartamentoId);

            var resultado = await Publicaciones(db).Editar(new EditarPublicacionViewModel
            {
                PublicacionVacaId = p.PublicacionVacaId,
                Precio = 999_000,
                Descripcion = "Nueva descripción de la vaca",
                DepartamentoId = Prueba.OtroDepartamentoId,
                MunicipioId = Prueba.OtroMunicipioId,
                FotoNueva = Prueba.Archivo("otra.webp", "image/webp")
            });

            Assert.IsType<RedirectToActionResult>(resultado);
            var editada = db.PublicacionesVaca.Single();
            Assert.Equal(999_000, editada.Precio);
            Assert.Equal(Prueba.OtroMunicipioId, editada.MunicipioId);
            Assert.NotNull(editada.FechaActualizacion); // RN-5
        }

        [Fact]
        public async Task EditarPublicacion_Vendida_NoSePuede_RN4()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var p = Prueba.AgregarPublicacion(db, vaca, estadoId: Catalogos.PublicacionVendida);

            Assert.IsType<RedirectToActionResult>(await Publicaciones(db).Editar(p.PublicacionVacaId));
            Assert.IsType<ViewResult>(await Publicaciones(db).Editar(new EditarPublicacionViewModel
            {
                PublicacionVacaId = p.PublicacionVacaId, Precio = 1, Descripcion = "Descripción suficiente", MunicipioId = 31337
            }));
            Assert.IsType<NotFoundResult>(await Publicaciones(db).Editar(9999));
            Assert.IsType<NotFoundResult>(await Publicaciones(db).Editar(new EditarPublicacionViewModel { PublicacionVacaId = 9999 }));
        }

        [Fact]
        public async Task EliminarPublicacion_LaVacaSeConserva_RN3()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            var p = Prueba.AgregarPublicacion(db, vaca);
            var vendida = Prueba.AgregarPublicacion(db, Prueba.AgregarVaca(db, identificacion: "V9"), estadoId: Catalogos.PublicacionVendida);

            await Publicaciones(db).Eliminar(p.PublicacionVacaId);
            Assert.Equal(Catalogos.PublicacionCancelada, db.PublicacionesVaca.Single(x => x.PublicacionVacaId == p.PublicacionVacaId).EstadoId);
            Assert.Equal(2, db.Vacas.Count());

            await Publicaciones(db).Eliminar(vendida.PublicacionVacaId); // RN-2: vendida no se elimina
            Assert.Equal(Catalogos.PublicacionVendida, db.PublicacionesVaca.Single(x => x.PublicacionVacaId == vendida.PublicacionVacaId).EstadoId);
            Assert.IsType<NotFoundResult>(await Publicaciones(db, Prueba.OtroProductorId).Eliminar(p.PublicacionVacaId));
        }
    }
}
