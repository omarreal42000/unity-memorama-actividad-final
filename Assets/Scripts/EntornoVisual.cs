using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Construcción rápida de objetos 3D con primitivas.</summary>
public static class Geo
{
    public static GameObject Crear(PrimitiveType tipo, Transform padre, string nombre, Vector3 posLocal,
        Vector3 escala, Material mat, Vector3? rotEuler = null, bool conCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        go.transform.localScale = escala;
        if (rotEuler.HasValue) go.transform.localRotation = Quaternion.Euler(rotEuler.Value);
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!conCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
        }
        return go;
    }
}

/// <summary>
/// Carga los materiales creados por el menú Memorama > Configurar proyecto.
/// Si no existen, crea uno de respaldo para que el juego nunca se vea rosa.
/// </summary>
public static class Materiales
{
    static Material _particula;

    public static Material Cargar(string nombre, Color colorRespaldo)
    {
        Material m = Resources.Load<Material>("Materiales/" + nombre);
        if (m != null) return m;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        m = new Material(sh);
        m.color = colorRespaldo;
        return m;
    }

    public static Material Particula()
    {
        if (_particula != null) return _particula;
        Material m = Resources.Load<Material>("Materiales/Particula");
        if (m == null)
        {
            m = new Material(Shader.Find("Sprites/Default"));
            m.mainTexture = UIFactory.Circulo.texture;
        }
        _particula = m;
        return m;
    }
}

/// <summary>Cámara, luces, luz ambiental y niebla compartidas por menú y juego.</summary>
public static class EntornoVisual
{
    static readonly Color Fondo = new Color(0.04f, 0.05f, 0.10f);

    public static Camera AsegurarCamara()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Fondo;
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 150f;
        return cam;
    }

    public static void Configurar()
    {
        AsegurarCamara();

        // Luz ambiental en tres tonos (cielo, horizonte, suelo)
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.35f, 0.40f, 0.60f);
        RenderSettings.ambientEquatorColor = new Color(0.25f, 0.25f, 0.32f);
        RenderSettings.ambientGroundColor = new Color(0.10f, 0.08f, 0.08f);

        // Niebla suave para dar profundidad y ocultar el borde del escenario
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Fondo;
        RenderSettings.fogDensity = 0.012f;

        // Luz principal con sombras suaves
        Light sol = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) { sol = l; break; }
        }
        if (sol == null)
        {
            GameObject go = new GameObject("Luz Principal");
            sol = go.AddComponent<Light>();
            sol.type = LightType.Directional;
        }
        sol.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        sol.color = new Color(1f, 0.95f, 0.85f);
        sol.intensity = 1f;
        sol.shadows = LightShadows.Soft;
        sol.shadowStrength = 0.85f;
    }

    public static Light CrearLuzPuntual(Transform padre, Vector3 pos, Color color, float rango, float intensidad)
    {
        GameObject go = new GameObject("LuzPuntual");
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = rango;
        l.intensity = intensidad;
        l.shadows = LightShadows.None;
        return l;
    }

    public static Light CrearFoco(Transform padre, Vector3 pos, Color color)
    {
        GameObject go = new GameObject("FocoCentral");
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Light l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = color;
        l.range = 35f;
        l.spotAngle = 75f;
        l.intensity = 1.8f;
        l.shadows = LightShadows.None;
        return l;
    }
}
