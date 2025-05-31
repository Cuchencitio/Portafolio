$(document).ready(function () {
    // Variables
    let serviciosData = {};

    // Manejar cambio de proveedor
    $('#proveedorSelect').change(function () {
        const proveedorId = $(this).val();
        const selectedOption = $(this).find('option:selected');

        console.log('Proveedor seleccionado - ID:', proveedorId);
        console.log('Opción seleccionada:', selectedOption);
        console.log('Valor del select:', $(this).val());
        console.log('Texto de la opción:', selectedOption.text());

        if (proveedorId && proveedorId !== '') {
            // Mostrar información del proveedor
            $('#nombreNegocio').text(selectedOption.data('negocio'));
            $('#direccionNegocio').text(selectedOption.data('direccion'));
            $('#infoProveedor').slideDown();

            // Cargar servicios
            cargarServicios(proveedorId);
            $('#servicioSelect').prop('disabled', false);
        } else {
            console.log('ProveedorId está vacío o es falsy');
            $('#infoProveedor').slideUp();
            $('#infoServicio').slideUp();
            $('#servicioSelect').prop('disabled', true).html('<option value="">-- Primero selecciona un proveedor --</option>');
            resetearFormulario();
        }
    });

    // Manejar cambio de servicio
    $('#servicioSelect').change(function () {
        const servicioId = $(this).val();

        if (servicioId && serviciosData[servicioId]) {
            const servicio = serviciosData[servicioId];
            $('#nombreServicio').text(servicio.nombre);
            $('#descripcionServicio').text(servicio.descripcion || 'Sin descripción');
            $('#precioServicio').text('$' + servicio.precio.toLocaleString());
            $('#duracionServicio').text(servicio.duracion + ' minutos');
            $('#infoServicio').slideDown();

            cargarHorarios();
        } else {
            $('#infoServicio').slideUp();
            $('#horaSelect').prop('disabled', true).html('<option value="">-- Selecciona un servicio --</option>');
            $('#btnAgendar').prop('disabled', true);
        }
    });

    // Manejar cambio de fecha
    $('#fechaInput').change(function () {
        cargarHorarios();
    });

    // Manejar cambio de hora
    $('#horaSelect').change(function () {
        const hora = $(this).val();
        $('#btnAgendar').prop('disabled', !hora);
    });

    // Funciones auxiliares
    function cargarServicios(proveedorId) {
        console.log('Cargando servicios para proveedor:', proveedorId);

        $.get(`/Citas/GetServicios?proveedorId=${proveedorId}`)
            .done(function (data) {
                console.log('Servicios recibidos:', data);
                serviciosData = {};
                let options = '<option value="">-- Selecciona un servicio --</option>';

                data.forEach(function (servicio) {
                    serviciosData[servicio.id] = servicio;
                    options += `<option value="${servicio.id}">${servicio.nombre} - ${servicio.precio.toLocaleString()}</option>`;
                });

                $('#servicioSelect').html(options);
            })
            .fail(function (xhr, status, error) {
                console.error('Error al cargar servicios:', error);
                console.error('Status:', status);
                console.error('Response:', xhr.responseText);
                alert('Error al cargar los servicios: ' + error);
            });
    }

    function cargarHorarios() {
        const proveedorId = $('#proveedorSelect').val();
        const servicioId = $('#servicioSelect').val();
        const fecha = $('#fechaInput').val();

        console.log('Cargando horarios:', { proveedorId, servicioId, fecha });

        if (proveedorId && servicioId && fecha) {
            $('#horaSelect').prop('disabled', true).html('<option value="">Cargando...</option>');

            $.get(`/Citas/GetHorasDisponibles?proveedorId=${proveedorId}&servicioId=${servicioId}&fecha=${fecha}`)
                .done(function (data) {
                    console.log('Horarios recibidos:', data);
                    let options = '<option value="">-- Selecciona una hora --</option>';

                    if (data && data.length > 0) {
                        data.forEach(function (hora) {
                            options += `<option value="${hora.valor}">${hora.texto}</option>`;
                        });
                    } else {
                        options = '<option value="">No hay horarios disponibles</option>';
                    }

                    $('#horaSelect').html(options).prop('disabled', false);
                })
                .fail(function (xhr, status, error) {
                    console.error('Error al cargar horarios:', error);
                    console.error('Response:', xhr.responseText);
                    $('#horaSelect').html('<option value="">Error al cargar horarios</option>');
                });
        }
    }

    function resetearFormulario() {
        $('#ServicioId').val('');
        $('#Hora').val('');
        $('#btnAgendar').prop('disabled', true);
    }
});