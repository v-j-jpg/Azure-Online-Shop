using Lib.Models;
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
    public interface IOrder : IService
    {
        [OperationContract]
        Task<bool> AddOrderToStorage(Order order);
        [OperationContract]
        Task<List<string>> GetAllOrdersFromStorage();
    }
}
