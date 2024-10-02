using UnityEngine;


namespace FluidDemo
{
    public class TestWall : MonoBehaviour
    {
        public float spacing = 1f;
        public int layersX = 5;
        public int layersY = 5;

        void Start()
        {
            FluidSimDemo fluidSim = GameObject.FindObjectOfType<FluidSimDemo>();
            if (fluidSim == null)
            {
                Debug.LogError("No FluidSimDemo found");
                return;
            }

            float hSpacing = Mathf.Sqrt(3f) * spacing;
            float vSpacing = (3f / 2f) * spacing;

            for (int y = 0; y < layersY; y++)
            {
                for (int x = 0; x < layersX; x++)
                {
                    Vector2 localPos = new Vector2(x * hSpacing, y * vSpacing);
                    if (y % 2 == 1) localPos += new Vector2(hSpacing / 2f, 0f);

                    Vector3 worldPos = transform.TransformPoint(localPos);
                    fluidSim.SpawnParticle(worldPos, Vector2.zero, FluidId.Rock);
                }
            }
        }
    }
}
