using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LLM
{
    namespace Llamafile
    {
        // Clase que descarga el servidor llamafile y devuelve un proceso apra ejecutarlo
        public class LlamafileLibrary : LlamaLibrary
        {
            private static string libraryVersion = "0.9.0";

            private static string libraryName = GetLibraryName(libraryVersion);

            private static string libraryReleaseUrl = GetLibraryReleaseUrl(libraryVersion);
            private static string libraryUrl = GetLibraryUrl(libraryReleaseUrl, libraryName);

            private static string libraryPath = AddLibraryExtension(GetLibraryPath(libraryName));

            private static string GetLibraryReleaseUrl(string libraryVersion)
            {
                return $"https://github.com/Mozilla-Ocho/llamafile/releases/download/{libraryVersion}";
            }

            private static string GetLibraryName(string libraryVersion)
            {
                return $"llamafile-{libraryVersion}";
            }

            // Como se trata de un servidor, hay que guardar en StreamingAssets, para que Unity no lo comprima en la build
            private static string GetLibraryPath(string libraryName)
            {
                return Path.Combine(Application.streamingAssetsPath, libraryName);
            }

            private static string AddLibraryExtension(string path)
            {
                return $"{path}.exe";
            }

            public static bool LibraryExists()
            {
                return File.Exists(libraryPath);
            }

#if UNITY_EDITOR
            // Se ha realizado de esta manera en vez de subirla a github, porque pesa mucho
            [InitializeOnLoadMethod]
            private async static void InitializeOnLoad()
            {
                // Si la libreria no existe, se trata de descargar
                if (!LibraryExists())
                {
                    // Se descarga en una ruta temporal
                    string temporaryPath = Path.Combine(Application.temporaryCachePath, libraryName);
                    bool downloaded = await DownloadLibrary(libraryUrl, temporaryPath, libraryName);
                    if (downloaded)
                    {
                        string directoryPath = Path.GetDirectoryName(libraryPath);
                        // Se crea el directorio si no existe
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        UnityEngine.Debug.Log($"{libraryName} moved to {directoryPath}.");
                        // Se mueve a dicho directorio
                        File.Move(temporaryPath, libraryPath);
                        AssetDatabase.Refresh();
                    }
                }
            }
#endif

            public static Process CreateProcess(string args, bool showWindow, out string command)
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    FileName = libraryPath,
                    Arguments = args,
                    CreateNoWindow = true,
                };

                if (!showWindow)
                {
                    processStartInfo.CreateNoWindow = true;
                    processStartInfo.UseShellExecute = false;
                }

                command = $"{libraryName} {args}";

                Process process = new Process();
                process.StartInfo = processStartInfo;

                return process;
            }
        }
    }
}
