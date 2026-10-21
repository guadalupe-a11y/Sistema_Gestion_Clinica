using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Gestión_Clinica.Models
{
    [Table("Expediente")]
    public class Expediente
    {
        [Key]
        public int IdExpediente { get; set; }
        public int IdPaciente { get; set; }
        public DateTime FechaApertura { get; set; } = DateTime.Now;
        public string? GrupoSanguineo { get; set; }
        public string? Alergias { get; set; }
        public string? Antecedentes { get; set; }

        [ForeignKey("IdPaciente")]
        public virtual Paciente? Paciente { get; set; }
    }
}
