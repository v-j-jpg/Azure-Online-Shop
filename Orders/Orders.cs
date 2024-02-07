using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Linq;
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
using static Azure.Core.HttpHeader;

namespace Orders
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Orders : StatelessService, IOrder
    {
        TableServiceClient tableServiceClient;
        TableClient tableOrders;
        public Orders(StatelessServiceContext context)
            : base(context)
        {
            string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            tableServiceClient = new TableServiceClient(connectionString);
            tableOrders = tableServiceClient.GetTableClient(
            tableName: "orders"
             );
        }

        public async Task<bool> AddOrderToStorage(Order order)
        {
            try
            {
                if (order != null)
                {
                    var tableOrder = new TableOrder(order.Id)
                    {
                        Id = order.Id,
                        Address = order.Address,
                        Price = order.Price,
                        TimeOfOrder = DateTime.UtcNow,
                        Timestamp = DateTimeOffset.UtcNow,
                        Products = string.Join(",", order.Products.ToArray())
                    };

                    // Add new item to server-side table
                    tableOrders.AddEntity(tableOrder);

                    return true;
                }
                return false;
            } 
            catch(Exception ex) {

                throw new Exception(ex.Message);
            }
           
        }

        public async Task<List<string>> GetAllOrdersFromStorage(string order)
        {
            Pageable<TableOrder> queryResultsFilter = tableOrders.Query<TableOrder>(filter: $"PartitionKey eq 'TableOrders'");
            var orders = new List<string>();

            foreach (TableOrder qEntity in queryResultsFilter)
            {
                Order newOrder = new Order(qEntity.Id, qEntity.TimeOfOrder, qEntity.Price, qEntity.UserId, qEntity.Address, qEntity.PaymentMethod, new List<string>());
                orders.Add(JsonConvert.SerializeObject(newOrder));
            }

            return orders;
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
