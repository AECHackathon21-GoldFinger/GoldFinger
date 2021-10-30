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
        /// List of all voxels that are solids
        /// </summary>
        public List<Voxel> Voxels {get;set;}

        /// <summary>
        /// Dimensions of one voxel
        /// </summary>
        public double[] VoxelDimensions { get; set; } = new double[3];

        /// <summary>
        /// Dimensions of our entire grid (measured in number of voxels)
        /// </summary>
        public int[] GridDimensions { get; set; } = new int[3];

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


        public void AddVoxel(Voxel voxel) => this.Voxels.Add(voxel);
        public bool RemoveVoxel(Voxel voxel)
        {
            if (this.Voxels.Contains(voxel)) 
            {
                this.Voxels.Remove(voxel);
                return true;
            }
            return false;
        }

        public Box GetBox(Voxel voxel)  => new Box(basePlane, 
                new Interval(voxel.X, voxel.X + VoxelDimensions[0]),
                new Interval(voxel.Y, voxel.Y + VoxelDimensions[1]),
                new Interval(voxel.Z, voxel.Z + VoxelDimensions[2]));

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
            return false;
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
