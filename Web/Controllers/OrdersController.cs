using Lib.Interfaces;
using Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Newtonsoft.Json;
using System.Fabric;
using static NuGet.Packaging.PackagingConstants;

namespace Web.Controllers
{
    public class OrdersController : Controller
    {
        public static OrderList ListOfElements = new OrderList();

        public async Task<IActionResult> History()
        {
            var fabricpaymentURI = new Uri("fabric:/Shop/Payment");

            FabricClient fabricClient = new FabricClient();
            var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricpaymentURI));
            int index = new Random().Next(0, partitionsList.Count);

            IPayment paymentProxy = null;
            var servicePartitionKey = new ServicePartitionKey(index % partitionsList.Count);
            paymentProxy = ServiceProxy.Create<IPayment>(fabricpaymentURI, servicePartitionKey); // Payment dictionary
            var orders = new List<Order>();

            List<string> ordersJSON = await paymentProxy.GetOrdersHistory();
            ordersJSON.ForEach(x => orders.Add(JsonConvert.DeserializeObject<Order>(x)!));


            List<OrderViewModel> ordersViewModelList = new List<OrderViewModel>();

            foreach (var order in orders)
            {
                List<Product> listOfProductsTemp = new List<Product>();

                foreach (var product in order.Products)
                {
                    listOfProductsTemp.Add(JsonConvert.DeserializeObject<Product>(product)!);
                }
                OrderViewModel orderViewModel = new OrderViewModel(order.Id, order.TimeOfOrder, order.Price, order.UserId, order.Address, listOfProductsTemp, false);
                ordersViewModelList.Add(orderViewModel);
            }

            return View(ordersViewModelList);
           // return Page.Response.Redirect(Page.Request.Url.ToString(), true);

        }

        //[HttpGet]
        //[Route("Orders/History")]
        //public IActionResult History()
        //{

        //    return View(ListOfElements);
        //}

        //[HttpPost]
        //[Route("Orders/Update")]
        //public IActionResult Update([FromBody]List<string> ordersJSON)
        //{
        //    if(ordersJSON!=null)
        //    {
        //        var orders = new List<Order>();

        //        ordersJSON.ForEach(x => orders.Add(JsonConvert.DeserializeObject<Order>(x)!));
        //        OrderList orderList = new OrderList(orders);
        //        ListOfElements = orderList;
        //    }


        //    return RedirectToAction("History", "Orders");
        //}


    }
}
