using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Lib.Interfaces;
using Lib.Models;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using static System.Reflection.Metadata.BlobBuilder;

namespace Cart
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Cart : StatefulService, ICart
    {
        public IReliableDictionary<string, Product>? basketDictionary;
        TableServiceClient tableServiceClient;
        TableClient tableBasket, tableProducts;


        public Cart(StatefulServiceContext context)
            : base(context)
        {
            string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            tableServiceClient = new TableServiceClient(connectionString);

            tableBasket = tableServiceClient.GetTableClient(
            tableName: "basket"
             );
            tableProducts = tableServiceClient.GetTableClient(
            tableName: "products"
             );
        }

        public async Task AddProductToBasketDictionary(string productId)
        {
            try
            {   //Find the product in the Database 
                Product newProduct = await GetProductStorage(productId);

                if (newProduct != null)
                {
                    basketDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Product>>("basket");

                    // Create a new Transaction object for this partition
                    using (var tx = StateManager.CreateTransaction())
                    {
                        // AddAsync takes key's write lock; if >4 secs, TimeoutException
                        // Key & value put in temp dictionary (read your own writes),
                        // serialized, redo/undo record is logged & sent to secondary replicas
                        await basketDictionary!.AddOrUpdateAsync(tx, newProduct.Id!, newProduct, (k, v) => v); ;

                        //AddAsync doesn't work if u reload the page

                        // CommitAsync sends Commit record to log & secondary replicas
                        // After quorum responds, all locks released
                        await tx.CommitAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task DeleteProductFromBasket(string productId)
        {
            try
            {
                //Find the product in the Database 
                Product newProduct = await GetProductStorage(productId);

                if (newProduct != null)
                {
                    basketDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Product>>("basket");

                    using (var tx = StateManager.CreateTransaction())
                    {

                        await basketDictionary!.TryRemoveAsync(tx, newProduct.Id!); ;

                        await tx.CommitAsync();
                    }
                    await DeleteBasketItemFromStorage(newProduct);
                }

            }
            catch (Exception ex)
            {

            }
        }

        public async Task<Product> GetProductStorage(string productId)
        {
            TableProduct qEntity = await tableProducts.GetEntityAsync<TableProduct>("TableProducts", productId);

            if (qEntity != null)
            {
                Product product = new Product(productId, qEntity.Name, qEntity.Description, qEntity.Category, qEntity.Price, 1); //Adds only one product to the basket


                return product;
            }

            return null;
        }

        public async Task<List<string>> GetBasketProducts()
        {
            basketDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Product>>("basket");

            var basket = new List<string>();

            using (var transaction = StateManager.CreateTransaction())
            {
                var enumerator = (await basketDictionary.CreateEnumerableAsync(transaction)).GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    basket.Add(JsonConvert.SerializeObject(enumerator.Current.Value));
                }
            }

            return basket;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            basketDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Product>>("basket");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }
                await SaveBasketToStorage();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public async Task<string> GetBasketProduct(string productId)
        {
            try
            {
                using (var transaction = StateManager.CreateTransaction())
                {
                    var product = await basketDictionary!.TryGetValueAsync(transaction, productId!);

                    return JsonConvert.SerializeObject(product!.Value);
                }
            }
            catch (Exception ex)
            {
                throw new NotImplementedException(ex.Message);
            }

        }
        public async Task EditProductInBasket(string product)
        {
            try
            {
                Product newProduct = JsonConvert.DeserializeObject<Product>(product);
                Product baseProduct = await GetProductStorage(newProduct.Id); //Get the value from the storage

                if (newProduct != null)
                {
                    basketDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Product>>("basket");

                    // Create a new Transaction object for this partition
                    using (var tx = StateManager.CreateTransaction())
                    {
                        // AddAsync takes key's write lock; if >4 secs, TimeoutException
                        // Key & value put in temp dictionary (read your own writes),
                        // serialized, redo/undo record is logged & sent to secondary replicas

                        Product temp = (await basketDictionary.TryGetValueAsync(tx, newProduct.Id)).Value; //Get the old value

                        newProduct.Price = baseProduct.Price * newProduct.Quantity;

                        await basketDictionary!.TryUpdateAsync(tx, newProduct.Id!, newProduct, temp);

                        //AddAsync doesn't work if u reload the page

                        // CommitAsync sends Commit record to log & secondary replicas
                        // After quorum responds, all locks released
                        await tx.CommitAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task RemoveAllProductsDictionary()
        {
            basketDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Product>>("basket");

            using (var transaction = StateManager.CreateTransaction())
            {
                await basketDictionary.ClearAsync();
            }
        }

        public async Task SaveBasketToStorage()
        {

            basketDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Product>>("basket");

            var basket = new List<string>();

            using (var transaction = StateManager.CreateTransaction())
            {
                var enumerator = (await basketDictionary.CreateEnumerableAsync(transaction)).GetAsyncEnumerator();

                //Loop through the whole basket dictionary and update the storage
                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    Product product = enumerator.Current.Value;
                    TableProduct newProductInBasket = new TableProduct()
                    {
                        Id = product.Id,
                        RowKey = product.Id,
                        PartitionKey = product.Category,
                        Price = product.Price,
                        Description = product.Description,
                        Quantity = product.Quantity
                    };
                    await tableBasket.UpsertEntityAsync(newProductInBasket, TableUpdateMode.Merge);
                }
            }
        }

        public async Task DeleteBasketItemFromStorage(Product basketItem)
        {
           tableBasket.DeleteEntity(basketItem.Category, basketItem.Category);
        }
    }
}
