using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Azure;
using Azure.Data.Tables;

using Lib.Interfaces;
using Lib.Models;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;

namespace ProductCatalog
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class ProductCatalog : StatelessService, ICatalog
    {
        
        TableServiceClient tableServiceClient;
        TableClient tableClient;

        public ProductCatalog(StatelessServiceContext context)
            : base(context)
        {
            string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            tableServiceClient = new TableServiceClient(connectionString);

           tableClient = tableServiceClient.GetTableClient(
            tableName: "products"
            );
          
        }

        public async Task<bool> AddProductsToStorage(Product product)
        {
            var prod1 = new TableProduct(product.Id)
            {
                Name = product.Name,
                Price= product.Price,
                Quantity = product.Quantity,
                Category = product.Category,
                Description = product.Description,
                Id = product.Id
            };

            // Add new item to server-side table
            tableClient.AddEntity(prod1);

            return true;
        }

        public async Task<List<string>> GetAllProducts()
        {

            Pageable<TableProduct> queryResultsFilter = tableClient.Query<TableProduct>(filter: $"PartitionKey eq 'TableProducts'");
            var products = new List<string>();

            foreach (TableProduct qEntity in queryResultsFilter)
            {
                Product product = new Product(qEntity.Id,qEntity.Name, qEntity.Description, qEntity.Category,qEntity.Price,qEntity.Quantity);
                products.Add(JsonConvert.SerializeObject(product));
            }

            return products;
        }

        public async Task<string?> GetProduct(string productId)
        {
            TableProduct qEntity = await tableClient.GetEntityAsync<TableProduct>("TableProduct", productId);

            if (qEntity != null)
            {
                //Product product = new Product(qEntity.Id, qEntity.Name, qEntity.Description, qEntity.Category, qEntity.Price, qEntity.Quantity);
                return JsonConvert.SerializeObject(qEntity);
            }

            return null;
            
        }

        public async Task<List<string>> GetProductByFilter(string query)
        {
            Pageable<TableProduct> queryResults = tableClient.Query<TableProduct>(ent => ent.Name == query || ent.Category == query);
            var productsJSON = new List<string>();

            if (queryResults != null)
            {
                foreach (TableProduct qEntity in queryResults)
                {
                    productsJSON.Add(JsonConvert.SerializeObject(new Product(qEntity.Id, qEntity.Name, qEntity.Description, qEntity.Category, qEntity.Price, qEntity.Quantity)));
                }
                return productsJSON;

            }

            return null;

        }

        public async Task RemoveBoughtProductsFromStorage(List<string> products)
        {
            List<TableProduct> boughtProducts = new List<TableProduct>();
            products.ForEach(product => boughtProducts.Add(JsonConvert.DeserializeObject<TableProduct>(product)));

            Pageable<TableProduct> queryResultsFilter = tableClient.Query<TableProduct>(filter: $"PartitionKey eq 'TableProducts'"); //get all products

            if (queryResultsFilter != null)
            {
                foreach (TableProduct qEntity in queryResultsFilter)
                {
                    foreach (TableProduct product in boughtProducts)
                    {
                        if(product.Id == qEntity.Id)
                        {
                            qEntity.Quantity -= product.Quantity;
                            await tableClient.UpsertEntityAsync(qEntity, TableUpdateMode.Merge);
                        }
                    }
                }


            }
        }



        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
