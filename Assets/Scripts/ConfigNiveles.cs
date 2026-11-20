/// <summary>Parámetros de un nivel.</summary>
public class ConfigNivel
{
    public string nombre;
    public int columnas;
    public int filas;
    public float tiempo;            // segundos disponibles
    public float velocidadJugador;
    public float vistaPrevia;       // segundos que se muestran las cartas al empezar
    public float tiempoMostrar;     // segundos que se ve un par incorrecto antes de taparse
    public int enemigos;            // guardianes que patrullan
    public float velEnemigo;
    public int puntosPar;           // puntos base por pareja
}

/// <summary>Los tres niveles: cada uno añade más cartas, menos memoria visual y más peligros.</summary>
public static class ConfigNiveles
{
    public const string EscenaMenu = "Menu";
    public const string EscenaJuego = "Juego";

    public static int NivelActual;

    public static readonly ConfigNivel[] Niveles =
    {
        new ConfigNivel
        {
            nombre = "Nivel 1: El calentamiento",
            columnas = 3, filas = 2, tiempo = 60f, velocidadJugador = 5.5f,
            vistaPrevia = 3f, tiempoMostrar = 1.3f, enemigos = 0, velEnemigo = 0f, puntosPar = 100
        },
        new ConfigNivel
        {
            nombre = "Nivel 2: Los guardianes",
            columnas = 5, filas = 2, tiempo = 75f, velocidadJugador = 5.5f,
            vistaPrevia = 2f, tiempoMostrar = 1.0f, enemigos = 1, velEnemigo = 3f, puntosPar = 150
        },
        new ConfigNivel
        {
            nombre = "Nivel 3: El gran casino",
            columnas = 4, filas = 4, tiempo = 100f, velocidadJugador = 6f,
            vistaPrevia = 1.5f, tiempoMostrar = 0.7f, enemigos = 3, velEnemigo = 4f, puntosPar = 200
        }
    };

    public static int Total { get { return Niveles.Length; } }
}
