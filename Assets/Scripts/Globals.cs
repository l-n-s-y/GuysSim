using UnityEngine;

public static class Globals {

    public static float INITIAL_TIMESCALE = 1f;

    public static float MIN_ZOOM_SCALE = 1f;
    public static float MAX_ZOOM_SCALE = 5;

    public static float DEG2RAD = Mathf.PI / 180;
    public static float RAD2DEG = 57.2958f;
    public static float RToGrey = 0.299f;
    public static float GToGrey = 0.587f;
    public static float BToGrey = 0.114f;

    // public static Vector3 UNSET_DESTINATION = Vector3.positiveInfinity;
    public static Vector3 UNSET_DESTINATION = Vector3.zero;
    public static float FOOD_ENERGY = 20f;
    public static float FOOD_DECAY_RATE = 10; // seconds

    public static int MAX_CHILD_COUNT = 5;

    // public static float YEAR = 300f;
    public static float YEAR = 3;
    public static float MONTH = 2f;

    public static int CHILD_STARTING_AGE = 0;
    public static int ADULT_STARTING_AGE = 21;
    
    public static float LIFE_EXPECTANCY = 50; // years

    public static float DEFAULT_MUTATION_RANGE = 0.05f;

    public static float ALBINO_MUTATION_FREQUENCY = 500;

    public static float HYPER_MUTATION_POSSIBILITY = 500; // 1/x odds
    public static float HYPER_MUTATION_MULTIPLIER = 10;

    public static Vector3 Vector3Clamp(Vector3 s, float min, float max) {
        Vector3 v = s;
        v.x = Mathf.Clamp(s.x,min,max);
        v.y = Mathf.Clamp(s.y,min,max);
        v.z = Mathf.Clamp(s.z,min,max);
        return v;
    }

    public static float Vector2Angle(Vector2 a, Vector2 b) {
        float xDiff = a.x - b.x;
        float yDiff = a.y - b.y;

        return Mathf.Atan2(xDiff,yDiff);
    }

    public static void CopyValues<T>(T from, T to) {
        var json = JsonUtility.ToJson(from);
        JsonUtility.FromJsonOverwrite(json, to);
    }
}