﻿using RayTracingTutorial11.RTX;
using RayTracingTutorial11.Structs;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace RayTracingTutorial11
{
    public class Scene
    {
        private const int D3D12DefaultShader4ComponentMapping = 5768;
        private const int kRtvHeapSize = 3;
        private Color4 clearColor = new Color4(0.4f, 0.6f, 0.2f, 1.0f);

        private readonly Window Window;
        private D3D12GraphicsContext context;
        private IntPtr mHwnd;
        private ID3D12Device5 mpDevice;
        private ID3D12CommandQueue mpCmdQueue;
        private IDXGISwapChain3 mpSwapChain;
        private ID3D12GraphicsCommandList4 mpCmdList;
        private HeapData mRtvHeap;
        private FrameObject[] mFrameObjects;
        private ID3D12Fence mpFence;
        private uint mFenceValue = 0;
        private EventWaitHandle mFenceEvent;
        private Rect mSwapChainRect;
        private ID3D12Resource mpTopLevelAS;
        private ID3D12Resource[] mpBottomLevelAS;
        private ID3D12StateObject mpPipelineState;
        private ID3D12RootSignature mpEmptyRootSig;
        private AccelerationStructures acs;
        private ID3D12Resource mpOutputResource;
        private ID3D12DescriptorHeap mpSrvUavHeap;
        private ID3D12Resource mpShaderTable;
        private uint mShaderTableEntrySize;
        private ID3D12Resource[] mpContantBuffer;

        public Scene(Window window)
        {
            this.Window = window;
            this.context = new D3D12GraphicsContext(window.Width, window.Height);

            // InitDXR Tutorial 02
            this.InitDXR((IntPtr)window.Handle, window.Width, window.Height);

            // Acceleration Structures Tutorial 03
            this.CreateAccelerationStructures();

            // RtPipeline Tutorial 04
            this.CreateRtPipelineState();

            // ShaderResources Tutorial 06. Need to do this before initializing the shader-table
            this.CreateShaderResources();

            // ConstantBuffer Tutorial 09. Yes, we need to do it before creating the shader-table
            this.CreateConstantBuffer();

            // ShaderTable Tutorial 05
            this.CreateShaderTable();
        }

        private void InitDXR(IntPtr winHandle, int winWidth, int winHeight)
        {
            mHwnd = winHandle;
            this.mSwapChainRect = new Rect(0, 0, winWidth, winHeight);

            // Initialize the debug layer for debug builds
#if DEBUG
            if (D3D12.D3D12GetDebugInterface<ID3D12Debug>(out var pDx12Debug).Success)
            {
                pDx12Debug.EnableDebugLayer();
            }
#endif
            // Create the DXGI factory
            IDXGIFactory4 pDXGIFactory;
            DXGI.CreateDXGIFactory1<IDXGIFactory4>(out pDXGIFactory);
            mpDevice = this.context.CreateDevice(pDXGIFactory);
            mpCmdQueue = this.context.CreateCommandQueue(mpDevice);
            mpSwapChain = this.context.CreateDXGISwapChain(pDXGIFactory, mHwnd, winWidth, winHeight, Format.R8G8B8A8_UNorm, mpCmdQueue);

            // Create a RTV descriptor heap
            mRtvHeap.Heap = this.context.CreateDescriptorHeap(mpDevice, kRtvHeapSize, DescriptorHeapType.RenderTargetView, false);

            // Create the per-frame objects
            this.mFrameObjects = new FrameObject[this.context.kDefaultSwapChainBuffers];
            for (int i = 0; i < this.context.kDefaultSwapChainBuffers; i++)
            {
                mFrameObjects[i].pCmdAllocator = mpDevice.CreateCommandAllocator(CommandListType.Direct);
                mFrameObjects[i].pSwapChainBuffer = mpSwapChain.GetBuffer<ID3D12Resource>(i);
                mFrameObjects[i].rtvHandle = context.CreateRTV(mpDevice, mFrameObjects[i].pSwapChainBuffer, mRtvHeap.Heap, ref mRtvHeap.usedEntries, Format.R8G8B8A8_UNorm_SRgb);
            }

            // Create the command-list
            var cmdList = mpDevice.CreateCommandList(0, CommandListType.Direct, mFrameObjects[0].pCmdAllocator, null);
            this.mpCmdList = cmdList.QueryInterface<ID3D12GraphicsCommandList4>();

            // Create a fence and the event
            this.mpFence = mpDevice.CreateFence(0, FenceFlags.None);
            this.mFenceEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public void CreateAccelerationStructures()
        {
            acs = new AccelerationStructures();

            long mTlasSize = 0;

            ID3D12Resource[] mpVertexBuffer = new ID3D12Resource[2];
            mpVertexBuffer[0] = acs.CreateTriangleVB(mpDevice);
            mpVertexBuffer[1] = acs.CreatePlaneVB(mpDevice);

            // The first bottom-level buffer is for the plane and the triangle
            int[] vertexCount = new int[] { 3, 6 }; // Triangle has 3 vertices, plane has 6
            AccelerationStructureBuffers[] bottomLevelBuffers = new AccelerationStructureBuffers[2];
            bottomLevelBuffers[0] = acs.CreateBottomLevelAS(mpDevice, mpCmdList, mpVertexBuffer, vertexCount, 2);
            mpBottomLevelAS = new ID3D12Resource[2];
            mpBottomLevelAS[0] = bottomLevelBuffers[0].pResult;

            // The second bottom-level buffer is for the triangle only
            bottomLevelBuffers[1] = acs.CreateBottomLevelAS(mpDevice, mpCmdList, mpVertexBuffer, vertexCount, 1);
            mpBottomLevelAS[1] = bottomLevelBuffers[1].pResult;

            // Create the TLAS
            AccelerationStructureBuffers topLevelBuffers = acs.CreateTopLevelAS(mpDevice, mpCmdList, mpBottomLevelAS, ref mTlasSize);

            // The tutorial doesn't have any resource lifetime management, so we flush and sync here. This is not required by the DXR spec - you can submit the list whenever you like as long as you take care of the resources lifetime.
            mFenceValue = context.SubmitCommandList(mpCmdList, mpCmdQueue, mpFence, mFenceValue);
            mpFence.SetEventOnCompletion(mFenceValue, mFenceEvent);
            mFenceEvent.WaitOne();
            int bufferIndex = mpSwapChain.GetCurrentBackBufferIndex();
            mpCmdList.Reset(mFrameObjects[0].pCmdAllocator, null);

            // Store the AS buffers. The rest of the buffers will be released once we exit the function
            mpTopLevelAS = topLevelBuffers.pResult;
        }

        public void CreateRtPipelineState()
        {
            var rtpipeline = new RTPipeline();

            // Need 10 subobjects:
            //  1 for the DXIL library
            //  1 for hit-group
            //  2 for RayGen root-signature (root-signature and the subobject association)
            //  2 for hit-program root-signature (root-signature and the subobject association)
            //  2 for miss-shader root-signature (signature and association)
            //  2 for shader config (shared between all programs. 1 for the config, 1 for association)
            //  1 for pipeline config
            //  1 for the global root signature
            StateSubObject[] subobjects = new StateSubObject[12];
            int index = 0;

            // Create the DXIL library
            DxilLibrary dxilLib = rtpipeline.CreateDxilLibrary();
            subobjects[index++] = dxilLib.stateSubObject; // 0 Library

            HitProgram hitProgram = new HitProgram(null, RTPipeline.kClosestHitShader, RTPipeline.kHitGroup);
            subobjects[index++] = hitProgram.subObject; // 1 Hit Group

            // Create the ray-gen root-signature and association
            Structs.LocalRootSignature rgsRootSignature = new Structs.LocalRootSignature(mpDevice, rtpipeline.CreateRayGenRootDesc());
            subobjects[index] = rgsRootSignature.subobject; // 2 RayGen Root Sig

            int rgsRootIndex = index++; // 2
            ExportAssociation rgsRootAssociation = new ExportAssociation(new string[] { RTPipeline.kRayGenShader }, subobjects[rgsRootIndex]);
            subobjects[index++] = rgsRootAssociation.subobject; // 3 Associate Root Sig to RGS

            // Create the hit root-signature and association
            Structs.LocalRootSignature hitRootSignature = new Structs.LocalRootSignature(mpDevice, rtpipeline.CreateHitRootDesc());
            subobjects[index] = hitRootSignature.subobject; // 4 Hit Root Sig

            int hitRootIndex = index++; // 4
            ExportAssociation hitRootAssociation = new ExportAssociation(new string[] { RTPipeline.kClosestHitShader }, subobjects[hitRootIndex]); // 5 Associate Hit Root Sig to Hit Group
            subobjects[index++] = hitRootAssociation.subobject; // 6 Associate Hit Root Sig to Hit Group

            // Create the miss root-signature and association
            RootSignatureDescription emptyDesc = new RootSignatureDescription(RootSignatureFlags.LocalRootSignature);
            Structs.LocalRootSignature missRootSignature = new Structs.LocalRootSignature(mpDevice, emptyDesc);
            subobjects[index] = missRootSignature.subobject; // 6 Miss Root Sig

            int missRootIndex = index++;  // 6
            ExportAssociation missRootAssociation = new ExportAssociation(new string[] { RTPipeline.kMissShader }, subobjects[missRootIndex]);
            subobjects[index++] = missRootAssociation.subobject; // 7 Associate Miss Root Sig to Miss Shader

            // Bind the payload size to the programs
            ShaderConfig shaderConfig = new ShaderConfig(sizeof(float) * 2, sizeof(float) * 3);
            subobjects[index] = shaderConfig.subObject; // 8 Shader Config;

            int shaderConfigIndex = index++; // 8
            string[] shaderExports = new string[] { RTPipeline.kMissShader, RTPipeline.kClosestHitShader, RTPipeline.kRayGenShader };
            ExportAssociation configAssociation = new ExportAssociation(shaderExports, subobjects[shaderConfigIndex]);
            subobjects[index++] = configAssociation.subobject;  // 9 Associate Shader Config to Miss, CHS, RGS

            // Create the pipeline config
            PipelineConfig config = new PipelineConfig(1);
            subobjects[index++] = config.suboject; // 10

            // Create the global root signature and store the empty signature
            Structs.GlobalRootSignature root = new Structs.GlobalRootSignature(mpDevice, new RootSignatureDescription());
            mpEmptyRootSig = root.pRootSig.RootSignature;
            subobjects[index++] = root.suboject; // 11

            // Create the state
            StateObjectDescription desc = new StateObjectDescription(StateObjectType.RaytracingPipeline, subobjects);

            mpPipelineState = mpDevice.CreateStateObject(desc);
        }

        private const uint D3D12ShaderIdentifierSizeInBytes = 32;
        private const uint D3D12RaytracingShaderRecordByteAlignment = 32;

        private static uint align_to(uint _alignment, uint _val)
        {
            return (((_val + _alignment - 1) / _alignment) * _alignment);
        }

        public unsafe void CreateShaderTable()
        {
            /** The shader-table layout is as follows:
                Entry 0 - Ray-gen program
                Entry 1 - Miss program
                Entry 2 - Hit program for triangle 0 and plane
                Entry 3 - Hit program for triangle 1
                Entry 4 - Hit program for triangle 2
                All entries in the shader-table must have the same size, so we will choose it base on the largest required entry.
                The ray-gen program requires the largest entry - sizeof(program identifier) + 8 bytes for a descriptor-table.
                The entry size must be aligned up to D3D12_RAYTRACING_SHADER_RECORD_BYTE_ALIGNMENT
            */

            // Calculate the size and create the buffer          
            mShaderTableEntrySize = D3D12ShaderIdentifierSizeInBytes;
            mShaderTableEntrySize += 8; // the ray-gen's descriptor table
            mShaderTableEntrySize = align_to(D3D12RaytracingShaderRecordByteAlignment, mShaderTableEntrySize);
            uint shaderTableSize = mShaderTableEntrySize * 5;

            // For simplicity, we create the shader.table on the upload heap. You can also create it on the default heap
            mpShaderTable = this.acs.CreateBuffer(mpDevice, shaderTableSize, ResourceFlags.None, ResourceStates.GenericRead, AccelerationStructures.kUploadHeapProps);

            // Map the buffer
            IntPtr pData;
            pData = mpShaderTable.Map(0, null);

            ID3D12StateObjectProperties pRtsoProps;
            pRtsoProps = mpPipelineState.QueryInterface<ID3D12StateObjectProperties>();

            // Entry 0 - ray-gen program ID and descriptor data
            Unsafe.CopyBlock((void*)pData, (void*)pRtsoProps.GetShaderIdentifier(RTPipeline.kRayGenShader), D3D12ShaderIdentifierSizeInBytes);
            ulong heapStart = (ulong)mpSrvUavHeap.GetGPUDescriptorHandleForHeapStart().Ptr;
            Unsafe.Write<ulong>((pData + (int)D3D12ShaderIdentifierSizeInBytes).ToPointer(), heapStart);

            // This is where we need to set the descriptor data for the ray-gen shader. We'll get to it in the next tutorial

            // Entry 1 - miss program
            pData += (int)mShaderTableEntrySize; // +1 skips ray-gen
            Unsafe.CopyBlock((void*)pData, (void*)pRtsoProps.GetShaderIdentifier(RTPipeline.kMissShader), D3D12ShaderIdentifierSizeInBytes);

            // Entry 2-4 - The triangles' hit program, ProgramID and constant-buffer data
            for (int i = 0; i < 3; i++)
            {
                pData += (int)mShaderTableEntrySize;
                Unsafe.CopyBlock((void*)pData, (void*)pRtsoProps.GetShaderIdentifier(RTPipeline.kHitGroup), D3D12ShaderIdentifierSizeInBytes);
                void* pCbDesc = (pData + (int)D3D12ShaderIdentifierSizeInBytes).ToPointer(); // The location of the root-descriptor
                Unsafe.Write<ulong>(pCbDesc, (ulong)mpContantBuffer[i].GPUVirtualAddress);
            }

            // Unmap
            mpShaderTable.Unmap(0, null);
        }

        public void CreateShaderResources()
        {
            // Create the output resource. The dimensions and format should match the swap-chain
            ResourceDescription resDesc = new ResourceDescription();
            resDesc.DepthOrArraySize = 1;
            resDesc.Dimension = ResourceDimension.Texture2D;
            resDesc.Format = Format.R8G8B8A8_UNorm; // The backbuffer is actually DXGI_FORMAT_R8G8B8A8_UNORM_SRGB, but sRGB formats can't be used with UAVs. We will convert to sRGB ourselves in the shader
            resDesc.Flags = ResourceFlags.AllowUnorderedAccess;
            resDesc.Height = mSwapChainRect.Height;
            resDesc.Layout = TextureLayout.Unknown;
            resDesc.MipLevels = 1;
            resDesc.SampleDescription = new SampleDescription(1, 0);
            resDesc.Width = mSwapChainRect.Width;
            mpOutputResource = mpDevice.CreateCommittedResource(AccelerationStructures.kDefaultHeapProps, HeapFlags.None, resDesc, ResourceStates.CopySource, null);  // Starting as copy-source to simplify onFrameRender()

            // Create an SRV/UAV descriptor heap. Need 2 entries - 1 SRV for the scene and 1 UAV for the output
            mpSrvUavHeap = this.context.CreateDescriptorHeap(mpDevice, 2, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, true);

            // Create the UAV. Based on the root signature we created it should be the first entry
            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription();
            uavDesc.ViewDimension = UnorderedAccessViewDimension.Texture2D;
            mpDevice.CreateUnorderedAccessView(mpOutputResource, null, uavDesc, mpSrvUavHeap.GetCPUDescriptorHandleForHeapStart());

            // Create the TLAS SRV right after the UAV. Note that we are using a different SRV desc here
            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription();
            srvDesc.ViewDimension = ShaderResourceViewDimension.RaytracingAccelerationStructure;
            srvDesc.Shader4ComponentMapping = D3D12DefaultShader4ComponentMapping;
            srvDesc.RaytracingAccelerationStructure = new RaytracingAccelerationStructureShaderResourceView();
            srvDesc.RaytracingAccelerationStructure.Location = mpTopLevelAS.GPUVirtualAddress;
            CpuDescriptorHandle srvHandle = mpSrvUavHeap.GetCPUDescriptorHandleForHeapStart();
            srvHandle.Ptr += mpDevice.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            mpDevice.CreateShaderResourceView(null, srvDesc, srvHandle);
        }

        public unsafe void CreateConstantBuffer()
        {
            // The shader declares the CB with 9 float3. However, due to HLSL packing rules, we create the CB with 9 float4 (each float3 needs to start on a 16-byte boundary)
            Vector4[] bufferData = new Vector4[]
            {
                // Instance 0
                new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(1.0f, 0.0f, 1.0f, 1.0f),

                // Instance 1
                new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                
                // Instance 2
                new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
            };

            mpContantBuffer = new ID3D12Resource[3];
            for (int i = 0; i < 3; i++)
            {
                uint bufferSize = (uint)Unsafe.SizeOf<Vector4>() * 3;
                mpContantBuffer[i] = this.acs.CreateBuffer(mpDevice, bufferSize, ResourceFlags.None, ResourceStates.GenericRead, AccelerationStructures.kUploadHeapProps);
                IntPtr pData;
                pData = mpContantBuffer[i].Map(0, null);                
                fixed (void* pSource = &bufferData[i * 3])
                {
                    Unsafe.CopyBlock((void*)pData, pSource, bufferSize);
                }
                mpContantBuffer[i].Unmap(0, null);
            }            
        }

        private int BeginFrame()
        {
            // Bind the descriptor heaps
            ID3D12DescriptorHeap[] heaps = new ID3D12DescriptorHeap[] { mpSrvUavHeap };
            mpCmdList.SetDescriptorHeaps(1, heaps);

            return this.mpSwapChain.GetCurrentBackBufferIndex();
        }

        public bool DrawFrame(Action<int, int> draw, [CallerMemberName] string frameName = null)
        {
            int rtvIndex = BeginFrame();

            // Let's raytrace
            context.ResourceBarrier(mpCmdList, mpOutputResource, ResourceStates.CopySource, ResourceStates.UnorderedAccess);
            DispatchRaysDescription raytraceDesc = new DispatchRaysDescription();
            raytraceDesc.Width = mSwapChainRect.Width;
            raytraceDesc.Height = mSwapChainRect.Height;
            raytraceDesc.Depth = 1;

            // RayGen is the first entry in the shader-table
            raytraceDesc.RayGenerationShaderRecord.StartAddress = mpShaderTable.GPUVirtualAddress;
            raytraceDesc.RayGenerationShaderRecord.SizeInBytes = mShaderTableEntrySize;

            // Miss is the second entry in the shader-table
            uint missOffset = 1 * mShaderTableEntrySize;
            raytraceDesc.MissShaderTable.StartAddress = mpShaderTable.GPUVirtualAddress + missOffset;
            raytraceDesc.MissShaderTable.StrideInBytes = mShaderTableEntrySize;
            raytraceDesc.MissShaderTable.SizeInBytes = mShaderTableEntrySize; // Only a s single miss-entry 

            // Hit is the third entry in the shader-table
            uint hitOffset = 2 * mShaderTableEntrySize;
            raytraceDesc.HitGroupTable.StartAddress = mpShaderTable.GPUVirtualAddress + hitOffset;
            raytraceDesc.HitGroupTable.StrideInBytes = mShaderTableEntrySize;
            raytraceDesc.HitGroupTable.SizeInBytes = mShaderTableEntrySize * 3;

            // Bind the empty root signature
            mpCmdList.SetComputeRootSignature(mpEmptyRootSig);

            // Dispatch
            mpCmdList.SetPipelineState1(mpPipelineState);
            mpCmdList.DispatchRays(raytraceDesc);

            // Copy the results to the back-buffer
            context.ResourceBarrier(mpCmdList, mpOutputResource, ResourceStates.UnorderedAccess, ResourceStates.CopySource);
            context.ResourceBarrier(mpCmdList, mFrameObjects[rtvIndex].pSwapChainBuffer, ResourceStates.Present, ResourceStates.CopyDestination);
            mpCmdList.CopyResource(mFrameObjects[rtvIndex].pSwapChainBuffer, mpOutputResource);

            EndFrame(rtvIndex);

            return true;
        }

        private void EndFrame(int rtvIndex)
        {
            context.ResourceBarrier(mpCmdList, mFrameObjects[rtvIndex].pSwapChainBuffer, ResourceStates.CopyDestination, ResourceStates.Present);
            mFenceValue = context.SubmitCommandList(mpCmdList, mpCmdQueue, mpFence, mFenceValue);
            mpSwapChain.Present(0, 0);

            // Prepare the command list for the next frame
            int bufferIndex = mpSwapChain.GetCurrentBackBufferIndex();

            // Make sure we have the new back-buffer is ready
            if (mFenceValue > context.kDefaultSwapChainBuffers)
            {
                mpFence.SetEventOnCompletion(mFenceValue - context.kDefaultSwapChainBuffers + 1, mFenceEvent);
                this.mFenceEvent.WaitOne();
            }

            mFrameObjects[bufferIndex].pCmdAllocator.Reset();
            mpCmdList.Reset(mFrameObjects[bufferIndex].pCmdAllocator, null);
        }

        public void Dispose()
        {
            mFenceValue++;
            mpCmdQueue.Signal(mpFence, mFenceValue);
            mpFence.SetEventOnCompletion(mFenceValue, mFenceEvent);
            mFenceEvent.WaitOne();
        }
    }
}
