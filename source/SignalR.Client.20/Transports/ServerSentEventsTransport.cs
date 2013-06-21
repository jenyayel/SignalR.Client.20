using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SignalR.Client._20.Http;
using SignalR.Client._20.Infrastructure;

namespace SignalR.Client._20.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        private const string ReaderKey = "sse.reader";
        private int _initializedCalled;

        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(2);

        public ServerSentEventsTransport()
            : this(new DefaultHttpClient())
        {
        }

        public ServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient, "serverSentEvents")
        {
            ConnectionTimeout = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// Time allowed before failing the connect request
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        protected override void OnStart(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            OpenConnection(connection, data, initializeCallback, errorCallback);
        }

        private void Reconnect(IConnection connection, string data)
        {
            if (!connection.IsActive)
            {
                return;
            }

            // Wait for a bit before reconnecting
            Thread.Sleep(ReconnectDelay);

            // Now attempt a reconnect
            OpenConnection(connection, data, initializeCallback: null, errorCallback: null);
        }

        private void OpenConnection(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            // If we're reconnecting add /connect to the url
            bool reconnecting = initializeCallback == null;

            var url = (reconnecting ? connection.Url : connection.Url + "connect");

            Action<IRequest> prepareRequest = PrepareRequest(connection);

            EventSignal<IResponse> signal;

            if (shouldUsePost(connection))
            {
                url += GetReceiveQueryString(connection, data);

                Debug.WriteLine(string.Format("SSE: POST {0}", url));

                signal = _httpClient.PostAsync(url, request =>
                                                        {
                                                            prepareRequest(request);
                                                            request.Accept = "text/event-stream";
                                                        }, new Dictionary<string, string> { { "groups", GetSerializedGroups(connection) } });
            }
            else
            {
                url += GetReceiveQueryStringWithGroups(connection, data);

                Debug.WriteLine(string.Format("SSE: GET {0}", url));

                signal = _httpClient.GetAsync(url, request =>
                {
                    prepareRequest(request);

                    request.Accept = "text/event-stream";
                });
            }

            signal.Finished += (sender, e) =>
            {
                if (e.Result.IsFaulted)
                {
                    var exception = e.Result.Exception.GetBaseException();
                    if (!HttpBasedTransport.IsRequestAborted(exception))
                    {
                        if (errorCallback != null &&
                            Interlocked.Exchange(ref _initializedCalled, 1) == 0)
                        {
                            errorCallback(exception);
                        }
                        else if (reconnecting)
                        {
                            // Only raise the error event if we failed to reconnect
                            connection.OnError(exception);
                        }
                    }

                    if (reconnecting)
                    {
                        // Retry
                        Reconnect(connection, data);
                        return;
                    }
                }
                else
                {
                    // Get the reseponse stream and read it for messages
                    var response = e.Result;
                    var stream = response.GetResponseStream();
                    var reader = new AsyncStreamReader(stream,
                                                       connection,
                                                       () =>
                                                       {
                                                           if (Interlocked.CompareExchange(ref _initializedCalled, 1, 0) == 0)
                                                           {
                                                               initializeCallback();
                                                           }
                                                       },
                                                       () =>
                                                       {
                                                           response.Close();

                                                           Reconnect(connection, data);
                                                       });

                    if (reconnecting)
                    {
                        // Raise the reconnect event if the connection comes back up
                        connection.OnReconnected();
                    }

                    reader.StartReading();

                    // Set the reader for this connection
                    connection.Items[ReaderKey] = reader;
                }
            };

            if (initializeCallback != null)
            {
                Thread.Sleep(ConnectionTimeout);
                if (Interlocked.CompareExchange(ref _initializedCalled, 1, 0) == 0)
                {
                    // Stop the connection
                    Stop(connection);

                    // Connection timeout occured
                    errorCallback(new TimeoutException());
                }
            }
        }

        private static bool shouldUsePost(IConnection connection)
        {
            return new List<string>(connection.Groups).Count > 20;
        }

        protected override void OnBeforeAbort(IConnection connection)
        {
            // Get the reader from the connection and stop it
            var reader = ConnectionExtensions.GetValue<AsyncStreamReader>(connection, ReaderKey);
            if (reader != null)
            {
                // Stop reading data from the stream
                reader.StopReading(false);

                // Remove the reader
                connection.Items.Remove(ReaderKey);
            }
        }
    }
}
