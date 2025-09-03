namespace PolyKiteFun;
public class Edge(PointF start, PointF end) : IEquatable<Edge>
{
    private PointF? _midpoint;
    public PointF Start { get; } = new(start);
    public PointF End { get; } = new(end);

    public PointF Midpoint => _midpoint ??= GetMidpoint();

    // --- Core Properties ---

    public float Length => PointF.Distance(Start, End);

    private PointF GetMidpoint()
    {
        return new PointF((Start.X + End.X) / 2f, (Start.Y + End.Y) / 2f);
    }

    // This transformation logic is moved here from the old Cluster class to be self-contained.
    public Matrix3x2 ComputeTransformation(Edge targetEdge)
    {
        var sourceVec = new PointF(this.End.X - this.Start.X, this.End.Y - this.Start.Y);
        var targetVec = new PointF(targetEdge.End.X - targetEdge.Start.X, targetEdge.End.Y - targetEdge.Start.Y);

        float angle = (float)Math.Atan2(targetVec.Y, targetVec.X) - (float)Math.Atan2(sourceVec.Y, sourceVec.X);
        float angleDegrees = (float)(angle * 180 / Math.PI);

        // Start with a fresh identity matrix.
        var matrix = new Matrix3x2(); // Uses the new default constructor for Identity

        // Step 1: Translate the source edge's start point to the origin.
        matrix.Translate(-this.Start.X, -this.Start.Y);

        // Step 2: Rotate around the origin to align the vectors.
        matrix.Rotate(angleDegrees);

        // Step 3: Translate from the origin to the target edge's start point.
        matrix.Translate(targetEdge.Start.X, targetEdge.Start.Y);

        return matrix;
    }

    public Edge Transform(Matrix3x2 matrix)
    {
        // Apply the transformation to both endpoints of the edge.
        var transformedStart = matrix.TransformPoint(Start);
        var transformedEnd = matrix.TransformPoint(End);
        var result = new Edge(transformedStart, transformedEnd);
        if (_midpoint is not null)
        {
            // If the midpoint was already calculated, transform it as well.
            result._midpoint = matrix.TransformPoint(_midpoint);
        }

        return result;
    }

    public IEnumerable<Edge> GetTargetEdgesToCover()
    {
        yield return this; // The original edge
        yield return Mirrored(); // The standard "back-to-back" tiling
    }

    public Edge Mirrored()
    {
        // Create a new edge that is the mirror image of this one.
        // This is done by swapping the start and end points.
        var result = new Edge(End, Start)
        {
            _midpoint = _midpoint
        };
        return result;
    }

    // --- IEquatable<T> Implementation ---

    /// <summary>
    /// Provides a robust, direction-agnostic equality check for edges.
    /// </summary>
    public bool Equals(Edge? other)
    {
        if (other is null) return false;

        // Since PointF has its own robust == operator, this code is clean and reliable.
        // Check for equality in both directions: (A->B == A'->B') or (A->B == B'->A').
        return (this.Start == other.Start && this.End == other.End) ||
               (this.Start == other.End && this.End == other.Start);
    }

    // --- Standard Equality Overrides ---

    /// <summary>
    /// Overrides the base Equals method to ensure consistent behavior.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Edge);
    }

    /// <summary>
    /// Provides a direction-agnostic hash code. Two edges that are considered
    /// equal will always produce the same hash code.
    /// </summary>
    public override int GetHashCode()
    {
        throw new InvalidOperationException("You should not use this class in hash based data structures!");
    }

    // --- Operator Overloads for Convenience ---
    public static bool operator ==(Edge? e1, Edge? e2)
    {
        if (e1 is null)
        {
            return e2 is null;
        }
        return e1.Equals(e2);
    }

    public static bool operator !=(Edge e1, Edge e2)
    {
        return !(e1 == e2);
    }
}

public class EdgeCounter(Edge edge, int count)
{
    public Edge Edge { get; set; } = edge;
    public int Count { get; set; } = count;
}