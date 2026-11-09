using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("UI Elementos")]
    public RawImage colorSatValueMap; // El cuadrado de Saturación/Valor
    public Slider hueSlider;          // Slider para cambiar el Tono (0-360)
    public Image colorPreview;        // Muestra el color seleccionado

    [Header("Estado Actual")]
    public Color currentColor = Color.red;

    private Texture2D satValTexture;
    private float currentHue = 0f;
    private float currentSat = 1f;
    private float currentVal = 1f;

    public System.Action<Color> OnColorChanged;

    void Awake()
    {
        GenerateSatValTexture();
        if (hueSlider != null)
        {
            hueSlider.onValueChanged.AddListener(UpdateHue);
        }
    }

    void GenerateSatValTexture()
    {
        int texSize = 64;
        satValTexture = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
        satValTexture.wrapMode = TextureWrapMode.Clamp;

        UpdateSatValTexture();

        if (colorSatValueMap != null)
        {
            colorSatValueMap.texture = satValTexture;
        }
    }

    void UpdateSatValTexture()
    {
        int size = satValTexture.width;
        for (int y = 0; y < size; y++)
        {
            float v = (float)y / (size - 1);
            for (int x = 0; x < size; x++)
            {
                float s = (float)x / (size - 1);
                Color pixel = Color.HSVToRGB(currentHue, s, v);
                satValTexture.SetPixel(x, y, pixel);
            }
        }
        satValTexture.Apply();
    }

    public void UpdateHue(float hueValue)
    {
        currentHue = hueValue;
        UpdateSatValTexture();
        UpdateCurrentColor();
    }

    public void OnPointerDown(PointerEventData eventData) => HandleSatValClick(eventData);
    public void OnDrag(PointerEventData eventData) => HandleSatValClick(eventData);

    private void HandleSatValClick(PointerEventData eventData)
    {
        if (colorSatValueMap == null) return;

        RectTransform rectTransform = colorSatValueMap.rectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            Rect rect = rectTransform.rect;
            currentSat = Mathf.Clamp01((localPoint.x - rect.x) / rect.width);
            currentVal = Mathf.Clamp01((localPoint.y - rect.y) / rect.height);
            UpdateCurrentColor();
        }
    }

    private void UpdateCurrentColor()
    {
        currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);
        if (colorPreview != null) colorPreview.color = currentColor;
        OnColorChanged?.Invoke(currentColor);
    }

    public void SetColor(Color newColor)
    {
        // Método correcto de Unity para extraer HSV a partir de un Color
        Color.RGBToHSV(newColor, out currentHue, out currentSat, out currentVal);

        if (hueSlider != null) hueSlider.value = currentHue;
        UpdateSatValTexture();
        if (colorPreview != null) colorPreview.color = newColor;
    }
}