#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Core.Downloader.Business;
using Core.Json;
using LogLib;
using Utilities.FileOperation;

namespace Mirance.DeviceConfig;

public class iDeviceConfigManager
{
	private const string config_url = "https://dl.imobie.com/config/Models.cf";

	public Dictionary<string, string> iosMap = new Dictionary<string, string>
	{
		{ "a", "iPad" },
		{ "b", "iPhone" },
		{ "c", "iPod" },
		{ "d", "c Touch" }
	};

	private List<iDeviceClass> list = new List<iDeviceClass>();

	private static iDeviceConfigManager _instance;

	private ideviceFile file;

	public static iDeviceConfigManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new iDeviceConfigManager();
			}
			return _instance;
		}
	}

	public iDeviceConfigManager()
	{
		Init();
	}

	private void Init()
	{
		try
		{
			string path = Utilities.GetConfigDevicePath();
			if (!File.Exists(path))
			{
				path = ResourceManager.LoadResource(Path.GetTempPath(), "Mirance.DeviceConfig.Resources.Models.cf", "Models.cf");
			}
			if (File.Exists(path))
			{
				string str = File.ReadAllText(path);
				file = JsonConvert.DeserializeObject<ideviceFile>(str);
			}
		}
		catch (Exception ex)
		{
			LogMessager.WriteError("Update Model.cf file error " + ex.Message);
		}
	}

	public void DownloadConfig()
	{
		try
		{
			Trace.TraceInformation("download device config");
			string configDevicePath = Utilities.GetConfigDevicePath();
			Core.Downloader.Business.Downloader downloader = new Core.Downloader.Business.Downloader();
			FileTool.DeleteFile(configDevicePath);
			if (!downloader.Download("https://dl.imobie.com/config/Models.cf", configDevicePath))
			{
				Trace.TraceInformation("download device config failed");
			}
			else
			{
				Init();
			}
		}
		catch (Exception ex)
		{
			Trace.TraceWarning("download device config exception: " + ex.Message);
		}
	}

	public string QueryFName(string productID)
	{
		string result = "";
		if (string.IsNullOrEmpty(productID))
		{
			return "";
		}
		string text = productID.ToLower();
		if (text.Contains("iphone"))
		{
			result = "iPhone";
		}
		else if (text.Contains("ipad"))
		{
			result = "iPad";
		}
		else if (text.Contains("ipod"))
		{
			result = "iPod touch";
		}
		else if (text.Contains("tv"))
		{
			result = "Apple TV";
		}
		if (file != null)
		{
			iDeviceClass iDeviceClass2 = file.models.FirstOrDefault((iDeviceClass p) => p.productid.Contains(GetIDKey(productID)));
			if (iDeviceClass2 != null)
			{
				iDeviceClass2.friendlyname = GetIDValue(iDeviceClass2.friendlyname);
				return iDeviceClass2.friendlyname;
			}
		}
		return result;
	}

	private string GetIDKey(string id)
	{
		string text = id;
		foreach (string key in iosMap.Keys)
		{
			if (text.StartsWith(iosMap[key]))
			{
				text = text.Replace(iosMap[key], key);
			}
		}
		return text;
	}

	public string GetIDValue(string id)
	{
		string text = id;
		while (true)
		{
			string text2 = text.Substring(0, 1);
			if (!iosMap.ContainsKey(text2))
			{
				break;
			}
			text = text.Replace(text2 + " ", iosMap[text2] + " ");
		}
		return text;
	}
}
