namespace GameJam.InGame.System
{
    public static class GJDefine
    {
        // Layer
        public const string GROUND_LAYER_NAME = "Ground";

        // Player Movement
        public const float PLAYER_MOVE_SPEED = 7f;
        public const float PLAYER_JUMP_VELOCITY = 12f;
        public const int PLAYER_MAX_JUMP_COUNT = 3;
        public const float PLAYER_GRAVITY_SCALE = 3f;

        // Ground Check
        public const float GROUND_CHECK_DISTANCE = 0.05f;
        // Checked slightly narrower than the collider so touching a wall isn't treated as grounded.
        public const float GROUND_CHECK_WIDTH_RATIO = 0.9f;
    }
}
