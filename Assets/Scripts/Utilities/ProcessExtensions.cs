using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Utilities
{
    // https://stackoverflow.com/questions/470256/process-waitforexit-asynchronously
    public static class ProcessExtensions
    {
        public static async Task<int> WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            // Se crea una tarea, que se puede usar para llevar a cabo una operacion asincrona y controlar cuando termina
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            void ProcessExited(object sender, EventArgs e)
            {
                // Cuando el proceso termina, se establece el resultado de la tarea y,
                // por lo tanto, se termina de esperar
                tcs.TrySetResult(process.ExitCode);
            }

            try
            {
                // Se habilita que el proceso lance eventos (por ejemplo, cuando se cierra)
                process.EnableRaisingEvents = true;
            }
            catch (InvalidOperationException) when (process.HasExited)
            {
                // Se produce este error cuando se trata de habilitar eventos en un proceso ya finalizado
                // Simplemente hay que ignorarlo
            }

            Action register = () =>
            {
                tcs.TrySetCanceled();
            };

            // Se registra la cancelacion del token, para indicar que la tarea ha sido cancelado y,
            // por lo tanto, la espera ha termiando
            using (cancellationToken.Register(register))
            {
                process.Exited += ProcessExited;

                try
                {
                    if (process.HasExited)
                    {
                        tcs.TrySetResult(process.ExitCode);
                    }

                    // Cuando se espera a una tarea, normalmente el codigo que viene
                    // a continuacion del await se ejecuta en el mismo hilo
                    // Se puede deshabilitar dicho comportamiento para mejorar el rendimiento y evitar bloqueos en la UI
                    return await tcs.Task.ConfigureAwait(false);
                }
                finally
                {
                    process.Exited -= ProcessExited;
                }
            }
        }
    }
}
