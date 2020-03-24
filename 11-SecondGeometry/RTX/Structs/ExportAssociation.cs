﻿using Vortice.Direct3D12;

namespace RayTracingTutorial11.Structs
{
    public struct ExportAssociation
    {
        public StateSubObject subobject;
        private SubObjectToExportsAssociation association;

        public ExportAssociation(string[] exportNames, StateSubObject pSubobjectToAssociate)
        {
            this.association = new SubObjectToExportsAssociation(pSubobjectToAssociate, exportNames);

            this.subobject = new StateSubObject(this.association);
        }
    }
}
