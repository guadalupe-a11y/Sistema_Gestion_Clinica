
using System;
using System.Collections.Generic;

namespace Sistema_Gestión_Clinica.ViewModels
{
    public class MonitorSalaViewModel
    {
        public DateTime Fecha { get; set; } = DateTime.Today;

        public List<TurnoMonitorViewModel> Turnos { get; set; }
            = new List<TurnoMonitorViewModel>();
    }

    public class TurnoMonitorViewModel
    {
        public string CodigoTurno { get; set; } = string.Empty;

        public string Consultorio { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public DateTime FechaHora { get; set; }
    }
}
