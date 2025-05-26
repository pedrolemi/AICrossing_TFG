using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLM
{
    namespace LlamaCpp
    {
        public class LlamaCppLibrary : LlamaLibrary
        {
            private static string subfolderName = "LlamaCpp";
            private static string libraryVersion = "b4800";

            private static string libraryReleaseUrl = GetLibraryReleaseUrl(libraryVersion);

            private static string cudaLibName = "cudart-llama-bin-win-cu11.7-x64.zip";
            private static string cudaLibUrl = GetLibraryUrl(libraryReleaseUrl, cudaLibName);

            private static string llamaLibName = "llama-b4800-bin-win-cuda-cu11.7-x64.zip";
            private static string llamaLibUrl = GetLibraryUrl(libraryReleaseUrl, llamaLibName);

            public const string GGML = "ggml";
            public const string GGML_BASE = "ggml-base";
            public const string GGML_CPU = "ggml-cpu";
            public const string GGML_CUDA = "ggml-cuda";
            public const string GGML_RPC = "ggml-rpc";
            public const string LLAMA = "llama";
            public const string CUBLAS64_11 = "cublas64_11";
            public const string CUBLASLT64_11 = "cublasLt64_11";
            public const string CUDART64_110 = "cudart64_110";

            private static HashSet<string> filesKeep = new HashSet<string>
            {
                AddDllExtension(GGML),
                AddDllExtension(GGML_BASE),
                AddDllExtension(GGML_CPU),
                AddDllExtension(GGML_CUDA),
                AddDllExtension(GGML_RPC),
                AddDllExtension(LLAMA),
                AddDllExtension(CUBLAS64_11),
                AddDllExtension(CUBLASLT64_11),
                AddDllExtension(CUDART64_110)
            };

            private static string AddDllExtension(string file)
            {
                return $"{file}.dll";
            }

            private static string GetLibraryReleaseUrl(string libraryVersion)
            {
                return $"https://github.com/ggml-org/llama.cpp/releases/download/{libraryVersion}";
            }

            private static string GetLibraryPath(string libraryName)
            {
                return Path.Combine(Application.dataPath, "Plugins", $"{subfolderName}-{libraryVersion}", libraryName);
            }

            private static bool LibraryExists()
            {
                return Directory.Exists(GetLibraryPath(""));
            }

            private static async Task<bool> DownloadLibraryAndUnzip(string libraryUrl, string libraryName)
            {
                string temporaryPath = Path.Combine(Application.temporaryCachePath, libraryName);
                bool downloaded = await DownloadLibrary(libraryUrl, temporaryPath, libraryName);
                if (downloaded)
                {
                    string directoryPath = GetLibraryPath("");
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    using (ZipArchive archive = ZipFile.OpenRead(temporaryPath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (filesKeep.Contains(entry.Name))
                            {
                                string destinationPath = Path.Combine(directoryPath, entry.FullName);
                                entry.ExtractToFile(destinationPath, true);
                                Debug.Log($"{entry.FullName} extracted in {directoryPath}.");
                            }
                        }
                    }
                    File.Delete(temporaryPath);
                }
                return downloaded;
            }

#if UNITY_EDITOR
            [InitializeOnLoadMethod]
            private async static void InitializeOnLoad()
            {
                if (!LibraryExists())
                {
                    bool llamaDownloaded = await DownloadLibraryAndUnzip(llamaLibUrl, llamaLibName);
                    bool cudaDownloaded = await DownloadLibraryAndUnzip(cudaLibUrl, cudaLibName);
                    if (llamaDownloaded || cudaDownloaded)
                    {
                        AssetDatabase.Refresh();
                    }
                }
            }
#endif
        }
    }
}