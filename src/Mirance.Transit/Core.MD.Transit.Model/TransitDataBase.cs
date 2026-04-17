using System;

namespace Mirance.Model;

public class TransitDataBase
{
	public string identity;

	public ulong pts;

	public byte[] data;

	public IntPtr dataPtr;

	public int dataPtrlength;

	public byte flags;

	public bool isKeyFrams;

	public string Guid { get; }

	public int Length
	{
		get
		{
			if (data != null)
			{
				return data.Length;
			}
			return dataPtrlength;
		}
	}
}
