using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Menú Memorama > Configurar proyecto completo.
/// Crea texturas y materiales (compatibles con Built-in o URP), las escenas Menu y Juego,
/// las añade a Build Settings y deja listos los ajustes de Android.
/// </summary>
public static class ConfiguradorProyecto
{
    const string RutaRes = "Assets/Resources";

    [MenuItem("Memorama/Configurar proyecto completo")]
    public static void ConfigurarTodo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(RutaRes + "/Texturas");
        Directory.CreateDirectory(RutaRes + "/Materiales");
        Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();

        Texture2D dorso = GuardarPng("dorso", TexDorso());
        Texture2D fieltro = GuardarPng("fieltro", TexFieltro());
        Texture2D madera = GuardarPng("madera", TexMadera());
        Texture2D circulo = GuardarPng("circulo", TexCirculo());

        CrearMateriales(dorso, fieltro, madera, circulo);
        CrearEscenas();
        ConfigurarAndroid();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Memorama 3D",
            "Listo: texturas, materiales, escenas (Menu y Juego) y ajustes de Android creados.\n\n" +
            "Abre la escena Menu y pulsa Play. Revisa que Active Input Handling esté en 'Both'.", "Entendido");
    }

    // ---------- Materiales ----------

    static Shader ShaderLit()
    {
        RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
        if (rp != null && rp.defaultMaterial != null) return rp.defaultMaterial.shader; // URP Lit
        return Shader.Find("Standard");
    }

    static void CrearMateriales(Texture2D dorso, Texture2D fieltro, Texture2D madera, Texture2D circulo)
    {
        Mat("Fieltro", Color.white, 0f, 0.15f, fieltro, new Vector2(6f, 6f), null);
        Mat("Madera", Color.white, 0f, 0.35f, madera, new Vector2(4f, 1f), null);
        Mat("Dorado", new Color(1f, 0.78f, 0.25f), 0.9f, 0.75f, null, null, null);
        Mat("CartaBorde", new Color(0.96f, 0.93f, 0.84f), 0f, 0.4f, null, null, null);
        Mat("CartaDorso", Color.white, 0.1f, 0.5f, dorso, null, null);
        Mat("CartaFrente", new Color(0.97f, 0.97f, 0.95f), 0f, 0.5f, null, null, null);
        Mat("Simbolo", Color.white, 0.3f, 0.8f, null, null, null);
        Mat("Jugador", new Color(0.95f, 0.45f, 0.15f), 0f, 0.4f, null, null, null);
        Mat("JugadorOscuro", new Color(0.08f, 0.08f, 0.10f), 0.2f, 0.6f, null, null, null);
        Mat("Blanco", Color.white, 0f, 0.6f, null, null, null);
        Mat("Enemigo", new Color(0.85f, 0.08f, 0.08f), 0.2f, 0.6f, null, null, new Color(0.8f, 0.05f, 0.05f));
        Mat("Exterior", new Color(0.03f, 0.03f, 0.05f), 0f, 0.1f, null, null, null);
        Mat("Lampara", new Color(1f, 0.9f, 0.6f), 0f, 0.8f, null, null, new Color(1.5f, 1.3f, 0.8f));

        // Material de partículas (sin iluminación, con textura circular)
        string ruta = RutaRes + "/Materiales/Particula.mat";
        Material p = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (p == null)
        {
            p = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(p, ruta);
        }
        p.mainTexture = circulo;
        EditorUtility.SetDirty(p);
    }

    static Material Mat(string nombre, Color color, float metal, float suavidad,
        Texture2D tex, Vector2? tiling, Color? emision)
    {
        string ruta = RutaRes + "/Materiales/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = (m == null);
        if (nuevo) m = new Material(ShaderLit());

        m.color = color;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad); // Standard
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad); // URP
        if (tex != null)
        {
            m.mainTexture = tex;
            if (tiling.HasValue) m.mainTextureScale = tiling.Value;
        }
        if (emision.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (nuevo) AssetDatabase.CreateAsset(m, ruta);
        else EditorUtility.SetDirty(m);
        return m;
    }

    // ---------- Texturas procedurales ----------

    static Texture2D GuardarPng(string nombre, Texture2D tex)
    {
        string ruta = RutaRes + "/Texturas/" + nombre + ".png";
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static Texture2D TexDorso()
    {
        const int n = 256;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color azul = new Color(0.09f, 0.13f, 0.42f);
        Color azul2 = new Color(0.14f, 0.20f, 0.55f);
        Color oro = new Color(0.95f, 0.75f, 0.25f);

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                Color c = azul;
                int u = (x + y) % 32;
                int v = ((x - y) % 32 + 32) % 32;
                if (u < 2 || v < 2) c = azul2; // retícula sutil

                float dx = x - n / 2f, dy = y - n / 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 34f && d < 42f) c = oro;
                else if (d <= 34f) c = Color.Lerp(azul, oro, 0.25f * (1f - d / 34f));

                int borde = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                if (borde >= 8 && borde < 13) c = oro;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    static Texture2D TexFieltro()
    {
        const int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float r = Random.value * 0.06f - 0.03f;
                t.SetPixel(x, y, new Color(0.08f + r, 0.38f + r, 0.22f + r));
            }
        }
        t.Apply();
        return t;
    }

    static Texture2D TexMadera()
    {
        const int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float veta = 0.5f + 0.5f * Mathf.Sin(y * 0.35f + Mathf.Sin(x * 0.05f) * 2f);
                float k = 0.75f + 0.25f * veta + (Random.value - 0.5f) * 0.05f;
                t.SetPixel(x, y, new Color(0.40f * k, 0.23f * k, 0.11f * k));
            }
        }
        t.Apply();
        return t;
    }

    static Texture2D TexCirculo()
    {
        const int n = 64;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.SmoothStep(1f, 0f, (d - r * 0.55f) / (r * 0.45f));
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
        }
        t.Apply();
        return t;
    }

    // ---------- Escenas ----------

    static void CrearEscenas()
    {
        CrearEscena("Menu", typeof(MenuPrincipal));
        CrearEscena("Juego", typeof(JuegoManager));

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Juego.unity", true)
        };
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
    }

    static void CrearEscena(string nombre, System.Type tipoControlador)
    {
        UnityEngine.SceneManagement.Scene escena =
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject go = new GameObject("_" + nombre);
        go.AddComponent(tipoControlador);
        EditorSceneManager.SaveScene(escena, "Assets/Scenes/" + nombre + ".unity");
    }

    // ---------- Android ----------

    static void ConfigurarAndroid()
    {
        try
        {
            PlayerSettings.companyName = "MiEstudio"; // cámbialo por el tuyo
            PlayerSettings.productName = "Memorama 3D";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.miestudio.memorama3d");

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudieron aplicar todos los ajustes de Android: " + e.Message);
        }
    }
}
