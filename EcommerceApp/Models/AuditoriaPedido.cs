using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Bitácora automática de cambios de estado en Pedidos generada por trigger PostgreSQL (esquema auditoria).
    /// </summary>
    public class AuditoriaPedido
    {
        [Key]
        public int Id { get; set; }

        public int? PedidoId { get; set; }
        public short? EstadoAntes { get; set; }
        public short? EstadoDespues { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;

        [Required]
        public string Usuario { get; set; } = string.Empty;
    }
}
