using System.Collections.Generic;
using System.Linq;

namespace ChessNEA.Logic.Objects;

public class IntArrayComparer : IEqualityComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        if (x is null || y is null)
        {
            return false;
        }
        
        if (x.Length != y.Length)
        {
            return false;
        }

        return !x.Where((t, i) => t != y[i]).Any();
    }

    public int GetHashCode(int[] obj)
    {
        return obj.Aggregate(0, (current, i) => current ^ i);
    }
}