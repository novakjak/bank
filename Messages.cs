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
        MsgType.BC => BankCode.FromString(ref str),
        MsgType.BA => BankAmount.FromString(ref str),
        MsgType.BN => BankNumber.FromString(ref str),
        MsgType.AC => AccountCreate.FromString(ref str),
        MsgType.AD => AccountDeposit.FromString(ref str),
        MsgType.AW => AccountWithdraw.FromString(ref str),
        MsgType.AB => AccountBalance.FromString(ref str),
        MsgType.AR => AccountRemove.FromString(ref str),
        MsgType.RP => RobberyPlan.FromString(ref str),
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
    static abstract IBankMsg FromString(ref string str);
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
    public T? InnerMsg { get; set; }

    public P GetProp<P>(string name)
    {
        var cls = typeof(T);
        var prop = cls.GetProperty(name, typeof(P))!.GetValue(this, null)!;
        return (P)prop;
    }

    public static IBankMsg FromString(ref string str)
    {
        BankMsg<T>.ConsumeType(ref str);
        if (str.Length > 0)
            throw new ArgumentException("Too many arguments specified");
        var t = new BankMsg<T>();
        t.InnerMsg = new T();
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
public class MsgWithDetails<T> : BankMsg<T>, IMsgWithDetails where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public int Account { get; set; }
    public string Code { get; set; } = "";
    new public T? InnerMsg { get; set; }

    new public static IBankMsg FromString(ref string str)
    {
        str = str.Trim();
        var (number, addr) = MsgWithDetails<T>.ConsumeAccountDetails(ref str);
        var t = (T)T.FromString(ref str);
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

    public override string? ToString() => $"{InnerMsg!.ToString()} {Account}/{Code}";
}
public class MsgWithAmount<T> : BankMsg<T>, IMsgWithAmount where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public int Amount { get; set; }
    new public T? InnerMsg { get; set; }

    new public static IBankMsg FromString(ref string str)
    {
        str = str.Trim();
        var amount = MsgWithAmount<T>.ConsumeAmount(ref str);
        var t = (T)T.FromString(ref str);
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

    public override string? ToString() => $"{InnerMsg!.ToString()} {Amount}";
}
public class MsgWithString<T> : BankMsg<T>, IMsgWithString where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public string Str { get; set; } = "";
    new public T? InnerMsg { get; set; }

    new public static IBankMsg FromString(ref string str)
    {
        str = str.Trim();
        var strValue = MsgWithString<T>.ConsumeString(ref str);
        Console.WriteLine(str);
        var t = (T)T.FromString(ref str);
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

    public override string? ToString() => $"{InnerMsg!.ToString()} {Str}";
}
public class MsgWithIpAddr<T> : BankMsg<T>, IMsgWithIpAddr where T: IBankMsg, new()
{
    new public static MsgType Type => T.Type;
    public IPAddress Addr { get; set; } = IPAddress.None;
    new public T? InnerMsg { get; set; }

    new public static IBankMsg FromString(ref string str)
    {
        str = str.Trim();
        var addr = MsgWithIpAddr<T>.ConsumeIpAddr(ref str);
        var t = (T)T.FromString(ref str);
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

    public override string? ToString() => $"{InnerMsg!.ToString()} {Addr.ToString()}";
}

public sealed class BankCode : BankMsg<BankCode>
{
    new public static MsgType Type => MsgType.BC;
}
public sealed class BankCodeResp : MsgWithIpAddr<BankMsg<BankCode>>
{
    new public static MsgType Type => MsgType.BC;
}
public sealed class BankAmount : BankMsg<BankAmount>
{
    new public static MsgType Type => MsgType.BA;
}
public sealed class BankAmountResp : MsgWithAmount<BankMsg<BankAmount>>
{
    new public static MsgType Type => MsgType.BA;
}
public sealed class BankNumber : BankMsg<BankNumber>
{
    new public static MsgType Type => MsgType.BN;
}
public sealed class BankNumberResp : MsgWithAmount<BankMsg<BankNumber>>
{
    new public static MsgType Type => MsgType.BN;
}

public sealed class AccountCreate : BankMsg<AccountCreate>
{
    new public static MsgType Type => MsgType.AC;
}
public sealed class AccountCreateResp : MsgWithDetails<BankMsg<AccountCreate>>
{
    new public static MsgType Type => MsgType.AC;
}
public sealed class AccountDeposit : MsgWithAmount<MsgWithDetails<BankMsg<AccountDeposit>>>
{
    new public static MsgType Type => MsgType.AD;
}
public sealed class AccountDepositResp : BankMsg<AccountDeposit>
{
    new public static MsgType Type => MsgType.AD;
}
public sealed class AccountWithdraw : MsgWithAmount<MsgWithDetails<BankMsg<AccountWithdraw>>>
{
    new public static MsgType Type => MsgType.AW;
}
public sealed class AccountWithdrawResp :BankMsg<AccountWithdraw>
{
    new public static MsgType Type => MsgType.AW;
}
public sealed class AccountBalance : MsgWithDetails<BankMsg<AccountBalance>>
{
    new public static MsgType Type => MsgType.AB;
}
public sealed class AccountBalanceResp : MsgWithAmount<BankMsg<AccountBalance>>
{
    new public static MsgType Type => MsgType.AB;
}
public sealed class AccountRemove : MsgWithDetails<BankMsg<AccountRemove>>
{
    new public static MsgType Type => MsgType.AR;
}
public sealed class AccountRemoveResp : BankMsg<AccountRemove>
{
    new public static MsgType Type => MsgType.AR;
}

public sealed class RobberyPlan : MsgWithAmount<BankMsg<RobberyPlan>>
{
    new public static MsgType Type => MsgType.RP;
}
public sealed class RobberyPlanResp : MsgWithString<BankMsg<RobberyPlan>>
{
    new public static MsgType Type => MsgType.RP;
}

public sealed class ErrorResp : MsgWithString<BankMsg<ErrorResp>>
{
    new public static MsgType Type => MsgType.ER;
}
