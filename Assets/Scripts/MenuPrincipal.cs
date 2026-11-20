using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escena "Menu". Construye por código el fondo 3D (anillo de cartas flotantes) y toda la
/// navegación con UI Canvas: Jugar, Niveles, Opciones, Cómo jugar y Salir.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    RectTransform raiz;
    GameObject panelNiveles, panelOpciones, panelAyuda;
    Text txtVolumen, txtSonido, txtBorrar, txtTotal;
    bool confirmarBorrado;
    Transicion transicion;

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Time.timeScale = 1f;

        EntornoVisual.Configurar();
        CrearDecorado();
        UIFactory.AsegurarEventSystem();
        ConstruirUI();
        AudioManager.Instancia.AplicarVolumen();
    }

    void Update()
    {
        if (UIFactory.EscapePresionado())
        {
            if (HayVentanaAbierta()) CerrarVentanas();
            else Salir();
        }
    }

    // ---------- Decorado 3D ----------

    void CrearDecorado()
    {
        Camera cam = EntornoVisual.AsegurarCamara();
        cam.transform.position = new Vector3(0f, 2.5f, -13f);
        cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);

        Transform anillo = new GameObject("AnilloDeCartas").transform;
        anillo.gameObject.AddComponent<Flotante>().giro = new Vector3(0f, 10f, 0f);

        const int n = 12;
        const float radio = 7f;
        for (int i = 0; i < n; i++)
        {
            float ang = i * Mathf.PI * 2f / n;
            Vector3 radial = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            Vector3 pos = radial * radio + Vector3.up * (Mathf.Sin(i * 1.7f) * 0.8f);

            Carta c = Carta.Crear(anillo, pos, i % 8);
            // De pie, con el dorso mirando hacia afuera del anillo
            c.transform.localRotation = Quaternion.LookRotation(-radial) * Quaternion.Euler(-90f, 0f, 0f);
            Flotante f = c.gameObject.AddComponent<Flotante>();
            f.amplitud = 0.25f;
            f.frecuencia = 1.2f;
        }

        EntornoVisual.CrearFoco(null, new Vector3(0f, 10f, -4f), new Color(1f, 0.92f, 0.75f));
        EntornoVisual.CrearLuzPuntual(null, new Vector3(-7f, 3f, -5f), new Color(0.3f, 0.5f, 1f), 14f, 2f);
        EntornoVisual.CrearLuzPuntual(null, new Vector3(7f, 3f, -5f), new Color(1f, 0.3f, 0.8f), 14f, 2f);
    }

    // ---------- Interfaz ----------

    void ConstruirUI()
    {
        Canvas canvas = UIFactory.CrearCanvas("CanvasMenu");
        raiz = canvas.GetComponent<RectTransform>();
        Transform r = canvas.transform;

        Text titulo = UIFactory.CrearTexto(r, "MEMORAMA 3D", 150, TextAnchor.MiddleCenter, UIFactory.ColorPrimario);
        UIFactory.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1600f, 200f));

        Text sub = UIFactory.CrearTexto(r, "El paseo de las cartas", 54, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 1f));
        UIFactory.Colocar(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(1200f, 80f));

        GameObject contGo = new GameObject("Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contGo.transform.SetParent(r, false);
        RectTransform cont = contGo.GetComponent<RectTransform>();
        UIFactory.Colocar(cont, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(640f, 640f));
        VerticalLayoutGroup vl = contGo.GetComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.spacing = 18f;
        vl.childControlWidth = false;
        vl.childControlHeight = false;
        vl.childForceExpandWidth = false;
        vl.childForceExpandHeight = false;

        Vector2 tb = new Vector2(560f, 110f);
        UIFactory.CrearBoton(cont, "Jugar", tb, Jugar);
        UIFactory.CrearBoton(cont, "Niveles", tb, () => Abrir(panelNiveles), UIFactory.ColorSecundario, 44, Color.white);
        UIFactory.CrearBoton(cont, "Opciones", tb, () => Abrir(panelOpciones), UIFactory.ColorSecundario, 44, Color.white);
        UIFactory.CrearBoton(cont, "Cómo jugar", tb, () => Abrir(panelAyuda), UIFactory.ColorSecundario, 44, Color.white);
        UIFactory.CrearBoton(cont, "Salir", tb, Salir, new Color(0.55f, 0.18f, 0.2f), 44, Color.white);

        txtTotal = UIFactory.CrearTexto(r, "", 38, TextAnchor.LowerLeft, new Color(0.85f, 0.9f, 1f));
        UIFactory.Colocar(txtTotal.rectTransform, new Vector2(0f, 0f), new Vector2(50f, 30f), new Vector2(900f, 60f));
        ActualizarTotal();

        ConstruirNiveles();
        ConstruirOpciones();
        ConstruirAyuda();

        transicion = Transicion.Crear(r); // siempre al final: queda por encima de todo
    }

    void ActualizarTotal()
    {
        txtTotal.text = "Puntos acumulados: " + Guardado.Datos.puntosTotales;
    }

    void ConstruirNiveles()
    {
        if (panelNiveles != null) Destroy(panelNiveles);

        RectTransform vent = UIFactory.CrearVentana(raiz, "VentanaNiveles", "ELIGE UN NIVEL",
            new Vector2(1000f, 820f), out panelNiveles, out _);
        DatosJugador d = Guardado.Datos;

        for (int i = 0; i < ConfigNiveles.Total; i++)
        {
            int idx = i;
            bool abierto = i < d.nivelDesbloqueado;
            Button b = UIFactory.CrearBoton(vent, ConfigNiveles.Niveles[i].nombre, new Vector2(900f, 120f),
                () => JugarNivel(idx),
                abierto ? UIFactory.ColorSecundario : new Color(0.25f, 0.25f, 0.30f), 34, Color.white);

            Text t = b.GetComponentInChildren<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.offsetMin = new Vector2(30f, 8f);
            t.text = ConfigNiveles.Niveles[i].nombre + "\n" +
                     (abierto ? "Récord: " + d.mejoresPuntos[i] : "Bloqueado");
            b.interactable = abierto;

            if (abierto)
            {
                RectTransform est = UIFactory.CrearEstrellas(b.transform, d.estrellas[i], 60f);
                UIFactory.Colocar(est, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), est.sizeDelta);
            }
        }

        UIFactory.CrearBoton(vent, "Volver", new Vector2(560f, 100f), CerrarVentanas);
        panelNiveles.SetActive(false);
    }

    void ConstruirOpciones()
    {
        RectTransform vent = UIFactory.CrearVentana(raiz, "VentanaOpciones", "OPCIONES",
            new Vector2(860f, 760f), out panelOpciones, out _);

        GameObject fila = new GameObject("FilaVolumen", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        fila.transform.SetParent(vent, false);
        fila.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 100f);
        HorizontalLayoutGroup h = fila.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 20f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = false;
        h.childControlHeight = false;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        UIFactory.CrearBoton(fila.transform, "-", new Vector2(110f, 100f), () => CambiarVolumen(-0.1f),
            UIFactory.ColorSecundario, 60, Color.white);
        txtVolumen = UIFactory.CrearTexto(fila.transform, "", 42);
        txtVolumen.rectTransform.sizeDelta = new Vector2(400f, 100f);
        UIFactory.CrearBoton(fila.transform, "+", new Vector2(110f, 100f), () => CambiarVolumen(0.1f),
            UIFactory.ColorSecundario, 60, Color.white);

        Vector2 tb = new Vector2(560f, 100f);
        Button bs = UIFactory.CrearBoton(vent, "Sonido", tb, AlternarSonido, UIFactory.ColorSecundario, 42, Color.white);
        txtSonido = bs.GetComponentInChildren<Text>();

        Button bb = UIFactory.CrearBoton(vent, "Borrar progreso", tb, BorrarProgreso, new Color(0.55f, 0.18f, 0.2f), 42, Color.white);
        txtBorrar = bb.GetComponentInChildren<Text>();

        UIFactory.CrearBoton(vent, "Volver", tb, CerrarVentanas);
        ActualizarOpciones();
        panelOpciones.SetActive(false);
    }

    void ConstruirAyuda()
    {
        RectTransform vent = UIFactory.CrearVentana(raiz, "VentanaAyuda", "CÓMO JUGAR",
            new Vector2(1100f, 880f), out panelAyuda, out _);

        Text t = UIFactory.CrearTexto(vent,
            "• Mueve al personaje con el joystick de la pantalla (o con WASD / flechas en la computadora).\n" +
            "• Pisa una carta para voltearla y busca su pareja.\n" +
            "• Cada acierto suma puntos; los aciertos seguidos activan el combo.\n" +
            "• Los guardianes rojos te quitan 5 segundos: ¡evítalos!\n" +
            "• Encuentra todas las parejas antes de que se acabe el tiempo.",
            38, TextAnchor.UpperLeft);
        t.rectTransform.sizeDelta = new Vector2(1000f, 520f);

        UIFactory.CrearBoton(vent, "Entendido", new Vector2(560f, 100f), CerrarVentanas);
        panelAyuda.SetActive(false);
    }

    // ---------- Acciones ----------

    void Jugar()
    {
        // Continúa en el nivel más alto desbloqueado
        ConfigNiveles.NivelActual = Mathf.Clamp(Guardado.Datos.nivelDesbloqueado - 1, 0, ConfigNiveles.Total - 1);
        transicion.IrA(ConfigNiveles.EscenaJuego);
    }

    void JugarNivel(int idx)
    {
        ConfigNiveles.NivelActual = idx;
        transicion.IrA(ConfigNiveles.EscenaJuego);
    }

    void Abrir(GameObject panel)
    {
        CerrarVentanas();
        panel.SetActive(true);
    }

    void CerrarVentanas()
    {
        if (panelNiveles != null) panelNiveles.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelAyuda != null) panelAyuda.SetActive(false);
        confirmarBorrado = false;
        if (txtBorrar != null) txtBorrar.text = "Borrar progreso";
    }

    bool HayVentanaAbierta()
    {
        return (panelNiveles != null && panelNiveles.activeSelf) ||
               (panelOpciones != null && panelOpciones.activeSelf) ||
               (panelAyuda != null && panelAyuda.activeSelf);
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CambiarVolumen(float delta)
    {
        DatosJugador d = Guardado.Datos;
        d.volumen = Mathf.Clamp01(Mathf.Round((d.volumen + delta) * 10f) / 10f);
        Guardado.Guardar();
        AudioManager.Instancia.AplicarVolumen();
        ActualizarOpciones();
    }

    void AlternarSonido()
    {
        DatosJugador d = Guardado.Datos;
        d.sonidoActivo = !d.sonidoActivo;
        Guardado.Guardar();
        AudioManager.Instancia.AplicarVolumen();
        ActualizarOpciones();
    }

    void BorrarProgreso()
    {
        if (!confirmarBorrado)
        {
            confirmarBorrado = true;
            txtBorrar.text = "¿Seguro? Pulsa otra vez";
            return;
        }
        confirmarBorrado = false;
        txtBorrar.text = "Borrar progreso";
        Guardado.Borrar();
        AudioManager.Instancia.AplicarVolumen();
        ActualizarOpciones();
        ActualizarTotal();
        ConstruirNiveles();
    }

    void ActualizarOpciones()
    {
        DatosJugador d = Guardado.Datos;
        txtVolumen.text = "Volumen: " + Mathf.RoundToInt(d.volumen * 100f) + "%";
        txtSonido.text = "Sonido: " + (d.sonidoActivo ? "activado" : "desactivado");
    }
}
