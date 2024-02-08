using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Models
{
    public class OrderList
    {
        [DataMember]
        private List<Order> orders;
        public OrderList() {
            orders = new List<Order>();
        }
        public OrderList(List<Order> ordersReceived)
        {
            orders = ordersReceived;
        }

        public List<Order> Orders { get => orders; set => orders = value; }
    }
}
