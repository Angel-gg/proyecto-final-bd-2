using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Marca normalizada de hardware gamer (esquema catalogo, 3FN).
    /// </summary>
    public class Marca
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(80)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(80)]
        [Display(Name = "País")]
        public string? Pais { get; set; }

        public bool Activa { get; set; } = true;

        public ICollection<Componente> Componentes { get; set; } = new List<Componente>();
    }
}
