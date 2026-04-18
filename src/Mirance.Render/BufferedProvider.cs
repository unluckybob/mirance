using NAudio.Wave;

namespace Mirance.Render;

internal class BufferedProvider : BufferedWaveProvider, IWaveProvider
{
	public long AddedLength { get; set; }

	public long ReadedLength { get; set; }

	public BufferedProvider(WaveFormat waveFormat)
		: base(waveFormat)
	{
	}

	public new void AddSamples(byte[] buffer, int offset, int count)
	{
		base.AddSamples(buffer, offset, count);
	}

	public new int Read(byte[] buffer, int offset, int count)
	{
		return base.Read(buffer, offset, count);
	}
}
