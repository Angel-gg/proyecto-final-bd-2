using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Región geográfica operativa para sucursales y gerencias regionales (esquema operaciones).
    /// </summary>
    public class Region
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        public bool Activa { get; set; } = true;

        public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
    }
}
