using Lib.Interfaces;
using Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using Web.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace Web.Controllers
{
   
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public static ProductList ListOfElements = new ProductList();
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            //INotification notificationProxy = ServiceProxy.Create<INotification>(new Uri("fabric:/Shop/Notifications")); // Notification API

            //var Message = await notificationProxy.ReceiveMessage("ordersqueue");
            //ViewData["Notification"] = Message;

            IPayment paymentProxy = ServiceProxy.Create<IPayment>(new Uri("fabric:/Shop/Payment"), new ServicePartitionKey(1)); // save to dictionary
            List<string> messages = await paymentProxy.GetOrderMessages();
            List<Message> messagesList = new List<Message>();

            if(messages.Count > 0)
            {
                foreach (var message in messages)
                {
                    messagesList.Add(JsonConvert.DeserializeObject<Message>(message));
                }
            }
            return View(messagesList);
        }
        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

       
    }
}