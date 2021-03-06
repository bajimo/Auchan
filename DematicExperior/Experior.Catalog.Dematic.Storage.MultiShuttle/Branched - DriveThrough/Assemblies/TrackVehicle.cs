using Experior.Core.Assemblies;
using Experior.Core.Loads;
using Experior.Core.Routes;
using Experior.Core.TransportSections;
using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace Experior.Catalog.Dematic.Storage.MultiShuttle.Assemblies
{
    public class TrackVehicle : Box
    {
        public Core.Routes.ActionPoint DestAP; //This is the AP that is moved for the shuttle car to travel to
        public TrackRail Track;
        public event EventHandler OnVehicleArrived;
        public event EventHandler<ApEnterEventArgs> OnLoadArrived;
        internal StraightTransportSection shuttleConveyor;
        public ActionPoint shuttleAP, shuttleAP2; //shuttleAP2 this is for creating load in the rack so that it does not interact with shuttleAP until it needs to.
        private TrackVehicleInfo trackVehicleInfo;
        private Assembly ControlAssembly;

        public TrackVehicle(TrackVehicleInfo info) : base(info)
        {
            ControlAssembly = info.controlAssembly;

            trackVehicleInfo = info;
            Length           =  0.05f;
            Width            = 0.5f;
            Height           = 0.05f;
            Color            = Color.Yellow;

            shuttleConveyor = new StraightTransportSection(Core.Environment.Scene.DefaultColor, 0.1f, 0.1f) { Height = 0.05f };
            
            shuttleAP          = shuttleConveyor.Route.InsertActionPoint(shuttleConveyor.Length / 2);
            shuttleAP2         = shuttleConveyor.Route.InsertActionPoint(shuttleConveyor.Length / 2);
            shuttleAP.OnEnter += ShuttleAP_OnEnter;
            shuttleConveyor.Route.Motor.Stop();
            shuttleConveyor.Visible = false;
            Track                   = info.trackRail;
            DestAP                  = Track.Route.InsertActionPoint(info.moveToDistance);
            Track.Route.Add(this, info.moveToDistance);
            
            DestAP.Visible  = false;
            DestAP.OnEnter += MoveTo_OnEnter;
            Deletable       = false;
            UserDeletable   = false;
            Embedded        = true;
            Stop();
            Load.Items.Add(this);

            Core.Environment.Scene.OnLoaded += Scene_OnLoaded;
        }

        private void Scene_OnLoaded()
        {
            trackVehicleInfo.controlAssembly.Add(shuttleConveyor, new Vector3(Track.Length / 2, 0, 0));
            OnPositionChanged += TrackVehicle_OnPositionChanged;
            Core.Environment.Scene.OnLoaded -= Scene_OnLoaded;
        }

        public void Reset()
        {
            if(LoadOnBoard != null)
            {
                LoadOnBoard.Dispose();
            }
            Stop();
            if (Route != null)
            {
                Route.Motor.Reset();
                Route.Motor.Stop();
                Distance = trackVehicleInfo.moveToDistance;
                DestAP.Distance = trackVehicleInfo.moveToDistance;
            }
        }

        private void TrackVehicle_OnPositionChanged(Load load, Vector3 position)
        {
            shuttleConveyor.Route.TranslationVector = new Vector3(-load.Distance, 0, 0);
        }

        private void ShuttleAP_OnEnter(ActionPoint sender, Load load)
        {
            if(OnLoadArrived != null)
            {
                OnLoadArrived(this, new ApEnterEventArgs(load));
            }
        }

        public override void Dispose()
        {
            Deletable = true;
            shuttleAP.OnEnter -= ShuttleAP_OnEnter;
            DestAP.OnEnter -= MoveTo_OnEnter;
            OnPositionChanged -= TrackVehicle_OnPositionChanged;

            base.Dispose();
        }

        private void MoveTo_OnEnter(Core.Routes.ActionPoint sender, Load load)
        {
            Stop();
            Track.Route.Motor.Stop(); // Don't know if we really need to do this

            if (OnVehicleArrived != null)
            {
                OnVehicleArrived(this, new EventArgs());
            }
        }

        public enum ExceptionTypes
        {
            [Description("None")]
            None,
            [Description("Bin Store Full (status 04)")]
            BinStoreFull,
            [Description("Bin Store Blocked (status 12)")]
            BinStoreBlocked,
            [Description("Bin Retrieve Empty (status 05)")]
            BinRetrieveEmpty,
            [Description("Bin Retrieve Blocked (status 11)")]
            BinRetrieveBlocked
        }

        ExceptionTypes exceptionType = ExceptionTypes.None;

        [Category("Status")]
        [DisplayName("Exception")]
        [Description("Create this exception in next job. Note BinStoreBlocked and BinRetrieveBlocked is only sent if depth > 1.")]

        [ReadOnly(false)]
        [Core.Properties.AlwaysEditable]
        public ExceptionTypes ExceptionType
        {
            get { return exceptionType; }
            set { exceptionType = value; }
        }
        public bool InException = false;

        private bool _CompleteTask = false;
        [ReadOnly(false)]
        [Category("Elevator Maintenance")]
        [DisplayName("Complete Task")]
        [Description("[Elevator Only] Complete the current task and search for the next task")]
        [Core.Properties.AlwaysEditable]
        public bool CompleteTask
        {
            get { return _CompleteTask; }
            set
            {
                if (value)
                {
                    if (ControlAssembly is Elevator)
                    {
                        ((Elevator)ControlAssembly).CompleteTask();
                    }
                }
            }
        }

        private bool _ResetVehicle = false;
        [ReadOnly(false)]
        [Category("Elevator Maintenance")]
        [DisplayName("Reset")]
        [Description("[Elevator Only] Complete the current task, completely reset the elevator and delete all tasks in the task list")]
        [Core.Properties.AlwaysEditable]
        public bool ResetVehicle
        {
            get { return _ResetVehicle; }
            set
            {
                if (value)
                {
                    if (ControlAssembly is Elevator)
                    {
                        ((Elevator)ControlAssembly).ResetVehicle();
                    }
                }
            }
        }

        private bool _PrintTasks = false;
        [ReadOnly(false)]
        [Category("Elevator Maintenance")]
        [DisplayName("Print Tasks")]
        [Description("[Elevator Only] Print the list of tasks that the elevator has")]
        [Core.Properties.AlwaysEditable]
        public bool PrintTasks
        {
            get { return _PrintTasks; }
            set
            {
                if (value)
                {
                    if (ControlAssembly is Elevator)
                    {
                        ((Elevator)ControlAssembly).PrintTasks();
                    }
                }
            }
        }


        [Category("Status")]
        [DisplayName("Motor status")]
        [Description("Motor status - for debugging")]
        public string MotorStatus
        {
            get
            {
                if (Disposed)
                {
                    return "DISPOSED";
                }

                if (Route == null)
                {
                    return "NO MOTOR";
                }

                if (Stopped && !Route.Motor.Running)
                {
                    return "STOPPED";
                }

                if (!Stopped && Route.Motor.Running)
                {
                    return "RUNNING";
                }

                return "UNKNOWN";
            }
        }

        public string TaskDisplay
        {
            get;
            set;
        }

        [Browsable(false)]
        public override float Angle
        {
            get{ return base.Angle; }
            set{ base.Angle = value; }
        }


        [DisplayName("Vehicle Positon")]
        public float vehiclepositon
        {
            get { return DestAP.Distance; }
        }


        /// <summary>
        /// The number of loads currently on board the shuttle
        /// </summary>
        public int LoadsOnBoard
        {
            get
            {
                return shuttleConveyor.Route.Loads.Count;
            }
        }

        /// <summary>
        /// Give a reference to the actuaal load on board
        /// </summary>
        public Load LoadOnBoard
        {
            get
            {
                if (shuttleAP.Active)
                {
                    return shuttleAP.ActiveLoad;
                }
                return null;
            }
        }
    }

    public class ApEnterEventArgs : EventArgs
    {
        public readonly Load _load;
        public ApEnterEventArgs(Load load)
        {
            _load = load;
        }
            
    }
    public class TrackVehicleInfo : LoadInfo
    {
        public TrackRail trackRail;
        public float moveToDistance;
        public Assembly controlAssembly;
    }
}
