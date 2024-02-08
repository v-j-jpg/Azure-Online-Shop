using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub;
using System.Net.WebSockets;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Collections;
using Azure.Messaging.ServiceBus;
using static System.Fabric.FabricClient;
using Microsoft.Azure.ServiceBus;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Lib.Interfaces;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Newtonsoft.Json;

namespace Notifications
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Notifications : StatelessService, INotification
    {
        string connectionString = ConfigurationManager.AppSettings["ConnectionString"];

        // name of your Service Bus queue
        // the client that owns the connection and can be used to create senders and receivers
        ServiceBusClient client;

        // the sender used to publish messages to the queue
        ServiceBusSender sender;


        ServiceBusClientOptions clientOptions;
        public Notifications(StatelessServiceContext context)
            : base(context)
        {
            clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets
            };

        }

        public async Task PublishMessage(string serviceBusMessage, string queueName)
        {
            try
            {
                // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
                await using var client = new ServiceBusClient(connectionString);

                // create the sender
                ServiceBusSender sender = client.CreateSender(queueName);

                // create a message that we can send. UTF-8 encoding is used when providing a string.
                ServiceBusMessage message = new ServiceBusMessage(serviceBusMessage);

                // send the message
                await sender.SendMessageAsync(message);

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
        public async Task<string?> ReceiveMessage(string queueName)
        {
            await using var client = new ServiceBusClient(connectionString);

            // create a receiver that we can use to receive the message
            ServiceBusReceiver receiver = client.CreateReceiver(queueName);

            // the received message is a different type as it contains some service set properties
            ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();
            string message = receivedMessage.Body.ToString();

            if (message != string.Empty)
            {
                // get the message body as a string
                return receivedMessage.Body.ToString();
            }

            return null;
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
