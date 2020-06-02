using CodeFirstWebFramework;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RepairCafe {
	class Program {
		public static WebServer Server;
		static void Main(string[] args) {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			// Load standard config file
			Config.Load(args);
			switch (Environment.OSVersion.Platform) {
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
					// On Windows, for testing, auto-launch a browser pointing at our web app by default
					if (Config.CommandLineFlags["nolaunch"] == null) {
						string url = "http://" + Config.Default.DefaultServer.ServerName + ":" + Config.Default.Port + "/";
						if (Config.CommandLineFlags["url"] != null)
							url += Config.CommandLineFlags["url"];
						if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
							Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}"));
						} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
							Process.Start("xdg-open", "'" + url + "'");
						} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
							Process.Start("open", "'" + url + "'");
						}
					}
					break;
			}
			// Create WebServer - will connect to and upgrade Database if required
			Server = new WebServer();
			// Run the web server
			Server.Start();
		}
	}

}
