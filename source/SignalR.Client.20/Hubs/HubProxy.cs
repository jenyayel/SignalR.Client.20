using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SignalR.Client._20.Transports;

namespace SignalR.Client._20.Hubs
{
	public class HubProxy : IHubProxy
	{
		private readonly string _hubName;
		private readonly IConnection _connection;
		private readonly Dictionary<string, object> _state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, Subscription> _subscriptions = new Dictionary<string, Subscription>(StringComparer.OrdinalIgnoreCase);

		public HubProxy(IConnection connection, string hubName)
		{
			_connection = connection;
			_hubName = hubName;
		}

		public object this[string name]
		{
			get
			{
				object value;
				_state.TryGetValue(name, out value);
				return value;
			}
			set
			{
				_state[name] = value;
			}
		}

		public Subscription Subscribe(string eventName)
		{
			if (eventName == null)
			{
				throw new ArgumentNullException("eventName");
			}

			Subscription subscription;
			if (!_subscriptions.TryGetValue(eventName, out subscription))
			{
				subscription = new Subscription();
				_subscriptions.Add(eventName, subscription);
			}

			return subscription;
		}

		public EventSignal<object> Invoke(string method, params object[] args)
		{
			return Invoke<object>(method, args);
		}

		public EventSignal<T> Invoke<T>(string method, params object[] args)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}

			var hubData = new HubInvocation
			{
				Hub = _hubName,
				Method = method,
				Args = args,
				State = _state,
                CallbackId = "1"
			};

			var value = JsonConvert.SerializeObject(hubData);
			var newSignal = new OptionalEventSignal<T>();
			var signal = _connection.Send<HubResult<T>>(value);
			signal.Finished += (sender, e) =>
			{
				if (e.Result != null)
				{

					if (e.Result.Error != null)
					{
						throw new InvalidOperationException(e.Result.Error);
					}

					HubResult<T> hubResult = e.Result;
					if (hubResult.State != null)
					{
						foreach (var pair in hubResult.State)
						{
							this[pair.Key] = pair.Value;
						}
					}

					newSignal.OnFinish(hubResult.Result);
				}
				else
				{
					newSignal.OnFinish(default(T));
				}
			};
			return newSignal;
		}

		public void InvokeEvent(string eventName, object[] args)
		{
			Subscription eventObj;
			if (_subscriptions.TryGetValue(eventName, out eventObj))
			{
				eventObj.OnData(args);
			}
		}

		public IEnumerable<string> GetSubscriptions()
		{
			return _subscriptions.Keys;
		}
	}
}
