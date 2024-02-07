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

namespace Payment
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Payment : StatefulService, IPayment
    {
        public IReliableDictionary<string, Order>? ordersDictionary;

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

        public async Task SaveOrderToDictionary(string order)
        {
            try
            {   //Convert object to string
                Order? newOrder = JsonConvert.DeserializeObject<Order>(order);

                if (newOrder != null)
                {
                    ordersDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Order>>("orders");

                    // Create a new Transaction object for this partition
                    using (var tx = StateManager.CreateTransaction())
                    {
                        // AddAsync takes key's write lock; if >4 secs, TimeoutException
                        // Key & value put in temp dictionary (read your own writes),
                        // serialized, redo/undo record is logged & sent to secondary replicas
                        await ordersDictionary!.AddOrUpdateAsync(tx, newOrder.Id!, newOrder, (k, v) => v); ;

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
            ordersDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Order>>("orders");

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

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
