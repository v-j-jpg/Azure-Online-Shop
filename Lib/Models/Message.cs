﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Models
{
    public class Message
    {
        public string message { get; set; }

        public Message(string message)
        {
            this.message = message;
        }
    }
}
