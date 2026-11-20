using UnityEngine;

/// <summary>Efectos de "game feel" generados por código.</summary>
public static class Efectos
{
    /// <summary>Ráfaga de partículas que se desvanecen.</summary>
    public static void Particulas(Vector3 pos, Color color, int cantidad = 30, float velocidad = 5f, float tam = 0.2f)
    {
        GameObject go = new GameObject("FX_Particulas");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = Materiales.Particula();

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidad * 0.4f, velocidad);
        main.startSize = new ParticleSystem.MinMaxCurve(tam * 0.5f, tam);
        main.startColor = color;
        main.gravityModifier = 0.8f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        ParticleSystem.ShapeModule sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.25f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ps.Play();
        Object.Destroy(go, 2f);
    }

    /// <summary>Destello breve de luz de color.</summary>
    public static void Destello(Vector3 pos, Color color, float intensidad = 4f)
    {
        GameObject go = new GameObject("FX_Destello");
        go.transform.position = pos + Vector3.up * 1.5f;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = 8f;
        l.intensity = intensidad;
        l.shadows = LightShadows.None;
        go.AddComponent<DestelloLuz>().inicial = intensidad;
    }

    /// <summary>Polvo que deja el personaje al caminar.</summary>
    public static ParticleSystem CrearPolvo(Transform padre)
    {
        GameObject go = new GameObject("Polvo");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Materiales.Particula();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startColor = new Color(0.85f, 0.85f, 0.8f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = 3f;

        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 0.25f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ps.Play();
        return ps;
    }
}

/// <summary>Luz que se apaga en unos instantes y se destruye.</summary>
public class DestelloLuz : MonoBehaviour
{
    public float inicial = 4f;
    float t;
    Light luz;

    void Awake() { luz = GetComponent<Light>(); }

    void Update()
    {
        t += Time.deltaTime / 0.45f;
        luz.intensity = inicial * (1f - Mathf.Clamp01(t));
        if (t >= 1f) Destroy(gameObject);
    }
}

/// <summary>Giro y flotación suave (decorado del menú).</summary>
public class Flotante : MonoBehaviour
{
    public Vector3 giro;
    public float amplitud;
    public float frecuencia = 1f;

    Vector3 posBase;
    float fase;

    void Start()
    {
        posBase = transform.localPosition;
        fase = Random.value * 6.28f;
    }

    void Update()
    {
        transform.Rotate(giro * Time.deltaTime, Space.Self);
        if (amplitud > 0f)
            transform.localPosition = posBase + Vector3.up * (Mathf.Sin(Time.time * frecuencia + fase) * amplitud);
    }
}
