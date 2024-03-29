﻿using Lib.Interfaces;
using Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Fabric;
using System.Net;
using System.Text;

namespace Web.Controllers
{
    public class CheckoutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> PayPal([FromForm]object userPaymentCredential)
        {
            //Intagrate PayPal sandbox
            INotification notificationProxy = ServiceProxy.Create<INotification>(new Uri("fabric:/Shop/Notifications")); // Notification API

            try
            {
                var fabricpaymentURI = new Uri("fabric:/Shop/Payment");
                var fabricAuthURI = new Uri("fabric:/Shop/Users");
                var fabricCartURI = new Uri("fabric:/Shop/Cart");

                FabricClient fabricClient = new FabricClient();
                var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricpaymentURI));
                int index = new Random().Next(0, partitionsList.Count);

                IPayment paymentProxy = null;
                IAuth authProxy = null;
                ICart cartProxy = null;

                var servicePartitionKey = new ServicePartitionKey(index % partitionsList.Count);
                paymentProxy = ServiceProxy.Create<IPayment>(fabricpaymentURI, servicePartitionKey); // Payment dictionary
                authProxy = ServiceProxy.Create<IAuth>(fabricAuthURI, new ServicePartitionKey(index % partitionsList.Count)); //User dictionary
                cartProxy = ServiceProxy.Create<ICart>(fabricCartURI, new ServicePartitionKey(index % partitionsList.Count)); // Basket dictionary

                //Stateless
                IOrder orderProxy = ServiceProxy.Create<IOrder>(new Uri("fabric:/Shop/Orders")); //save to storage
                ICatalog catalogProxy = ServiceProxy.Create<ICatalog>(new Uri("fabric:/Shop/ProductCatalog"));


                List<string> products = await cartProxy.GetBasketProducts();
                var listOfProducts = new List<Product>();
                products.ForEach(x => listOfProducts.Add(JsonConvert.DeserializeObject<Product>(x)!));

                string user = await authProxy.GetActiveUser();

                if (user != string.Empty || user != null && products.Count > 0)
                {
                    double price = 0;

                    //Count the price of all products
                    foreach (Product item in listOfProducts)
                    {
                        price += item.Quantity * item.Price;
                    }

                    Guid id = Guid.NewGuid();
                    User userObject = JsonConvert.DeserializeObject<User>(user)!;

                    Order order = new Order(id.ToString(), DateTime.Now, price, userObject.Id, "", "Pay Pal", products);
                    string orderJSON = JsonConvert.SerializeObject(order);


                    //Call to notification service
                    await notificationProxy.PublishMessage($"[{id}] Pay Pal order is created successfully at [{order.TimeOfOrder}]", "ordersqueue");

                    await paymentProxy.SaveOrderToDictionary(orderJSON);
                    await orderProxy.AddOrderToStorage(order);

                    //Remove all of the products in the basket
                    await cartProxy.RemoveAllProductsDictionary();

                    //From the dicitonary remove products that are bought 

                    await catalogProxy.RemoveBoughtProductsFromStorage(products);

                }

                return RedirectToAction("Cart", "Products");

            }
            catch (Exception ex)
            {
                await notificationProxy.PublishMessage($"There was something wrong with your order!", "ordersqueue");

                return StatusCode(500, "Error in communication with service " + ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> OnDelivery([FromForm]string address)
        {
            // Call basket and User to collect data
            //Add the order to the OrderDict for history
            // Save the order to database
            //Redirect to Home Page and send a notificaiton
            INotification notificationProxy = ServiceProxy.Create<INotification>(new Uri("fabric:/Shop/Notifications")); // Notification API
            ;

            try
            {
                var fabricpaymentURI = new Uri("fabric:/Shop/Payment");
                var fabricAuthURI = new Uri("fabric:/Shop/Users");
                var fabricCartURI = new Uri("fabric:/Shop/Cart");

                FabricClient fabricClient = new FabricClient();
                var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricpaymentURI));
                int index = !address.Equals("") ? address.Count() : new Random().Next(0, partitionsList.Count);

                IPayment paymentProxy = null;
                IAuth authProxy = null;
                ICart cartProxy = null;

                var servicePartitionKey = new ServicePartitionKey(index % partitionsList.Count);
                paymentProxy = ServiceProxy.Create<IPayment>(fabricpaymentURI, servicePartitionKey); // Payment dictionary
                authProxy = ServiceProxy.Create<IAuth>(fabricAuthURI, new ServicePartitionKey(index % partitionsList.Count)); //User dictionary
                cartProxy = ServiceProxy.Create<ICart>(fabricCartURI, new ServicePartitionKey(index % partitionsList.Count)); // Basket dictionary

                //Stateless
                IOrder orderProxy = ServiceProxy.Create<IOrder>(new Uri("fabric:/Shop/Orders")); //save to storage
                ICatalog catalogProxy = ServiceProxy.Create<ICatalog>(new Uri("fabric:/Shop/ProductCatalog"));


                List<string> products = await cartProxy.GetBasketProducts();
                var listOfProducts = new List<Product>();
                products.ForEach(x => listOfProducts.Add(JsonConvert.DeserializeObject<Product>(x)!));

                string user = await authProxy.GetActiveUser();

                if (user != string.Empty || user != null && products.Count > 0)
                {
                    double price = 0;

                    //Count the price of all products
                    foreach (Product item in listOfProducts)
                    {
                        price += item.Quantity * item.Price;
                    }

                    Guid id = Guid.NewGuid();
                    User userObject = JsonConvert.DeserializeObject<User>(user)!;

                    Order order = new Order(id.ToString(), DateTime.Now, price, userObject.Id, address, "Pay On Delivery", products);
                    string orderJSON = JsonConvert.SerializeObject(order);


                    //Call to notification service
                    await notificationProxy.PublishMessage($"[{id}] On Delivery Payment order is created successfully at [{order.TimeOfOrder}]", "ordersqueue");

                    await paymentProxy.SaveOrderToDictionary(orderJSON);
                    await orderProxy.AddOrderToStorage(order);

                    //Remove all of the products in the basket
                    await cartProxy.RemoveAllProductsDictionary();

                    //From the dicitonary remove products that are bought 

                    await catalogProxy.RemoveBoughtProductsFromStorage(products);

                }

                return RedirectToAction("Cart", "Products");
                
            }
            catch (Exception ex)
            {
                await notificationProxy.PublishMessage($"There was something wrong with your order!", "ordersqueue");

                return StatusCode(500, "Error in communication with service " + ex.Message);
            }

        }

    }
}
