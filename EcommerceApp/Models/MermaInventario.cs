using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Control de mermas, obsolescencia y disposición RAEE de hardware (esquema auditoria - ODS 12).
    /// </summary>
    public class MermaInventario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ComponenteId { get; set; }
        public Componente? Componente { get; set; }

        public int? SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Required, MaxLength(50)]
        public string Motivo { get; set; } = "Obsolescencia";

        public string? Observaciones { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoEstimado { get; set; }

        [Required, MaxLength(50)]
        public string DisposicionFinal { get; set; } = "ReciclajeCertificado";

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        [Required]
        public string UsuarioRegistro { get; set; } = string.Empty;
    }
}
