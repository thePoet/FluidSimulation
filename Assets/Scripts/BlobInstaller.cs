using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace FluidSimulation
{
    public class BlobInstaller : MonoInstaller
    {
        public GameObject blobPrefab;

        public override void InstallBindings()
        {
            //TODO: pakko olla parempi tapa
            Container.Bind<System.Type>().FromInstance(typeof(BlobPhysicsCustom)).WhenInjectedInto<Blob>();
            Container.BindFactory<StateOfMatter, Blob, BlobFactory>().FromComponentInNewPrefab(blobPrefab);
        }
    }
    
    
    public class BlobFactory : PlaceholderFactory<StateOfMatter, Blob>{}
}