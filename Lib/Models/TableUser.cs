using Azure;
using Azure.Data.Tables;
using ITableEntity = Azure.Data.Tables.ITableEntity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Models
{
    public class TableUser: ITableEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PartitionKey { get ; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public TableUser(string id, string name, string email, string password)
        {
            Id = id;
            Name = name;
            Email = email;
            Password = password;
        }

        public TableUser(string id)
        {
            PartitionKey = "TableUsers";
            RowKey = id;
        }

        public TableUser()
        {
        }
    }
}
