using MySql.Data.MySqlClient;

public sealed class MySqlAccountStorage : IAccountStorage
{
    private readonly string _conn;
    public string StrategyName => "MYSQL";

    public MySqlAccountStorage(string conn)
    {
        _conn = conn;
    }

    private MySqlConnection Open()
    {
        var c = new MySqlConnection(_conn);
        c.Open();
        return c;
    }

    public bool IsAvailable()
    {
        try
        {
            using var c = Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<AccountRecord> Dump()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "select account_number, bank_code, balance from accounts";

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            yield return new AccountRecord(
                r.GetInt64(0),
                r.GetString(1),
                r.GetInt64(2)
            );
        }
    }

    public void Restore(IEnumerable<AccountRecord> data)
    {
        using var c = Open();

        foreach (var a in data)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"
                insert into accounts(account_number, bank_code, balance)
                values(@a,@b,@bal)
                on duplicate key update balance=@bal
            ";

            cmd.Parameters.AddWithValue("@a", a.AccountNumber);
            cmd.Parameters.AddWithValue("@b", a.BankCode);
            cmd.Parameters.AddWithValue("@bal", a.Balance);

            cmd.ExecuteNonQuery();
        }
    }

    public void Load() { }
    public void Save() { }

    public long OpenAccount(string bank)
    {
        bank = BankCodeUtil.Normalize(bank);
        using var c = Open();

        long id;
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = @"
                select ifnull(max(account_number),10000)+1
                from accounts
                where bank_code=@b
            ";
            cmd.Parameters.AddWithValue("@b", bank);
            id = Convert.ToInt64(cmd.ExecuteScalar());
        }

        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = @"
                insert into accounts(account_number, bank_code, balance)
                values(@a,@b,0)
            ";
            cmd.Parameters.AddWithValue("@a", id);
            cmd.Parameters.AddWithValue("@b", bank);
            cmd.ExecuteNonQuery();
        }

        return id;
    }

    public void Remove(long account, string bank)
    {
        bank = BankCodeUtil.Normalize(bank);
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
            delete from accounts
            where account_number=@a and bank_code=@b
        ";
        cmd.Parameters.AddWithValue("@a", account);
        cmd.Parameters.AddWithValue("@b", bank);
        cmd.ExecuteNonQuery();
    }

    public void Deposit(long account, string bank, long amount)
    {
        bank = BankCodeUtil.Normalize(bank);
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
            update accounts
            set balance = balance + @v
            where account_number=@a and bank_code=@b
        ";
        cmd.Parameters.AddWithValue("@v", amount);
        cmd.Parameters.AddWithValue("@a", account);
        cmd.Parameters.AddWithValue("@b", bank);
        cmd.ExecuteNonQuery();
    }

    public void Withdraw(long account, string bank, long amount)
        => Deposit(account, bank, -amount);

    public long Balance(long account, string bank)
    {
        bank = BankCodeUtil.Normalize(bank);
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
            select balance
            from accounts
            where account_number=@a and bank_code=@b
        ";
        cmd.Parameters.AddWithValue("@a", account);
        cmd.Parameters.AddWithValue("@b", bank);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public long TotalBalance()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "select ifnull(sum(balance),0) from accounts";
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public long AccountCount()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "select count(*) from accounts";
        return Convert.ToInt64(cmd.ExecuteScalar());
    }
}
