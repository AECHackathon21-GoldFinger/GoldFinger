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
            callback = new MouseCallback(this);
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

                if (updated || reset)
                {
                    model = CreateModel(worldBox, xSize, ySize, zSize);


                }
            }

            if (!reset)
            {
                callback.Enabled = enabled;

            }

            DA.SetDataList("Boxes", model.Voxels.Select(v => model.GetBox(v)));


        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (model != null)
            {
                if (model.SelectedVoxel.Z >= -1)
                {
                    DisplayMethods.BoxCorners(model.GetBox(model.SelectedVoxel), args);
                }
                if (model.SelectedDirection != Voxel.FaceDirections.None)
                {
                    DisplayMethods.BlankMesh(model.GetFaces(model.SelectedVoxel)[model.selectedFace], args);
                }
            }

            base.DrawViewportMeshes(args);
        }

        public void OnClick()
        {
            if (enabled)
            {
                if (GetClickInfo(model, mouseLine, out int selectedBoxIndex, out int selectedFaceIndex, out Voxel voxel, out int[] offset, out Voxel.FaceDirections faceDirection))
                {



                    model.SelectedVoxel = model.Voxels[selectedBoxIndex];


                    selectedBox = selectedBoxIndex;
                    selectedFace = selectedFaceIndex;

                    model.AddVoxel(new Voxel(voxel.X + offset[0], voxel.Y + offset[1], voxel.Z + offset[2]));
                    Rhino.RhinoApp.WriteLine($"Clicked {voxel}");
                    this.ExpireSolution(true);

                }



            }
        }




        public void OnMouseOver()
        {
            if (enabled && mouseLine.HasValue && model != null)
            {
                GetClickInfo(model, mouseLine, out int selectedBoxIndex, out int selectedFaceIndex, out Voxel voxel, out int[] offset, out Voxel.FaceDirections faceDirection);
                if (voxel.X != model.SelectedVoxel.X || voxel.Y != model.SelectedVoxel.Y || voxel.Z != model.SelectedVoxel.Z)
                {
                    Rhino.RhinoApp.WriteLine($"selected {voxel}");
                    model.SelectedDirection = faceDirection;
                    model.SelectedVoxel = voxel;
                    selectedBox = selectedBoxIndex;
                    selectedFace = selectedFaceIndex;
                }



            }
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("BF47A037-8EC2-4370-A6C8-16148149E47F");


        // Check if the inputes have changed  
        private bool CheckInput(Box solutionSpace_new, ref Box solutionSpace_existing)
        {
            // Compare hashes
            if (!EqualBoxes(solutionSpace_new, solutionSpace_existing))
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

                Voxel[] voxels = ConstructBaseplane(plane, xSize, ySize, zSize, box, out int[] gridSize);

                return new Model(gridSize, new double[] { xSize, ySize, zSize }) { Voxels = voxels.ToList() };
            }

        }


        public bool GetClickInfo(Model model, Line? lineFromMouse, out int selectedBoxIndex, out int selectedFaceIndex, out Voxel voxel, out int[] offset, out Voxel.FaceDirections faceDirection)
        {
            Box[] boxes = model.Voxels.Select(v => model.GetBox(v)).ToArray();
            faceDirection = Voxel.FaceDirections.None;
            //TODO: get box from voxels,

            if (!lineFromMouse.HasValue)
            {
                selectedBoxIndex = -1;
                selectedFaceIndex = -1;
                voxel = new Voxel();
                offset = new int[0];
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

                model.SelectedVoxel = model.Voxels[selectedBoxIndex];
                var outFaces = model.GetFaces(model.SelectedVoxel);

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

                model.selectedFace = selectedFaceIndex;

                voxel = model.Voxels[selectedBoxIndex];


                switch (selectedFaceIndex)
                {
                    case 0:
                        offset = new int[3] { 0, 0, -1 };
                        faceDirection = Voxel.FaceDirections.Down;
                        break;
                    case 1:
                        offset = new int[3] { 0, 0, 1 };
                        faceDirection = Voxel.FaceDirections.Up;
                        break;
                    case 2:
                        offset = new int[3] { 0, -1, 0 };
                        faceDirection = Voxel.FaceDirections.South;
                        break;
                    case 3:
                        offset = new int[3] { 1, 0, 0 };
                        faceDirection = Voxel.FaceDirections.East;
                        break;
                    case 4:
                        offset = new int[3] { 0, 1, 0 };
                        faceDirection = Voxel.FaceDirections.North;
                        break;
                    case 5:
                        offset = new int[3] { -1, 0, 0 };
                        faceDirection = Voxel.FaceDirections.West;
                        break;
                    default:
                        throw new Exception("wrong face id");

                }

            }
            else
            {
                selectedBoxIndex = -1;
                selectedFaceIndex = -1;
                voxel = new Voxel();
                offset = new int[0];
                return false;
            }
            Rhino.RhinoApp.WriteLine($"Clicked voxel {voxel.X}, {voxel.Y}, {voxel.Z}. Active face was {model.selectedFace} and the direction is {model.SelectedDirection}");
            return true;
        }

        private Voxel[] ConstructBaseplane(Plane plane, double xSize, double ySize, double zSize, Box box, out int[] cellCount)
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


            List<Voxel> groundPlaneSlotsList = new List<Voxel>();
            

            for (int ix = 0; ix <  coutInX; ++ix)
            {
                for (int jy = 0; jy < coutInY; ++jy)
                {
                    groundPlaneSlotsList.Add(new Voxel(ix, jy, 0));
                }
                y += ySize;
                x = new Interval(X.Min, X.Min + xSize);
            }
            Voxel[] groundPlaneSlots = groundPlaneSlotsList.ToArray();
            return groundPlaneSlots;
        }


        private bool EqualBoxes(Box a, Box b)
            => a.X.Min == b.X.Min && a.X.Max == b.X.Max &&
            a.Y.Min == b.Y.Min && a.Y.Max == b.Y.Max &&
            a.Z.Min == b.Z.Min && a.Z.Max == b.Z.Max;


    }

}