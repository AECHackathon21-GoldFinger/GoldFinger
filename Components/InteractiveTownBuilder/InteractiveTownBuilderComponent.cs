using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace InteractiveTownBuilder
{
    public class InteractiveTownBuilderComponent : GH_Component
    {

        public InteractiveTownBuilderComponent()
          : base("InteractiveTownBuilder", "ITB",
            "This component enables interactive town building in Grasshopper",
            "GoldFinger", "SetUp")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBoxParameter("Solution Space", "SS", "A bounding box of the solution space to consider", GH_ParamAccess.item, Box.Empty );
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Slots", "S", "The slots that monoceros will use for its solution", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }


        protected override System.Drawing.Bitmap Icon => null;


        public override Guid ComponentGuid => new Guid("BF47A037-8EC2-4370-A6C8-16148149E47F");
    }
}