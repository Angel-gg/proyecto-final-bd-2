using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Ítem individual dentro de un pedido con FKs exclusivas a Componente o Combo (esquema ventas).
    /// </summary>
    public class PedidoItem
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        public int? ComponenteId { get; set; }
        public Componente? Componente { get; set; }

        public int? ComboId { get; set; }
        public Combo? Combo { get; set; }

        [Required, MaxLength(200)]
        public string ProductoNombre { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        public int Cantidad { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        public string? ImageUrl { get; set; }

        [NotMapped]
        public string Tipo => ComboId.HasValue ? "combo" : "componente";
    }
}
