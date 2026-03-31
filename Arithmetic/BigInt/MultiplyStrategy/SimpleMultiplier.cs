using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier {
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b) 
        => throw new NotImplementedException("O(n^2)");
}
