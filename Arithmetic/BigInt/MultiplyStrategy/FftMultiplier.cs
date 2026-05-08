using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier {
    private const int ChunkBits = 16;
    private const int ChunkBase = 1 << ChunkBits;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.GetDigits().Length == 1 && a.GetDigits()[0] == 0u || b.GetDigits().Length == 1 && b.GetDigits()[0] == 0u) {
            return BetterBigInteger.FromDigits([0u]);
        }

        ushort[] leftChunks = ToChunks(a.GetDigits());
        ushort[] rightChunks = ToChunks(b.GetDigits());
        uint[] product = MultiplyChunks(leftChunks, rightChunks);
        bool isNegative = a.IsNegative ^ b.IsNegative;
        return BetterBigInteger.FromDigits(product, isNegative);
    }

    private static ushort[] ToChunks(ReadOnlySpan<uint> digits)
    {
        digits = BetterBigInteger.TrimLeadingZeros(digits);
        ushort[] chunks = new ushort[digits.Length * 2];
        for (int i = 0; i < digits.Length; i++) {
            chunks[2 * i] = (ushort)(digits[i] & 0xFFFF);
            chunks[2 * i + 1] = (ushort)(digits[i] >> 16);
        }

        int length = chunks.Length;
        while (length > 1 && chunks[length - 1] == 0) {
            length--;
        }

        return chunks[..length];
    }

    private static uint[] MultiplyChunks(ReadOnlySpan<ushort> left, ReadOnlySpan<ushort> right)
    {
        int size = 1;
        while (size < left.Length + right.Length) {
            size <<= 1;
        }

        Complex[] fa = new Complex[size];
        Complex[] fb = new Complex[size];
        for (int i = 0; i < left.Length; i++) {
            fa[i] = new Complex(left[i], 0);
        }

        for (int i = 0; i < right.Length; i++) {
            fb[i] = new Complex(right[i], 0);
        }

        Transform(fa, invert: false);
        Transform(fb, invert: false);

        for (int i = 0; i < size; i++) {
            fa[i] *= fb[i];
        }

        Transform(fa, invert: true);

        long[] normalized = new long[left.Length + right.Length + 2];
        long carry = 0;
        for (int i = 0; i < normalized.Length; i++) {
            long value = carry;
            if (i < size) {
                value += (long)Math.Round(fa[i].Real);
            }

            normalized[i] = value % ChunkBase;
            carry = value / ChunkBase;
        }

        int chunkCount = normalized.Length;
        while (carry > 0) {
            Array.Resize(ref normalized, normalized.Length + 1);
            normalized[chunkCount++] = carry % ChunkBase;
            carry /= ChunkBase;
        }

        while (chunkCount > 1 && normalized[chunkCount - 1] == 0) {
            chunkCount--;
        }

        int digitCount = (chunkCount + 1) / 2;
        uint[] digits = new uint[digitCount];
        for (int i = 0; i < digitCount; i++) {
            uint low = (uint)normalized[2 * i];
            uint high = 2 * i + 1 < chunkCount ? (uint)normalized[2 * i + 1] : 0u;
            digits[i] = low | (high << ChunkBits);
        }

        return BetterBigInteger.NormalizeDigits(digits);
    }

    private static void Transform(Complex[] values, bool invert)
    {
        int n = values.Length;
        for (int i = 1, j = 0; i < n; i++) {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) {
                j ^= bit;
            }

            j ^= bit;
            if (i < j) {
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        for (int length = 2; length <= n; length <<= 1) {
            double angle = 2 * Math.PI / length * (invert ? -1 : 1);
            Complex wLength = Complex.FromPolarCoordinates(1, angle);
            for (int i = 0; i < n; i += length) {
                Complex w = Complex.One;
                int half = length / 2;
                for (int j = 0; j < half; j++) {
                    Complex u = values[i + j];
                    Complex v = values[i + j + half] * w;
                    values[i + j] = u + v;
                    values[i + j + half] = u - v;
                    w *= wLength;
                }
            }
        }

        if (!invert) {
            return;
        }

        for (int i = 0; i < n; i++) {
            values[i] /= n;
        }
    }
}
