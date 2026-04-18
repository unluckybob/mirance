using System.Runtime.Serialization;

namespace Mirance.DeviceConfig;

[DataContract]
public class iDeviceClass
{
	[DataMember(Name = "b")]
	public string productid { get; set; }

	[DataMember(Name = "c")]
	public string friendlyname { get; set; }
}
