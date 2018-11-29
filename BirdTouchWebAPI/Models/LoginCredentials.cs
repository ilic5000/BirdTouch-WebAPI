using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BirdTouchWebAPI.Models
{
    public class LoginCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Description { get; set; }
    }
}
