using Lib.Interfaces;
using Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Newtonsoft.Json;
using System.Fabric;

namespace Web.Controllers
{
    public class ProductsController : Controller
    {
        public async Task<IActionResult> Catalog()
        {
            try
            {
                ICatalog proxy = ServiceProxy.Create<ICatalog>(new Uri("fabric:/Shop/ProductCatalog"));
                var products = new List<Product>();


                List<string> productsJson = await proxy.GetAllProducts();

                productsJson.ForEach(x => products.Add(JsonConvert.DeserializeObject<Product>(x)!));

                return View(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error in communication with service " + ex.Message);
            }

        }
       
        public async Task<IActionResult> Cart()
        {
            try
            {
                var fabricCartURI = new Uri("fabric:/Shop/Cart");

                FabricClient fabricClient = new FabricClient();
                var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricCartURI));
                int index = new Random().Next(0, partitionsList.Count);

                ICart cartProxy = null;

                var servicePartitionKey = new ServicePartitionKey(index);
                cartProxy = ServiceProxy.Create<ICart>(fabricCartURI, servicePartitionKey); // Basket dictionary

                var basket = new List<Product>();


                List<string> productsJson = await cartProxy.GetBasketProducts();

                productsJson.ForEach(x => basket.Add(JsonConvert.DeserializeObject<Product>(x)!));
                double totalPrice = 0;
                if(basket.Count > 0) { basket.ForEach(product => totalPrice += product.Price); }
                

                ViewBag.TotalPrice = totalPrice;

                return View(basket);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error in communication with service " + ex.Message);
            }

        }

        [HttpGet]
        public async Task<IActionResult> Buy(string id)
        {
            try
            {
                var fabricCartURI = new Uri("fabric:/Shop/Cart");

                FabricClient fabricClient = new FabricClient();
                var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricCartURI));
                int index = (id != null || id.Equals(string.Empty)) ? id.Count() :new Random().Next(0, partitionsList.Count);

                ICart cartProxy = null;

                var servicePartitionKey = new ServicePartitionKey(index % partitionsList.Count);
                cartProxy = ServiceProxy.Create<ICart>(fabricCartURI, servicePartitionKey); // Basket dictionary

                var basket = new List<Product>();


                await cartProxy.AddProductToBasketDictionary(id);

                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error in communication with service " + ex.Message);
            }
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var fabricCartURI = new Uri("fabric:/Shop/Cart");

                FabricClient fabricClient = new FabricClient();
                var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricCartURI));
                int index = (id != null || id.Equals(string.Empty)) ? id.Count() : new Random().Next(0, partitionsList.Count);

                ICart cartProxy = null;

                var servicePartitionKey = new ServicePartitionKey(index % partitionsList.Count);
                cartProxy = ServiceProxy.Create<ICart>(fabricCartURI, servicePartitionKey); // Basket dictionary

                var basket = new List<Product>();

                await cartProxy.DeleteProductFromBasket(id);

                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error in communication with service " + ex.Message);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var fabricCartURI = new Uri("fabric:/Shop/Cart");

                FabricClient fabricClient = new FabricClient();
                var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricCartURI));
                int index = (id != null || id.Equals(string.Empty)) ? id.Count() : new Random().Next(0, partitionsList.Count);

                ICart cartProxy = null;

                var servicePartitionKey = new ServicePartitionKey(index % partitionsList.Count);
                cartProxy = ServiceProxy.Create<ICart>(fabricCartURI, servicePartitionKey); // Basket dictionary

                string product = await cartProxy.GetBasketProduct(id);

                return View(JsonConvert.DeserializeObject<Product>(product));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error in communication with service " + ex.Message);
            }
        }
        public async Task<IActionResult> EditAction([FromForm]Product product)
        { 
            //Update product with Quanity
            try
            {
                var fabricCartURI = new Uri("fabric:/Shop/Cart");

                FabricClient fabricClient = new FabricClient();
                var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricCartURI));
                int index = (product.Id != null || product.Id.Equals(string.Empty)) ? product.Id.Count() : new Random().Next(0, partitionsList.Count);

                ICart cartProxy = null;

                var servicePartitionKey = new ServicePartitionKey(index % partitionsList.Count);
                cartProxy = ServiceProxy.Create<ICart>(fabricCartURI, servicePartitionKey); // Basket dictionary

                await cartProxy.EditProductInBasket(JsonConvert.SerializeObject(product));

                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error in communication with service " + ex.Message);
            }
        }

        public async Task<IActionResult> Search([FromForm]string productQuery)
        {
            try
            {
                ICatalog proxy = ServiceProxy.Create<ICatalog>(new Uri("fabric:/Shop/ProductCatalog"));
                var products = new List<Product>();

                List<string> productsJSON =await proxy.GetProductByFilter(productQuery);
                productsJSON.ForEach(x => products.Add(JsonConvert.DeserializeObject<Product>(x)!));

                return View("Catalog", products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error in communication with service " + ex.Message);
            }
        }
    }
}
