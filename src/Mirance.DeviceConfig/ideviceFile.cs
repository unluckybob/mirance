using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mirance.DeviceConfig;

[DataContract]
public class ideviceFile
{
	[DataMember(Name = "r")]
	public List<iDeviceClass> models { get; set; }
}
