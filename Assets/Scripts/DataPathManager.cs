using System.IO;
using UnityEngine;

public class DataPathManager : MonoBehaviour
{
    public void GuardarArchivo(string nombreArchivo, string contenido)
    {
        // 1. Definir la ruta de la carpeta de forma segura según el SO
        string carpetaDestino = Path.Combine(Application.persistentDataPath, "MisCarpetas");

        // 2. Verificar y crear la carpeta si no existe en el disco duro
        if (!Directory.Exists(carpetaDestino))
        {
            Directory.CreateDirectory(carpetaDestino);
        }

        // 3. Combinar la carpeta con el nombre del archivo
        string rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

        // 4. Escribir o leer el archivo de forma segura
        File.WriteAllText(rutaCompleta, contenido);

        Debug.Log("Archivo guardado con éxito en: " + rutaCompleta);
    }
}