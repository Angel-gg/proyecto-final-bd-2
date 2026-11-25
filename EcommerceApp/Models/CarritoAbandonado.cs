using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Registro de un carrito que fue abandonado sin completar la compra (esquema ventas, JSONB).
    /// </summary>
    public class CarritoAbandonado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? Usuario { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaUltimaActividad { get; set; } = DateTime.UtcNow;
        public DateTime? FechaExpira { get; set; }

        /// <summary>JSONB serializado de los ítems del carrito</summary>
        [Column(TypeName = "jsonb")]
        public string ItemsJson { get; set; } = "[]";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEstimado { get; set; }

        public int TotalItems { get; set; }

        /// <summary>true = ya completó la compra (no cuenta como abandonado)</summary>
        public bool Convertido { get; set; } = false;
    }
}
