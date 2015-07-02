using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace ServerDemo.Hubs
{
    public class TestHub : Hub
    {
        public void Ping(string from)
        {
            Clients.All.Pong(new
            {
                message = "Hi",
                from = from
            });
        }

        public dynamic RequestReplyDynamic()
        {
            return new { time = DateTime.Now.ToLongTimeString() };
        }

        public int RequestReplyValueType()
        {
            return DateTime.Now.Millisecond;
        }
    }
}