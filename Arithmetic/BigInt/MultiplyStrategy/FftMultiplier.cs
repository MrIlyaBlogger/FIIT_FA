using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier {
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
        => throw new NotImplementedException("O(n log n log log n)");
}
