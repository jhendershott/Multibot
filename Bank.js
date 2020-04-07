const { Pool } = require('pg')
process.env.NODE_TLS_REJECT_UNAUTHORIZED = 0
const pool = new Pool({
    connectionString: 'postgres://otaezimgjezlqz:e1229aee93dabd15a5747c19cb63857c3669137d147df681c5d501f1df2e6a70@ec2-52-87-58-157.compute-1.amazonaws.com:5432/d7rat0uua58ok5',
    ssl: true,
})

class Bank {

    async Deposit(user,amount){
        var currentBal = parseInt(await this.GetBankBalance());
        console.log(currentBal)
        var newBalance = currentBal + parseInt(amount)
        console.log(newBalance);
        await pool.query(`Update Bank Set Balance = ${newBalance} Where account_id = 1`).then(res =>{
            const data = res.rows;
            for(let row of data){
                console.log(row);
            }  
        })

        await this.UpdateUserTransaction(user, amount)
        return newBalance
    }

    async Withdraw(amount){
        var currentBal = parseInt(await this.GetBankBalance());
        console.log(currentBal)
        var newBalance = currentBal - parseInt(amount)
        console.log(newBalance);
        await pool.query(`Update Bank Set Balance = ${newBalance} Where account_id = 1`).then(res =>{
            const data = res.rows;
            for(let row of data){
                console.log(row);
            }  
        })

        return newBalance
    }

    async GetBankBalance(){
        var balance;
        await pool.query(`select balance from bank where account_id = 1`).then(res =>{
            const data = res.rows;
            balance = data[0].balance
        
        });    
          
        //return balance[0].amount;
        return balance
    }

    async GetBalanceNewConnect(){
        balance = await this.GetBankBalance();
        return balance;
    }

    async AddMember(name){
       await pool.query(`Insert Into mcmember(username) values('${name}')`).then(res =>{
        const data = res.rows;
        balance = data[0].balance
        });
    }

    async GetMemberId(name){
        var user;
        await pool.query(`select user_id from mcmember where username = '${name}'`).then(res =>{
            const data = res.rows;
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

        await pool.query(`Insert Into Transactions (user_id, amount) Values (${user_id}, ${amount})`).then(res =>{
            const data = res.rows;
            })
            
        return transId
    }
    async UpdateUserTransaction(name, amount){
        var userId = this.GetMemberId(name);
        if(userId === null){
            this.AddMember(name);
        }

        userId = await this.GetMemberId(name);
        var transId = await this.GetTransactionId(userId);
        console.log(transId);
        if(transId === undefined || transId === null){
            console.log('creating new transaction')
            transId = await this.CreateNewTransaction(userId, amount);
        }
        else{
            console.log('updating transaction')
            var newAmount = parseInt(amount) + await this.GetTransactionAmount(transId)
            console.log(newAmount)
            await pool.query(`Update transactions set amount = ${newAmount}  where transaction_id = ${transId}`).then(res =>{
                const data = res.rows;
                console.log('UpdateUserTransaction');
                for(let row of data){
                    console.log(row)
                };
            
            })
        }
    }

    async GetTransactionAmount(transaction_id){
        var amount

        await pool.query(`select amount from transactions where transaction_id = ${transaction_id}`).then(res =>{
            const data = res.rows;
            console.log('GetTransactionAmount');
            amount = data[0].amount;
            console.log(amount);
            })
        
        return amount

    }

}

module.exports = Bank;