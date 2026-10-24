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

        // La propiedad en C# es Contrasena, pero apunta a la columna SQL "PasswordHash"
        [Required, StringLength(255)]
        [Column("PasswordHash")]
        public string Contrasena { get; set; } = string.Empty;

        // Propiedad auxiliar para que compile DbInitializer y cualquier otra llamada
        [NotMapped]
        public string PasswordHash
        {
            get => Contrasena;
            set => Contrasena = value;
        }

        public int IdRol { get; set; }

        [ForeignKey("IdRol")]
        public virtual Rol? Rol { get; set; }

        public bool Activo { get; set; }
    }
}