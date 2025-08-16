namespace PolyKiteFun
{
    // --- Helper class for the Graph Dictionary replacement ---
    public class VertexGraphNode(PointF vertex)
    {
        public PointF Vertex { get; } = vertex;
        public List<Tuple<int, int>> Edges { get; } = [];
    }

    public static class ClusterDeduplicator
    {
        // --- Pre-calculated properties of the base Kite shape ---
        private static readonly float ShortEdgeLength = 0.5f;
        private static readonly float LongEdgeLength = (float)Math.Sqrt(3) / 6.0f;
        private static readonly float LengthClassifier = (ShortEdgeLength + LongEdgeLength) / 2.0f;

        /// <summary>
        /// Takes a list of clusters and returns a new list containing only the unique geometric shapes,
        /// using a robust graph-based canonical signature without relying on GetHashCode.
        /// </summary>
        public static List<Cluster> Deduplicate(List<Cluster> clusters)
        {
            var uniqueClusters = new List<Tuple<Cluster, string>>();
            var foundSignatures = new HashSet<string>();

            foreach (var cluster in clusters)
            {
                string? canonicalSignature = GetCanonicalSignature(cluster);
                if (canonicalSignature is null)
                {
                    continue;
                }

                // If we have not seen this signature before, it's a new unique shape.
                if (foundSignatures.Add(canonicalSignature))
                {
                    uniqueClusters.Add(new Tuple<Cluster, string>(cluster, canonicalSignature));
                }
            }

            uniqueClusters.Sort((x,y) => String.Compare(x.Item2, y.Item2, StringComparison.Ordinal));

            return uniqueClusters.Select(el => el.Item1).ToList();
        }
        
        /// <summary>
        /// Finds the canonical (lexicographically smallest) signature for a cluster by checking all its symmetries.
        /// </summary>
        public static string? GetCanonicalSignature(Cluster cluster)
        {
            string? smallestSignature = null;
            var clustersToCheck = new[] { cluster, cluster.CreateMirroredVersion() };

            foreach (var baseCluster in clustersToCheck)
            {
                var currentShape = baseCluster;
                for (int i = 0; i < 12; i++) // Check all 12 rotations
                {
                    if (i > 0)
                    {
                        var center = currentShape.GetClusterCenter();
                        var matrix = new Matrix3x2();
                        matrix.RotateAt(30, center);
                        currentShape = currentShape.Transform(matrix);
                    }
                    
                    string signature = GenerateGraphSignature(currentShape);

                    if (smallestSignature == null || string.CompareOrdinal(signature, smallestSignature) < 0)
                    {
                        smallestSignature = signature;
                    }
                }
            }
            return smallestSignature;
        }

        /// <summary>
        /// Generates a canonical signature for a cluster by converting it to an abstract, discrete graph.
        /// This method is immune to floating-point errors and does not use unreliable hash codes.
        /// </summary>
        private static string GenerateGraphSignature(Cluster cluster)
        {
            var uniqueVertices = cluster.UniqueVertices;
            if (uniqueVertices.Count == 0) return "";
            
            // --- Build the graph using a List instead of a Dictionary ---
            var graph = uniqueVertices.Select(v => new VertexGraphNode(v)).ToList();

            var allUniqueEdges = cluster.UniqueEdges;

            foreach (var edge in allUniqueEdges)
            {
                var vec1 = new PointF(edge.End.X - edge.Start.X, edge.End.Y - edge.Start.Y);
                var vec2 = new PointF(edge.Start.X - edge.End.X, edge.Start.Y - edge.End.Y);
                
                int lengthType = GetLengthType(edge.Length);
                int angleType1 = GetAngleType(vec1);
                int angleType2 = GetAngleType(vec2);

                // Find the graph nodes for the start and end vertices using robust Equals.
                graph.FirstOrDefault(n => n.Vertex.Equals(edge.Start))?.Edges.Add(new Tuple<int, int>(lengthType, angleType1));
                graph.FirstOrDefault(n => n.Vertex.Equals(edge.End))?.Edges.Add(new Tuple<int, int>(lengthType, angleType2));
            }

            // --- Build the Canonical Signature ---
            var nodeSignatures = new List<string>();
            // Consistent ordering of vertices before building the final signature string.
            var sortedGraphNodes = graph.OrderBy(n => n.Edges.Count)
                                        .ThenBy(n => Math.Round(n.Vertex.X, 4))
                                        .ThenBy(n => Math.Round(n.Vertex.Y, 4));

            foreach (var node in sortedGraphNodes)
            {
                // For each vertex, sort its connected edges to create a canonical node signature.
                var sortedEdges = node.Edges.OrderBy(t => t.Item1).ThenBy(t => t.Item2).ToList();
                nodeSignatures.Add(string.Join(";", sortedEdges.Select(t => $"{t.Item1},{t.Item2}")));
            }

            // The final graph signature is the sorted concatenation of all node signatures.
            return string.Join("|", nodeSignatures.OrderBy(s => s));
        }

        private static int GetLengthType(float length)
        {
            return length < LengthClassifier ? 2 : 1; // Long edge has smaller length
        }

        private static int GetAngleType(PointF vector)
        {
            double angleDeg = Math.Atan2(vector.Y, vector.X) * 180.0 / Math.PI;
            if (angleDeg < 0) angleDeg += 360;
            return (int)Math.Round(angleDeg / 30.0) % 12;
        }
    }
}