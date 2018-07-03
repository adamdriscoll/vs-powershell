using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace VisualStudio.PowerShell
{
	[ContentType("PowerShell")]
	[Export(typeof(ILanguageClient))]
	public class PowerShellLanguageClient : ILanguageClient
	{
		public string Name => "PowerShell Language Extension";

		public IEnumerable<string> ConfigurationSections => null;

		public object InitializationOptions => null;

		public IEnumerable<string> FilesToWatch => null;

		public event AsyncEventHandler<EventArgs> StartAsync;
		public event AsyncEventHandler<EventArgs> StopAsync;

		public async Task<Connection> ActivateAsync(CancellationToken token)
		{
			await Task.Yield();
            var assemblyDirectoryInfo = new FileInfo(GetType().Assembly.Location).Directory;

            var extensionDirectory = Path.Combine(assemblyDirectoryInfo.FullName, "PowerShellEditorServices");
			var script = Path.Combine(extensionDirectory, "Start-EditorServices.ps1");

            var stateDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%"), "PowerShellEditorServices");

            if (!Directory.Exists(Path.Combine(stateDirectory, "sessions"))) {
                Directory.CreateDirectory(Path.Combine(stateDirectory, "sessions"));
            }

            if (!Directory.Exists(Path.Combine(stateDirectory, "logs")))
            {
                Directory.CreateDirectory(Path.Combine(stateDirectory, "logs"));
            }

            var info = new ProcessStartInfo();
            var sessionFile = $@"{stateDirectory}\sessions\PSES-VS-{Guid.NewGuid()}";
            info.FileName = @"C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe";
			info.Arguments = $@"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "" & '{script}' -HostName 'Visual Studio Code Host' -HostProfileId 'Microsoft.VSCode' -HostVersion '1.7.0' -AdditionalModules @() -BundledModulesPath '{assemblyDirectoryInfo.FullName}' -EnableConsoleRepl -LogLevel 'diagnostic' -LogPath '{stateDirectory}\logs\VSEditorServices.log' -SessionDetailsPath '{sessionFile}' -FeatureFlags @() -Verbose """;
			info.RedirectStandardInput = true;
			info.RedirectStandardOutput = true;
			info.UseShellExecute = false;
			info.CreateNoWindow = false;

			var process = new Process();
			process.StartInfo = info;
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

			if (process.Start())
			{
                while (!File.Exists(sessionFile))
                {
                    Thread.Sleep(50);

                    if (process.HasExited)
                    {
                        return null;
                    }
                }

                Thread.Sleep(50);

                var sessionInfo = File.ReadAllText(sessionFile);

                var sessionInfoJObject = JsonConvert.DeserializeObject<JObject>(sessionInfo);

				var languageServicePort = (int)sessionInfoJObject["languageServicePort"];

                int retry = 0;

                while(retry < 3)
                {
                    try
                    {
                        TcpClient tcpClient = new TcpClient("localhost", languageServicePort);
                        NetworkStream ns = tcpClient.GetStream();

                        return new Connection(ns, ns);
                    }
                    catch
                    {
                        retry++;
                        Thread.Sleep(1000);
                    }
                }

                return null;
			}

			return null;
		}

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        public async Task OnLoadedAsync()
		{
			await StartAsync?.InvokeAsync(this, EventArgs.Empty);
		}

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }
    }
}
