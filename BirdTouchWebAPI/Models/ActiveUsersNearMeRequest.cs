namespace BirdTouchWebAPI.Models
{
    public class ActiveUsersNearMeRequest
    {
        public int? ActiveMode { get; set; }
        /// <summary>
        /// in meters
        /// </summary>
        public int? RadiusOfSearch { get; set; }
    }
}
