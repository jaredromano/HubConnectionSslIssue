using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace HubConnectionExample
{
    public class TestHub : Hub
    {
        public Task SendMessage(string user, string message)
        {
            return Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
