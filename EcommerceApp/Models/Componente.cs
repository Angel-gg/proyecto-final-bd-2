using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Componente de hardware normalizado en 3FN (esquema catalogo).
    /// </summary>
    public class Componente
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(800)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        [Required]
        [Display(Name = "Marca")]
        public int MarcaId { get; set; }
        public Marca? Marca { get; set; }

        [Display(Name = "Proveedor")]
        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        [Required, Range(0.01, 9999999.99)]
        [Display(Name = "Precio (Bs.)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Required, Range(0, int.MaxValue)]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [Required, Range(0, int.MaxValue)]
        [Display(Name = "Stock Mínimo")]
        public int StockMinimo { get; set; } = 5;

        [Display(Name = "URL de imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Especificaciones técnicas")]
        public string? Especificaciones { get; set; }

        [Required, MaxLength(20)]
        [Display(Name = "Estado de Obsolescencia")]
        public string EstadoObsolescencia { get; set; } = "Vigente";

        public bool Activo { get; set; } = true;

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public DateTime? ActualizadoEn { get; set; }

        // Relación inversa con combos
        public ICollection<ComboComponente> ComboComponentes { get; set; } = new List<ComboComponente>();
    }
}
