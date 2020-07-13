using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);



        //public void AddNewTransaction(string userId, int amount)
        //{
        //    InsertUpdateTable($"INSERT INTO Transactions (user_id, amount) VALUES ({userId}, {amount})");
        //}

        //public string GetBankBalance(DiscordGuild guild)
        //{
        //    var orgId = GetOrgId(guild.Name);
        //    return GetFirstResult($"SELECT balance FROM bank WHERE org_id = {orgId}");
        //}

        //public string GetTransactionId(string userId)
        //{
        //    return GetFirstResult($"SELECT transaction_id FROM transactions WHERE user_id = {userId}");
        //}

        //public string GetTransactionValue(string transId)
        //{
        //    return GetFirstResult($"SELECT amount FROM transactions WHERE transaction_id = {transId}");
        //}

        //public List<Tuple<string, string>> GetOrgTopTransactions(DiscordGuild guild)
        //{
        //    List<Tuple<string, string>> transactionList = new List<Tuple<string, string>>();

        //    StringBuilder query = new StringBuilder("SELECT m.username, t.amount");
        //    query.Append(" FROM transactions t join mcmember m on m.user_id = t.user_id");
        //    query.Append($" WHERE m.org_id = {GetOrgId(guild.Name)} Order By t.amount desc Limit 5");

        //    var cmd = new NMultiBotDbCommand(query.ToString(), Connection);
        //    var reader = cmd.ExecuteReader();
        //    DataTable dt = new DataTable();
        //    dt.Load(reader);

        //    foreach(DataRow row in dt.Rows){
        //        transactionList.Add(new Tuple<string, string>(row["username"].ToString(), row["amount"].ToString()));
        //    }

        //    return transactionList;

        //}

        //public void UpdateUserTransaction(string name, int amount, string orgId)
        //{
        //    string userId = GetMemberId(name, orgId);
        //    if (userId == null)
        //    {
        //        AddMember(name, orgId);
        //        userId = GetMemberId(name, orgId);
        //    }

        //    string transId = GetTransactionId(userId);
        //    if (transId == null)
        //    {
        //        AddNewTransaction(userId, amount);
        //    }
        //    else
        //    {
        //        var newAmount = int.Parse(GetTransactionValue(transId)) + amount;
        //        InsertUpdateTable($"UPDATE transactions SET amount = {newAmount} WHERE transaction_id = {transId}");
        //    }

        //}

        //public string UpdateBankBalance(int amount, DiscordGuild guild)
        //{
        //    int newBalance = amount + int.Parse(GetBankBalance(guild));
        //    InsertUpdateTable($"UPDATE bank SET balance = {newBalance} WHERE org_id = {GetOrgId(guild.Name)}");
        //    return GetBankBalance(guild);
        //}

        //public void UpdateNickName(string oldNick, string newNick, DiscordGuild guild)
        //{
        //    string memberId = GetMemberId(oldNick, GetOrgId(guild.Name));
        //    if (memberId != null)
        //    {
        //        InsertUpdateTable($"UPDATE mcmember SET username = '{newNick}' WHERE user_id = {memberId}");
        //    }

        //}

    }
}
