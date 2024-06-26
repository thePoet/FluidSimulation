

public struct Fluid
{
    public int StateIndex;
    public float Stiffness;
    public float NearStiffness;
    public float RestDensity;
    public float ViscositySigma;
    public float ViscosityBeta;
    public float GravityScale;
    public float Mass;
            
    public static int Stride => sizeof(int) + 7 * sizeof(float);
    
 
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
