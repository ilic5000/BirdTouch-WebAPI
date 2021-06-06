using System;
using System.Collections.Generic;

namespace BirdTouchWebAPI.Data.Application
{
    public partial class ActiveUsers
    {
        public Guid Id { get; set; }
        public Guid FkUserId { get; set; }
        public decimal? LocationLatitude { get; set; }
        public decimal? LocationLongitude { get; set; }
        public int? ActiveMode { get; set; }
        public DateTime DatetimeLastUpdate { get; set; }

        public AspNetUsers FkUser { get; set; }
    }
}
