using System;
using System.Collections;
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

namespace Users
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Users : StatefulService, IAuth
    {
        public IReliableDictionary<string, User>? userDictionary;
        TableServiceClient tableServiceClient;
        TableClient tableClient;
        public Users(StatefulServiceContext context)
            : base(context)
        {
            var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            tableServiceClient = new TableServiceClient(connectionString);

            tableClient = tableServiceClient.GetTableClient(
            tableName: "users"
             );
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
            userDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("users");

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

        public async Task<List<string>> ListClientsFromTheDicitonary()
        {
            var clients = new List<string>();
            userDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("users");

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerator = (await userDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var user = enumerator.Current.Value;
                    clients.Add(JsonConvert.SerializeObject(user));
                }
            }

            return clients;
        }
        public async Task AddOrUpdateUserToDictionary(string user)
        {
            userDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("users");
            User userModel = JsonConvert.DeserializeObject<User>(user);

            try
            {
                // Create a new Transaction object for this partition
                using (var tx = StateManager.CreateTransaction())
                {
                    // AddAsync takes key's write lock; if >4 secs, TimeoutException
                    // Key & value put in temp dictionary (read your own writes),
                    // serialized, redo/undo record is logged & sent to secondary replicas

                    await userDictionary!.AddOrUpdateAsync(tx, userModel.Id!, userModel, (k, v) => v);

                    // CommitAsync sends Commit record to log & secondary replicas
                    // After quorum responds, all locks released
                    await tx.CommitAsync();
                }
            }
            catch (TimeoutException ex)
            {
                //file in use/locked, delay the transaction 
                await Task.Delay(100);
            }

        }
        public async Task<bool> UpdateUserDictionary(string user)
        {
            userDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("users");
            User userModel = JsonConvert.DeserializeObject<User>(user);

            try
            {
                // Create a new Transaction object for this partition
                using (var tx = StateManager.CreateTransaction())
                {
                    // AddAsync takes key's write lock; if >4 secs, TimeoutException
                    // Key & value put in temp dictionary (read your own writes),
                    // serialized, redo/undo record is logged & sent to secondary replicas

                    User temp = (await userDictionary.TryGetValueAsync(tx, userModel.Id)).Value; //Get the old value
                    await userDictionary!.TryUpdateAsync(tx, userModel.Id!, userModel, temp);

                    // CommitAsync sends Commit record to log & secondary replicas
                    // After quorum responds, all locks released
                    await tx.CommitAsync();
                }
                return true;
            }
            catch (TimeoutException ex)
            {
                //file in use/locked, delay the transaction 
                await Task.Delay(100);
                return false;
            }

        }
        public async Task<bool> AddUsersToStorage(string user)
        {
            User userModel = JsonConvert.DeserializeObject<User>(user);

            var userTableObject = new TableUser(userModel.Id)
            {
                Name = userModel.Name,
                Email = userModel.Email,
                Password = userModel.Password
            };

            // Add new item to server-side table
            tableClient.AddEntity(userTableObject);

            return true;
        }

        public async  Task<string> GetUser()
        {
            userDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("users");

            try
            {
                using (var tx = StateManager.CreateTransaction())
                {
                    var customer = await userDictionary!.TryGetValueAsync(tx, "0"); //Active user will always be first

                    //if user exist, the object will be returned
                    return JsonConvert.SerializeObject(customer.Value);
                }
            }
            catch (Exception ex)
            {
                throw new NotImplementedException(ex.Message);
            }
        }
        public async Task<string> GetUserById(string Id)
        {
            userDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("users");

            try
            {
                using (var tx = StateManager.CreateTransaction())
                {
                    var customer = await userDictionary!.TryGetValueAsync(tx, Id);

                    //if user exist, the object will be returned
                    return JsonConvert.SerializeObject(customer.Value);
                }
            }
            catch (Exception ex)
            {
                throw new NotImplementedException(ex.Message);
            }
        }

        public async Task<string> CheckUsersCredential(string userJSON)
        {
            try
            {
                User user = JsonConvert.DeserializeObject<User>(userJSON);
                Pageable<TableUser> queryResultsFilter = tableClient.Query<TableUser>(filter: $"Email eq '{user.Email}' and Password eq '{user.Password}'");

                foreach (TableUser qEntity in queryResultsFilter)
                {
                    User foundUser = new User(qEntity.RowKey, qEntity.Name, qEntity.Email, qEntity.Password);
                    return JsonConvert.SerializeObject(foundUser);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> UpdateUserStorage(string user)
        {
            User userModel = JsonConvert.DeserializeObject<User>(user);

            var userTableObject = new TableUser(userModel.Id)
            {
                Name = userModel.Name,
                Email = userModel.Email,
                Password = userModel.Password
            };


            // Since no UpdateMode was passed, the request will default to Merge.
            object value = await tableClient.UpsertEntityAsync(userTableObject, TableUpdateMode.Merge);

            return true;
        }

        public async Task LogOut()
        {
            userDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("users");

            using (var transaction = StateManager.CreateTransaction())
            {
                await userDictionary.ClearAsync();
            }
        }
    }
}
