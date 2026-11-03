// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Llamada AJAX para obtener notificaciones activas sin recargar la página
function cargarNotificaciones() {
    fetch('/Paciente/ObtenerNotificaciones')
        .then(response => response.json())
        .then(data => {
            let container = document.getElementById('notificationMenu');
            let badge = document.getElementById('notificationBadge');

            badge.innerText = data.length;
            container.innerHTML = '';

            if (data.length === 0) {
                container.innerHTML = '<li class="dropdown-item text-muted">No tienes notificaciones</li>';
            } else {
                data.forEach(notif => {
                    container.innerHTML += `
                        <li>
                            <a class="dropdown-item small" href="${notif.url}">
                                <strong>${notif.titulo}</strong><br/>
                                <span class="text-muted">${notif.mensaje}</span>
                            </a>
                        </li><hr class="dropdown-divider">`;
                });
            }
        });
}

// Ejecutar cada 10 segundos para tiempo real
setInterval(cargarNotificaciones, 10000);
