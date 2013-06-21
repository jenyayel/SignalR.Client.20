using System.Collections.Generic;
using Newtonsoft.Json;
using SignalR.Client._20.Transports;
using Newtonsoft.Json.Linq;

namespace SignalR.Client._20.Hubs
{
	public class HubConnection : Connection
	{
		private readonly Dictionary<string, HubProxy> _hubs = new Dictionary<string, HubProxy>();

		public HubConnection(string url)
			: base(GetUrl(url))
		{
		}

		public override void Start(IClientTransport transport)
		{
			Sending += OnConnectionSending;

			base.Start(transport);
		}

		public override void Stop()
		{
			Sending -= OnConnectionSending;
			base.Stop();
		}

		protected override void OnReceived(JToken message)
		{
			var invocation = message.ToObject<HubInvocation>();
			HubProxy hubProxy;
			if (_hubs.TryGetValue(invocation.Hub, out hubProxy))
			{
				if (invocation.State != null)
				{
					foreach (var state in invocation.State)
					{
						hubProxy[state.Key] = state.Value;
					}
				}

				hubProxy.InvokeEvent(invocation.Method, invocation.Args);
			}

			base.OnReceived(message);
		}

		public IHubProxy CreateProxy(string hubName)
		{
			HubProxy hubProxy;
			if (!_hubs.TryGetValue(hubName, out hubProxy))
			{
				hubProxy = new HubProxy(this, hubName);
				_hubs[hubName] = hubProxy;
			}
			return hubProxy;
		}

		private string OnConnectionSending()
		{
			var data = new List<HubRegistrationData>();
			foreach (var p in _hubs)
			{
				data.Add(new HubRegistrationData { Name = p.Key });
			}
			
			return JsonConvert.SerializeObject(data);
		}

		private static string GetUrl(string url)
		{
			if (!url.EndsWith("/"))
			{
				url += "/";
			}
			return url + "signalr";
		}
	}
}
