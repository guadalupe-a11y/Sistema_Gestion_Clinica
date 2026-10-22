namespace Sistema_Gestión_Clinica.ViewModels
{
    public class FilaVirtualViewModel
    {
        public int IdCita { get; set; }
        public string NombreMedico { get; set; } = string.Empty;
        public int PacientesAdelante { get; set; }
        public int TiempoEstimadoMinutos { get; set; }
        public string EstadoActual { get; set; } = string.Empty;
    }
}
