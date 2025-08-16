using System.Text.Json.Serialization;

namespace PolyKiteFun;

public record Bounds(float MinX, float MaxX, float MinY, float MaxY)
{
    public Bounds Transform(Matrix3x2 matrix)
    {
        // Transform the corners of the bounding box using the matrix
        var topLeft = matrix.TransformPoint(new PointF(MinX, MinY));
        var topRight = matrix.TransformPoint(new PointF(MaxX, MinY));
        var bottomLeft = matrix.TransformPoint(new PointF(MinX, MaxY));
        var bottomRight = matrix.TransformPoint(new PointF(MaxX, MaxY));
        // Calculate new bounds
        float newMinX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        float newMaxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        float newMinY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        float newMaxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
        return new Bounds(newMinX, newMaxX, newMinY, newMaxY);
    }
}

public class Kite : IEquatable<Kite>
{
    private List<Edge>? _edges;
    private PointF? _centroid;
    private Bounds? _bounds;
    
    [JsonIgnore]
    public Bounds Bounds => _bounds ??= CalculateBounds();

    public List<PointF> Vertices { get; }

    [JsonIgnore]
    public List<Edge> Edges
    {
        get
        {
            if (_edges is null || _edges.Count == 0)
            {
                _edges = GetEdges();
            }

            return _edges;
        }
    }

    [JsonIgnore]
    public PointF Centroid => _centroid ??= GetCentroid();

    public Kite()
    {
        Vertices =
        [
            new(0, 0),
            new(0.5f, 0),
            new(0.5f, (float)(Math.Sqrt(3) / 6)),
            new(0.25f, (float)(Math.Sqrt(3) / 4))
        ];
    }
    [JsonConstructor]
    public Kite(List<PointF> vertices)
    {
        Vertices = vertices;
    }

    /// <summary>
    /// Calculates the geometric center (centroid) of the kite.
    /// </summary>
    private PointF GetCentroid()
    {
        float avgX = 0;
        float avgY = 0;
        foreach (var v in Vertices)
        {
            avgX += v.X;
            avgY += v.Y;
        }
        return new PointF(avgX / Vertices.Count, avgY / Vertices.Count);
    }

    private static float OrientationCrossProduct(PointF p, PointF q, PointF r)
    {
        return (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
    }

    /// <summary>
    /// Given three collinear points p, q, r, checks if point q lies on segment 'pr'.
    /// </summary>
    private static bool OnSegment(PointF p, PointF q, PointF r)
    {
        return (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y));
    }

    private List<Edge> GetEdges()
    {
        var edges = new List<Edge>(Vertices.Count / 2);
        for (int i = 0; i < Vertices.Count; i++)
        {
            int next = (i + 1) % Vertices.Count;
            edges.Add(new Edge(Vertices[i], Vertices[next]));
        }
        return edges;
    }

    public Kite Transform(Matrix3x2 matrix)
    {
        var result = new Kite(matrix.TransformPoints(Vertices));
        if (_edges is not null && _edges.Count > 0)
        {
            result._edges = new List<Edge>(_edges.Count);
            foreach (var edge in _edges)
            {
                result._edges.Add(edge.Transform(matrix));
            }
        }

        if (_centroid is not null)
        {
            result._centroid = matrix.TransformPoint(_centroid);
        }

        if (_bounds is not null)
        {
            result._bounds = _bounds.Transform(matrix);
        }

        return result;
    }

    public bool IsStrictlyInside(PointF p)
    {
        const float epsilon = 1e-5f;
        int? sign = null;

        foreach (var edge in Edges)
        {
            // Direct vector calculations without creating new objects
            float edgeVecX = edge.End.X - edge.Start.X;
            float edgeVecY = edge.End.Y - edge.Start.Y;
            float toPX = p.X - edge.Start.X;
            float toPY = p.Y - edge.Start.Y;

            // Cross product calculation
            float cross = edgeVecX * toPY - edgeVecY * toPX;

            if (Math.Abs(cross) < epsilon)
            {
                return false;
            }

            int currentSign = Math.Sign(cross);

            if (sign == null)
            {
                sign = currentSign;
            }
            else if (sign != currentSign)
            {
                return false;
            }
        }

        return true;
    }
    private static bool FastEdgesIntersect(Edge e1, Edge e2)
    {
        PointF p1 = e1.Start, q1 = e1.End;
        PointF p2 = e2.Start, q2 = e2.End;

        // Calculate orientations using cross products
        float o1 = OrientationCrossProduct(p1, q1, p2);
        float o2 = OrientationCrossProduct(p1, q1, q2);
        float o3 = OrientationCrossProduct(p2, q2, p1);
        float o4 = OrientationCrossProduct(p2, q2, q1);

        const float epsilon = 1e-5f;

        // General case: The segments cross each other
        if ((o1 * o2 < 0) && (o3 * o4 < 0))
            return true;

        // Special Cases for collinear points
        if (Math.Abs(o1) < epsilon && OnSegment(p1, p2, q1)) return true;
        if (Math.Abs(o2) < epsilon && OnSegment(p1, q2, q1)) return true;
        if (Math.Abs(o3) < epsilon && OnSegment(p2, p1, q2)) return true;
        if (Math.Abs(o4) < epsilon && OnSegment(p2, q1, q2)) return true;

        return false;
    }

    /// <summary>
    /// Optimized method to calculate bounding box of kite.
    /// </summary>
    private Bounds CalculateBounds()
    {
        var vertices = Vertices;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            if (v.X < minX) minX = v.X;
            if (v.X > maxX) maxX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.Y > maxY) maxY = v.Y;
        }

        return new Bounds(minX, maxX, minY, maxY);
    }

    public bool Overlaps(Kite other)
    {
        // 1. Quick AABB (bounding box) rejection test
        var thisBounds = this.Bounds;
        var otherBounds = other.Bounds;
        
        if (thisBounds.MaxX < otherBounds.MinX || thisBounds.MinX > otherBounds.MaxX || 
            thisBounds.MaxY < otherBounds.MinY || thisBounds.MinY > otherBounds.MaxY)
        {
            return false;
        }

        // 2. Fast check for coincidence - cache properties to avoid recalculation
        var thisCentroid = this.Centroid;
        var otherCentroid = other.Centroid;
        if (thisCentroid.Equals(otherCentroid))
        {
            return true;
        }

        // 3. Vertex invasion checks - unrolled since kites always have 4 vertices
        var thisVertices = this.Vertices;
        var otherVertices = other.Vertices;

        // Check if any vertex of this kite is inside the other kite
        for (int i = 0; i < 4; i++)
        {
            if (other.IsStrictlyInside(thisVertices[i]))
                return true;
        }

        // Check if any vertex of the other kite is inside this kite
        for (int i = 0; i < 4; i++)
        {
            if (this.IsStrictlyInside(otherVertices[i]))
                return true;
        }

        // 4. Edge intersection tests - cache edges to avoid recalculation
        var thisEdges = this.Edges;
        var otherEdges = other.Edges;

        // Check all edge pairs for intersection
        for (int i = 0; i < thisEdges.Count; i++)
        {
            var e1 = thisEdges[i];
            for (int j = 0; j < otherEdges.Count; j++)
            {
                var e2 = otherEdges[j];

                // Skip if edges are identical
                if (e1.Equals(e2)) continue;

                // Skip if edges meet at endpoints (valid connection)
                if (e1.Start.Equals(e2.Start) || e1.Start.Equals(e2.End) ||
                    e1.End.Equals(e2.Start) || e1.End.Equals(e2.End))
                {
                    // Check if midpoints are inside the other shape
                    if (this.IsStrictlyInside(e2.Midpoint) ||
                        other.IsStrictlyInside(e1.Midpoint))
                    {
                        return true;
                    }
                    continue;
                }

                // Check for true edge intersection
                if (FastEdgesIntersect(e1, e2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool Equals(Kite? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Vertices.SequenceEqual(other.Vertices);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Kite)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Vertices[0], Vertices[1], Vertices[2], Vertices[3]); //kite should always have 4 vertices
    }

    public static bool operator ==(Kite? left, Kite? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Kite? left, Kite? right)
    {
        return !Equals(left, right);
    }
}