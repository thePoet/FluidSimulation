using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FluidSimulation
{


    public interface ISpatialPartitioning2D<T>
    {
        void AddEntity(T entity, Vector2 position);
        void RemoveEntity(T entity, Vector2 position);
        void UpdateEntity(T entity, Vector2 oldPosition, Vector2 newPosition);
        List<T> GetEntitiesInsideCircle(Vector2 center, float radius);
        void RemoveAllEntities();
    }
}

    

