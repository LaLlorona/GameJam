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

        // Post Process
        // Higher than the default global volume so the Flow look wins while blended in.
        public const float FLOW_VOLUME_PRIORITY = 10f;
        // Global shader properties read by the GameJam/FlowSpeedEffect full screen shader.
        public const string FLOW_SPEED_INTENSITY_PROPERTY = "_GJFlowSpeedIntensity";
        public const string FLOW_SPEED_CENTER_PROPERTY = "_GJFlowSpeedCenter";
        public const string FLOW_BLUR_STRENGTH_PROPERTY = "_GJFlowBlurStrength";
        public const string FLOW_BLUR_INNER_RADIUS_PROPERTY = "_GJFlowBlurInnerRadius";
        public const string FLOW_BLUR_FALLOFF_PROPERTY = "_GJFlowBlurFalloff";
        public const string FLOW_LINE_COUNT_PROPERTY = "_GJFlowLineCount";
        public const string FLOW_LINE_DENSITY_PROPERTY = "_GJFlowLineDensity";
        public const string FLOW_LINE_WIDTH_PROPERTY = "_GJFlowLineWidth";
        public const string FLOW_LINE_INNER_RADIUS_PROPERTY = "_GJFlowLineInnerRadius";
        public const string FLOW_LINE_FALLOFF_PROPERTY = "_GJFlowLineFalloff";
        public const string FLOW_LINE_OPACITY_PROPERTY = "_GJFlowLineOpacity";
        public const string FLOW_LINE_COLOR_PROPERTY = "_GJFlowLineColor";
        public const string FLOW_LINE_FLICKER_RATE_PROPERTY = "_GJFlowLineFlickerRate";
    }
}
