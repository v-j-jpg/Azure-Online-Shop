using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Lib.Models;

namespace Lib.Interfaces
{
    [ServiceContract]
    public interface ICatalog : IService
    {
        [OperationContract]
        Task<List<string>> GetAllProducts();

        [OperationContract]
        Task<string?> GetProduct(string productId);

        [OperationContract]
        Task<bool> AddProductsToStorage(Product product);

        [OperationContract]
        Task<List<string>> GetProductByFilter(string query);

        [OperationContract]
        Task RemoveBoughtProductsFromStorage(List<string> products);
    }
}
