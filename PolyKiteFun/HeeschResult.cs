namespace PolyKiteFun;
public class HeeschResult(Cluster prototile, int heeschNumber, List<List<Cluster>> coronas)
{
    /// <summary> The prototile that was tested. </summary>
    public Cluster Prototile { get; } = prototile;

    /// <summary> The highest number of complete layers successfully placed. </summary>
    public int HeeschNumber { get; } = heeschNumber;

    /// <summary> A list where each item is a list of clusters forming one corona. </summary>
    public List<List<Cluster>> Coronas { get; } = coronas ?? [];

    public List<Kite> GetFinalArrangement()
    {
        var arrangement = new List<Kite>(Prototile.Kites);
        Coronas.ForEach(corona => corona.ForEach(cluster => arrangement.AddRange(cluster.Kites)));
        return arrangement;
    }
}
