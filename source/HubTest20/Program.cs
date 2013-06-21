using Newtonsoft.Json.Linq;
using SignalR.Client._20.Hubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace HubTest20
{
    class Program
    {
        static void Main(string[] args)
        {
            // Debug.Listeners.Add(new ConsoleTraceListener());

            string _sessionCode = args.Length > 0 ? args[0] : "4575";
            string _url = args.Length > 1 ? args[1] : "http://localhost:62190/";

            HubConnection _conn = new HubConnection(_url);
            IHubProxy _proxy = _conn.CreateProxy("digitalClass");

            _proxy.Subscribe("invokeCommand").Data += data => {
                if (data == null || data.Length == 0)
                    return;

                JToken _first = data[0] as JToken;

                if (_first["command"].ToString() == "PauseActivity")
                    Console.WriteLine("Pause Game");

                if (_first["command"].ToString() == "ResumeActivity")
                    Console.WriteLine("Resume Game");

                if (_first["command"].ToString() == "ClosePresentation")
                    Console.WriteLine("Close Game");
            };

            _conn.Start();
            _proxy.Invoke("JoinFromGame", _sessionCode);

            // wait for push message
            while (true) { };
        }
    }
}
