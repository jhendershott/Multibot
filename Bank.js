const { Pool } = require('pg')
process.env.NODE_TLS_REJECT_UNAUTHORIZED = 0
const pool = new Pool({
    connectionString: 'postgres://otaezimgjezlqz:e1229aee93dabd15a5747c19cb63857c3669137d147df681c5d501f1df2e6a70@ec2-52-87-58-157.compute-1.amazonaws.com:5432/d7rat0uua58ok5',
    ssl: true,
})

class Bank {

    async Deposit(user,amount){
        var currentBal = parseInt(await this.GetBankBalance());
        var newBalance = currentBal + parseInt(amount)
        await pool.query(`Update Bank Set Balance = ${newBalance} Where account_id = 1`).then(res =>{
            const data = res.rows;
        })
        console.log('deposit successful - updating user transaction');
        await this.UpdateUserTransaction(user, amount)
        return newBalance
    }

    async Withdraw(amount){
        var currentBal = parseInt(await this.GetBankBalance());
        var newBalance = currentBal - parseInt(amount)
        await pool.query(`Update Bank Set Balance = ${newBalance} Where account_id = 1`).then(res =>{
            const data = res.rows;
        })

        return newBalance
    }

    async GetBankBalance(){
        var balance;
        await pool.query(`select balance from bank where account_id = 1`).then(res =>{
            const data = res.rows;
            balance = data[0].balance;        
        });    
        //return balance[0].amount;
        return balance
    }

    async GetBalanceNewConnect(){
        var balance = await this.GetBankBalance();
        return balance;
    }

    async AddMember(name){
        console.log('adding member')
        await pool.query(`Insert Into mcmember(username) values('${name}')`).then(res =>{
            const data = res.rows;
            console.log(data);
        });
    }

    async GetMemberId(name){
        var user = null;
        await pool.query(`select user_id from mcmember where username = '${name}'`).then(res =>{
            const data = res.rows;
            if(data.length > 0)
                user = data[0].user_id
            });

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
    async UpdateUserTransaction(name, amount){
        var userId = await this.GetMemberId(name);
        console.log(userId);
        if(!userId){
            await this.AddMember(name);
        }

        userId = await this.GetMemberId(name);
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