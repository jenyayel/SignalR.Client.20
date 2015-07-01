using Newtonsoft.Json.Linq;
using SignalR.Client._20.Hubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            // uncomment below to stream debug into console
            // Debug.Listeners.Add(new ConsoleTraceListener());

            HubConnection connection = new HubConnection("http://localhost:58438/");
            IHubProxy proxy = connection.CreateProxy("TestHub");

            // subscribe to event
            proxy.Subscribe("Pong").Data += data =>
            {
                var _first = data[0] as JToken;
                Console.WriteLine("Received: [{0}] from {1}",
                    _first["message"].ToString(), _first["from"].ToString());
            };

            Console.Write("Connecting... ");
            connection.Start();
            Console.WriteLine("done. Hit 'enter' to send message or other key to exit.");

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Enter)
                {
                    Console.Write("Sending hi... ");
                    proxy.Invoke("Ping", Environment.UserName).Finished += (sender, e) =>
                    {
                        Console.WriteLine("done");
                    };
                }
                else
                    break;
            }
        }
    }
}
