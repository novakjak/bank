using System.Reflection;
using System.Net;
using System.Net.Sockets;

public enum MsgType
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
        MsgType.BC => BankCode.FromString(str),
        MsgType.BA => BankAmount.FromString(str),
        MsgType.BN => BankNumber.FromString(str),
        MsgType.AC => AccountCreate.FromString(str),
        MsgType.AD => AccountDeposit.FromString(str),
        MsgType.AW => AccountWithdraw.FromString(str),
        MsgType.AB => AccountBalance.FromString(str),
        MsgType.AR => AccountRemove.FromString(str),
        MsgType.RP => RobberyPlan.FromString(str),
        _ => throw new InvalidOperationException("Msg type is invalid"),
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
}

public interface IBankMsg
{
    static abstract MsgType Type { get; }
    static abstract IBankMsg FromString(string str);
    IBankMsg Handle(BankConnection bc);
    // ToString in on object and does not need to be defined here
}
public interface IMsgWithDetails : IBankMsg
{
    public int Account { get; set; }
    public string Code { get; set; }
}
public interface IMsgWithAmount : IBankMsg
{
    public int Amount { get; set; }
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

    public virtual IBankMsg Handle(BankConnection bc)
            => throw new NotImplementedException($"Handling of message {T.Type} was not implemented");

    public static IBankMsg FromString(string str)
    {
        BankMsg<T>.ConsumeType(ref str);
        if (str.Length > 0)
            throw new ArgumentException("Too many arguments specified");
        var t = new BankMsg<T>();
        return t;
    }

    private static void ConsumeType(ref string str)
    {
        str = str.Trim();
        if (str.Length != 2)
            throw new ArgumentException("Message type has incorrect length.");
        var typeStr = String.Join("", str.Take(2)).ToUpper();
        if (typeStr != T.Type.ToString())
            throw new ArgumentException($"Cannot parse message as {T.Type}");
        str = String.Empty;
    }

    public override string? ToString() => $"{T.Type.ToString()}";
}
public class Contains<T, U> : BankMsg<U> where U: IBankMsg, new() where T: Contains<T, U>, new()
{
    public U InnerMsg { get; set; }

    public Contains() => InnerMsg = new U();

    public override IBankMsg Handle(BankConnection bc)
    {
        if (InnerMsg is null)
            throw new NotImplementedException($"Handling of message {U.Type} was");
        Console.WriteLine(this.GetType().Name);
        return InnerMsg.Handle(bc);
    }
    new public static IBankMsg FromString(string str)
    {
        var t = new T();
        t.InnerMsg = (U)U.FromString(str);
        return t;
    }

    public override string? ToString() => InnerMsg.ToString();
}
public class MsgWithDetails<T> : Contains<MsgWithDetails<T>, T>, IMsgWithDetails where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public int Account { get; set; }
    public string Code { get; set; } = "";

    new public static IBankMsg FromString(string str)
    {
        str = str.Trim();
        var (number, addr) = MsgWithDetails<T>.ConsumeAccountDetails(ref str);
        var t = (T)T.FromString(str);
        var msg = new MsgWithDetails<T>();
        msg.Account = number;
        msg.Code = addr.ToString();
        msg.InnerMsg = t;
        return msg;
    }

    public static (int, IPAddress) ConsumeAccountDetails(ref string str)
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
        int number;
        try
        {
            number = Int32.Parse(accountDetails[0]);
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

    public override string? ToString() => $"{InnerMsg.ToString()} {Account}/{Code}";
}
public class MsgWithAmount<T> : Contains<MsgWithAmount<T>, T>, IMsgWithAmount where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public int Amount { get; set; }

    new public static IBankMsg FromString(string str)
    {
        str = str.Trim();
        var amount = MsgWithAmount<T>.ConsumeAmount(ref str);
        var t = (T)T.FromString(str);
        var msg = new MsgWithAmount<T>();
        msg.Amount = amount;
        msg.InnerMsg = t;
        return msg;
    }

    public static int ConsumeAmount(ref string str)
    {
        char[] separators = {' ', '\t', '\v'};
        var parts = str.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Count() < 1)
            throw new ArgumentException("Message has wrong format.");
        var numStr = parts.Last();
        str = str.Substring(0, str.Length - numStr.Length);
        try
        {
            return Int32.Parse(numStr);
        }
        catch
        {
            throw new ArgumentException("Cannot parse account number");
        }
    }

    public override string? ToString() => $"{InnerMsg.ToString()} {Amount}";
}
public class MsgWithString<T> : Contains<MsgWithString<T>, T>, IMsgWithString where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public string Str { get; set; } = "";

    new public static IBankMsg FromString(string str)
    {
        str = str.Trim();
        var strValue = MsgWithString<T>.ConsumeString(ref str);
        Console.WriteLine(str);
        var t = (T)T.FromString(str);
        var msg = new MsgWithString<T>();
        msg.Str = strValue;
        msg.InnerMsg = t;
        return msg;
    }

    public static string ConsumeString(ref string str)
    {
        char[] separators = {' ', '\t', '\v'};
        var parts = str.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Count() < 1)
            throw new ArgumentException("Message has wrong format.");
        var msgStr = parts.Skip(1);
        str = str.Substring(0, 2);
        return String.Join(" ", msgStr);
    }

    public override string? ToString() => $"{InnerMsg.ToString()} {Str}";
}
public class MsgWithIpAddr<T> : Contains<MsgWithIpAddr<T>, T>, IMsgWithIpAddr where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public IPAddress Addr { get; set; } = IPAddress.None;

    new public static IBankMsg FromString(string str)
    {
        str = str.Trim();
        var addr = MsgWithIpAddr<T>.ConsumeIpAddr(ref str);
        var t = (T)T.FromString(str);
        var msg = new MsgWithIpAddr<T>();
        msg.Addr = addr;
        msg.InnerMsg = t;
        return msg;
    }

    public static IPAddress ConsumeIpAddr(ref string str)
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

    public override string? ToString() => $"{InnerMsg.ToString()} {Addr.ToString()}";
}

public sealed class BankCode : Contains<BankCode, BankMsg<BankCode>>
{
    new public static MsgType Type => MsgType.BC;
    public override IBankMsg Handle(BankConnection bc)
    {
        var resp = new BankCodeResp();
        resp.InnerMsg.Addr = NetworkListener.LocalAddr;
        resp.InnerMsg.InnerMsg = new BankMsg<BankCodeResp>();
        return resp;
    }
}
public sealed class BankCodeResp : Contains<BankCodeResp, MsgWithIpAddr<BankMsg<BankCodeResp>>>
{
    new public static MsgType Type => MsgType.BC;
}
public sealed class BankAmount : Contains<BankAmount, BankMsg<BankAmount>>
{
    new public static MsgType Type => MsgType.BA;
    public override IBankMsg Handle(BankConnection bc)
    {
        var resp = new BankAmountResp();
        resp.InnerMsg.Amount = BankStorage.Get().TotalBalance();
        resp.InnerMsg.InnerMsg = new BankMsg<BankAmountResp>();
        return resp;
    }
}
public sealed class BankAmountResp : Contains<BankAmountResp, MsgWithAmount<BankMsg<BankAmountResp>>>
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
        resp.InnerMsg.InnerMsg = new BankMsg<BankNumberResp>();
        return resp;
    }
}
public sealed class BankNumberResp : Contains<BankNumberResp, MsgWithAmount<BankMsg<BankNumberResp>>>
{
    new public static MsgType Type => MsgType.BN;
}

public sealed class AccountCreate : Contains<AccountCreate, BankMsg<AccountCreate>>
{
    new public static MsgType Type => MsgType.AC;
    public override IBankMsg Handle(BankConnection bc)
    {
        var resp = new AccountCreateResp();
        resp.InnerMsg.Code = NetworkListener.LocalAddr.ToString();
        resp.InnerMsg.Account = BankStorage.Get().OpenAccount();
        resp.InnerMsg.InnerMsg = new BankMsg<AccountCreateResp>();
        return resp;
    }
}
public sealed class AccountCreateResp : Contains<AccountCreateResp, MsgWithDetails<BankMsg<AccountCreateResp>>>
{
    new public static MsgType Type => MsgType.AC;
}
public sealed class AccountDeposit : Contains<AccountDeposit, MsgWithAmount<MsgWithDetails<BankMsg<AccountDeposit>>>>
{
    new public static MsgType Type => MsgType.AD;
    public override IBankMsg Handle(BankConnection bc)
    {
        var storage = BankStorage.Get();
        storage.Deposit(InnerMsg.InnerMsg.Account, InnerMsg.Amount);
        var resp = new AccountDepositResp();
        return resp;
    }
}
public sealed class AccountDepositResp : Contains<AccountDepositResp, BankMsg<AccountDepositResp>>
{
    new public static MsgType Type => MsgType.AD;
}
public sealed class AccountWithdraw : Contains<AccountWithdraw, MsgWithAmount<MsgWithDetails<BankMsg<AccountWithdraw>>>>
{
    new public static MsgType Type => MsgType.AW;
    public override IBankMsg Handle(BankConnection bc)
    {
        var storage = BankStorage.Get();
        storage.Withdraw(InnerMsg.InnerMsg.Account, InnerMsg.Amount);
        var resp = new AccountWithdrawResp();
        return resp;
    }
}
public sealed class AccountWithdrawResp : Contains<AccountWithdrawResp, BankMsg<AccountWithdrawResp>>
{
    new public static MsgType Type => MsgType.AW;
}
public sealed class AccountBalance : Contains<AccountBalance, MsgWithDetails<BankMsg<AccountBalance>>>
{
    new public static MsgType Type => MsgType.AB;
    public override IBankMsg Handle(BankConnection bc)
    {
        var storage = BankStorage.Get();
        var resp = new AccountBalanceResp();
        resp.InnerMsg.Amount = storage.Balance(InnerMsg.Account);
        resp.InnerMsg.InnerMsg = new BankMsg<AccountBalanceResp>();
        return resp;
    }
}
public sealed class AccountBalanceResp : Contains<AccountBalanceResp, MsgWithAmount<BankMsg<AccountBalanceResp>>>
{
    new public static MsgType Type => MsgType.AB;
}
public sealed class AccountRemove : Contains<AccountRemove, MsgWithDetails<BankMsg<AccountRemove>>>
{
    new public static MsgType Type => MsgType.AR;
    public override IBankMsg Handle(BankConnection bc)
    {
        var storage = BankStorage.Get();
        storage.Remove(InnerMsg.Account);
        var resp = new AccountRemoveResp();
        return resp;
    }
}
public sealed class AccountRemoveResp : Contains<AccountRemoveResp, BankMsg<AccountRemoveResp>>
{
    new public static MsgType Type => MsgType.AR;
}

public sealed class RobberyPlan : Contains<RobberyPlan, MsgWithAmount<BankMsg<RobberyPlan>>>
{
    new public static MsgType Type => MsgType.RP;
}
public sealed class RobberyPlanResp : Contains<RobberyPlanResp, MsgWithString<BankMsg<RobberyPlanResp>>>
{
    new public static MsgType Type => MsgType.RP;
}

public sealed class ErrorResp : Contains<ErrorResp, MsgWithString<BankMsg<ErrorResp>>>
{
    new public static MsgType Type => MsgType.ER;
}
