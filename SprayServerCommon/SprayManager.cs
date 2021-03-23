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
			sprays.Add(spray);
			Tools.log($"{fileName} saved");
		}


		public void reloadSprays() => loadSprays(saveDirectory);
		public void loadSprays(DirectoryInfo directory) {
			
			Directory.CreateDirectory(saveDirectory.FullName);
			List<FileInfo> files = directory.GetFiles().Where(f => f.isValidSpray()).ToList();

			List<Spray> sprays = files.Select(f => new Spray() {
				fileInfo = f
			}).ToList();

			if (this.sprays == null || !this.sprays.SequenceEqual(sprays)) {
				Tools.log("Loading Sprays");
				this.sprays = sprays;
			}

		}

	}
}
