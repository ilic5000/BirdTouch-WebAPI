using System;
using System.Collections.Generic;

namespace BirdTouchWebAPI.Data.Application
{
    public partial class AspNetRoleClaims
    {
        public int Id { get; set; }
        public Guid RoleId { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }

        public AspNetRoles Role { get; set; }
    }
}
