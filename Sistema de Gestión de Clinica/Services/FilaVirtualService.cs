using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Services
{
    public class FilaVirtualService
    {
        private readonly SistemaDeGestionDB _context;

        public FilaVirtualService(SistemaDeGestionDB context)
        {
            _context = context;
        }

        public async Task<FilaVirtualViewModel?> ObtenerEstadoFilaAsync(int idCita)
        {
            var cita = await _context.Cita
                .Include(c => c.Medico)
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.IdCita == idCita);

            if (cita == null) return null;

            var pacientesAdelante = await _context.Cita
                .Where(c => c.IdMedico == cita.IdMedico
                         && c.FechaHora.Date == cita.FechaHora.Date
                         && c.IdCita < cita.IdCita
                         && (c.EstadoCita!.NombreEstado == "En Espera" || c.EstadoCita.NombreEstado == "Agendada"))
                .CountAsync();

            int duracion = cita.Medico?.DuracionConsultaMin ?? 20;

            return new FilaVirtualViewModel
            {
                IdCita = cita.IdCita,
                NombreMedico = $"Dr/a. {cita.Medico?.Nombre} {cita.Medico?.Apellido}",
                PacientesAdelante = pacientesAdelante,
                TiempoEstimadoMinutos = pacientesAdelante * duracion,
                EstadoActual = cita.EstadoCita?.NombreEstado ?? "Pendiente"
            };
        }
    }
}