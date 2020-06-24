const { Pool } = require('pg')
process.env.NODE_TLS_REJECT_UNAUTHORIZED = 0
const pool = new Pool({
    connectionString: process.env.POSTGRESCONNECTIONSTRING,
    ssl: true,
})

class Bank {

    async Deposit(userName,amount,guild){
        var orgId = await this.GetOrgId(guild);
        var currentBal = parseInt(await this.GetBankBalance(orgId));
        var newBalance = currentBal + parseInt(amount);
        await pool.query(`Update Bank Set Balance = ${newBalance} Where org_id = ${orgId}`).then(res =>{
            const data = res.rows;
        })
        console.log('deposit successful - updating user transaction');
        await this.UpdateUserTransaction(userName, amount, orgId)
        return newBalance
    }

    async Withdraw(amount, guild){
        var orgId = await this.GetOrgId(guild);
        var currentBal = parseInt(await this.GetBankBalance(orgId));
        var newBalance = currentBal - parseInt(amount)
        await pool.query(`Update Bank Set Balance = ${newBalance} Where org_id = ${orgId}`).then(res =>{
            const data = res.rows;
        })

        return newBalance
    }

    async GetBankBalance(orgId){
        console.log(orgId)
        var balance;
        await pool.query(`select balance from bank where org_id = ${orgId}`).then(res =>{
            const data = res.rows;
            balance = data[0].balance;        
        });    
        //return balance[0].amount;
        return balance
    }

    async GetBalanceNewConnect(guildName){
        var orgId = await this.GetOrgId(guildName);
        var balance = await this.GetBankBalance(orgId);
        return balance;
    }

    async AddMember(name, org_id){
        console.log('adding member')
        var highestId = await this.GetHighestUserId()
        await pool.query(`Insert Into mcmember(username, org_id, user_id) values('${name}', ${org_id}, ${highestId + 1})`).then(res =>{
            const data = res.rows;
            console.log(data);
        });
    }

    async GetHighestUserId(){
        var id;
        await pool.query(`select user_id from mcmember order by user_id desc limit 1`).then(res =>{
            const data = res.rows;
            console.log(data);
            if(data.length > 0)
            id = data[0].user_id
        });

        if(id !== null){
            return id;
        }
        else{
            return null
        };
    }

    async GetOrgId(name){
        var org = null;

        await pool.query(`select id from orgs where org_name = '${name}'`).then(res =>{
            const data = res.rows;
            if(data.length > 0)
                org = data[0].id
            });

        if(org !== null){
            return org;
        }
        else{
            return null
        }
    }
    async GetMemberId(name, org_id){
        var user = null;
        console.log(`GetMemberId ${org_id}`)
        await pool.query(`select user_id from mcmember where username = '${name}' AND org_id = ${org_id}`).then(res =>{
            const data = res.rows;
            if(data.length > 0)
                user = data[0].user_id
            });
        
        console.log(`GetMemberId ${user}`)
        if(user !== null){
            return user;
        }
        else{
            return null;
        }
    }

    async GetTransactionId(user_id){
        var transId
        try{
        await pool.query(`select transaction_id from transactions where user_id = ${user_id}`).then(res =>{
            const data = res.rows;
            transId = data[0].transaction_id;   
            })
        }
        catch(e){
            transId = null
        }
        
        return transId;
    }

    async CreateNewTransaction(user_id, amount){
        var transId
        var query = `Insert Into Transactions (user_id, amount) Values (${user_id}, ${amount})`
        await pool.query(query).then(res =>{
            const data = res.rows;
            })
            
        return transId
    }
    async UpdateUserTransaction(name, amount, org_id){
        var userId = await this.GetMemberId(name, org_id);
        console.log(`UpdateUserTransaction ${userId}`)
        if(!userId){
            await this.AddMember(name, org_id);
        }

        userId = await this.GetMemberId(name, org_id);
        var transId = await this.GetTransactionId(userId);
        if(!transId){
            transId = await this.CreateNewTransaction(userId, amount);
        }
        else{
            var newAmount = parseInt(amount) + await this.GetTransactionAmount(transId)
            await pool.query(`Update transactions set amount = ${newAmount}  where transaction_id = ${transId}`).then(res =>{
                const data = res.rows;
                for(let row of data){
                };
            
            })
        }
    }

    async GetTransactionAmount(transaction_id){
        var amount

        await pool.query(`select amount from transactions where transaction_id = ${transaction_id}`).then(res =>{
            const data = res.rows;
            amount = data[0].amount;
            })
        
        return amount

    }

}

module.exports = Bank;