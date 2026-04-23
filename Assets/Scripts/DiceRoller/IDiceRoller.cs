using System.Collections.Generic;

public interface IDiceRoller
{
    // Roll using standard NdM +/- K notation, e.g. "2d6+1" or "1d20-2".
    List<int> RollNotation(string notation, out int total);

    // Roll N dice with given sides; returns list of rolls and out total (sum).
    List<int> Roll(int count, int sides, out int total);

    // Roll N dice with sides and take the highest single die
    int RollAndTakeHighest(int count, int sides);
}
