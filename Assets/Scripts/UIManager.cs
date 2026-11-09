using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Referencias")]
    public TextureGenerator generator;
    public AutoUIBuilder uiBuilder;

    [Header("Estado de Selección")]
    public int selectedLayerIndex = 0;

    // =========================================================================
    // IMPORTACIÓN NATIVA DIRECTA PARA WINDOWS STANDALONE
    // =========================================================================
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinOpenFileName
    {
        public int structSize;
        public IntPtr dlgOwner;
        public IntPtr instance;
        public string filter;
        public string customFilter;
        public int maxCustomFilter;
        public int filterIndex;
        public string file;
        public int maxFile;
        public string fileTitle;
        public int maxFileTitle;
        public string initialDir;
        public string title;
        public int flags;
        public short fileOffset;
        public short fileExtension;
        public string defExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public IntPtr lpTemplateName;
        public IntPtr reservedPtr;
        public int reservedInt;
        public int flagsEx;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetOpenFileName(ref WinOpenFileName ofn);

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetSaveFileName(ref WinOpenFileName ofn);
#endif

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();
    }

    public void EnsureReferences()
    {
        if (generator == null) generator = FindFirstObjectByType<TextureGenerator>();
        if (uiBuilder == null) uiBuilder = FindFirstObjectByType<AutoUIBuilder>();
    }

    private string SimpleOpenFile(string title, string filter, string defaultExt)
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.OpenFilePanel(title, "", defaultExt);
#elif UNITY_STANDALONE_WIN
        string currentDir = Environment.CurrentDirectory; // Protege a Unity del cambio de carpeta
        string selectedPath = null;

        WinOpenFileName ofn = new WinOpenFileName();
        ofn.structSize = Marshal.SizeOf(typeof(WinOpenFileName));
        ofn.dlgOwner = GetActiveWindow();
        ofn.filter = filter.Replace('|', '\0') + "\0\0";
        ofn.file = new string(new char[512]);
        ofn.maxFile = ofn.file.Length;
        ofn.title = title;
        ofn.defExt = defaultExt;
        // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
        ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;

        if (GetOpenFileName(ref ofn))
        {
            selectedPath = ofn.file;
        }

        Environment.CurrentDirectory = currentDir; // Restaura el directorio de Unity
        return selectedPath;
#else
        Debug.LogWarning("[UIManager] El explorador de archivos no está soportado en esta plataforma.");
        return null;
#endif
    }

    private string SimpleSaveFile(string title, string defaultName, string filter, string defaultExt)
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.SaveFilePanel(title, "", defaultName, defaultExt);
#elif UNITY_STANDALONE_WIN
        string currentDir = Environment.CurrentDirectory; // Protege a Unity del cambio de carpeta
        string selectedPath = null;

        WinOpenFileName ofn = new WinOpenFileName();
        ofn.structSize = Marshal.SizeOf(typeof(WinOpenFileName));
        ofn.dlgOwner = GetActiveWindow();
        ofn.filter = filter.Replace('|', '\0') + "\0\0";
        ofn.file = defaultName + new string('\0', 512 - defaultName.Length);
        ofn.maxFile = 512;
        ofn.title = title;
        ofn.defExt = defaultExt;
        // OFN_EXPLORER | OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
        ofn.flags = 0x00080000 | 0x00000002 | 0x00000800 | 0x00000008;

        if (GetSaveFileName(ref ofn))
        {
            selectedPath = ofn.file;
        }

        Environment.CurrentDirectory = currentDir; // Restaura el directorio de Unity
        return selectedPath;
#else
        Debug.LogWarning("[UIManager] El explorador de archivos no está soportado en esta plataforma.");
        return null;
#endif
    }

    // =========================================================================
    // DIÁLOGOS DE SELECCIÓN DE ARCHIVOS
    // =========================================================================

    public void OpenSelectPngDialogForLayer(int index)
    {
        EnsureReferences();
        string path = SimpleOpenFile("Seleccionar PNG", "Archivos PNG (*.png)|*.png|Todos los archivos (*.*)|*.*", "png");
        if (!string.IsNullOrEmpty(path))
        {
            LoadPngToLayer(index, path);
        }
    }

    public void OpenSaveTxtrDialog()
    {
        EnsureReferences();
        string path = SimpleSaveFile("Guardar Proyecto Txtr", "Proyecto.txtr", "Proyecto Txtr (*.txtr)|*.txtr", "txtr");
        if (!string.IsNullOrEmpty(path))
        {
            SaveProjectTxtr(path);
        }
    }

    public void OpenLoadTxtrDialog()
    {
        EnsureReferences();
        string path = SimpleOpenFile("Cargar Proyecto Txtr", "Proyecto Txtr (*.txtr)|*.txtr", "txtr");
        if (!string.IsNullOrEmpty(path))
        {
            LoadProjectTxtr(path);
        }
    }

    public void OpenExportPngDialog()
    {
        EnsureReferences();
        string path = SimpleSaveFile("Exportar como PNG", "Textura.png", "Imagen PNG (*.png)|*.png", "png");
        if (!string.IsNullOrEmpty(path))
        {
            ExportTexture(path, "png");
        }
    }

    public void OpenExportJpgDialog()
    {
        EnsureReferences();
        string path = SimpleSaveFile("Exportar como JPG", "Textura.jpg", "Imagen JPG (*.jpg)|*.jpg", "jpg");
        if (!string.IsNullOrEmpty(path))
        {
            ExportTexture(path, "jpg");
        }
    }

    // =========================================================================
    // GESTIÓN DE CAPAS Y CARGA DE PNG
    // =========================================================================

    public void AddNewStampLayer()
    {
        EnsureReferences();
        if (generator == null) return;

        if (generator.stampLayers == null)
            generator.stampLayers = new List<TextureGenerator.StampLayer>();

        var newLayer = new TextureGenerator.StampLayer
        {
            layerName = "Capa " + (generator.stampLayers.Count + 1)
        };
        generator.stampLayers.Add(newLayer);
        selectedLayerIndex = generator.stampLayers.Count - 1;

        RefreshUI();
    }

    public void SelectStampLayer(int index)
    {
        EnsureReferences();
        if (generator == null || generator.stampLayers == null || index < 0 || index >= generator.stampLayers.Count) return;

        selectedLayerIndex = index;
        OpenSelectPngDialogForLayer(index);
    }

    public void LoadPngToLayer(int index, string path)
    {
        EnsureReferences();
        if (generator == null || generator.stampLayers == null || index < 0 || index >= generator.stampLayers.Count) return;

        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                var layer = generator.stampLayers[index];
                layer.texture = tex;
                layer.pngPath = path;
                if (string.IsNullOrEmpty(layer.layerName) || layer.layerName.StartsWith("Capa "))
                {
                    layer.layerName = Path.GetFileNameWithoutExtension(path);
                }

                generator.GenerateAndApply();
                RefreshUI();
            }
        }
    }

    // =========================================================================
    // GUARDADO / CARGA JSON Y EXPORTACIÓN
    // =========================================================================

    [System.Serializable]
    public class ProjectData
    {
        public int width;
        public int height;
        public int patternType;
        public int interpolationType;
        public float rotationAngle;
        public float noiseScale;
        public List<TextureGenerator.ColorStop> colorStops;
        public List<LayerData> stampLayers;

        public float waveIntensity;
        public float waveFrequency;
        public float waveDirection;
    }

    [System.Serializable]
    public class LayerData
    {
        public string layerName;
        public string pngPath;
        public int count;
        public int seed;
        public bool randomScale;
        public float scale;
        public float minScale;
        public float maxScale;
        public bool randomRotation;
        public float rotation;
        public float minRotation;
        public float maxRotation;
    }

    public void SaveProjectTxtr(string path)
    {
        EnsureReferences();
        if (generator == null) return;

        ProjectData data = new ProjectData
        {
            width = generator.width,
            height = generator.height,
            patternType = (int)generator.patternType,
            interpolationType = (int)generator.interpolationType,
            rotationAngle = generator.rotationAngle,
            noiseScale = generator.noiseScale,
            colorStops = generator.colorStops,
            waveIntensity = generator.waveIntensity,
            waveFrequency = generator.waveFrequency,
            waveDirection = generator.waveDirection,
            stampLayers = new List<LayerData>()
        };

        if (generator.stampLayers != null)
        {
            foreach (var l in generator.stampLayers)
            {
                data.stampLayers.Add(new LayerData
                {
                    layerName = l.layerName,
                    pngPath = l.pngPath,
                    count = l.count,
                    seed = l.seed,
                    randomScale = l.randomScale,
                    scale = l.scale,
                    minScale = l.minScale,
                    maxScale = l.maxScale,
                    randomRotation = l.randomRotation,
                    rotation = l.rotation,
                    minRotation = l.minRotation,
                    maxRotation = l.maxRotation
                });
            }
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public void LoadProjectTxtr(string path)
    {
        EnsureReferences();
        if (generator == null || !File.Exists(path)) return;

        string json = File.ReadAllText(path);
        ProjectData data = JsonUtility.FromJson<ProjectData>(json);
        if (data == null) return;

        generator.width = data.width;
        generator.height = data.height;
        generator.patternType = (TextureGenerator.PatternType)data.patternType;
        generator.interpolationType = (TextureGenerator.InterpolationType)data.interpolationType;
        generator.rotationAngle = data.rotationAngle;
        generator.noiseScale = data.noiseScale;
        generator.colorStops = data.colorStops ?? new List<TextureGenerator.ColorStop>();
        generator.waveIntensity = data.waveIntensity;
        generator.waveFrequency = data.waveFrequency;
        generator.waveDirection = data.waveDirection;

        if (generator.stampLayers == null) generator.stampLayers = new List<TextureGenerator.StampLayer>();
        generator.stampLayers.Clear();

        if (data.stampLayers != null)
        {
            foreach (var ld in data.stampLayers)
            {
                var layer = new TextureGenerator.StampLayer
                {
                    layerName = ld.layerName,
                    pngPath = ld.pngPath,
                    count = ld.count,
                    seed = ld.seed,
                    randomScale = ld.randomScale,
                    scale = ld.scale,
                    minScale = ld.minScale,
                    maxScale = ld.maxScale,
                    randomRotation = ld.randomRotation,
                    rotation = ld.rotation,
                    minRotation = ld.minRotation,
                    maxRotation = ld.maxRotation
                };

                if (!string.IsNullOrEmpty(ld.pngPath) && File.Exists(ld.pngPath))
                {
                    byte[] bytes = File.ReadAllBytes(ld.pngPath);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        layer.texture = tex;
                    }
                }

                generator.stampLayers.Add(layer);
            }
        }

        generator.GenerateAndApply();
        RefreshUI();
    }

    public void ExportTexture(string path, string format)
    {
        EnsureReferences();
        if (generator == null || generator.previewDisplay == null || generator.previewDisplay.texture == null) return;

        Texture2D tex = generator.previewDisplay.texture as Texture2D;
        if (tex == null) return;

        byte[] bytes = (format.ToLower() == "jpg" || format.ToLower() == "jpeg") ? tex.EncodeToJPG() : tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    public void RefreshUI()
    {
        EnsureReferences();
        if (uiBuilder != null)
        {
            uiBuilder.RefreshPngLayersList();
            uiBuilder.RefreshRightInspector();
        }
    }
}