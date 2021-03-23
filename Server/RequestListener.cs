using Newtonsoft.Json.Linq;
using SprayServerCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using HttpMultipartParser;

namespace Server {
	class RequestListener {

		
		private HttpListener listener = new HttpListener();

		private SprayManager sprayManager = new SprayManager(new DirectoryInfo("Sprays"));

		public void startListening() {
			listener.Prefixes.Add("http://*:55000/");

			Tools.log("Starting listeners on: ");
			foreach(string url in listener.Prefixes) {
				Tools.log("\t" + url);
			}

			listener.Start();

			listenLoop();
		}


		private void listenLoop() {
			Tools.log("Listening...");
			while(true) {
				//waits for a request
				HttpListenerContext context = listener.GetContext();
				HttpListenerRequest request = context.Request;
				HttpListenerResponse response = context.Response;
				bool sentResponse = false;

				List<string> pathElements = request.RawUrl.Split('/').Where(s => s != string.Empty).ToList();
				
				Tools.log($"{request.HttpMethod} request received for: /{string.Join("/", pathElements)}");

				handleRequest(request, pathElements, response);
			}
		}




		


		public void handleRequest(HttpListenerRequest request, List<string> route, HttpListenerResponse response) {


			if (request.HttpMethod == HttpMethod.Get.ToString()) {
				if (route[0].Equals("Hashes", StringComparison.InvariantCultureIgnoreCase)) {
					Tools.log($"Processing spray hashes request");
					response.StatusCode = (int)HttpStatusCode.OK;
					sendResponse(response,
						new JObject {
							{"hashes", JArray.FromObject(sprayManager.sprays.Select(s => s.name))}
						}
					);
					return;
				} else if (route[0].Equals("CollectionHash", StringComparison.InvariantCultureIgnoreCase)) {
					Tools.log($"Processing spray collection hash request");
					response.StatusCode = (int)HttpStatusCode.OK;
					sendResponse(response,
						new JObject {
							{"hash", sprayManager.sprays.sprayCollectionHash()}
						}
					);
					return;
				} else if (route[0].Equals("Spray", StringComparison.InvariantCultureIgnoreCase)) {

					Tools.log($"Processing spray get request");
					var fileName = route[1];
					Spray spray = sprayManager.sprays.FirstOrDefault(s => s.name == fileName);
					
					response.StatusCode = (int)HttpStatusCode.OK;
					sendSpray(response, spray);
					return;
				}
			} else if (request.HttpMethod == HttpMethod.Put.ToString()) {
				if (route[0].Equals("Spray", StringComparison.InvariantCultureIgnoreCase)) {
					Tools.log($"Processing spray put request");

					if (!request.HasEntityBody) {
						response.StatusCode = (int)HttpStatusCode.BadRequest;
						sendResponse(response,
							new JObject {
								{"error", "No body"}
							}
						);
						return;
					}

					using (Stream stream = request.InputStream)
					{
						var parser = MultipartFormDataParser.Parse(stream);
						var x = parser.Files;
						foreach (FilePart filePart in x) {
							MemoryStream memoryStream = new MemoryStream();
							filePart.Data.CopyTo(memoryStream);
							byte[] bytes = memoryStream.ToArray();
							sprayManager.addSpray(filePart.FileName, bytes);
						}

					}

					response.StatusCode = (int)HttpStatusCode.OK;
					sendResponse(response,
						new JObject()
					);
					return;
				}
			}
			
			response.StatusCode = (int)HttpStatusCode.NotFound;
			sendResponse(response, "Invalid Url");
			return;
		}









		private void sendResponse(HttpListenerResponse response, JObject responseJObject) {
			sendResponse(response, responseJObject.ToString());
		}

		private void sendResponse(HttpListenerResponse response, String responseString) {
			try {
				byte[] buffer = Encoding.UTF8.GetBytes(responseString);
				response.ContentLength64 = buffer.Length;
				Stream output = response.OutputStream;
				output.Write(buffer, 0, buffer.Length);
				output.Close();
			} catch (Exception e) {
				Tools.logError($"Error sending response: {e.Message}\n{e.StackTrace}");
			}
			
		}

		void sendSpray(HttpListenerResponse response, Spray spray) {

			byte[] bytes = spray.data;
			string filename = spray.name;

			response.ContentLength64 = bytes.Length;
			response.SendChunked = false;
			response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
			response.AddHeader("Content-disposition", "attachment; filename=" + filename);

			response.OutputStream.Write(bytes);

			response.OutputStream.Close();
			
		}

	}
}
