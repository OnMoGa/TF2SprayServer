using Newtonsoft.Json.Linq;
using SprayServerCommon;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HttpMultipartParser;

namespace Server {
	class RequestListener {

		
		private Page routeTreeBase;

		private HttpListener listener = new HttpListener();

		private SprayManager sprayManager = new SprayManager(new DirectoryInfo("Sprays"));
		private NameValueCollection config;

		public class Page {
			public Dictionary<(HttpMethod method, string component), Page> children { get; set; } = new Dictionary<(HttpMethod, string), Page>();
			public Action<HttpListenerRequest, HttpListenerResponse> action { get; set; } = (request, response) => {
				response.StatusCode = (int)HttpStatusCode.NotFound;
				sendResponse(response, "Invalid URL");
			};
		}

		public RequestListener(NameValueCollection config) {
			this.config=config;
		
			routeTreeBase = new Page {
				children = new Dictionary<(HttpMethod, string), Page>() {
					{
						(HttpMethod.Get, "Hashes"),
						new Page {
							action = processSprayHashesRequest
						}
					},
					{
						(HttpMethod.Get, "CollectionHash"),
						new Page {
							action = processSprayCollectionHashRequest
						}
					},
					{
						(HttpMethod.Get, @"Spray"),
						new Page {
							children = new Dictionary<(HttpMethod method, string component), Page>() {
								{
									(HttpMethod.Get, @"\w+\.vtf"),
									new Page {
										action = processSprayGetRequest
									}
								}
							}
						}
					},
					{
						(HttpMethod.Get, "Version"),
						new Page {
							action = processVersionRequest
						}
					},
					{
						(HttpMethod.Get, "Config"),
						new Page {
							action = processConfigRequest
						}
					},
					{
						(HttpMethod.Put, "Spray"),
						new Page {
							action = processSprayPutRequest
						}
					},
				}
			};
		}

		

		public void startListening(int port) {
			listener.Prefixes.Add($"http://*:{port}/");

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

				new Thread(() => {
					handleRequest(request, response);
				}).Start();

			}
		}


		private void handleRequest(HttpListenerRequest request, HttpListenerResponse response) {
			List<string> route = request.RawUrl.Split('/').Where(s => s != string.Empty).ToList();
			Page page = getPage(new HttpMethod(request.HttpMethod), route);
			page.action(request, response);
		}

		private Page getPage(HttpMethod method, IEnumerable<string> route, Page page = null) {
			page ??= routeTreeBase;
			if (!route.Any()) return page;
			string nextPathComponent = route.First();
			Page nextPage = page.children.FirstOrDefault(c => 
				c.Key.method == method &&
				Regex.IsMatch(nextPathComponent, c.Key.component, RegexOptions.IgnoreCase)
			).Value;
			return getPage(method, route.Skip(1), nextPage);
		}




		private void processSprayHashesRequest(HttpListenerRequest request, HttpListenerResponse response) {
			Tools.log($"Processing spray hashes request");
			response.StatusCode = (int)HttpStatusCode.OK;
			sendResponse(response,
				new JObject {
					{"hashes", JArray.FromObject(sprayManager.sprays.Select(s => s.name))}
				}
			);
		}

		private void processSprayCollectionHashRequest(HttpListenerRequest request, HttpListenerResponse response) {
			Tools.log($"Processing spray collection hash request");
			response.StatusCode = (int)HttpStatusCode.OK;
			sendResponse(response,
				new JObject {
					{"hash", sprayManager.sprays.sprayCollectionHash()}
				}
			);
		}

		private void processSprayGetRequest(HttpListenerRequest request, HttpListenerResponse response) {
			Tools.log($"Processing spray get request");

			List<string> route = request.RawUrl.Split('/').Where(s => s != string.Empty).ToList();
			var fileName = route[1];
			Spray spray = sprayManager.sprays.FirstOrDefault(s => s.name == fileName);
					
			response.StatusCode = (int)HttpStatusCode.OK;
			sendSpray(response, spray);
		}

		private void processSprayPutRequest(HttpListenerRequest request, HttpListenerResponse response) {
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
				foreach (FilePart filePart in parser.Files) {
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
		}
		private void processVersionRequest(HttpListenerRequest request, HttpListenerResponse response) {
			Tools.log($"Processing version request");
			response.StatusCode = (int)HttpStatusCode.OK;
			sendResponse(response,
				new JObject {
					{"Version", Program.Version}
				}
			);
		}

		private void processConfigRequest(HttpListenerRequest request, HttpListenerResponse response) {
			Tools.log($"Processing config request");
			response.StatusCode = (int)HttpStatusCode.OK;
			sendResponse(response,
				new JObject {
					{"MinimumRefreshMilliseconds", int.Parse(config["MinimumRefreshMilliseconds"])}
				}
			);
		}





		private static void sendResponse(HttpListenerResponse response, JObject responseJObject) {
			sendResponse(response, responseJObject.ToString());
		}
		private static void sendResponse(HttpListenerResponse response, string responseString) {
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
		private static void sendSpray(HttpListenerResponse response, Spray spray) {
			try {
				byte[] bytes = spray.data;
				string filename = spray.name;

				response.ContentLength64 = bytes.Length;
				response.SendChunked = false;
				response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
				response.AddHeader("Content-disposition", "attachment; filename=" + filename);

				response.OutputStream.Write(bytes);
				response.OutputStream.Close();
			} catch (Exception e) {
				Tools.logError($"Error sending spray: {e.Message}\n{e.StackTrace}");
			}
		}

	}
}
