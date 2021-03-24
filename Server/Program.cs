using System;
using System.Collections.Specialized;
using System.Configuration;
using SprayServerCommon;

namespace Server {
	class Program {
		
		public const int Version = 1;
		static void Main(string[] args) {

			NameValueCollection config = ConfigurationManager.AppSettings;

			int port = 0;
			try {
				port = int.Parse(config["Port"]);
			} catch (UriFormatException e) {
				Tools.logError($"Invalid Config Value: Port");
				Environment.Exit(1);
			}


			RequestListener requestListener = new RequestListener(config);
			try {
				requestListener.startListening(port);
			} catch (Exception e) {
				Tools.logError($"{e.Message} {e.StackTrace}");
			}
			

		}
	}
}
