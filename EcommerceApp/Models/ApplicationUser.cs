using Microsoft.AspNetCore.Identity;

namespace EcommerceApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        // --- Datos personales (3FN: columna única NombreCompleto) ---
        public string? NombreCompleto { get; set; }

        // --- Jerarquía organizacional con FKs reales hacia esquema operaciones ---
        public int? RegionId { get; set; }
        public Region? Region { get; set; }

        public int? SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }

        // Código único alfanumérico para autocompletar en el POS (solo Clientes)
        public string? CodigoCliente { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
