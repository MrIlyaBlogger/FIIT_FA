using System.Diagnostics;
using Arithmetic.BigInt;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

IMultiplier[] multipliers = [
    new SimpleMultiplier(),
    new KaratsubaMultiplier(),
    new FftMultiplier()
];

int[] lengths = [128, 512, 2048];
Random random = new(42);

Console.WriteLine("BetterBigInteger multiplication benchmark");
Console.WriteLine("Seed: 42");

foreach (int length in lengths) {
    string left = GenerateDigits(random, length);
    string right = GenerateDigits(random, length);

    BetterBigInteger a = new(left, 10);
    BetterBigInteger b = new(right, 10);

    Console.WriteLine();
    Console.WriteLine($"Digits: {length}");

    string? baseline = null;
    foreach (IMultiplier multiplier in multipliers) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        BetterBigInteger result = multiplier.Multiply(a, b);
        stopwatch.Stop();

        string text = result.ToString();
        baseline ??= text;
        bool matches = baseline == text;

        Console.WriteLine($"{multiplier.GetType().Name,-20} {stopwatch.ElapsedMilliseconds,6} ms  match={matches}");
    }
}

static string GenerateDigits(Random random, int length)
{
    char[] chars = new char[length];
    chars[0] = (char)('1' + random.Next(9));
    for (int i = 1; i < chars.Length; i++) {
        chars[i] = (char)('0' + random.Next(10));
    }

    return new string(chars);
}
