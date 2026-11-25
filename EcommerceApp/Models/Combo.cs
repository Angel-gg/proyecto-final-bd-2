using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Combo — paquete de componentes vendidos juntos con precio especial (esquema catalogo).
    /// </summary>
    public class Combo
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Nombre del Combo")]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio de Venta (Bs.)")]
        [Range(0.01, 9999999.99)]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Descuento (%)")]
        [Range(0, 100)]
        public decimal Descuento { get; set; } = 0;

        [Display(Name = "URL de imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Range(0, int.MaxValue)]
        [Display(Name = "Stock Disponible")]
        public int Stock { get; set; } = 0;

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public DateTime? ActualizadoEn { get; set; }

        // Relación con componentes
        public ICollection<ComboComponente> ComboComponentes { get; set; } = new List<ComboComponente>();

        // Propiedades calculadas (no mapeadas)
        [NotMapped]
        public decimal PrecioBase => ComboComponentes.Sum(cc =>
            (cc.Componente?.Precio ?? 0) * cc.Cantidad);

        [NotMapped]
        public decimal Ahorro => PrecioBase - PrecioVenta;
    }
}
