using UnityEngine;

public enum Sfx { Clic, Voltear, Acierto, Error, Golpe, Victoria, Derrota, Combo }

/// <summary>
/// Reproduce efectos y música. Todos los sonidos se generan por código,
/// así el proyecto no depende de archivos de audio externos.
/// </summary>
public class AudioManager : MonoBehaviour
{
    static AudioManager _instancia;

    public static AudioManager Instancia
    {
        get
        {
            if (_instancia == null)
            {
                GameObject go = new GameObject("AudioManager");
                DontDestroyOnLoad(go);
                _instancia = go.AddComponent<AudioManager>();
                _instancia.Iniciar();
            }
            return _instancia;
        }
    }

    AudioSource fuenteSfx, fuenteMusica;
    AudioClip[] clips;

    void Iniciar()
    {
        fuenteSfx = gameObject.AddComponent<AudioSource>();
        fuenteSfx.playOnAwake = false;
        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteMusica.loop = true;
        fuenteMusica.clip = CrearMusica();

        clips = new AudioClip[8];
        clips[(int)Sfx.Clic] = Tonos("clic", new float[] { 880f }, 0.06f, 30f, false, 0.5f);
        clips[(int)Sfx.Voltear] = Tonos("voltear", new float[] { 600f, 780f }, 0.05f, 22f, false, 0.5f);
        clips[(int)Sfx.Acierto] = Tonos("acierto", new float[] { 523f, 659f, 784f, 1047f }, 0.1f, 8f, false, 0.55f);
        clips[(int)Sfx.Error] = Tonos("error", new float[] { 160f, 120f }, 0.16f, 4f, true, 0.45f);
        clips[(int)Sfx.Golpe] = Tonos("golpe", new float[] { 110f, 80f, 60f }, 0.08f, 6f, true, 0.6f);
        clips[(int)Sfx.Victoria] = Tonos("victoria", new float[] { 523f, 659f, 784f, 1047f, 784f, 1047f, 1319f }, 0.14f, 5f, false, 0.55f);
        clips[(int)Sfx.Derrota] = Tonos("derrota", new float[] { 392f, 330f, 262f, 196f }, 0.22f, 3f, true, 0.45f);
        clips[(int)Sfx.Combo] = Tonos("combo", new float[] { 784f, 988f, 1175f }, 0.07f, 9f, false, 0.5f);

        AplicarVolumen();
        fuenteMusica.Play();
    }

    public void Play(Sfx sfx)
    {
        if (!Guardado.Datos.sonidoActivo) return;
        fuenteSfx.pitch = (sfx == Sfx.Voltear) ? Random.Range(0.92f, 1.12f) : 1f;
        fuenteSfx.PlayOneShot(clips[(int)sfx]);
    }

    public void AplicarVolumen()
    {
        DatosJugador d = Guardado.Datos;
        fuenteSfx.volume = d.sonidoActivo ? d.volumen : 0f;
        fuenteMusica.volume = d.sonidoActivo ? d.volumen * 0.35f : 0f;
    }

    // ---------- Generación de sonido ----------

    static AudioClip Tonos(string nombre, float[] frecuencias, float duracionNota, float caida, bool cuadrada, float volumen)
    {
        const int sr = 44100;
        int porNota = (int)(sr * duracionNota);
        int total = porNota * frecuencias.Length;
        float[] datos = new float[total];
        int fade = (int)(sr * 0.01f);

        for (int i = 0; i < total; i++)
        {
            int idx = i / porNota;
            int local = i % porNota;
            float t = (float)local / sr;
            float env = Mathf.Exp(-t * caida) * Mathf.Clamp01(local / 150f);
            float s = Mathf.Sin(2f * Mathf.PI * frecuencias[idx] * t);
            if (cuadrada) s = Mathf.Sign(s) * 0.6f;
            float salida = Mathf.Clamp01((total - i) / (float)fade);
            datos[i] = s * env * volumen * salida;
        }

        AudioClip clip = AudioClip.Create(nombre, total, 1, sr, false);
        clip.SetData(datos, 0);
        return clip;
    }

    static AudioClip CrearMusica()
    {
        const int sr = 44100;
        float[] notas =
        {
            220f, 261.63f, 329.63f, 392f, 329.63f, 261.63f, 440f, 392f,
            220f, 261.63f, 329.63f, 440f, 392f, 329.63f, 261.63f, 196f
        };
        int porNota = (int)(sr * 0.3f);
        float[] datos = new float[porNota * notas.Length];

        for (int i = 0; i < datos.Length; i++)
        {
            int idx = i / porNota;
            int local = i % porNota;
            float t = (float)local / sr;
            float frac = (float)local / porNota;
            float env = Mathf.Sin(Mathf.PI * frac);
            env *= env; // llega a cero en los extremos: sin chasquidos al repetir
            float f = notas[idx];
            float s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f
                    + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.12f;
            if (idx % 4 == 0) s += Mathf.Sin(2f * Mathf.PI * f * 0.5f * t) * 0.4f;
            datos[i] = s * env * 0.4f;
        }

        AudioClip clip = AudioClip.Create("musica", datos.Length, 1, sr, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
