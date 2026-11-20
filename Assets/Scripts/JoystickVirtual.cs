using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Joystick en pantalla: funciona con el dedo en móvil y con el ratón en PC.</summary>
public class JoystickVirtual : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Vector2 Valor { get; private set; }

    RectTransform fondo, palanca;
    float radio = 120f;

    public static JoystickVirtual Crear(Transform padre)
    {
        UnityEngine.UI.Image fondo = UIFactory.CrearImagen(padre, "Joystick", UIFactory.Circulo, new Color(1f, 1f, 1f, 0.15f));
        fondo.raycastTarget = true;
        UIFactory.Colocar(fondo.rectTransform, new Vector2(0f, 0f), new Vector2(260f, 260f),
            new Vector2(340f, 340f), new Vector2(0.5f, 0.5f));

        UnityEngine.UI.Image palanca = UIFactory.CrearImagen(fondo.transform, "Palanca", UIFactory.Circulo, new Color(1f, 1f, 1f, 0.55f));
        UIFactory.Colocar(palanca.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));

        JoystickVirtual j = fondo.gameObject.AddComponent<JoystickVirtual>();
        j.fondo = fondo.rectTransform;
        j.palanca = palanca.rectTransform;
        return j;
    }

    public void OnPointerDown(PointerEventData e) { OnDrag(e); }

    public void OnDrag(PointerEventData e)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(fondo, e.position, e.pressEventCamera, out local))
            return;
        local = Vector2.ClampMagnitude(local, radio);
        palanca.anchoredPosition = local;
        Valor = local / radio;
    }

    public void OnPointerUp(PointerEventData e)
    {
        Valor = Vector2.zero;
        palanca.anchoredPosition = Vector2.zero;
    }

    void OnDisable()
    {
        Valor = Vector2.zero;
        if (palanca != null) palanca.anchoredPosition = Vector2.zero;
    }
}
