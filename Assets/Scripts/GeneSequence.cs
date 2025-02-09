using UnityEngine;

public class GeneSequence : MonoBehaviour
{
    
    Color gColor = Guy.DEFAULT_COLOR;
    public Color Color {
        get { return gColor; }
        set { gColor = value; }
    }
    float gWalkSpeed = Guy.DEFAULT_WALK_SPEED;
    public float WalkSpeed {
        get { return gWalkSpeed; }
    }
    float gRunSpeed = Guy.DEFAULT_RUN_SPEED;
    public float RunSpeed {
        get { return gRunSpeed; }
    }


    public GeneSequence() {

    }

    public static float RandomMutationFactor(float mutationRange, out bool isHyper) {
    // public static float RandomMutationFactor(float mutationRange) {
        // Generate random number used to offset genes, like a mutation
        float n = Random.Range(-mutationRange,mutationRange);
        isHyper = false;
        if (Mathf.Round(Random.Range(1,Globals.HYPER_MUTATION_POSSIBILITY)) == 1 || Input.GetKey(KeyCode.H)) {
            Debug.Log("HYPER MUTANT");
            isHyper = true;
            n*=Random.Range(1,Globals.HYPER_MUTATION_MULTIPLIER);
        }
        return Random.Range(0,2) == 1 ? n : -n;
    }

    public static GeneSequence GenerateNewSequence() {
        GeneSequence newSequence = new GeneSequence();

        // DEBUG
        // newSequence.gColor = Guy.DEFAULT_COLOR;
        // newSequence.gRunSpeed = Guy.DEFAULT_RUN_SPEED*(1+RandomMutationFactor(Guy.DEFAULT_MUTATION_RANGE));
        // DEBUG

        return newSequence;
    }

    public static Color AverageColours(Color[] cols) {
        Color c = new Color();

        float avgR=0, avgG=0, avgB=0;
        foreach (Color col in cols) {
            avgR += col.r;
            avgG += col.g;
            avgB += col.b;
        }

        avgR /= cols.Length;
        avgG /= cols.Length;
        avgB /= cols.Length;

        c.a = 1;
        c.r = Mathf.Round(avgR*1000)/1000f;
        c.g = Mathf.Round(avgG*1000)/1000f;
        c.b = Mathf.Round(avgB*1000)/1000f;

        return c;
    }

    public static Color AverageColours(Color a, Color b) {
        Color c = new Color();

        // float avgR = (a.r + b.r) / 2;
        // float avgG = (a.g + b.g) / 2;
        // float avgB = (a.b + b.b) / 2;

        // c.a = 1;
        // c.r = Mathf.Round(avgR*1000)/1000f;
        // c.g = Mathf.Round(avgG*1000)/1000f;
        // c.b = Mathf.Round(avgB*1000)/1000f;
        c.a = 1;
        Vector3 aHSV;
        Color.RGBToHSV(a,out aHSV.x,out aHSV.y,out aHSV.z);

        Vector3 bHSV;
        Color.RGBToHSV(b,out bHSV.x,out bHSV.y,out bHSV.z);

        float rAvg = (aHSV.x+bHSV.x)/2;
        float gAvg = (aHSV.y+bHSV.y)/2;
        float bAvg = (aHSV.z+bHSV.z)/2;

        c = Color.HSVToRGB(rAvg,gAvg,bAvg);


        // increase saturation by 20%
        // c = SetSaturation(c,Random.Range(0.8f,1.2f));
        // c = SetHue(c,Random.Range(0.9f,1.1f));

        return c;
    }

    public static Color LerpColour(Color from, Color to, float v) {
        Color c = from;

        c.r = Mathf.Lerp(from.r,to.r,v);
        c.g = Mathf.Lerp(from.g,to.g,v);
        c.b = Mathf.Lerp(from.b,to.b,v);

        return c;
    }

    static Color SetSaturation(Color c, float m) {
        float h,s,v;
        Color.RGBToHSV(c,out h,out s,out v);
        // s = Mathf.Min(1,s*m); 
        s = (s*m)%1;
        c = Color.HSVToRGB(h,s,v);
        return c;
    }

    static Color SetHue(Color c, float m) {
        float h,s,v;
        Color.RGBToHSV(c,out h,out s,out v);
        // h = Mathf.Min(1,h*m);
        h = (h*m)%1;
        c = Color.HSVToRGB(h,s,v);
        return c;
    }

    
    static float GreyscaleFromRGB(Color color) {
        return color.r * Globals.RToGrey + color.g * Globals.GToGrey + color.b * Globals.BToGrey;
    }

    // [Range(0.00f,1.00f)]
    // public float rangeOffset = 0;
    static float colourSimilarity = (1/Guy.DEFAULT_COLOUR_COMPATABILITY_RANGE);
    static bool AreColourGenesCompatible(Color a, Color b, float similarityRange) {
        // bool rCompat = a.r >= b.r-similarityRange && a.r <= b.r+similarityRange;
        // bool gCompat = a.g >= b.g-similarityRange && a.g <= b.g+similarityRange;
        // bool bCompat = a.b >= b.b-similarityRange && a.b <= b.b+similarityRange;

        // new Vector3(a.r,a.g,a.b) * new Vector3(Globals.RToGrey,Globals.GToGrey,Globals.BToGrey);
        // new Vector3(b.r,b.g,b.b) * new Vector3(Globals.RToGrey,Globals.GToGrey,Globals.BToGrey);

        float aG = GreyscaleFromRGB(a);
        float bG = GreyscaleFromRGB(b);

        return aG >= bG - similarityRange && aG <= bG + similarityRange;
    }

    public static bool AreParentsCompatible(GeneSequence a, GeneSequence b, float similarityRange) {
        return AreColourGenesCompatible(a.Color,b.Color,similarityRange);
    }

    public static Color MutateColour(Color col, float m) {

        Color c = col;

        float h,s,v;
        Color.RGBToHSV(c,out h,out s,out v);

        // h += m;
        if ((int)Random.Range(1,Globals.ALBINO_MUTATION_FREQUENCY)==1) {
            Debug.Log("ALBINO");
            s = 0;
        } else {
            h = (h+m)%1;
        }
        // s = (s+1/m)%1;
        c = Color.HSVToRGB(h,s,v);

        // Debug.Log($"MUTATING COLOUR (factor of {m}): {col} -> {c}");

        return c;
    }

    public static GeneSequence GenerateSequenceFromParents(Guy child, GeneSequence mother, GeneSequence father) {
        GeneSequence newSequence = new GeneSequence();

        // DEBUG
        Color newColour = AverageColours(mother.Color,father.Color);
        bool isHyper = false;
        // newColour = LerpColour(newColour,father.Color,0.5f); // Shift towards father's colour
        newSequence.gColor = MutateColour(newColour,RandomMutationFactor(Globals.DEFAULT_MUTATION_RANGE,out isHyper));

        float h,s,v;
        Color.RGBToHSV(newSequence.gColor,out h,out s,out v);
        newSequence.gColor = Color.HSVToRGB(h,s,v);
        // Debug.Log($"{h},{s},{v}");
        if (s == 0) {
            Debug.Log($"ALBINO: {child.transform.name}");
        }
        if (isHyper) {
            Debug.Log($"HYPERMUTANT: {child.transform.name}");
        }
        // newSequence.gWalkSpeed = Guy.DEFAULT_WALK_SPEED*(1+Mathf.Abs(RandomMutationFactor(Globals.DEFAULT_MUTATION_RANGE)));
        // DEBUG

        return newSequence;
    }
}
