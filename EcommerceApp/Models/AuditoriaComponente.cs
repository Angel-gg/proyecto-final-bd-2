using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Bitácora automática de cambios en Componentes generada por trigger PostgreSQL (esquema auditoria).
    /// </summary>
    public class AuditoriaComponente
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(1)]
        public string Operacion { get; set; } = "U";

        public int? ComponenteId { get; set; }
        public int? StockAntes { get; set; }
        public int? StockDespues { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioAntes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioDespues { get; set; }

        [MaxLength(20)]
        public string? EstadoAntes { get; set; }

        [MaxLength(20)]
        public string? EstadoDespues { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.UtcNow;

        [Required]
        public string Usuario { get; set; } = string.Empty;
    }
}
