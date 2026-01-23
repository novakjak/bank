public sealed class BankStorage
{
    private static BankStorage? _instance;
    private readonly IAccountStorage _storage;
    private readonly string _bankCode;

    private BankStorage(IAccountStorage storage)
    {
        _storage = storage;
        _bankCode = BankCodeUtil.Normalize(NetworkListener.LocalAddr);
        _storage.Load();
    }

    public static void Init(IAccountStorage storage)
    {
        if (_instance != null)
            throw new InvalidOperationException();
        _instance = new BankStorage(storage);
    }

    public static BankStorage Get()
        => _instance ?? throw new InvalidOperationException();

    public long OpenAccount()
        => _storage.OpenAccount(_bankCode);

    public void Deposit(long acc, long amount)
    {
        _storage.Deposit(acc, _bankCode, amount);
        _storage.Save();
    }

    public void Withdraw(long acc, long amount)
    {
        _storage.Withdraw(acc, _bankCode, amount);
        _storage.Save();
    }

    public long Balance(long acc)
        => _storage.Balance(acc, _bankCode);

    public void Remove(long acc)
    {
        _storage.Remove(acc, _bankCode);
        _storage.Save();
    }

    public long TotalBalance()
        => _storage.TotalBalance();

    public long AccountCount()
        => _storage.AccountCount();

    public void Save()
        => _storage.Save();
}
