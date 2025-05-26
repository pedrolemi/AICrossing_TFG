using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LLM
{
    // Clase que permite descargar una libreria
    public class LlamaLibrary
    {
        protected static string GetLibraryUrl(string libraryReleaseUrl, string libraryName)
        {
            return $"{libraryReleaseUrl}/{libraryName}";
        }

        protected static async Task<bool> DownloadLibrary(string libraryUrl, string savePath, string libraryName)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(libraryUrl))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                var asyncOperation = webRequest.SendWebRequest();

                Debug.Log($"Starting to download {libraryName}.");

                while (!asyncOperation.isDone)
                {
                    await Task.Yield();
                    Debug.Log($"{libraryName} download progress: {webRequest.downloadProgress}.");
                }

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(savePath, webRequest.downloadHandler.data);
                    Debug.Log($"{libraryName} fully downloaded in {savePath}.");
                    return true;
                }
                else
                {
                    Debug.LogError($"Failed to download {libraryName} due to the following error: {webRequest.error}.");
                    return false;
                }
            }
        }
    }
}