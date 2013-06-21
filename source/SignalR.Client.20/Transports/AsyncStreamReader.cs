using SignalR.Client._20.Http;
using SignalR.Client._20.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace SignalR.Client._20.Transports
{
    public class AsyncStreamReader
    {
        private readonly object _bufferLock = new object();
        private readonly Stream _stream;
        private readonly ChunkBuffer _buffer;
        private readonly System.Action _initializeCallback;
        private readonly System.Action _closeCallback;
        private readonly IConnection _connection;
        private int _processingQueue;
        private int _reading;
        private bool _processingBuffer;

        protected object BufferLock
        {
            get
            {
                return _bufferLock;
            }
        }

        public AsyncStreamReader(Stream stream, IConnection connection, System.Action initializeCallback, System.Action closeCallback)
        {
            _initializeCallback = initializeCallback;
            _closeCallback = closeCallback;
            _stream = stream;
            _connection = connection;
            _buffer = new ChunkBuffer();
        }

        public bool Reading
        {
            get
            {
                return _reading == 1;
            }
        }

        public void StartReading()
        {
            if (Interlocked.Exchange(ref _reading, 1) == 0)
            {
                ReadLoop();
            }
        }

        public void StopReading(bool raiseCloseCallback = true)
        {
            if (Interlocked.Exchange(ref _reading, 0) == 1)
            {
                if (raiseCloseCallback)
                {
                    _closeCallback();
                }
            }
        }

        private void ReadLoop()
        {
            if (!Reading)
            {
                return;
            }

            var buffer = new byte[1024];

            var signal = new EventSignal<CallbackDetail<int>>();
            signal.Finished += (sender, e) =>
            {
                if (e.Result.IsFaulted)
                {
                    Exception exception = e.Result.Exception.GetBaseException();

                    if (!HttpBasedTransport.IsRequestAborted(exception))
                    {
                        if (!(exception is IOException))
                        {
                            _connection.OnError(exception);
                        }

                        StopReading();
                    }
                    return;
                }

                int read = e.Result.Result;

                if (read > 0)
                {
                    // Put chunks in the buffer
                    _buffer.Add(buffer, read);
                }

                if (read == 0)
                {
                    // Stop any reading we're doing
                    StopReading();

                    return;
                }

                // Keep reading the next set of data
                ReadLoop();

                if (read <= buffer.Length)
                {
                    // If we read less than we wanted or if we filled the buffer, process it
                    ProcessBuffer();
                }
            };
            StreamExtensions.ReadAsync(signal, _stream, buffer);
        }

        private void ProcessBuffer()
        {
            if (!Reading)
            {
                return;
            }

            if (_processingBuffer)
            {
                // Increment the number of times we should process messages
                _processingQueue++;
                return;
            }

            _processingBuffer = true;

            int total = Math.Max(1, _processingQueue);

            for (int i = 0; i < total; i++)
            {
                if (!Reading)
                {
                    return;
                }

                ProcessChunks();
            }

            if (_processingQueue > 0)
            {
                _processingQueue -= total;
            }

            _processingBuffer = false;
        }

        private void ProcessChunks()
        {
            while (Reading && _buffer.HasChunks)
            {
                string line = _buffer.ReadLine();

                // No new lines in the buffer so stop processing
                if (line == null)
                {
                    break;
                }

                if (!Reading)
                {
                    return;
                }

                // Try parsing the sseEvent
                SseEvent sseEvent;
                if (!TryParseEvent(line, out sseEvent))
                {
                    continue;
                }

                if (!Reading)
                {
                    return;
                }

                Debug.WriteLine("SSE READ: " + sseEvent);

                switch (sseEvent.Type)
                {
                    case EventType.Id:
                        long id;
                        if (Int64.TryParse(sseEvent.Data, out id))
                        {
                            _connection.MessageId = id;
                        }
                        break;
                    case EventType.Data:
                        if (sseEvent.Data.Equals("initialized", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_initializeCallback != null)
                            {
                                // Mark the connection as started
                                _initializeCallback();
                            }
                        }
                        else
                        {
                            if (Reading)
                            {
                                // We don't care about timedout messages here since it will just reconnect
                                // as part of being a long running request
                                bool timedOutReceived;
                                bool disconnectReceived;

                                HttpBasedTransport.ProcessResponse(_connection, sseEvent.Data, out timedOutReceived, out disconnectReceived);

                                if (disconnectReceived)
                                {
                                    _connection.Stop();
                                }

                                if (timedOutReceived)
                                {
                                    return;
                                }
                            }
                        }
                        break;
                }
            }
        }

        protected bool TryParseEvent(string line, out SseEvent sseEvent)
        {
            sseEvent = null;

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                string data = line.Substring("data:".Length).Trim();
                sseEvent = new SseEvent(EventType.Data, data);
                return true;
            }
            else if (line.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
            {
                string data = line.Substring("id:".Length).Trim();
                sseEvent = new SseEvent(EventType.Id, data);
                return true;
            }

            return false;
        }
    }
}
