using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier {
    private const int SimpleThreshold = 32;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.GetDigits().Length == 1 && a.GetDigits()[0] == 0u || b.GetDigits().Length == 1 && b.GetDigits()[0] == 0u) {
            return BetterBigInteger.FromDigits([0u]);
        }

        uint[] product = MultiplyCore(a.GetDigits(), b.GetDigits());
        bool isNegative = a.IsNegative ^ b.IsNegative;
        return BetterBigInteger.FromDigits(product, isNegative);
    }

    private static uint[] MultiplyCore(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        left = BetterBigInteger.TrimLeadingZeros(left);
        right = BetterBigInteger.TrimLeadingZeros(right);

        if (left.Length == 1 && left[0] == 0u || right.Length == 1 && right[0] == 0u) {
            return [0u];
        }

        int n = Math.Max(left.Length, right.Length);
        if (n <= SimpleThreshold) {
            return SimpleMultiplier.MultiplyDigits(left, right);
        }

        int mid = n / 2;
        ReadOnlySpan<uint> leftLow = left[..Math.Min(left.Length, mid)];
        ReadOnlySpan<uint> leftHigh = left[Math.Min(left.Length, mid)..];
        ReadOnlySpan<uint> rightLow = right[..Math.Min(right.Length, mid)];
        ReadOnlySpan<uint> rightHigh = right[Math.Min(right.Length, mid)..];

        uint[] z0 = MultiplyCore(leftLow, rightLow);
        uint[] z2 = MultiplyCore(leftHigh.Length == 0 ? [0u] : leftHigh, rightHigh.Length == 0 ? [0u] : rightHigh);
        uint[] sumLeft = AddDigits(leftLow, leftHigh);
        uint[] sumRight = AddDigits(rightLow, rightHigh);
        uint[] z1 = MultiplyCore(sumLeft, sumRight);
        uint[] cross = SubtractDigits(SubtractDigits(z1, z2), z0);

        uint[] combined = AddDigits(ShiftDigits(z2, mid * 2), AddDigits(ShiftDigits(cross, mid), z0));
        return BetterBigInteger.NormalizeDigits(combined);
    }

    private static uint[] AddDigits(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
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
        return BetterBigInteger.NormalizeDigits(result);
    }

    private static uint[] SubtractDigits(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        uint[] result = new uint[left.Length];
        long borrow = 0;

        for (int i = 0; i < left.Length; i++) {
            long diff = (long)left[i] - (i < right.Length ? right[i] : 0L) - borrow;
            if (diff < 0) {
                diff += 1L << 32;
                borrow = 1;
            }
            else {
                borrow = 0;
            }

            result[i] = (uint)diff;
        }

        return BetterBigInteger.NormalizeDigits(result);
    }

    private static uint[] ShiftDigits(ReadOnlySpan<uint> digits, int digitShift)
    {
        digits = BetterBigInteger.TrimLeadingZeros(digits);
        if (digits.Length == 1 && digits[0] == 0u) {
            return [0u];
        }

        uint[] result = new uint[digits.Length + digitShift];
        digits.CopyTo(result.AsSpan(digitShift));
        return result;
    }
}
