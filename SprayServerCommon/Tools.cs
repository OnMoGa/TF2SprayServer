using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SprayServerCommon {
	public class Tools {

		static string path = @"log.txt";


		public static void log(string message) {
			Console.WriteLine("{0} - {1}", DateTime.Now, message);
			File.AppendAllText(path, DateTime.Now + " - " + message + Environment.NewLine);
		}


		public static void logError(string message) {
			Console.WriteLine("\u001b[31m{0} - ERROR: {1} \u001b[0m", DateTime.Now, message);
			File.AppendAllText(path, DateTime.Now + " - ERROR: " + message + Environment.NewLine);
		}




		


	}
}
