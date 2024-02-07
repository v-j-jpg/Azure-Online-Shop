using Lib.Interfaces;
using Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class AuthentificationController : Controller
    { 
        Random rn= new Random();
        public async Task<IActionResult> Users()
        {
            var random = rn.Next(1, 5);
            IAuth proxy = ServiceProxy.Create<IAuth>(new Uri("fabric:/Shop/Users"), new ServicePartitionKey(random));
            var users = new List<User>();

            List<string> usersJson = await proxy.ListClientsFromTheDicitonary();

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
            IAuth proxy = ServiceProxy.Create<IAuth>(new Uri("fabric:/Shop/Users"), new ServicePartitionKey(Int32.Parse(user.Id) % 5)); // split to 5 nodes
            
            var convertedUser = JsonConvert.SerializeObject(user);
            var isUserInDatabase = proxy.AddUsersToStorage(convertedUser);

            await proxy.AddOrUpdateUserToDictionary(convertedUser);
            

            return RedirectToAction("Users");
        }

        public IActionResult SignIn()
        {

            return View();
        }
        public async Task<IActionResult> SignInAction([FromForm] User user)
        {
            //Compare the email and pass with the database

            IAuth proxy = ServiceProxy.Create<IAuth>(new Uri("fabric:/Shop/Users"), new ServicePartitionKey(1));
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
            IAuth proxy = ServiceProxy.Create<IAuth>(new Uri("fabric:/Shop/Users"), new ServicePartitionKey(1));

            if(id != null)
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
            IAuth proxy = ServiceProxy.Create<IAuth>(new Uri("fabric:/Shop/Users"), new ServicePartitionKey(1));

            var userJSON = JsonConvert.SerializeObject(user);
            await proxy.UpdateUserStorage(userJSON);
            await proxy.UpdateUserDictionary(userJSON);

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> LogOut()
        {
            //Edit the user in the database and dictionary
            IAuth proxy = ServiceProxy.Create<IAuth>(new Uri("fabric:/Shop/Users"), new ServicePartitionKey(1));


            return RedirectToAction("Index", "Home");
        }
    }
}
