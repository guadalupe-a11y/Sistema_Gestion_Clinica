
namespace Sistema_Gestión_Clinica.ViewModels
{
    public class IndicadoresViewModel
    {
        public DateTime Fecha { get; set; } = DateTime.Today;

        public int TotalCitas { get; set; }

        public int CitasPendientes { get; set; }

        public int PacientesEnEspera { get; set; }

        public int CitasFinalizadas { get; set; }

        public int CitasCanceladas { get; set; }

        public int PacientesNoPresentados { get; set; }
    }
}
