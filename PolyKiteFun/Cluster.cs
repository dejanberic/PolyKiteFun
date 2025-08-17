using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Text.Json.Serialization;

namespace PolyKiteFun;

public class Cluster(List<Kite> kites) : IEquatable<Cluster>
{
    private static readonly Matrix3x2 MirrorMatrix = new(-1, 0, 0, 1, 0, 0);
    private List<Edge>? _boundaryEdges;
    private List<Edge>? _uniqueEdges;
    private List<PointF>? _uniqueVertices;
    public List<Kite> Kites { get; } = kites;
    public int? HeeschNumber { get; set; }
    public int? MaxHeeschNumberExplored { get; set; }
    public long? SolutionId { get; set; }
    public double? TimeToProcess { get; set; }

    [JsonIgnore]
    public List<Edge> BoundaryEdges
    {
        get
        {
            if (_boundaryEdges is null || _boundaryEdges.Count == 0)
            {
                _boundaryEdges = GetBoundaryEdges();
            }

            return _boundaryEdges;
        }
        set
        {
            _boundaryEdges = value;
        }
    }

    [JsonIgnore]
    public List<Edge> UniqueEdges
    {
        get
        {
            if (_uniqueEdges is null || _uniqueEdges.Count == 0)
            {
                _uniqueEdges = GetAllUniqueEdges();
            }

            return _uniqueEdges;
        }
        set
        {
            _uniqueEdges = value;
        }
    }

    [JsonIgnore]
    public List<PointF> UniqueVertices
    {
        get
        {
            if (_uniqueVertices is null || _uniqueVertices.Count == 0)
            {
                _uniqueVertices = GetUniqueVertices();
            }

            return _uniqueVertices;
        }
        set
        {
            _uniqueVertices = value;
        }
    }

    #region Initialization Methods
    public static Cluster InitializeWithOneKite()
    {
        var result = new Cluster([new Kite()]);
        return result;
    }

    /// <summary>
    /// Initializes the cluster with a stable base of 3 kites
    /// forming a perfect equilateral triangle.
    /// </summary>
    public static Cluster InitializeWithTriangle()
    {
        // The central point where the three kites will meet. This is the "pointy"
        // vertex of the default Kite shape.
        var centerPoint = new PointF(0.5f, (float)(Math.Sqrt(3) / 6.0));

        // --- Create the 3 Kites ---

        // Kite 1: The base kite in its default position.
        var kite1 = new Kite();

        // Kite 2: A new kite, rotated 120 degrees around the common center point.
        var matrix120 = new Matrix3x2();
        matrix120.RotateAt(120, centerPoint);
        var kite2 = new Kite().Transform(matrix120);

        // Kite 3: A new kite, rotated 240 degrees around the common center point.
        var matrix240 = new Matrix3x2();
        matrix240.RotateAt(240, centerPoint);
        var kite3 = new Kite().Transform(matrix240);

        var result = new Cluster([kite1, kite2, kite3]);
        return result;
    }

    public static Cluster InitializeWithEinsteinTile()
    {
        // The central point where the three kites will meet. This is the "pointy"
        // vertex of the default Kite shape.
        var centerPoint = new PointF(0.5f, (float)(Math.Sqrt(3) / 6.0));

        // --- Create the 3 Kites ---

        // Kite 1: The base kite in its default position.
        var kite1 = new Kite();

        // Kite 2: A new kite, rotated 120 degrees around the common center point.
        var matrix120 = new Matrix3x2();
        matrix120.RotateAt(120, centerPoint);
        var kite2 = new Kite().Transform(matrix120);

        // Kite 3: A new kite, rotated 240 degrees around the common center point.
        var matrix240 = new Matrix3x2();
        matrix240.RotateAt(240, centerPoint);
        var kite3 = new Kite().Transform(matrix240);

        var kite4 = new Kite(kite3.Vertices);
        var matrix4 = new Matrix3x2();
        matrix4.RotateAt(60, kite4.Vertices[0]);
        kite4 = kite4.Transform(matrix4);

        var kite5 = new Kite(kite1.Vertices);
        var matrix5 = new Matrix3x2();
        matrix5.RotateAt(-60, kite5.Vertices[0]);
        kite5 = kite5.Transform(matrix5);

        var kite6 = new Kite(kite2.Vertices);
        var matrix6 = new Matrix3x2();
        matrix6.RotateAt(60, kite6.Vertices[0]);
        kite6 = kite6.Transform(matrix6);

        var kite7 = new Kite(kite1.Vertices);
        var matrix7 = new Matrix3x2();
        matrix7.RotateAt(60, kite7.Vertices[0]);
        kite7 = kite7.Transform(matrix7);

        //final kite for Einstein tile
        var kite8 = new Kite(kite1.Vertices);
        var matrix8 = new Matrix3x2();
        matrix8.RotateAt(120, kite8.Vertices[0]);
        kite8 = kite8.Transform(matrix8);

        var result = new Cluster([kite1, kite2, kite3, kite4, kite5, kite6, kite7, kite8]);
        return result;
    }

    #endregion

    public void ResetCache()
    {
        _boundaryEdges = null;
        _uniqueEdges = null;
        _uniqueVertices = null;
    }

    public void AddKite(Kite kiteToAdd)
    {
        this.Kites.Add(kiteToAdd);
        if (_boundaryEdges is not null && _boundaryEdges.Count > 0)
        {
            _boundaryEdges = GetBoundaryEdges();
        }
        if (_uniqueEdges is not null && _uniqueEdges.Count > 0)
        {
            _uniqueEdges = GetAllUniqueEdges();
        }
        if (_uniqueVertices is not null && _uniqueVertices.Count > 0)
        {
            _uniqueVertices = GetUniqueVertices();
        }
    }

    public void AddKites(IEnumerable<Kite> kites)
    {
        this.Kites.AddRange(kites);
        if (_boundaryEdges is not null && _boundaryEdges.Count > 0)
        {
            _boundaryEdges = GetBoundaryEdges();
        }
        if (_uniqueEdges is not null && _uniqueEdges.Count > 0)
        {
            _uniqueEdges = GetAllUniqueEdges();
        }
        if (_uniqueVertices is not null && _uniqueVertices.Count > 0)
        {
            _uniqueVertices = GetUniqueVertices();
        }
    }

    public Cluster Clone()
    {
        var newCluster = new Cluster([.. this.Kites]);
        if (_boundaryEdges is not null && _boundaryEdges.Count > 0)
        {
            newCluster._boundaryEdges = [.. _boundaryEdges];
        }
        if (_uniqueEdges is not null && _uniqueEdges.Count > 0)
        {
            newCluster._uniqueEdges = [.. _uniqueEdges];
        }
        if (_uniqueVertices is not null && _uniqueVertices.Count > 0)
        {
            newCluster._uniqueVertices = [.. _uniqueVertices];
        }
        return newCluster;
    }

    private Cluster FinishTransform(Matrix3x2 matrix, List<Kite> transformedKites)
    {
        var transformedCluster = new Cluster(transformedKites);
        // If boundary edges are already calculated, transform them as well
        if (_boundaryEdges is not null && _boundaryEdges.Count > 0)
        {
            transformedCluster._boundaryEdges = _boundaryEdges.Select(edge => edge.Transform(matrix)).ToList();
        }
        // If unique edges are already calculated, transform them as well
        if (_uniqueEdges is not null && _uniqueEdges.Count > 0)
        {
            transformedCluster._uniqueEdges = _uniqueEdges.Select(edge => edge.Transform(matrix)).ToList();
        }
        // If unique vertices are already calculated, transform them as well
        if (_uniqueVertices is not null && _uniqueVertices.Count > 0)
        {
            transformedCluster._uniqueVertices = _uniqueVertices.Select(matrix.TransformPoint).ToList();
        }
        return transformedCluster;
    }

    public Cluster Transform(Matrix3x2 matrix)
    {
        // Create a new cluster with transformed kites
        var transformedKites = this.Kites.Select(kite => kite.Transform(matrix)).ToList();
        return FinishTransform(matrix, transformedKites);
    }

    public Cluster? TransformWithoutOverlap(Matrix3x2 matrix,
        List<Kite> allKitesToTestAgainst,
        Dictionary<(int, int), bool> overlapCache)
    {
        bool hasOverlap = false;
        var transformedKites = new List<Kite>();
        foreach (var kite in Kites)
        {
            var transformedKite = kite.Transform(matrix);

            for (int i = 0; i < allKitesToTestAgainst.Count && !hasOverlap; i++)
            {
                var existingKite = allKitesToTestAgainst[i];
                // Use centroids as a unique key for the transformed kite pair.
                var c1 = existingKite.Centroid.GetHashCode();
                var c2 = transformedKite.Centroid.GetHashCode();

                // Order the centroids to create a canonical key, ensuring that
                // (c1, c2) and (c2, c1) are treated as the same pair.
                var key = c1 < c2 ? (c1, c2) : (c2, c1);

                // Check the cache, or compute and add if it's not there.
                if (overlapCache.TryGetValue(key, out var value))
                {
                    hasOverlap = value;
                }
                else
                {
                    hasOverlap = existingKite.Overlaps(transformedKite);
                    overlapCache.Add(key, hasOverlap);
                }
            }

            if (hasOverlap)
            {
                break;
            }
            // If no overlap, add the transformed kite to the list
            transformedKites.Add(transformedKite);
        }

        if (!hasOverlap)
        {
            return FinishTransform(matrix, transformedKites);
        }

        return null;
    }

    /// <summary>
    /// Creates a new cluster that is a mirror image (reflection) of this one.
    /// The reflection is performed across the Y-axis (x=0).
    /// </summary>
    /// <returns>A new, mirrored Cluster object.</returns>
    public Cluster CreateMirroredVersion()
    {
        // Use the existing Transform method to apply the mirroring effect.
        // This correctly transforms all kites and cached geometric data.
        return Transform(MirrorMatrix);
    }

    public IEnumerable<Cluster> GetClustersToTry()
    {
        yield return this;
        yield return CreateMirroredVersion();
    }

    #region Definitive Tiling Verification Logic
    private List<Edge> GetBoundaryEdges()
    {
        // Pre-allocate with estimated capacity - each kite has 4 edges
        int estimatedCapacity = Kites.Count * 4;
        var allEdges = new List<EdgeCounter>(estimatedCapacity);
        var boundaryEdges = new List<Edge>(estimatedCapacity / 2);

        // Count all edges in a single pass
        foreach (var kite in Kites)
        {
            foreach (var edge in kite.Edges)
            {
                bool foundMatch = false;

                // Check if this edge already exists in our list
                for (int i = 0; i < allEdges.Count; i++)
                {
                    if (allEdges[i].Edge.Equals(edge))
                    {
                        allEdges[i].Count++;
                        foundMatch = true;
                        break;
                    }
                }

                // If not found, add it as a new edge
                if (!foundMatch)
                {
                    allEdges.Add(new EdgeCounter(edge, 1));
                }
            }
        }

        // Extract boundary edges (edges that appear exactly once)
        for (int i = 0; i < allEdges.Count; i++)
        {
            if (allEdges[i].Count == 1)
            {
                boundaryEdges.Add(allEdges[i].Edge);
            }
        }

        return boundaryEdges;
    }

    private List<PointF> GetUniqueVertices()
    {
        var uniqueVertices = new List<PointF>();
        foreach (var v in this.Kites.SelectMany(k => k.Vertices))
        {
            if (!uniqueVertices.Any(p => p.Equals(v)))
            {
                uniqueVertices.Add(v);
            }
        }
        return uniqueVertices;
    }

    private List<Edge> GetAllUniqueEdges()
    {
        var uniqueEdges = new List<Edge>(this.Kites.Count * 4);
        foreach (var e in this.Kites.SelectMany(k => k.Edges))
        {
            if (!uniqueEdges.Any(edge => edge.Equals(e)))
            {
                uniqueEdges.Add(e);
            }
        }
        return uniqueEdges;
    }

    #endregion

    #region Check For Holes Or Gaps

    /// <summary>
    /// Finds a single, continuous, non-self-intersecting loop from a set of edges.
    /// This is the definitive path-finding algorithm based on the user's insight.
    /// </summary>
    /// <param name="allBoundaryEdges">The set of all edges to search.</param>
    /// <returns>A list of edges forming a single, simple path.</returns>
    public static List<Edge> FindAContiguousLoop(List<Edge> allBoundaryEdges)
    {
        if (allBoundaryEdges.Count == 0) return [];

        var path = new List<Edge>();
        var visitedVertices = new List<PointF>();
        // We use a HashSet for the remaining edges for efficient removal.
        var remainingEdges = new List<Edge>(allBoundaryEdges);

        // Start with an arbitrary edge.
        var startEdge = remainingEdges.First();
        var loopStartVertex = startEdge.Start; // The vertex where the loop must end.

        path.Add(startEdge);
        if (!visitedVertices.Contains(startEdge.Start))
        {
            visitedVertices.Add(startEdge.Start);
        }

        if (!visitedVertices.Contains(startEdge.End))
        {
            visitedVertices.Add(startEdge.End);
        }

        remainingEdges.Remove(startEdge);

        var currentVertex = startEdge.End;

        while (true)
        {
            Edge? nextEdge = null;

            // Find a connecting edge whose OTHER endpoint has NOT been visited yet.
            // This is the crucial check that prevents crossing into holes.
            foreach (var candidate in remainingEdges)
            {
                PointF? nextVertex = null;
                if (candidate.Start.Equals(currentVertex))
                {
                    nextVertex = candidate.End;
                }
                else if (candidate.End.Equals(currentVertex))
                {
                    nextVertex = candidate.Start;
                }

                if (nextVertex is not null)
                {
                    // We can move to the next vertex if it's either brand new OR if it's the
                    // original starting vertex, which would close the loop.
                    if (!visitedVertices.Contains(nextVertex) || nextVertex.Equals(loopStartVertex))
                    {
                        nextEdge = candidate;
                        break;
                    }
                }
            }

            if (nextEdge is not null)
            {
                // We found a valid next step.
                path.Add(nextEdge);
                var nextVertex = nextEdge.Start.Equals(currentVertex) ? nextEdge.End : nextEdge.Start;
                // If we've returned to the start, the loop is closed and complete.
                if (nextVertex.Equals(loopStartVertex))
                {
                    break;
                }

                if (!visitedVertices.Contains(nextVertex))
                {
                    visitedVertices.Add(nextVertex);
                }

                remainingEdges.Remove(nextEdge);
                currentVertex = nextVertex;
            }
            else
            {
                // No valid next step found. The simple path is complete.
                break;
            }
        }

        return path;
    }

    public bool CheckForUnfillableCorners()
    {
        const float minimumKiteAngle = 60.0f;
        const float tolerance = 0.1f;
        var perimeterPath = FindAContiguousLoop(this.BoundaryEdges);
        if (perimeterPath.Count < 3) return false;

        for (int i = 0; i < perimeterPath.Count; i++)
        {
            Edge edge1 = perimeterPath[i];
            Edge edge2 = perimeterPath[(i + 1) % perimeterPath.Count];

            PointF currentVertex, prevVertex, nextVertex;
            if (edge1.End == edge2.Start) { currentVertex = edge1.End; prevVertex = edge1.Start; nextVertex = edge2.End; }
            else if (edge1.End == edge2.End) { currentVertex = edge1.End; prevVertex = edge1.Start; nextVertex = edge2.Start; }
            else if (edge1.Start == edge2.Start) { currentVertex = edge1.Start; prevVertex = edge1.End; nextVertex = edge2.End; }
            else { currentVertex = edge1.Start; prevVertex = edge1.End; nextVertex = edge2.Start; }

            if (GetAngle(currentVertex, prevVertex, nextVertex) < minimumKiteAngle - tolerance) return true;
        }
        return false;
    }

    public bool FinalTilingHasGaps()
    {
        var allBoundaryEdges = BoundaryEdges;
        if (allBoundaryEdges.Count == 0) return false;
        var mainPerimeterPath = FindAContiguousLoop(allBoundaryEdges);
        return mainPerimeterPath.Count < allBoundaryEdges.Count;
    }

    public bool CheckForUnfillableCornersOrHasGaps()
    {
        var allBoundaryEdges = BoundaryEdges;
        if (allBoundaryEdges.Count == 0) return false;
        var perimeterPath = FindAContiguousLoop(allBoundaryEdges);
        if (perimeterPath.Count < allBoundaryEdges.Count)
        {
            return true;
        }

        const float minimumKiteAngle = 60.0f;
        const float tolerance = 0.1f;
        if (perimeterPath.Count < 3) return false;

        for (int i = 0; i < perimeterPath.Count; i++)
        {
            Edge edge1 = perimeterPath[i];
            Edge edge2 = perimeterPath[(i + 1) % perimeterPath.Count];

            PointF currentVertex, prevVertex, nextVertex;
            if (edge1.End == edge2.Start) { currentVertex = edge1.End; prevVertex = edge1.Start; nextVertex = edge2.End; }
            else if (edge1.End == edge2.End) { currentVertex = edge1.End; prevVertex = edge1.Start; nextVertex = edge2.Start; }
            else if (edge1.Start == edge2.Start) { currentVertex = edge1.Start; prevVertex = edge1.End; nextVertex = edge2.End; }
            else { currentVertex = edge1.Start; prevVertex = edge1.End; nextVertex = edge2.Start; }

            if (GetAngle(currentVertex, prevVertex, nextVertex) < minimumKiteAngle - tolerance) return true;
        }
        return false;
    }

    #endregion

    #region Helper Methods for Tiling Analysis

    /// <summary>
    /// Calculates the positive interior angle in degrees at corner 'b' of the triangle 'abc'.
    /// Using Math.Abs ensures we always get a positive angle, which simplifies summing logic.
    /// </summary>
    private static float GetAngle(PointF b, PointF a, PointF c)
    {
        var ab = new PointF(a.X - b.X, a.Y - b.Y);
        var cb = new PointF(c.X - b.X, c.Y - b.Y);

        float dot = ab.X * cb.X + ab.Y * cb.Y;
        float cross = ab.X * cb.Y - ab.Y * cb.X;

        // Atan2 gives a result between -PI and +PI.
        float angle = (float)Math.Atan2(cross, dot);

        // By taking the absolute value of the result in degrees, we guarantee
        // a positive interior angle, removing any ambiguity.
        return Math.Abs((float)(angle * 180 / Math.PI));
    }

    /// <summary>
    /// Calculates the geometric center of the entire cluster of kites.
    /// </summary>
    public PointF GetClusterCenter()
    {
        var allVertices = Kites.SelectMany(k => k.Vertices).ToList();
        float avgX = allVertices.Average(v => v.X);
        float avgY = allVertices.Average(v => v.Y);
        return new PointF(avgX, avgY);
    }
    #endregion

    public bool Equals(Cluster? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Kites.SequenceEqual(other.Kites);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Cluster)obj);
    }

    public override int GetHashCode()
    {
        return Kites.GetHashCode();
    }

    public static bool operator ==(Cluster? left, Cluster? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Cluster? left, Cluster? right)
    {
        return !Equals(left, right);
    }
    public void GenerateImage(string filePath, int width = 800, int height = 800)
    {
        if (File.Exists(filePath))
        {
            return;
        }

        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.Clear(Color.White));

        if (Kites.Count == 0) return;

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var v in Kites.SelectMany(k => k.Vertices))
        {
            if (v.X < minX) minX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.X > maxX) maxX = v.X;
            if (v.Y > maxY) maxY = v.Y;
        }

        // Prevent division by zero if it's a single point
        if (maxX - minX < 1e-6) maxX = minX + 1;
        if (maxY - minY < 1e-6) maxY = minY + 1;

        float scale = Math.Min((width - 50) / (maxX - minX), (height - 50) / (maxY - minY));
        float offsetX = (width - (maxX - minX) * scale) / 2 - minX * scale;
        float offsetY = (height - (maxY - minY) * scale) / 2 - minY * scale;

        // Use a different color for each kite to see them clearly
        var colorPen = new SolidPen(Color.Black, 1.5f);
        var colors = new Color[] { Color.LightBlue, Color.LightCoral, Color.LightGreen, Color.LightGoldenrodYellow, Color.Plum, Color.LightSalmon };
        int colorIndex = 0;

        image.Mutate(ctx =>
        {
            foreach (var kite in Kites)
            {
                var fillBrush = new SolidBrush(colors[colorIndex % colors.Length]);
                colorIndex++;

                var points = kite.Vertices.Select(v => new SixLabors.ImageSharp.PointF(v.X * scale + offsetX, height - (v.Y * scale + offsetY))).ToArray();
                ctx.FillPolygon(fillBrush, points);
                ctx.DrawPolygon(colorPen, points);
            }
        });

        image.SaveAsPng(filePath);
    }

    public static void GenerateHeeschImage(HeeschResult result, string filePath, int width = 1200, int height = 1200)
    {
        using var image = new Image<Rgba32>(width, height);

        var allKites = result.GetFinalArrangement();
        if (allKites.Count == 0)
        {
            image.Mutate(ctx => ctx.Clear(Color.White));
            image.SaveAsPng(filePath);
            return;
        }

        // --- Calculate Bounds for Centering and Scaling ---
        float minX = allKites.Min(k => k.Vertices.Min(PointF.GetX));
        float maxX = allKites.Max(k => k.Vertices.Max(PointF.GetX));
        float minY = allKites.Min(k => k.Vertices.Min(PointF.GetY));
        float maxY = allKites.Max(k => k.Vertices.Max(PointF.GetY));

        float scale = Math.Min((width - 50f) / (maxX - minX), (height - 50f) / (maxY - minY));
        float offsetX = (width - (maxX - minX) * scale) / 2f - minX * scale;
        float offsetY = (height - (maxY - minY) * scale) / 2f - minY * scale;

        // --- Color Palette for the Coronas (Layers) ---
        var coronaColors = new Color[]
        {
            Color.SlateGray, Color.Teal, Color.SaddleBrown, Color.Olive,
            Color.Tomato, Color.MediumSeaGreen, Color.RoyalBlue, Color.Orchid, Color.Gold
        };

        image.Mutate(ctx =>
        {
            ctx.Clear(Color.White);

            // --- Draw the CORONAS (Layers 1 to n) ---
            for (int i = 0; i < result.Coronas.Count; i++)
            {
                var corona = result.Coronas[i];
                Color baseColor = coronaColors[i % coronaColors.Length];
                var baseColorPixelType = baseColor.ToPixel<Rgba32>();

                var fillBrush = new SolidBrush(baseColor.WithAlpha(0.588f)); // 150/255
                var thinInternalPen = new SolidPen(baseColor.WithAlpha(0.784f), 1.0f); // 200/255
                var thickBoundaryPen = new SolidPen(Color.FromRgba((byte)(baseColorPixelType.R / 2), (byte)(baseColorPixelType.G / 2), (byte)(baseColorPixelType.B / 2), 220), 2.5f);

                foreach (var cluster in corona)
                {
                    // 1. Fill all kites in the cluster first.
                    foreach (var kite in cluster.Kites)
                    {
                        var points = kite.Vertices.Select(v => new SixLabors.ImageSharp.PointF(v.X * scale + offsetX, height - (v.Y * scale + offsetY))).ToArray();
                        ctx.FillPolygon(fillBrush, points);
                    }

                    // 2. Draw the thin INTERNAL edges of the cluster.
                    var allEdges = cluster.UniqueEdges;
                    var boundaryEdges = new List<Edge>(cluster.BoundaryEdges);
                    var internalEdges = allEdges.Where(e => !boundaryEdges.Contains(e));

                    foreach (var edge in internalEdges)
                    {
                        ctx.DrawLine(thinInternalPen,
                            new SixLabors.ImageSharp.PointF(edge.Start.X * scale + offsetX, height - (edge.Start.Y * scale + offsetY)),
                            new SixLabors.ImageSharp.PointF(edge.End.X * scale + offsetX, height - (edge.End.Y * scale + offsetY)));
                    }

                    // 3. Draw the thick BOUNDARY edges of the cluster on top.
                    foreach (var edge in boundaryEdges)
                    {
                        ctx.DrawLine(thickBoundaryPen,
                            new SixLabors.ImageSharp.PointF(edge.Start.X * scale + offsetX, height - (edge.Start.Y * scale + offsetY)),
                            new SixLabors.ImageSharp.PointF(edge.End.X * scale + offsetX, height - (edge.End.Y * scale + offsetY)));
                    }
                }
            }

            // --- Draw the Central PROTOTILE (on top and fully opaque) ---
            var originalColors = new Color[] { Color.LightCoral, Color.LightSkyBlue, Color.Plum, Color.LightGreen, Color.LightGoldenrodYellow, Color.Orange, Color.Turquoise, Color.Pink };
            var pen = new SolidPen(Color.Black, 2.5f);
            for (int i = 0; i < result.Prototile.Kites.Count; i++)
            {
                var brush = new SolidBrush(originalColors[i % originalColors.Length]);
                var points = result.Prototile.Kites[i].Vertices.Select(v => new SixLabors.ImageSharp.PointF(v.X * scale + offsetX, height - (v.Y * scale + offsetY))).ToArray();
                ctx.FillPolygon(brush, points);
                ctx.DrawPolygon(pen, points);
            }
        });

        image.SaveAsPng(filePath);
    }
}