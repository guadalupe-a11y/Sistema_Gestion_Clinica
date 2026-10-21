using Sistema_de_Gestión_de_Clinica.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Usuario")]
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }
        public int IdRol { get; set; }

        [Required, StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        [ForeignKey("IdRol")]
        public virtual Rol? Rol { get; set; }
    }
}
