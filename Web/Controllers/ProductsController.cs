using Lib.Interfaces;
using Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Newtonsoft.Json;

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

                //await proxy.AddProductsToStorage(new Product("6", "Test product","test test","Pants", 44.50, 80));

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
                ICart proxy = ServiceProxy.Create<ICart>(new Uri("fabric:/Shop/Cart"), new ServicePartitionKey(1));
                var basket = new List<Product>();

                // await proxy.AddProductsToStorage(new Product());

                List<string> productsJson = await proxy.GetBasketProducts();

                productsJson.ForEach(x => basket.Add(JsonConvert.DeserializeObject<Product>(x)!));
                double totalPrice = 0;
                basket.ForEach(product => totalPrice += product.Price);

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
                ICart proxy = ServiceProxy.Create<ICart>(new Uri("fabric:/Shop/Cart"), new ServicePartitionKey(1));
                var basket = new List<Product>();


                await proxy.AddProductToBasketDictionary(id);

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
                ICart proxy = ServiceProxy.Create<ICart>(new Uri("fabric:/Shop/Cart"), new ServicePartitionKey(1));
                var basket = new List<Product>();

                await proxy.DeleteProductFromBasket(id);

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
                ICart proxy = ServiceProxy.Create<ICart>(new Uri("fabric:/Shop/Cart"), new ServicePartitionKey(1));

                string product = await proxy.GetBasketProduct(id);

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
                ICart proxy = ServiceProxy.Create<ICart>(new Uri("fabric:/Shop/Cart"), new ServicePartitionKey(1));
                await proxy.EditProductInBasket(JsonConvert.SerializeObject(product));

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
