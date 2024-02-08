using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Models
{
    public class TableOrder : ITableEntity
    {
        private string id;
        private string products;
        private DateTime timeOfOrder;
        private double price;
        private string userId;
        private string address;
        private string paymentMethod;
        private bool isHistory;
        public TableOrder()
        {
        }

        public TableOrder(string id)
        {
            PartitionKey = "TableOrders";
            RowKey = id;
        }

        public string Id { get => id; set => id = value; }
        public string Products { get => products; set => products = value; }

        public DateTime TimeOfOrder { get => timeOfOrder; set => timeOfOrder = value; }
        public double Price { get => price; set => price = value; }
       
        public string Address { get; set; }
        public string PartitionKey { get ; set ; }
        public string RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get ; set; }
        public string UserId { get => userId; set => userId = value; }
        public string Address1 { get => address; set => address = value; }
        public string PaymentMethod { get => paymentMethod; set => paymentMethod = value; }
        public bool IsHistory { get => isHistory; set => isHistory = value; }
    }
}
