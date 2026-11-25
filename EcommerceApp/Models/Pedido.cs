using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    public enum EstadoPedido : short
    {
        Pendiente  = 0,
        Completado = 1,
        Entregado  = 2,
        Devuelto   = 3,
        Cancelado  = 4
    }

    public enum MetodoPago : short
    {
        Efectivo      = 0,
        Tarjeta       = 1,
        Transferencia = 2,
        QR            = 3
    }

    /// <summary>
    /// Representa un pedido/compra realizado por un cliente en la tienda (esquema ventas).
    /// </summary>
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Número legible: TPC-YYYYMMDD-NNNN</summary>
        [Required, MaxLength(30)]
        public string NumeroPedido { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? Usuario { get; set; }

        public int? SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }

        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
        public DateTime? FechaEntrega { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        /// <summary>IVA 16%</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Impuesto { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        /// <summary>Comisión del vendedor (5% del subtotal)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Comision { get; set; }

        public MetodoPago MetodoPago { get; set; } = MetodoPago.Tarjeta;
        public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

        public string? Notas { get; set; }

        public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
    }
}
