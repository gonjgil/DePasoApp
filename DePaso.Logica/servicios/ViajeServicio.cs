using DePaso.Entidades;
using DePaso.Logica.dtos;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Logica.servicios;

public sealed class ViajeServicio : IViajeServicio
{
    public const decimal DistanciaMaximaKm = 99.99m;
    public const decimal DesvioMaximoPermitidoKm = 99.99m;
    public const decimal PesoMaximoPermitidoKg = 999.99m;
    public const int MaxPaquetesPermitido = 100;
    public const int MaxParadas = 10;

    public static readonly IReadOnlySet<byte> EstadosEnCurso = new HashSet<byte>
        {
            (byte)EstadoEnvio.ConductorAsignado,
            (byte)EstadoEnvio.Retirado,
            (byte)EstadoEnvio.EnCamino,
        };

    private readonly DePasoAppContext _context;
    private readonly IServicioRutas _rutas;

    public ViajeServicio(DePasoAppContext context, IServicioRutas rutas)
    {
        _context = context;
        _rutas = rutas;
    }

    public async Task<IReadOnlyList<ViajeResumenDto>> ListarAsync(
        int usuarioId,
        FiltroViajes filtro,
        CancellationToken ct = default)
    {
        var consulta = _context.Viajes.AsNoTracking().Where(v => v.UsuarioId == usuarioId);

        consulta = filtro switch
        {
            FiltroViajes.Activos => consulta.Where(v => v.Activo == true),
            FiltroViajes.Inactivos => consulta.Where(v => v.Activo != true),
            _ => consulta,
        };

        var viajes = await consulta
            .Include(v => v.ViajeDia)
            .Include(v => v.Paradas)
            .Include(v => v.Asignaciones).ThenInclude(a => a.Envio)
            .ToListAsync(ct);

        return viajes
            .OrderByDescending(v => v.Activo == true)
            .ThenBy(v => v.HoraSalida)
            .ThenBy(v => v.Id)
            .Select(AResumen)
            .ToList();
    }

    public async Task<Resultado<ViajeDetalleDto>> ObtenerAsync(int id, int usuarioId, CancellationToken ct = default)
    {
        var (viaje, error) = await CargarPropioAsync(id, usuarioId, seguimiento: false, ct);
        return error ?? Resultado<ViajeDetalleDto>.Exitoso(ADetalle(viaje!));
    }

    public async Task<Resultado<IReadOnlyList<ParadaDto>>> ListarParadasAsync(
        int id,
        int usuarioId,
        CancellationToken ct = default)
    {
        var (viaje, error) = await CargarPropioAsync(id, usuarioId, seguimiento: false, ct);
        if (error is not null)
        {
            return error.Propagar<IReadOnlyList<ParadaDto>>();
        }

        return Resultado<IReadOnlyList<ParadaDto>>.Exitoso(AParadas(viaje!));
    }

    public async Task<Resultado<IReadOnlyList<PaqueteAsignadoDto>>> ListarPaquetesAsync(
        int id,
        int usuarioId,
        CancellationToken ct = default)
    {
        var (viaje, error) = await CargarPropioAsync(id, usuarioId, seguimiento: false, ct);
        if (error is not null)
        {
            return error.Propagar<IReadOnlyList<PaqueteAsignadoDto>>();
        }

        return Resultado<IReadOnlyList<PaqueteAsignadoDto>>.Exitoso(APaquetes(viaje!));
    }

    public async Task<Resultado<ViajeDetalleDto>> CrearAsync(ViajeDatos datos, CancellationToken ct = default)
    {
        var errores = ValidarDatos(datos);
        if (errores.Count > 0)
        {
            return Resultado<ViajeDetalleDto>.Invalido(errores);
        }

        var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Id == datos.UsuarioId, ct);
        if (!existeUsuario)
        {
            return Resultado<ViajeDetalleDto>.Fallo(TipoError.NoEncontrado, "Usuario no encontrado.");
        }

        var plan = OrdenarParadas(datos.Paradas).Select(d => new ParadaPlan(d, null)).ToList();
        var recorrido = await ResolverRecorridoAsync(datos.Origen, datos.Destino, plan, conocidas: null, ct);
        if (!recorrido.Exito)
        {
            return recorrido.Propagar<ViajeDetalleDto>();
        }

        var ruta = await CalcularRutaGuardableAsync(recorrido.Valor!, ct);
        if (!ruta.Exito)
        {
            return ruta.Propagar<ViajeDetalleDto>();
        }

        var viaje = new Viajes { UsuarioId = datos.UsuarioId, Activo = true };
        AplicarOrigen(viaje, recorrido.Valor!.Origen);
        AplicarDestino(viaje, recorrido.Valor.Destino);
        AplicarDatosGenerales(viaje, datos);
        viaje.DistanciaKm = ruta.Valor!.DistanciaKm;
        viaje.DuracionMin = ruta.Valor.DuracionMin;

        foreach (var dia in NormalizarDias(datos.Dias))
        {
            viaje.ViajeDia.Add(new ViajeDia { DiaSemana = dia });
        }

        SincronizarParadas(viaje, recorrido.Valor.Paradas);

        _context.Viajes.Add(viaje);
        await _context.SaveChangesAsync(ct);

        return Resultado<ViajeDetalleDto>.Exitoso(ADetalle(viaje));
    }

    public async Task<Resultado<ViajeDetalleDto>> ActualizarAsync(int id, ViajeDatos datos, CancellationToken ct = default)
    {
        var (viaje, error) = await CargarPropioAsync(id, datos.UsuarioId, seguimiento: true, ct);
        if (error is not null)
        {
            return error;
        }

        var errores = ValidarDatos(datos);
        if (errores.Count > 0)
        {
            return Resultado<ViajeDetalleDto>.Invalido(errores);
        }

        var paradasNuevas = OrdenarParadas(datos.Paradas);
        var diasNuevos = NormalizarDias(datos.Dias);
        var cambiaRecorrido = CambiaRecorrido(viaje!, datos.Origen, datos.Destino, paradasNuevas);
        var cambianDias = !diasNuevos.SetEquals(viaje!.ViajeDia.Select(d => d.DiaSemana));

        var enCurso = AsignacionesEnCurso(viaje);
        if (enCurso.Count > 0)
        {
            if (cambiaRecorrido || cambianDias)
            {
                return Resultado<ViajeDetalleDto>.Fallo(
                    TipoError.Conflicto,
                    $"El viaje tiene {enCurso.Count} envío(s) asignados en curso: no se puede cambiar el recorrido ni los días hasta que se entreguen o se liberen.");
            }

            var conflicto = VerificarCapacidadContraAsignados(enCurso, datos.MaxPaquetes, datos.PesoMaxKg, datos.DesvioMaxKm);
            if (conflicto is not null)
            {
                return Resultado<ViajeDetalleDto>.Fallo(TipoError.Conflicto, conflicto);
            }
        }

        if (cambiaRecorrido)
        {
            var plan = PlanDesdeDirecciones(viaje, paradasNuevas);
            var recorrido = await ResolverRecorridoAsync(datos.Origen, datos.Destino, plan, CoordenadasConocidas(viaje), ct);
            if (!recorrido.Exito)
            {
                return recorrido.Propagar<ViajeDetalleDto>();
            }

            var ruta = await CalcularRutaGuardableAsync(recorrido.Valor!, ct);
            if (!ruta.Exito)
            {
                return ruta.Propagar<ViajeDetalleDto>();
            }

            AplicarOrigen(viaje, recorrido.Valor!.Origen);
            AplicarDestino(viaje, recorrido.Valor.Destino);
            SincronizarParadas(viaje, recorrido.Valor.Paradas);
            viaje.DistanciaKm = ruta.Valor!.DistanciaKm;
            viaje.DuracionMin = ruta.Valor.DuracionMin;
        }
        else
        {
            AplicarTextoDireccion(viaje, datos.Origen, datos.Destino, paradasNuevas);
        }

        if (cambianDias)
        {
            SincronizarDias(viaje, diasNuevos);
        }

        AplicarDatosGenerales(viaje, datos);
        await _context.SaveChangesAsync(ct);

        return Resultado<ViajeDetalleDto>.Exitoso(ADetalle(viaje));
    }

    public async Task<Resultado<ViajeDetalleDto>> DesactivarAsync(int id, int usuarioId, CancellationToken ct = default)
    {
        var (viaje, error) = await CargarPropioAsync(id, usuarioId, seguimiento: true, ct);
        if (error is not null)
        {
            return error;
        }

        if (viaje!.Activo != true)
        {
            return Resultado<ViajeDetalleDto>.Fallo(TipoError.Conflicto, "El viaje ya está inactivo.");
        }

        var enCurso = AsignacionesEnCurso(viaje);
        if (enCurso.Count > 0)
        {
            return Resultado<ViajeDetalleDto>.Fallo(
                TipoError.Conflicto,
                $"No se puede desactivar: el viaje tiene {enCurso.Count} envío(s) asignados en curso.");
        }

        viaje.Activo = false;
        await _context.SaveChangesAsync(ct);
        return Resultado<ViajeDetalleDto>.Exitoso(ADetalle(viaje));
    }

    public async Task<Resultado<ViajeDetalleDto>> ActivarAsync(int id, int usuarioId, CancellationToken ct = default)
    {
        var (viaje, error) = await CargarPropioAsync(id, usuarioId, seguimiento: true, ct);
        if (error is not null)
        {
            return error;
        }

        if (viaje!.Activo == true)
        {
            return Resultado<ViajeDetalleDto>.Fallo(TipoError.Conflicto, "El viaje ya está activo.");
        }

        viaje.Activo = true;
        await _context.SaveChangesAsync(ct);
        return Resultado<ViajeDetalleDto>.Exitoso(ADetalle(viaje));
    }

    public async Task<Resultado<RutaDto>> CalcularRutaAsync(
        DireccionDatos origen,
        DireccionDatos destino,
        IReadOnlyList<ParadaDatos> paradas,
        CancellationToken ct = default)
    {
        var errores = ValidarRecorrido(origen, destino, paradas ?? Array.Empty<ParadaDatos>());
        if (errores.Count > 0)
        {
            return Resultado<RutaDto>.Invalido(errores);
        }

        var plan = OrdenarParadas(paradas ?? Array.Empty<ParadaDatos>()).Select(d => new ParadaPlan(d, null)).ToList();
        var recorrido = await ResolverRecorridoAsync(origen, destino, plan, conocidas: null, ct);
        if (!recorrido.Exito)
        {
            return recorrido.Propagar<RutaDto>();
        }

        var r = recorrido.Valor!;
        var ruta = await _rutas.CalcularRutaAsync(PuntosDe(r), ct);

        var puntos = new List<PuntoRutaDto> { new("Origen", 0, Texto(r.Origen.Dir), r.Origen.Lat, r.Origen.Lng) };
        puntos.AddRange(r.Paradas.Select((p, i) => new PuntoRutaDto("Parada", i + 1, Texto(p.Dir), p.Lat, p.Lng)));
        puntos.Add(new PuntoRutaDto("Destino", r.Paradas.Count + 1, Texto(r.Destino.Dir), r.Destino.Lat, r.Destino.Lng));

        var advertencia = ruta.DistanciaKm > DistanciaMaximaKm
            ? $"El recorrido mide {ruta.DistanciaKm} km y por ahora sólo se pueden guardar viajes de hasta {DistanciaMaximaKm} km."
            : null;

        return Resultado<RutaDto>.Exitoso(new RutaDto(ruta.DistanciaKm, ruta.DuracionMin, ruta.Estimada, puntos, advertencia));
    }

    public async Task<Resultado<ViajeDetalleDto>> AgregarParadaAsync(
        int id,
        int usuarioId,
        ParadaDatos parada,
        CancellationToken ct = default)
    {
        var (viaje, error) = await CargarEditableParaParadasAsync(id, usuarioId, ct);
        if (error is not null)
        {
            return error;
        }

        var plan = PlanActual(viaje!);
        var posicion = parada.Orden ?? plan.Count + 1;
        if (posicion < 1 || posicion > plan.Count + 1)
        {
            return Resultado<ViajeDetalleDto>.Invalido(
                $"El orden de la parada tiene que estar entre 1 y {plan.Count + 1}.");
        }

        plan.Insert(posicion - 1, new ParadaPlan(parada.Direccion, null));
        return await GuardarParadasAsync(viaje!, plan, ct);
    }

    public async Task<Resultado<ViajeDetalleDto>> ModificarParadaAsync(
        int id,
        int paradaId,
        int usuarioId,
        DireccionDatos direccion,
        CancellationToken ct = default)
    {
        var (viaje, error) = await CargarEditableParaParadasAsync(id, usuarioId, ct);
        if (error is not null)
        {
            return error;
        }

        var plan = PlanActual(viaje!);
        var indice = plan.FindIndex(p => p.IdExistente == paradaId);
        if (indice < 0)
        {
            return Resultado<ViajeDetalleDto>.Fallo(TipoError.NoEncontrado, "Parada no encontrada en este viaje.");
        }

        var anterior = plan[indice].Dir;
        var mismoLugar = TextoNormalizado.ClaveDireccion(anterior) == TextoNormalizado.ClaveDireccion(direccion);
        var nueva = TieneCoordenadas(direccion) || !mismoLugar
            ? direccion
            : direccion with { Lat = anterior.Lat, Lng = anterior.Lng };

        plan[indice] = new ParadaPlan(nueva, paradaId);
        return await GuardarParadasAsync(viaje!, plan, ct);
    }

    public async Task<Resultado<ViajeDetalleDto>> EliminarParadaAsync(
        int id,
        int paradaId,
        int usuarioId,
        CancellationToken ct = default)
    {
        var (viaje, error) = await CargarEditableParaParadasAsync(id, usuarioId, ct);
        if (error is not null)
        {
            return error;
        }

        var plan = PlanActual(viaje!);
        var quitadas = plan.RemoveAll(p => p.IdExistente == paradaId);
        if (quitadas == 0)
        {
            return Resultado<ViajeDetalleDto>.Fallo(TipoError.NoEncontrado, "Parada no encontrada en este viaje.");
        }

        return await GuardarParadasAsync(viaje!, plan, ct);
    }

    public async Task<Resultado<ViajeDetalleDto>> ReordenarParadasAsync(
        int id,
        int usuarioId,
        IReadOnlyList<int> paradaIds,
        CancellationToken ct = default)
    {
        var (viaje, error) = await CargarEditableParaParadasAsync(id, usuarioId, ct);
        if (error is not null)
        {
            return error;
        }

        var plan = PlanActual(viaje!);
        var ids = paradaIds ?? Array.Empty<int>();
        var esPermutacion = ids.Count == plan.Count
            && ids.Distinct().Count() == ids.Count
            && ids.All(i => plan.Any(p => p.IdExistente == i));
        if (!esPermutacion)
        {
            return Resultado<ViajeDetalleDto>.Invalido(
                "Para reordenar enviá todas las paradas del viaje, una sola vez cada una.");
        }

        var reordenado = ids.Select(i => plan.First(p => p.IdExistente == i)).ToList();
        return await GuardarParadasAsync(viaje!, reordenado, ct);
    }

    private async Task<(Viajes? Viaje, Resultado<ViajeDetalleDto>? Error)> CargarEditableParaParadasAsync(
        int id,
        int usuarioId,
        CancellationToken ct)
    {
        var (viaje, error) = await CargarPropioAsync(id, usuarioId, seguimiento: true, ct);
        if (error is not null)
        {
            return (null, error);
        }

        var enCurso = AsignacionesEnCurso(viaje!);
        if (enCurso.Count > 0)
        {
            return (null, Resultado<ViajeDetalleDto>.Fallo(
                TipoError.Conflicto,
                $"El viaje tiene {enCurso.Count} envío(s) asignados en curso: no se pueden modificar las paradas."));
        }

        return (viaje, null);
    }

    private async Task<Resultado<ViajeDetalleDto>> GuardarParadasAsync(Viajes viaje, List<ParadaPlan> plan, CancellationToken ct)
    {
        var origen = DireccionDeOrigen(viaje);
        var destino = DireccionDeDestino(viaje);

        var errores = ValidarParadas(origen, destino, plan.Select(p => new ParadaDatos(p.Dir)).ToList());
        if (errores.Count > 0)
        {
            return Resultado<ViajeDetalleDto>.Invalido(errores);
        }

        var recorrido = await ResolverRecorridoAsync(origen, destino, plan, CoordenadasConocidas(viaje), ct);
        if (!recorrido.Exito)
        {
            return recorrido.Propagar<ViajeDetalleDto>();
        }

        var ruta = await CalcularRutaGuardableAsync(recorrido.Valor!, ct);
        if (!ruta.Exito)
        {
            return ruta.Propagar<ViajeDetalleDto>();
        }

        SincronizarParadas(viaje, recorrido.Valor!.Paradas);
        viaje.DistanciaKm = ruta.Valor!.DistanciaKm;
        viaje.DuracionMin = ruta.Valor.DuracionMin;
        await _context.SaveChangesAsync(ct);

        return Resultado<ViajeDetalleDto>.Exitoso(ADetalle(viaje));
    }

    public static List<string> ValidarDatos(ViajeDatos d)
    {
        var errores = new List<string>();

        errores.AddRange(ValidarRecorrido(d.Origen, d.Destino, d.Paradas ?? Array.Empty<ParadaDatos>()));

        var dias = d.Dias ?? Array.Empty<byte>();
        if (dias.Count == 0)
        {
            errores.Add("Elegí al menos un día.");
        }
        else if (dias.Any(x => x is < 1 or > 7))
        {
            errores.Add("Los días tienen que estar entre 1 (lunes) y 7 (domingo).");
        }

        if (d.HoraLlegada <= d.HoraSalida)
        {
            errores.Add("La hora estimada de llegada tiene que ser posterior a la hora de salida.");
        }

        if (d.MaxPaquetes <= 0)
        {
            errores.Add("La cantidad máxima de paquetes tiene que ser mayor que cero.");
        }
        else if (d.MaxPaquetes > MaxPaquetesPermitido)
        {
            errores.Add($"La cantidad máxima de paquetes no puede superar {MaxPaquetesPermitido}.");
        }

        if (d.PesoMaxKg <= 0)
        {
            errores.Add("El peso máximo tiene que ser mayor que cero.");
        }
        else if (d.PesoMaxKg > PesoMaximoPermitidoKg)
        {
            errores.Add($"El peso máximo no puede superar {PesoMaximoPermitidoKg} kg.");
        }

        if (d.DesvioMaxKm <= 0)
        {
            errores.Add("El desvío máximo tiene que ser mayor que cero.");
        }
        else if (d.DesvioMaxKm > DesvioMaximoPermitidoKm)
        {
            errores.Add($"El desvío máximo no puede superar {DesvioMaximoPermitidoKm} km.");
        }

        return errores;
    }

    public static List<string> ValidarRecorrido(DireccionDatos? origen, DireccionDatos? destino, IReadOnlyList<ParadaDatos> paradas)
    {
        var errores = new List<string>();
        var origenOk = ValidarDireccion(origen, "origen", errores);
        var destinoOk = ValidarDireccion(destino, "destino", errores);

        if (origenOk && destinoOk)
        {
            if (TextoNormalizado.ClaveDireccion(origen!) == TextoNormalizado.ClaveDireccion(destino!))
            {
                errores.Add("El origen y el destino tienen que ser distintos.");
            }

            errores.AddRange(ValidarParadas(origen!, destino!, paradas));
        }

        return errores;
    }

    public static List<string> ValidarParadas(DireccionDatos origen, DireccionDatos destino, IReadOnlyList<ParadaDatos> paradas)
    {
        var errores = new List<string>();
        if (paradas.Count > MaxParadas)
        {
            errores.Add($"Un viaje admite hasta {MaxParadas} paradas.");
            return errores;
        }

        var ordenes = paradas.Where(p => p.Orden.HasValue).Select(p => p.Orden!.Value).ToList();
        if (ordenes.Any(o => o < 1))
        {
            errores.Add("El orden de las paradas empieza en 1.");
        }

        if (ordenes.Distinct().Count() != ordenes.Count)
        {
            errores.Add("Dos paradas no pueden tener el mismo número de orden.");
        }

        var claveOrigen = TextoNormalizado.ClaveDireccion(origen);
        var claveDestino = TextoNormalizado.ClaveDireccion(destino);
        var vistas = new HashSet<string>();

        for (var i = 0; i < paradas.Count; i++)
        {
            var etiqueta = $"la parada {i + 1}";
            if (!ValidarDireccion(paradas[i]?.Direccion, etiqueta, errores))
            {
                continue;
            }

            var clave = TextoNormalizado.ClaveDireccion(paradas[i].Direccion);
            if (clave == claveOrigen || clave == claveDestino)
            {
                errores.Add($"La parada {i + 1} no puede coincidir con el origen o el destino.");
            }
            else if (!vistas.Add(clave))
            {
                errores.Add($"La parada {i + 1} está repetida.");
            }
        }

        return errores;
    }

    private static bool ValidarDireccion(DireccionDatos? d, string etiqueta, List<string> errores)
    {
        if (d is null)
        {
            errores.Add($"Completá la dirección de {etiqueta}.");
            return false;
        }

        var antes = errores.Count;
        if (string.IsNullOrWhiteSpace(d.Calle))
        {
            errores.Add($"Ingresá la calle de {etiqueta}.");
        }

        if (d.Altura <= 0)
        {
            errores.Add($"Ingresá una altura válida para {etiqueta}.");
        }

        if (string.IsNullOrWhiteSpace(d.Localidad))
        {
            errores.Add($"Ingresá la localidad de {etiqueta}.");
        }

        if (string.IsNullOrWhiteSpace(d.Provincia))
        {
            errores.Add($"Ingresá la provincia de {etiqueta}.");
        }

        if (d.Lat is < -90 or > 90 || d.Lng is < -180 or > 180)
        {
            errores.Add($"Las coordenadas de {etiqueta} no son válidas.");
        }

        return errores.Count == antes;
    }

    private static string? VerificarCapacidadContraAsignados(
        IReadOnlyList<Asignaciones> enCurso,
        int maxPaquetes,
        decimal pesoMaxKg,
        decimal desvioMaxKm)
    {
        foreach (var salida in enCurso.GroupBy(a => a.FechaViaje).OrderBy(g => g.Key))
        {
            var fecha = salida.Key.ToString("dd/MM/yyyy");
            var cantidad = salida.Count();
            var peso = salida.Sum(a => a.Envio.PesoKg);
            var desvio = salida.Sum(a => a.DesvioKm);

            if (cantidad > maxPaquetes)
            {
                return $"El {fecha} hay {cantidad} paquetes asignados: la capacidad no puede ser menor a eso.";
            }

            if (peso > pesoMaxKg)
            {
                return $"El {fecha} hay {peso} kg asignados: el peso máximo no puede ser menor a eso.";
            }

            if (desvio > desvioMaxKm)
            {
                return $"El {fecha} los envíos asignados suman {desvio} km de desvío: el desvío máximo no puede ser menor a eso.";
            }
        }

        return null;
    }

    private sealed record ParadaPlan(DireccionDatos Dir, int? IdExistente);

    private sealed record PuntoResuelto(DireccionDatos Dir, decimal Lat, decimal Lng, int? IdExistente);

    private sealed record Recorrido(PuntoResuelto Origen, PuntoResuelto Destino, List<PuntoResuelto> Paradas);

    private async Task<Resultado<Recorrido>> ResolverRecorridoAsync(
        DireccionDatos origen,
        DireccionDatos destino,
        IReadOnlyList<ParadaPlan> paradas,
        IReadOnlyDictionary<string, (decimal Lat, decimal Lng)>? conocidas,
        CancellationToken ct)
    {
        var o = await ResolverPuntoAsync(origen, null, conocidas, ct);
        if (o is null)
        {
            return Resultado<Recorrido>.Invalido("No se pudo ubicar la dirección de origen.");
        }

        var d = await ResolverPuntoAsync(destino, null, conocidas, ct);
        if (d is null)
        {
            return Resultado<Recorrido>.Invalido("No se pudo ubicar la dirección de destino.");
        }

        if (o.Lat == d.Lat && o.Lng == d.Lng)
        {
            return Resultado<Recorrido>.Invalido("El origen y el destino tienen que ser distintos.");
        }

        var resueltas = new List<PuntoResuelto>();
        for (var i = 0; i < paradas.Count; i++)
        {
            var p = await ResolverPuntoAsync(paradas[i].Dir, paradas[i].IdExistente, conocidas, ct);
            if (p is null)
            {
                return Resultado<Recorrido>.Invalido($"No se pudo ubicar la dirección de la parada {i + 1}.");
            }

            resueltas.Add(p);
        }

        return Resultado<Recorrido>.Exitoso(new Recorrido(o, d, resueltas));
    }

    private async Task<PuntoResuelto?> ResolverPuntoAsync(
        DireccionDatos dir,
        int? idExistente,
        IReadOnlyDictionary<string, (decimal Lat, decimal Lng)>? conocidas,
        CancellationToken ct)
    {
        if (TieneCoordenadas(dir))
        {
            return new PuntoResuelto(dir, dir.Lat!.Value, dir.Lng!.Value, idExistente);
        }

        if (conocidas is not null && conocidas.TryGetValue(TextoNormalizado.ClaveDireccion(dir), out var c))
        {
            return new PuntoResuelto(dir, c.Lat, c.Lng, idExistente);
        }

        var geo = await _rutas.GeocodificarAsync(dir, ct);
        if (geo is null)
        {
            return null;
        }

        return new PuntoResuelto(
            dir,
            Math.Round((decimal)geo.Value.Lat, 6),
            Math.Round((decimal)geo.Value.Lng, 6),
            idExistente);
    }

    private async Task<Resultado<RutaCalculada>> CalcularRutaGuardableAsync(Recorrido recorrido, CancellationToken ct)
    {
        var ruta = await _rutas.CalcularRutaAsync(PuntosDe(recorrido), ct);
        if (ruta.DistanciaKm > DistanciaMaximaKm)
        {
            return Resultado<RutaCalculada>.Invalido(
                $"El recorrido mide {ruta.DistanciaKm} km y por ahora sólo se pueden guardar viajes de hasta {DistanciaMaximaKm} km.");
        }

        return Resultado<RutaCalculada>.Exitoso(ruta);
    }

    private static List<Coordenada> PuntosDe(Recorrido r)
    {
        var puntos = new List<Coordenada> { new((double)r.Origen.Lat, (double)r.Origen.Lng) };
        puntos.AddRange(r.Paradas.Select(p => new Coordenada((double)p.Lat, (double)p.Lng)));
        puntos.Add(new Coordenada((double)r.Destino.Lat, (double)r.Destino.Lng));
        return puntos;
    }

    private static bool TieneCoordenadas(DireccionDatos d) =>
        d.Lat is not null && d.Lng is not null && (d.Lat != 0 || d.Lng != 0);

    private async Task<(Viajes? Viaje, Resultado<ViajeDetalleDto>? Error)> CargarPropioAsync(
        int id,
        int usuarioId,
        bool seguimiento,
        CancellationToken ct)
    {
        IQueryable<Viajes> consulta = _context.Viajes
            .Include(v => v.ViajeDia)
            .Include(v => v.Paradas)
            .Include(v => v.Asignaciones).ThenInclude(a => a.Envio);

        if (!seguimiento)
        {
            consulta = consulta.AsNoTracking();
        }

        var viaje = await consulta.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (viaje is null)
        {
            return (null, Resultado<ViajeDetalleDto>.Fallo(TipoError.NoEncontrado, "Viaje no encontrado."));
        }

        if (viaje.UsuarioId != usuarioId)
        {
            return (null, Resultado<ViajeDetalleDto>.Fallo(TipoError.Prohibido, "El viaje pertenece a otro usuario."));
        }

        return (viaje, null);
    }

    private static List<Asignaciones> AsignacionesEnCurso(Viajes viaje) =>
        viaje.Asignaciones.Where(a => a.Envio is not null && EstadosEnCurso.Contains(a.Envio.Estado)).ToList();

    private static List<DireccionDatos> OrdenarParadas(IReadOnlyList<ParadaDatos> paradas) =>
        (paradas ?? Array.Empty<ParadaDatos>())
            .Select((p, i) => (p, i))
            .OrderBy(x => x.p.Orden ?? int.MaxValue)
            .ThenBy(x => x.i)
            .Select(x => x.p.Direccion)
            .ToList();

    private static HashSet<byte> NormalizarDias(IReadOnlyList<byte> dias) =>
        (dias ?? Array.Empty<byte>()).Where(d => d is >= 1 and <= 7).ToHashSet();

    private static List<ParadaPlan> PlanActual(Viajes viaje) =>
        viaje.Paradas.OrderBy(p => p.Orden)
            .Select(p => new ParadaPlan(DireccionDeParada(p), p.Id))
            .ToList();

    private static List<ParadaPlan> PlanDesdeDirecciones(Viajes viaje, List<DireccionDatos> nuevas)
    {
        var libres = viaje.Paradas.ToList();
        var plan = new List<ParadaPlan>();
        foreach (var dir in nuevas)
        {
            var clave = TextoNormalizado.ClaveDireccion(dir);
            var existente = libres.FirstOrDefault(p => ClaveParada(p) == clave);
            if (existente is not null)
            {
                libres.Remove(existente);
            }

            plan.Add(new ParadaPlan(dir, existente?.Id));
        }

        return plan;
    }

    private static Dictionary<string, (decimal Lat, decimal Lng)> CoordenadasConocidas(Viajes viaje)
    {
        var conocidas = new Dictionary<string, (decimal, decimal)>
        {
            [TextoNormalizado.Clave(viaje.OrigenCalle, viaje.OrigenAltura, viaje.OrigenLocalidad, viaje.OrigenProvincia)] =
                (viaje.OrigenLat, viaje.OrigenLng),
            [TextoNormalizado.Clave(viaje.DestinoCalle, viaje.DestinoAltura, viaje.DestinoLocalidad, viaje.DestinoProvincia)] =
                (viaje.DestinoLat, viaje.DestinoLng),
        };
        foreach (var p in viaje.Paradas)
        {
            conocidas[ClaveParada(p)] = (p.Lat, p.Lng);
        }

        return conocidas;
    }

    private static bool CambiaRecorrido(Viajes viaje, DireccionDatos origen, DireccionDatos destino, List<DireccionDatos> paradas)
    {
        if (TextoNormalizado.ClaveDireccion(origen)
                != TextoNormalizado.Clave(viaje.OrigenCalle, viaje.OrigenAltura, viaje.OrigenLocalidad, viaje.OrigenProvincia)
            || CoordenadasDistintas(origen, viaje.OrigenLat, viaje.OrigenLng))
        {
            return true;
        }

        if (TextoNormalizado.ClaveDireccion(destino)
                != TextoNormalizado.Clave(viaje.DestinoCalle, viaje.DestinoAltura, viaje.DestinoLocalidad, viaje.DestinoProvincia)
            || CoordenadasDistintas(destino, viaje.DestinoLat, viaje.DestinoLng))
        {
            return true;
        }

        var actuales = viaje.Paradas.OrderBy(p => p.Orden).ToList();
        if (actuales.Count != paradas.Count)
        {
            return true;
        }

        for (var i = 0; i < actuales.Count; i++)
        {
            if (ClaveParada(actuales[i]) != TextoNormalizado.ClaveDireccion(paradas[i])
                || CoordenadasDistintas(paradas[i], actuales[i].Lat, actuales[i].Lng))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CoordenadasDistintas(DireccionDatos d, decimal lat, decimal lng) =>
        TieneCoordenadas(d) && (Math.Abs(d.Lat!.Value - lat) > 0.000001m || Math.Abs(d.Lng!.Value - lng) > 0.000001m);

    private static void AplicarDatosGenerales(Viajes viaje, ViajeDatos d)
    {
        viaje.HoraSalida = d.HoraSalida;
        viaje.HoraLlegada = d.HoraLlegada;
        viaje.MaxPaquetes = d.MaxPaquetes;
        viaje.PesoMaxKg = d.PesoMaxKg;
        viaje.DesvioMaxKm = d.DesvioMaxKm;
    }

    private static void AplicarOrigen(Viajes v, PuntoResuelto p)
    {
        AplicarTextoOrigen(v, p.Dir);
        v.OrigenLat = p.Lat;
        v.OrigenLng = p.Lng;
    }

    private static void AplicarDestino(Viajes v, PuntoResuelto p)
    {
        AplicarTextoDestino(v, p.Dir);
        v.DestinoLat = p.Lat;
        v.DestinoLng = p.Lng;
    }

    private static void AplicarTextoOrigen(Viajes v, DireccionDatos d)
    {
        v.OrigenCalle = d.Calle.Trim();
        v.OrigenAltura = d.Altura;
        v.OrigenPiso = Opcional(d.Piso);
        v.OrigenDepartamento = Opcional(d.Departamento);
        v.OrigenLocalidad = d.Localidad.Trim();
        v.OrigenProvincia = d.Provincia.Trim();
    }

    private static void AplicarTextoDestino(Viajes v, DireccionDatos d)
    {
        v.DestinoCalle = d.Calle.Trim();
        v.DestinoAltura = d.Altura;
        v.DestinoPiso = Opcional(d.Piso);
        v.DestinoDepartamento = Opcional(d.Departamento);
        v.DestinoLocalidad = d.Localidad.Trim();
        v.DestinoProvincia = d.Provincia.Trim();
    }

    private static void AplicarTextoDireccion(Viajes v, DireccionDatos origen, DireccionDatos destino, List<DireccionDatos> paradas)
    {
        AplicarTextoOrigen(v, origen);
        AplicarTextoDestino(v, destino);
        var actuales = v.Paradas.OrderBy(p => p.Orden).ToList();
        for (var i = 0; i < actuales.Count && i < paradas.Count; i++)
        {
            AplicarTextoParada(actuales[i], paradas[i]);
        }
    }

    private static void AplicarTextoParada(Paradas p, DireccionDatos d)
    {
        p.Calle = d.Calle.Trim();
        p.Altura = d.Altura;
        p.Piso = Opcional(d.Piso);
        p.Departamento = Opcional(d.Departamento);
        p.Localidad = d.Localidad.Trim();
        p.Provincia = d.Provincia.Trim();
        p.Direccion = Recortar(Texto(d), 255);
    }

    private void SincronizarParadas(Viajes viaje, List<PuntoResuelto> nuevas)
    {
        var existentes = viaje.Paradas.ToList();
        var usadas = new HashSet<int>();

        for (var i = 0; i < nuevas.Count; i++)
        {
            var punto = nuevas[i];
            var entidad = punto.IdExistente is int id ? existentes.FirstOrDefault(p => p.Id == id) : null;
            if (entidad is null)
            {
                entidad = new Paradas();
                viaje.Paradas.Add(entidad);
            }
            else
            {
                usadas.Add(entidad.Id);
            }

            AplicarTextoParada(entidad, punto.Dir);
            entidad.Orden = i + 1;
            entidad.Lat = punto.Lat;
            entidad.Lng = punto.Lng;
        }

        foreach (var sobrante in existentes.Where(p => !usadas.Contains(p.Id)))
        {
            _context.Paradas.Remove(sobrante);
        }
    }

    private void SincronizarDias(Viajes viaje, HashSet<byte> dias)
    {
        foreach (var dia in viaje.ViajeDia.Where(d => !dias.Contains(d.DiaSemana)).ToList())
        {
            _context.ViajeDia.Remove(dia);
        }

        var actuales = viaje.ViajeDia.Select(d => d.DiaSemana).ToHashSet();
        foreach (var dia in dias.Where(d => !actuales.Contains(d)))
        {
            viaje.ViajeDia.Add(new ViajeDia { ViajeId = viaje.Id, DiaSemana = dia });
        }
    }

    private static string? Opcional(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static string Recortar(string texto, int max) => texto.Length <= max ? texto : texto[..max];

    private static string ClaveParada(Paradas p) => TextoNormalizado.Clave(p.Calle, p.Altura, p.Localidad, p.Provincia);

    private static string Texto(DireccionDatos d) =>
        $"{d.Calle.Trim()} {d.Altura}, {d.Localidad.Trim()}, {d.Provincia.Trim()}";

    private static string Texto(string calle, int altura, string localidad, string provincia) =>
        $"{calle} {altura}, {localidad}, {provincia}";

    private static DireccionDatos DireccionDeOrigen(Viajes v) =>
        new(v.OrigenCalle, v.OrigenAltura, v.OrigenPiso, v.OrigenDepartamento, v.OrigenLocalidad, v.OrigenProvincia, v.OrigenLat, v.OrigenLng);

    private static DireccionDatos DireccionDeDestino(Viajes v) =>
        new(v.DestinoCalle, v.DestinoAltura, v.DestinoPiso, v.DestinoDepartamento, v.DestinoLocalidad, v.DestinoProvincia, v.DestinoLat, v.DestinoLng);

    private static DireccionDatos DireccionDeParada(Paradas p) =>
        new(p.Calle, p.Altura, p.Piso, p.Departamento, p.Localidad, p.Provincia, p.Lat, p.Lng);

    private static DireccionDto ADireccionDto(DireccionDatos d) =>
        new(d.Calle, d.Altura, d.Piso, d.Departamento, d.Localidad, d.Provincia, d.Lat ?? 0, d.Lng ?? 0, Texto(d));

    private static List<ParadaDto> AParadas(Viajes v) =>
        v.Paradas.OrderBy(p => p.Orden)
            .Select(p => new ParadaDto(p.Id, p.Orden, ADireccionDto(DireccionDeParada(p))))
            .ToList();

    private static List<PaqueteAsignadoDto> APaquetes(Viajes v) =>
        v.Asignaciones
            .Where(a => a.Envio is not null)
            .OrderBy(a => a.FechaViaje)
            .ThenBy(a => a.Id)
            .Select(a => new PaqueteAsignadoDto(
                a.Id,
                a.EnvioId,
                a.Envio.Descripcion,
                Texto(a.Envio.OrigenCalle, a.Envio.OrigenAltura, a.Envio.OrigenLocalidad, a.Envio.OrigenProvincia),
                Texto(a.Envio.DestinoCalle, a.Envio.DestinoAltura, a.Envio.DestinoLocalidad, a.Envio.DestinoProvincia),
                a.FechaViaje,
                a.Envio.PesoKg,
                a.Ganancia,
                a.DesvioKm,
                (EstadoEnvio)a.Envio.Estado,
                EstadosEnvio.Texto(a.Envio.Estado)))
            .ToList();

    private static List<byte> DiasOrdenados(Viajes v) =>
        v.ViajeDia.Select(d => d.DiaSemana).Distinct().OrderBy(d => d).ToList();

    private static ViajeResumenDto AResumen(Viajes v)
    {
        var dias = DiasOrdenados(v);
        return new ViajeResumenDto(
            v.Id,
            Texto(v.OrigenCalle, v.OrigenAltura, v.OrigenLocalidad, v.OrigenProvincia),
            Texto(v.DestinoCalle, v.DestinoAltura, v.DestinoLocalidad, v.DestinoProvincia),
            dias,
            DiasSemana.Formatear(dias),
            v.HoraSalida,
            v.HoraLlegada,
            v.MaxPaquetes,
            v.PesoMaxKg,
            v.DesvioMaxKm,
            v.DistanciaKm,
            v.DuracionMin,
            v.Activo == true,
            v.Paradas.Count,
            AsignacionesEnCurso(v).Count);
    }

    private static ViajeDetalleDto ADetalle(Viajes v)
    {
        var dias = DiasOrdenados(v);
        return new ViajeDetalleDto(
            v.Id,
            v.UsuarioId,
            ADireccionDto(DireccionDeOrigen(v)),
            ADireccionDto(DireccionDeDestino(v)),
            dias,
            DiasSemana.Formatear(dias),
            v.HoraSalida,
            v.HoraLlegada,
            v.MaxPaquetes,
            v.PesoMaxKg,
            v.DesvioMaxKm,
            v.DistanciaKm,
            v.DuracionMin,
            v.Activo == true,
            AParadas(v),
            APaquetes(v));
    }
}