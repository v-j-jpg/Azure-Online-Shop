using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Models
{
    [DataContract]
    public class OrderViewModel
    {

        [DataMember]
        private string id;
        [DataMember]
        private List<Product> products;
        [DataMember]
        private string address;
        [DataMember]
        private DateTime timeOfOrder;
        [DataMember]
        private double price;
        [DataMember]
        private string userId;
        [DataMember]
        private bool isHistory;

        public OrderViewModel()
        {
            //products = new List<string>();
        }


            public OrderViewModel(string id, DateTime timeOfOrder, double price, string userId, string address, List<Product> products, bool isHistory)
        {
            this.id = id;
            this.timeOfOrder = timeOfOrder;
            this.price = price;
            this.userId = userId;
            this.products = products;
            this.address = address;
            this.IsHistory = isHistory;
        }

        //public Order(string id)
        //{
        //    PartitionKey = "Order";
        //    RowKey = id;
        //}

        public string Id { get => id; set => id = value; }
        public List<Product> Products { get => products; set => products = value; }
        public DateTime TimeOfOrder { get => timeOfOrder; set => timeOfOrder = value; }
        public double Price { get => price; set => price = value; }
        public string Address { get => address; set => address = value; }
        public string UserId { get => userId; set => userId = value; }
        public bool IsHistory { get => isHistory; set => isHistory = value; }
    }
}
