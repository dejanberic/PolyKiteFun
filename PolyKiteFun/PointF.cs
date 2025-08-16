using System.Text.Json.Serialization;

namespace PolyKiteFun;

public class PointF : IEquatable<PointF>
{
    private const float Tolerance = 1e-5f;
    private const int HashFactor = (int)(1 / Tolerance);
    private readonly int _hashCode;

    public float X { get; }
    public float Y { get; }

    [JsonConstructor]
    public PointF(float x, float y)
    {
        X = x;
        Y = y;
        _hashCode = HashCode.Combine(
            (int)(X * HashFactor),
            (int)(Y * HashFactor)
        );
    }

    public PointF(PointF copy)
    {
        X = copy.X;
        Y = copy.Y;
        _hashCode = copy._hashCode;
    }

    public static float GetX(PointF point) => point.X;

    public static float GetY(PointF point) => point.Y;

    // --- Other Helpers ---
    public static float Distance(PointF p1, PointF p2)
    {
        return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }

    // --- IEquatable<T> Implementation ---
    /// <summary>
    /// Provides a strongly-typed, tolerance-based equality check.
    /// </summary>
    public bool Equals(PointF? other)
    {
        if (other is null) return false;
        // Delegate the logic to our single, consistent comparer.
        return Math.Abs(this.X - other.X) < Tolerance && Math.Abs(this.Y - other.Y) < Tolerance;
    }

    // --- Standard Equality Overrides ---
    /// <summary>
    /// Overrides the base Equals method to ensure consistent behavior.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as PointF);
    }

    /// <summary>
    /// Overrides GetHashCode to ensure that two points considered "equal"
    /// by the comparer will always produce the same hash code.
    /// </summary>
    public override int GetHashCode()
    {
        return _hashCode;
    }

    // --- Operator Overloads for Convenience ---
    public static bool operator ==(PointF? p1, PointF? p2)
    {
        if (p1 is null)
        {
            return p2 is null;
        }
        return p1.Equals(p2);
    }

    public static bool operator !=(PointF p1, PointF p2)
    {
        return !(p1 == p2);
    }
}