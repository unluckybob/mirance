using Mirance.Model;

namespace Mirance.Transit;

internal class TransitFrame : TransitBase
{
	public TransitFrame(string identity)
		: base(identity)
	{
		TransitWatcher();
	}

	protected override void OnPop(TransitDataBase data)
	{
		PopCenter.Instance.PopFrame(data);
	}
}
