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
    public interface ICart : IService
    {
       [OperationContract]
       Task<List<string>> GetBasketProducts();

       [OperationContract]
       Task AddProductToBasketDictionary(string productId);

       [OperationContract]
       Task DeleteProductFromBasket(string productId);

       [OperationContract]
       Task<string> GetBasketProduct(string productId);

        [OperationContract]
        Task EditProductInBasket(string productId);
    }
}
