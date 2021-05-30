using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace BirdTouchWebAPI.Data.Application
{
    public partial class ApplicationDbContext : DbContext
    {
        public IConfiguration _configuration;

        public ApplicationDbContext(IConfiguration Configuration)
        {
            _configuration = Configuration;
        }

        public virtual DbSet<ActiveUsers> ActiveUsers { get; set; }
        public virtual DbSet<AspNetRoleClaims> AspNetRoleClaims { get; set; }
        public virtual DbSet<AspNetRoles> AspNetRoles { get; set; }
        public virtual DbSet<AspNetUserClaims> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogins> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUserRoles> AspNetUserRoles { get; set; }
        public virtual DbSet<AspNetUsers> AspNetUsers { get; set; }
        public virtual DbSet<AspNetUserTokens> AspNetUserTokens { get; set; }
        public virtual DbSet<BusinessInfo> BusinessInfo { get; set; }
        public virtual DbSet<SavedBusiness> SavedBusiness { get; set; }
        public virtual DbSet<SavedPrivate> SavedPrivate { get; set; }
        public virtual DbSet<UserInfo> UserInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActiveUsers>(entity =>
            {
                entity.ToTable("active_users");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.ActiveMode).HasColumnName("active_mode");

                entity.Property(e => e.DatetimeLastUpdate).HasColumnName("datetime_last_update");

                entity.Property(e => e.FkUserId).HasColumnName("fk_user_id");

                entity.Property(e => e.LocationLatitude)
                    .HasColumnName("location_latitude")
                    .HasColumnType("numeric(11,8)")
                    .HasDefaultValueSql("NULL::numeric");

                entity.Property(e => e.LocationLongitude)
                    .HasColumnName("location_longitude")
                    .HasColumnType("numeric(11,8)")
                    .HasDefaultValueSql("NULL::numeric");

                entity.HasOne(d => d.FkUser)
                    .WithMany(p => p.ActiveUsers)
                    .HasForeignKey(d => d.FkUserId)
                    .HasConstraintName("fk_active_users_users_id");
            });

            modelBuilder.Entity<AspNetRoleClaims>(entity =>
            {
                entity.ToTable("asp_net_role_claims");

                entity.HasIndex(e => e.RoleId)
                    .HasDatabaseName("ix_asp_net_role_claims_role_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ClaimType).HasColumnName("claim_type");

                entity.Property(e => e.ClaimValue).HasColumnName("claim_value");

                entity.Property(e => e.RoleId).HasColumnName("role_id");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetRoleClaims)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("fk_asp_net_role_claims_asp_net_roles_role_id");
            });

            modelBuilder.Entity<AspNetRoles>(entity =>
            {
                entity.ToTable("asp_net_roles");

                entity.HasIndex(e => e.NormalizedName)
                    .HasDatabaseName("role_name_index")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(256);

                entity.Property(e => e.NormalizedName)
                    .HasColumnName("normalized_name")
                    .HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetUserClaims>(entity =>
            {
                entity.ToTable("asp_net_user_claims");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("ix_asp_net_user_claims_user_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ClaimType).HasColumnName("claim_type");

                entity.Property(e => e.ClaimValue).HasColumnName("claim_value");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserClaims)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("fk_asp_net_user_claims_asp_net_users_user_id");
            });

            modelBuilder.Entity<AspNetUserLogins>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

                entity.ToTable("asp_net_user_logins");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("ix_asp_net_user_logins_user_id");

                entity.Property(e => e.LoginProvider).HasColumnName("login_provider");

                entity.Property(e => e.ProviderKey).HasColumnName("provider_key");

                entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserLogins)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("fk_asp_net_user_logins_asp_net_users_user_id");
            });

            modelBuilder.Entity<AspNetUserRoles>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.ToTable("asp_net_user_roles");

                entity.HasIndex(e => e.RoleId)
                    .HasDatabaseName("ix_asp_net_user_roles_role_id");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.RoleId).HasColumnName("role_id");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("fk_asp_net_user_roles_asp_net_roles_role_id");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("fk_asp_net_user_roles_asp_net_users_user_id");
            });

            modelBuilder.Entity<AspNetUsers>(entity =>
            {
                entity.ToTable("asp_net_users");

                entity.HasIndex(e => e.NormalizedEmail)
                    .HasDatabaseName("email_index");

                entity.HasIndex(e => e.NormalizedUserName)
                    .HasDatabaseName("user_name_index")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");

                entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(256);

                entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");

                entity.Property(e => e.LockoutEnabled).HasColumnName("lockout_enabled");

                entity.Property(e => e.LockoutEnd)
                    .HasColumnName("lockout_end")
                    .HasColumnType("timestamp with time zone");

                entity.Property(e => e.NormalizedEmail)
                    .HasColumnName("normalized_email")
                    .HasMaxLength(256);

                entity.Property(e => e.NormalizedUserName)
                    .HasColumnName("normalized_user_name")
                    .HasMaxLength(256);

                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");

                entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");

                entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");

                entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp");

                entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");

                entity.Property(e => e.UserName)
                    .HasColumnName("user_name")
                    .HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetUserTokens>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

                entity.ToTable("asp_net_user_tokens");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.LoginProvider).HasColumnName("login_provider");

                entity.Property(e => e.Name).HasColumnName("name");

                entity.Property(e => e.Value).HasColumnName("value");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserTokens)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("fk_asp_net_user_tokens_asp_net_users_user_id");
            });

            modelBuilder.Entity<BusinessInfo>(entity =>
            {
                entity.ToTable("business_info");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Adress).HasColumnName("adress");

                entity.Property(e => e.Companyname).HasColumnName("companyname");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Email).HasColumnName("email");

                entity.Property(e => e.FkUserId).HasColumnName("fk_user_id");

                entity.Property(e => e.Phonenumber).HasColumnName("phonenumber");

                entity.Property(e => e.Profilepicturedata).HasColumnName("profilepicturedata");

                entity.Property(e => e.Website).HasColumnName("website");

                entity.HasOne(d => d.FkUser)
                    .WithMany(p => p.BusinessInfo)
                    .HasForeignKey(d => d.FkUserId)
                    .HasConstraintName("fk_business_info_users_id");
            });

            modelBuilder.Entity<SavedBusiness>(entity =>
            {
                entity.ToTable("saved_business");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.FkSavedContactId).HasColumnName("fk_saved_contact_id");

                entity.Property(e => e.FkUserId).HasColumnName("fk_user_id");

                entity.HasOne(d => d.FkSavedContact)
                    .WithMany(p => p.SavedBusinessFkSavedContact)
                    .HasForeignKey(d => d.FkSavedContactId)
                    .HasConstraintName("fk_saved_business_saved_contact_users_id");

                entity.HasOne(d => d.FkUser)
                    .WithMany(p => p.SavedBusinessFkUser)
                    .HasForeignKey(d => d.FkUserId)
                    .HasConstraintName("fk_ssaved_business_users_id");
            });

            modelBuilder.Entity<SavedPrivate>(entity =>
            {
                entity.ToTable("saved_private");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.FkSavedContactId).HasColumnName("fk_saved_contact_id");

                entity.Property(e => e.FkUserId).HasColumnName("fk_user_id");

                entity.HasOne(d => d.FkSavedContact)
                    .WithMany(p => p.SavedPrivateFkSavedContact)
                    .HasForeignKey(d => d.FkSavedContactId)
                    .HasConstraintName("fk_saved_private_saved_contact_users_id");

                entity.HasOne(d => d.FkUser)
                    .WithMany(p => p.SavedPrivateFkUser)
                    .HasForeignKey(d => d.FkUserId)
                    .HasConstraintName("fk_saved_private_users_id");
            });

            modelBuilder.Entity<UserInfo>(entity =>
            {
                entity.ToTable("user_info");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Adress).HasColumnName("adress");

                entity.Property(e => e.Dateofbirth).HasColumnName("dateofbirth");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Email).HasColumnName("email");

                entity.Property(e => e.Fblink).HasColumnName("fblink");

                entity.Property(e => e.Firstname).HasColumnName("firstname");

                entity.Property(e => e.FkUserId).HasColumnName("fk_user_id");

                entity.Property(e => e.Gpluslink).HasColumnName("gpluslink");

                entity.Property(e => e.Lastname).HasColumnName("lastname");

                entity.Property(e => e.Linkedinlink).HasColumnName("linkedinlink");

                entity.Property(e => e.Phonenumber).HasColumnName("phonenumber");

                entity.Property(e => e.Profilepicturedata).HasColumnName("profilepicturedata");

                entity.Property(e => e.Twlink).HasColumnName("twlink");

                entity.HasOne(d => d.FkUser)
                    .WithMany(p => p.UserInfo)
                    .HasForeignKey(d => d.FkUserId)
                    .HasConstraintName("fk_user_info_users_id");
            });

            modelBuilder.HasSequence("asp_net_role_claims_id_seq");

            modelBuilder.HasSequence("asp_net_user_claims_id_seq");
        }
    }
}
