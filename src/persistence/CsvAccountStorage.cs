public sealed class CsvAccountStorage : IAccountStorage
{
    public bool IsAvailable() => true;
    public string StrategyName => "CSV";

    private readonly string _path;
    private readonly Dictionary<(long,string), long> _accounts = new();

    public CsvAccountStorage(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    public void Load()
    {
        if (!File.Exists(_path)) return;

        foreach (var line in File.ReadAllLines(_path).Skip(1))
        {
            var p = line.Split(',');
            _accounts[(long.Parse(p[0]), p[1])] = long.Parse(p[2]);
        }
    }

    public void Save()
    {
        using var w = new StreamWriter(_path);
        w.WriteLine("account,bank,balance");
        foreach (var a in _accounts)
            w.WriteLine($"{a.Key.Item1},{a.Key.Item2},{a.Value}");
    }

    public IEnumerable<AccountRecord> Dump()
    {
        foreach (var a in _accounts)
            yield return new AccountRecord(a.Key.Item1, a.Key.Item2, a.Value);
    }

    public long OpenAccount(string bank)
    {
        bank = BankCodeUtil.Normalize(bank);
        var id = _accounts.Keys
            .Where(k => k.Item2 == bank)
            .Select(k => k.Item1)
            .DefaultIfEmpty(10000)
            .Max() + 1;

        _accounts[(id, bank)] = 0;
        return id;
    }

    public void Deposit(long a, string b, long c)
    {
        b = BankCodeUtil.Normalize(b);
        _accounts[(a,b)] += c;
    }

    public void Withdraw(long a, string b, long c)
    {
        b = BankCodeUtil.Normalize(b);
        _accounts[(a,b)] -= c;
    }

    public long Balance(long a, string b)
    {
        b = BankCodeUtil.Normalize(b);
        return _accounts[(a,b)];
    }

    public void Remove(long a, string b)
    {
        b = BankCodeUtil.Normalize(b);
        _accounts.Remove((a,b));
    }

    public long TotalBalance() => _accounts.Values.Sum();
    public long AccountCount() => _accounts.Count;

    public void Restore(IEnumerable<AccountRecord> data)
    {
        _accounts.Clear();
        foreach (var a in data)
            _accounts[(a.AccountNumber, a.BankCode)] = a.Balance;
    }
}
