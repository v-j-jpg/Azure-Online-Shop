using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Interfaces
{
    [ServiceContract]
    public interface INotification : IService
    {
        [OperationContract]
        Task PublishMessage(string serviceBusMessage, string queueName);

        [OperationContract]
        Task<string?> ReceiveMessage(string queueName);
    }
}
