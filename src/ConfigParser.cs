// This file was taken and adapted from my bittorrent project

using System;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.Configuration;

public interface IConfig<T>
{
    public abstract static T Get();
}

[Config]
public sealed class Config : IConfig<Config>
{
    private static Config? _instance;

    public string InfoLogFile { get; set; } = Logger.DefaultInfoLogFile;
    public string ErrorLogFile { get; set; } = Logger.DefaultErrorLogFile;

    [Section("networking")]
    public int Timeout { get; set; } = 5;

    [Section("networking")]
    public int? DefaultPort { get; set; } = null;

    private Config() {}

    public static Config Get()
    {
        _instance ??= new Config();
        return _instance;
    }
}

public sealed class ConfigParser
{
    public static T Parse<T>(string path) where T : IConfig<T>
    {
        var ini = new ConfigurationBuilder()
            .AddIniFile(path, true)
            .Build();

        var type = typeof(T);
        if (!type.IsDefined(typeof(ConfigAttribute)))
            throw new InvalidOperationException("Cannot parse a non config class");

        var conf = T.Get();
        foreach (var prop in type.GetProperties())
        {
            try
            {
                var key = prop.Name;
                var section = prop.GetCustomAttribute<SectionAttribute>();
                if (section != null)
                    key = $"{section.Section}:{key}";

                var value = ini.GetValue(prop.PropertyType, key);
                if (value != null)
                    prop.SetValue(conf, value);
            }
            catch {}
        }
        return conf;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class ConfigAttribute : Attribute {}

[AttributeUsage(AttributeTargets.Property)]
public class SectionAttribute : Attribute
{
    public string Section { get; }
    public SectionAttribute(string section) => Section = section;
}
