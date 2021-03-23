using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SprayServerCommon {
	public class Spray {
		public FileInfo fileInfo { get; set; }
		public string name => fileInfo.Name.Split('.').First();
		public byte[] data {
			get {
				if (fileInfo == null) return null;
				return File.ReadAllBytes(fileInfo.FullName);
			}
			set {
				if (fileInfo == null) throw new Exception("FileInfo not set");
				Directory.CreateDirectory(fileInfo.DirectoryName);
				File.WriteAllBytes(fileInfo.FullName, value);
			}
		}


		public override bool Equals(object obj) {
			if (obj != null && obj is Spray other) {
				return GetHashCode() == other.GetHashCode();
			}

			return false;
		}
		public override int GetHashCode() {
			return name.Sum(c => c);
		}
		public override string ToString() {
			return $"{fileInfo.Name}";
		}
	}

	public static class SprayExtensions {
		
		public static int sprayCollectionHash(this IEnumerable<Spray> sprays) {
			long hash = 0;
			foreach (Spray spray in sprays) {
				hash += spray.GetHashCode();
				hash %= int.MaxValue;
			}
			return (int)hash;
		}


	}
}
