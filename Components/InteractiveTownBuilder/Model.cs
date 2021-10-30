using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Grasshopper.Kernel;

namespace InteractiveTownBuilder
{

    /// <summary>
    /// The main model to host our "map" or "level"
    /// </summary>
    public class Model
    {
        /// <summary>
        /// Private baseplane to avoid acidental overrides 
        /// </summary>
        private Plane basePlane = Plane.WorldXY;

        

        /// <summary>
        /// Public get method for the BasePlane
        /// </summary>
        public Plane BasePlane { get => basePlane; }

        /// <summary>
        /// 
        /// </summary>
        public List<Voxel> GroundPlane { get; set; }

        /// <summary>
        /// List of all voxels that are solids
        /// </summary>
        public List<Voxel> Voxels { get; set; }

        /// <summary>
        /// Dimensions of one voxel
        /// </summary>
        public double[] VoxelDimensions { get; set; } = new double[3];

        /// <summary>
        /// Dimensions of our entire grid (measured in number of voxels)
        /// </summary>
        public int[] GridDimensions { get; set; } = new int[3];

        public Voxel SelectedVoxel { get; set; } = new Voxel(0, 0, -2);

        /// <summary>
        /// used for logics
        /// </summary>
        public Voxel.FaceDirections SelectedDirection { get; set; } = Voxel.FaceDirections.None;


        public int selectedFace { get; set; } = -1;


        /// <summary>
        /// master model that hosts everything
        /// </summary>
        /// <param name="gridDimensions">number of voxels in each direction</param>
        /// <param name="voxelDimensions">size of one voxel</param>
        public Model(int[] gridDimensions, double[] voxelDimensions)
        {
            if (gridDimensions.Length != 3 || voxelDimensions.Length != 3)
            {
                throw new Exception("you have to input a 3d array to griddimensions and voxeldimensions");
            }

            VoxelDimensions = voxelDimensions;
            GridDimensions = gridDimensions;
        }


        public void AddVoxel(Voxel voxel) 
        {
            if (this.isInside(voxel)) this.Voxels.Add(voxel); 
        }
        public bool RemoveVoxel(Voxel voxel)
        {
            if (this.Voxels.Contains(voxel)) 
            {
                if (!this.isGroudplane(voxel))
                {
                    this.Voxels.Remove(voxel);
                    return true;
                }

            }
            return false;
        }


        public List<Mesh> GetFaces(Voxel v)
        {
            
            Mesh m = Mesh.CreateFromBox(GetBox(v), 1, 1, 1);

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

            return outFaces;
        }

        public Box GetBox(Voxel voxel)  => new Box(basePlane, 
                new Interval(voxel.X * VoxelDimensions[0], (voxel.X * VoxelDimensions[0]) + VoxelDimensions[0]),
                new Interval(voxel.Y * VoxelDimensions[1], (voxel.Y * VoxelDimensions[1]) + VoxelDimensions[1]),
                new Interval(voxel.Z * VoxelDimensions[2], (voxel.Z * VoxelDimensions[2]) + VoxelDimensions[2]));

        /// <summary>
        /// Check if the voxel is part of the solution space
        /// </summary>
        /// <param name="model"></param>
        /// <param name="voxel"></param>
        /// <returns></returns>
        private bool isInside(Voxel voxel) 
        {
            if (voxel.X < 0 | voxel.Y < 0 | voxel.Z < 0) return false;
            if (voxel.X < this.GridDimensions[0] & voxel.Y < this.GridDimensions[1] & voxel.Z < this.GridDimensions[2]) return true;
            return false;
        }

        /// <summary>
        /// Check if the voxel is part of the groundplane
        /// </summary>
        /// <param name="model"></param>
        /// <param name="voxel"></param>
        /// <returns></returns>
        private bool isGroudplane(Voxel voxel)
        {
            if (voxel.X < 0 | voxel.Y < 0 | voxel.Z > 0) return false;
            if (voxel.X < this.GridDimensions[0] & voxel.Y < this.GridDimensions[1] & voxel.Z > - this.GridDimensions[2]) return true;
            return false;
        }


        public void DisplayGround(IGH_PreviewArgs args) 
        {
            var groundPlane = this.GroundPlane.Select(v => this.GetBox(v));
            groundPlane.Select(b => b.Transform(Transform.Scale(basePlane, 1.0,1.0, 0.1)));


            foreach (Box box in groundPlane) box.BoxCorners(args);

        }

        public void DisplayModel(IGH_PreviewArgs args)
        {
            Mesh mesh = new Mesh();
            bool CustomJoin(Mesh mesh1, Mesh mesh2) 
            {
                mesh1.Append(mesh2);
                return true;
            }
            var meshs = this.Voxels.Where(v => this.isGroudplane(v) == false).Select( v => this.GetFaces(v).Select(m => CustomJoin(mesh,m)));
            mesh.BlankMesh(args);
        }

        public static Model operator +(Model a, Voxel b)
        {
            a.AddVoxel(b);
            return a;
        }
        public static Model operator -(Model a, Voxel b) 
        {
            a.RemoveVoxel(b);
            return a;
        }
    }
}
