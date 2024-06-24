

public struct Fluid
{
    public int IsSolid;
    public float Stiffness;
    public float NearStiffness;
    public float RestDensity;
    public float ViscositySigma;
    public float ViscosityBeta;
    public float GravityScale;
            
    public static int Stride => sizeof(int) + 6 * sizeof(float);
}
