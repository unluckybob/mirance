using Mirance.Model;

namespace Mirance.Transit;

internal class TransitAudio : TransitBase
{
	public TransitAudio(string identity)
		: base(identity)
	{
	}

	protected override void OnPop(TransitDataBase data)
	{
		PopCenter.Instance.PopAudio(data);
	}
}
