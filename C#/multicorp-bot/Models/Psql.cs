using DSharpPlus.Entities;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace multicorp_bot.Models
{
    public class Psql
    {
        private readonly NpgsqlConnection Connection;
        public Psql()
        {
            Connection = new NpgsqlConnection(
                Environment.GetEnvironmentVariable("POSTGRESCONNECTIONSTRING"));
        }

        public void AddMember(string name, int orgId)
        {
            InsertUpdateTable($"INSERT INTO mcmember(username, org_id, user_id) VALUES('${name}', ${orgId}, ${GetHighestUserId() + 1})");
        }

        public void AddNewTransaction(string userId, int amount)
        {
            InsertUpdateTable($"INSERT INTO Transactions (user_id, amount) VALUES (${userId}, ${amount})");
        }

        public void AddOrg(DiscordGuild guid)
        {
            
        }

        public void UpdateUserTransaction(string name, int amount, int orgId)
        {
            string userId = GetMemberId(name, orgId);
            if(userId == null)
            {
                AddMember(name, orgId);
                userId = GetMemberId(name, orgId);
            }

            string transId = GetTransactionId(userId);
            if(transId == null)
            {
                AddNewTransaction(userId, amount);
            }
            else
            {
                var newAmount = int.Parse(GetTransactionValue(transId)) + amount;
                InsertUpdateTable($"UPDATE transactions SET amount = ${amount} WHERE transaction_id = ${transId}");
            }
            
        }

        public string GetBankBalance(DiscordGuild guild)
        {
            var orgId = GetOrgId(guild.Name);
            return GetFirstResult($"SELECT balance FROM bank WHERE org_id = {orgId}");
        }

        private string GetHighestUserId()
        {
            return GetFirstResult("SELECT user_id FROM mcmember ORDER BY user_id desc LIMIT 1");
        }

        private string GetHighestOrgId()
        {
            return GetFirstResult("SELECT org_id FROM orgs ORDER BY org_id desc LIMIT 1");
        }

        public string GetMemberId(string name, int orgId)
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
            return GetFirstResult($"SELECT amount FROM transactions WHERE transaction_id = ${transId}");
        }
        


        private void InsertUpdateTable(string statement)
        {
            Connection.Open();

            var cmd = new NpgsqlCommand(statement, Connection);
            cmd.ExecuteNonQuery();

            Connection.Close();
        }


        private string GetFirstResult(string query)
        {
            Connection.Open();
            var cmd = new NpgsqlCommand(query, Connection);
            string result = cmd.ExecuteScalar().ToString();
            Connection.Close();
            return result;
        }
    }
}
