using UnityEngine;
using UnityEngine.UI;

public class StampInspectorUI : MonoBehaviour
{
    [Header("Panel Lateral Derecho")]
    public GameObject inspectorPanel; // Asigna el panel anclado a la derecha

    [Header("UI Controls")]
    public Text titleText;
    public Slider countSlider;
    public Slider minScaleSlider;
    public Slider maxScaleSlider;
    public Slider minRotSlider;
    public Slider maxRotSlider;
    public InputField seedInputField;

    private TextureGenerator.StampLayer activeLayer;
    private TextureGenerator generator;

    public void OpenInspector(TextureGenerator.StampLayer layer, TextureGenerator targetGenerator)
    {
        activeLayer = layer;
        generator = targetGenerator;

        if (inspectorPanel != null) inspectorPanel.SetActive(true);
        if (titleText != null) titleText.text = layer.layerName;

        if (countSlider != null) { countSlider.value = layer.count; countSlider.onValueChanged.RemoveAllListeners(); countSlider.onValueChanged.AddListener(OnCountChanged); }
        if (minScaleSlider != null) { minScaleSlider.value = layer.minScale; minScaleSlider.onValueChanged.RemoveAllListeners(); minScaleSlider.onValueChanged.AddListener(OnMinScaleChanged); }
        if (maxScaleSlider != null) { maxScaleSlider.value = layer.maxScale; maxScaleSlider.onValueChanged.RemoveAllListeners(); maxScaleSlider.onValueChanged.AddListener(OnMaxScaleChanged); }
        if (minRotSlider != null) { minRotSlider.value = layer.minRotation; minRotSlider.onValueChanged.RemoveAllListeners(); minRotSlider.onValueChanged.AddListener(OnMinRotChanged); }
        if (maxRotSlider != null) { maxRotSlider.value = layer.maxRotation; maxRotSlider.onValueChanged.RemoveAllListeners(); maxRotSlider.onValueChanged.AddListener(OnMaxRotChanged); }
        if (seedInputField != null) { seedInputField.text = layer.seed.ToString(); seedInputField.onValueChanged.RemoveAllListeners(); seedInputField.onValueChanged.AddListener(OnSeedChanged); }
    }

    public void CloseInspector()
    {
        if (inspectorPanel != null) inspectorPanel.SetActive(false);
    }

    public void OnRandomizeSeed()
    {
        if (activeLayer == null) return;
        activeLayer.seed = UnityEngine.Random.Range(0, 999999);
        if (seedInputField != null) seedInputField.text = activeLayer.seed.ToString();
        generator.GenerateAndApply();
    }

    private void OnCountChanged(float val) { if (activeLayer != null) { activeLayer.count = Mathf.RoundToInt(val); generator.GenerateAndApply(); } }
    private void OnMinScaleChanged(float val) { if (activeLayer != null) { activeLayer.minScale = val; generator.GenerateAndApply(); } }
    private void OnMaxScaleChanged(float val) { if (activeLayer != null) { activeLayer.maxScale = val; generator.GenerateAndApply(); } }
    private void OnMinRotChanged(float val) { if (activeLayer != null) { activeLayer.minRotation = val; generator.GenerateAndApply(); } }
    private void OnMaxRotChanged(float val) { if (activeLayer != null) { activeLayer.maxRotation = val; generator.GenerateAndApply(); } }
    private void OnSeedChanged(string input) { if (activeLayer != null && int.TryParse(input, out int seed)) { activeLayer.seed = seed; generator.GenerateAndApply(); } }
}