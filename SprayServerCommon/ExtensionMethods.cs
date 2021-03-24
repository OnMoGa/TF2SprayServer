using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SprayServerCommon {
	public static class ExtensionMethods {

		public static bool hasValidExtension(this FileInfo fileInfo) {
			return fileInfo.Extension == ".vtf";
		}


		public static bool isVtf(this FileInfo fileInfo) {
			if (!fileInfo.hasValidExtension()) return false;

			byte[] buffer = new byte[3];
			using (FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read))
			{
				int bytesRead = fs.Read(buffer, 0, buffer.Length);
				fs.Close();

				if (bytesRead != buffer.Length)
				{
					//file was too short
					return false;
				}
			}

			bool containsVtfSignature = new string(buffer.Select(b => (char)b).ToArray()) == "VTF";

			
			return containsVtfSignature;
		}



	}
}
