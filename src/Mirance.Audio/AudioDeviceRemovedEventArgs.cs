using System;

namespace Mirance.Audio;

public class AudioDeviceRemovedEventArgs : EventArgs
{
	private readonly string _deviceId;

	public string DeviceId => _deviceId;

	public AudioDeviceRemovedEventArgs(string deviceId)
	{
		_deviceId = deviceId;
	}
}
