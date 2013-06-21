using System.Collections.Generic;

namespace SignalR.Client._20.Hubs
{
	public class HubResult<T>
	{
		public T Result { get; set; }
		public string Error { get; set; }
		public IDictionary<string, object> State { get; set; }
	}
}