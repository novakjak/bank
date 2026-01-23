public sealed class HybridAccountStorage : IAccountStorage
{
    private readonly IAccountStorage _csv;
    private readonly IAccountStorage _db;
    private IAccountStorage _active;

    public string StrategyName => _active.StrategyName;

    public HybridAccountStorage(IAccountStorage csv, IAccountStorage db)
    {
        _csv = csv;
        _db = db;
        _active = _db.IsAvailable() ? _db : _csv;
    }

    private void EnsureActive()
    {
        if (_active == _csv && _db.IsAvailable())
        {
            var data = _csv.Dump();
            _db.Restore(data);
            _active = _db;
        }
        else if (_active == _db && !_db.IsAvailable())
        {
            _active = _csv;
            _csv.Load();
        }
    }

    public bool IsAvailable()
    {
        EnsureActive();
        return _active.IsAvailable();
    }

    public void Load()
    {
        EnsureActive();
        _active.Load();
    }

    public void Save()
    {
        EnsureActive();
        _active.Save();
    }

    public long OpenAccount(string bank)
    {
        EnsureActive();
        var r = _active.OpenAccount(bank);
        if (_active == _csv) _csv.Save();
        return r;
    }

    public void Deposit(long a, string b, long c)
    {
        EnsureActive();
        _active.Deposit(a, b, c);
        if (_active == _csv) _csv.Save();
    }

    public void Withdraw(long a, string b, long c)
    {
        EnsureActive();
        _active.Withdraw(a, b, c);
        if (_active == _csv) _csv.Save();
    }

    public void Remove(long a, string b)
    {
        EnsureActive();
        _active.Remove(a, b);
        if (_active == _csv) _csv.Save();
    }


    public long Balance(long account, string bank)
    {
        EnsureActive();
        return _active.Balance(account, bank);
    }

    public long TotalBalance()
    {
        EnsureActive();
        return _active.TotalBalance();
    }

    public long AccountCount()
    {
        EnsureActive();
        return _active.AccountCount();
    }

    public IEnumerable<AccountRecord> Dump()
    {
        EnsureActive();
        return _active.Dump();
    }

    public void Restore(IEnumerable<AccountRecord> data)
    {
        EnsureActive();
        _active.Restore(data);
    }
}
