using System.Diagnostics;

namespace PolyKiteFun;
using System;

public class PolyKiteGenerator
{
    public static void Explore()
    {
        Console.WriteLine("--- PolyKite Tile Combination Explorer ---");
        Console.WriteLine("Warning: The number of combinations grows extremely fast!");
        Console.WriteLine("A value of n > 5 may take a very long time and generate many files.");
        Console.WriteLine("\nEnter the number of kites to combine (n):");

        if (!int.TryParse(Console.ReadLine(), out int n) || n <= 1)
        {
            Console.WriteLine("Invalid number. Please enter a number greater than 1. Exiting.");
            return;
        }

        Console.WriteLine($"Enter the maximum number of Heesch layers to test (e.g., 1, 2, 3...):");
        if (!int.TryParse(Console.ReadLine(), out int maxLayers) || maxLayers <= 0) { Console.WriteLine("Invalid number."); return; }

        // Use the new systematic explorer
        var explorer = new SystematicExplorer(n);
        explorer.FindAllCombinations(n);

        var foundCombinations = explorer.FoundCombinations;

        for(int i = 0; i < foundCombinations.Count; i++)
        {
            var prototile = foundCombinations[i];
            prototile.SolutionId = i; // Assign a solution ID for tracking.

            // Skip clusters that have already been solved.
            if (prototile.HeeschNumber.HasValue &&
                prototile.MaxHeeschNumberExplored.HasValue &&
                prototile.HeeschNumber.Value <= prototile.MaxHeeschNumberExplored &&
                prototile.MaxHeeschNumberExplored.Value >= maxLayers)
            {
                Console.WriteLine($"Skipping solution {i}, Heesch number already known: {prototile.HeeschNumber}");
                continue;
            }

            Console.WriteLine($"Starting to find a tiling for solution: {i}");
            var start = Stopwatch.GetTimestamp();

            var solver = new HeeschSolver(prototile, maxLayers);
            var result = solver.Solve();
            prototile.TimeToProcess = Stopwatch.GetElapsedTime(start).TotalSeconds;
            prototile.HeeschNumber = result.HeeschNumber;
            prototile.MaxHeeschNumberExplored = maxLayers;

            if (result.HeeschNumber > 0)
            {
                // Store the best result found for this shape index.
                string dirPath = Path.Combine("combinations", n.ToString(), result.HeeschNumber.ToString());
                Directory.CreateDirectory(dirPath);
                string imagePath = Path.Combine(dirPath, $"heesch_{result.HeeschNumber}_solution_{i}.png");

                Cluster.GenerateHeeschImage(result, imagePath);
                Console.WriteLine($"Success in finding a tiling for solution: {i} with HeeschNumber {result.HeeschNumber}, time: {prototile.TimeToProcess} seconds");
            }
            else
            {
                Console.WriteLine($"Failed to find a tiling for solution: {i}, time: {prototile.TimeToProcess} seconds");
            }

            // Save the updated combinations list to persist the new Heesch number.
            explorer.SaveCombinations(n);
        }
    }
}
