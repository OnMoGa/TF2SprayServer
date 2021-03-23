using System;
using System.Configuration;
using System.IO;
using SprayServerCommon;

namespace Client {
	class Program {
		static void Main(string[] args) {

			var config = ConfigurationManager.AppSettings;

			DirectoryInfo saveDirectory = null;
			try {
				saveDirectory = new DirectoryInfo(config["SprayLocation"]);
				if (!saveDirectory.Exists) {
					Tools.logError($"Invalid Config Value: Spray Location doesn't exist");
					Environment.Exit(1);
				}
			} catch (ArgumentException e) {
				Tools.logError($"Invalid Config Value: Spray Location");
				Environment.Exit(2);
			}

			Uri serverUri = null;
			try {
				serverUri = new Uri(config["Server"]);
			} catch (UriFormatException e) {
				Tools.logError($"Invalid Config Value: Server Uri");
				Environment.Exit(3);
			}


			SyncEngine syncEngine = new SyncEngine(saveDirectory, serverUri);
			syncEngine.start();

		}
	}
}
