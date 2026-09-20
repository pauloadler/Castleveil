using UnityEngine;

namespace Game.Player
{
    public enum Direction8
    {
        North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest
    }

    public static class DirectionResolver
    {
        // World XY: +Y is north, +X is east. Zero input preserves the supplied direction.
        public static Direction8 Resolve(Vector2 direction, Direction8 fallback = Direction8.South)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return fallback;
            float clockwiseDegrees = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            int sector = Mathf.FloorToInt((clockwiseDegrees + 22.5f + 360f) / 45f) % 8;
            return (Direction8)sector;
        }
    }
}
