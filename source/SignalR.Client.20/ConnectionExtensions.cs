using System;
using Newtonsoft.Json;
using SignalR.Client._20.Hubs;

namespace SignalR.Client._20
{
    public static class ConnectionExtensions
    {
        public static T GetValue<T>(IConnection connection, string key)
        {
            object value;
            if (connection.Items.TryGetValue(key, out value))
            {
                return (T)value;
            }

            return default(T);
        }
    }
}
