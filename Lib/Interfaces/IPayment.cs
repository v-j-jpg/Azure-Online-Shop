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
    public interface IPayment : IService
    {
        [OperationContract]
        Task<List<string>> GetOrdersHistory();
        [OperationContract]
        Task SaveOrderToDictionary(string order);
    }
}
