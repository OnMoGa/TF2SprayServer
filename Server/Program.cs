using System;
using SprayServerCommon;

namespace Server {
	class Program {
		static void Main(string[] args) {
			

			RequestListener requestListener = new RequestListener();
			try {
				requestListener.startListening();
			} catch (Exception e) {
				Tools.logError($"{e.Message} {e.StackTrace}");
			}
			

		}
	}
}
