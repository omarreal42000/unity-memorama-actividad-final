using UnityEngine;

/// <summary>Guardián que patrulla entre dos puntos. Si toca al jugador le quita tiempo.</summary>
public class Enemigo : MonoBehaviour
{
    Vector3 a, b;
    float velocidad, fase, largo;
    Transform modelo;

    public static Enemigo Crear(Transform padre, Vector3 desde, Vector3 hasta, float vel, float faseInicial)
    {
        GameObject go = new GameObject("Guardian");
        go.transform.SetParent(padre, false);
        go.transform.position = desde;

        Enemigo e = go.AddComponent<Enemigo>();
        e.a = desde;
        e.b = hasta;
        e.velocidad = vel;
        e.fase = faseInicial;
        e.largo = Mathf.Max(0.1f, Vector3.Distance(desde, hasta));

        Material mat = Materiales.Cargar("Enemigo", new Color(0.9f, 0.1f, 0.1f));
        e.modelo = new GameObject("Modelo").transform;
        e.modelo.SetParent(go.transform, false);
        Geo.Crear(PrimitiveType.Sphere, e.modelo, "Nucleo", Vector3.zero, Vector3.one * 1.1f, mat);
        Geo.Crear(PrimitiveType.Cube, e.modelo, "PuaY", Vector3.zero, new Vector3(0.22f, 2f, 0.22f), mat);
        Geo.Crear(PrimitiveType.Cube, e.modelo, "PuaX", Vector3.zero, new Vector3(0.22f, 2f, 0.22f), mat, new Vector3(0f, 0f, 90f));
        Geo.Crear(PrimitiveType.Cube, e.modelo, "PuaZ", Vector3.zero, new Vector3(0.22f, 2f, 0.22f), mat, new Vector3(90f, 0f, 0f));

        EntornoVisual.CrearLuzPuntual(go.transform, desde, new Color(1f, 0.2f, 0.15f), 5f, 1.5f);
        return e;
    }

    void Update()
    {
        fase += Time.deltaTime * velocidad / largo;
        Vector3 p = Vector3.Lerp(a, b, Mathf.PingPong(fase, 1f));
        p.y += Mathf.Sin(Time.time * 3f) * 0.1f;
        transform.position = p;
        modelo.Rotate(120f * Time.deltaTime, 200f * Time.deltaTime, 0f);

        JuegoManager juego = JuegoManager.Instancia;
        Jugador jugador = Jugador.Instancia;
        if (juego == null || jugador == null || !juego.Jugando) return;

        Vector3 d = jugador.transform.position - transform.position;
        d.y = 0f;
        if (d.magnitude < 1f) juego.JugadorGolpeado(transform.position);
    }
}
