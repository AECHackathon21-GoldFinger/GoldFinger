using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InteractiveTownBuilder
{
    class MouseCallback : Rhino.UI.MouseCallback
    {

        private GH_TestMouseInteraction gh_component;

        public string HoverViewportName { get; set; }

        public List<Box> VisibleBoxes = new List<Box>();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        public MouseCallback(GH_TestMouseInteraction cm)
        {

            gh_component = cm;
            Enabled = false;
        }


        protected override void OnMouseDown(MouseCallbackEventArgs e)
        {
            if (e.Button == MouseButtons.Left && gh_component != null && (gh_component.inActiveDocument) && GetForegroundWindow() == RhinoApp.MainWindowHandle())
            {
                gh_component.mouseLine = new Line?(e.View.ActiveViewport.ClientToWorld(e.ViewportPoint));
                gh_component.ExpireSolution(true);
                e.Cancel = true;
            }
            else
            {
                base.OnMouseDown(e);
            }
        }


        public class RhinoSelectionEventArgs : EventArgs
        {

            public RhinoSelectionEventArgs(int voxelID, int faceID)
            {
                FaceID = faceID;
                VoxelID = voxelID;
            }
            public int VoxelID;
            public int FaceID;
        }
    }
}
