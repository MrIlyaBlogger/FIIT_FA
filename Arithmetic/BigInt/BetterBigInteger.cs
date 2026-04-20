using System.Globalization;
using System.Numerics;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger {
    private const int KaratsubaThresholdDigits = 16;
    private const int FftThresholdDigits = 64;

    private static readonly IMultiplier SimpleMultiplierInstance = new SimpleMultiplier();
    private static readonly IMultiplier KaratsubaMultiplierInstance = new KaratsubaMultiplier();
    private static readonly IMultiplier FftMultiplierInstance = new FftMultiplier();

    private int _signBit;
    private uint _smallValue;
    private uint[]? _data;

    public bool IsNegative => _signBit == 1;

    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        InitializeFromDigits(digits, isNegative);
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        InitializeFromDigits([.. digits], isNegative);
    }

    public BetterBigInteger(string value, int radix)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        if (radix is < 2 or > 36) {
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be in range [2, 36].");
        }

        string trimmed = value.Trim();
        if (trimmed.Length == 0) {
            throw new FormatException("Empty numeric string.");
        }

        bool isNegative = false;
        int index = 0;
        if (trimmed[0] is '+' or '-') {
            isNegative = trimmed[0] == '-';
            index = 1;
        }

        if (index == trimmed.Length) {
            throw new FormatException("Sign without digits.");
        }

        BigInteger parsed = BigInteger.Zero;
        for (; index < trimmed.Length; index++) {
            int digit = ParseDigit(trimmed[index]);
            if (digit >= radix) {
                throw new FormatException($"Digit '{trimmed[index]}' is invalid for radix {radix}.");
            }

            parsed = parsed * radix + digit;
        }

        if (isNegative) {
            parsed = BigInteger.Negate(parsed);
        }

        InitializeFromBigInteger(parsed);
    }

    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }

    public int CompareTo(IBigInteger? other)
    {
        if (other is null) {
            return 1;
        }

        if (ReferenceEquals(this, other)) {
            return 0;
        }

        if (other is BetterBigInteger better) {
            return CompareCore(this, better);
        }

        return ToBigInteger().CompareTo(ParseFromDigits(other.GetDigits(), other.IsNegative));
    }

    public bool Equals(IBigInteger? other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(_signBit);
        foreach (uint digit in GetDigits()) {
            hash.Add(digit);
        }

        return hash.ToHashCode();
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b) => FromBigInteger(a.ToBigInteger() + b.ToBigInteger());

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) => FromBigInteger(a.ToBigInteger() - b.ToBigInteger());

    public static BetterBigInteger operator -(BetterBigInteger a) => FromBigInteger(BigInteger.Negate(a.ToBigInteger()));

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero) {
            throw new DivideByZeroException();
        }

        return FromBigInteger(a.ToBigInteger() / b.ToBigInteger());
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero) {
            throw new DivideByZeroException();
        }

        return FromBigInteger(a.ToBigInteger() % b.ToBigInteger());
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        IMultiplier multiplier = SelectMultiplier(a, b);
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a) => FromBigInteger(~a.ToBigInteger());

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) => FromBigInteger(a.ToBigInteger() & b.ToBigInteger());

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) => FromBigInteger(a.ToBigInteger() | b.ToBigInteger());

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) => FromBigInteger(a.ToBigInteger() ^ b.ToBigInteger());

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift) => FromBigInteger(a.ToBigInteger() << shift);

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift) => FromBigInteger(a.ToBigInteger() >> shift);

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);

    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);

    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;

    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;

    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;

    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix is < 2 or > 36) {
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be in range [2, 36].");
        }

        BigInteger value = ToBigInteger();
        if (value.IsZero) {
            return "0";
        }

        bool isNegative = value.Sign < 0;
        if (isNegative) {
            value = BigInteger.Abs(value);
        }

        Span<char> alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        List<char> chars = [];
        BigInteger divisor = radix;
        while (value > BigInteger.Zero) {
            value = BigInteger.DivRem(value, divisor, out BigInteger remainder);
            chars.Add(alphabet[(int)remainder]);
        }

        if (isNegative) {
            chars.Add('-');
        }

        chars.Reverse();
        return new string([.. chars]);
    }

    internal bool IsZero => _data is null ? _smallValue == 0u : false;

    internal int DigitCount => _data?.Length ?? 1;

    internal static BetterBigInteger FromDigits(uint[] digits, bool isNegative = false) => new(digits, isNegative);

    internal static BetterBigInteger FromBigInteger(BigInteger value)
    {
        BetterBigInteger result = new([0u]);
        result.InitializeFromBigInteger(value);
        return result;
    }

    internal BigInteger ToBigInteger() => ParseFromDigits(GetDigits(), IsNegative);

    private static IMultiplier SelectMultiplier(BetterBigInteger a, BetterBigInteger b)
    {
        int maxDigits = Math.Max(a.DigitCount, b.DigitCount);
        if (maxDigits >= FftThresholdDigits) {
            return FftMultiplierInstance;
        }

        if (maxDigits >= KaratsubaThresholdDigits) {
            return KaratsubaMultiplierInstance;
        }

        return SimpleMultiplierInstance;
    }

    private static int CompareCore(BetterBigInteger left, BetterBigInteger right)
    {
        if (left.IsNegative != right.IsNegative) {
            return left.IsNegative ? -1 : 1;
        }

        int magnitudeCompare = CompareMagnitude(left.GetDigits(), right.GetDigits());
        return left.IsNegative ? -magnitudeCompare : magnitudeCompare;
    }

    private static int CompareMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        left = TrimLeadingZeros(left);
        right = TrimLeadingZeros(right);

        if (left.Length != right.Length) {
            return left.Length.CompareTo(right.Length);
        }

        for (int i = left.Length - 1; i >= 0; i--) {
            if (left[i] != right[i]) {
                return left[i].CompareTo(right[i]);
            }
        }

        return 0;
    }

    internal static ReadOnlySpan<uint> TrimLeadingZeros(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 1 && digits[length - 1] == 0u) {
            length--;
        }

        return digits[..length];
    }

    internal static uint[] NormalizeDigits(ReadOnlySpan<uint> digits)
    {
        digits = TrimLeadingZeros(digits);
        return digits.ToArray();
    }

    private static BigInteger ParseFromDigits(ReadOnlySpan<uint> digits, bool isNegative)
    {
        digits = TrimLeadingZeros(digits);
        BigInteger result = BigInteger.Zero;
        for (int i = digits.Length - 1; i >= 0; i--) {
            result <<= 32;
            result += digits[i];
        }

        return isNegative && result != BigInteger.Zero ? BigInteger.Negate(result) : result;
    }

    private static int ParseDigit(char c)
    {
        if (c is >= '0' and <= '9') {
            return c - '0';
        }

        char upper = char.ToUpperInvariant(c);
        if (upper is >= 'A' and <= 'Z') {
            return upper - 'A' + 10;
        }

        throw new FormatException($"Invalid digit '{c}'.");
    }

    private void InitializeFromBigInteger(BigInteger value)
    {
        bool isNegative = value.Sign < 0;
        BigInteger magnitude = BigInteger.Abs(value);

        if (magnitude.IsZero) {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;
        }

        List<uint> digits = [];
        while (magnitude > BigInteger.Zero) {
            magnitude = BigInteger.DivRem(magnitude, BigInteger.One << 32, out BigInteger remainder);
            digits.Add((uint)remainder);
        }

        InitializeFromDigits([.. digits], isNegative);
    }

    private void InitializeFromDigits(ReadOnlySpan<uint> digits, bool isNegative)
    {
        uint[] normalized = NormalizeDigits(digits);
        if (normalized.Length == 1) {
            _smallValue = normalized[0];
            _data = null;
            _signBit = _smallValue == 0u ? 0 : (isNegative ? 1 : 0);
            return;
        }

        _data = normalized;
        _smallValue = 0;
        _signBit = isNegative ? 1 : 0;
    }
}
