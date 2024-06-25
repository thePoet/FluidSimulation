

public struct Fluid
{
    public int StateIndex;
    public float Stiffness;
    public float NearStiffness;
    public float RestDensity;
    public float ViscositySigma;
    public float ViscosityBeta;
    public float GravityScale;
            
    public static int Stride => sizeof(int) + 6 * sizeof(float);
    
 
    public State State
    {
        get => (State) StateIndex;
        set => StateIndex = (int) value;
    }
}

public enum State
{
    Liquid,
    Gas,
    Solid,
}
