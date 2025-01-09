using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace LocalFileWebService.Controllers
{
    public class UploadHub : Hub
    {
        public void UpdateProgress(string fileName, int progress)
        {
            Clients.All.updateProgress(fileName, progress);
        }
    }
}