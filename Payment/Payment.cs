using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lib.Interfaces;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Lib.Models;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Transactions;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace Payment
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Payment : StatefulService, IPayment
    {
        public IReliableDictionary<string, Order>? ordersDictionary;
        public IReliableDictionary<string, Message>? messages;
        public Payment(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<List<string>> GetOrdersHistory()
        {
            ordersDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Order>>("orders");

            var orders = new List<string>();

            using (var transaction = StateManager.CreateTransaction())
            {
                var enumerator = (await ordersDictionary.CreateEnumerableAsync(transaction)).GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var order = enumerator.Current.Value;
                    orders.Add(JsonConvert.SerializeObject(order));
                }
            }

            return orders;
        }
        public async Task<List<string>> GetOrderMessages()
        {
            messages = await StateManager.GetOrAddAsync<IReliableDictionary<string, Message>>("messages");

            var messagesJSON = new List<string>();

            using (var transaction = StateManager.CreateTransaction())
            {
                var enumerator = (await messages.CreateEnumerableAsync(transaction)).GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var message = enumerator.Current.Value;
                    messagesJSON.Add(JsonConvert.SerializeObject(message));
                }
            }

            return messagesJSON;
        }

        public async Task SaveOrderToDictionary(string order)
        {
            try
            {   //Convert object to string
                Order? newOrder = JsonConvert.DeserializeObject<Order>(order);

                if (newOrder != null)
                {
                    ordersDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Order>>("orders");
                    messages = await StateManager.GetOrAddAsync<IReliableDictionary<string, Message>>("messages");

                    // Create a new Transaction object for this partition
                    using (var tx = StateManager.CreateTransaction())
                    {
                        // AddAsync takes key's write lock; if >4 secs, TimeoutException
                        // Key & value put in temp dictionary (read your own writes),
                        // serialized, redo/undo record is logged & sent to secondary replicas
                        string id= Guid.NewGuid().ToString();
                        await ordersDictionary!.AddOrUpdateAsync(tx, newOrder.Id!, newOrder, (k, v) => v);
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

        public async Task CheckOrders()
        {
            IOrder orderProxy = ServiceProxy.Create<IOrder>(new Uri("fabric:/Shop/Orders")); 
            List<Order> orders = new List<Order>();
            List<string> ordersJSON = await orderProxy.GetAllOrdersFromStorage();


                        //try
                        //{
                        //    HttpClient client = new HttpClient();
                        //    var json = JsonConvert.SerializeObject(ordersJSON);
                        //    var data = new StringContent(json, Encoding.UTF8, "application/json");
                        //    await client.PostAsync("http://localhost:8319/Orders/Update",data);
                        //}
                        //catch (Exception e)
                        //{
                        //    Console.WriteLine(e.Message);
                        //}
                    
                
            
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
            messages = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Message>>("messages");



            while (true)
            {
                ordersDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Order>>("orders");

                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");
                    var result2 = await ordersDictionary.TryGetValueAsync(tx, "");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.



                    await tx.CommitAsync();
                }

               // await CheckOrders();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
