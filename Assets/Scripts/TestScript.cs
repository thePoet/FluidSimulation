using System;
using System.Collections.Generic;
using System.Transactions;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Timer = RikusGameDevToolbox.GeneralUse.Timer;


// List + Dict with struct  53.2ms 34.8ms = 88.7ms
// List + Dict with class  54.0ms 23.3ms = 77.3ms

// Arrays with class  52.3ms 17.3ms = 69.6ms
// Huom: oli hitaampaa operoida suoraan taulukolla kuin kopioida operoitava partikkeli muuttujaan ja operoida sillä.

// Arrays with struct  20.9ms 18.2ms 39.0ms (operoidaan paikallaan, ei kopioida muuttujaan)

// Spanneillä 21.0ms 16.3 = 37.3ms

public class TestScript : MonoBehaviour
{
    
    struct Particle
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
            Position = new Vector2(Random.Range(0f, 10f), Random.Range(0f, 10f)),
            PreviousPosition = new Vector2(Random.Range(0f, 10f), Random.Range(0f, 10f)),
            a = Random.Range(-0.00001f, 0.00001f)
        };
    }
    
   

    int numParticles = 10000;
    int numNeighbours = 40;
    int numIterations = 100;


    private Particle[] plist;
    private int[][] neighbours;


    Span<Particle> ParticlesSpan()
    {
        return plist;
    }
    
    Span<int> NeighboursSpan(int i)
    {
        return neighbours[i];
    }
    
    void Start()
    {
        
        plist = new Particle[numParticles];
        neighbours = new int[numParticles][];
        for (int i=0; i<numParticles; i++)
        {
            neighbours[i] = new int[numNeighbours];
        }
        
        
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

        
        Debug.Log("Population took " + tPop/numIterations * 1000f + "ms");
        Debug.Log("Operation took " + tOp/numIterations * 1000f + "ms");
        Debug.Log("TOTAL " + (tOp + tPop)/numIterations * 1000f + "ms");



        
    }

    private void Clear()
    {
        //plist.Clear();
        //neighbours.Clear();
    }

    private void Prep()
    {
        
        
    }

    private void Operate()
    {
        var span = ParticlesSpan();
        
        for (int i = 0; i < numParticles; i++)
        {
            //var nSpan = NeighboursSpan(i);
            
            foreach (int n in NeighboursSpan(i))
            {
                //DoTheThing(i, j);
             //   int n = nSpan[j];
               span[i].Position += span[n].Position * span[n].a;
               span[n].Position += span[i].Position * span[i].a;
               
               //neighbours[i,j].Position += plist[i].Position * plist[i].a;
                /*
                Particle a = plist[i];
                Particle b = neighbours[i,j];
                a.Position += b.Position * b.a;
                b.Position += a.Position * a.a;
                plist[i] = a;
                neighbours[i,j] = b;*/
            }
        }
    }
/*
    private void DoTheThing(int i, int j)
    {
        int nIndex = neighbours[i, j];
        plist[i].Position += plist[nIndex].Position * plist[nIndex].a;
        plist[nIndex].Position += plist[i].Position * plist[i].a;
    }*/

    private void Populate()
    {
        var span = ParticlesSpan();

        for (int i = 0; i < numParticles; i++)
        {
            span[i] = RandomParticle();
        }


        for (int i = 0; i < numParticles; i++)
        {
            var nSpan = NeighboursSpan(i);

            //neighbours.A(i, new List<Particle>());
            
            for (int j = 0; j < numNeighbours; j++)
            {
                int nIdx = Random.Range(0, numParticles - 1);
                //neighbours[i][j] = nIdx;
                nSpan[j] = nIdx;
            }
        }
    }
}
