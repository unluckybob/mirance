using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using Mirance.Audio.Interop;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Mirance.Audio;

[Export(typeof(AudioDeviceManager))]
public class AudioDeviceManager : IMMNotificationClient, IDisposable
{
	private readonly MMDeviceEnumerator _deviceEnumerator;

	private readonly SynchronizationContext _synchronizationContext;

	public event EventHandler<AudioDeviceEventArgs> DeviceAdded;

	public event EventHandler<AudioDeviceRemovedEventArgs> DeviceRemoved;

	public event EventHandler<AudioDeviceEventArgs> DevicePropertyChanged;

	public event EventHandler<DefaultAudioDeviceEventArgs> DefaultDeviceChanged;

	public event EventHandler<AudioDeviceStateEventArgs> DeviceStateChanged;

	public AudioDeviceManager(MMDeviceEnumerator deviceEnumerator)
	{
		_synchronizationContext = SynchronizationContext.Current;
		_deviceEnumerator = deviceEnumerator;
		int num = _deviceEnumerator.RegisterEndpointNotificationCallback(this);
		if (num != HResult.OK)
		{
			throw Marshal.GetExceptionForHR(num);
		}
	}

	public AudioDeviceCollection GetAudioDevices(DataFlow dataFlow, DeviceState state)
	{
		MMDeviceCollection mMDeviceCollection = _deviceEnumerator.EnumerateAudioEndPoints(dataFlow, state);
		if (mMDeviceCollection != null)
		{
			return new AudioDeviceCollection(mMDeviceCollection);
		}
		return null;
	}

	public void SetDefaultAudioDevice(AudioDevice device)
	{
		if (device == null)
		{
			throw new ArgumentNullException("device");
		}
		SetDefaultAudioDevice(device, Role.Multimedia);
		SetDefaultAudioDevice(device, Role.Communications);
		SetDefaultAudioDevice(device, Role.Console);
	}

	public void SetDefaultAudioDevice(AudioDevice device, Role role)
	{
		if (device == null)
		{
			throw new ArgumentNullException("device");
		}
		PolicyConfig policyConfig = new PolicyConfig();
		int num = ((!(policyConfig is IPolicyConfig2 policyConfig2)) ? ((IPolicyConfig3)policyConfig).SetDefaultEndpoint(device.Id, role) : policyConfig2.SetDefaultEndpoint(device.Id, role));
		if (num != HResult.OK)
		{
			throw Marshal.GetExceptionForHR(num);
		}
	}

	public bool IsDefaultAudioDevice(AudioDevice device, Role role)
	{
		if (device == null)
		{
			throw new ArgumentNullException("device");
		}
		AudioDevice defaultAudioDevice = GetDefaultAudioDevice(device.Kind, role);
		if (defaultAudioDevice == null)
		{
			return false;
		}
		return string.Equals(defaultAudioDevice.Id, device.Id, StringComparison.OrdinalIgnoreCase);
	}

	public bool IsDefaultAudioDevice(string iD, DataFlow kind, Role role)
	{
		if (string.IsNullOrEmpty(iD))
		{
			throw new ArgumentNullException("iD");
		}
		AudioDevice defaultAudioDevice = GetDefaultAudioDevice(kind, role);
		if (defaultAudioDevice == null)
		{
			return false;
		}
		return string.Equals(defaultAudioDevice.Id, iD, StringComparison.OrdinalIgnoreCase);
	}

	public AudioDevice GetDefaultAudioDevice(DataFlow dataFlow, Role role)
	{
		MMDevice defaultAudioEndpoint = _deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, role);
		if (defaultAudioEndpoint != null)
		{
			return new AudioDevice(defaultAudioEndpoint);
		}
		return null;
	}

	public AudioDevice GetDevice(string id)
	{
		if (id == null)
		{
			throw new ArgumentNullException("id");
		}
		MMDevice device = _deviceEnumerator.GetDevice(id);
		if (device != null)
		{
			return new AudioDevice(device);
		}
		return null;
	}

	public void OnDeviceAdded(string deviceId)
	{
		InvokeOnSynchronizationContext(delegate
		{
			EventHandler<AudioDeviceEventArgs> deviceAdded = this.DeviceAdded;
			if (deviceAdded != null)
			{
				AudioDevice device = GetDevice(deviceId);
				if (device != null)
				{
					deviceAdded(this, new AudioDeviceEventArgs(device));
				}
			}
		});
	}

	public void OnDeviceRemoved(string deviceId)
	{
		InvokeOnSynchronizationContext(delegate
		{
			this.DeviceRemoved?.Invoke(this, new AudioDeviceRemovedEventArgs(deviceId));
		});
	}

	private void InvokeOnSynchronizationContext(Action action)
	{
		if (_synchronizationContext == null)
		{
			action();
			return;
		}
		_synchronizationContext.Post(delegate
		{
			action();
		}, null);
	}

	public void OnDeviceStateChanged(string deviceId, DeviceState newState)
	{
		InvokeOnSynchronizationContext(delegate
		{
			EventHandler<AudioDeviceStateEventArgs> deviceStateChanged = this.DeviceStateChanged;
			if (deviceStateChanged != null)
			{
				AudioDevice device = GetDevice(deviceId);
				if (device != null)
				{
					deviceStateChanged(this, new AudioDeviceStateEventArgs(device, newState));
				}
			}
		});
	}

	public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
	{
		InvokeOnSynchronizationContext(delegate
		{
			EventHandler<DefaultAudioDeviceEventArgs> defaultDeviceChanged = this.DefaultDeviceChanged;
			if (defaultDeviceChanged != null)
			{
				AudioDevice device = null;
				if (defaultDeviceId != null)
				{
					device = GetDevice(defaultDeviceId);
				}
				defaultDeviceChanged(this, new DefaultAudioDeviceEventArgs(device, flow, role));
			}
		});
	}

	public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
	{
		InvokeOnSynchronizationContext(delegate
		{
			EventHandler<AudioDeviceEventArgs> devicePropertyChanged = this.DevicePropertyChanged;
			if (devicePropertyChanged != null)
			{
				AudioDevice device = GetDevice(pwstrDeviceId);
				if (device != null)
				{
					devicePropertyChanged(this, new AudioDeviceEventArgs(device));
				}
			}
		});
	}

	public void Dispose()
	{
		_deviceEnumerator.UnregisterEndpointNotificationCallback(this);
		_ = HResult.OK;
	}
}
