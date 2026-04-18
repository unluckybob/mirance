using System;
using System.IO;
using System.Reflection;
using LogLib;

namespace Mirance.DeviceConfig;

public static class ResourceManager
{
	public static string LoadResource(string BaseFolderPath, string ResourceName, string DllName)
	{
		string text = Path.Combine(BaseFolderPath, DllName);
		try
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			GetResourceFile(ResourceName, text);
		}
		catch (Exception ex)
		{
			LogMessager.Write($"StackTrace:{ex.StackTrace}---Message:{ex.Message}", LogMessager.LogMessageType.Error);
		}
		return text;
	}

	public static int DeleteResource(string SourcePath)
	{
		if (File.Exists(SourcePath))
		{
			try
			{
				File.Delete(SourcePath);
			}
			catch (Exception ex)
			{
				LogMessager.Write($"StackTrace:{ex.StackTrace}---Message:{ex.Message}", LogMessager.LogMessageType.Error);
				return 1;
			}
		}
		return 0;
	}

	private static void GetResourceFile(string resouece, string destPath)
	{
		Assembly.GetExecutingAssembly().GetManifestResourceNames();
		Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resouece);
		if (manifestResourceStream != null)
		{
			if (!File.Exists(destPath) || new FileInfo(destPath).Length != manifestResourceStream.Length)
			{
				using FileStream fileStream = new FileStream(destPath, FileMode.Create);
				byte[] array = new byte[65536];
				int count;
				while ((count = manifestResourceStream.Read(array, 0, array.Length)) > 0)
				{
					fileStream.Write(array, 0, count);
				}
			}
			manifestResourceStream.Close();
		}
		else
		{
			LogMessager.WriteInfo("there is no " + resouece);
		}
	}
}
