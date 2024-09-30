using UnityEngine;

namespace FluidSimulation.Internal
{
    public class ShaderBuffer
    {
        public enum Type
        {
            IO, Internal
        }

        public string Name;
        public ComputeBuffer ComputeBuffer;

        public ShaderBuffer(string name, int count, int stride, Type bufferType)
        {
            Name = name;
            if (bufferType == Type.Internal)
            {
                ComputeBuffer = new ComputeBuffer(count, stride);
            }

            if (bufferType == Type.IO)
            {
                ComputeBuffer = new ComputeBuffer(count, stride,
                    ComputeBufferType.Default, ComputeBufferMode.Dynamic);
            }
        }
        
    }
    
    

}