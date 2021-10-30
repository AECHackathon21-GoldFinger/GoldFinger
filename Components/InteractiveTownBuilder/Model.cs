using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractiveTownBuilder
{

    /// <summary>
    /// The main model to host our "map" or "level"
    /// </summary>
    public class Model
    {

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


        public static Model operator +(Model a, Voxel b)
        {
            a.AddVoxel(b);
            return a;
        }
    }
}
