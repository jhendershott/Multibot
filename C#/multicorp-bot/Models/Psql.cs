using DSharpPlus.Entities;
using multicorp_bot.POCO;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;

namespace multicorp_bot.Models
{
    public class Psql
    {
        private NpgsqlConnection Connection;
        public Psql()
        {
            string connectionString = Environment.GetEnvironmentVariable("POSTGRESCONNECTIONSTRING");

            Connection = new NpgsqlConnection(connectionString);
            Connection.Open();
        }

        public void AddMember(string name, string orgId)
        {
            
            InsertUpdateTable($"INSERT INTO mcmember(username, org_id, user_id) VALUES('{name}', {orgId}, {int.Parse(GetHighestUserId()) + 1})");
        }

        public void AddNewTransaction(string userId, int amount)
        {
            InsertUpdateTable($"INSERT INTO Transactions (user_id, amount) VALUES ({userId}, {amount})");
        }

        public void AddOrg(DiscordGuild guild)
        {
            InsertUpdateTable($"INSERT INTO orgs (org_id, name) VALUES ({GetHighestOrgId() + 1}, '{guild.Name}')");
        }

        public string GetBankBalance(DiscordGuild guild)
        {
            var orgId = GetOrgId(guild.Name);
            return GetFirstResult($"SELECT balance FROM bank WHERE org_id = {orgId}");
        }

        public string GetMemberId(string name, string orgId)
        {
            string query = $"SELECT user_id FROM mcmember WHERE username = '{name}' AND org_id = {orgId}";
            return GetFirstResult(query);
        }

        public string GetOrgId(string orgName)
        {
            string query = $"SELECT id FROM orgs WHERE org_name = '{orgName}'";
                        
            return GetFirstResult(query);
        }

        public string GetTransactionId(string userId)
        {
            return GetFirstResult($"SELECT transaction_id FROM transactions WHERE user_id = {userId}");
        }

        public string GetTransactionValue(string transId)
        {
            return GetFirstResult($"SELECT amount FROM transactions WHERE transaction_id = {transId}");
        }

        public List<Tuple<string, string>> GetOrgTopTransactions(DiscordGuild guild)
        {
            List<Tuple<string, string>> transactionList = new List<Tuple<string, string>>();

            StringBuilder query = new StringBuilder("SELECT m.username, t.amount");
            query.Append(" FROM transactions t join mcmember m on m.user_id = t.user_id");
            query.Append($" WHERE m.org_id = {GetOrgId(guild.Name)} Order By t.amount desc Limit 5");

            var cmd = new NpgsqlCommand(query.ToString(), Connection);
            var reader = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(reader);

            foreach(DataRow row in dt.Rows){
                transactionList.Add(new Tuple<string, string>(row["username"].ToString(), row["amount"].ToString()));
            }

            return transactionList;
            
        }

        public void UpdateUserTransaction(string name, int amount, string orgId)
        {
            string userId = GetMemberId(name, orgId);
            if (userId == null)
            {
                AddMember(name, orgId);
                userId = GetMemberId(name, orgId);
            }

            string transId = GetTransactionId(userId);
            if (transId == null)
            {
                AddNewTransaction(userId, amount);
            }
            else
            {
                var newAmount = int.Parse(GetTransactionValue(transId)) + amount;
                InsertUpdateTable($"UPDATE transactions SET amount = {newAmount} WHERE transaction_id = {transId}");
            }

        }

        public string UpdateBankBalance(int amount, DiscordGuild guild)
        {
            int newBalance = amount + int.Parse(GetBankBalance(guild));
            InsertUpdateTable($"UPDATE bank SET balance = {newBalance} WHERE org_id = {GetOrgId(guild.Name)}");
            return GetBankBalance(guild);
        }

        public void UpdateNickName(string oldNick, string newNick, DiscordGuild guild)
        {
            string memberId = GetMemberId(oldNick, GetOrgId(guild.Name));
            if (memberId != null)
            {
                InsertUpdateTable($"UPDATE mcmember SET username = '{newNick}' WHERE user_id = {memberId}");
            }
            
        }

        public void WipeBank(DiscordGuild guild)
        {
            InsertUpdateTable($"UPDATE bank SET balance = 0 WHERE org_id = {GetOrgId(guild.Name)}");
        }

        public void WipeTransactions(DiscordGuild guild)
        {
            var cmd = new NpgsqlCommand($"SELECT user_id FROM members WHERE org_id = WHERE org_id = {GetOrgId(guild.Name)}", Connection);
            var reader = cmd.ExecuteReader();
            List<string> userIds = new List<string>();
            while (reader.Read())
            {
                for(int i = 0; i < reader.FieldCount; i++)
                {
                    userIds.Add(reader.GetValue(i).ToString());
                }
            }

            InsertUpdateTable($"UPDATE transactions SET amount = 0 WHERE user_id in ({string.Join(", ", userIds)})");
                            
        }

        private void InsertUpdateTable(string statement)
        {
            var cmd = new NpgsqlCommand(statement, Connection);
            cmd.ExecuteNonQuery();
        }

        private string GetHighestUserId()
        {
            return GetFirstResult("SELECT user_id FROM mcmember ORDER BY user_id desc LIMIT 1");
        }

        private string GetHighestOrgId()
        {
            return GetFirstResult("SELECT org_id FROM orgs ORDER BY org_id desc LIMIT 1");
        }


        private string GetFirstResult(string query)
        {
            var cmd = new NpgsqlCommand(query, Connection);
            var result = cmd.ExecuteScalar();
            if(result != null)
            {
                return cmd.ExecuteScalar().ToString();
            }
            else
            {
                return null; 
            }
        }
    }
}
