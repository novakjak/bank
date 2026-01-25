public sealed class InMemoryAccountStorage : IAccountStorage
{
    private readonly Dictionary<long, long> _accounts = new();
    private long _nextId = 1;

    public string StrategyName => "INMEMORY";

    // --- lifecycle ---

    public bool IsAvailable() => true;

    public void Load() { }

    public void Save() { }

    // --- accounts ---

    public long OpenAccount(string bankCode)
    {
        var id = _nextId++;
        _accounts[id] = 0;
        return id;
    }

    public void Deposit(long account, string bankCode, long amount)
    {
        EnsureExists(account);
        _accounts[account] += amount;
    }

    public void Withdraw(long account, string bankCode, long amount)
    {
        EnsureExists(account);
        _accounts[account] -= amount;
    }

    public long Balance(long account, string bankCode)
    {
        EnsureExists(account);
        return _accounts[account];
    }

    public void Remove(long account, string bankCode)
    {
        EnsureExists(account);
        _accounts.Remove(account);
    }

    // --- stats ---

    public long TotalBalance()
        => _accounts.Values.Sum();

    public long AccountCount()
        => _accounts.Count;

    // --- persistence ---

    public IEnumerable<AccountRecord> Dump()
    {
        foreach (var kv in _accounts)
        {
            yield return new AccountRecord(
                kv.Key,        // AccountNumber
                "LOCAL",       // BankCode (dummy, but valid)
                kv.Value       // Balance
            );
        }
    }

    public void Restore(IEnumerable<AccountRecord> data)
    {
        _accounts.Clear();
        _nextId = 1;

        foreach (var r in data)
        {
            _accounts[r.AccountNumber] = r.Balance;
            _nextId = Math.Max(_nextId, r.AccountNumber + 1);
        }
    }

    // --- helpers ---

    private void EnsureExists(long account)
    {
        if (!_accounts.ContainsKey(account))
            throw new InvalidOperationException($"Account {account} does not exist");
    }
}
