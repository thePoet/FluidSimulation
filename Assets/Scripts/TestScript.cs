using System.Collections.Generic;
using System.Transactions;
using Unity.VisualScripting;
using UnityEngine;
using Timer = RikusGameDevToolbox.GeneralUse.Timer;


// List + Dict with struct  53.2ms 34.8ms = 88.7ms
// List + Dict with class  54.0ms 23.3ms = 77.3ms

public class TestScript : MonoBehaviour
{
    
    class Particle
    {
        public int Id;
        public Vector2 Position;
        public Vector2 PreviousPosition;
        public float a;
    }

    private int freeId = -1;
    Particle RandomParticle()
    {
        freeId++;

        return new Particle()
        {
            Id = freeId,
            Position = new Vector2(Random.Range(0, 10), Random.Range(0, 10)),
            PreviousPosition = new Vector2(Random.Range(0, 10), Random.Range(0, 10)),
            a = Random.Range(-10f, 10f)
        };
    }
    
    List<Particle> plist = new List<Particle>();
    Dictionary<int, List<Particle>> neighbours = new Dictionary<int, List<Particle>>();

    int numParticles = 10000;
    int numNeighbours = 40;
    int numIterations = 100;
    void Start()
    {
        float tPop = 0f;
        float tOp = 0f;

        Timer timer = new Timer();

        for (int i=0; i<numIterations; i++)
        {
            Prep();
            timer.Reset();
            Populate();
            tPop+=timer.Time;
            timer.Reset();
            Operate();
            tOp+=timer.Time;

            Clear();
        }

        
        Debug.Log("Operation took " + tPop/numIterations * 1000f + "ms");
        Debug.Log("Population took " + tOp/numIterations * 1000f + "ms");
        Debug.Log("TOTAL " + (tOp + tPop)/numIterations * 1000f + "ms");
    }

    private void Clear()
    {
        plist.Clear();
        neighbours.Clear();
    }

    private void Prep()
    {
        
        
    }

    private void Operate()
    {
        for (int i = 0; i < numParticles; i++)
        {
            for (int j=0; j< numNeighbours; j++)
            {
                Particle a = plist[i];
                Particle b = neighbours[i][j];
                a.Position += b.Position * b.a;
                b.Position += a.Position * a.a;
               // plist[i] = a;
                //neighbours[i][j] = b;
            }
        }
    }

    private void Populate()
    {
      

        for (int i = 0; i < numParticles; i++)
        {
            plist.Add(RandomParticle());
        }

        for (int i = 0; i < numParticles; i++)
        {
            neighbours.Add(i, new List<Particle>());
            for (int j = 0; j < numNeighbours; j++)
            {
                int nIdx = Random.Range(0, numParticles - 1);
                neighbours[i].Add(plist[nIdx]);
            }
        }
    }
}
