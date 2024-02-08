using Microsoft.Azure.ServiceBus;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text;
using System.Text.Json;

namespace Web.Services
{
    public class QueueService : IQueueService
    {
        private readonly IConfiguration _config;
        public QueueService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendMessageAsync<T>(T serviceBusMessage, string queueName)
        {
            try
            {
                var queueClient = new QueueClient(_config.GetConnectionString("AzureServiceBus"), queueName);
                string messageBody = JsonSerializer.Serialize(serviceBusMessage);
                var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                await queueClient.SendAsync(message);
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
    }
}
