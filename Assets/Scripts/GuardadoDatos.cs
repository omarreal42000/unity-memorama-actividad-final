using System;
using System.IO;
using UnityEngine;

/// <summary>Datos esenciales que se guardan entre sesiones.</summary>
[Serializable]
public class DatosJugador
{
    public int nivelDesbloqueado = 1;
    public int[] mejoresPuntos = new int[3];
    public int[] estrellas = new int[3];
    public int puntosTotales;
    public float volumen = 0.8f;
    public bool sonidoActivo = true;
}

/// <summary>Guarda y carga el progreso en un archivo JSON (funciona en Android, iOS y PC).</summary>
public static class Guardado
{
    static DatosJugador _datos;

    static string Ruta
    {
        get { return Path.Combine(Application.persistentDataPath, "memorama_datos.json"); }
    }

    public static DatosJugador Datos
    {
        get
        {
            if (_datos == null) Cargar();
            return _datos;
        }
    }

    public static void Cargar()
    {
        try
        {
            if (File.Exists(Ruta))
                _datos = JsonUtility.FromJson<DatosJugador>(File.ReadAllText(Ruta));
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo leer el guardado: " + e.Message);
        }

        if (_datos == null) _datos = new DatosJugador();
        Array.Resize(ref _datos.mejoresPuntos, ConfigNiveles.Total);
        Array.Resize(ref _datos.estrellas, ConfigNiveles.Total);
        _datos.nivelDesbloqueado = Mathf.Clamp(_datos.nivelDesbloqueado, 1, ConfigNiveles.Total);
    }

    public static void Guardar()
    {
        try
        {
            File.WriteAllText(Ruta, JsonUtility.ToJson(Datos));
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo guardar el progreso: " + e.Message);
        }
    }

    public static void Borrar()
    {
        _datos = new DatosJugador();
        Cargar();
        Guardar();
    }

    /// <summary>Registra el resultado de un nivel ganado y desbloquea el siguiente.</summary>
    public static void RegistrarResultado(int nivelIdx, int puntos, int estrellas)
    {
        DatosJugador d = Datos;
        if (puntos > d.mejoresPuntos[nivelIdx]) d.mejoresPuntos[nivelIdx] = puntos;
        if (estrellas > d.estrellas[nivelIdx]) d.estrellas[nivelIdx] = estrellas;
        d.puntosTotales += puntos;
        d.nivelDesbloqueado = Mathf.Max(d.nivelDesbloqueado, Mathf.Min(nivelIdx + 2, ConfigNiveles.Total));
        Guardar();
    }
}
