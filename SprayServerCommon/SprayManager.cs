using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SprayServerCommon {
	public class SprayManager {

		private DirectoryInfo saveDirectory;
		public List<Spray> sprays { get; private set; }

		public SprayManager(DirectoryInfo saveDirectory) {
			this.saveDirectory = saveDirectory;
			loadSprays(saveDirectory);
		}


		public void addSpray(string fileName, byte[] bytes) {
			Spray spray = new Spray();
			spray.fileInfo = new FileInfo(Path.Combine(saveDirectory.FullName, fileName));
			spray.data = bytes;
			if (spray.fileInfo.isVtf()) {
				sprays.Add(spray);
				Tools.log($"{fileName} saved");
			} else {
				Tools.logError($"File {spray.fileInfo.FullName} is not a valid spray. Not saving.");
			}
		}


		public void reloadSprays() => loadSprays(saveDirectory);
		private void loadSprays(DirectoryInfo directory) {
			if(!saveDirectory.Exists) Directory.CreateDirectory(saveDirectory.FullName);

			List<FileInfo> files = directory.GetFiles().Where(f => f.hasValidExtension()).ToList();

			List<FileInfo> validVtfFiles = files.Where(f => f.isVtf()).ToList();

			foreach (FileInfo invalidVtf in files.Except(validVtfFiles)) {
				Tools.logError($"File {invalidVtf.FullName} is not a valid spray. You should remove it.");
			}


			List<Spray> sprays = files.Select(f => new Spray() {
				fileInfo = f
			}).ToList();

			if (this.sprays == null || !this.sprays.SequenceEqual(sprays)) {
				Tools.log("(Re)Loaded Sprays");
				this.sprays = sprays;
			}

		}

	}
}
