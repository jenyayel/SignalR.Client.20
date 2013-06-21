using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.Client._20.Transports
{
    public class SseEvent
    {
        public SseEvent(EventType type, string data)
        {
            Type = type;
            Data = data;
        }

        public EventType Type { get; private set; }
        public string Data { get; private set; }
    }

    public enum EventType
    {
        Id,
        Data
    }
}
