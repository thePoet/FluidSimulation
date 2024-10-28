using UnityEngine;

namespace FluidDemo
{
    [RequireComponent(typeof(LineRenderer))]
    public class LevelOutline : MonoBehaviour
    {
        public Rect Bounds;
        
        private void OnValidate()
        {
            UpdateBoundaryLine();
        }

        private void UpdateBoundaryLine()
        {
            LineRenderer lr = GetComponent<LineRenderer>();

            lr.positionCount = 4;
            lr.SetPosition(0, new Vector3(Bounds.xMin, Bounds.yMin, 0));
            lr.SetPosition(1, new Vector3(Bounds.xMax, Bounds.yMin, 0));
            lr.SetPosition(2, new Vector3(Bounds.xMax, Bounds.yMax, 0));
            lr.SetPosition(3, new Vector3(Bounds.xMin, Bounds.yMax, 0));
        }
    }

}
