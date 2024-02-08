using Lib.Interfaces;
using Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Newtonsoft.Json;
using System.Fabric;

namespace Web.Controllers
{
    public class AuthentificationController : Controller
    { 
        Random rn= new Random();
        public async Task<IActionResult> Users()
        {
            var users = new List<User>();
            List<string> usersJson;

            var fabricUserURI = new Uri("fabric:/Shop/Users");
            FabricClient fabricClient = new FabricClient();
            var partitionsList= (await fabricClient.QueryManager.GetPartitionListAsync(fabricUserURI));
            IAuth proxy = null;
            var random = rn.Next(0, partitionsList.Count);

            var partitionKey = partitionsList[random].PartitionInformation as Int64RangePartitionInformation;
            
            var servicePartitionKey = new ServicePartitionKey(random % partitionsList.Count);
            proxy = ServiceProxy.Create<IAuth>(fabricUserURI, servicePartitionKey);


            proxy = ServiceProxy.Create<IAuth>(fabricUserURI, servicePartitionKey);
            usersJson = await proxy.ListClientsFromTheDicitonary();
            usersJson.ForEach(x => users.Add(JsonConvert.DeserializeObject<User>(x)!));



            return View(users);
        }

        public  IActionResult Registration()
        {
            // Return View with input fields
            // Check user input
            return View();
        }

        public async Task<IActionResult> Register([FromForm]User user)
        {
            var fabricUserURI = new Uri("fabric:/Shop/Users");
            FabricClient fabricClient = new FabricClient();
            var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricUserURI));
            IAuth proxy = null;

            var partitionKey = partitionsList[Int32.Parse(user.Id) % partitionsList.Count].PartitionInformation as Int64RangePartitionInformation;

            var servicePartitionKey = new ServicePartitionKey(Int32.Parse(user.Id) % partitionsList.Count);
            proxy = ServiceProxy.Create<IAuth>(fabricUserURI, servicePartitionKey);

            var convertedUser = JsonConvert.SerializeObject(user);
            var isUserInDatabase = proxy.AddUsersToStorage(convertedUser);

            await proxy.AddOrUpdateUserToDictionary(convertedUser);
            

            return RedirectToAction("Users");
        }

        public IActionResult SignIn()
        {
            //rturn form
            return View();
        }
        public async Task<IActionResult> SignInAction([FromForm] User user)
        {
            //Compare the email and pass with the database

            var fabricUserURI = new Uri("fabric:/Shop/Users");
            FabricClient fabricClient = new FabricClient();
            var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricUserURI));
            IAuth proxy = null;
            var random = rn.Next(0, partitionsList.Count);

            var partitionKey = partitionsList[random].PartitionInformation as Int64RangePartitionInformation;

            var servicePartitionKey = new ServicePartitionKey(random % partitionsList.Count);
            proxy = ServiceProxy.Create<IAuth>(fabricUserURI, servicePartitionKey);

            var userJSON = JsonConvert.SerializeObject(user);

            string userInfo = await proxy.CheckUsersCredential(userJSON);

            if (userInfo != null)
            {
                //add user to active list
                await proxy.AddOrUpdateUserToDictionary(userInfo);
                return RedirectToAction("Users");
            }

            ViewBag.Error = "User credentials are not found in the database.";

            return View("SignIn");

        }

        public async Task<IActionResult> Edit(string id)
        {
            var fabricUserURI = new Uri("fabric:/Shop/Users");
            FabricClient fabricClient = new FabricClient();
            var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricUserURI));
            IAuth proxy = null;

            var partitionKey = partitionsList[Int32.Parse(id) % partitionsList.Count].PartitionInformation as Int64RangePartitionInformation;

            var servicePartitionKey = new ServicePartitionKey(Int32.Parse(id) % partitionsList.Count);
            proxy = ServiceProxy.Create<IAuth>(fabricUserURI, servicePartitionKey);

            if (id != null)
            {
                string user = await proxy.GetUserById(id);
                
                return View(JsonConvert.DeserializeObject<User>(user));
            }
            ViewBag.Error = "An error occured. User not found";
            return View();
            
        }

        public async Task<IActionResult> EditAction([FromForm]User user)
        {
            //Edit the user in the database and dictionary
            var fabricUserURI = new Uri("fabric:/Shop/Users");
            FabricClient fabricClient = new FabricClient();
            var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricUserURI));
            IAuth proxy = null;

            var partitionKey = partitionsList[Int32.Parse(user.Id) % partitionsList.Count].PartitionInformation as Int64RangePartitionInformation;

            var servicePartitionKey = new ServicePartitionKey(Int32.Parse(user.Id) % partitionsList.Count);
            proxy = ServiceProxy.Create<IAuth>(fabricUserURI, servicePartitionKey);

            var userJSON = JsonConvert.SerializeObject(user);
            await proxy.UpdateUserStorage(userJSON);
            await proxy.UpdateUserDictionary(userJSON);

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> LogOut()
        {

            var fabricUserURI = new Uri("fabric:/Shop/Users");
            FabricClient fabricClient = new FabricClient();
            var partitionsList = (await fabricClient.QueryManager.GetPartitionListAsync(fabricUserURI));
            IAuth proxy = null;
            var random = rn.Next(0, partitionsList.Count);

            var partitionKey = partitionsList[random].PartitionInformation as Int64RangePartitionInformation;

            var servicePartitionKey = new ServicePartitionKey(random % partitionsList.Count);
            proxy = ServiceProxy.Create<IAuth>(fabricUserURI, servicePartitionKey);

            await proxy.LogOut();

            return RedirectToAction("Index", "Home");
        }
    }
}
