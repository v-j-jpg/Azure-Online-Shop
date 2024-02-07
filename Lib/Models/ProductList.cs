using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Models
{
    [DataContract]
    public class ProductList
    {
        [DataMember]
        private List<Product> products;
        [DataMember]
        private Dictionary<string, Order> actives;
        [DataMember]
        private Dictionary<string, Order> history;

        public ProductList()
        {
            Products = new List<Product>();
            Actives = new Dictionary<string, Order>();
            History = new Dictionary<string, Order>();
        }

        public List<Product> Products { get => products; set => products = value; }
        public Dictionary<string, Order> Actives { get => actives; set => actives = value; }
        public Dictionary<string, Order> History { get => history; set => history = value; }
    }
}
