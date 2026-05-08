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

        uint[] parsed = [0u];
        for (; index < trimmed.Length; index++) {
            int digit = ParseDigit(trimmed[index]);
            if (digit >= radix) {
                throw new FormatException($"Digit '{trimmed[index]}' is invalid for radix {radix}.");
            }

            parsed = AddMagnitude(MultiplyMagnitudeByUInt(parsed, (uint)radix), [(uint)digit]);
        }

        InitializeFromDigits(parsed, isNegative);
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

        int signCompare = IsNegative.CompareTo(other.IsNegative);
        if (signCompare != 0) {
            return IsNegative ? -1 : 1;
        }

        int magnitudeCompare = CompareMagnitude(GetDigits(), other.GetDigits());
        return IsNegative ? -magnitudeCompare : magnitudeCompare;
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

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.IsNegative == b.IsNegative) {
            return FromDigits(AddMagnitude(a.GetDigits(), b.GetDigits()), a.IsNegative);
        }

        int compare = CompareMagnitude(a.GetDigits(), b.GetDigits());
        if (compare == 0) {
            return FromDigits([0u]);
        }

        return compare > 0
            ? FromDigits(SubtractMagnitude(a.GetDigits(), b.GetDigits()), a.IsNegative)
            : FromDigits(SubtractMagnitude(b.GetDigits(), a.GetDigits()), b.IsNegative);
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a + -b;
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return a.IsZero ? FromDigits([0u]) : FromDigits(a.GetDigits().ToArray(), !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero) {
            throw new DivideByZeroException();
        }

        (uint[] quotient, _) = DivideMagnitude(a.GetDigits(), b.GetDigits());
        return FromDigits(quotient, a.IsNegative ^ b.IsNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero) {
            throw new DivideByZeroException();
        }

        (_, uint[] remainder) = DivideMagnitude(a.GetDigits(), b.GetDigits());
        return FromDigits(remainder, a.IsNegative);
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        IMultiplier multiplier = SelectMultiplier(a, b);
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        int length = a.DigitCount + 1;
        uint[] words = ToTwosComplement(a, length);
        for (int i = 0; i < words.Length; i++) {
            words[i] = ~words[i];
        }

        return FromTwosComplement(words);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) => Bitwise(a, b, static (x, y) => x & y);

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) => Bitwise(a, b, static (x, y) => x | y);

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) => Bitwise(a, b, static (x, y) => x ^ y);

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift < 0) {
            return a >> -shift;
        }

        return FromDigits(ShiftLeftMagnitude(a.GetDigits(), shift), a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift < 0) {
            return a << -shift;
        }

        if (!a.IsNegative) {
            return FromDigits(ShiftRightMagnitude(a.GetDigits(), shift));
        }

        uint[] shifted = ShiftRightMagnitude(a.GetDigits(), shift);
        if (HasAnyLowerBit(a.GetDigits(), shift)) {
            shifted = AddMagnitude(shifted, [1u]);
        }

        return FromDigits(shifted, isNegative: true);
    }

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

        if (IsZero) {
            return "0";
        }

        Span<char> alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        uint[] value = GetDigits().ToArray();
        List<char> chars = [];
        while (!IsZeroMagnitude(value)) {
            (value, uint remainder) = DivideMagnitudeByUInt(value, (uint)radix);
            chars.Add(alphabet[(int)remainder]);
        }

        if (IsNegative) {
            chars.Add('-');
        }

        chars.Reverse();
        return new string([.. chars]);
    }

    internal bool IsZero => _data is null ? _smallValue == 0u : false;

    internal int DigitCount => _data?.Length ?? 1;

    internal static BetterBigInteger FromDigits(uint[] digits, bool isNegative = false) => new(digits, isNegative);

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

    private static bool IsZeroMagnitude(ReadOnlySpan<uint> digits)
    {
        digits = TrimLeadingZeros(digits);
        return digits.Length == 1 && digits[0] == 0u;
    }

    private static uint[] AddMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        left = TrimLeadingZeros(left);
        right = TrimLeadingZeros(right);
        int length = Math.Max(left.Length, right.Length);
        uint[] result = new uint[length + 1];
        ulong carry = 0UL;

        for (int i = 0; i < length; i++) {
            ulong sum = carry;
            if (i < left.Length) {
                sum += left[i];
            }

            if (i < right.Length) {
                sum += right[i];
            }

            result[i] = (uint)sum;
            carry = sum >> 32;
        }

        result[length] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static uint[] SubtractMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        left = TrimLeadingZeros(left);
        right = TrimLeadingZeros(right);
        if (CompareMagnitude(left, right) < 0) {
            throw new ArgumentException("Left magnitude must be greater than or equal to right magnitude.");
        }

        uint[] result = new uint[left.Length];
        long borrow = 0L;

        for (int i = 0; i < left.Length; i++) {
            long diff = (long)left[i] - (i < right.Length ? right[i] : 0L) - borrow;
            if (diff < 0) {
                diff += 1L << 32;
                borrow = 1L;
            }
            else {
                borrow = 0L;
            }

            result[i] = (uint)diff;
        }

        return NormalizeDigits(result);
    }

    private static uint[] MultiplyMagnitudeByUInt(ReadOnlySpan<uint> digits, uint multiplier)
    {
        digits = TrimLeadingZeros(digits);
        if (multiplier == 0u || IsZeroMagnitude(digits)) {
            return [0u];
        }

        uint[] result = new uint[digits.Length + 1];
        ulong carry = 0UL;
        for (int i = 0; i < digits.Length; i++) {
            ulong product = (ulong)digits[i] * multiplier + carry;
            result[i] = (uint)product;
            carry = product >> 32;
        }

        result[digits.Length] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static (uint[] Quotient, uint Remainder) DivideMagnitudeByUInt(ReadOnlySpan<uint> digits, uint divisor)
    {
        if (divisor == 0u) {
            throw new DivideByZeroException();
        }

        digits = TrimLeadingZeros(digits);
        uint[] quotient = new uint[digits.Length];
        ulong remainder = 0UL;

        for (int i = digits.Length - 1; i >= 0; i--) {
            ulong value = (remainder << 32) | digits[i];
            quotient[i] = (uint)(value / divisor);
            remainder = value % divisor;
        }

        return (NormalizeDigits(quotient), (uint)remainder);
    }

    private static (uint[] Quotient, uint[] Remainder) DivideMagnitude(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor)
    {
        dividend = TrimLeadingZeros(dividend);
        divisor = TrimLeadingZeros(divisor);

        if (IsZeroMagnitude(divisor)) {
            throw new DivideByZeroException();
        }

        int compare = CompareMagnitude(dividend, divisor);
        if (compare < 0) {
            return ([0u], dividend.ToArray());
        }

        if (compare == 0) {
            return ([1u], [0u]);
        }

        uint[] quotient = [0u];
        uint[] remainder = dividend.ToArray();
        int divisorBits = GetBitLength(divisor);

        while (CompareMagnitude(remainder, divisor) >= 0) {
            int shift = GetBitLength(remainder) - divisorBits;
            uint[] shiftedDivisor = ShiftLeftMagnitude(divisor, shift);
            if (CompareMagnitude(shiftedDivisor, remainder) > 0) {
                shift--;
                shiftedDivisor = ShiftLeftMagnitude(divisor, shift);
            }

            remainder = SubtractMagnitude(remainder, shiftedDivisor);
            quotient = AddMagnitude(quotient, OneShiftedLeft(shift));
        }

        return (NormalizeDigits(quotient), NormalizeDigits(remainder));
    }

    private static int GetBitLength(ReadOnlySpan<uint> digits)
    {
        digits = TrimLeadingZeros(digits);
        if (digits.Length == 1 && digits[0] == 0u) {
            return 0;
        }

        uint high = digits[^1];
        int highBits = 0;
        while (high != 0u) {
            highBits++;
            high >>= 1;
        }

        return (digits.Length - 1) * 32 + highBits;
    }

    private static uint[] OneShiftedLeft(int shift)
    {
        int digitShift = shift / 32;
        int bitShift = shift % 32;
        uint[] result = new uint[digitShift + 1];
        result[digitShift] = 1u << bitShift;
        return result;
    }

    private static uint[] ShiftLeftMagnitude(ReadOnlySpan<uint> digits, int shift)
    {
        digits = TrimLeadingZeros(digits);
        if (shift == 0) {
            return digits.ToArray();
        }

        if (IsZeroMagnitude(digits)) {
            return [0u];
        }

        int digitShift = shift / 32;
        int bitShift = shift % 32;
        uint[] result = new uint[digits.Length + digitShift + 1];
        ulong carry = 0UL;

        for (int i = 0; i < digits.Length; i++) {
            ulong value = ((ulong)digits[i] << bitShift) | carry;
            result[i + digitShift] = (uint)value;
            carry = value >> 32;
        }

        result[digits.Length + digitShift] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static uint[] ShiftRightMagnitude(ReadOnlySpan<uint> digits, int shift)
    {
        digits = TrimLeadingZeros(digits);
        if (shift == 0) {
            return digits.ToArray();
        }

        int digitShift = shift / 32;
        if (digitShift >= digits.Length) {
            return [0u];
        }

        int bitShift = shift % 32;
        int length = digits.Length - digitShift;
        uint[] result = new uint[length];
        if (bitShift == 0) {
            digits[digitShift..].CopyTo(result);
            return NormalizeDigits(result);
        }

        uint carry = 0u;
        for (int i = digits.Length - 1; i >= digitShift; i--) {
            uint current = digits[i];
            result[i - digitShift] = (current >> bitShift) | carry;
            carry = current << (32 - bitShift);
        }

        return NormalizeDigits(result);
    }

    private static bool HasAnyLowerBit(ReadOnlySpan<uint> digits, int bitCount)
    {
        if (bitCount <= 0) {
            return false;
        }

        digits = TrimLeadingZeros(digits);
        int fullDigits = bitCount / 32;
        int partialBits = bitCount % 32;

        for (int i = 0; i < Math.Min(fullDigits, digits.Length); i++) {
            if (digits[i] != 0u) {
                return true;
            }
        }

        if (partialBits == 0 || fullDigits >= digits.Length) {
            return false;
        }

        uint mask = (1u << partialBits) - 1u;
        return (digits[fullDigits] & mask) != 0u;
    }

    private static BetterBigInteger Bitwise(BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> operation)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        int length = Math.Max(a.DigitCount, b.DigitCount) + 1;
        uint[] left = ToTwosComplement(a, length);
        uint[] right = ToTwosComplement(b, length);
        uint[] result = new uint[length];

        for (int i = 0; i < length; i++) {
            result[i] = operation(left[i], right[i]);
        }

        return FromTwosComplement(result);
    }

    private static uint[] ToTwosComplement(BetterBigInteger value, int length)
    {
        uint[] result = new uint[length];
        ReadOnlySpan<uint> digits = value.GetDigits();
        digits[..Math.Min(digits.Length, length)].CopyTo(result);

        if (!value.IsNegative) {
            return result;
        }

        for (int i = 0; i < result.Length; i++) {
            result[i] = ~result[i];
        }

        AddOneInPlace(result);
        return result;
    }

    private static BetterBigInteger FromTwosComplement(ReadOnlySpan<uint> words)
    {
        bool isNegative = (words[^1] & 0x80000000u) != 0u;
        uint[] magnitude = words.ToArray();
        if (!isNegative) {
            return FromDigits(magnitude);
        }

        for (int i = 0; i < magnitude.Length; i++) {
            magnitude[i] = ~magnitude[i];
        }

        AddOneInPlace(magnitude);
        return FromDigits(magnitude, isNegative: true);
    }

    private static void AddOneInPlace(Span<uint> digits)
    {
        ulong carry = 1UL;
        for (int i = 0; i < digits.Length && carry != 0UL; i++) {
            ulong sum = digits[i] + carry;
            digits[i] = (uint)sum;
            carry = sum >> 32;
        }
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
