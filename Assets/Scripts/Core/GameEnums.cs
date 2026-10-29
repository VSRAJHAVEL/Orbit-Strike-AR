namespace OrbitStrike.Core
{
    public enum GameState
    {
        None = 0,
        Menu = 1,
        SurfaceScan = 2,
        PadPlacement = 3,
        Aiming = 4,
        Countdown = 5,
        Launching = 6,
        FlightTracking = 7,
        TargetHit = 8,
        Miss = 9,
        LevelComplete = 10,
        GameOver = 11,
        Shop = 12,
        Settings = 13
    }

    public enum RocketType
    {
        Basic = 0,
        Heavy = 1,
        Speedster = 2,
        Precision = 3,
        Guided = 4,
        Split = 5
    }

    public enum DifficultyLevel
    {
        Casual = 0,   // Full trajectory dots (30 dots)
        Standard = 1, // Medium trajectory dots (15 dots)
        Expert = 2    // Short trajectory dots (6 dots)
    }

    public enum TargetTier
    {
        Outer25 = 0,
        Middle50 = 1,
        Bullseye100 = 2,
        Shield = 3,
        MovingDrone = 4,
        BossCore = 5
    }

    public enum PowerUpType
    {
        None = 0,
        GuidedMissile = 1,
        SplitRocket = 2,
        SlowMo = 3,
        BiggerBlast = 4
    }

    public enum WorldType
    {
        MojaveDesert = 0,
        FrostpeakArctic = 1,
        NeoCity = 2,
        CanopyJungle = 3,
        LunarBase = 4
    }
}

