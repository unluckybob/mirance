using System;

namespace Mirance.Model;

public class MirroringMode
{
	public Action<bool> RequestMirroringResult;

	public MirrorType MirrorType { get; set; }

	public string Version { get; set; }

	public string Identity { get; set; }

	public int Width { get; set; }

	public int Height { get; set; }

	public string DeviceName { get; set; }

	public string DeviceModel { get; set; }

	public bool IsPad { get; set; }

	public bool IsRadar { get; set; }

	public int AndroidConnectFrom { get; set; }

	public string AndroidBrand { get; set; }
}
