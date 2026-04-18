using System;

namespace Mirance.Audio;

public class AudioDeviceEventArgs : EventArgs
{
	private readonly AudioDevice _device;

	public AudioDevice Device => _device;

	public AudioDeviceEventArgs(AudioDevice device)
	{
		_device = device;
	}
}
