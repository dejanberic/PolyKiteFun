using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolyKiteFun;

[method: JsonConstructor]
public class CombinationCache(int n, List<Cluster> combinations)
{
    public int N { get; set; } = n;
    public List<Cluster> Combinations { get; set; } = combinations;
}

public class SystematicExplorer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string _imageOutputDirectory;
    private readonly string _outputDirectory;
    private readonly int _maxKites;
    private readonly Kite _baseKite = new();
    public List<Cluster> FoundCombinations { get; private set; }
    private readonly HashSet<string> _foundSignatures = [];

    public SystematicExplorer(int maxKites, string outputDirectory = "combinations")
    {
        _outputDirectory = outputDirectory;
        _maxKites = maxKites;
        _imageOutputDirectory = $"{_outputDirectory}\\{maxKites}";
        if (!Directory.Exists(_imageOutputDirectory))
        {
            Directory.CreateDirectory(_imageOutputDirectory);
        }
        FoundCombinations = [];
    }

    private string GetCacheFilePath() => Path.Combine(_outputDirectory, $"combinations_{_maxKites}.json");

    public void SaveCombinations()
    {
        string cacheFilePath = GetCacheFilePath();
        // Note: Rewriting the entire file on each update can be slow for very large
        // combination sets. For true high-performance scenarios, a more advanced
        // persistence strategy like a database or append-only log might be considered.
        Console.WriteLine($"Saving {FoundCombinations.Count} combinations to {cacheFilePath}...");
        var newCache = new CombinationCache(_maxKites, FoundCombinations);
        var newJson = JsonSerializer.Serialize(newCache, JsonSerializerOptions);
        File.WriteAllText(cacheFilePath, newJson);
        Console.WriteLine("Save complete.");
    }

    public void FindAllCombinations()
    {
        if (_maxKites <= 0) return;

        string cacheFilePath = GetCacheFilePath();
        if (File.Exists(cacheFilePath))
        {
            Console.WriteLine($"Loading combinations for n={_maxKites} from cache...");
            var json = File.ReadAllText(cacheFilePath);
            var cache = JsonSerializer.Deserialize<CombinationCache>(json);
            if (cache != null)
            {
                FoundCombinations = cache.Combinations;
                Console.WriteLine($"Loaded {FoundCombinations.Count} combinations from {cacheFilePath}.");
                return;
            }
        }

        FoundCombinations.Clear();
        _foundSignatures.Clear();

        var initialCluster = Cluster.InitializeWithOneKite();
        //var initialCluster = Cluster.InitializeWithTriangle();
        //var initialCluster = Cluster.InitializeWithEinsteinTile();

        Console.WriteLine($"Starting systematic search for {_maxKites} kites...");
        var start = Stopwatch.GetTimestamp();
        Explore(initialCluster);
        var totalSeconds = Stopwatch.GetElapsedTime(start).TotalSeconds;
        Console.WriteLine($"Search complete. Found {FoundCombinations.Count} unique combinations in {totalSeconds} seconds.");

        SaveCombinations();
    }

    //private static int counter = 0;
    private void Explore(Cluster currentCluster)
    {
        // Base Case: If we've reached the desired number of kites, we found a valid combination.
        if (currentCluster.Kites.Count == _maxKites)
        {
            string? canonicalSignature = ClusterDeduplicator.GetCanonicalSignature(currentCluster);
            if (canonicalSignature is null)
            {
                return;
            }

            // If we have not seen this signature before, it's a new unique shape.
            if (_foundSignatures.Add(canonicalSignature))
            {
                currentCluster.ResetCache();
                FoundCombinations.Add(currentCluster);
                currentCluster.GenerateImage($"{_imageOutputDirectory}\\{FoundCombinations.Count - 1}.png");
            }

            return;
        }

        // --- Combinatorial Explosion Warning ---
        // For each boundary edge, we try to attach a new kite in every possible way.
        // This branching factor is why the number of states grows so rapidly.
        var boundaryEdges = currentCluster.BoundaryEdges;
        foreach (var boundaryEdge in boundaryEdges)
        {
            foreach (var kiteEdge in _baseKite.Edges)
            {
                if (Math.Abs(kiteEdge.Length - boundaryEdge.Length) < 0.001f)
                {
                    var transform = kiteEdge.ComputeTransformation(boundaryEdge.Mirrored());
                    var transformedKite = _baseKite.Transform(transform);
                        
                    if (!currentCluster.Kites.Any(k => k.Overlaps(transformedKite)))
                    {
                        var nextCluster = currentCluster.Clone();
                        nextCluster.AddKite(transformedKite);
                        Explore(nextCluster);
                    }
                }
            }
        }
    }
}