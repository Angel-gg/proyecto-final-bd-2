using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Categoría normalizada de componentes (esquema catalogo, 3FN).
    /// </summary>
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(80)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Slug")]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;

        public ICollection<Componente> Componentes { get; set; } = new List<Componente>();
    }
}
