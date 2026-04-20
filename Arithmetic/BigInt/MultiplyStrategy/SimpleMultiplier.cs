using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier {
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.GetDigits().Length == 1 && a.GetDigits()[0] == 0u || b.GetDigits().Length == 1 && b.GetDigits()[0] == 0u) {
            return BetterBigInteger.FromDigits([0u]);
        }

        uint[] product = MultiplyDigits(a.GetDigits(), b.GetDigits());
        bool isNegative = a.IsNegative ^ b.IsNegative;
        return BetterBigInteger.FromDigits(product, isNegative);
    }

    internal static uint[] MultiplyDigits(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        left = BetterBigInteger.TrimLeadingZeros(left);
        right = BetterBigInteger.TrimLeadingZeros(right);
        if (left.Length == 1 && left[0] == 0u || right.Length == 1 && right[0] == 0u) {
            return [0u];
        }

        uint[] result = new uint[left.Length + right.Length];
        for (int i = 0; i < left.Length; i++) {
            ulong carry = 0UL;
            ulong a = left[i];

            for (int j = 0; j < right.Length; j++) {
                ulong sum = result[i + j] + a * right[j] + carry;
                result[i + j] = (uint)sum;
                carry = sum >> 32;
            }

            int index = i + right.Length;
            while (carry != 0UL) {
                ulong sum = result[index] + carry;
                result[index] = (uint)sum;
                carry = sum >> 32;
                index++;
            }
        }

        return BetterBigInteger.NormalizeDigits(result);
    }
}
