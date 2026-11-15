namespace TuVaca.Models.Marketplace
{
    /// <summary>CU-05: criterios de búsqueda (llegan por la URL, formulario GET).</summary>
    public class MarketplaceFiltros
    {
        public string? Q { get; set; }              // palabra clave (paso 3)
        public int? DepartamentoId { get; set; }    // RN-2: ubicación
        public int? MunicipioId { get; set; }
        public decimal? PrecioMin { get; set; }     // RN-2: rango de precio
        public decimal? PrecioMax { get; set; }
        public string? Raza { get; set; }           // RN-2: raza
        public int? EdadMinAnios { get; set; }      // RN-2: edad
        public int? EdadMaxAnios { get; set; }
        public string Orden { get; set; } = "recientes"; // RN-4: recientes | precio_asc | precio_desc

        public bool HayFiltros =>
            !string.IsNullOrWhiteSpace(Q) || DepartamentoId != null || MunicipioId != null
            || PrecioMin != null || PrecioMax != null || !string.IsNullOrWhiteSpace(Raza)
            || EdadMinAnios != null || EdadMaxAnios != null;
    }

    /// <summary>Una tarjeta del listado de resultados (paso 6: foto, precio y datos principales).</summary>
    public class MarketplaceItem
    {
        public int PublicacionVacaId { get; set; }
        public string FotoUrl { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string Raza { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public DateOnly? FechaNacimiento { get; set; }
        public decimal Peso { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaPublicacion { get; set; }
    }

    public class MarketplaceIndexViewModel
    {
        public MarketplaceFiltros Filtros { get; set; } = new();
        public List<MarketplaceItem> Resultados { get; set; } = new();
        public List<string> Razas { get; set; } = new();
        public string? Error { get; set; }          // FE-2
    }

    /// <summary>FA-2-CU05: detalle completo de una publicación.</summary>
    public class MarketplaceDetalleViewModel
    {
        public int PublicacionVacaId { get; set; }
        public int VendedorId { get; set; }
        public string FotoUrl { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public DateTime FechaPublicacion { get; set; }

        public string Identificacion { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public DateOnly? FechaNacimiento { get; set; }
        public decimal Peso { get; set; }

        public string VendedorNombre { get; set; } = string.Empty;
        public string? VendedorTelefono { get; set; }   // FA-2-CU06: contacto directo
    }
}
