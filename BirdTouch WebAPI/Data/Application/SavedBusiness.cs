using System;
using System.Collections.Generic;

namespace BirdTouchWebAPI.Data.Application
{
    public partial class SavedBusiness
    {
        public Guid Id { get; set; }
        public Guid FkUserId { get; set; }
        public Guid FkSavedContactId { get; set; }
        public string Description { get; set; }

        public AspNetUsers FkSavedContact { get; set; }
        public AspNetUsers FkUser { get; set; }
    }
}
