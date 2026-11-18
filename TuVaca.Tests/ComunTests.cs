using System.ComponentModel.DataAnnotations;
using TuVaca.Models.Cuentas;
using TuVaca.Models.Hato;
using TuVaca.Models.Marketplace;
using TuVaca.Models.Produccion;
using TuVaca.Seguridad;

namespace TuVaca.Tests
{
    /// <summary>Clases de apoyo: formatos, fotos, contraseñas y validaciones de los formularios.</summary>
    public class ComunTests
    {
        [Fact]
        public void Pesos_FormateaEnPesosColombianos()
        {
            var texto = Formatos.Pesos(1_500_000);
            Assert.Contains("1.500.000", texto);
        }

        [Fact]
        public void Fecha_FormateaDiaMesAnio()
        {
            Assert.Equal("05/03/2026", Formatos.Fecha(new DateTime(2026, 3, 5)));
            Assert.Equal("05/03/2026", Formatos.Fecha(new DateOnly(2026, 3, 5)));
            Assert.Equal("05/03/2026 14:30", Formatos.FechaHora(new DateTime(2026, 3, 5, 14, 30, 0)));
        }

        [Fact]
        public void Edad_CalculaAniosYMeses()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            Assert.Equal("Sin dato", Formatos.Edad(null));
            Assert.Equal("Menos de 1 mes", Formatos.Edad(hoy));
            Assert.Equal("1 año", Formatos.Edad(hoy.AddYears(-1)));
            Assert.Equal("2 años y 1 mes", Formatos.Edad(hoy.AddYears(-2).AddMonths(-1)));
            Assert.Equal("3 meses", Formatos.Edad(hoy.AddMonths(-3)));
        }

        [Fact]
        public void Fotos_Validar_AceptaImagenPequena()
        {
            Assert.Null(Fotos.Validar(null));
            Assert.Null(Fotos.Validar(Prueba.Archivo("vaca.jpg", "image/jpeg")));
        }

        [Fact]
        public void Fotos_Validar_RechazaTipoIncorrectoYArchivoGrande()
        {
            Assert.NotNull(Fotos.Validar(Prueba.Archivo("vaca.txt", "text/plain")));
            Assert.NotNull(Fotos.Validar(Prueba.Archivo("vaca.png", "image/png", (int)Fotos.TamanoMaximo + 1)));
        }

        [Fact]
        public async Task Fotos_Guardar_CreaArchivoYDevuelveRuta()
        {
            var carpeta = Path.Combine(Path.GetTempPath(), "tuvaca-fotos", Guid.NewGuid().ToString("N"));
            var ruta = await Fotos.GuardarAsync(Prueba.Archivo("vaca.png", "image/png"), carpeta, "vacas");

            Assert.StartsWith("/uploads/vacas/", ruta);
            Assert.EndsWith(".png", ruta);
            Assert.True(File.Exists(Path.Combine(carpeta, "uploads", "vacas", Path.GetFileName(ruta))));
        }

        [Fact]
        public void Contrasenas_HashearYVerificar()
        {
            var hash = Contrasenas.Hashear("Clave123!");

            Assert.NotEqual("Clave123!", hash); // RNF-01: nunca en texto plano
            Assert.True(Contrasenas.Verificar(hash, "Clave123!"));
            Assert.False(Contrasenas.Verificar(hash, "otra"));
            Assert.False(Contrasenas.Verificar(null, "Clave123!"));
            Assert.False(Contrasenas.Verificar("no-es-un-hash", "Clave123!"));
        }

        [Theory]
        [InlineData("Clave123!", true)]
        [InlineData("corta1!", false)]       // menos de 8
        [InlineData("SinNumero!", false)]    // sin número
        [InlineData("SinEspecial1", false)]  // sin carácter especial
        public void Contrasenas_PatronRN3(string clave, bool valida)
        {
            Assert.Equal(valida, System.Text.RegularExpressions.Regex.IsMatch(clave, Contrasenas.Patron));
        }

        [Fact]
        public void RegistroViewModel_ValidaCamposObligatorios()
        {
            var errores = Validar(new RegistroViewModel());
            Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RegistroViewModel.Correo)));
            Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RegistroViewModel.Contrasena)));
        }

        [Fact]
        public void RegistrarVacaViewModel_PesoDebeSerMayorACero()
        {
            var modelo = new RegistrarVacaViewModel { Identificacion = "A1", Raza = "Jersey", Sexo = "Hembra", Peso = 0 };
            Assert.Contains(Validar(modelo), e => e.MemberNames.Contains(nameof(RegistrarVacaViewModel.Peso)));
        }

        [Fact]
        public void RegistrarProduccion_CantidadDebeSerMayorACero()
        {
            var modelo = new RegistrarProduccionViewModel { Fecha = DateTime.Today, CantidadLitros = 0 };
            Assert.Contains(Validar(modelo), e => e.MemberNames.Contains(nameof(RegistrarProduccionViewModel.CantidadLitros)));
        }

        [Fact]
        public void MarketplaceFiltros_HayFiltros()
        {
            Assert.False(new MarketplaceFiltros().HayFiltros);
            Assert.True(new MarketplaceFiltros { Raza = "Holstein" }.HayFiltros);
            Assert.True(new MarketplaceFiltros { PrecioMax = 10 }.HayFiltros);
        }

        [Fact]
        public void HistorialProduccion_TotalYPromedio()
        {
            var modelo = new HistorialProduccionViewModel
            {
                Registros =
                {
                    new ProduccionItem { CantidadLitros = 10 },
                    new ProduccionItem { CantidadLitros = 20 }
                }
            };
            Assert.Equal(30, modelo.Total);
            Assert.Equal(15, modelo.Promedio);
            Assert.Equal(0, new HistorialProduccionViewModel().Promedio);
            Assert.Equal(0, new ResumenProduccionItem().Promedio);
        }

        [Fact]
        public void UsuarioActual_LeeLosClaims()
        {
            var sinSesion = new ClaimsPrincipal(new ClaimsIdentity());
            Assert.Null(sinSesion.ObtenerUsuarioId());
            Assert.False(sinSesion.EstaAutenticado());
            Assert.Equal(string.Empty, sinSesion.ObtenerNombre());

            var c = Prueba.Preparar(new TUVACA.Controllers.HomeController(), Prueba.ProductorId, Catalogos.RolProductor, "Jean");
            Assert.Equal(Prueba.ProductorId, c.User.ObtenerUsuarioId());
            Assert.Equal("Jean", c.User.ObtenerNombre());
            Assert.Equal(Catalogos.RolProductor, c.User.ObtenerRol());
            Assert.Equal("token-prueba", c.User.ObtenerTokenSesion());
            Assert.True(c.User.EstaAutenticado());
        }

        private static List<ValidationResult> Validar(object modelo)
        {
            var resultados = new List<ValidationResult>();
            Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true);
            return resultados;
        }
    }
}
