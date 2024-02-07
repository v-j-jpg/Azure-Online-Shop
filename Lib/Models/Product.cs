using AutoMapper;
using Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Models
{
    [DataContract]
    public class Product
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Category { get; set; }
        [DataMember]
        public double Price { get; set; }
        [DataMember]
        public int Quantity { get; set; }

        public Product() {  }

        public Product(string id, string name, string description, string category, double price, int quantity)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Price = price;
            Quantity = quantity;
        }

     
    }
}
