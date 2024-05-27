using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        for (int d = 1; d < 6; d++)
        {
            Debug.Log("--- " + d + "dice ---- (5/7)");
            for (int a = 1; a < 5; a++) Roll(d, a, 5, 7);
        }
    }

    private (float, float, float) Roll(int numDice, int numSuccessesRequired, int limitPartial, int limitSuccess)
    {
        int repetitions = 1000000;
        int success = 0;
        int failure = 0;
        int partial = 0;
        
        for (int i=0; i<repetitions; i++)
        {
            int numThrows = numDice;
            int successes = 0;
            int partials = 0;
            
            for (int j=0; j<numThrows; j++)
            {
                int roll = RollD10;
                if (roll == 10) numThrows++;
                if (roll >= limitSuccess) 
                    successes++;
                if (roll >= limitPartial)
                    partials++;
            }

            if (successes >= numSuccessesRequired)
            {
                success++;
            }
            else if (partials >= numSuccessesRequired)
            {
                partial++;
            }
            else
            {
                failure++;
            }
            
            
            
            
        }
        
       Debug.Log( numDice + " dice for " + numSuccessesRequired + " : " + 100f*(float)failure/(float)repetitions 
       + " " + 100f*(float)partial/(float)repetitions + " " + 100f*(float)success/(float)repetitions);
   
        return (100f*(float)failure/(float)repetitions, 100f*(float)partial/(float)repetitions, 100f*(float)success/(float)repetitions);
    }
    int RollD10 => Random.Range(1, 11);
}
