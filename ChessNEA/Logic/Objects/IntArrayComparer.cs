using System.Collections.Generic;
using System.Linq;

namespace ChessNEA.Logic.Objects;

public class IntArrayComparer : IEqualityComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        if (x is null || y is null) return false;
        if (x.Length != y.Length) return false;

        for (int i = 0; i < x.Length; i++)
            if (x[i] != y[i])
                return false;
        return true;
    }

    public int GetHashCode(int[] obj)
    {
        return obj.Aggregate(0, (current, i) => current ^ i);
    }
}