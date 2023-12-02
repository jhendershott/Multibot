using System;
using Microsoft.EntityFrameworkCore;
using multicorp_bot.Models.DbModels;

namespace multicorp_bot
{
    public partial class MultiBotDb : DbContext
    {
        public MultiBotDb()
        {
        }

        public MultiBotDb(DbContextOptions<MultiBotDb> options)
            : base(options)
        {
        }

        public virtual DbSet<Bank> Bank { get; set; }
        public virtual DbSet<DispatchLog> DispatchLog { get; set; }
        public virtual DbSet<DispatchType> DispatchType { get; set; }
        public virtual DbSet<Expenses> Expenses { get; set; }
        public virtual DbSet<Factions> Factions { get; set; }
        public virtual DbSet<FactionFavor> FactionFavors { get; set; }
        public virtual DbSet<Loans> Loans { get; set; }
        public virtual DbSet<Member> Member { get; set; }
        public virtual DbSet<Orgs> Orgs { get; set; }
        public virtual DbSet<OrgDispatch> OrgDispatch { get; set; }
        public virtual DbSet<Transactions> Transactions { get; set; }
        public virtual DbSet<WantedShips> WantedShips { get; set; }
        public virtual DbSet<WorkOrderRequirements> WorkOrderRequirements { get; set; }
        public virtual DbSet<WorkOrderTypes> WorkOrderTypes { get; set; }
        public virtual DbSet<WorkOrders> WorkOrders { get; set; }
        public virtual DbSet<WorkOrderMembers> WorkOrderMembers { get; set; }
     

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRESCONNECTIONSTRING"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bank>(entity =>
            {
                entity.HasKey(e => e.AccountId)
                    .HasName("bank_pkey");

                entity.ToTable("bank");

                entity.Property(e => e.AccountId).HasColumnName("account_id")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.Balance).HasColumnName("balance");
                entity.Property(e => e.Merits).HasColumnName("merits");

                entity.Property(e => e.OrgId).HasColumnName("org_id");
                entity.Property(e => e.IsRp)
                    .IsRequired()
                    .HasColumnName("is_rp")
                    .HasColumnType("boolean");

                entity.HasOne(d => d.Org)
                    .WithMany(p => p.Bank)
                    .HasForeignKey(d => d.OrgId)
                    .HasConstraintName("org_fk");
            });

            modelBuilder.Entity<DispatchLog>(entity =>
            {
                entity.ToTable("dispatch_log");

                entity.Property(e => e.Id)
                    .HasColumnName("id").ValueGeneratedOnAdd();

                entity.Property(e => e.RequestorName)
                    .IsRequired()
                    .HasColumnName("requestor_name");

                entity.Property(e => e.RequestorOrg)
                    .IsRequired()
                    .HasColumnName("requestor_org");

                entity.Property(e => e.AcceptorName)
                    .HasColumnName("acceptor_name");

                entity.Property(e => e.AcceptorOrg)
                    .HasColumnName("acceptor_org");
            });

            modelBuilder.Entity<DispatchType>(entity =>
            {
                entity.ToTable("dispatch_type");

                entity.Property(e => e.DispatchTypeId)
                    .IsRequired()
                    .HasColumnName("dispatch_type_id");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnName("description");
            });

            modelBuilder.Entity<Expenses>(entity =>
            {
                entity.ToTable("expenses");
                entity.HasKey(e => e.Id).HasName("primary");

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasColumnName("id")
                    .HasColumnType("integer");

                entity.Property(e => e.OrgId)
                    .IsRequired()
                    .HasColumnName("org_id")
                    .HasColumnType("integer");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("character varying");

                entity.Property(e => e.Amount)
                    .IsRequired()
                    .HasColumnName("amount")
                    .HasColumnType("bigint");

                entity.Property(e => e.Remaining)
                    .IsRequired()
                    .HasColumnName("remaining")
                    .HasColumnType("bigint");

                entity.Property(e => e.Period)
                    .IsRequired()
                    .HasColumnName("period")
                    .HasColumnType("integer");

                entity.Property(e => e.NumPeriods)
                    .HasColumnName("num_periods")
                    .HasColumnType("integer");
            });

            modelBuilder.Entity<Factions>(entity =>
            {
                entity.HasKey(e => e.FactionId)
                    .HasName("primary");

                entity.ToTable("factions");

                entity.Property(e => e.FactionId)
                    .IsRequired()
                    .HasColumnName("faction_id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name");

                entity.Property(e => e.OrgId)
                    .IsRequired()
                    .HasColumnName("org_id");
            });

            modelBuilder.Entity<FactionFavor>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("primary");

                entity.ToTable("faction_favor");

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasColumnName("id");

                entity.Property(e => e.FactionID)
                    .IsRequired()
                    .HasColumnName("faction_id");

                entity.Property(e => e.OrgId)
                    .IsRequired()
                    .HasColumnName("org_id");

                entity.Property(e => e.FavorPoints)
                    .IsRequired()
                    .HasColumnName("favor_points");
            });


            modelBuilder.Entity<Loans>(entity =>
            {
                entity.HasKey(e => e.LoanId)
                    .HasName("primary");

                entity.ToTable("loans");

                entity.HasIndex(e => e.ApplicantId)
                    .HasDatabaseName("fki_requestor_id");

                entity.HasIndex(e => e.FunderId)
                    .HasDatabaseName("fki_funder_fk");

                entity.HasIndex(e => e.OrgId)
                    .HasDatabaseName("fki_org_id");

                entity.Property(e => e.LoanId)
                    .HasColumnName("loan_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.ApplicantId).HasColumnName("applicant_id");

                entity.Property(e => e.IsCompleted).HasColumnName("is_completed");

                entity.Property(e => e.Status).HasColumnName("status");

                entity.Property(e => e.FunderId).HasColumnName("funder_id");

                entity.Property(e => e.InterestAmount).HasColumnName("interest_amount");

                entity.Property(e => e.OrgId).HasColumnName("org_id");

                entity.Property(e => e.RemainingAmount).HasColumnName("remaining_amount");

                entity.Property(e => e.RequestedAmount).HasColumnName("requested_amount");

            });

            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("member_pkey");

                entity.ToTable("member");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.OrgId)
                    .IsRequired()
                    .HasColumnName("org_id");

                entity.Property(e => e.DiscordId)
                    .IsRequired()
                    .HasColumnName("discord_id");

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("username")
                    .HasMaxLength(50);

                entity.Property(e => e.Xp).HasColumnName("xp");
            });

            modelBuilder.Entity<Orgs>(entity =>
            {
                entity.ToTable("orgs");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.OrgName)
                    .IsRequired()
                    .HasColumnName("org_name")
                    .HasColumnType("character varying");

                entity.Property(e => e.DiscordId)
                    .HasColumnName("discord_id")
                    .HasColumnType("character varying");

                entity.Property(e => e.IsRp)
                   .HasColumnName("is_rp")
                   .HasColumnType("boolean");
            });

            modelBuilder.Entity<OrgDispatch>(entity =>
            {
                entity.ToTable("org_dispatch");

                entity.Property(e => e.OrgDispatchId)
                    .IsRequired()
                    .HasColumnName("org_dispatch_id");

                entity.Property(e => e.OrgId)
                    .IsRequired()
                    .HasColumnName("org_id");

                entity.Property(e => e.DispatchType)
                    .IsRequired()
                    .HasColumnName("dispatch_type_id");
            });

            modelBuilder.Entity<Transactions>(entity =>
            {
                entity.HasKey(e => e.TransactionId)
                    .HasName("transactions_pkey");

                entity.ToTable("transactions");

                entity.Property(e => e.TransactionId).HasColumnName("transaction_id");

                entity.Property(e => e.Amount).HasColumnName("amount");

                entity.Property(e => e.Merits).HasColumnName("merits");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("transactions_user_id_fkey");
            });

            modelBuilder.Entity<WantedShips>(entity =>
            {
                entity.ToTable("wanted_ships");

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.ImgUrl)
                    .HasColumnName("img_url")
                    .HasColumnType("character varying");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("character varying");

                entity.Property(e => e.OrgId).HasColumnName("org_id");

                entity.Property(e => e.RemainingPrice).HasColumnName("remaining_price");

                entity.Property(e => e.TotalPrice).HasColumnName("total_price");
                entity.Property(e => e.IsCompleted).HasColumnName("is_completed");
            });

            modelBuilder.Entity<WorkOrders>(entity =>
            {
                entity.ToTable("work_orders");

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnName("description")
                    .HasColumnType("character varying");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("character varying");

                entity.Property(e => e.OrgId).HasColumnName("org_id");

                entity.Property(e => e.WorkOrderTypeId).HasColumnName("work_order_type_id");

                entity.Property(e => e.isCompleted).HasColumnName("is_completed");

                entity.Property(e => e.Location)
                    .HasColumnName("location")
                    .HasColumnType("character varying");

                entity.Property(e => e.FactionId)
                    .HasColumnName("faction_id");
            });

            modelBuilder.Entity<WorkOrderMembers>(entity =>
            {
                entity.ToTable("work_order_members");

                entity.Property(e => e.Id)
                    .HasColumnName("id").ValueGeneratedOnAdd();

                entity.Property(e => e.MemberId)
                    .IsRequired()
                    .HasColumnName("member_id");

                entity.Property(e => e.WorkOrderId).HasColumnName("work_order_id");
            });

            modelBuilder.Entity<WorkOrderRequirements>(entity =>
            {
                entity.ToTable("work_order_requirements");

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Amount).HasColumnName("amount");

                entity.Property(e => e.TypeId).HasColumnName("type_id");

                entity.Property(e => e.WorkOrderId).HasColumnName("work_order_id");

                entity.Property(e => e.Material).HasColumnName("material");

                entity.Property(e => e.isCompleted).HasColumnName("is_completed");
            });

            modelBuilder.Entity<WorkOrderTypes>(entity =>
            {
                entity.ToTable("work_order_types");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("character varying");

                entity.Property(e => e.XpModifier).HasColumnName("xp_modifier");
                entity.Property(e => e.CreditModifier).HasColumnName("credit_modifier");

                entity.Property(e => e.ImgUrl)
                    .HasColumnName("img_url")
                    .HasColumnType("character varying"); ;
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
