using UnityEngine;

/// <summary>Personaje que se desplaza por la mesa en 3D (joystick en móvil, WASD/flechas en PC).</summary>
[RequireComponent(typeof(CharacterController))]
public class Jugador : MonoBehaviour
{
    public static Jugador Instancia { get; private set; }

    public JoystickVirtual joystick;
    public bool PuedeMoverse = true;
    public float velocidad = 5.5f;

    CharacterController cc;
    Transform visual;
    Renderer[] renderers;
    ParticleSystem polvo;
    float vy;
    float invulnerable;
    Vector3 empuje;

    public static Jugador Crear(Vector3 pos, float velocidad)
    {
        GameObject go = new GameObject("Jugador");
        go.transform.position = pos; // se posiciona antes de añadir el CharacterController

        CharacterController c = go.AddComponent<CharacterController>();
        c.height = 1.8f;
        c.radius = 0.4f;
        c.center = new Vector3(0f, 0.9f, 0f);
        c.stepOffset = 0.3f;

        Jugador j = go.AddComponent<Jugador>();
        j.velocidad = velocidad;
        j.ConstruirVisual();
        Instancia = j;
        return j;
    }

    void Awake() { cc = GetComponent<CharacterController>(); }

    void ConstruirVisual()
    {
        Material cuerpo = Materiales.Cargar("Jugador", new Color(0.95f, 0.45f, 0.15f));
        Material oscuro = Materiales.Cargar("JugadorOscuro", new Color(0.08f, 0.08f, 0.10f));
        Material blanco = Materiales.Cargar("Blanco", Color.white);
        Material oro = Materiales.Cargar("Dorado", new Color(1f, 0.78f, 0.25f));

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);

        Geo.Crear(PrimitiveType.Capsule, visual, "Cuerpo", new Vector3(0f, 0.9f, 0f), Vector3.one * 0.9f, cuerpo);
        // Sombrero de crupier
        Geo.Crear(PrimitiveType.Cylinder, visual, "SombreroAla", new Vector3(0f, 1.62f, 0f), new Vector3(0.85f, 0.05f, 0.85f), oscuro);
        Geo.Crear(PrimitiveType.Cylinder, visual, "SombreroCopa", new Vector3(0f, 1.9f, 0f), new Vector3(0.52f, 0.28f, 0.52f), oscuro);
        Geo.Crear(PrimitiveType.Cylinder, visual, "SombreroCinta", new Vector3(0f, 1.7f, 0f), new Vector3(0.54f, 0.04f, 0.54f), oro);
        // Ojos (el personaje mira hacia +Z)
        for (int i = -1; i <= 1; i += 2)
        {
            Geo.Crear(PrimitiveType.Sphere, visual, "Ojo", new Vector3(0.17f * i, 1.3f, 0.38f), Vector3.one * 0.2f, blanco);
            Geo.Crear(PrimitiveType.Sphere, visual, "Pupila", new Vector3(0.17f * i, 1.3f, 0.47f), Vector3.one * 0.09f, oscuro);
        }

        renderers = visual.GetComponentsInChildren<Renderer>();
        polvo = Efectos.CrearPolvo(transform);
    }

    void Update()
    {
        Vector2 entrada = PuedeMoverse ? LeerEntrada() : Vector2.zero;
        Vector3 dir = new Vector3(entrada.x, 0f, entrada.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy -= 25f * Time.deltaTime;
        empuje = Vector3.Lerp(empuje, Vector3.zero, 6f * Time.deltaTime);
        cc.Move((dir * velocidad + empuje + Vector3.up * vy) * Time.deltaTime);

        float mov = dir.magnitude;
        if (mov > 0.05f)
            visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(dir), 14f * Time.deltaTime);

        // Estirar y aplastar al caminar (animación sencilla)
        float osc = Mathf.Sin(Time.time * 14f) * 0.06f * mov;
        visual.localScale = new Vector3(1f + osc, 1f - osc, 1f + osc);

        if (polvo != null)
        {
            ParticleSystem.EmissionModule em = polvo.emission;
            em.enabled = mov > 0.1f;
        }

        // Parpadeo tras recibir un golpe
        if (invulnerable > 0f)
        {
            invulnerable -= Time.deltaTime;
            bool visible = invulnerable <= 0f || Mathf.Sin(Time.time * 40f) > 0f;
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = visible;
        }
    }

    Vector2 LeerEntrada()
    {
        Vector2 v = Vector2.zero;
        if (joystick != null) v = joystick.Valor;
#if ENABLE_LEGACY_INPUT_MANAGER
        Vector2 k = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (k.sqrMagnitude > 0.01f) v = k;
#endif
        return v;
    }

    /// <summary>Devuelve false si el personaje aún es invulnerable.</summary>
    public bool Golpeado(Vector3 origen)
    {
        if (invulnerable > 0f) return false;
        invulnerable = 1.6f;
        Vector3 d = transform.position - origen;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) d = Vector3.back;
        empuje = d.normalized * 10f;
        return true;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }
}
