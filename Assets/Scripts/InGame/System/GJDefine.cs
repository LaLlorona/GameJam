namespace GameJam.InGame.System
{
    // Fixed constants only. Tunable gameplay values live in GJInGameParameter.
    public static class GJDefine
    {
        // Layer
        public const string GROUND_LAYER_NAME = "Ground";

        // Map
        // LDtk objects whose name contains this (case-insensitive) are moved to the ground layer.
        public const string MAP_COLLISION_OBJECT_NAME = "Collisions";
        public const string MAP_BOUNDS_OBJECT_NAME = "GJMapBounds";
        public const string MAP_BOUNDS_LAYER_NAME = "Ignore Raycast";

        // Effect
        // Used only when GJAssetHolder has no flow trail prefab assigned.
        public const string FLOW_TRAIL_DEFAULT_SHADER_NAME = "Sprites/Default";
        public const int FLOW_TRAIL_SORTING_ORDER = 100;
    }
}
