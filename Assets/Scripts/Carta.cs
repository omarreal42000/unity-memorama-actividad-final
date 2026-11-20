using System.Collections;
using UnityEngine;

/// <summary>
/// Carta tridimensional. Boca abajo muestra el dorso; al voltearla aparece un símbolo 3D
/// (forma + color) que identifica su pareja.
/// </summary>
public class Carta : MonoBehaviour
{
    public static readonly Vector2 Tamano = new Vector2(1.3f, 1.8f); // ancho (X) y largo (Z)

    static readonly Color[] Colores =
    {
        new Color(0.90f, 0.20f, 0.20f), new Color(0.20f, 0.45f, 0.95f),
        new Color(1.00f, 0.85f, 0.15f), new Color(0.25f, 0.80f, 0.35f),
        new Color(1.00f, 0.50f, 0.10f), new Color(0.20f, 0.85f, 0.90f),
        new Color(0.85f, 0.30f, 0.85f), new Color(0.95f, 0.95f, 0.95f)
    };

    static readonly PrimitiveType[] Formas =
    {
        PrimitiveType.Sphere, PrimitiveType.Cube, PrimitiveType.Cylinder, PrimitiveType.Capsule
    };

    public static Color ColorDe(int id) { return Colores[id % Colores.Length]; }

    public int Id { get; private set; }
    public bool Volteada { get; private set; }
    public bool Emparejada { get; private set; }
    public bool JugadorDentro;

    Transform modelo, simbolo;
    Material matCara, matSimbolo;
    readonly Vector3 escalaSimbolo = Vector3.one * 0.6f;

    public static Carta Crear(Transform padre, Vector3 pos, int id)
    {
        GameObject go = new GameObject("Carta_" + id);
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        Carta c = go.AddComponent<Carta>();
        c.Construir(id);
        return c;
    }

    void Construir(int id)
    {
        Id = id;
        Color color = ColorDe(id);

        Material borde = Materiales.Cargar("CartaBorde", new Color(0.95f, 0.92f, 0.82f));
        Material dorso = Materiales.Cargar("CartaDorso", new Color(0.12f, 0.16f, 0.50f));
        Material frente = Materiales.Cargar("CartaFrente", Color.white);
        Material simb = Materiales.Cargar("Simbolo", Color.white);

        modelo = new GameObject("Modelo").transform;
        modelo.SetParent(transform, false);

        Geo.Crear(PrimitiveType.Cube, modelo, "Cuerpo", Vector3.zero,
            new Vector3(Tamano.x, 0.06f, Tamano.y), borde);
        Geo.Crear(PrimitiveType.Quad, modelo, "Dorso", new Vector3(0f, 0.032f, 0f),
            new Vector3(Tamano.x * 0.88f, Tamano.y * 0.88f, 1f), dorso, new Vector3(90f, 0f, 0f));
        GameObject cara = Geo.Crear(PrimitiveType.Quad, modelo, "Frente", new Vector3(0f, -0.032f, 0f),
            new Vector3(Tamano.x * 0.88f, Tamano.y * 0.88f, 1f), null, new Vector3(-90f, 0f, 0f));

        matCara = new Material(frente);
        matCara.color = Color.Lerp(Color.white, color, 0.35f);
        cara.GetComponent<Renderer>().sharedMaterial = matCara;

        GameObject s = Geo.Crear(Formas[id % Formas.Length], transform, "Simbolo",
            new Vector3(0f, 0.85f, 0f), Vector3.zero, null);
        matSimbolo = new Material(simb);
        matSimbolo.color = color;
        matSimbolo.EnableKeyword("_EMISSION");
        matSimbolo.SetColor("_EmissionColor", color * 0.6f);
        s.GetComponent<Renderer>().sharedMaterial = matSimbolo;
        simbolo = s.transform;
    }

    void Update()
    {
        // El símbolo gira y flota mientras la carta está descubierta
        if (Volteada && !Emparejada && simbolo != null)
        {
            simbolo.Rotate(0f, 120f * Time.deltaTime, 0f);
            Vector3 p = simbolo.localPosition;
            p.y = 0.85f + Mathf.Sin(Time.time * 4f + Id) * 0.08f;
            simbolo.localPosition = p;
        }
    }

    /// <summary>Anima el giro de la carta. arriba = true la deja boca arriba.</summary>
    public IEnumerator Voltear(bool arriba, float duracion = 0.35f)
    {
        Volteada = arriba;
        float desde = arriba ? 0f : 180f;
        float hasta = arriba ? 180f : 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            float k = Mathf.Clamp01(t);
            float e = Mathf.SmoothStep(0f, 1f, k);
            modelo.localRotation = Quaternion.Euler(Mathf.Lerp(desde, hasta, e), 0f, 0f);
            modelo.localPosition = new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.8f, 0f);

            float s = arriba ? EaseOutBack(Mathf.Clamp01((k - 0.5f) * 2f)) : 1f - Mathf.Clamp01(k * 2f);
            simbolo.localScale = escalaSimbolo * Mathf.Max(0f, s);
            yield return null;
        }
        modelo.localRotation = Quaternion.Euler(hasta, 0f, 0f);
        modelo.localPosition = Vector3.zero;
        simbolo.localScale = arriba ? escalaSimbolo : Vector3.zero;
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    /// <summary>La carta sube girando y se desvanece al encontrar su pareja.</summary>
    public void Emparejar()
    {
        Emparejada = true;
        StartCoroutine(AnimEmparejar());
    }

    IEnumerator AnimEmparejar()
    {
        yield return new WaitForSeconds(0.25f);
        Vector3 inicio = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.7f;
            float e = Mathf.Clamp01(t);
            e *= e;
            transform.position = inicio + Vector3.up * (e * 3f);
            transform.Rotate(0f, 540f * Time.deltaTime, 0f);
            transform.localScale = Vector3.one * Mathf.Max(0f, 1f - e);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (matCara != null) Destroy(matCara);
        if (matSimbolo != null) Destroy(matSimbolo);
    }
}
