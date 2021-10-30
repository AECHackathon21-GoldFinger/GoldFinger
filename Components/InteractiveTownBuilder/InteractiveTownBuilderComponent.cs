using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace InteractiveTownBuilder
{
    public class InteractiveTownBuilderComponent : GH_Component
    {

        private Box SolutionSpace = Box.Empty;
        Box[] SolutionArray = null;
        List<Point3d> Slots = new List<Point3d>();

        public InteractiveTownBuilderComponent()
          : base("InteractiveTownBuilder", "ITB",
            "This component enables interactive town building in Grasshopper",
            "GoldFinger", "SetUp")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBoxParameter("Solution Space", "SS", "A bounding box of the solution space to consider", GH_ParamAccess.item, Box.Empty);
            pManager.AddNumberParameter("Size", "s", "The size of a voxel meter", GH_ParamAccess.item, 3);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Slots", "S", "The slots that monoceros will use for its solution", GH_ParamAccess.list);
            pManager.AddBoxParameter("Boxes", "B", "The slotes represented as boxes", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Box solutio_space = Box.Empty;
            double size = 3;

            DA.GetData("Solution Space", ref solutio_space);
            DA.GetData("Size", ref size);


            bool updated = CheckInput(solutio_space, ref SolutionSpace);


            if (updated)
                SolutionArray = SubdivideBox(SolutionSpace, size);


            DA.SetDataList("Boxes", SolutionArray);


        }


        protected override System.Drawing.Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("BF47A037-8EC2-4370-A6C8-16148149E47F");


        // Check if the inputes have changed  
        private bool CheckInput(Box solutionSpace_new, ref Box solutionSpace_existing)
        {
            // Compare hashes
            if (!EqualBoxes(solutionSpace_new ,solutionSpace_existing))
            {
                solutionSpace_existing = solutionSpace_new;
                return true;
            }
            return false;
        }


        // Subdivide the the bounding box
        private Box[] SubdivideBox(Box box, double cell_size)
        {
            var x_axis = box.X;
            var y_axis = box.Y;
            var groundHeight = box.Z.Min;

            Plane plane = new Plane(new Point3d(x_axis.Min, y_axis.Min, groundHeight), Vector3d.XAxis, Vector3d.YAxis);


            // Later check if geometry already exists and no ground plane needs to be created
            if (true)
                return ConstructBaseplane(plane, cell_size, box.X, box.Y);

        }

        private Box[] ConstructBaseplane(Plane plane, double size, Interval X, Interval Y)
        {
            var coutInX = ((int)Math.Round(X.Length / size));
            var coutInY = ((int)Math.Round(Y.Length / size));


            Interval x = new Interval(X.Min, X.Min + size);
            Interval y = new Interval(X.Min, X.Min + size);
            Interval z = new Interval(plane.OriginZ, plane.OriginZ - size);


            Box[] results = new Box[coutInX * coutInY];


            for (int i = 0; i < (coutInX * coutInY); i += coutInX)
            {
                for (int j = 0; j < coutInX; ++j)
                {
                    results[i + j] = new Box(plane, x, y, z);
                    x += size;
                }
                y += size;
                x = new Interval(X.Min, X.Min + size);
            }

            return results;
        }


        private bool EqualBoxes(Box a, Box b) 
            => a.X.Min == b.X.Min && a.X.Max == b.X.Max &&
            a.Y.Min == b.Y.Min && a.Y.Max == b.Y.Max &&
            a.Z.Min == b.Z.Min && a.Z.Max == b.Z.Max;
    }

}