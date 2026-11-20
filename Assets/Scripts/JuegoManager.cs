using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Núcleo del juego. El personaje camina por una mesa de casino en 3D: al pisar una carta se voltea.
/// Hay que descubrir todas las parejas antes de que se acabe el tiempo, evitando a los guardianes.
/// </summary>
public class JuegoManager : MonoBehaviour
{
    public static JuegoManager Instancia { get; private set; }

    const float SepX = 2.1f;
    const float SepZ = 2.7f;
    const int PenalizacionError = 15;
    const float PenalizacionGolpe = 5f;

    enum Estado { Preparando, Jugando, Pausa, Fin }

    Estado estado = Estado.Preparando;
    ConfigNivel cfg;
    int nivelIdx;
    float mx, mz; // mitad del ancho y del largo de la mesa

    readonly List<Carta> cartas = new List<Carta>();
    readonly List<Carta> reveladas = new List<Carta>();
    bool bloqueo;

    int puntos, combo, paresEncontrados, paresTotales, errores;
    float puntosMostrados, pulsoPuntos, tiempoRestante;

    Jugador jugador;
    CamaraSeguimiento camara;
    Camera cam;
    Transform escenario;

    // Interfaz
    RectTransform canvasRect;
    Text txtPuntos, txtPares, txtNivel, txtTiempo, txtCombo, txtCentro, txtFinTitulo, txtFinDetalle;
    GameObject panelPausa, panelFin;
    Button btnSiguiente;
    RectTransform estrellasFin;
    Transicion transicion;

    public bool Jugando { get { return estado == Estado.Jugando; } }

    void Awake() { Instancia = this; }

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Time.timeScale = 1f;

        nivelIdx = Mathf.Clamp(ConfigNiveles.NivelActual, 0, ConfigNiveles.Total - 1);
        cfg = ConfigNiveles.Niveles[nivelIdx];
        tiempoRestante = cfg.tiempo;
        mx = cfg.columnas * SepX * 0.5f + 3.5f;
        mz = cfg.filas * SepZ * 0.5f + 3.5f;

        EntornoVisual.Configurar();
        cam = EntornoVisual.AsegurarCamara();

        ConstruirEscenario();
        ConstruirCartas();
        ConstruirJugador();
        ConstruirEnemigos();

        camara = cam.gameObject.AddComponent<CamaraSeguimiento>();
        camara.Iniciar(cam, jugador.transform);

        UIFactory.AsegurarEventSystem();
        ConstruirUI();
        StartCoroutine(Preparacion());
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        Time.timeScale = 1f;
    }

    // =====================================================================
    //  CONSTRUCCIÓN DEL MUNDO 3D
    // =====================================================================

    void ConstruirEscenario()
    {
        escenario = new GameObject("Escenario").transform;

        Material fieltro = Materiales.Cargar("Fieltro", new Color(0.08f, 0.38f, 0.22f));
        Material madera = Materiales.Cargar("Madera", new Color(0.35f, 0.20f, 0.10f));
        Material dorado = Materiales.Cargar("Dorado", new Color(1f, 0.78f, 0.25f));
        Material exterior = Materiales.Cargar("Exterior", new Color(0.03f, 0.03f, 0.05f));
        Material lampara = Materiales.Cargar("Lampara", new Color(1f, 0.9f, 0.6f));

        Geo.Crear(PrimitiveType.Cube, escenario, "Mesa", new Vector3(0f, -0.2f, 0f),
            new Vector3(2f * mx, 0.4f, 2f * mz), fieltro, null, true);
        Geo.Crear(PrimitiveType.Cube, escenario, "Exterior", new Vector3(0f, -0.6f, 0f),
            new Vector3(150f, 0.2f, 150f), exterior);

        const float h = 1.2f, g = 0.6f;
        Geo.Crear(PrimitiveType.Cube, escenario, "Pared_N", new Vector3(0f, h / 2f, mz + g / 2f),
            new Vector3(2f * mx + 2f * g, h, g), madera, null, true);
        Geo.Crear(PrimitiveType.Cube, escenario, "Pared_S", new Vector3(0f, h / 2f, -(mz + g / 2f)),
            new Vector3(2f * mx + 2f * g, h, g), madera, null, true);
        Geo.Crear(PrimitiveType.Cube, escenario, "Pared_E", new Vector3(mx + g / 2f, h / 2f, 0f),
            new Vector3(g, h, 2f * mz), madera, null, true);
        Geo.Crear(PrimitiveType.Cube, escenario, "Pared_O", new Vector3(-(mx + g / 2f), h / 2f, 0f),
            new Vector3(g, h, 2f * mz), madera, null, true);

        // Pilares dorados en las esquinas con luces de colores
        Color[] coloresLuz = { new Color(0.3f, 0.5f, 1f), new Color(1f, 0.3f, 0.8f) };
        int n = 0;
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 baseP = new Vector3(sx * mx, 0f, sz * mz);
                Geo.Crear(PrimitiveType.Cylinder, escenario, "Pilar", baseP + Vector3.up * 1.5f,
                    new Vector3(0.9f, 1.5f, 0.9f), dorado);
                Geo.Crear(PrimitiveType.Sphere, escenario, "Lampara", baseP + Vector3.up * 3.2f,
                    Vector3.one * 0.7f, lampara);
                EntornoVisual.CrearLuzPuntual(escenario, baseP + Vector3.up * 3.4f, coloresLuz[n % 2], 9f, 1.2f);
                n++;
            }
        }

        // Foco cálido sobre el centro de la mesa
        EntornoVisual.CrearFoco(escenario, new Vector3(0f, 12f, 0f), new Color(1f, 0.92f, 0.75f));
    }

    void ConstruirCartas()
    {
        int total = cfg.columnas * cfg.filas;
        paresTotales = total / 2;

        List<int> ids = new List<int>();
        for (int i = 0; i < paresTotales; i++) { ids.Add(i); ids.Add(i); }
        for (int i = ids.Count - 1; i > 0; i--) // barajar (Fisher-Yates)
        {
            int j = Random.Range(0, i + 1);
            int tmp = ids[i]; ids[i] = ids[j]; ids[j] = tmp;
        }

        int k = 0;
        for (int r = 0; r < cfg.filas; r++)
        {
            for (int c = 0; c < cfg.columnas; c++)
            {
                Vector3 pos = new Vector3((c - (cfg.columnas - 1) * 0.5f) * SepX, 0.04f,
                                          (r - (cfg.filas - 1) * 0.5f) * SepZ);
                cartas.Add(Carta.Crear(escenario, pos, ids[k++]));
            }
        }
    }

    void ConstruirJugador()
    {
        jugador = Jugador.Crear(new Vector3(0f, 0.1f, -mz + 1.6f), cfg.velocidadJugador);
    }

    void ConstruirEnemigos()
    {
        for (int k = 0; k < cfg.enemigos; k++)
        {
            // Cada guardián patrulla un pasillo entre filas de cartas
            float z = (k - (cfg.enemigos - 1) * 0.5f) * SepZ;
            Vector3 a = new Vector3(-mx + 1.2f, 0.9f, z);
            Vector3 b = new Vector3(mx - 1.2f, 0.9f, z);
            if (k % 2 == 1) { Vector3 t = a; a = b; b = t; }
            Enemigo.Crear(escenario, a, b, cfg.velEnemigo, k * 0.37f);
        }
    }

    // =====================================================================
    //  INTERFAZ
    // =====================================================================

    void ConstruirUI()
    {
        Canvas canvas = UIFactory.CrearCanvas("CanvasJuego");
        canvasRect = canvas.GetComponent<RectTransform>();
        Transform raiz = canvas.transform;

        txtPuntos = UIFactory.CrearTexto(raiz, "PUNTOS: 0", 50, TextAnchor.UpperLeft);
        UIFactory.Colocar(txtPuntos.rectTransform, new Vector2(0f, 1f), new Vector2(50f, -30f), new Vector2(700f, 70f));

        txtPares = UIFactory.CrearTexto(raiz, "PARES: 0/" + paresTotales, 38, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f));
        UIFactory.Colocar(txtPares.rectTransform, new Vector2(0f, 1f), new Vector2(50f, -95f), new Vector2(700f, 55f));

        txtNivel = UIFactory.CrearTexto(raiz, cfg.nombre, 38, TextAnchor.UpperCenter, new Color(0.85f, 0.9f, 1f));
        UIFactory.Colocar(txtNivel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(900f, 55f));

        txtTiempo = UIFactory.CrearTexto(raiz, "", 76, TextAnchor.UpperCenter, UIFactory.ColorPrimario);
        UIFactory.Colocar(txtTiempo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -75f), new Vector2(500f, 100f));

        txtCombo = UIFactory.CrearTexto(raiz, "", 52, TextAnchor.UpperCenter, new Color(1f, 0.55f, 0.2f));
        UIFactory.Colocar(txtCombo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(700f, 70f));

        txtCentro = UIFactory.CrearTexto(raiz, "", 120, TextAnchor.MiddleCenter);
        UIFactory.Colocar(txtCentro.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1400f, 300f));

        Button btnPausa = UIFactory.CrearBoton(raiz, "II", new Vector2(120f, 120f), () => Pausar(true),
            UIFactory.ColorSecundario, 56, Color.white);
        UIFactory.Colocar(btnPausa.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(120f, 120f));

        jugador.joystick = JoystickVirtual.Crear(raiz);

        // Ventana de pausa
        Vector2 tamBoton = new Vector2(560f, 100f);
        RectTransform vp = UIFactory.CrearVentana(raiz, "VentanaPausa", "PAUSA", new Vector2(760f, 620f), out panelPausa, out _);
        UIFactory.CrearBoton(vp, "Continuar", tamBoton, () => Pausar(false));
        UIFactory.CrearBoton(vp, "Reiniciar nivel", tamBoton, () => IrA(ConfigNiveles.EscenaJuego), UIFactory.ColorSecundario, 44, Color.white);
        UIFactory.CrearBoton(vp, "Menú principal", tamBoton, () => IrA(ConfigNiveles.EscenaMenu), UIFactory.ColorSecundario, 44, Color.white);
        panelPausa.SetActive(false);

        // Ventana de fin de nivel
        RectTransform vf = UIFactory.CrearVentana(raiz, "VentanaFin", "", new Vector2(860f, 900f), out panelFin, out txtFinTitulo);
        txtFinDetalle = UIFactory.CrearTexto(vf, "", 40, TextAnchor.MiddleCenter);
        txtFinDetalle.rectTransform.sizeDelta = new Vector2(780f, 190f);
        estrellasFin = UIFactory.CrearEstrellas(vf, 0, 90f);
        Vector2 tamBotonFin = new Vector2(620f, 100f);
        btnSiguiente = UIFactory.CrearBoton(vf, "Siguiente nivel", tamBotonFin, () =>
        {
            ConfigNiveles.NivelActual = nivelIdx + 1;
            IrA(ConfigNiveles.EscenaJuego);
        });
        UIFactory.CrearBoton(vf, "Reintentar", tamBotonFin, () => IrA(ConfigNiveles.EscenaJuego), UIFactory.ColorSecundario, 44, Color.white);
        UIFactory.CrearBoton(vf, "Menú principal", tamBotonFin, () => IrA(ConfigNiveles.EscenaMenu), UIFactory.ColorSecundario, 44, Color.white);
        panelFin.SetActive(false);

        transicion = Transicion.Crear(raiz);
    }

    void IrA(string escena) { transicion.IrA(escena); }

    void ActualizarHUD()
    {
        puntosMostrados = Mathf.MoveTowards(puntosMostrados, puntos,
            Mathf.Max(300f, Mathf.Abs(puntos - puntosMostrados) * 4f) * Time.unscaledDeltaTime);
        txtPuntos.text = "PUNTOS: " + Mathf.RoundToInt(puntosMostrados);
        pulsoPuntos = Mathf.MoveTowards(pulsoPuntos, 0f, 3f * Time.unscaledDeltaTime);
        txtPuntos.rectTransform.localScale = Vector3.one * (1f + 0.25f * pulsoPuntos);

        txtPares.text = "PARES: " + paresEncontrados + "/" + paresTotales;

        int seg = Mathf.CeilToInt(tiempoRestante);
        txtTiempo.text = (seg / 60) + ":" + (seg % 60).ToString("00");
        bool urgente = tiempoRestante <= 10f && estado != Estado.Fin;
        txtTiempo.color = urgente
            ? Color.Lerp(Color.white, new Color(1f, 0.25f, 0.2f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f))
            : UIFactory.ColorPrimario;

        txtCombo.text = combo >= 2 ? "¡COMBO x" + Mathf.Min(combo, 5) + "!" : "";
    }

    void TextoFlotante(string texto, Vector3 mundo, Color color, int tam)
    {
        Text t = UIFactory.CrearTexto(canvasRect, texto, tam, TextAnchor.MiddleCenter, color);
        t.rectTransform.sizeDelta = new Vector2(500f, 100f);
        Vector2 pantalla = cam.WorldToScreenPoint(mundo);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pantalla, null, out local);
        t.rectTransform.anchoredPosition = local;
        StartCoroutine(AnimarTexto(t, local));
    }

    IEnumerator AnimarTexto(Text t, Vector2 inicio)
    {
        float tiempo = 0f;
        Color c = t.color;
        while (tiempo < 1f && t != null)
        {
            tiempo += Time.unscaledDeltaTime / 1.0f;
            float k = Mathf.Clamp01(tiempo);
            t.rectTransform.anchoredPosition = inicio + Vector2.up * (140f * (1f - (1f - k) * (1f - k)));
            t.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, Mathf.Clamp01(k * 4f));
            t.color = new Color(c.r, c.g, c.b, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }

    // =====================================================================
    //  FLUJO DEL JUEGO
    // =====================================================================

    IEnumerator Preparacion()
    {
        estado = Estado.Preparando;
        jugador.PuedeMoverse = false;
        yield return new WaitForSeconds(0.8f);

        // Vista previa: se muestran todas las cartas para memorizarlas
        txtCentro.fontSize = 100;
        txtCentro.text = "¡Memoriza las cartas!";
        foreach (Carta c in cartas) StartCoroutine(c.Voltear(true, 0.4f));
        AudioManager.Instancia.Play(Sfx.Voltear);
        yield return new WaitForSeconds(cfg.vistaPrevia + 0.4f);

        foreach (Carta c in cartas) StartCoroutine(c.Voltear(false, 0.4f));
        txtCentro.text = "";
        yield return new WaitForSeconds(0.6f);

        // Cuenta regresiva
        txtCentro.fontSize = 220;
        string[] pasos = { "3", "2", "1", "¡YA!" };
        foreach (string p in pasos)
        {
            txtCentro.text = p;
            AudioManager.Instancia.Play(Sfx.Clic);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.8f;
                float k = Mathf.Clamp01(t);
                txtCentro.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, 1f - (1f - k) * (1f - k));
                yield return null;
            }
        }
        txtCentro.text = "";
        txtCentro.rectTransform.localScale = Vector3.one;

        estado = Estado.Jugando;
        jugador.PuedeMoverse = true;
    }

    void Update()
    {
        if (UIFactory.EscapePresionado())
        {
            if (estado == Estado.Jugando) Pausar(true);
            else if (estado == Estado.Pausa) Pausar(false);
        }

        if (estado == Estado.Jugando)
        {
            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0f)
            {
                tiempoRestante = 0f;
                Terminar(false);
            }
            else
            {
                DetectarCartas();
            }
        }

        ActualizarHUD();
    }

    void DetectarCartas()
    {
        Vector3 p = jugador.transform.position;
        for (int i = 0; i < cartas.Count; i++)
        {
            Carta c = cartas[i];
            if (c.Emparejada) continue;

            Vector3 d = p - c.transform.position;
            bool dentro = Mathf.Abs(d.x) < Carta.Tamano.x * 0.5f + 0.05f &&
                          Mathf.Abs(d.z) < Carta.Tamano.y * 0.5f + 0.05f;
            if (dentro && !c.JugadorDentro) IntentarVoltear(c);
            c.JugadorDentro = dentro; // hay que salir y volver a entrar para repetir
        }
    }

    void IntentarVoltear(Carta c)
    {
        if (bloqueo || c.Volteada || c.Emparejada) return;

        reveladas.Add(c);
        StartCoroutine(c.Voltear(true));
        AudioManager.Instancia.Play(Sfx.Voltear);
        Efectos.Particulas(c.transform.position + Vector3.up * 0.2f, Color.white, 8, 2.5f, 0.12f);

        if (reveladas.Count == 2) StartCoroutine(Resolver());
    }

    IEnumerator Resolver()
    {
        bloqueo = true;
        Carta a = reveladas[0];
        Carta b = reveladas[1];
        Vector3 medio = (a.transform.position + b.transform.position) * 0.5f;

        yield return new WaitForSeconds(0.4f); // deja terminar el giro
        if (estado == Estado.Fin) yield break;

        if (a.Id == b.Id)
        {
            // ACIERTO
            combo++;
            int ganado = cfg.puntosPar * Mathf.Min(combo, 5);
            puntos += ganado;
            paresEncontrados++;
            pulsoPuntos = 1f;

            Color col = Carta.ColorDe(a.Id);
            a.Emparejar();
            b.Emparejar();
            Efectos.Particulas(a.transform.position + Vector3.up * 0.6f, col, 35, 6f, 0.25f);
            Efectos.Particulas(b.transform.position + Vector3.up * 0.6f, col, 35, 6f, 0.25f);
            Efectos.Destello(medio, col);
            AudioManager.Instancia.Play(Sfx.Acierto);
            if (combo >= 2) AudioManager.Instancia.Play(Sfx.Combo);
            camara.Pulso(4f);
            TextoFlotante("+" + ganado, medio + Vector3.up, UIFactory.ColorPrimario, 70);

            reveladas.Clear();
            bloqueo = false;
            if (paresEncontrados >= paresTotales) Terminar(true);
        }
        else
        {
            // ERROR
            combo = 0;
            errores++;
            puntos = Mathf.Max(0, puntos - PenalizacionError);
            AudioManager.Instancia.Play(Sfx.Error);
            camara.Sacudir(0.25f, 0.25f);
            Vibrar();
            TextoFlotante("-" + PenalizacionError, medio + Vector3.up, new Color(1f, 0.35f, 0.3f), 60);

            yield return new WaitForSeconds(cfg.tiempoMostrar);
            if (estado == Estado.Fin) yield break;

            StartCoroutine(a.Voltear(false));
            StartCoroutine(b.Voltear(false));
            yield return new WaitForSeconds(0.4f);
            reveladas.Clear();
            bloqueo = false;
        }
    }

    /// <summary>Llamado por los guardianes al tocar al jugador.</summary>
    public void JugadorGolpeado(Vector3 origen)
    {
        if (estado != Estado.Jugando) return;
        if (!jugador.Golpeado(origen)) return;

        tiempoRestante = Mathf.Max(0f, tiempoRestante - PenalizacionGolpe);
        combo = 0;
        AudioManager.Instancia.Play(Sfx.Golpe);
        camara.Sacudir(0.5f, 0.35f);
        Efectos.Particulas(jugador.transform.position + Vector3.up, new Color(1f, 0.3f, 0.2f), 25, 6f, 0.25f);
        TextoFlotante("-" + (int)PenalizacionGolpe + " s", jugador.transform.position + Vector3.up * 2.5f,
            new Color(1f, 0.35f, 0.3f), 60);
        Vibrar();
    }

    void Terminar(bool victoria)
    {
        if (estado == Estado.Fin) return;
        estado = Estado.Fin;
        jugador.PuedeMoverse = false;

        int bonus = victoria ? Mathf.RoundToInt(tiempoRestante) * 10 : 0;
        puntos += bonus;
        if (bonus > 0) pulsoPuntos = 1f;

        int estrellas = 0;
        if (victoria)
            estrellas = tiempoRestante >= cfg.tiempo * 0.5f ? 3 : (tiempoRestante >= cfg.tiempo * 0.25f ? 2 : 1);

        int recordPrevio = Guardado.Datos.mejoresPuntos[nivelIdx];
        bool nuevoRecord = victoria && puntos > recordPrevio;
        if (victoria) Guardado.RegistrarResultado(nivelIdx, puntos, estrellas);

        StartCoroutine(MostrarFin(victoria, bonus, estrellas, nuevoRecord));
    }

    IEnumerator MostrarFin(bool victoria, int bonus, int estrellas, bool nuevoRecord)
    {
        AudioManager.Instancia.Play(victoria ? Sfx.Victoria : Sfx.Derrota);
        if (victoria)
        {
            Efectos.Particulas(jugador.transform.position + Vector3.up * 2f, UIFactory.ColorPrimario, 80, 8f, 0.3f);
            Efectos.Destello(jugador.transform.position, UIFactory.ColorPrimario, 6f);
        }
        else
        {
            camara.Sacudir(0.4f, 0.5f);
        }
        yield return new WaitForSeconds(1.0f);

        if (victoria)
        {
            txtFinTitulo.text = "¡NIVEL COMPLETADO!";
            int record = Guardado.Datos.mejoresPuntos[nivelIdx];
            txtFinDetalle.text = "Puntuación: " + puntos + "\nBonus de tiempo: +" + bonus +
                                 "\nErrores: " + errores + "\n" +
                                 (nuevoRecord ? "¡Nuevo récord!" : "Récord del nivel: " + record);
        }
        else
        {
            txtFinTitulo.text = "¡TIEMPO AGOTADO!";
            txtFinDetalle.text = "Encontraste " + paresEncontrados + " de " + paresTotales + " pares." +
                                 "\nPuntuación: " + puntos + "\n¡Inténtalo de nuevo!";
        }
        UIFactory.PintarEstrellas(estrellasFin, estrellas);
        btnSiguiente.gameObject.SetActive(victoria && nivelIdx < ConfigNiveles.Total - 1);
        panelFin.SetActive(true);
    }

    // =====================================================================
    //  PAUSA Y UTILIDADES
    // =====================================================================

    void Pausar(bool pausar)
    {
        if (panelPausa == null) return;
        if (pausar && estado == Estado.Jugando)
        {
            estado = Estado.Pausa;
            Time.timeScale = 0f;
            panelPausa.SetActive(true);
        }
        else if (!pausar && estado == Estado.Pausa)
        {
            estado = Estado.Jugando;
            Time.timeScale = 1f;
            panelPausa.SetActive(false);
        }
    }

    void OnApplicationPause(bool enPausa)
    {
        if (enPausa) Pausar(true);
    }

    void Vibrar()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
