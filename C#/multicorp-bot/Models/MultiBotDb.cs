﻿using System;
using Microsoft.EntityFrameworkCore;

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
        public virtual DbSet<Mcmember> Mcmember { get; set; }
        public virtual DbSet<Orgs> Orgs { get; set; }
        public virtual DbSet<Transactions> Transactions { get; set; }
        public virtual DbSet<WantedShips> WantedShips { get; set; }
        public virtual DbSet<WorkOrderRequirements> WorkOrderRequirements { get; set; }
        public virtual DbSet<WorkOrderTypes> WorkOrderTypes { get; set; }
        public virtual DbSet<WorkOrders> WorkOrders { get; set; }

        public virtual DbSet<Loans> Loans { get; set; }

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

                entity.HasIndex(e => e.OrgId)
                    .HasName("fki_orgId");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.Balance).HasColumnName("balance");
                entity.Property(e => e.Merits).HasColumnName("merits");

                entity.Property(e => e.OrgId).HasColumnName("org_id");

                entity.HasOne(d => d.Org)
                    .WithMany(p => p.Bank)
                    .HasForeignKey(d => d.OrgId)
                    .HasConstraintName("org_fk");
            });

            modelBuilder.Entity<Mcmember>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("mcmember_pkey");

                entity.ToTable("mcmember");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.OrgId).HasColumnName("org_id");

                entity.Property(e => e.DiscordId).HasColumnName("discord_id");

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
                entity.ToTable("wantedShips");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.ImgUrl)
                    .HasColumnName("imgUrl")
                    .HasColumnType("character varying");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("character varying");

                entity.Property(e => e.OrgId).HasColumnName("org_id");

                entity.Property(e => e.RemainingPrice).HasColumnName("remainingPrice");

                entity.Property(e => e.TotalPrice).HasColumnName("totalPrice");
                entity.Property(e => e.IsCompleted).HasColumnName("isCompleted");
            });

            modelBuilder.Entity<WorkOrderRequirements>(entity =>
            {
                entity.ToTable("workOrderRequirements");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Amount).HasColumnName("amount");

                entity.Property(e => e.TypeId).HasColumnName("typeId");

                entity.Property(e => e.WorkOrderId).HasColumnName("workOrderId");
            });

            modelBuilder.Entity<WorkOrderTypes>(entity =>
            {
                entity.ToTable("workOrderTypes");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("character varying");

                entity.Property(e => e.XpModifier).HasColumnName("xpModifier");
            });

            modelBuilder.Entity<WorkOrders>(entity =>
            {
                entity.ToTable("workOrders");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnName("description")
                    .HasColumnType("character varying");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("character varying");

                entity.Property(e => e.OrgId).HasColumnName("org_id");
            });

            modelBuilder.Entity<Loans>(entity =>
            {
                entity.HasKey(e => e.LoanId)
                    .HasName("primary");

                entity.ToTable("loans");

                entity.HasIndex(e => e.ApplicantId)
                    .HasName("fki_requestor_id");

                entity.HasIndex(e => e.FunderId)
                    .HasName("fki_funder_fk");

                entity.HasIndex(e => e.OrgId)
                    .HasName("fki_org_id");

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


            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
