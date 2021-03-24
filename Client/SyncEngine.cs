using System;
using SprayServerCommon;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Client {
	class SyncEngine {


		private int refreshMilliseconds;
		private HttpClient client = new HttpClient();
		private SprayManager sprayManager;

		private Uri baseUri;

		public SyncEngine(DirectoryInfo sprayCache, Uri uri, int refreshMilliseconds) {
			this.refreshMilliseconds = Math.Max(refreshMilliseconds, 5000);
			this.baseUri = uri;
			sprayManager = new SprayManager(sprayCache);
		}


		public void start() {

			int serverVersion = getServerVersion();
			Tools.log($"Client Version: {Program.Version}. Server Version: {serverVersion}.");
			if (serverVersion > Program.Version) {
				Tools.logError($"Outdated client version. Got v{Program.Version}, need v{serverVersion}. Please get the updated version from here https://github.com/OnMoGa/TF2SprayServer/releases");
				Console.ReadLine();
				Environment.Exit(3);
			}


			var serverConfig = getServerConfig();
			Tools.log("Got server config");
			int serverMinRefreshMillis = serverConfig.Value<int>("MinimumRefreshMilliseconds");

			while (true) {
				sprayManager.reloadSprays();

				if (sprayManager.sprays.sprayCollectionHash() != getServerSprayHash()) {
					Tools.log($"Discrepancy detected, getting spray list");
					List<string> sprayNameList = getServerSprayList();

					var toDownload = sprayNameList.Except(sprayManager.sprays.Select(ls => ls.name)).ToList();
					var toUpload = sprayManager.sprays.Where(ls => !sprayNameList.Contains(ls.name)).ToList();

					if (toDownload.Any()) {
						Tools.log($"Downloading {toDownload.Count} sprays");
						toDownload.ForEach(downloadSpray);
					}

					if (toUpload.Any()) {
						Tools.log($"Uploading {toUpload.Count} sprays");
						toUpload.ForEach(uploadSpray);
					}

				}
				
				Thread.Sleep(Math.Max(refreshMilliseconds, serverMinRefreshMillis));
			}
		}



		private HttpResponseMessage makeApiCall(HttpMethod method, string path, HttpContent content = null) {
			HttpResponseMessage response = null;

			Uri uri = new Uri($"{baseUri}/{path}");

			while (true) {
				try {
					if (method == HttpMethod.Get) {
						response = client.GetAsync(uri).Result;
					} else if (method == HttpMethod.Post) {
						response = client.PostAsync(uri, content).Result;
					} else if (method == HttpMethod.Put) {
						response = client.PutAsync(uri, content).Result;
					}

					if (!response.IsSuccessStatusCode) {
						Tools.logError($"Error making api call. Retrying...");
						Thread.Sleep(2000);
						continue;
					}
					return response;
				} catch (Exception e) {
					Tools.logError($"Error contacting server. Retrying...");
					Thread.Sleep(2000);
				}
			}
		}


		private int getServerSprayHash() {
			var response = makeApiCall(HttpMethod.Get, "CollectionHash");

			var responseString = response.Content.ReadAsStringAsync().Result;
			if (response.IsSuccessStatusCode) {
				JObject responseObject = JObject.Parse(responseString);
				var hash = responseObject.Value<int>("hash");
				return hash;
			}

			Tools.logError("Failed to retrieve spray collection hash");
			return 0;
		}


		private void downloadSpray(string fileName) {
			Tools.log($"Downloading {fileName}");

			var response = makeApiCall(HttpMethod.Get, $"Spray/{fileName}");

			if (response.IsSuccessStatusCode) {
				var bytes = response.Content.ReadAsByteArrayAsync().Result;
				sprayManager.addSpray(fileName, bytes);
				return;
			}
			
			Tools.logError("Failed to retrieve spray");
			return;
		}


		private void uploadSpray(Spray spray)
		{
			Tools.log($"Starting upload of {spray.name}");
			HttpContent fileStreamContent = new StreamContent(spray.fileInfo.OpenRead());

			MultipartFormDataContent formData = new MultipartFormDataContent {
				{fileStreamContent, "spray", spray.fileInfo.Name}
			};

			HttpResponseMessage response = makeApiCall(HttpMethod.Put, "Spray", formData);
			if (!response.IsSuccessStatusCode)
			{
				Tools.logError($"Failed upload of {spray.name}");
				return;
			}
			Tools.log($"Completed upload of {spray.name}");
		}


		private List<string> getServerSprayList() {
			
			var response = makeApiCall(HttpMethod.Get, "Hashes");

			var responseString = response.Content.ReadAsStringAsync().Result;
			if (response.IsSuccessStatusCode) {
				JObject responseObject = JObject.Parse(responseString);
				var hashes = responseObject["hashes"].Children();
				return hashes.Select(h => h.Value<string>()).ToList();
			}
			
			return null;
		}


		private JObject getServerConfig() {
			var response = makeApiCall(HttpMethod.Get, "Config");

			var responseString = response.Content.ReadAsStringAsync().Result;
			if (response.IsSuccessStatusCode) {
				JObject responseObject = JObject.Parse(responseString);
				return responseObject;
			}
			throw new Exception("Failed to fetch server config. Exiting.");
		}

		private int getServerVersion() {
			var response = makeApiCall(HttpMethod.Get, $"Version");
			var responseString = response.Content.ReadAsStringAsync().Result;
			if (response.IsSuccessStatusCode) {
				JObject responseObject = JObject.Parse(responseString);
				return responseObject.Value<int>("Version");
			}
			throw new Exception("Failed to fetch server version. Exiting.");
		}


	}


}
