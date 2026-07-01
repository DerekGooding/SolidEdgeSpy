namespace SpyNet10.Extensions;

public static class EnumExtensions
{
    public static bool IsSet<T>(this T value, T flags) where T : struct
    {
        var type = typeof(T);

        // only works with enums
        if (!type.IsEnum) throw new ArgumentException("The type parameter T must be an enum type");

        // handle each underlying type
        var numberType = Enum.GetUnderlyingType(type);

        return numberType.Equals(typeof(int))
        ? BoxUnbox<int>(value, flags, (a, b) => (a & b) == b)
        : numberType.Equals(typeof(sbyte))
        ? BoxUnbox<sbyte>(value, flags, (a, b) => (a & b) == b)
        : numberType.Equals(typeof(byte))
        ? BoxUnbox<byte>(value, flags, (a, b) => (a & b) == b)
        : numberType.Equals(typeof(short))
        ? BoxUnbox<short>(value, flags, (a, b) => (a & b) == b)
        : numberType.Equals(typeof(ushort))
        ? BoxUnbox<ushort>(value, flags, (a, b) => (a & b) == b)
        : numberType.Equals(typeof(uint))
        ? BoxUnbox<uint>(value, flags, (a, b) => (a & b) == b)
        : numberType.Equals(typeof(long))
        ? BoxUnbox<long>(value, flags, (a, b) => (a & b) == b)
        : numberType.Equals(typeof(ulong))
        ? BoxUnbox<ulong>(value, flags, (a, b) => (a & b) == b)
        : numberType.Equals(typeof(char))
        ? BoxUnbox<char>(value, flags, (a, b) => (a & b) == b)
        : throw new ArgumentException("Unknown enum underlying type " +
                    numberType.Name + "");
    }

    /// <summary>
    /// Helper function for handling the value types Boxes the params to
    /// object so that the cast can be called on them
    /// </summary>
    private static bool BoxUnbox<T>(object value, object flags, Func<T, T, bool> op) => op((T)value, (T)flags);
}