using System.Security.Cryptography;
using System.Text;
using TuVaca.Controllers;
using TuVaca.Models.Administracion;
using TuVaca.Models.Cuentas;
using TuVaca.Servicios;

namespace TuVaca.Tests
{
    /// <summary>CU-01 (cuenta), CU-23 (recuperar contraseña) y CU-10 (gestionar usuarios).</summary>
    public class CuentasYUsuariosTests
    {
        private static AccountController Cuenta(TuVacaDbContext db, int? usuario = null) =>
            Prueba.Preparar(new AccountController(db, NullLogger<AccountController>.Instance), usuario);

        private static RecuperarContrasenaController Recuperar(TuVacaDbContext db, int? usuario = null) =>
            Prueba.Preparar(new RecuperarContrasenaController(db, new CorreoService(Prueba.ConfiguracionVacia()),
                Prueba.Entorno(), NullLogger<RecuperarContrasenaController>.Instance), usuario);

        private static UsuariosController Usuarios(TuVacaDbContext db) =>
            Prueba.Preparar(new UsuariosController(db, NullLogger<UsuariosController>.Instance), Prueba.AdminId, Catalogos.RolAdministrador);

        // ------------------------------ CU-01 ------------------------------

        [Fact]
        public void Login_Get_SinSesion_MuestraFormulario_ConSesion_Redirige()
        {
            var db = Prueba.CrearDb();
            Assert.IsType<ViewResult>(Cuenta(db).Login());
            Assert.IsType<RedirectToActionResult>(Cuenta(db, Prueba.ProductorId).Login());
            Assert.IsType<ViewResult>(Cuenta(db).Register());
            Assert.IsType<RedirectToActionResult>(Cuenta(db, Prueba.ProductorId).Register());
            Assert.IsType<ViewResult>(Cuenta(db).AccesoDenegado());
        }

        [Fact]
        public async Task Login_Correcto_CreaSesionYRedirigeSegunRol()
        {
            var db = Prueba.CrearDb();

            var productor = await Cuenta(db).Login(new LoginViewModel { CorreoODocumento = "productor@tuvaca.com", Contrasena = Prueba.ClaveValida });
            Assert.Equal("/Vacas/Index", Assert.IsType<LocalRedirectResult>(productor).Url);

            var admin = await Cuenta(db).Login(new LoginViewModel { CorreoODocumento = "1003", Contrasena = Prueba.ClaveValida, MantenerSesion = true });
            Assert.Equal("Usuarios", Assert.IsType<RedirectToActionResult>(admin).ControllerName);

            var comprador = await Cuenta(db).Login(new LoginViewModel { CorreoODocumento = "comprador@tuvaca.com", Contrasena = Prueba.ClaveValida });
            Assert.Equal("Marketplace", Assert.IsType<RedirectToActionResult>(comprador).ControllerName);

            var conRetorno = await Cuenta(db).Login(new LoginViewModel { CorreoODocumento = "pedro@tuvaca.com", Contrasena = Prueba.ClaveValida }, "/Marketplace");
            Assert.Equal("/Marketplace", Assert.IsType<LocalRedirectResult>(conRetorno).Url);

            Assert.Equal(4, db.Sesiones.Count());
        }

        [Fact]
        public async Task Login_CincoIntentosFallidos_BloqueaLaCuenta_RN5()
        {
            var db = Prueba.CrearDb();
            for (var i = 0; i < 5; i++)
            {
                Assert.IsType<ViewResult>(await Cuenta(db).Login(new LoginViewModel { CorreoODocumento = "productor@tuvaca.com", Contrasena = "mala" }));
            }

            Assert.NotNull(db.Usuarios.Single(u => u.UsuarioId == Prueba.ProductorId).BloqueadoHasta);
            // Bloqueado: ni con la clave correcta entra
            var controlador = Cuenta(db);
            Assert.IsType<ViewResult>(await controlador.Login(new LoginViewModel { CorreoODocumento = "productor@tuvaca.com", Contrasena = Prueba.ClaveValida }));
            Assert.Empty(db.Sesiones);
        }

        [Fact]
        public async Task Login_UsuarioInactivo_O_Inexistente_O_FormularioInvalido()
        {
            var db = Prueba.CrearDb();
            db.Usuarios.Single(u => u.UsuarioId == Prueba.CompradorId).EstadoId = Catalogos.UsuarioInactivo;
            db.SaveChanges();

            Assert.IsType<ViewResult>(await Cuenta(db).Login(new LoginViewModel { CorreoODocumento = "comprador@tuvaca.com", Contrasena = Prueba.ClaveValida }));
            Assert.IsType<ViewResult>(await Cuenta(db).Login(new LoginViewModel { CorreoODocumento = "nadie@x.com", Contrasena = "x" }));

            var invalido = Cuenta(db);
            invalido.ModelState.AddModelError("Contrasena", "Requerida");
            Assert.IsType<ViewResult>(await invalido.Login(new LoginViewModel()));
            Assert.Empty(db.Sesiones);
        }

        [Fact]
        public async Task Registro_CreaCuentaCifrada_YNoPermiteAdministrador_RN4()
        {
            var db = Prueba.CrearDb();
            var modelo = new RegistroViewModel
            {
                NombreCompleto = "Nuevo Ganadero",
                NumeroDocumento = "5555555",
                Correo = "Nuevo@TuVaca.com",
                Rol = Catalogos.RolProductor,
                Contrasena = Prueba.ClaveValida,
                ConfirmarContrasena = Prueba.ClaveValida
            };

            Assert.IsType<LocalRedirectResult>(await Cuenta(db).Register(modelo));
            var nuevo = db.Usuarios.Single(u => u.NumeroDocumento == "5555555");
            Assert.Equal("nuevo@tuvaca.com", nuevo.Correo);
            Assert.NotEqual(Prueba.ClaveValida, nuevo.ContrasenaHash); // RNF-01

            modelo.Rol = Catalogos.RolAdministrador;
            modelo.NumeroDocumento = "6666666";
            Assert.IsType<ViewResult>(await Cuenta(db).Register(modelo));
        }

        [Fact]
        public async Task Registro_Duplicados_RN1_RN2()
        {
            var db = Prueba.CrearDb();
            var controlador = Cuenta(db);
            var resultado = await controlador.Register(new RegistroViewModel
            {
                NombreCompleto = "Copia", NumeroDocumento = "1001", Correo = "productor@tuvaca.com",
                Rol = Catalogos.RolComprador, Contrasena = Prueba.ClaveValida, ConfirmarContrasena = Prueba.ClaveValida
            });

            Assert.IsType<ViewResult>(resultado);
            Assert.True(controlador.ModelState.ContainsKey(nameof(RegistroViewModel.NumeroDocumento)));
            Assert.True(controlador.ModelState.ContainsKey(nameof(RegistroViewModel.Correo)));
        }

        [Fact]
        public async Task Logout_InvalidaLaSesion_RN6()
        {
            var db = Prueba.CrearDb();
            db.Sesiones.Add(new Sesiones { UsuarioId = Prueba.ProductorId, Token = "token-prueba", FechaInicio = DateTime.Now, Activa = true });
            db.SaveChanges();

            Assert.IsType<RedirectToActionResult>(await Cuenta(db, Prueba.ProductorId).Logout());
            Assert.False(db.Sesiones.Single().Activa);
        }

        // ------------------------------ CU-23 ------------------------------

        private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        [Fact]
        public async Task Recuperar_Solicitar()
        {
            var db = Prueba.CrearDb();
            Assert.IsType<ViewResult>(Recuperar(db).Solicitar());
            Assert.IsType<RedirectToActionResult>(Recuperar(db, Prueba.ProductorId).Solicitar());

            // RN-3: correo que no existe -> mismo mensaje genérico
            Assert.IsType<RedirectToActionResult>(await Recuperar(db).Solicitar(new SolicitarRecuperacionViewModel { Correo = "noexiste@x.com" }));

            // Correo que sí existe: se crea el token; como en la prueba no hay SMTP, se muestra el error (FE-1)
            var controlador = Recuperar(db);
            Assert.IsType<ViewResult>(await controlador.Solicitar(new SolicitarRecuperacionViewModel { Correo = "productor@tuvaca.com" }));
            Assert.Single(db.TokensRecuperacion);

            var invalido = Recuperar(db);
            invalido.ModelState.AddModelError("Correo", "Requerido");
            Assert.IsType<ViewResult>(await invalido.Solicitar(new SolicitarRecuperacionViewModel()));
        }

        [Fact]
        public async Task Recuperar_Restablecer_CambiaClaveYCierraSesiones_RN4()
        {
            var db = Prueba.CrearDb();
            db.TokensRecuperacion.Add(new TokensRecuperacion { UsuarioId = Prueba.ProductorId, Token = Hash("ABC"), FechaExpiracion = DateTime.Now.AddMinutes(30), FechaCreacion = DateTime.Now });
            db.Sesiones.Add(new Sesiones { UsuarioId = Prueba.ProductorId, Token = "s1", FechaInicio = DateTime.Now, Activa = true });
            db.SaveChanges();

            Assert.Equal("EnlaceInvalido", Assert.IsType<ViewResult>(await Recuperar(db).Restablecer((string?)"OTRO")).ViewName); // FE-2
            Assert.IsType<RestablecerContrasenaViewModel>(Assert.IsType<ViewResult>(await Recuperar(db).Restablecer((string?)"ABC")).Model);

            var invalido = Recuperar(db);
            invalido.ModelState.AddModelError("NuevaContrasena", "Débil");
            Assert.Null(Assert.IsType<ViewResult>(await invalido.Restablecer(new RestablecerContrasenaViewModel { Token = "ABC" })).ViewName);

            var resultado = await Recuperar(db).Restablecer(new RestablecerContrasenaViewModel
            {
                Token = "ABC", NuevaContrasena = "Nueva456#", ConfirmarContrasena = "Nueva456#"
            });

            Assert.Equal("Login", Assert.IsType<RedirectToActionResult>(resultado).ActionName);
            Assert.True(Contrasenas.Verificar(db.Usuarios.Single(u => u.UsuarioId == Prueba.ProductorId).ContrasenaHash, "Nueva456#"));
            Assert.True(db.TokensRecuperacion.Single().Usado);
            Assert.False(db.Sesiones.Single().Activa);
            Assert.Equal("EnlaceInvalido", Assert.IsType<ViewResult>(await Recuperar(db).Restablecer(new RestablecerContrasenaViewModel { Token = "ABC" })).ViewName);
        }

        // ------------------------------ CU-10 ------------------------------

        [Fact]
        public async Task Usuarios_ListadoYFiltros_FA2()
        {
            var db = Prueba.CrearDb();
            UsuariosIndexViewModel Listar(string? b, int? r, int? e) =>
                (UsuariosIndexViewModel)((ViewResult)Usuarios(db).Index(b, r, e).Result).Model!;

            Assert.Equal(4, Listar(null, null, null).Usuarios.Count);
            Assert.Equal(2, Listar(null, 1, null).Usuarios.Count);
            Assert.Single(Listar("Carla", null, null).Usuarios);
            Assert.Equal(4, Listar(null, null, Catalogos.UsuarioActivo).Usuarios.Count);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Usuarios_EditarDatos_QuedaEnAuditoria_RN5()
        {
            var db = Prueba.CrearDb();
            var get = Assert.IsType<EditarUsuarioViewModel>(Assert.IsType<ViewResult>(await Usuarios(db).Editar(Prueba.CompradorId)).Model);
            Assert.Equal("comprador@tuvaca.com", get.Correo);
            Assert.IsType<NotFoundResult>(await Usuarios(db).Editar(9999));

            get.NombreCompleto = "Carla Actualizada";
            get.Telefono = "3115550000";
            Assert.IsType<RedirectToActionResult>(await Usuarios(db).Editar(get));

            Assert.Equal("Carla Actualizada", db.Usuarios.Single(u => u.UsuarioId == Prueba.CompradorId).NombreCompleto);
            var auditoria = Assert.Single(db.AuditoriaUsuarios);
            Assert.Equal("EditarDatos", auditoria.Accion);
            Assert.Equal(Prueba.AdminId, auditoria.AdministradorId);

            var invalido = Usuarios(db);
            invalido.ModelState.AddModelError("NombreCompleto", "Requerido");
            Assert.IsType<ViewResult>(await invalido.Editar(get));
        }

        [Fact]
        public async Task Usuarios_CambiarRol_RN3()
        {
            var db = Prueba.CrearDb();
            await Usuarios(db).CambiarRol(Prueba.CompradorId, 3);
            Assert.Equal(3, db.Usuarios.Single(u => u.UsuarioId == Prueba.CompradorId).RolId);
            Assert.Equal("CambiarRol", db.AuditoriaUsuarios.Single().Accion);

            await Usuarios(db).CambiarRol(Prueba.AdminId, 1);       // a sí mismo: no
            await Usuarios(db).CambiarRol(Prueba.CompradorId, 3);   // mismo rol: no
            Assert.Equal(3, db.Usuarios.Single(u => u.UsuarioId == Prueba.AdminId).RolId);
            Assert.Single(db.AuditoriaUsuarios);
            Assert.IsType<NotFoundResult>(await Usuarios(db).CambiarRol(9999, 1));
        }

        [Fact]
        public async Task Usuarios_Desactivar_PausaPublicacionesYNoPermiteAutodesactivarse_RN2_RN4()
        {
            var db = Prueba.CrearDb();
            var vaca = Prueba.AgregarVaca(db);
            Prueba.AgregarPublicacion(db, vaca);
            db.Sesiones.Add(new Sesiones { UsuarioId = Prueba.ProductorId, Token = "s", FechaInicio = DateTime.Now, Activa = true });
            db.SaveChanges();

            await Usuarios(db).Desactivar(Prueba.AdminId); // FE-2
            Assert.Equal(Catalogos.UsuarioActivo, db.Usuarios.Single(u => u.UsuarioId == Prueba.AdminId).EstadoId);

            await Usuarios(db).Desactivar(Prueba.ProductorId);
            Assert.Equal(Catalogos.UsuarioInactivo, db.Usuarios.Single(u => u.UsuarioId == Prueba.ProductorId).EstadoId);
            Assert.Equal(Catalogos.PublicacionPausada, db.PublicacionesVaca.Single().EstadoId);
            Assert.False(db.Sesiones.Single().Activa);

            await Usuarios(db).Desactivar(Prueba.ProductorId); // ya inactivo
            await Usuarios(db).Activar(Prueba.ProductorId);
            Assert.Equal(Catalogos.UsuarioActivo, db.Usuarios.Single(u => u.UsuarioId == Prueba.ProductorId).EstadoId);
            await Usuarios(db).Activar(Prueba.ProductorId); // ya activo

            Assert.Equal(2, db.AuditoriaUsuarios.Count());
            Assert.IsType<NotFoundResult>(await Usuarios(db).Desactivar(9999));
            Assert.IsType<NotFoundResult>(await Usuarios(db).Activar(9999));
        }
    }
}
