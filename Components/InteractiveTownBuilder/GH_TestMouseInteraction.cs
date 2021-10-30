using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InteractiveTownBuilder
{
    public class GH_TestMouseInteraction : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_TestMouseInteraction class.
        /// </summary>
        public GH_TestMouseInteraction()
          : base("TestMouseInteraction", "MouseInteract",
              "Description",
              "Goldfinger", "Test")
        {
            mouseLine = new Line?();
            callback = new MouseCallback(this);
        }

        List<Box> boxes = new List<Box>();
        List<Mesh> clickableMeshes = new List<Mesh>();
        private GH_Document doc;
        internal Line? mouseLine;
        private MouseCallback callback;
        bool enabled = false;

        int selectedBox = -1;
        int selectedFace = -1;


        internal bool inActiveDocument = true;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBoxParameter("testboxes", "testboxes", "testbox", GH_ParamAccess.list);
            pManager.AddBooleanParameter("enabled", "enabled", "enabled", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

            pManager.AddIntegerParameter("selectedVoxelID", "selectedVoxelID", "voxel id", GH_ParamAccess.item);
            
            pManager.AddIntegerParameter("selectedVotexFaceId", "FaceID", "faceID", GH_ParamAccess.item);
            pManager.AddMeshParameter("mesh", "mesh", "mesh", GH_ParamAccess.item);
            pManager.HideParameter(2);
            pManager.AddMeshParameter("faces", "faces", "faces", GH_ParamAccess.list);
            pManager.HideParameter(3);
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


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh outMesh = new Mesh();
            
            if (!DA.GetData(1, ref enabled))
            {
                callback.Enabled = false;
                selectedBox = -1;
                selectedFace = -1;
                return;
            }

            if (!enabled)
            {
                selectedBox = -1;
                selectedFace = -1;
                return;
            }


            callback.Enabled = enabled;

            boxes.Clear();
            clickableMeshes.Clear();

            DA.GetDataList(0, boxes);

            Vector3d testMove = new Vector3d(0, 0, 0);

            List<Mesh> selectedMeshes = new List<Mesh>();
            List<double> intersectParams = new List<double>();
            List<double> intersectParamsFaces = new List<double>();
            List<Mesh> outMeshes = new List<Mesh>();
            List<int> selectedBoxID = new List<int>();

            for (int i = 0; i < boxes.Count; i++)
            {

                
                boxes[i].Transform(Transform.Translation(testMove));
                clickableMeshes.Add(Mesh.CreateFromBox(boxes[i], 1, 1, 1));
                Mesh thisMesh = clickableMeshes[clickableMeshes.Count - 1];


     
                    double num = Intersection.MeshRay(thisMesh, new Ray3d(mouseLine.Value.From, new Vector3d(mouseLine.Value.To - mouseLine.Value.From)));
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

                selectedBox = selectedBoxID[0];

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
                    if(face.IsQuad)
                    {
                        ptlist.Add(pts[face.D]);
                    }

                    msh.Vertices.AddVertices(ptlist);
                    msh.Faces.AddFace(face.IsQuad ? new MeshFace(0,1,2,3) : new MeshFace(0,1,2));
                    outFaces.Add(msh);

                }
                

                for (int i = 0; i < outFaces.Count; i++)
                {
                    Mesh meshFace = outFaces[i];
                    
                    outMesh = meshFace;
                    outMeshes.Add(outMesh);
                    double num = Intersection.MeshRay(meshFace, new Ray3d(mouseLine.Value.From, new Vector3d(mouseLine.Value.To - mouseLine.Value.From)));
                    if (num >= 0.0)
                    {
                        intersectParamsFaces.Add(num);
                        
                        selectedFaces.Add(i);

                    }
                }

                IOrderedEnumerable<int> sourceFaces = Enumerable.Range(0, selectedFaces.Count).OrderByDescending(i => intersectParamsFaces[i]);
                selectedFace = sourceFaces.Select(i => selectedFaces[i]).First();
                outMesh = outFaces[selectedFace];

            }
            
            DA.SetData(0, selectedBox);
            DA.SetData(1, selectedFace);
            DA.SetData(2, outMesh);
            DA.SetDataList(3, outMeshes);
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            for (int i = 0; i < clickableMeshes.Count; i++)
            {
                args.Display.DrawMeshFalseColors(clickableMeshes[i]);
            }

            if (selectedBox >= 0 && selectedFace >= 0 && enabled)
            {
                args.Display.Draw2dText($"Selected ID is {selectedBox} and selected face is {selectedFace}", System.Drawing.Color.Black, new Point2d(20, 20), false, 20);
            }
            base.DrawViewportMeshes(args);
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                return new BoundingBox(new Point3d[] { new Point3d(0.0, 0.0, 0.0) }); //dummy
            }

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d813b41b-d025-4b28-b866-a86fb21a96bd"); }
        }
    }
}