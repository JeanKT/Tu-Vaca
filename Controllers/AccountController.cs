using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Cuentas;
using TuVaca.Models.Entidades;
using TuVaca.Seguridad;

namespace TuVaca.Controllers
{
    /// <summary>CU-01: Registrarse, Iniciar sesión y Cerrar sesión.</summary>
    public class AccountController : Controller
    {
        private const int IntentosPermitidos = 5;   // RN-5-CU01
        private const int MinutosBloqueo = 15;      // RN-5-CU01
        private const string MensajeErrorGeneral = "No fue posible completar la operación. Intenta nuevamente."; // FE-1

        private readonly TuVacaDbContext _db;
        private readonly ILogger<AccountController> _logger;

        public AccountController(TuVacaDbContext db, ILogger<AccountController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // =========================== INICIAR SESIÓN ===========================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.EstaAutenticado())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(modelo); // FE-2
            }

            try
            {
                var dato = modelo.CorreoODocumento.Trim();
                var correo = dato.ToLowerInvariant();
                var usuario = await _db.Usuarios
                    .FirstOrDefaultAsync(u => u.Correo == correo || u.NumeroDocumento == dato);
                var ahora = DateTime.Now;

                // RN-5: la cuenta sigue bloqueada
                if (usuario != null && usuario.BloqueadoHasta != null && usuario.BloqueadoHasta > ahora)
                {
                    ModelState.AddModelError(string.Empty, MensajeBloqueo(usuario.BloqueadoHasta.Value, ahora));
                    return View(modelo);
                }

                // FE-3: credenciales incorrectas
                if (usuario == null || !Contrasenas.Verificar(usuario.ContrasenaHash, modelo.Contrasena))
                {
                    var mensaje = "Correo o contraseña incorrectos.";
                    if (usuario != null)
                    {
                        usuario.IntentosFallidos++;
                        if (usuario.IntentosFallidos >= IntentosPermitidos)
                        {
                            // RN-5: 5 intentos fallidos seguidos bloquean la cuenta 15 minutos
                            usuario.IntentosFallidos = 0;
                            usuario.BloqueadoHasta = ahora.AddMinutes(MinutosBloqueo);
                            mensaje = MensajeBloqueo(usuario.BloqueadoHasta.Value, ahora);
                        }
                        else
                        {
                            mensaje += $" Intento {usuario.IntentosFallidos} de {IntentosPermitidos}.";
                        }
                        await _db.SaveChangesAsync();
                    }

                    ModelState.AddModelError(string.Empty, mensaje);
                    return View(modelo);
                }

                if (usuario.EstadoId != Catalogos.UsuarioActivo)
                {
                    ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Comunícate con un administrador.");
                    return View(modelo);
                }

                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;

                var rol = await NombreRolAsync(usuario.RolId);
                DateTime? expira = modelo.MantenerSesion ? ahora.AddDays(30) : null;
                var token = await CrearSesionAsync(usuario.UsuarioId, ahora, expira); // también guarda el usuario
                await IniciarSesionAsync(usuario, rol, token, expira);

                return RedirigirSegunRol(rol, returnUrl); // paso 5
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-01: error al iniciar sesión");
                ModelState.AddModelError(string.Empty, MensajeErrorGeneral); // FE-1
                return View(modelo);
            }
        }

        // ============================ REGISTRARSE ============================

        [HttpGet]
        public IActionResult Register()
        {
            if (User.EstaAutenticado())
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegistroViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistroViewModel modelo)
        {
            // RN-4: el rol Administrador no se autoasigna
            if (modelo.Rol != Catalogos.RolProductor && modelo.Rol != Catalogos.RolComprador)
            {
                ModelState.AddModelError(nameof(modelo.Rol), "Selecciona Productor / Vendedor o Comprador / Visitante.");
            }

            if (!ModelState.IsValid)
            {
                return View(modelo); // FE-2
            }

            try
            {
                var correo = modelo.Correo.Trim().ToLowerInvariant();
                var documento = modelo.NumeroDocumento.Trim();

                // Paso 4: no puede haber duplicados (RN-1 y RN-2)
                if (await _db.Usuarios.AnyAsync(u => u.NumeroDocumento == documento))
                {
                    ModelState.AddModelError(nameof(modelo.NumeroDocumento), "Ya existe una cuenta con esta cédula.");
                }
                if (await _db.Usuarios.AnyAsync(u => u.Correo == correo))
                {
                    ModelState.AddModelError(nameof(modelo.Correo), "Ya existe una cuenta con este correo.");
                }
                if (!ModelState.IsValid)
                {
                    return View(modelo);
                }

                var rolId = await _db.Roles
                    .Where(r => r.NombreRol == modelo.Rol)
                    .Select(r => r.RolId)
                    .FirstAsync();
                var ahora = DateTime.Now;

                // Paso 5: se crea la cuenta (la contraseña se guarda cifrada, RNF-01)
                var usuario = new Usuarios
                {
                    NombreCompleto = modelo.NombreCompleto.Trim(),
                    NumeroDocumento = documento,
                    Correo = correo,
                    Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim(),
                    ContrasenaHash = Contrasenas.Hashear(modelo.Contrasena),
                    RolId = rolId,
                    EstadoId = Catalogos.UsuarioActivo,
                    IntentosFallidos = 0,
                    FechaRegistro = ahora
                };
                _db.Usuarios.Add(usuario);
                await _db.SaveChangesAsync();

                // Pasos 5 y 6: mensaje de bienvenida y entrada directa según el rol
                var token = await CrearSesionAsync(usuario.UsuarioId, ahora, null);
                await IniciarSesionAsync(usuario, modelo.Rol, token, null);
                TempData["Exito"] = $"¡Bienvenido a Tu Vaca, {usuario.NombreCompleto}! Tu cuenta quedó creada.";

                return RedirigirSegunRol(modelo.Rol, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-01: error al registrar el usuario");
                ModelState.AddModelError(string.Empty, MensajeErrorGeneral); // FE-1
                return View(modelo);
            }
        }

        // =========================== CERRAR SESIÓN ===========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // RN-6: la sesión queda invalidada en la base y no se puede reutilizar
                var token = User.ObtenerTokenSesion();
                if (token != null)
                {
                    var sesion = await _db.Sesiones.FirstOrDefaultAsync(s => s.Token == token);
                    if (sesion != null)
                    {
                        sesion.Activa = false;
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-01: error al cerrar la sesión en la base de datos");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Exito"] = "Cerraste sesión.";
            return RedirectToAction("Index", "Home"); // paso 3
        }

        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }

        // ============================== APOYO ==============================

        private Task<string> NombreRolAsync(int rolId) =>
            _db.Roles.Where(r => r.RolId == rolId).Select(r => r.NombreRol).FirstAsync();

        /// <summary>Crea la fila en Sesiones y guarda los cambios pendientes.</summary>
        private async Task<string> CrearSesionAsync(int usuarioId, DateTime ahora, DateTime? expira)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _db.Sesiones.Add(new Sesiones
            {
                UsuarioId = usuarioId,
                Token = token,
                FechaInicio = ahora,
                FechaExpiracion = expira,
                Activa = true
            });
            await _db.SaveChangesAsync();
            return token;
        }

        private async Task IniciarSesionAsync(Usuarios usuario, string rol, string token, DateTime? expira)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new(ClaimTypes.Name, usuario.NombreCompleto),
                new(ClaimTypes.Email, usuario.Correo),
                new(ClaimTypes.Role, rol),
                new(UsuarioActual.ClaimSesion, token)
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var propiedades = new AuthenticationProperties
            {
                // "Mantener sesión activa": la cookie dura 30 días aunque se cierre el navegador
                IsPersistent = expira.HasValue,
                ExpiresUtc = expira.HasValue ? new DateTimeOffset(expira.Value) : null
            };
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad), propiedades);
        }

        private IActionResult RedirigirSegunRol(string rol, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Página principal según el rol. "Mis vacas" se usa sola en cuanto exista el CU-02.
            if (rol == Catalogos.RolProductor)
            {
                var misVacas = Url.Action("Index", "Vacas");
                if (misVacas != null)
                {
                    return LocalRedirect(misVacas);
                }
            }
            return RedirectToAction("Index", "Home");
        }

        private static string MensajeBloqueo(DateTime bloqueadoHasta, DateTime ahora)
        {
            var minutos = Math.Max(1, (int)Math.Ceiling((bloqueadoHasta - ahora).TotalMinutes));
            return $"Tu cuenta quedó bloqueada por {IntentosPermitidos} intentos fallidos seguidos. " +
                   $"Intenta de nuevo en {minutos} minuto(s).";
        }
    }
}
