using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Usuario")]
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required, StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string Contrasena { get; set; } = string.Empty;

        // Propiedad auxiliar para compatibilidad con métodos Hash/Password
        [NotMapped]
        public string PasswordHash
        {
            get => Contrasena;
            set => Contrasena = value;
        }

        public int IdRol { get; set; }

        [ForeignKey("IdRol")]
        public virtual Rol? Rol { get; set; }
        public bool Activo { get; internal set; }
    }
}