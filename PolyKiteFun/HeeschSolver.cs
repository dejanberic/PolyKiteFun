namespace PolyKiteFun;

public class HeeschSolver(Cluster prototile, int maxLayers)
{
    private HeeschResult _bestOverallResult = new(prototile, 0, []);
    private readonly Dictionary<long, bool> _overlapCache = [];

    // Initialize with Heesch number 0 result

    public HeeschResult Solve()
    {
        // Start the main recursive search
        SolveHeeschRecursively(prototile, [], 1);

        return _bestOverallResult;
    }

    /// <summary>
    /// The definitive recursive engine. It updates a member variable with the best valid
    /// state found, correctly decoupling the final layer from the validation step.
    /// </summary>
    /// <param name="currentArrangement">The kites for layers 0 to (currentLayer - 1).</param>
    /// <param name="currentCoronas">The list of coronas built so far.</param>
    /// <param name="currentLayer">The layer number we are attempting to build.</param>
    private void SolveHeeschRecursively(Cluster currentArrangement, List<List<Cluster>> currentCoronas, int currentLayer)
    {
        // --- STEP 1: Record the current state as a potential best solution. ---
        // The fact that we have entered this function means we have successfully built
        // (currentLayer - 1) layers. Let's see if this is a new record.
        int successfulLayers = currentLayer - 1;
        if (successfulLayers > _bestOverallResult.HeeschNumber)
        {
            _bestOverallResult = new HeeschResult
            (
                prototile,
                successfulLayers,
                [.. currentCoronas]
            );
        }

        // --- STEP 2: Check if we can continue the search from this point. ---

        // Base Case 1: Have we reached the user-defined search limit?
        if (currentLayer > maxLayers)
        {
            return;
        }

        // Base Case 2: Is the CURRENT shape valid for building UPON?
        // This is the check that ensures layers 0..N-1 are perfect before we try to build layer N.
        if (currentArrangement.CheckForUnfillableCornersOrHasGaps())
        {
            // This shape is flawed, so we cannot continue this path.
            return;
        }

        // --- STEP 3: Find all possible ways to build the next layer. ---
        var allPossibleCoronas = FindAllPossibleTilingsForBoundary(currentArrangement);

        // --- STEP 4: Recurse for each possible path. ---
        foreach (var corona in allPossibleCoronas)
        {
            // Early exit optimization: If another branch has already found the max, stop.
            if (_bestOverallResult.HeeschNumber >= maxLayers)
            {
                return;
            }

            // Prepare for the next recursive call.
            var nextArrangement = currentArrangement.Clone();
            nextArrangement.AddKites(corona.SelectMany(el => el.Kites));

            var nextCoronas = new List<List<Cluster>>(currentCoronas)
            {
                corona
            };

            // Recurse to try and build the layer after this one.
            SolveHeeschRecursively(nextArrangement, nextCoronas, currentLayer + 1);
        }
    }

    /// <summary>
    /// Finds ALL possible complete tilings (coronas) for a given boundary.
    /// </summary>
    private IEnumerable<List<Cluster>> FindAllPossibleTilingsForBoundary(Cluster cluster)
    {
        // This is a new helper that launches the search for all solutions.
        foreach (var solution in FindAllBoundarySolutions_RecursiveHelper(cluster.BoundaryEdges, [], cluster.Kites))
        {
            yield return solution;
        }
    }

    /// <summary>
    /// The definitive, safe, recursive generator. It avoids state corruption by passing
    /// new state copies to its recursive calls instead of modifying a shared list.
    /// </summary>
    private IEnumerable<List<Cluster>> FindAllBoundarySolutions_RecursiveHelper(
        List<Edge> edgesToCover,
        List<Cluster> currentPath, // This represents the immutable path taken so far.
        List<Kite> existingKites)
    {
        // --- Base Case: A valid path has covered all edges. ---
        if (edgesToCover.Count == 0)
        {
            // Yield the completed path. This is already a safe snapshot.
            yield return currentPath;
            yield break;
        }

        var edgeToCover = edgesToCover.First();
        var potentialPlacements = FindValidPlacementsForEdge(edgeToCover, currentPath, existingKites);

        foreach (var placement in potentialPlacements)
        {
            // 1. Create the NEW path state for the next level of recursion.
            //    Do NOT modify the 'currentPath'.
            var nextPath = DeepClone(currentPath);
            nextPath.Add(placement);

            // 2. Calculate the next set of edges to cover.
            var nextEdgesToCover = UpdateCoveredEdges(edgesToCover, placement);

            // 3. Recurse with the NEW, independent state.
            foreach (var solution in FindAllBoundarySolutions_RecursiveHelper(nextEdgesToCover, nextPath, existingKites))
            {
                // Propagate solutions found in the deeper levels.
                yield return solution;
            }
        }
    }

    private static List<Cluster> DeepClone(List<Cluster> solution)
    {
        return [.. solution.Select(cluster => cluster.Clone())];
    }

    #region --- Recursive Solving and Validation Logic ---

    private List<Cluster> FindValidPlacementsForEdge(Edge edgeToCover, List<Cluster> placedNeighbors, List<Kite> existingKites)
    {
        var validPlacements = new List<Cluster>();
        var allKitesToTestAgainst = new List<Kite>(existingKites);
        foreach (var neighborCluster in placedNeighbors)
        {
            allKitesToTestAgainst.AddRange(neighborCluster.Kites);
        }

        foreach (var matchingCluster in prototile.GetClustersToTry())
        {
            foreach (var edgeOnMatcher in matchingCluster.BoundaryEdges)
            {
                if (Math.Abs(edgeToCover.Length - edgeOnMatcher.Length) < 0.01f)
                {
                    foreach (var targetEdge in edgeToCover.GetTargetEdgesToCover())
                    {
                        var transform = edgeOnMatcher.ComputeTransformation(targetEdge);
                        var transformedCluster = matchingCluster.TransformWithoutOverlap(transform,
                            allKitesToTestAgainst, _overlapCache);
                        if (transformedCluster is not null)
                        {
                            validPlacements.Add(transformedCluster);
                        }
                    }
                }
            }
        }

        return validPlacements;
    }

    private static List<Edge> UpdateCoveredEdges(List<Edge> currentEdges, Cluster newPlacement)
    {
        var newBoundary = newPlacement.BoundaryEdges;
        return currentEdges.Where(edge => newBoundary.All(e => e != edge)).ToList();
    }

    #endregion
}