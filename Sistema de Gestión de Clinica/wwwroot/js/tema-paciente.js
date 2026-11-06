(function () {
    var KEY = 'tema-paciente';
    var temas = [
        { id: 'claro', nombre: 'Claro', icono: 'fa-sun', fondo: 'linear-gradient(135deg,#ffffff,#e2e8f0)' },
        { id: 'oscuro', nombre: 'Oscuro', icono: 'fa-moon', fondo: 'linear-gradient(135deg,#0b1020,#38bdf8)' },
        { id: 'neon', nombre: 'Neón', icono: 'fa-bolt', fondo: 'linear-gradient(135deg,#00f5d4,#ff2e93)' },
        { id: 'aurora', nombre: 'Aurora', icono: 'fa-wand-magic-sparkles', fondo: 'linear-gradient(135deg,#312e81,#0e7490,#065f46)' },
        { id: 'atardecer', nombre: 'Atardecer', icono: 'fa-cloud-sun', fondo: 'linear-gradient(135deg,#ffd6e0,#f97316)' }
    ];

    var actual = 'claro';
    try { actual = localStorage.getItem(KEY) || 'claro'; } catch (e) { }
    if (!temas.some(function (t) { return t.id === actual; })) actual = 'claro';

    // Se aplica de inmediato para que no parpadee al cargar la página
    document.documentElement.setAttribute('data-theme', actual);

    function aplicar(id) {
        document.documentElement.setAttribute('data-theme', id);
        try { localStorage.setItem(KEY, id); } catch (e) { }
        document.querySelectorAll('#themePanel button').forEach(function (b) {
            b.classList.toggle('activo', b.getAttribute('data-tema') === id);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var fab = document.createElement('button');
        fab.id = 'themeFab';
        fab.type = 'button';
        fab.title = 'Cambiar apariencia';
        fab.setAttribute('aria-label', 'Cambiar apariencia');
        fab.innerHTML = '<i class="fa-solid fa-palette"></i>';

        var panel = document.createElement('div');
        panel.id = 'themePanel';
        panel.innerHTML = '<h6>Apariencia</h6>' + temas.map(function (t) {
            return '<button type="button" data-tema="' + t.id + '">' +
                '<span class="muestra" style="background:' + t.fondo + '"></span>' +
                '<i class="fa-solid ' + t.icono + '"></i> ' + t.nombre + '</button>';
        }).join('');

        document.body.appendChild(panel);
        document.body.appendChild(fab);

        fab.addEventListener('click', function (e) { e.stopPropagation(); panel.classList.toggle('abierto'); });
        panel.addEventListener('click', function (e) {
            var b = e.target.closest('button[data-tema]');
            if (b) aplicar(b.getAttribute('data-tema'));
        });
        document.addEventListener('click', function () { panel.classList.remove('abierto'); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') panel.classList.remove('abierto'); });

        aplicar(actual);
    });
})();