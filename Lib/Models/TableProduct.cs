using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ITableEntity = Azure.Data.Tables.ITableEntity;
using Azure;
using System.Reflection.Metadata.Ecma335;

namespace Lib.Models
{
    public class TableProduct: ITableEntity
    {
        private string id;
        private string name;
        private double price;
        private string orderId;
        private string description;
        private string category;

        public string RowKey { get; set; } = default!;

        public string PartitionKey { get; set; } = default!;

        public TableProduct()
        {
        }

        public TableProduct(string id)
        {
            PartitionKey = "TableProduct";
            RowKey = id;
        }

        public string Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string OrderId { get => orderId; set => orderId = value; }
        public double Price { get => price; set=> price = value; }
        public int Quantity { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Description { get => description; set => description = value; }
        public string Category { get => category; set => category = value; }
    }
}
