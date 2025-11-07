using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace SSC.Reflection;

/// <summary>
/// Type converter
/// </summary>
public static class TypeConverter
{
    /// <summary>
    /// Try get value as <paramref name="type"/> from JToken
    /// </summary>
    /// <param name="type">Value type</param>
    /// <param name="token">Input</param>
    /// <param name="hint">Value type hint</param>
    /// <param name="outError">Output error message</param>
    /// <param name="outValue">Output parameter</param>
    /// <returns></returns>
    public static bool TryGetValueAsFromJToken(Type type, JToken token, string hint, out string? outError, ref object outValue)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(token);

        outError = null;

        if (type == typeof(ulong))
        {
            if (token.Type == JTokenType.Integer) outValue = token.Value<ulong>();
            else outError = $"Value {hint} is expected to be ulong";
        }
        else if (type == typeof(long))
        {
            if (token.Type == JTokenType.Integer) outValue = token.Value<long>();
            else outError = $"Value {hint} is expected to be long";
        }
        else if (type == typeof(uint))
        {
            if (token.Type == JTokenType.Integer) outValue = token.Value<uint>();
            else outError = $"Value {hint} is expected to be uint";
        }
        else if (type == typeof(int))
        {
            if (token.Type == JTokenType.Integer) outValue = token.Value<int>();
            else outError = $"Value {hint} is expected to be int";
        }
        else if (type == typeof(ushort))
        {
            if (token.Type == JTokenType.Integer) outValue = token.Value<ushort>();
            else outError = $"Value {hint} is expected to be ushort";
        }
        else if (type == typeof(short))
        {
            if (token.Type == JTokenType.Integer) outValue = token.Value<short>();
            else outError = $"Value {hint} is expected to be short";
        }
        else if (type == typeof(byte))
        {
            if (token.Type == JTokenType.Integer) outValue = token.Value<byte>();
            else outError = $"Value {hint} is expected to be byte";
        }
        else if (type == typeof(sbyte))
        {
            if (token.Type == JTokenType.Integer) outValue = token.Value<sbyte>();
            else outError = $"Value {hint} is expected to be sbyte";
        }
        else if (type == typeof(bool))
        {
            if (token.Type == JTokenType.Boolean) outValue = token.Value<bool>();
            else outError = $"Value {hint} is expected to be bool";
        }
        else if (type == typeof(string))
        {
            if (token.Type == JTokenType.String) outValue = token.Value<string>()!;
            else outError = $"Value {hint} is expected to be string";
        }
        else if (type == typeof(float))
        {
            if (token.Type == JTokenType.Float) outValue = token.Value<float>();
            else outError = $"Value {hint} is expected to be float";
        }
        else if (type == typeof(double))
        {
            if (token.Type == JTokenType.Float) outValue = token.Value<double>();
            else outError = $"Value {hint} is expected to be double";
        }
        else if (type.BaseType == typeof(Enum))
        {
            if (token.Type == JTokenType.String)
            {
                if (Enum.TryParse(type, token.Value<string>(), true, out var l_EnumValue))
                    outValue = l_EnumValue;
                else
                {
                    outError = $"Unrecognized constant \"{token.Value<string>()}\" for parameter {hint}, candidates are: "
                                        + string.Join(", ", Enum.GetNames(type));
                }
            }
            else outError = $"Parameter {hint} is expected to be string/enum";
        }
        else
            outError = $"Unhandled value type {type.FullName} for {hint}";

        return string.IsNullOrEmpty(outError);
    }
    /// <summary>
    /// Try get value as <paramref name="type"/> from string
    /// </summary>
    /// <param name="type">Value type</param>
    /// <param name="input">Input</param>
    /// <param name="hint">Value type hint</param>
    /// <param name="outError">Output error message</param>
    /// <param name="outValue">Output parameter</param>
    /// <returns></returns>
    public static bool TryGetValueAsFromString(Type type, string input, string? hint, out string? outError, ref object outValue)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(input);

        outError = null;

        if (type == typeof(ulong))
        {
            if (ulong.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be ulong";
        }
        else if (type == typeof(long))
        {
            if (long.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be long";
        }
        else if (type == typeof(uint))
        {
            if (uint.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be uint";
        }
        else if (type == typeof(int))
        {
            if (int.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be int";
        }
        else if (type == typeof(ushort))
        {
            if (ushort.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be ushort";
        }
        else if (type == typeof(short))
        {
            if (short.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be short";
        }
        else if (type == typeof(byte))
        {
            if (byte.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be byte";
        }
        else if (type == typeof(sbyte))
        {
            if (sbyte.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be sbyte";
        }
        else if (type == typeof(bool))
        {
            if (bool.TryParse(input, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be bool";
        }
        else if (type == typeof(string))
        {
            outValue = input;
        }
        else if (type == typeof(float))
        {
            if (float.TryParse(input, CultureInfo.InvariantCulture, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be float";
        }
        else if (type == typeof(double))
        {
            if (double.TryParse(input, CultureInfo.InvariantCulture, out var l_Parsed)) outValue = l_Parsed;
            else outError = $"Value {hint ?? string.Empty} is expected to be double";
        }
        else if (type.BaseType == typeof(Enum))
        {
            if (Enum.TryParse(type, input, true, out var l_Parsed))
                outValue = l_Parsed;
            else
            {
                outError = $"Unrecognized constant \"{input}\" for parameter {hint ?? string.Empty}, candidates are: "
                                    + string.Join(", ", Enum.GetNames(type));
            }
        }
        else
            outError = $"Unhandled value type {type.FullName} for {hint ?? string.Empty}";

        return string.IsNullOrEmpty(outError);
    }
}
