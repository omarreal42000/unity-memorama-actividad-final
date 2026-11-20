using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Crea elementos de UI Canvas por código con un estilo visual coherente.</summary>
public static class UIFactory
{
    public static readonly Color ColorPrimario = new Color(0.96f, 0.74f, 0.20f);
    public static readonly Color ColorSecundario = new Color(0.20f, 0.27f, 0.50f);
    public static readonly Color ColorFondo = new Color(0.07f, 0.09f, 0.18f, 0.96f);
    public static readonly Color ColorTextoOscuro = new Color(0.10f, 0.10f, 0.16f);

    static Font _fuente;
    static Sprite _redondeado, _circulo, _estrella;

    // ---------- Recursos gráficos generados por código ----------

    public static Font Fuente
    {
        get
        {
            if (_fuente == null)
            {
                _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_fuente == null) _fuente = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            return _fuente;
        }
    }

    public static Sprite Redondeado
    {
        get
        {
            if (_redondeado == null)
            {
                const int t = 64, r = 20;
                Texture2D tex = new Texture2D(t, t, TextureFormat.RGBA32, false);
                Color[] px = new Color[t * t];
                for (int y = 0; y < t; y++)
                {
                    for (int x = 0; x < t; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, r, t - r);
                        float cy = Mathf.Clamp(y + 0.5f, r, t - r);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        px[y * t + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                _redondeado = Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            }
            return _redondeado;
        }
    }

    public static Sprite Circulo
    {
        get
        {
            if (_circulo == null)
            {
                const int t = 128;
                Texture2D tex = new Texture2D(t, t, TextureFormat.RGBA32, false);
                Color[] px = new Color[t * t];
                for (int y = 0; y < t; y++)
                {
                    for (int x = 0; x < t; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(t / 2f, t / 2f));
                        px[y * t + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(t / 2f - d));
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                _circulo = Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), 100f);
            }
            return _circulo;
        }
    }

    public static Sprite Estrella
    {
        get
        {
            if (_estrella == null)
            {
                const int t = 128;
                Vector2[] v = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                    float r = (i % 2 == 0) ? 60f : 25f;
                    v[i] = new Vector2(64f + r * Mathf.Cos(ang), 62f + r * Mathf.Sin(ang));
                }
                Texture2D tex = new Texture2D(t, t, TextureFormat.RGBA32, false);
                Color[] px = new Color[t * t];
                for (int y = 0; y < t; y++)
                {
                    for (int x = 0; x < t; x++)
                    {
                        int dentro = 0;
                        for (int sy = 0; sy < 2; sy++)
                            for (int sx = 0; sx < 2; sx++)
                                if (PuntoEnPoligono(new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f), v)) dentro++;
                        px[y * t + x] = new Color(1f, 1f, 1f, dentro / 4f);
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                _estrella = Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), 100f);
            }
            return _estrella;
        }
    }

    static bool PuntoEnPoligono(Vector2 p, Vector2[] v)
    {
        bool dentro = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
        {
            if (((v[i].y > p.y) != (v[j].y > p.y)) &&
                (p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x))
                dentro = !dentro;
        }
        return dentro;
    }

    // ---------- Canvas y EventSystem ----------

    public static Canvas CrearCanvas(string nombre, int orden = 0)
    {
        GameObject go = new GameObject(nombre, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = orden;
        CanvasScaler s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920f, 1080f);
        s.matchWidthOrHeight = 0.5f;
        return c;
    }

    public static void AsegurarEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_LEGACY_INPUT_MANAGER
        go.AddComponent<StandaloneInputModule>();
#else
        // Proyecto con el nuevo Input System: se usa su módulo de UI (sin dependencia directa en compilación).
        System.Type tipo = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (tipo != null)
        {
            Component modulo = go.AddComponent(tipo);
            System.Reflection.MethodInfo m = tipo.GetMethod("AssignDefaultActions");
            if (m != null) m.Invoke(modulo, null);
        }
        else
        {
            go.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    /// <summary>True cuando se pulsa Escape (en Android equivale al botón Atrás).</summary>
    public static bool EscapePresionado()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    // ---------- Posicionamiento ----------

    public static RectTransform Colocar(RectTransform rt, Vector2 ancla, Vector2 posicion, Vector2 tamano, Vector2? pivote = null)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote.HasValue ? pivote.Value : ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
        return rt;
    }

    public static void Estirar(RectTransform rt, float margen = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    // ---------- Elementos ----------

    public static RectTransform CrearPanel(Transform padre, string nombre, Color color, bool redondeado = true)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        if (redondeado)
        {
            img.sprite = Redondeado;
            img.type = Image.Type.Sliced;
        }
        return go.GetComponent<RectTransform>();
    }

    public static Image CrearImagen(Transform padre, string nombre, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Text CrearTexto(Transform padre, string contenido, int tamano,
        TextAnchor alineacion = TextAnchor.MiddleCenter, Color? color = null)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        Text t = go.GetComponent<Text>();
        t.font = Fuente;
        t.text = contenido;
        t.fontSize = tamano;
        t.alignment = alineacion;
        t.color = color.HasValue ? color.Value : Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Shadow sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.55f);
        sombra.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    public static Button CrearBoton(Transform padre, string texto, Vector2 tamano, UnityAction alClic,
        Color? color = null, int fuente = 44, Color? colorTexto = null)
    {
        RectTransform rt = CrearPanel(padre, "Boton_" + texto, color.HasValue ? color.Value : ColorPrimario);
        rt.sizeDelta = tamano;
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);
        b.colors = cb;

        Text t = CrearTexto(rt, texto, fuente, TextAnchor.MiddleCenter,
            colorTexto.HasValue ? colorTexto.Value : (color.HasValue ? Color.white : ColorTextoOscuro));
        Estirar(t.rectTransform, 8f);
        if (!color.HasValue) t.GetComponent<Shadow>().enabled = false;

        b.onClick.AddListener(() => AudioManager.Instancia.Play(Sfx.Clic));
        if (alClic != null) b.onClick.AddListener(alClic);
        rt.gameObject.AddComponent<EfectoBoton>();
        return b;
    }

    /// <summary>Ventana modal con título y apilado vertical automático de sus hijos.</summary>
    public static RectTransform CrearVentana(Transform padre, string nombre, string titulo, Vector2 tamano,
        out GameObject raiz, out Text txtTitulo)
    {
        RectTransform overlay = CrearPanel(padre, nombre, new Color(0f, 0f, 0f, 0.65f), false);
        Estirar(overlay);
        overlay.GetComponent<Image>().raycastTarget = true; // bloquea toques al fondo
        raiz = overlay.gameObject;

        RectTransform ventana = CrearPanel(overlay, "Ventana", ColorFondo);
        Colocar(ventana, new Vector2(0.5f, 0.5f), Vector2.zero, tamano);
        VerticalLayoutGroup vl = ventana.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.spacing = 22f;
        vl.padding = new RectOffset(40, 40, 40, 40);
        vl.childControlWidth = false;
        vl.childControlHeight = false;
        vl.childForceExpandWidth = false;
        vl.childForceExpandHeight = false;

        txtTitulo = CrearTexto(ventana, titulo, 64, TextAnchor.MiddleCenter, ColorPrimario);
        txtTitulo.rectTransform.sizeDelta = new Vector2(tamano.x - 80f, 90f);
        ventana.gameObject.AddComponent<PanelAnimado>();
        return ventana;
    }

    public static RectTransform CrearEstrellas(Transform padre, int llenas, float tam)
    {
        GameObject go = new GameObject("Estrellas", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        float sep = tam * 0.15f;
        rt.sizeDelta = new Vector2(tam * 3f + sep * 2f, tam);
        HorizontalLayoutGroup h = go.GetComponent<HorizontalLayoutGroup>();
        h.spacing = sep;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = false;
        h.childControlHeight = false;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        for (int i = 0; i < 3; i++)
        {
            Image img = CrearImagen(rt, "Estrella" + i, Estrella, Color.gray);
            img.rectTransform.sizeDelta = new Vector2(tam, tam);
        }
        PintarEstrellas(rt, llenas);
        return rt;
    }

    public static void PintarEstrellas(RectTransform contenedor, int llenas)
    {
        for (int i = 0; i < contenedor.childCount; i++)
        {
            contenedor.GetChild(i).GetComponent<Image>().color =
                i < llenas ? ColorPrimario : new Color(0.35f, 0.35f, 0.42f, 0.85f);
        }
    }
}

/// <summary>Los botones se encogen un poco al pulsarlos (sensación de respuesta).</summary>
public class EfectoBoton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    bool presionado;

    public void OnPointerDown(PointerEventData e) { presionado = true; }
    public void OnPointerUp(PointerEventData e) { presionado = false; }
    public void OnPointerExit(PointerEventData e) { presionado = false; }

    void Update()
    {
        Vector3 objetivo = presionado ? Vector3.one * 0.92f : Vector3.one;
        transform.localScale = Vector3.Lerp(transform.localScale, objetivo, Mathf.Min(1f, 20f * Time.unscaledDeltaTime));
    }

    void OnDisable()
    {
        presionado = false;
        transform.localScale = Vector3.one;
    }
}

/// <summary>Animación de aparición (fundido + escala) para paneles y ventanas.</summary>
public class PanelAnimado : MonoBehaviour
{
    CanvasGroup grupo;

    void OnEnable()
    {
        if (grupo == null)
        {
            grupo = GetComponent<CanvasGroup>();
            if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        }
        StopAllCoroutines();
        StartCoroutine(Animar());
    }

    IEnumerator Animar()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.25f;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            grupo.alpha = e;
            transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, e);
            yield return null;
        }
        grupo.alpha = 1f;
        transform.localScale = Vector3.one;
    }
}

/// <summary>Fundido a negro al entrar y salir de una escena.</summary>
public class Transicion : MonoBehaviour
{
    Image velo;

    public static Transicion Crear(Transform canvas)
    {
        RectTransform rt = UIFactory.CrearPanel(canvas, "VeloTransicion", Color.black, false);
        UIFactory.Estirar(rt);
        rt.SetAsLastSibling();
        Transicion t = rt.gameObject.AddComponent<Transicion>();
        t.velo = rt.GetComponent<Image>();
        t.StartCoroutine(t.Fundido(1f, 0f, 0.5f));
        return t;
    }

    public void IrA(string escena)
    {
        transform.SetAsLastSibling();
        StartCoroutine(Ir(escena));
    }

    IEnumerator Ir(string escena)
    {
        yield return Fundido(0f, 1f, 0.35f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(escena);
    }

    IEnumerator Fundido(float desde, float hasta, float dur)
    {
        velo.raycastTarget = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            velo.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, Mathf.Clamp01(t)));
            yield return null;
        }
        velo.color = new Color(0f, 0f, 0f, hasta);
        velo.raycastTarget = hasta > 0.5f;
    }
}
