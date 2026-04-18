using System;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Mirance.Render;

internal class MMDeviceNotification : IMMNotificationClient
{
	public Action<string> ActionSpeakerChanged;

	public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
	{
		switch (flow)
		{
		case DataFlow.Render:
			ActionSpeakerChanged?.Invoke(defaultDeviceId);
			break;
		case DataFlow.Capture:
		case DataFlow.All:
			break;
		}
	}

	public void OnDeviceAdded(string pwstrDeviceId)
	{
	}

	public void OnDeviceRemoved(string deviceId)
	{
	}

	public void OnDeviceStateChanged(string deviceId, DeviceState newState)
	{
	}

	public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
	{
	}
}
