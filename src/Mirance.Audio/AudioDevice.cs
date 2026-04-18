using NAudio.CoreAudioApi;

namespace Mirance.Audio;

public class AudioDevice
{
	private readonly MMDevice _underlyingDevice;

	private PropertyStore _propertyStore;

	public bool IsActive => State == DeviceState.Active;

	public PropertyStore Properties
	{
		get
		{
			if (_propertyStore == null)
			{
				_propertyStore = OpenPropertyStore();
			}
			return _propertyStore;
		}
	}

	public string DeviceDescription
	{
		get
		{
			if (Properties.Contains(PropertyKeys.PKEY_Device_DeviceDesc))
			{
				return (string)Properties[PropertyKeys.PKEY_Device_DeviceDesc].Value;
			}
			return "Unknown";
		}
	}

	public string DeviceFriendlyName => _underlyingDevice.DeviceFriendlyName;

	public string FriendlyName => _underlyingDevice.FriendlyName;

	public string IconPath => _underlyingDevice.IconPath;

	public string Id => _underlyingDevice.ID;

	public DataFlow Kind => _underlyingDevice.DataFlow;

	public DeviceState State => _underlyingDevice.State;

	public MMDevice MMDeivce => _underlyingDevice;

	public AudioDevice(MMDevice underlyingDevice)
	{
		_underlyingDevice = underlyingDevice;
	}

	public override string ToString()
	{
		return FriendlyName;
	}

	private PropertyStore OpenPropertyStore()
	{
		return _underlyingDevice.Properties;
	}
}
