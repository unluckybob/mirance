using System;
using System.IO;
using System.Reflection;
using LogLib;
using Utilities.RegistryOperation;

namespace Mirance.DeviceConfig;

public class Utilities
{
	public static string GetAPPDATAPath()
	{
		string text = "";
		try
		{
			text = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		}
		catch (Exception ex)
		{
			LogMessager.WriteInfo("SHGetSpecialFolderPath Error," + ex.Message);
			text = "";
		}
		if (string.IsNullOrEmpty(text))
		{
			try
			{
				Register register = new Register("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders\\", RegDomain.CurrentUser);
				if (register.IsSubKeyExist())
				{
					text = register.ReadRegeditKey("AppData").ToString();
				}
			}
			catch (Exception ex2)
			{
				LogMessager.WriteInfo("Register FolderPath Error," + ex2.Message);
				text = "";
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			if (Directory.Exists(folderPath))
			{
				folderPath = Path.Combine(folderPath, "AppData");
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				text = folderPath;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			text = Path.Combine(text, "iMobie", MacthProjectName(GetProjectName()));
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
		}
		return text;
	}

	public static string MacthProjectName(string proName)
	{
		switch (proName.Trim().ToLower())
		{
		case "phonetrans":
		case "phonetrans_pro":
			return "PhoneTrans";
		case "podtrans":
		case "podtrans_pro":
			return "PodTrans";
		case "anytrans":
		case "anytrans for ios":
			return "AnyTrans";
		case "anytrans-chipversion":
			return "AnyTrans-Chip Version";
		default:
			return proName;
		}
	}

	private static string GetProjectName()
	{
		string text = "";
		try
		{
			if (AppDomain.CurrentDomain != null && !string.IsNullOrEmpty(AppDomain.CurrentDomain.FriendlyName))
			{
				string[] array = AppDomain.CurrentDomain.FriendlyName.Split('.');
				if (array != null && array.Length != 0)
				{
					text = array[0];
				}
				else
				{
					text = Assembly.GetExecutingAssembly().ManifestModule.Name;
					if (!string.IsNullOrEmpty(text))
					{
						text = text.Replace(".", "").Replace("exe", "").Replace("EXE", "");
					}
				}
			}
			else
			{
				text = Assembly.GetExecutingAssembly().ManifestModule.Name;
				if (!string.IsNullOrEmpty(text))
				{
					text = text.Replace(".", "").Replace("exe", "");
				}
			}
		}
		catch
		{
			text = DateTime.Now.ToString("yyyyMMddHHmmss");
		}
		return MacthProjectName(text);
	}

	public static string GetConfigDevicePath()
	{
		string result = "";
		string aPPDATAPath = GetAPPDATAPath();
		if (!string.IsNullOrEmpty(aPPDATAPath))
		{
			aPPDATAPath = Path.Combine(aPPDATAPath, "AutoUpdate");
			if (!Directory.Exists(aPPDATAPath))
			{
				Directory.CreateDirectory(aPPDATAPath);
			}
			result = Path.Combine(aPPDATAPath, "Models.cf");
		}
		else
		{
			LogMessager.Write("Config GetAPPDATAPath is NULL", LogMessager.LogMessageType.Warn);
		}
		return result;
	}
}
