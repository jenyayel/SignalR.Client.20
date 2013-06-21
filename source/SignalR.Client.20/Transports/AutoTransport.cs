using System;
using SignalR.Client._20.Http;

namespace SignalR.Client._20.Transports
{
    public class AutoTransport : IClientTransport
	{
		// Transport that's in use
		private IClientTransport _transport;

		private readonly IHttpClient _httpClient;

		// List of transports in fallback order
		private readonly IClientTransport[] _transports;

		public AutoTransport(IHttpClient httpClient)
		{
			_httpClient = httpClient;
			_transports = new IClientTransport[] { new ServerSentEventsTransport(httpClient), new LongPollingTransport(httpClient) };
		}

		public EventSignal<NegotiationResponse> Negotiate(IConnection connection)
		{
			return HttpBasedTransport.GetNegotiationResponse(_httpClient, connection);
		}

    	public void Start(IConnection connection, string data)
        {
            // Resolve the transport
            ResolveTransport(connection, data, 0);
        }

		private void ResolveTransport(IConnection connection, string data, int index)
		{
			// Pick the current transport
			IClientTransport transport = _transports[index];

			try
			{
				transport.Start(connection, data);
				_transport = transport;
			}
			catch (Exception)
			{
				var next = index + 1;
				if (next < _transports.Length)
				{
					// Try the next transport
					ResolveTransport(connection, data, next);
				}
				else
				{
					// If there's nothing else to try then just fail
					throw new NotSupportedException("The transports available were not supported on this client.");
				}
			}
		}

    	public EventSignal<T> Send<T>(IConnection connection, string data)
        {
            return _transport.Send<T>(connection, data);
        }

        public void Stop(IConnection connection)
        {
            _transport.Stop(connection);
        }
    }
}
