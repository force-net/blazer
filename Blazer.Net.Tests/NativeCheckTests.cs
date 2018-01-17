using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Force.Blazer.Native;
using Force.Crc32;
using NUnit.Framework;

using E = Force.Blazer.Algorithms.Crc32C.Crc32C;

namespace Blazer.Net.Tests
{
	[TestFixture]
	public class NativeCheckTests
	{
		[Test]
		public void ResultConsistency()
		{
			// checking that native implementation is available. it works only on windows, but tests on windows now

			// removing old data
			var architectureSuffix = IntPtr.Size == 8 ? "x64" : "x86";
			var dllPath = Path.Combine(Path.GetTempPath(), "Blazer.Net.0.8.3.9", architectureSuffix);
			var fileName = Path.Combine(dllPath, "Blazer.Native.dll");
			if (File.Exists(fileName))
			{
				try
				{
					File.Delete(fileName);
				}
				catch (Exception e)
				{
					File.Move(fileName, fileName + ".old");
					throw;
				}
			}

			var result = (bool) typeof(NativeHelper).GetMethod("Init", BindingFlags.Static | BindingFlags.NonPublic)
				.Invoke(null, new object[0]);
			
			Assert.That(result, Is.True);
		}
	}
}
