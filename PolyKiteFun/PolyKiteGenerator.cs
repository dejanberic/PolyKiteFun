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

        if (!int.TryParse(Console.ReadLine(), out int n) || n <= 0)
        {
            Console.WriteLine("Invalid number. Please enter a number greater than 0. Exiting.");
            return;
        }

        Console.WriteLine($"Enter the maximum number of Heesch layers to test (e.g., 1, 2, 3...):");
        if (!int.TryParse(Console.ReadLine(), out int maxLayers) || maxLayers <= 0) { Console.WriteLine("Invalid number."); return; }

        // Use the new systematic explorer
        var explorer = new SystematicExplorer(n);
        explorer.FindAllCombinations();

        var foundCombinations = explorer.FoundCombinations;
        var totalCombinations = foundCombinations.Count;

        Console.WriteLine($"Found {totalCombinations} unique combinations.");
        Console.WriteLine("Do you want to process all of them? (Y/N)");
        var choice = Console.ReadLine()?.Trim().ToUpper();

        if (choice == "N")
        {
            Console.WriteLine("Please enter the solution index to process:");
            if (!int.TryParse(Console.ReadLine(), out int index) || index < 0 || index >= totalCombinations)
            {
                Console.WriteLine("Invalid index. Exiting.");
                return;
            }
            // Process just the selected combination
            ProcessCombination(foundCombinations[index], index, n, maxLayers);
            explorer.SaveCombinations(); // Save the result for the single processed item
        }
        else
        {
            // Process all combinations
            var saveInterval = Math.Max(1, totalCombinations / 100);
            for (int i = 0; i < totalCombinations; i++)
            {
                ProcessCombination(foundCombinations[i], i, n, maxLayers);

                // Save after every 1% of combinations are processed
                if ((i + 1) % saveInterval == 0 && (i + 1) < totalCombinations)
                {
                    Console.WriteLine($"Progress: {((i + 1) * 100) / totalCombinations}%. Saving combinations...");
                    explorer.SaveCombinations();
                }
            }

            // Final save after the loop to ensure the last batch is saved.
            Console.WriteLine("Processing complete. Performing final save...");
            explorer.SaveCombinations();
        }
    }

    private static void ProcessCombination(Cluster prototile, int index, int n, int maxLayers)
    {
        prototile.SolutionId = index; // Assign a solution ID for tracking.

        // Skip clusters that have already been solved.
        if (prototile.HeeschNumber.HasValue &&
            prototile.MaxHeeschNumberExplored.HasValue &&
            prototile.HeeschNumber.Value <= prototile.MaxHeeschNumberExplored &&
            prototile.MaxHeeschNumberExplored.Value >= maxLayers)
        {
            Console.WriteLine($"Skipping solution {index}, Heesch number already known: {prototile.HeeschNumber}");
            return;
        }

        Console.WriteLine($"Starting to find a tiling for solution: {index}");
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
            string imagePath = Path.Combine(dirPath, $"heesch_{result.HeeschNumber}_solution_{index}.png");

            Cluster.GenerateHeeschImage(result, imagePath);
            Console.WriteLine($"Success in finding a tiling for solution: {index} with HeeschNumber {result.HeeschNumber}, time: {prototile.TimeToProcess} seconds");
        }
        else
        {
            Console.WriteLine($"Failed to find a tiling for solution: {index}, time: {prototile.TimeToProcess} seconds");
        }
    }
}
