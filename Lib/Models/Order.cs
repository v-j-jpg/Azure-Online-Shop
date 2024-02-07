using Lib.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Models
{
    [DataContract]
    public class Order
    {

        [DataMember]
        private string id;
        [DataMember]
        private List<string> products;
        [DataMember]
        private string address;
        [DataMember]
        private DateTime timeOfOrder;
        [DataMember]
        private double price;
        [DataMember]
        private string userId;
        [DataMember]
        private string paymentMethod;

        public Order()
        {
            //products = new List<string>();
        }

            public Order(string id, DateTime timeOfOrder, double price, string userId, string address,string paymentMethod, List<string> products)
        {
            this.id = id;
            this.timeOfOrder = timeOfOrder;
            this.price = price;
            this.userId = userId;
            this.products = products;
            this.address = address;
            this.paymentMethod = paymentMethod;
        }

        //public Order(string id)
        //{
        //    PartitionKey = "Order";
        //    RowKey = id;
        //}

        public string Id { get => id; set => id = value; }
        public List<string> Products { get => products; set => products = value; }
        public DateTime TimeOfOrder { get => timeOfOrder; set => timeOfOrder = value; }
        public double Price { get => price; set => price = value; }
        public string Address { get => address; set => address = value; }
        public string UserId { get => userId; set => userId = value; }
        public string PaymentMethod { get => paymentMethod; set => paymentMethod = value; }
    }
}
