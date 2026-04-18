using NAudio.CoreAudioApi;

namespace Mirance.Audio;

public class DefaultAudioDeviceEventArgs : AudioDeviceEventArgs
{
	private readonly DataFlow _kind;

	private readonly Role _role;

	public DataFlow Kind => _kind;

	public Role Role => _role;

	public DefaultAudioDeviceEventArgs(AudioDevice device, DataFlow kind, Role role)
		: base(device)
	{
		_kind = kind;
		_role = role;
	}
}
