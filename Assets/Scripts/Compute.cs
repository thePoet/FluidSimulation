using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Compute : MonoBehaviour
{

    struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
    }
    
    public Transform CubePrefab;
    public ComputeShader CubeShader;

    private ComputeBuffer _particlesBuffer;

    // Grid size
    public int CubesPerAxis = 80;
    

    // Cube objects
    private Transform[] _cubes;

    // Array containing all z positions of cubes.
    // Will be put on the compute buffer
   // private Vector2[] _cubesPositions;

    private Particle[] _particles;
    
    private void Awake() 
    {
        _particlesBuffer = new ComputeBuffer(CubesPerAxis * CubesPerAxis, 4*sizeof(float));
    }
    private void Start() 
    {
        CreateGrid();
    }

    private void OnDestroy() 
    {
        _particlesBuffer.Release();
    }
    
    void CreateGrid() 
    {
        _cubes = new Transform[CubesPerAxis * CubesPerAxis];
        _particles = new Particle[CubesPerAxis * CubesPerAxis];
        for (int x = 0, i = 0; x < CubesPerAxis; x++) 
        {
            for (int y = 0; y < CubesPerAxis; y++, i++) 
            {
                _cubes[i] = Instantiate(CubePrefab, transform);
                _cubes[i].transform.position = new Vector3(x, y, 0);
            }
        }
        
        StartCoroutine(UpdateCubeGrid());
    }
    
    IEnumerator UpdateCubeGrid() {
        while (true) 
        {
         
            UpdatePositionsGPU();
        

            for (int i = 0; i < _cubes.Length; i++) 
            {
                Vector2 position = _particles[i].Position;
                _cubes[i].localPosition = position;
                //new Vector3(_cubesPositions[i].x, 
                  //  _cubesPositions[i].y,
                   // 0f );
            }
            yield return new WaitForSeconds(1);
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
    }
}