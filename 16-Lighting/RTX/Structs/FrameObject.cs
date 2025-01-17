﻿using Vortice.Direct3D12;

namespace RayTracingTutorial16.Structs
{
    internal struct FrameObject
    {
        public ID3D12CommandAllocator pCmdAllocator;
        public ID3D12Resource pSwapChainBuffer;
        public CpuDescriptorHandle rtvHandle;
    };
}
