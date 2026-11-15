using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Marketplace;

namespace TuVaca.Controllers
{
    /// <summary>
    /// CU-05: Buscar y filtrar publicaciones (Marketplace).
    /// No exige sesión: un visitante también puede buscar (precondición del CU-05).
    /// </summary>
    [AllowAnonymous]
    public class MarketplaceController : Controller
    {
        private readonly TuVacaDbContext _db;
        private readonly ILogger<MarketplaceController> _logger;

        public MarketplaceController(TuVacaDbContext db, ILogger<MarketplaceController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /Marketplace?Q=...&Raza=...&PrecioMin=...  (pasos 1 a 6, FA-1 = abrir sin filtros)
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] MarketplaceFiltros filtros)
        {
            var modelo = new MarketplaceIndexViewModel { Filtros = filtros };

            // Rango de precio al revés: se intercambia en lugar de dar error
            if (filtros.PrecioMin > filtros.PrecioMax)
            {
                (filtros.PrecioMin, filtros.PrecioMax) = (filtros.PrecioMax, filtros.PrecioMin);
            }
            if (filtros.EdadMinAnios > filtros.EdadMaxAnios)
            {
                (filtros.EdadMinAnios, filtros.EdadMaxAnios) = (filtros.EdadMaxAnios, filtros.EdadMinAnios);
            }

            try
            {
                modelo.Resultados = await BuscarAsync(filtros);
                modelo.Razas = await _db.PublicacionesVaca
                    .Where(p => p.EstadoId == Catalogos.PublicacionActiva)
                    .Select(p => p.Vaca.Raza)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // FE-2-CU05
                _logger.LogError(ex, "CU-05: error al consultar publicaciones");
                modelo.Error = "No fue posible cargar las publicaciones. Intenta nuevamente.";
            }

            await CargarDepartamentosAsync(filtros.DepartamentoId);
            return View(modelo);
        }

        // GET /Marketplace/Detalle/5  (FA-2-CU05: ver detalle de una publicación)
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var detalle = await (
                from p in _db.PublicacionesVaca
                join v in _db.Vacas on p.VacaId equals v.VacaId
                join u in _db.Usuarios on v.UsuarioId equals u.UsuarioId
                join m in _db.Municipios on p.MunicipioId equals m.MunicipioId
                join d in _db.Departamentos on m.DepartamentoId equals d.DepartamentoId
                where p.PublicacionVacaId == id && p.EstadoId == Catalogos.PublicacionActiva // RN-1
                select new MarketplaceDetalleViewModel
                {
                    PublicacionVacaId = p.PublicacionVacaId,
                    VendedorId = u.UsuarioId,
                    FotoUrl = p.FotoUrl,
                    Precio = p.Precio,
                    Descripcion = p.Descripcion,
                    Ubicacion = m.Nombre + ", " + d.Nombre,
                    FechaPublicacion = p.FechaPublicacion,
                    Identificacion = v.Identificacion,
                    Raza = v.Raza,
                    Sexo = v.Sexo,
                    FechaNacimiento = v.FechaNacimiento,
                    Peso = v.Peso,
                    VendedorNombre = u.NombreCompleto,
                    VendedorTelefono = u.Telefono
                }).FirstOrDefaultAsync();

            if (detalle == null)
            {
                TempData["Error"] = "Esta publicación ya no está disponible.";
                return RedirectToAction(nameof(Index));
            }

            return View(detalle);
        }

        // ============================== APOYO ==============================

        /// <summary>Paso 5: publicaciones activas que cumplen todos los criterios a la vez (RN-3).</summary>
        private async Task<List<MarketplaceItem>> BuscarAsync(MarketplaceFiltros f)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var consulta = _db.PublicacionesVaca
                .Where(p => p.EstadoId == Catalogos.PublicacionActiva); // RN-1: solo activas

            if (!string.IsNullOrWhiteSpace(f.Q))
            {
                var texto = f.Q.Trim();
                consulta = consulta.Where(p =>
                    p.Descripcion.Contains(texto) ||
                    p.Vaca.Raza.Contains(texto) ||
                    p.Vaca.Identificacion.Contains(texto) ||
                    p.Municipio.Nombre.Contains(texto));
            }
            if (f.MunicipioId != null)
            {
                consulta = consulta.Where(p => p.MunicipioId == f.MunicipioId);
            }
            else if (f.DepartamentoId != null)
            {
                consulta = consulta.Where(p => p.Municipio.DepartamentoId == f.DepartamentoId);
            }
            if (f.PrecioMin != null)
            {
                consulta = consulta.Where(p => p.Precio >= f.PrecioMin);
            }
            if (f.PrecioMax != null)
            {
                consulta = consulta.Where(p => p.Precio <= f.PrecioMax);
            }
            if (!string.IsNullOrWhiteSpace(f.Raza))
            {
                var raza = f.Raza.Trim();
                consulta = consulta.Where(p => p.Vaca.Raza == raza);
            }
            // Edad en años: se convierte a fechas de nacimiento límite
            if (f.EdadMinAnios != null)
            {
                var nacidaAntesDe = hoy.AddYears(-f.EdadMinAnios.Value);
                consulta = consulta.Where(p => p.Vaca.FechaNacimiento != null && p.Vaca.FechaNacimiento <= nacidaAntesDe);
            }
            if (f.EdadMaxAnios != null)
            {
                var nacidaDespuesDe = hoy.AddYears(-(f.EdadMaxAnios.Value + 1));
                consulta = consulta.Where(p => p.Vaca.FechaNacimiento != null && p.Vaca.FechaNacimiento > nacidaDespuesDe);
            }

            // RN-4: ordenar resultados
            consulta = f.Orden switch
            {
                "precio_asc" => consulta.OrderBy(p => p.Precio),
                "precio_desc" => consulta.OrderByDescending(p => p.Precio),
                _ => consulta.OrderByDescending(p => p.FechaPublicacion)
            };

            return await consulta
                .Select(p => new MarketplaceItem
                {
                    PublicacionVacaId = p.PublicacionVacaId,
                    FotoUrl = p.FotoUrl,
                    Precio = p.Precio,
                    Raza = p.Vaca.Raza,
                    Sexo = p.Vaca.Sexo,
                    FechaNacimiento = p.Vaca.FechaNacimiento,
                    Peso = p.Vaca.Peso,
                    Ubicacion = p.Municipio.Nombre + ", " + p.Municipio.Departamento.Nombre,
                    Descripcion = p.Descripcion,
                    FechaPublicacion = p.FechaPublicacion
                })
                .ToListAsync();
        }

        private async Task CargarDepartamentosAsync(int? seleccionado)
        {
            ViewBag.Departamentos = await _db.Departamentos
                .OrderBy(d => d.Nombre)
                .Select(d => new SelectListItem
                {
                    Value = d.DepartamentoId.ToString(),
                    Text = d.Nombre,
                    Selected = d.DepartamentoId == seleccionado
                })
                .ToListAsync();
        }
    }
}
