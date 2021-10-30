using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using Rhino.Display;

namespace InteractiveTownBuilder
{
    public static class DisplayMethods
    {
        static double arrowLength = 1.0;
        static DisplayMaterial blankMesh = new DisplayMaterial(Color.White, 0.1);
        static double hideDistance = 80;

        public static void DrawArrorRed(this Mesh mesh, int faceIndex, IGH_PreviewArgs args) 
        {
            Point3d meshCenter = HelpterFunctions.GetMeshFaceCenter(mesh, mesh.Faces[faceIndex]);

            var distance = meshCenter - args.Viewport.CameraLocation;

            if (distance.Length < hideDistance)
            {
                Vector3d faceNormal = mesh.FaceNormals[faceIndex];
                faceNormal.Unitize();

                Line line = new Line(meshCenter, faceNormal * arrowLength);
                args.Display.DrawLine(line, Color.Red, 2);
                args.Display.DrawArrow(line, Color.Red, 0.0, 0.2);
            }



            

        }

        public static void BoxCorners(this Box box, IGH_PreviewArgs args) 
        {
            args.Display.DrawBoxCorners( box.BoundingBox, Color.Black);
        }

        public static void BlankMesh(this Mesh mesh, IGH_PreviewArgs args) 
        {
            args.Display.DrawMeshShaded(mesh, blankMesh);
        }
    }


    public static class HelpterFunctions
    {
        public static Point3d GetMeshFaceCenter(Mesh mesh, MeshFace meshFace)
        {
            var A = mesh.Vertices[meshFace.A];
            var B = mesh.Vertices[meshFace.B];
            var C = mesh.Vertices[meshFace.C];

            Point3f vectorSum = A + B + C;
            int divide = 3;

            if (meshFace.IsQuad) { 
                var D = mesh.Vertices[meshFace.D];
                vectorSum = vectorSum + D;
                divide = 4;
            }
            var result = (Point3d)vectorSum;
            return result / divide;
        }
    }
}
