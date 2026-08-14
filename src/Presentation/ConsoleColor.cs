using System;
using System.Globalization;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Represents an engine-neutral RGBA color.
/// </summary>
public sealed class ConsoleColor : IEquatable<ConsoleColor>
{
    private ConsoleColor(byte red, byte green, byte blue, byte alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    /// <summary>
    /// Gets the red channel.
    /// </summary>
    public byte Red { get; }

    /// <summary>
    /// Gets the green channel.
    /// </summary>
    public byte Green { get; }

    /// <summary>
    /// Gets the blue channel.
    /// </summary>
    public byte Blue { get; }

    /// <summary>
    /// Gets the alpha channel.
    /// </summary>
    public byte Alpha { get; }

    /// <summary>
    /// Creates a color from RGB channels.
    /// </summary>
    public static ConsoleColor FromRgb(byte red, byte green, byte blue)
    {
        return new ConsoleColor(red, green, blue, 255);
    }

    /// <summary>
    /// Creates a color from RGBA channels.
    /// </summary>
    public static ConsoleColor FromRgba(byte red, byte green, byte blue, byte alpha)
    {
        return new ConsoleColor(red, green, blue, alpha);
    }

    /// <summary>
    /// Creates a color from #RGB, #RRGGBB, or #RRGGBBAA text.
    /// </summary>
    public static ConsoleColor FromHex(string hex)
    {
        if (hex is null)
        {
            throw new ArgumentNullException(nameof(hex));
        }

        if (!hex.StartsWith("#", StringComparison.Ordinal))
        {
            throw new FormatException("Color hex values must start with '#'.");
        }

        string value = hex.Substring(1);
        if (value.Length == 3)
        {
            return new ConsoleColor(
                ParseByte(new string(value[0], 2)),
                ParseByte(new string(value[1], 2)),
                ParseByte(new string(value[2], 2)),
                255);
        }

        if (value.Length == 6 || value.Length == 8)
        {
            return new ConsoleColor(
                ParseByte(value.Substring(0, 2)),
                ParseByte(value.Substring(2, 2)),
                ParseByte(value.Substring(4, 2)),
                value.Length == 8 ? ParseByte(value.Substring(6, 2)) : (byte)255);
        }

        throw new FormatException("Color hex values must use #RGB, #RRGGBB, or #RRGGBBAA.");
    }

    /// <summary>
    /// Returns the color as #RRGGBB.
    /// </summary>
    public string ToRgbHex()
    {
        return $"#{Red:X2}{Green:X2}{Blue:X2}";
    }

    /// <summary>
    /// Returns the color as #RRGGBBAA.
    /// </summary>
    public string ToRgbaHex()
    {
        return $"#{Red:X2}{Green:X2}{Blue:X2}{Alpha:X2}";
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Alpha == 255 ? ToRgbHex() : ToRgbaHex();
    }

    /// <inheritdoc />
    public bool Equals(ConsoleColor? other)
    {
        if (other is null)
        {
            return false;
        }

        return Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as ConsoleColor);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Red;
            hash = (hash * 397) ^ Green;
            hash = (hash * 397) ^ Blue;
            hash = (hash * 397) ^ Alpha;
            return hash;
        }
    }

    private static byte ParseByte(string value)
    {
        if (!byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte result))
        {
            throw new FormatException("Color hex values can only contain hexadecimal digits.");
        }

        return result;
    }
}
