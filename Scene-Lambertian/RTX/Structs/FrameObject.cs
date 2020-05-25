﻿using Vortice.Direct3D12;

namespace SceneLambertian.Structs
{
    internal struct FrameObject
    {
        public ID3D12CommandAllocator pCmdAllocator;
        public ID3D12Resource pSwapChainBuffer;
        public CpuDescriptorHandle rtvHandle;
    };
}
