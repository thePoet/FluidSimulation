using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boundaries : MonoBehaviour
{
    public Rect measure;
    public float brickDistance;
    public GameObject brickPrefab;
    
    void Start()
    {
       CreateWall(measure.min, measure.min + Vector2.up * measure.height);
       CreateWall(measure.min, measure.min + Vector2.right * measure.width);
       CreateWall(measure.max, measure.min + Vector2.up * measure.height);
       CreateWall(measure.max, measure.min+ Vector2.right * measure.width);
    }

    void CreateWall(Vector2 start, Vector2 end)
    {
        for (int i = 0; i <= (start - end).magnitude / brickDistance; i++)
        {
            Vector2 position = start + (end - start).normalized * i * brickDistance;
            Instantiate(brickPrefab, position, Quaternion.identity, transform);
        }
    }
}
