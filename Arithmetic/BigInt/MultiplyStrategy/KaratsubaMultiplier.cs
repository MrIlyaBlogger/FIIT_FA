using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier {
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b) 
        => throw new NotImplementedException("O(n^1.58)");
}
