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
    public interface IAuth : IService
    {
        [OperationContract]
        Task AddOrUpdateUserToDictionary(string user);
        [OperationContract]
        Task<bool> AddUsersToStorage(string user);
        [OperationContract]
        Task<List<string>> ListClientsFromTheDicitonary();

        [OperationContract]
        Task<string> GetActiveUser();

        [OperationContract]
       Task<string> CheckUsersCredential(string user);

        [OperationContract]
        Task<bool> UpdateUserStorage(string user);

        [OperationContract]
        Task<bool> UpdateUserDictionary(string user);


        [OperationContract]
        Task<string> GetUserById(string Id);

        [OperationContract]
        Task LogOut();

    }
}
