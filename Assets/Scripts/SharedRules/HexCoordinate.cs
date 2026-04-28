using System;
using System.Collections.Generic;

namespace KnightsAndGM.Shared
{
    [Serializable]
    public struct OffsetCoordinate : IEquatable<OffsetCoordinate>
    {
        public OffsetCoordinate(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public int Column { get; }
        public int Row { get; }

        public bool Equals(OffsetCoordinate other)
        {
            return Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is OffsetCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Column * 397) ^ Row;
            }
        }

        public override string ToString()
        {
            return $"{Column},{Row}";
        }
    }

    [Serializable]
    public struct HexCoordinate : IEquatable<HexCoordinate>
    {
        private static readonly HexCoordinate[] Directions =
        {
            new HexCoordinate(1, 0),
            new HexCoordinate(1, -1),
            new HexCoordinate(0, -1),
            new HexCoordinate(-1, 0),
            new HexCoordinate(-1, 1),
            new HexCoordinate(0, 1)
        };

        public HexCoordinate(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }
        public int R { get; }
        public int S => -Q - R;

        public static HexCoordinate FromOddRowOffset(int column, int row)
        {
            var q = column - ((row - (row & 1)) / 2);
            return new HexCoordinate(q, row);
        }

        public OffsetCoordinate ToOddRowOffset()
        {
            var column = Q + ((R - (R & 1)) / 2);
            return new OffsetCoordinate(column, R);
        }

        public HexCoordinate Neighbor(int direction)
        {
            if (direction < 0 || direction >= Directions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Direction must be between 0 and 5.");
            }

            var delta = Directions[direction];
            return new HexCoordinate(Q + delta.Q, R + delta.R);
        }

        public IEnumerable<HexCoordinate> GetNeighbors()
        {
            for (var i = 0; i < Directions.Length; i++)
            {
                yield return Neighbor(i);
            }
        }

        public int DistanceTo(HexCoordinate other)
        {
            return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
        }

        public bool Equals(HexCoordinate other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }

        public override string ToString()
        {
            return $"{Q},{R}";
        }

        public static bool operator ==(HexCoordinate left, HexCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoordinate left, HexCoordinate right)
        {
            return !left.Equals(right);
        }
    }

    public static class HexAdjacency
    {
        public static bool AreNeighbors(HexCoordinate a, HexCoordinate b)
        {
            return a.DistanceTo(b) == 1;
        }
    }

}
