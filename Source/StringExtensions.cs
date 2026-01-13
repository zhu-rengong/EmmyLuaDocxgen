using System.Reflection;

namespace EmmyLuaDocxgen;

public static class StringExtensions
{
    public static string Tab(this string str, int spaces) => $"{new string(' ', spaces)}{str}";
}