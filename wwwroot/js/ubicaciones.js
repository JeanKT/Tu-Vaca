// Listas en cascada Departamento -> Municipio (catálogos DANE).
// Uso en la vista:
//   <select data-municipios="IdDelSelectDeMunicipios" data-url="/Ubicaciones/Municipios"> (departamentos)
//   <select id="IdDelSelectDeMunicipios" data-seleccionado="IdDelMunicipioSiYaHabiaUno">  (municipios)
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('select[data-municipios]').forEach(function (selectDepartamento) {
        var selectMunicipio = document.getElementById(selectDepartamento.dataset.municipios);
        if (!selectMunicipio) {
            return;
        }
        var seleccionado = selectMunicipio.dataset.seleccionado || '';

        function cargarMunicipios() {
            var departamento = selectDepartamento.value;
            selectMunicipio.innerHTML = '';
            var vacio = document.createElement('option');
            vacio.value = '';
            vacio.textContent = departamento ? 'Cargando…' : 'Selecciona primero el departamento';
            selectMunicipio.appendChild(vacio);
            selectMunicipio.disabled = true;
            if (!departamento) {
                return;
            }

            fetch(selectDepartamento.dataset.url + '?departamentoId=' + encodeURIComponent(departamento))
                .then(function (respuesta) { return respuesta.json(); })
                .then(function (municipios) {
                    vacio.textContent = 'Selecciona…';
                    municipios.forEach(function (municipio) {
                        var opcion = document.createElement('option');
                        opcion.value = municipio.id;
                        opcion.textContent = municipio.nombre;
                        if (String(municipio.id) === seleccionado) {
                            opcion.selected = true;
                        }
                        selectMunicipio.appendChild(opcion);
                    });
                    selectMunicipio.disabled = false;
                    seleccionado = '';
                })
                .catch(function () {
                    vacio.textContent = 'No se pudieron cargar los municipios';
                });
        }

        selectDepartamento.addEventListener('change', cargarMunicipios);
        if (selectDepartamento.value) {
            cargarMunicipios();
        }
    });
});
