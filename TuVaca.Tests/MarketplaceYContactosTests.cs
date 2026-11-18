using TuVaca.Controllers;
using TuVaca.Models.Contactos;
using TuVaca.Models.Marketplace;
using TuVaca.Servicios;

namespace TuVaca.Tests
{
    /// <summary>CU-05 Buscar y filtrar publicaciones y CU-06 Contactar vendedor.</summary>
    public class MarketplaceYContactosTests
    {
        private static MarketplaceController Marketplace(TuVacaDbContext db, int? usuario = null) =>
            Prueba.Preparar(new MarketplaceController(db, NullLogger<MarketplaceController>.Instance), usuario);

        private static ContactosController Contactos(TuVacaDbContext db, int usuario, string rol) =>
            Prueba.Preparar(new ContactosController(db, new NotificacionService(db),
                new CorreoService(Prueba.ConfiguracionVacia()), NullLogger<ContactosController>.Instance), usuario, rol, "Carla Compradora");

        private static (TuVacaDbContext db, PublicacionesVaca activa) Escenario()
        {
            var db = Prueba.CrearDb();
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var v1 = Prueba.AgregarVaca(db, identificacion: "A1", raza: "Holstein", nacimiento: hoy.AddYears(-2));
            var v2 = Prueba.AgregarVaca(db, identificacion: "A2", raza: "Jersey", nacimiento: hoy.AddYears(-6));
            var v3 = Prueba.AgregarVaca(db, Prueba.OtroProductorId, "B1", "Normando");
            var activa = Prueba.AgregarPublicacion(db, v1, 2_000_000, fecha: DateTime.Now.AddDays(-2));
            Prueba.AgregarPublicacion(db, v2, 5_000_000, municipioId: Prueba.OtroMunicipioId, descripcion: "Excelente productora", fecha: DateTime.Now.AddDays(-1));
            Prueba.AgregarPublicacion(db, v3, 1_000_000, estadoId: Catalogos.PublicacionPausada);
            return (db, activa);
        }

        private static MarketplaceIndexViewModel Buscar(TuVacaDbContext db, MarketplaceFiltros filtros)
        {
            var resultado = Marketplace(db).Index(filtros).Result;
            return Assert.IsType<MarketplaceIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        }

        [Fact]
        public void Index_SinFiltros_SoloMuestraActivas_RN1()
        {
            var (db, _) = Escenario();
            var modelo = Buscar(db, new MarketplaceFiltros());

            Assert.Equal(2, modelo.Resultados.Count);
            Assert.Equal("A2", db.Vacas.First(v => v.Raza == modelo.Resultados[0].Raza).Identificacion); // más reciente primero
            Assert.Equal(new[] { "Holstein", "Jersey" }, modelo.Razas);
        }

        [Fact]
        public void Index_FiltrosCombinados_RN3()
        {
            var (db, _) = Escenario();
            Assert.Single(Buscar(db, new MarketplaceFiltros { Raza = "Jersey" }).Resultados);
            Assert.Single(Buscar(db, new MarketplaceFiltros { PrecioMin = 1_500_000, PrecioMax = 3_000_000 }).Resultados);
            Assert.Single(Buscar(db, new MarketplaceFiltros { PrecioMin = 3_000_000, PrecioMax = 1_500_000 }).Resultados); // rango al revés
            Assert.Single(Buscar(db, new MarketplaceFiltros { DepartamentoId = Prueba.OtroDepartamentoId }).Resultados);
            Assert.Single(Buscar(db, new MarketplaceFiltros { MunicipioId = Prueba.MunicipioId }).Resultados);
            Assert.Single(Buscar(db, new MarketplaceFiltros { Q = "productora" }).Resultados);
            Assert.Empty(Buscar(db, new MarketplaceFiltros { Raza = "Jersey", MunicipioId = Prueba.MunicipioId }).Resultados); // FE-1
        }

        [Fact]
        public void Index_FiltroDeEdad()
        {
            var (db, _) = Escenario();
            Assert.Single(Buscar(db, new MarketplaceFiltros { EdadMinAnios = 5 }).Resultados);
            Assert.Single(Buscar(db, new MarketplaceFiltros { EdadMaxAnios = 3 }).Resultados);
            Assert.Equal(2, Buscar(db, new MarketplaceFiltros { EdadMinAnios = 10, EdadMaxAnios = 1 }).Resultados.Count); // se intercambia
        }

        [Fact]
        public void Index_OrdenarPorPrecio_RN4()
        {
            var (db, _) = Escenario();
            var asc = Buscar(db, new MarketplaceFiltros { Orden = "precio_asc" }).Resultados;
            var desc = Buscar(db, new MarketplaceFiltros { Orden = "precio_desc" }).Resultados;
            Assert.Equal(2_000_000, asc[0].Precio);
            Assert.Equal(5_000_000, desc[0].Precio);
        }

        [Fact]
        public async Task Detalle_Activa_MuestraDatosDelVendedor()
        {
            var (db, activa) = Escenario();
            var resultado = await Marketplace(db).Detalle(activa.PublicacionVacaId);

            var modelo = Assert.IsType<MarketplaceDetalleViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Equal("Jean Productor", modelo.VendedorNombre);
            Assert.Equal("3001234567", modelo.VendedorTelefono);
            Assert.Equal("Facatativá, Cundinamarca", modelo.Ubicacion);
        }

        [Fact]
        public async Task Detalle_NoActiva_RedirigeAlMarketplace()
        {
            var (db, _) = Escenario();
            var pausada = db.PublicacionesVaca.First(p => p.EstadoId == Catalogos.PublicacionPausada);
            var resultado = await Marketplace(db).Detalle(pausada.PublicacionVacaId);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        }

        // ------------------------------ CU-06 ------------------------------

        [Fact]
        public async Task Contactar_Get_PublicacionActiva_MuestraFormulario()
        {
            var (db, activa) = Escenario();
            var resultado = await Contactos(db, Prueba.CompradorId, Catalogos.RolComprador).Crear(activa.PublicacionVacaId);
            var modelo = Assert.IsType<ContactarViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Equal("Jean Productor", modelo.VendedorNombre);
        }

        [Fact]
        public async Task Contactar_Get_PublicacionNoDisponible_FE3()
        {
            var (db, _) = Escenario();
            var resultado = await Contactos(db, Prueba.CompradorId, Catalogos.RolComprador).Crear(9999);
            Assert.Equal("Marketplace", Assert.IsType<RedirectToActionResult>(resultado).ControllerName);
        }

        [Fact]
        public async Task Contactar_Post_RegistraMensajeYNotificaAlVendedor()
        {
            var (db, activa) = Escenario();
            var controlador = Contactos(db, Prueba.CompradorId, Catalogos.RolComprador);

            var resultado = await controlador.Crear(new ContactarViewModel
            {
                PublicacionVacaId = activa.PublicacionVacaId,
                Mensaje = "Hola, me interesa la vaca"
            });

            Assert.Equal("Detalle", Assert.IsType<RedirectToActionResult>(resultado).ActionName);
            var contacto = Assert.Single(db.Contactos);
            Assert.Equal(Prueba.CompradorId, contacto.CompradorId); // RN-3
            var notificacion = Assert.Single(db.Notificaciones);
            Assert.Equal(Prueba.ProductorId, notificacion.UsuarioId);
            Assert.Equal("Tu mensaje fue enviado al vendedor.", controlador.TempData["Exito"]);
        }

        [Fact]
        public async Task Contactar_Post_MensajeVacio_FE2()
        {
            var (db, activa) = Escenario();
            var resultado = await Contactos(db, Prueba.CompradorId, Catalogos.RolComprador)
                .Crear(new ContactarViewModel { PublicacionVacaId = activa.PublicacionVacaId, Mensaje = "   " });

            Assert.IsType<ViewResult>(resultado);
            Assert.Empty(db.Contactos);
        }

        [Fact]
        public async Task Contactar_Post_PropiaPublicacion_NoSePermite()
        {
            var (db, activa) = Escenario();
            var resultado = await Contactos(db, Prueba.ProductorId, Catalogos.RolProductor)
                .Crear(new ContactarViewModel { PublicacionVacaId = activa.PublicacionVacaId, Mensaje = "Hola hola" });

            Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Empty(db.Contactos);
        }

        [Fact]
        public async Task Contactar_Post_PublicacionInactiva_FE3()
        {
            var (db, _) = Escenario();
            var pausada = db.PublicacionesVaca.First(p => p.EstadoId == Catalogos.PublicacionPausada);
            var resultado = await Contactos(db, Prueba.CompradorId, Catalogos.RolComprador)
                .Crear(new ContactarViewModel { PublicacionVacaId = pausada.PublicacionVacaId, Mensaje = "Hola hola" });

            Assert.Equal("Marketplace", Assert.IsType<RedirectToActionResult>(resultado).ControllerName);
        }

        [Fact]
        public async Task Recibidos_MuestraDatosDelComprador_RN4()
        {
            var (db, activa) = Escenario();
            db.Contactos.Add(new Contactos { PublicacionVacaId = activa.PublicacionVacaId, CompradorId = Prueba.CompradorId, Mensaje = "Hola", FechaContacto = DateTime.Now });
            db.SaveChanges();

            var resultado = await Contactos(db, Prueba.ProductorId, Catalogos.RolProductor).Recibidos();
            var lista = Assert.IsType<List<ContactoRecibidoItem>>(Assert.IsType<ViewResult>(resultado).Model);
            var item = Assert.Single(lista);
            Assert.Equal("comprador@tuvaca.com", item.CompradorCorreo);
        }
    }
}
