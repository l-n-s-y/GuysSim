using UnityEngine;

public class LandscapeGenerator : MonoBehaviour
{

    public Vector2 size;

    public Sprite[] topSprites;
    public Sprite[] middleSprites;
    public Sprite[] bottomSprites;

    public Sprite[][] landTiles;

    Vector2 spriteSize;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        landTiles = new Sprite[][] { topSprites,middleSprites,bottomSprites };
        spriteSize = topSprites[0].rect.size;

        int xIdx=0;
        int yIdx=0;

        GameObject template = new GameObject();
        GameObject curr;
        Vector3 offset;
        template.AddComponent<SpriteRenderer>();
        for (int i=0; i<size.y; i++) {
            for (int j=0; j<size.x; j++) {
                offset = new Vector3(j,i,transform.position.z);
                curr = Instantiate(template, transform.position, Quaternion.identity, transform);
                curr.layer = LayerMask.NameToLayer("Ignore Raycast");
                curr.transform.name = $"land_tile_{j}_{i}";
                offset.x -= size.x/2;
                offset.y -= size.y/2;
                curr.transform.localPosition += offset;


                if (i==0) { // top
                    yIdx = 0;
                } else if (i==size.y-1) { // bottom
                    yIdx = landTiles.Length-1;
                } else { // middle
                    yIdx = landTiles.Length-2;
                }

                if (j==0) { // left
                    xIdx = 0;
                } else if (j==size.x-1) { // right
                    xIdx = landTiles[0].Length-1;
                } else {
                    xIdx = landTiles[0].Length-2;
                }


                curr.GetComponent<SpriteRenderer>().sprite = landTiles[^(yIdx+1)][xIdx];
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
