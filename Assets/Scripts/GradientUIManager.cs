using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GradientUIManager : MonoBehaviour
{
    [Header("Referencias Principales")]
    public TextureGenerator generator;
    public ColorPickerUI colorPicker;

    [Header("UI Contenedores y Prefabs")]
    public Transform colorStopsContainer; // Un Panel con LayoutGroup para listar paradas
    public Button addColorButton;

    [Header("Prefabs Dinámicos")]
    public GameObject colorStopItemPrefab; // Objeto UI con: Image (color), Slider (pos), Button (borrar)

    private int selectedStopIndex = 0;
    private List<GameObject> activeStopUIItems = new List<GameObject>();

    void Start()
    {
        if (colorPicker != null)
        {
            colorPicker.OnColorChanged += OnColorPickerUpdated;
        }

        if (addColorButton != null)
        {
            addColorButton.onClick.AddListener(AddNewStop);
        }

        RefreshUIList();
    }

    // Regenera los elementos de la interfaz para cada color del degradado
    public void RefreshUIList()
    {
        foreach (var item in activeStopUIItems)
        {
            Destroy(item);
        }
        activeStopUIItems.Clear();

        for (int i = 0; i < generator.colorStops.Count; i++)
        {
            int index = i; // Captura de variable para eventos
            TextureGenerator.ColorStop stop = generator.colorStops[i];

            GameObject item = Instantiate(colorStopItemPrefab, colorStopsContainer);
            activeStopUIItems.Add(item);

            // Obtener componentes dentro del Prefab
            Image colorDisplay = item.transform.Find("ColorPreview").GetComponent<Image>();
            Slider posSlider = item.transform.Find("PositionSlider").GetComponent<Slider>();
            Button selectBtn = item.transform.Find("SelectButton").GetComponent<Button>();
            Button deleteBtn = item.transform.Find("DeleteButton").GetComponent<Button>();

            // Configurar datos iniciales
            colorDisplay.color = stop.color;
            posSlider.value = stop.position;

            // Eventos
            posSlider.onValueChanged.AddListener((val) => {
                generator.UpdateColorStopPosition(index, val);
            });

            selectBtn.onClick.AddListener(() => {
                selectedStopIndex = index;
                colorPicker.SetColor(generator.colorStops[index].color);
            });

            deleteBtn.onClick.AddListener(() => {
                generator.RemoveColorStop(index);
                RefreshUIList();
            });
        }
    }

    void AddNewStop()
    {
        // Añade un nuevo color en una posición intermedia
        Color newColor = colorPicker != null ? colorPicker.currentColor : Color.red;
        generator.AddColorStop(newColor, 0.5f);
        RefreshUIList();
    }

    void OnColorPickerUpdated(Color newColor)
    {
        if (generator.colorStops.Count > selectedStopIndex)
        {
            generator.UpdateColorStopColor(selectedStopIndex, newColor);

            // Actualizar el indicador visual del elemento seleccionado
            if (selectedStopIndex < activeStopUIItems.Count)
            {
                Image display = activeStopUIItems[selectedStopIndex].transform.Find("ColorPreview").GetComponent<Image>();
                if (display != null) display.color = newColor;
            }
        }
    }
}