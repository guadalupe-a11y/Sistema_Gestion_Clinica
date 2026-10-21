using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Rol")]
    public class Rol
    {
        [Key]
        public int IdRol { get; set; }

        [Required, StringLength(30)]
        public string NombreRol { get; set; } = string.Empty;

        // Relación uno a muchos con Usuario (opcional pero recomendada para navegación)
        public virtual ICollection<Usuario>? Usuarios { get; set; }
    }
}