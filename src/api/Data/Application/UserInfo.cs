using System;
using System.Collections.Generic;

namespace BirdTouchWebAPI.Data.Application
{
    public partial class UserInfo
    {
        public Guid Id { get; set; }
        public Guid FkUserId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Phonenumber { get; set; }
        public string Dateofbirth { get; set; }
        public string Adress { get; set; }
        public string Fblink { get; set; }
        public string Twlink { get; set; }
        public string Gpluslink { get; set; }
        public string Linkedinlink { get; set; }
        public string Description { get; set; }
        public byte[] Profilepicturedata { get; set; }

        public AspNetUsers FkUser { get; set; }
    }
}
