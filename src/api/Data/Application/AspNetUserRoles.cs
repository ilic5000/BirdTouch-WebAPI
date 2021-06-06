using System;
using System.Collections.Generic;

namespace BirdTouchWebAPI.Data.Application
{
    public partial class AspNetUserRoles
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

        public AspNetRoles Role { get; set; }
        public AspNetUsers User { get; set; }
    }
}
