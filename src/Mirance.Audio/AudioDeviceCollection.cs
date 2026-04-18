using System.Collections;
using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace Mirance.Audio;

public class AudioDeviceCollection : IEnumerable<AudioDevice>, IEnumerable
{
	private readonly MMDeviceCollection _underlyingCollection;

	public int Count => _underlyingCollection.Count;

	public AudioDevice this[int index] => new AudioDevice(_underlyingCollection[index]);

	public AudioDeviceCollection(MMDeviceCollection parent)
	{
		_underlyingCollection = parent;
	}

	public IEnumerator<AudioDevice> GetEnumerator()
	{
		int count = Count;
		for (int index = 0; index < count; index++)
		{
			yield return this[index];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
