using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SprayServerCommon {
	public static class ExtensionMethods {

		public static bool isValidSpray(this FileInfo fileInfo) {
			return fileInfo.Extension == ".vtf";
		}






	}
}
