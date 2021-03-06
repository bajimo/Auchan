﻿using System;
using System.Drawing;
using Experior.Catalog.Dematic.Storage.MultiShuttle.Assemblies;

namespace Experior.Controller.AuchanCarvin
{
    public partial class AuchanCarvinRouting : Experior.Catalog.ControllerExtended
    {
        Experior.Core.Environment.UI.Toolbar.Button Speed1, Speed2, Speed5, Speed10, Speed20, HideShow, ResetButton, fps1, localProp, connectButt, disconnectButt;
        Core.Environment.UI.Toolbar.Button btnWMSt;
        ModelTools toolsForm;
        LocalProperties localProperties;

        private void StandardConstructor()
        {
            localProperties = new LocalProperties(this);

            //Add some speed buttons
            Speed1 = new Core.Environment.UI.Toolbar.Button("  1  ", Speed1_Click);
            Speed5 = new Core.Environment.UI.Toolbar.Button("  5  ", Speed5_Click);
            Speed2 = new Core.Environment.UI.Toolbar.Button("  2  ", Speed2_Click);
            Speed10 = new Core.Environment.UI.Toolbar.Button("  10  ", Speed10_Click);
            Speed20 = new Core.Environment.UI.Toolbar.Button("  20  ", Speed20_Click);
            ResetButton = new Core.Environment.UI.Toolbar.Button("Reset", Reset_Click);
            fps1 = new Core.Environment.UI.Toolbar.Button("1 FPS", FPS1_Click);
            localProp = new Core.Environment.UI.Toolbar.Button("Local", localProp_Click);
            connectButt = new Core.Environment.UI.Toolbar.Button("Connect", connectButt_Click);
            disconnectButt = new Core.Environment.UI.Toolbar.Button("Disconnect", disconnectButt_Click);
            HideShow = new Core.Environment.UI.Toolbar.Button("Hide/Show DMS", hideShow_Click);

            Core.Environment.UI.Toolbar.Add(Speed1, "Speed");
            Core.Environment.UI.Toolbar.Add(Speed2, "Speed");
            Core.Environment.UI.Toolbar.Add(Speed5, "Speed");
            Core.Environment.UI.Toolbar.Add(Speed10, "Speed");
            Core.Environment.UI.Toolbar.Add(Speed20, "Speed");
            Core.Environment.UI.Toolbar.Add(ResetButton, "Project");
            Core.Environment.UI.Toolbar.Add(fps1, "Project");
            Core.Environment.UI.Toolbar.Add(localProp, "Tools");
            //Core.Environment.UI.Toolbar.Add(connectButt, "Communication");
            //Core.Environment.UI.Toolbar.Add(disconnectButt, "Communication");
            Core.Environment.UI.Toolbar.Add(HideShow, "Project");


            toolsForm = new ModelTools(this);
            toolsForm.Show();
            toolsForm.Hide();

            //Add a button to load the form
            btnWMSt = new Core.Environment.UI.Toolbar.Button("Tools", btnWMSt_Click);
            Core.Environment.UI.Toolbar.Add(btnWMSt, "Tools");
        }

        private enum MessageSeverity
        {
            Critical,           //RED
            Warning,            //ORANGE
            Information,        //BLACK
            Test                //BLUE
        }

        private enum LoadType
        {
            Tote,
            Carton
        }

        private void ExperiorOutputMessage(string message, MessageSeverity messageSeverity)
        {
            Color colour = new Color();

            switch (messageSeverity)
            {
                case MessageSeverity.Critical: colour = System.Drawing.Color.Red; break;
                case MessageSeverity.Warning: colour = System.Drawing.Color.Orange; break;
                case MessageSeverity.Information: colour = System.Drawing.Color.Black; break;
                case MessageSeverity.Test: colour = System.Drawing.Color.Blue; break;
                default: colour = System.Drawing.Color.Black; break;
            }
            Experior.Core.Environment.Log.Write(DateTime.Now + " Routing: " + message, colour);
        }

        void btnWMSt_Click(object sender, EventArgs e)
        {
            toolsForm.Show();
            //Experior.Core.Environment.Scene.Reset();
        }

        void Speed20_Click(object sender, EventArgs e)
        {
            Experior.Core.Environment.Time.Scale = 20;
        }

        void Speed10_Click(object sender, EventArgs e)
        {
            Experior.Core.Environment.Time.Scale = 10;
        }

        void Speed5_Click(object sender, EventArgs e)
        {
            Experior.Core.Environment.Time.Scale = 5;
        }

        void Speed2_Click(object sender, EventArgs e)
        {
            Experior.Core.Environment.Time.Scale = 2;
        }

        void Speed1_Click(object sender, EventArgs e)
        {
            Experior.Core.Environment.Time.Scale = 1;
        }

        void Reset_Click(object sender, EventArgs e)
        {
            ResetStandard();
        }

        void FPS1_Click(object sender, EventArgs e)
        {
            Experior.Core.Environment.Scene.FPS = 1;
        }

        void localProp_Click(object sender, EventArgs e)
        {
            Core.Environment.Properties.Set(localProperties);
        }

        void connectButt_Click(object sender, EventArgs e)
        {
            foreach (Core.Communication.TCPIP.Connection connection in Core.Communication.Connection.Items.Values)
            {
                if (connection is Core.Communication.TCPIP.TCP)
                {
                    Core.Communication.TCPIP.TCP ffsConn = connection as Core.Communication.TCPIP.TCP;
                    ffsConn.Connect();
                    ffsConn.AutoConnect = false;
                }
            }
        }

        void disconnectButt_Click(object sender, EventArgs e)
        {
            foreach (Core.Communication.TCPIP.Connection connection in Core.Communication.Connection.Items.Values)
            {
                connection.Disconnect();
                connection.AutoConnect = false;
                connection.Reset();
            }
        }

        void hideShow_Click(object sender, EventArgs e)
        {
            for (int i = 1; i < 9; i++)
            {
                string name = string.Format("Multi-Shuttle {0}", i.ToString());
                MultiShuttle dms = Core.Assemblies.Assembly.Items[name] as MultiShuttle;

                if (dms.Visible)
                {
                    dms.Visible = false;
                }
                else
                {
                    dms.Visible = true;
                }

            }
        }
    }
}
