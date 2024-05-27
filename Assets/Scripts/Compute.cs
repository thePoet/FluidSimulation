
using UnityEngine;
using Random = UnityEngine.Random;

public class Compute : MonoBehaviour
{
    struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public byte IsAlive;
    }

    struct TestData
    {
        public Vector2 something;
        public int[] somethingElse;
    }
    
    private int _bytesPerParticle = 5*sizeof(float);
    
    public Transform CubePrefab;
    public ComputeShader CubeShader;

    private ComputeBuffer _particlesBuffer;

    // Grid size
    public int CubesPerAxis = 80;
    
    private Transform[] _sprites;
    private Particle[] _particles;
    
    private void Awake() 
    {
        _particlesBuffer = new ComputeBuffer(CubesPerAxis * CubesPerAxis, _bytesPerParticle);
        _particles = CreateParticles();
        _particlesBuffer.SetData(_particles);
        CreateSprites();
    }


    private Particle[] CreateParticles()
    {
        Particle[] particles = new Particle[CubesPerAxis * CubesPerAxis];
        for (int i=0; i<particles.Length; i++)
        {
            particles[i] = new Particle
            {
                Position = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f)),
                Velocity = Random.insideUnitCircle * 10f,
                IsAlive = CoinToss()
            };
        }

        return particles;
        
        
        byte CoinToss() => (byte)Random.Range(0, 2);
    }

    private void OnDestroy() 
    {
        _particlesBuffer.Release();
    }

    private void Update()
    {
        UpdatePositionsGPU();
        MoveSprites();

    }

    void CreateSprites() 
    {
        _sprites = new Transform[CubesPerAxis * CubesPerAxis];
       
        for (int x = 0, i = 0; x < CubesPerAxis; x++) 
        {
            for (int y = 0; y < CubesPerAxis; y++, i++) 
            {
                _sprites[i] = Instantiate(CubePrefab, transform);
                _sprites[i].transform.position = new Vector3(x, y, 0);
            }
        }
    }
    

    private void MoveSprites()
    {
        for (int i = 0; i < _sprites.Length; i++) 
        {
            _sprites[i].position = _particles[i].Position;
        }
    }

    void UpdatePositionsGPU() 
    {
        CubeShader.SetBuffer(0, "_Particles", _particlesBuffer);

        CubeShader.SetInt("_CubesPerAxis", CubesPerAxis);
        CubeShader.SetFloat("_Time", Time.deltaTime);
        
        int workgroups = Mathf.CeilToInt(CubesPerAxis / 8.0f);
        CubeShader.Dispatch(0, workgroups, workgroups, 1);
        _particlesBuffer.GetData(_particles);
        
        CubeShader.Dispatch(1, workgroups, workgroups, 1);
    }
    

}