using Newtonsoft.Json;
using System.Collections.Generic;

namespace SignalR.Client._20.Hubs
{
	public class HubResult<T>
    {
        [JsonProperty("R")]
        public T Result { get; set; }

        [JsonProperty("E")]
        public string Error { get; set; }

        [JsonProperty("S")]
		public IDictionary<string, object> State { get; set; }
	}
}