using System.Reflection;
using System.Net;
using System.Net.Sockets;

public enum MsgType : int
{
    BC,
    BA,
    BN,
    AC,
    AD,
    AW,
    AB,
    AR,
    RP,
    ER,
}

public static class MsgTypeExtensions
{
    public static IBankMsg MsgFromString(this MsgType type, string str) => type switch
    {
        MsgType.BC => new BankCode().FromString(str),
        MsgType.BA => new BankAmount().FromString(str),
        MsgType.BN => new BankNumber().FromString(str),
        MsgType.AC => new AccountCreate().FromString(str),
        MsgType.AD => new AccountDeposit().FromString(str),
        MsgType.AW => new AccountWithdraw().FromString(str),
        MsgType.AB => new AccountBalance().FromString(str),
        MsgType.AR => new AccountRemove().FromString(str),
        MsgType.RP => new RobberyPlan().FromString(str),
        _ => throw new InvalidOperationException("Response type is invalid"),
    };
    public static IBankMsg MsgFromString(this string str)
    {
        if (str.Length < 2)
            throw new ArgumentException("Unknown message");
        var typeStr = String.Join("", str.Take(2));
        MsgType type;
        try
        {
            type = (MsgType)Enum.Parse(typeof(MsgType), typeStr.ToUpper());
        }
        catch
        {
            throw new ArgumentException($"Unrecognized message {typeStr}");
        }
        return type.MsgFromString(str);
    }
    public static IBankMsg RespFromString(this MsgType type, string str) => type switch
    {
        MsgType.BC => new BankCodeResp().FromString(str),
        MsgType.BA => new BankAmountResp().FromString(str),
        MsgType.BN => new BankNumberResp().FromString(str),
        MsgType.AC => new AccountCreateResp().FromString(str),
        MsgType.AD => new AccountDepositResp().FromString(str),
        MsgType.AW => new AccountWithdrawResp().FromString(str),
        MsgType.AB => new AccountBalanceResp().FromString(str),
        MsgType.AR => new AccountRemoveResp().FromString(str),
        MsgType.RP => new RobberyPlanResp().FromString(str),
        MsgType.ER => new ErrorResp().FromString(str),
        _ => throw new InvalidOperationException("Response type is invalid"),
    };
}

public interface IBankMsg
{
    static virtual MsgType Type { get; }
    MsgType GetMsgType();
    IBankMsg FromString(string str);
    string ToString();
    IBankMsg? Handle(BankConnection bc);
}
public interface IMsgWithDetails : IBankMsg
{
    public long Account { get; set; }
    public IPAddress Code { get; set; }
}
public interface IMsgWithAmount : IBankMsg
{
    public long Amount { get; set; }
}
public interface IMsgWithString : IBankMsg
{
    public string Str { get; set; }
}
public interface IMsgWithIpAddr : IBankMsg
{
    public IPAddress Addr { get; set; }
}

public class BankMsg<T> : IBankMsg where T: IBankMsg, new()
{
    public static MsgType Type { get {
        var cls = typeof(T);
        var t = cls.GetProperty("Type", typeof(MsgType))!.GetValue(null, null)!;
        return (MsgType)t;
    }}
    public MsgType GetMsgType() => T.Type;

    public virtual IBankMsg? Handle(BankConnection bc)
            => throw new NotImplementedException($"Handling of message {T.Type} was not implemented");

    public virtual IBankMsg FromString(string str)
    {
        BankMsg<T>.ConsumeType(ref str);
        if (str.Length > 0)
            throw new ArgumentException("Too many arguments specified");
        return this;
    }

    public bool TryProxy(IPAddress to, BankConnection bc)
    {
        if (to == NetworkListener.LocalAddr)
            return false;
        bc.RaiseProxyRequest(to, this);
        return true;
    }

    private static void ConsumeType(ref string str)
    {
        str = str.Trim();
        if (str.Length != 2)
            throw new ArgumentException("Message has incorrect format.");
        var typeStr = String.Join("", str.Take(2)).ToUpper();
        if (typeStr != T.Type.ToString())
            throw new ArgumentException($"Cannot parse message as {T.Type}");
        str = String.Empty;
    }

    public override string ToString() => $"{T.Type.ToString()}";
}
public class BankResp<T>: BankMsg<T> where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public override IBankMsg? Handle(BankConnection bc) => null;
}
public class Contains<T, U> : BankMsg<U> where U: IBankMsg, new() where T: Contains<T, U>, new()
{
    new public static MsgType Type => U.Type;
    public U InnerMsg { get; set; }

    public Contains() => InnerMsg = new U();

    public override IBankMsg? Handle(BankConnection bc)
    {
        if (InnerMsg is null)
            throw new NotImplementedException($"Handling of message {U.Type} was not implemented");
        return InnerMsg.Handle(bc);
    }
    public override IBankMsg FromString(string str)
    {
        InnerMsg.FromString(str);
        return this;
    }

    public override string ToString() => InnerMsg.ToString();
}
public class MsgWithDetails<T> : Contains<MsgWithDetails<T>, T>, IMsgWithDetails  where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public long Account { get; set; }
    public IPAddress Code { get; set; } = IPAddress.None;

    public override IBankMsg FromString(string str)
    {
        str = str.Trim();
        var (number, addr) = MsgWithDetails<T>.ConsumeAccountDetails(ref str);
        Account = number;
        Code = addr;
        InnerMsg.FromString(str);
        return this;
    }

    private static (long, IPAddress) ConsumeAccountDetails(ref string str)
    {
        char[] separators = {' ', '\t', '\v'};
        var parts = str.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Count() < 1)
            throw new ArgumentException("Message has wrong format.");
        var accountDetailsStr = parts.Last();
        str = str.Substring(0, str.Length - accountDetailsStr.Length);
        var accountDetails = accountDetailsStr.Split('/');
        if (accountDetails.Count() != 2)
            throw new ArgumentException("Account has wrong format.");
        long number;
        try
        {
            number = Int64.Parse(accountDetails[0]);
        }
        catch
        {
            throw new ArgumentException("Cannot parse account number");
        }
        IPAddress addr;
        try
        {
            addr = IPAddress.Parse(accountDetails[1]);
        }
        catch
        {
            throw new ArgumentException("Cannot parse IP address");
        }
        if (addr.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("IP Address is in an incorrect format");
        return (number, addr);
    }

    public override string ToString() => $"{InnerMsg.ToString()} {Account}/{Code}";
}
public class MsgWithAmount<T> : Contains<MsgWithAmount<T>, T>, IMsgWithAmount  where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public long Amount { get; set; }

    public override IBankMsg FromString(string str)
    {
        str = str.Trim();
        var amount = MsgWithAmount<T>.ConsumeAmount(ref str);
        Amount = amount;
        InnerMsg.FromString(str);
        return this;
    }

    private static long ConsumeAmount(ref string str)
    {
        char[] separators = {' ', '\t', '\v'};
        var parts = str.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Count() < 1)
            throw new ArgumentException("Message has wrong format.");
        var numStr = parts.Last();
        str = str.Substring(0, str.Length - numStr.Length);
        try
        {
            return Int64.Parse(numStr);
        }
        catch
        {
            throw new ArgumentException("Cannot parse account number");
        }
    }

    public override string ToString() => $"{InnerMsg.ToString()} {Amount}";
}
public class MsgWithString<T> : Contains<MsgWithString<T>, T>, IMsgWithString  where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public string Str { get; set; } = "";

    public override IBankMsg FromString(string str)
    {
        str = str.Trim();
        var strValue = MsgWithString<T>.ConsumeString(ref str);
        Str = strValue;
        InnerMsg.FromString(str);
        return this;
    }

    private static string ConsumeString(ref string str)
    {
        char[] separators = {' ', '\t', '\v'};
        var parts = str.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Count() < 1)
            throw new ArgumentException("Message has wrong format.");
        var msgStr = parts.Skip(1);
        str = str.Substring(0, 2);
        return String.Join(" ", msgStr);
    }

    public override string ToString() => $"{InnerMsg.ToString()} {Str}";
}
public class MsgWithIpAddr<T> : Contains<MsgWithIpAddr<T>, T>, IMsgWithIpAddr  where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public IPAddress Addr { get; set; } = IPAddress.None;

    public override IBankMsg FromString(string str)
    {
        str = str.Trim();
        var addr = MsgWithIpAddr<T>.ConsumeIpAddr(ref str);
        Addr = addr;
        InnerMsg.FromString(str);
        return this;
    }

    private static IPAddress ConsumeIpAddr(ref string str)
    {
        char[] separators = {' ', '\t', '\v'};
        var parts = str.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Count() < 1)
            throw new ArgumentException("Message has wrong format.");
        var ipStr = parts.Last();
        str = str.Substring(0, str.Length - ipStr.Length);
        IPAddress addr;
        try
        {
            addr = IPAddress.Parse(ipStr);
        }
        catch
        {
            throw new ArgumentException("Cannot parse IP address");
        }
        if (addr.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("IP Address is in an incorrect format");
        return addr;
    }

    public override string ToString() => $"{InnerMsg.ToString()} {Addr.ToString()}";
}

public sealed class BankCode : Contains<BankCode, BankMsg<BankCode>>
{
    new public static MsgType Type => MsgType.BC;
    public override IBankMsg Handle(BankConnection bc)
    {
        var resp = new BankCodeResp();
        resp.InnerMsg.Addr = NetworkListener.LocalAddr;
        return resp;
    }
}
public sealed class BankCodeResp : Contains<BankCodeResp, MsgWithIpAddr<BankResp<BankCodeResp>>>
{
    new public static MsgType Type => MsgType.BC;
    public override IBankMsg? Handle(BankConnection bc)
    {
        bc.BankIp = InnerMsg.Addr;
        Logger.Debug($"Got bank code {bc.BankIp}");
        return null;
    }
}
public sealed class BankAmount : Contains<BankAmount, BankMsg<BankAmount>>
{
    new public static MsgType Type => MsgType.BA;
    public override IBankMsg Handle(BankConnection bc)
    {
        var resp = new BankAmountResp();
        resp.InnerMsg.Amount = BankStorage.Get().TotalBalance();
        return resp;
    }
}
public sealed class BankAmountResp : Contains<BankAmountResp, MsgWithAmount<BankResp<BankAmountResp>>>
{
    new public static MsgType Type => MsgType.BA;
}
public sealed class BankNumber : Contains<BankNumber, BankMsg<BankNumber>>
{
    new public static MsgType Type => MsgType.BN;
    public override IBankMsg Handle(BankConnection bc)
    {
        var resp = new BankNumberResp();
        resp.InnerMsg.Amount = BankStorage.Get().AccountCount();
        return resp;
    }
}
public sealed class BankNumberResp : Contains<BankNumberResp, MsgWithAmount<BankResp<BankNumberResp>>>
{
    new public static MsgType Type => MsgType.BN;
}

public sealed class AccountCreate : Contains<AccountCreate, BankMsg<AccountCreate>>
{
    new public static MsgType Type => MsgType.AC;
    public override IBankMsg Handle(BankConnection bc)
    {
        var resp = new AccountCreateResp();
        resp.InnerMsg.Code = NetworkListener.LocalAddr;
        resp.InnerMsg.Account = BankStorage.Get().OpenAccount();
        return resp;
    }
}
public sealed class AccountCreateResp : Contains<AccountCreateResp, MsgWithDetails<BankResp<AccountCreateResp>>>
{
    new public static MsgType Type => MsgType.AC;
}
public sealed class AccountDeposit : Contains<AccountDeposit, MsgWithAmount<MsgWithDetails<BankMsg<AccountDeposit>>>>
{
    new public static MsgType Type => MsgType.AD;
    public override IBankMsg? Handle(BankConnection bc)
    {
        if (TryProxy(InnerMsg.InnerMsg.Code, bc))
            return null;
        var storage = BankStorage.Get();
        storage.Deposit(InnerMsg.InnerMsg.Account, InnerMsg.Amount);
        var resp = new AccountDepositResp();
        return resp;
    }
}
public sealed class AccountDepositResp : Contains<AccountDepositResp, BankResp<AccountDepositResp>>
{
    new public static MsgType Type => MsgType.AD;
}
public sealed class AccountWithdraw : Contains<AccountWithdraw, MsgWithAmount<MsgWithDetails<BankMsg<AccountWithdraw>>>>
{
    new public static MsgType Type => MsgType.AW;
    public override IBankMsg? Handle(BankConnection bc)
    {
        if (TryProxy(InnerMsg.InnerMsg.Code, bc))
            return null;
        var storage = BankStorage.Get();
        storage.Withdraw(InnerMsg.InnerMsg.Account, InnerMsg.Amount);
        var resp = new AccountWithdrawResp();
        return resp;
    }
}
public sealed class AccountWithdrawResp : Contains<AccountWithdrawResp, BankResp<AccountWithdrawResp>>
{
    new public static MsgType Type => MsgType.AW;
}
public sealed class AccountBalance : Contains<AccountBalance, MsgWithDetails<BankMsg<AccountBalance>>>
{
    new public static MsgType Type => MsgType.AB;
    public override IBankMsg? Handle(BankConnection bc)
    {
        if (TryProxy(InnerMsg.Code, bc))
            return null;
        var storage = BankStorage.Get();
        var resp = new AccountBalanceResp();
        resp.InnerMsg.Amount = storage.Balance(InnerMsg.Account);
        return resp;
    }
}
public sealed class AccountBalanceResp : Contains<AccountBalanceResp, MsgWithAmount<BankResp<AccountBalanceResp>>>
{
    new public static MsgType Type => MsgType.AB;
}
public sealed class AccountRemove : Contains<AccountRemove, MsgWithDetails<BankMsg<AccountRemove>>>
{
    new public static MsgType Type => MsgType.AR;
    public override IBankMsg? Handle(BankConnection bc)
    {
        if (TryProxy(InnerMsg.Code, bc))
            return null;
        var storage = BankStorage.Get();
        storage.Remove(InnerMsg.Account);
        var resp = new AccountRemoveResp();
        return resp;
    }
}
public sealed class AccountRemoveResp : Contains<AccountRemoveResp, BankResp<AccountRemoveResp>>
{
    new public static MsgType Type => MsgType.AR;
}

public sealed class RobberyPlan : Contains<RobberyPlan, MsgWithAmount<BankMsg<RobberyPlan>>>
{
    new public static MsgType Type => MsgType.RP;
}
public sealed class RobberyPlanResp : Contains<RobberyPlanResp, MsgWithString<BankResp<RobberyPlanResp>>>
{
    new public static MsgType Type => MsgType.RP;
}

public sealed class ErrorResp : Contains<ErrorResp, MsgWithString<BankResp<ErrorResp>>>
{
    new public static MsgType Type => MsgType.ER;
    public override IBankMsg Handle(BankConnection bc)
    {
        var resp = new ErrorResp();
        resp.InnerMsg.Str = "Unexpected error";
        return resp;
    }
}
