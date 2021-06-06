using System;
using System.Collections.Generic;

namespace BirdTouchWebAPI.Data.Application
{
    public partial class BusinessInfo
    {
        public Guid Id { get; set; }
        public Guid FkUserId { get; set; }
        public string Companyname { get; set; }
        public string Email { get; set; }
        public string Phonenumber { get; set; }
        public string Website { get; set; }
        public string Adress { get; set; }
        public string Description { get; set; }
        public byte[] Profilepicturedata { get; set; }

        public AspNetUsers FkUser { get; set; }
    }
}
