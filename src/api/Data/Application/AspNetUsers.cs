using System;
using System.Collections.Generic;

namespace BirdTouchWebAPI.Data.Application
{
    public partial class AspNetUsers
    {
        public AspNetUsers()
        {
            ActiveUsers = new HashSet<ActiveUsers>();
            AspNetUserClaims = new HashSet<AspNetUserClaims>();
            AspNetUserLogins = new HashSet<AspNetUserLogins>();
            AspNetUserRoles = new HashSet<AspNetUserRoles>();
            AspNetUserTokens = new HashSet<AspNetUserTokens>();
            BusinessInfo = new HashSet<BusinessInfo>();
            SavedBusinessFkSavedContact = new HashSet<SavedBusiness>();
            SavedBusinessFkUser = new HashSet<SavedBusiness>();
            SavedPrivateFkSavedContact = new HashSet<SavedPrivate>();
            SavedPrivateFkUser = new HashSet<SavedPrivate>();
            UserInfo = new HashSet<UserInfo>();
        }

        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string ConcurrencyStamp { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }

        public ICollection<ActiveUsers> ActiveUsers { get; set; }
        public ICollection<AspNetUserClaims> AspNetUserClaims { get; set; }
        public ICollection<AspNetUserLogins> AspNetUserLogins { get; set; }
        public ICollection<AspNetUserRoles> AspNetUserRoles { get; set; }
        public ICollection<AspNetUserTokens> AspNetUserTokens { get; set; }
        public ICollection<BusinessInfo> BusinessInfo { get; set; }
        public ICollection<SavedBusiness> SavedBusinessFkSavedContact { get; set; }
        public ICollection<SavedBusiness> SavedBusinessFkUser { get; set; }
        public ICollection<SavedPrivate> SavedPrivateFkSavedContact { get; set; }
        public ICollection<SavedPrivate> SavedPrivateFkUser { get; set; }
        public ICollection<UserInfo> UserInfo { get; set; }
    }
}
