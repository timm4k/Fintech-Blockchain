using System.Diagnostics;

namespace FintechBlockchain;

public static class MiningConsole
{
    public static async Task<MiningResult> WaitAsync(
        Task<MiningResult> mining, string mode, CancellationTokenSource cancellation)
    {
        var stopwatch = Stopwatch.StartNew();
        TimeSpan nextStatus = TimeSpan.FromSeconds(5);
        Console.WriteLine($"{mode}: running");
        while (!mining.IsCompleted)
        {
            if (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                ConsoleKey key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.Q)
                {
                    cancellation.Cancel();
                }
                else if (key == ConsoleKey.S)
                {
                    Console.WriteLine($"Status requested | {mode} | {stopwatch.Elapsed.TotalSeconds:F1} s");
                }
            }

            await Task.WhenAny(mining, Task.Delay(100));
            if (!mining.IsCompleted && stopwatch.Elapsed >= nextStatus)
            {
                Console.WriteLine($"Status | {mode} | {stopwatch.Elapsed.TotalSeconds:F1} s");
                nextStatus = stopwatch.Elapsed + TimeSpan.FromSeconds(5);
            }
        }
        return await mining;
    }

    public static void PrintComparison(
        HashingService hashingService,
        Block sequentialBlock, MiningResult sequential,
        Block parallelBlock, MiningResult parallel)
    {
        Console.WriteLine();
        Console.WriteLine("MINING RESULTS");
        Console.WriteLine($"{"Mode",-14}{"Workers",-10}{"Nonce",-16}{"Attempts",-18}{"Time (ms)",-16}{"Valid"}");
        PrintResult("Sequential", sequentialBlock, sequential, hashingService);
        PrintResult("Parallel", parallelBlock, parallel, hashingService);
        Console.WriteLine($"Sequential hash: {sequential.Hash}");
        Console.WriteLine($"Parallel hash:   {parallel.Hash}");
        Console.WriteLine();
        Console.WriteLine("PARALLEL NONCE RANGES");
        Console.WriteLine($"{"Worker",-10}{"First nonce",-16}{"Step",-10}{"Attempts"}");
        for (int worker = 0; worker < parallel.AttemptsPerWorker.Count; worker++)
        {
            Console.WriteLine($"{worker,-10}{worker,-16}{parallel.AttemptsPerWorker.Count,-10}{parallel.AttemptsPerWorker[worker]}");
        }
        Console.WriteLine($"Workers active after completion: {parallel.ActiveWorkersAtCompletion}");
        Console.WriteLine($"Speedup (sequential / parallel): {sequential.Elapsed.TotalMilliseconds / parallel.Elapsed.TotalMilliseconds:F2}x");
    }

    private static void PrintResult(
        string mode, Block block, MiningResult result, HashingService hashingService)
    {
        bool valid = result.Hash == hashingService.ComputeHash(block)
            && result.Hash.StartsWith(new string('0', block.DifficultyAtMining), StringComparison.Ordinal);
        Console.WriteLine($"{mode,-14}{result.AttemptsPerWorker.Count,-10}{result.Nonce,-16}{result.Attempts,-18}{result.Elapsed.TotalMilliseconds,-16:F3}{valid}");
    }
}
