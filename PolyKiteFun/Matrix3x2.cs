namespace PolyKiteFun;
/// <summary>
/// Represents a 3x2 matrix for 2D affine transformations (translation, rotation).
/// </summary>
public struct Matrix3x2
{
    public float M11, M12, M21, M22, OffsetX, OffsetY;

    /// <summary>
    /// Creates a new Matrix3x2.
    /// </summary>
    public Matrix3x2(float m11, float m12, float m21, float m22, float offsetX, float offsetY)
    {
        M11 = m11; M12 = m12; M21 = m21; M22 = m22; OffsetX = offsetX; OffsetY = offsetY;
    }

    /// <summary>
    /// Initializes a new identity matrix. This ensures 'new Matrix3x2()' is valid.
    /// </summary>
    public Matrix3x2()
    {
        M11 = 1; M12 = 0; M21 = 0; M22 = 1; OffsetX = 0; OffsetY = 0;
    }

    /// <summary>
    /// Gets the identity matrix, which performs no transformation.
    /// </summary>
    public static Matrix3x2 Identity => new(1, 0, 0, 1, 0, 0);

    /// <summary>
    /// Multiplies two matrices together, combining their transformations.
    /// The order is crucial: the transformation of 'b' is applied AFTER 'a'.
    /// </summary>
    public static Matrix3x2 Multiply(Matrix3x2 a, Matrix3x2 b)
    {
        return new Matrix3x2(
            a.M11 * b.M11 + a.M12 * b.M21,
            a.M11 * b.M12 + a.M12 * b.M22,
            a.M21 * b.M11 + a.M22 * b.M21,
            a.M21 * b.M12 + a.M22 * b.M22,
            a.OffsetX * b.M11 + a.OffsetY * b.M21 + b.OffsetX,
            a.OffsetX * b.M12 + a.OffsetY * b.M22 + b.OffsetY
        );
    }

    /// <summary>
    /// Appends a translation to this matrix.
    /// </summary>
    public void Translate(float x, float y)
    {
        var translationMatrix = new Matrix3x2(1, 0, 0, 1, x, y);
        this = Multiply(this, translationMatrix);
    }

    /// <summary>
    /// Appends a rotation (in degrees) to this matrix around the origin (0,0).
    /// </summary>
    public void Rotate(float angleDegrees)
    {
        float angleRadians = (float)(angleDegrees * Math.PI / 180.0);
        float cos = (float)Math.Cos(angleRadians);
        float sin = (float)Math.Sin(angleRadians);

        var rotationMatrix = new Matrix3x2(cos, sin, -sin, cos, 0, 0);
        this = Multiply(this, rotationMatrix);
    }

    /// <summary>
    /// Appends a rotation (in degrees) to this matrix around a specific point.
    /// </summary>
    public void RotateAt(float angleDegrees, PointF center)
    {
        // Correct sequence:
        // 1. Move the center point TO the origin.
        // 2. Perform the rotation around the origin.
        // 3. Move the center point BACK to its original position.
        this.Translate(-center.X, -center.Y);
        this.Rotate(angleDegrees);
        this.Translate(center.X, center.Y);
    }

    public readonly List<PointF> TransformPoints(IList<PointF> points)
    {
        List<PointF> result = new(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            result.Add(TransformPoint(points[i]));
        }

        return result;
    }

    public readonly PointF TransformPoint(PointF point)
    {
        // Store the original coordinates before performing any calculations.
        float originalX = point.X;
        float originalY = point.Y;

        // Calculate the new X using only original values.
        float newX = originalX * M11 + originalY * M21 + OffsetX;

        // Calculate the new Y using only original values.
        float newY = originalX * M12 + originalY * M22 + OffsetY;

        return new PointF(newX, newY);
    }
}
