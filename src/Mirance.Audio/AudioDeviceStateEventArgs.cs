using NAudio.CoreAudioApi;

namespace Mirance.Audio;

public class AudioDeviceStateEventArgs : AudioDeviceEventArgs
{
	private readonly DeviceState _newState;

	public DeviceState NewState => _newState;

	public AudioDeviceStateEventArgs(AudioDevice device, DeviceState newState)
		: base(device)
	{
		_newState = newState;
	}
}
