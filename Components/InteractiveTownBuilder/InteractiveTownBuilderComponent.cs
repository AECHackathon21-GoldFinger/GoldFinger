using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InteractiveTownBuilder
{
    public class InteractiveTownBuilderComponent : GH_Component
    {
        Model model;
        private Box oldSolutionSpace = Box.Empty;
        Box[] boxArray = null;
        List<Point3d> Slots = new List<Point3d>();
        List<MeshFace> Faces = new List<MeshFace>();


        List<Box> boxes = new List<Box>();
        List<Mesh> clickableMeshes = new List<Mesh>();
        private GH_Document doc;
        internal Line? mouseLine;
        private MouseCallback callback;
        bool enabled = false;

        Voxel[] voxelArray;

        int selectedBox = -1;
        int selectedFace = -1;

        public InteractiveTownBuilderComponent()
          : base("InteractiveTownBuilder", "ITB",
            "This component enables interactive town building in Grasshopper",
            "GoldFinger", "SetUp")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBoxParameter("Bounding Box", "BB", "A bounding box of the solution space to consider", GH_ParamAccess.item, Box.Empty);
            pManager.AddNumberParameter("xSize", "X", "The size of a voxel meter", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("ySize", "Y", "The size of a voxel meter", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("zSize", "Z", "The size of a voxel meter", GH_ParamAccess.item, 3);
            pManager.AddBooleanParameter("Enable", "Enable", "enable", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Reset", "Reset", "Resets your world :o", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Slots", "S", "The slots that monoceros will use for its solution", GH_ParamAccess.list);
            pManager.AddBoxParameter("Boxes", "B", "The slotes represented as boxes", GH_ParamAccess.list);
        }

        protected override void BeforeSolveInstance()
        {
            this.doc = this.OnPingDocument();
            //this.doc.ObjectsDeleted -= new GH_Document.ObjectsDeletedEventHandler(this.ObjectsDeleted);
            //this.doc.ObjectsDeleted += new GH_Document.ObjectsDeletedEventHandler(this.ObjectsDeleted);
            //Instances.ActiveCanvas.Document.ContextChanged -= new GH_Document.ContextChangedEventHandler(this.ContextChanged);
            //Instances.ActiveCanvas.Document.ContextChanged += new GH_Document.ContextChangedEventHandler(this.ContextChanged);
            base.BeforeSolveInstance();
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            callback.Enabled = false;


            Box worldBox = Box.Empty;
            double xSize = 3;
            double ySize = 3;
            double zSize = 3;

            
            if (!DA.GetData(4, ref enabled))
            {
                callback.Enabled = false;
                selectedBox = -1;
                selectedFace = -1;
                return;
            }

            

            bool reset = false;
            DA.GetData(5, ref reset);

            DA.GetData("Bounding Box", ref worldBox);
            DA.GetData("xSize", ref xSize);
            DA.GetData("ySize", ref ySize);
            DA.GetData("zSize", ref zSize);

            if (reset || model == null)
            {
                if (worldBox.X.Length / xSize * worldBox.Y.Length / ySize * worldBox.Z.Length / zSize > 10e4)
                {
                    throw new Exception("you have inputted more than 10k cells. We believe this is an error with units, so we cancelled the request");

                }

                bool updated = CheckInput(worldBox, ref oldSolutionSpace);

                if (updated)
                {
                    model = CreateModel(oldSolutionSpace, xSize, ySize, zSize);
                   

                }
            }

            



            DA.SetDataList("Boxes", boxArray);


        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);
        }

        public void OnClick()
        {
            if(enabled && mouseLine.HasValue)
            {
                if (GetClickInfo(boxArray, mouseLine, out int selectedBoxIndex, out int selectedFaceIndex))
                {
                    selectedBox = selectedBoxIndex;
                    selectedFace = selectedFaceIndex;



                }



            }
        }


        public void OnMouseOver()
        {
            if (enabled && mouseLine.HasValue)
            {
                GetClickInfo(boxArray, mouseLine, out int selectedBoxIndex, out int selectedFaceIndex);
                selectedBox = selectedBoxIndex;
                selectedFace = selectedFaceIndex;
            }
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
        private Model CreateModel(Box box, double xSize, double ySize, double zSize)
        {
            var x_axis = box.X;
            var y_axis = box.Y;
            var groundHeight = box.Z.Min;

            Plane plane = new Plane(new Point3d(x_axis.Min, y_axis.Min, groundHeight), Vector3d.XAxis, Vector3d.YAxis);


            // Later check if geometry already exists and no ground plane needs to be created
            if (true)
            {

                (Box[] Volume, Voxel[] voxels) = ConstructBaseplane(plane, xSize, ySize, zSize, box, out int[] cellCount);
            
                return new Model(cellCount, new double[] { xSize, ySize, zSize }) { Voxels = voxels.ToList() };
            }

        }


        public bool GetClickInfo(Box[] boxes, Line? lineFromMouse, out int selectedBoxIndex, out int selectedFaceIndex)
        {
            if (!lineFromMouse.HasValue)
            {
                selectedBoxIndex = -1;
                selectedFaceIndex = -1;
                return false;
            }

            List<Mesh> clickableMeshes = new List<Mesh>();
            List<Mesh> selectedMeshes = new List<Mesh>();
            List<double> intersectParams = new List<double>();
            List<double> intersectParamsFaces = new List<double>();
            List<Mesh> outMeshes = new List<Mesh>();
            List<int> selectedBoxID = new List<int>();

            for (int i = 0; i < boxes.Length; i++)
            {

                clickableMeshes.Add(Mesh.CreateFromBox(boxes[i], 1, 1, 1));
                Mesh thisMesh = clickableMeshes[clickableMeshes.Count - 1];


                double num = Intersection.MeshRay(thisMesh, new Ray3d(lineFromMouse.Value.From, new Vector3d(lineFromMouse.Value.To - lineFromMouse.Value.From)));
                if (num >= 0.0)
                {
                    intersectParams.Add(num);
                    selectedMeshes.Add(thisMesh);

                    selectedBoxID.Add(i);
                }

            }

            IOrderedEnumerable<int> source = Enumerable.Range(0, selectedMeshes.Count).OrderByDescending(i => intersectParams[i]);
            selectedMeshes = source.Select(i => selectedMeshes[i]).ToList();
            selectedBoxID = source.Select(i => selectedBoxID[i]).ToList();

            List<int> selectedFaces = new List<int>();

            if (selectedMeshes.Count > 0)
            {
                selectedMeshes = new List<Mesh>()
                    {
                        selectedMeshes[0]
                    };

                selectedBoxIndex = selectedBoxID[0];

                Mesh m = selectedMeshes[0];

                List<Mesh> outFaces = new List<Mesh>();
                var faces = m.Faces;
                var pts = m.Vertices;

                for (int i = 0; i < faces.Count; i++)
                {

                    var face = faces[i];
                    var ptlist = new List<Point3d>();
                    var msh = new Mesh();

                    ptlist.Add(pts[face.A]);
                    ptlist.Add(pts[face.B]);
                    ptlist.Add(pts[face.C]);
                    if (face.IsQuad)
                    {
                        ptlist.Add(pts[face.D]);
                    }

                    msh.Vertices.AddVertices(ptlist);
                    msh.Faces.AddFace(face.IsQuad ? new MeshFace(0, 1, 2, 3) : new MeshFace(0, 1, 2));
                    outFaces.Add(msh);

                }


                for (int i = 0; i < outFaces.Count; i++)
                {
                    Mesh meshFace = outFaces[i];

                    double num = Intersection.MeshRay(meshFace, new Ray3d(lineFromMouse.Value.From, new Vector3d(lineFromMouse.Value.To - lineFromMouse.Value.From)));
                    if (num >= 0.0)
                    {
                        intersectParamsFaces.Add(num);

                        selectedFaces.Add(i);

                    }
                }

                IOrderedEnumerable<int> sourceFaces = Enumerable.Range(0, selectedFaces.Count).OrderByDescending(i => intersectParamsFaces[i]);
                selectedFaceIndex = sourceFaces.Select(i => selectedFaces[i]).First();
                

            }
            else
            {
                selectedBoxIndex = -1;
                selectedFaceIndex = -1;
                return false;
            }
           

            return true;
        }

        private (Box[] Volume, Voxel[] Slot) ConstructBaseplane(Plane plane, double xSize, double ySize, double zSize, Box box, out int[] cellCount)
        {
            Interval X = box.X;
            Interval Y = box.Y;
            var coutInX = ((int)Math.Round(X.Length / xSize));
            var coutInY = ((int)Math.Round(Y.Length / ySize));
            int heightCount = ((int)Math.Round(box.Z.Length / zSize));

            cellCount = new int[3] { coutInX, coutInY, heightCount };


            Interval x = new Interval(X.Min, X.Min + xSize);
            Interval y = new Interval(X.Min, X.Min + ySize);
            Interval z = new Interval(plane.OriginZ, plane.OriginZ - zSize);


            Box[] groundPlaneVolumes = new Box[coutInX * coutInY];
            Voxel[] groundPlaneSlots = new Voxel[coutInX * coutInY];


            for (int i = 0; i < (coutInX * coutInY); i += coutInX)
            {
                for (int j = 0; j < coutInX; ++j)
                {
                    groundPlaneVolumes[i + j] = new Box(plane, x, y, z);
                    groundPlaneSlots[i + j] = new Voxel(i,j,0);
                    x += xSize;
                }
                y += ySize;
                x = new Interval(X.Min, X.Min + xSize);
            }

            return (groundPlaneVolumes, groundPlaneSlots);
        }


        private bool EqualBoxes(Box a, Box b) 
            => a.X.Min == b.X.Min && a.X.Max == b.X.Max &&
            a.Y.Min == b.Y.Min && a.Y.Max == b.Y.Max &&
            a.Z.Min == b.Z.Min && a.Z.Max == b.Z.Max;


    }

}