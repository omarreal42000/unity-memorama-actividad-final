using UnityEngine;

/// <summary>Cámara inclinada que sigue suavemente al jugador. Incluye sacudida y pulso de campo de visión.</summary>
public class CamaraSeguimiento : MonoBehaviour
{
    const float FovBase = 50f;
    readonly Vector3 offset = new Vector3(0f, 11f, -7.7f);

    Camera cam;
    Transform objetivo;
    Vector3 posBase;
    float tiempoSacudida, duracionSacudida, intensidadSacudida;
    float fovExtra;

    public void Iniciar(Camera camara, Transform seguir)
    {
        cam = camara;
        objetivo = seguir;
        posBase = seguir.position + offset;
        transform.position = posBase;
        transform.rotation = Quaternion.Euler(55f, 0f, 0f);
    }

    public void Sacudir(float intensidad, float duracion)
    {
        intensidadSacudida = intensidad;
        duracionSacudida = Mathf.Max(0.01f, duracion);
        tiempoSacudida = duracion;
    }

    /// <summary>Acercamiento rápido de cámara (positivo = acerca).</summary>
    public void Pulso(float grados) { fovExtra = -grados; }

    void LateUpdate()
    {
        if (objetivo == null || cam == null) return;

        posBase = Vector3.Lerp(posBase, objetivo.position + offset, 1f - Mathf.Exp(-6f * Time.deltaTime));

        Vector3 sacudida = Vector3.zero;
        if (tiempoSacudida > 0f)
        {
            tiempoSacudida -= Time.deltaTime;
            float k = Mathf.Clamp01(tiempoSacudida / duracionSacudida);
            sacudida = Random.insideUnitSphere * intensidadSacudida * k;
        }
        transform.position = posBase + sacudida;

        fovExtra = Mathf.Lerp(fovExtra, 0f, 8f * Time.deltaTime);
        cam.fieldOfView = FovBase + fovExtra;
    }
}
