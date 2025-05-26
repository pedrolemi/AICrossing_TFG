using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace LLM
{
    namespace Llamafile
    {
        public class LlamafileModel : LlamaModelComp
        {
            [Tooltip("Numero de hilos que usar durante la generacion.\n" +
                "-1 = todos.")]
            [SerializeField]
            private int nThreads = -1;
            [SerializeField]
            private int port = Port.GENERATION_DEFAULT;
            [SerializeField]
            private bool showWindow = true;

            private Process llamafileProcess;

            protected override void Start()
            {
                base.Start();

                llamafileProcess = null;
                if (LlamafileLibrary.LibraryExists())
                {
                    string args = GetServerArgs();
                    llamafileProcess = LlamafileLibrary.CreateProcess(args, showWindow, out string command);
                    UnityEngine.Debug.Log($"Deploy llamafile server command: {command}.");
                    llamafileProcess.Start();
                }
                else
                {
                    UnityEngine.Debug.LogError($"Llamafile library doesn't exist.");
                    Destroy(this);
                }
            }

            private string GetServerArgs()
            {
                List<string> argsList = new List<string>()
                {
                    $"-m {modelPath}",
                    $"-c {contextSize}",
                    $"-ngl {nGPULayers}",
                    $"-b {batchSize}",
                    $"--host {Host.DEFAULT}",
                    $"--port {port}",
                    $"--server",
                    $"--nobrowser",
                    $"--log-disable"
                };

                if (nThreads > 0)
                {
                    argsList.Add($"-t {nThreads}");
                }

                string args = string.Join(" ", argsList);
                return args;
            }

            public void Load(string modelPath, ChatTemplateType fallbackChatTemplate, bool showWindow)
            {
                base.Load(modelPath, fallbackChatTemplate);
                this.showWindow = showWindow;
            }

            private void OnDestroy()
            {
                if (llamafileProcess != null && !llamafileProcess.HasExited)
                {
                    // Si el proceso tiene una ventana, se trata de cerrar la venta, que a su vez acabara con el proceso
                    if (showWindow)
                    {
                        llamafileProcess.CloseMainWindow();
                    }
                    // Si no tiene ventana, se mata el proceso
                    else
                    {
                        llamafileProcess.Kill();
                    }
                }
            }
        }
    }
}