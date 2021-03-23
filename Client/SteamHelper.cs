using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SprayServerCommon;

namespace Client {
	class SteamHelper {

		IEnumerable<VDF> appManifests;
		

		public SteamHelper() {
			object? steamInstallPathResult = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null);
			if (steamInstallPathResult == null) {
				Tools.logError($"Failed to locate Steam install directory");
				Environment.Exit(2);
			}
			DirectoryInfo steamInstallPath = new DirectoryInfo((string)steamInstallPathResult);

			IEnumerable<DirectoryInfo> libraryFolders = getLibraryFolders(steamInstallPath);
			appManifests = libraryFolders.SelectMany(getAppManifests).ToList();
		}



		private IEnumerable<DirectoryInfo> getLibraryFolders(DirectoryInfo steamInstallPath) {
			DirectoryInfo mainSteamApps = new DirectoryInfo(Path.Combine(steamInstallPath.FullName, @"steamapps"));
			FileInfo libraryFoldersFile = new FileInfo(Path.Combine(steamInstallPath.FullName, mainSteamApps.FullName, "libraryfolders.vdf"));

			VDF libraryFoldersVdf = new VDF(libraryFoldersFile);

			IEnumerable<DirectoryInfo> libraryFolders = libraryFoldersVdf.Value.Children()
				.Cast<VProperty>().Where(t => Regex.IsMatch(t.Key, @"\d+"))
				.Select(t => new DirectoryInfo(Path.Combine(t.Value.ToString(), "steamapps")));

			return libraryFolders;
		}


		private IEnumerable<VDF> getAppManifests(DirectoryInfo libraryFolder) {

			IEnumerable<FileInfo> appManifestFiles = libraryFolder.GetFiles().Where(f => Regex.IsMatch(f.Name, @"appmanifest_\d+\.acf")).ToList();

			return appManifestFiles.Select(a => {
				VDF vdf;
				try {
					vdf = new VDF(a);
				} catch (Exception e) {
					return null;
				}
				vdf.name = Regex.Match(a.Name, @"appmanifest_(?<appId>\d+)\.acf").Groups["appId"].Value;
				return vdf;
			}).Where(v => v != null);
		}



		public DirectoryInfo getAppInstallDirectory(int appId) {
			dynamic appManifest = appManifests.DefaultIfEmpty(null).FirstOrDefault(a => a.name == appId.ToString());
			if (appManifest == null) return null;

			string steamappsCommonName = appManifest.Value.installdir.Value;
			DirectoryInfo installDir = new DirectoryInfo(Path.Combine(appManifest.fileInfo.DirectoryName, "common", steamappsCommonName));
			
			return installDir;
		}








		private class VDF : VProperty {

			public string name { get; set; }
			public FileInfo fileInfo { get; set; }

			public VDF(FileInfo fileInfo) : base(VdfConvert.Deserialize(File.ReadAllText(fileInfo.FullName))) {
				this.fileInfo = fileInfo;
				name = File.ReadLines(fileInfo.FullName).DefaultIfEmpty(string.Empty).FirstOrDefault().Trim('"');
			}

		}


	}
	
}
