using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace InteractiveTownBuilder
{
    public class InteractiveTownBuilderInfo : GH_AssemblyInfo
    {
        public override string Name => "InteractiveTownBuilder";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("3DB02F5E-56EE-45A1-A3BC-514F8AD83A05");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}