using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextureGenerator : MonoBehaviour
{
    [System.Serializable]
    public class ColorStop
    {
        public Color color;
        public float position;

        public ColorStop(Color color, float position)
        {
            this.color = color;
            this.position = position;
        }
    }

    [System.Serializable]
    public class StampLayer
    {
        public string layerName = "Capa PNG";
        public string pngPath = ""; // <-- AÑADIR ESTA LÍNEA
        public Texture2D texture;
        public int count = 10;
        public int seed = 12345;

        // Controles de Escala
        public bool randomScale = false;
        public float scale = 1.0f;
        public float minScale = 0.5f;
        public float maxScale = 1.5f;

        // Controles de Rotación (grados)
        public bool randomRotation = false;
        public float rotation = 0f;
        public float minRotation = 0f;
        public float maxRotation = 360f;
    }

    public enum PatternType { Linear, Radial, Checkerboard, Noise }
    public enum InterpolationType { Linear, Smooth, Step }

    [Header("Configuración de Textura")]
    public RawImage previewDisplay;
    public int width = 512;
    public int height = 512;
    public PatternType patternType = PatternType.Linear;
    public InterpolationType interpolationType = InterpolationType.Linear;
    public float rotationAngle = 0f;
    public float noiseScale = 10f;

    [Header("Listas de Datos")]
    public List<ColorStop> colorStops = new List<ColorStop>();
    public List<StampLayer> stampLayers = new List<StampLayer>();

    private Texture2D targetTexture;


// Añadir en las variables públicas de TextureGenerator.cs
[Header("Distorsión por Ondas")]
public float waveIntensity = 0f;    // 0 = Desactivado, >0 = Intensidad
public float waveFrequency = 5f;    // Cantidad de ondas/duración
public float waveDirection = 0f;    // Ángulo de la onda (0° a 360°)

// Modificar la función GenerateBaseTexturePixels() para aplicar la distorsión:
private void GenerateBaseTexturePixels()
{
    float rad = rotationAngle * Mathf.Deg2Rad;
    float cos = Mathf.Cos(rad);
    float sin = Mathf.Sin(rad);

    float waveRad = waveDirection * Mathf.Deg2Rad;
    float waveCos = Mathf.Cos(waveRad);
    float waveSin = Mathf.Sin(waveRad);

    for (int y = 0; y < height; y++)
    {
        float v = (float)y / Mathf.Max(1, height - 1);
        for (int x = 0; x < width; x++)
        {
            float u = (float)x / Mathf.Max(1, width - 1);

            // Deformación por Ondas en UVs
            if (waveIntensity > 0f)
            {
                float proj = u * waveCos + v * waveSin;
                float wave = Mathf.Sin(proj * waveFrequency * Mathf.PI * 2f) * waveIntensity;
                u += Mathf.Cos(waveRad + Mathf.PI * 0.5f) * wave;
                v += Mathf.Sin(waveRad + Mathf.PI * 0.5f) * wave;
            }

            // Aplicar rotación al mapa UV
            float cu = u - 0.5f;
            float cv = v - 0.5f;
            float ru = (cu * cos - cv * sin) + 0.5f;
            float rv = (cu * sin + cv * cos) + 0.5f;

            float t = CalculatePatternFactor(ru, rv);
            Color pixelColor = EvaluateGradient(t);
            targetTexture.SetPixel(x, y, pixelColor);
        }
    }
}

// Métodos públicos para los controles de la UI
public void SetWaveIntensity(float val) { waveIntensity = val; GenerateAndApply(); }
public void SetWaveFrequency(float val) { waveFrequency = val; GenerateAndApply(); }
public void SetWaveDirection(float val) { waveDirection = val; GenerateAndApply(); }
    private void Reset()
    {
        // Puntos de color por defecto
        colorStops.Clear();
        colorStops.Add(new ColorStop(Color.black, 0f));
        colorStops.Add(new ColorStop(Color.white, 1f));
    }

    private void Awake()
    {
        if (colorStops.Count == 0)
        {
            Reset();
        }
    }

    public void GenerateAndApply()
    {
        if (width <= 0 || height <= 0) return;

        // 1. Inicializar/reorganizar textura
        if (targetTexture == null || targetTexture.width != width || targetTexture.height != height)
        {
            targetTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            targetTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // 2. Generar Fondo (Gradiente / Patrón Base)
        GenerateBaseTexturePixels();

        // 3. Dibujar Capas de PNG (del fondo al frente: índice mayor primero, índice 0 al final)
        if (stampLayers != null)
        {
            for (int i = stampLayers.Count - 1; i >= 0; i--)
            {
                DrawStampLayer(stampLayers[i]);
            }
        }

        // 4. Aplicar píxeles y asignar a la vista previa
        targetTexture.Apply();

        if (previewDisplay != null)
        {
            previewDisplay.texture = targetTexture;
        }
    }

    

    private float CalculatePatternFactor(float u, float v)
    {
        switch (patternType)
        {
            case PatternType.Linear:
                return Mathf.Clamp01(u);

            case PatternType.Radial:
                float dist = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                return Mathf.Clamp01(dist);

            case PatternType.Checkerboard:
                int cx = Mathf.FloorToInt(u * 8f);
                int cy = Mathf.FloorToInt(v * 8f);
                return (cx + cy) % 2 == 0 ? 0f : 1f;

            case PatternType.Noise:
                return Mathf.PerlinNoise(u * noiseScale, v * noiseScale);

            default:
                return u;
        }
    }

    private Color EvaluateGradient(float t)
    {
        if (colorStops == null || colorStops.Count == 0) return Color.white;
        if (colorStops.Count == 1) return colorStops[0].color;

        t = Mathf.Clamp01(t);

        if (t <= colorStops[0].position) return colorStops[0].color;
        if (t >= colorStops[colorStops.Count - 1].position) return colorStops[colorStops.Count - 1].color;

        for (int i = 0; i < colorStops.Count - 1; i++)
        {
            if (t >= colorStops[i].position && t <= colorStops[i + 1].position)
            {
                float range = colorStops[i + 1].position - colorStops[i].position;
                float localT = range > 0.0001f ? (t - colorStops[i].position) / range : 0f;

                // Aplicar tipo de interpolación
                switch (interpolationType)
                {
                    case InterpolationType.Smooth:
                        localT = localT * localT * (3f - 2f * localT);
                        break;
                    case InterpolationType.Step:
                        localT = localT >= 0.5f ? 1f : 0f;
                        break;
                }

                return Color.Lerp(colorStops[i].color, colorStops[i + 1].color, localT);
            }
        }

        return colorStops[colorStops.Count - 1].color;
    }

    public void DrawStampLayer(StampLayer layer)
    {
        if (layer == null || layer.texture == null) return;

        var prevState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(layer.seed);

        for (int i = 0; i < layer.count; i++)
        {
            float cx = UnityEngine.Random.Range(0, targetTexture.width);
            float cy = UnityEngine.Random.Range(0, targetTexture.height);

            float currentScale = layer.randomScale ? UnityEngine.Random.Range(layer.minScale, layer.maxScale) : layer.scale;
            float currentRot = layer.randomRotation ? UnityEngine.Random.Range(layer.minRotation, layer.maxRotation) : layer.rotation;

            DrawSingleStamp(layer.texture, cx, cy, currentScale, currentRot);
        }

        UnityEngine.Random.state = prevState;
    }

    private void DrawSingleStamp(Texture2D stampTex, float cx, float cy, float scale, float angleDeg)
    {
        float scaledW = stampTex.width * scale;
        float scaledH = stampTex.height * scale;
        if (scaledW <= 0f || scaledH <= 0f) return;

        float rad = -angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        float radius = Mathf.Sqrt(scaledW * scaledW + scaledH * scaledH) * 0.5f;
        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
        int maxX = Mathf.Min(targetTexture.width - 1, Mathf.CeilToInt(cx + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
        int maxY = Mathf.Min(targetTexture.height - 1, Mathf.CeilToInt(cy + radius));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Matriz de rotación inversa para la textura fuente
                float rx = dx * cos - dy * sin;
                float ry = dx * sin + dy * cos;

                float u = (rx / scaledW) + 0.5f;
                float v = (ry / scaledH) + 0.5f;

                if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                {
                    Color stampColor = stampTex.GetPixelBilinear(u, v);
                    if (stampColor.a > 0f)
                    {
                        Color dest = targetTexture.GetPixel(x, y);
                        Color blended = Color.Lerp(dest, stampColor, stampColor.a);
                        targetTexture.SetPixel(x, y, blended);
                    }
                }
            }
        }
    }

    // --- MÉTODOS PÚBLICOS DE CONFIGURACIÓN (Llamados por AutoUIBuilder) ---
    public void SetWidth(int w) { width = Mathf.Max(16, w); GenerateAndApply(); }
    public void SetHeight(int h) { height = Mathf.Max(16, h); GenerateAndApply(); }
    public void SetPattern(int index) { patternType = (PatternType)index; GenerateAndApply(); }
    public void SetInterpolation(int index) { interpolationType = (InterpolationType)index; GenerateAndApply(); }
    public void SetRotation(float angle) { rotationAngle = angle; GenerateAndApply(); }
    public void SetNoiseScale(float scale) { noiseScale = scale; GenerateAndApply(); }

    public void AddColorStop(Color color, float pos)
    {
        colorStops.Add(new ColorStop(color, pos));
        colorStops.Sort((a, b) => a.position.CompareTo(b.position));
        GenerateAndApply();
    }

    public void UpdateColorStopColor(int index, Color color)
    {
        if (index >= 0 && index < colorStops.Count)
        {
            colorStops[index].color = color;
            GenerateAndApply();
        }
    }

    public void UpdateColorStopPosition(int index, float pos)
    {
        if (index >= 0 && index < colorStops.Count)
        {
            colorStops[index].position = Mathf.Clamp01(pos);
            colorStops.Sort((a, b) => a.position.CompareTo(b.position));
            GenerateAndApply();
        }
    }

    public void RemoveColorStop(int index)
    {
        if (index >= 0 && index < colorStops.Count && colorStops.Count > 1)
        {
            colorStops.RemoveAt(index);
            GenerateAndApply();
        }
    }

    // --- MÉTODOS DE COMPATIBILIDAD PARA LLAMADAS DIRECTAS A TEXTUREGENERATOR ---
    public StampLayer GetPrimaryLayer()
    {
        return (stampLayers != null && stampLayers.Count > 0) ? stampLayers[0] : null;
    }

    public int pngCount
    {
        get => GetPrimaryLayer() != null ? GetPrimaryLayer().count : 0;
        set { if (GetPrimaryLayer() != null) { GetPrimaryLayer().count = value; GenerateAndApply(); } }
    }

    public int seed
    {
        get => GetPrimaryLayer() != null ? GetPrimaryLayer().seed : 0;
        set { if (GetPrimaryLayer() != null) { GetPrimaryLayer().seed = value; GenerateAndApply(); } }
    }

    public void SaveProjectTxtr(string path)
    {
        var mgr = GetComponent<UIManager>();
        if (mgr != null) mgr.SaveProjectTxtr(path);
    }

    public void LoadProjectTxtr(string path)
    {
        var mgr = GetComponent<UIManager>();
        if (mgr != null) mgr.LoadProjectTxtr(path);
    }

    public void ExportTexture(string path, string format)
    {
        var mgr = GetComponent<UIManager>();
        if (mgr != null) mgr.ExportTexture(path, format);
    }
}