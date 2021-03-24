using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using SprayServerCommon;

namespace Client {
	class Program {

		public const int Version = 1;


		static void Main(string[] args) {

			NameValueCollection config = ConfigurationManager.AppSettings;
			Uri serverUri = null;
			try {
				serverUri = new Uri(config["Server"]);
			} catch (UriFormatException e) {
				Tools.logError($"Invalid Config Value: Server Uri");
				Environment.Exit(1);
			}

			int refreshMilliseconds = 0;
			try {
				refreshMilliseconds = int.Parse(config["MinimumRefreshMilliseconds"]);
			} catch (FormatException e) {
				Tools.logError($"Invalid Config Value: MinimumRefreshMilliseconds");
				Environment.Exit(2);
			}

			SteamHelper steamHelper = new SteamHelper();
			DirectoryInfo saveDirectory = new DirectoryInfo(Path.Combine(steamHelper.getAppInstallDirectory(440).FullName, "tf", "materials", "temp"));

			try {
				SyncEngine syncEngine = new SyncEngine(saveDirectory, serverUri, refreshMilliseconds);
				syncEngine.start();
			} catch (Exception e) {
				Tools.logError($"{e.Message} {e.StackTrace}");
				Console.ReadLine();
			}
			

		}
	}
}
