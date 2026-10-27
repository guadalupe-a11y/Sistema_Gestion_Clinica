using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;

namespace Sistema_Gestión_Clinica.Services
{
    public class ReasignacionListaEsperaService
    {
        private readonly SistemaDeGestionDB _context;

        public ReasignacionListaEsperaService(SistemaDeGestionDB context)
        {
            _context = context;
        }

        public async Task ReasignarCupoLiberadoAsync(int idMedico, DateTime fechaHora)
        {
            var enEspera = await _context.ListaEspera
                .Where(l => l.IdMedico == idMedico && !l.Atendido)
                .OrderBy(l => l.FechaRegistro)
                .FirstOrDefaultAsync();

            if (enEspera != null)
            {
                enEspera.Atendido = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}