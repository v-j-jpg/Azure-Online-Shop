using Lib.Interfaces;
using Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class OrdersController : Controller
    {
        public async Task<IActionResult> History()
        {
            IPayment proxy = ServiceProxy.Create<IPayment>(new Uri("fabric:/Shop/Payment"), new ServicePartitionKey(1));
            var orders = new List<Order>();

            List<string> ordersJSON = await proxy.GetOrdersHistory();

            ordersJSON.ForEach(x => orders.Add(JsonConvert.DeserializeObject<Order>(x)!));


            List<OrderViewModel> ordersViewModelList = new List<OrderViewModel>();

            foreach (var order in orders)
            {
                List<Product> listOfProductsTemp = new List<Product>();

                foreach (var product in order.Products)
                {
                    listOfProductsTemp.Add(JsonConvert.DeserializeObject<Product>(product)!);
                }
                OrderViewModel orderViewModel = new OrderViewModel(order.Id, order.TimeOfOrder, order.Price, order.UserId, order.Address, listOfProductsTemp);
                ordersViewModelList.Add(orderViewModel);
            }

            return View(ordersViewModelList);
        }
    }
}
