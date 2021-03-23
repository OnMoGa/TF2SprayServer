using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Security.Permissions;
using SprayServerCommon;
using Microsoft.Win32;

namespace Client {
	class Program {
		static void Main(string[] args) {

			NameValueCollection config = ConfigurationManager.AppSettings;
			Uri serverUri = null;
			try {
				serverUri = new Uri(config["Server"]);
			} catch (UriFormatException e) {
				Tools.logError($"Invalid Config Value: Server Uri");
				Environment.Exit(3);
			}

			SteamHelper steamHelper = new SteamHelper();
			DirectoryInfo saveDirectory = new DirectoryInfo(Path.Combine(steamHelper.getAppInstallDirectory(440).FullName, "tf", "materials", "temp"));
			
			SyncEngine syncEngine = new SyncEngine(saveDirectory, serverUri);
			syncEngine.start();

		}
	}
}
