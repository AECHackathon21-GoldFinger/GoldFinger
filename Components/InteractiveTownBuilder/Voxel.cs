using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractiveTownBuilder
{
    struct Voxel
    {

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Voxel(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
