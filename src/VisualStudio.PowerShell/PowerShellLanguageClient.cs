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
using System.Reflection;
using System.Text;
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

			var vscodeDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vscode", "extensions");
			if (!Directory.Exists(vscodeDirectory))
			{
				throw new Exception("PowerShell VS Code extension required.");
			}

			var extensionDirectory = Directory.GetDirectories(vscodeDirectory).FirstOrDefault(m => m.Contains("ms-vscode.powershell"));

			var script = Path.Combine(extensionDirectory, "scripts", "Start-EditorServices.ps1");

			ProcessStartInfo info = new ProcessStartInfo();
			info.FileName = @"C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe";
			info.Arguments = $@"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "" & '{script}' -EditorServicesVersion '1.5.1' -HostName 'Visual Studio Code Host' -HostProfileId 'Microsoft.VSCode' -HostVersion '1.5.1' -AdditionalModules @('PowerShellEditorServices.VSCode') -BundledModulesPath '{extensionDirectory}\modules' -EnableConsoleRepl -LogLevel 'verbose' -LogPath '{extensionDirectory}\logs\VSEditorServices.log' -SessionDetailsPath '{extensionDirectory}\sessions\PSES-VS' -FeatureFlags @()""";
			info.RedirectStandardInput = true;
			info.RedirectStandardOutput = true;
			info.UseShellExecute = false;
			info.CreateNoWindow = true;

			Process process = new Process();
			process.StartInfo = info;

			if (process.Start())
			{
				//Wait for startup....
				Thread.Sleep(5000);

				var sessionInfo = File.ReadAllText($@"{extensionDirectory}\sessions\PSES-VS");

				var sessionInfoJObject = JsonConvert.DeserializeObject<JObject>(sessionInfo);

				var languageServicePort = (int)sessionInfoJObject["languageServicePort"];
				TcpClient tcpClient = new TcpClient("localhost", languageServicePort);
				NetworkStream ns = tcpClient.GetStream();
				
				return new Connection(ns, ns);
			}

			return null;
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
