const formulario = document.querySelector("form[data-url-calles]");

for (const lado of ["Origen", "Destino"]) {
  const calle = formulario.querySelector(`#${lado}Calle`);
  const localidad = formulario.querySelector(`#${lado}Localidad`);
  const provincia = formulario.querySelector(`#${lado}Provincia`);
  const altura = formulario.querySelector(`#${lado}Altura`);
  const lista = formulario.querySelector(`#${lado}Sugerencias`);
  const estado = formulario.querySelector(`#${lado}EstadoCalle`);
  let espera;
  let version = 0;

  calle.addEventListener("input", () => {
    clearTimeout(espera);
    const versionActual = ++version;

    lista.replaceChildren();
    estado.textContent = "";
    localidad.value = "";
    provincia.value = "";

    const texto = calle.value.trim();
    if (texto.length < 3) return;

    espera = setTimeout(async () => {
      const url = new URL(formulario.dataset.urlCalles, window.location.origin);
      url.searchParams.set("texto", texto);

      try {
        const respuesta = await fetch(url);
        if (!respuesta.ok) throw new Error("Error de búsqueda");

        const sugerencias = await respuesta.json();
        if (versionActual !== version) return;

        if (sugerencias.length === 0) {
          estado.textContent = "No se encontraron calles.";
          return;
        }

        for (const sugerencia of sugerencias) {
          const opcion = document.createElement("button");
          opcion.type = "button";
          opcion.className = "list-group-item list-group-item-action";
          opcion.textContent = `${sugerencia.calle}, ${sugerencia.localidad}, ${sugerencia.provincia}`;

          opcion.addEventListener("click", () => {
            calle.value = sugerencia.calle;
            localidad.value = sugerencia.localidad;
            provincia.value = sugerencia.provincia;
            lista.replaceChildren();
            estado.textContent = "";
            altura.focus();
          });

          lista.append(opcion);
        }
      } catch {
        if (versionActual === version) {
          estado.textContent = "No se pudieron cargar las sugerencias.";
        }
      }
    }, 400);
  });

  formulario.addEventListener("submit", (evento) => {
    if (localidad.value.trim() && provincia.value.trim()) return;

    evento.preventDefault();
    estado.textContent = "Elegí una calle de la lista.";
  });
}
