﻿using Experior.Catalog.Dematic.Case;
using Experior.Catalog.Dematic.Case.Components;
using Experior.Core;
using Experior.Core.Loads;
using Experior.Core.Parts;
using Experior.Core.Routes;
using Experior.Dematic.Base;
using Experior.Dematic.Base.Devices;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace Experior.Catalog.Dematic.Storage.MultiShuttle.Assemblies
{
    public class PickStationConveyor : StraightAccumulationConveyor, IPickDropConv 
    {        
        public PickStationConveyorInfo pickStationConveyorInfo;
        public DematicActionPoint LocationA, LocationB;
        //private bool infeedInProgress;
        private LoadWaitingStatus PreviousLoadWaitingStatus;
        //public int Level = 1; //TODO currently only dealing with a single infeed level more dev needed for > 1 levels
        //public int AisleNumber;
        //public float Height;
        //public RackSide Side;

        public Timer psTimeoutTimer;

        public PickStationConveyor(PickStationConveyorInfo info) : base(info)
        {            
            pickStationConveyorInfo = info;
            Name = info.name;
            RouteAvailable = RouteStatuses.Request;
            LineFullPosition = 0; // no line full needed

            psTimeoutTimer = new Timer(ParentMultiShuttle.PStimeout);
            ParentMultiShuttle.OnPSTimeOutChanged += ParentMultiShuttle_OnPSTimeOutChanged;
            psTimeoutTimer.OnElapsed += psTimeoutTimer_OnElapsed;
        }

        void ParentMultiShuttle_OnPSTimeOutChanged(object sender, System.EventArgs e)
        {            
            psTimeoutTimer.Stop();
            psTimeoutTimer.Timeout = ParentMultiShuttle.PStimeout;
            psTimeoutTimer.Reset();
        }

        public override void StartFixPoint_OnSnapped(FixPoint stranger, FixPoint.SnapEventArgs e)
        {
            PreviousConveyor = stranger.Parent as IRouteStatus;
            PreviousLoadWaitingStatus = PreviousConveyor.GetLoadWaitingStatus(stranger);
            PreviousLoadWaitingStatus.OnLoadWaitingChanged += PreviousLoadWaitingStatus_OnLoadWaitingChanged;
        }

        public override void StartFixPoint_OnUnSnapped(FixPoint stranger)
        {
            PreviousConveyor = null;
            PreviousLoadWaitingStatus.OnLoadWaitingChanged -= PreviousLoadWaitingStatus_OnLoadWaitingChanged;
            PreviousLoadWaitingStatus = null;
            Reset();
        }

        public override void ThisRouteStatus_OnAvailableChanged(object sender, RouteStatusChangedEventArgs e)
        {
            base.ThisRouteStatus_OnAvailableChanged(sender, e);
            if (e._available == RouteStatuses.Request && PreviousLoadWaitingStatus != null && PreviousLoadWaitingStatus.LoadWaiting)
            {
                RouteAvailable = RouteStatuses.Available;
            }
        }

        void PreviousLoadWaitingStatus_OnLoadWaitingChanged(object sender, LoadWaitingChangedEventArgs e)
        {
            if (e._loadWaiting) //Load is waiting to be released from previous conveyor
            {
                if (RouteAvailable == RouteStatuses.Request)
                {
                    psTimeoutTimer.Stop();
                    psTimeoutTimer.Reset();
                    RouteAvailable = RouteStatuses.Available;
                    ParentMultiShuttle.LoadTransferingToPickStation(new PickDropStationArrivalEventArgs(null, e._waitingLoad as Case_Load, Elevator,1));
                }
            }
            else //Load has completed transfer from previous conveyor
            {
                if (RouteAvailable == RouteStatuses.Available)
                {
                    if (TransportSection.Route.Loads.Count == 2) //2 loads are on the pick station
                    {
                        RouteAvailable = RouteStatuses.Blocked;
                    }
                    else
                    {
                        RouteAvailable = RouteStatuses.Request;
                    }
                }
            }
        }

        /// <summary>
        /// As the accumilation conveyor length is not set conventually it is set via positions, outfeedsection
        /// infeedsection and AccPitch. We have to wait until all these are set. So we need this method to place 
        /// transition points.
        /// </summary>
        public void ConvLocationConfiguration(string level)
        {
            Leaving.OnEnter += Leaving_OnEnter;
            RouteStatus nextRouteStatus = new RouteStatus() { Available = RouteStatuses.Available };
            NextRouteStatus = nextRouteStatus;
            Leaving.Distance = Length;
 
            foreach (AccumulationSensor sensor in sensors)
            {
                //The accumilation conveyor PEC last (wrt the load travel direction) position is called "0", this is position 2 for the multishuttle.
                //The accumilation conveyor PEC second from last position (wrt the load travel direction) is called "1", this is position 1 for the multishuttle.

                int convPosName;
                if (int.TryParse(sensor.sensor.Name, out convPosName) && convPosName == 0)
                {                    
                    LocationB                      = sensor.sensor;
                    LocationB.LocName              = string.Format("{0}{1}{2}{3}{4}", AisleNumber.ToString().PadLeft(2, '0'), (char)Side, level, (char)ConveyorTypes.Pick, "B");
                    ParentMultiShuttle.ConveyorLocations.Add(LocationB);
                    sensor.sensor.OnEnter         += sensor_OnEnterB;
                    //sensor.sensor.OnLeave         += sensor_OnLeave2;
                    //sensor.sensor.leaving.Edge     = ActionPoint.Edges.Leading;
                    //sensor.sensor.leaving.Distance = sensor.sensor.leaving.Distance + 0.02f;//ParentMultiShuttle.workarround; //Workaround for a load switiching to another conveyor whilst blocking a sensor, hopfully Xcelgo will fix this                                                                                            
                }
                else if (int.TryParse(sensor.sensor.Name, out convPosName) && convPosName == 1)
                { 
                    LocationA         = sensor.sensor;
                    LocationA.LocName = string.Format("{0}{1}{2}{3}{4}", AisleNumber.ToString().PadLeft(2, '0'), (char)Side, level, (char)ConveyorTypes.Pick, "A");
                    ParentMultiShuttle.ConveyorLocations.Add(sensor.sensor);
                    sensor.sensor.OnEnter += sensor_OnEnterA;
                }
            }
            LocationB.Visible = false;
        }

        public override void Scene_OnLoaded()
        {
            //base.Scene_OnLoaded();
        }

        public override void Reset()
        {
            foreach (Load l in Route.Loads)
            {
                l.Dispose();
            }

            base.Reset();
            RouteAvailable = RouteStatuses.Request;
            ElevatorPickOneOfTwo = false;
        }

        //void sensor_OnLeave2(DematicSensor sender, Load load)
        //{
        //    if (infeedInProgress) //This doesn't do anything!
        //    {
        //        RouteAvailable = RouteStatuses.Blocked;
        //    }
        //}

        public void sensor_OnEnterA(DematicSensor sender, Load load)
        {
            load.UserData = null;
            if (TransportSection.Route.Loads.Count == 2)
            {
                load.Stop();

                psTimeoutTimer.Stop();
                psTimeoutTimer.Reset();
                RouteAvailable = RouteStatuses.Blocked;
                ParentMultiShuttle.ArrivedAtPickStationConvPosA(new PickDropStationArrivalEventArgs(LocationA.LocName, (Case_Load)load, Elevator, 2));
            }
            else
            {
                ParentMultiShuttle.ArrivedAtPickStationConvPosA(new PickDropStationArrivalEventArgs(LocationA.LocName, (Case_Load)load, Elevator,1));
            }
        }

        private void sensor_OnEnterB(DematicSensor sender, Load load)
        {
            if (RouteAvailable == RouteStatuses.Blocked && TransportSection.Route.Loads.Count < 2 && !ElevatorPickOneOfTwo)
            {
                //This is the second load. Let it move to the elevator
                return;
            }

            if (RouteAvailable == RouteStatuses.Blocked && ElevatorPickOneOfTwo)
            {
                //RouteAvailable = RouteStatuses.Request;
                ElevatorPickOneOfTwo = false;
            }

            load.Stop();
            psTimeoutTimer.Start();
            //ParentMultiShuttle.ArrivedAtPickStationConvPosB(new PickDropStationArrivalEventArgs(LocationB.LocName, (Case_Load)load, Elevator));
        }

        void psTimeoutTimer_OnElapsed(Timer sender)
        {
            if (TransportSection.Route.Loads.Count > 1)
                return;            

            if (LocationB.ActiveLoad != null && RouteAvailable == RouteStatuses.Request) //If the conveyor is availbale then do not send arrival message as one is transferring
            {
                RouteAvailable = RouteStatuses.Blocked;
                ParentMultiShuttle.ArrivedAtPickStationConvPosB(new PickDropStationArrivalEventArgs(LocationB.LocName, (Case_Load)LocationB.ActiveLoad, Elevator,1));
            }
        }

        private Route route;

        public Route Route
        {
          get { return TransportSection.Route; }
          set { route = value; }
        }

        [Browsable(false)]
        public Core.Parts.FixPoint infeedFix { get; set; }

        [Browsable(false)]
        public Elevator Elevator 
        {
            get { return pickStationConveyorInfo.elevator; }
            set { pickStationConveyorInfo.elevator = value; }
        }

        [Browsable(false)]
        public LevelID Level
        {
             get { return pickStationConveyorInfo.level; }
             set { pickStationConveyorInfo.level = value; }
        }

        [Browsable(false)]
        public MultiShuttle ParentMultiShuttle
        {
            get { return pickStationConveyorInfo.parentMultiShuttle; }
            set { pickStationConveyorInfo.parentMultiShuttle = value; }
        }

        public RackSide Side { get; set; }

        public int AisleNumber { get; set; }
        public bool ElevatorPickOneOfTwo { get; internal set; }

        void Leaving_OnEnter(ActionPoint sender, Load load)
        {
            ClearPreviousPhotocells(load);
            load.Switch(Elevator.ElevatorConveyor.Entering);
        }

        private void ClearPreviousPhotocells(Load load)
        {
            //Look back at the previous conveyor and clear the load from any photocells that it may still be covering
            //This is to stop the load from locking up the divert when the divert point is closer than half the load
            //length away from the feeding conveyor photocell.

            for (int i = load.Route.ActionPoints.Count - 1; i >= 0; i--)
            {
                ActionPoint actionPoint = load.Route.ActionPoints[i];
                if (actionPoint is DematicSensor && actionPoint.ActiveLoad == load)
                {
                    ((DematicSensor)actionPoint).ForceLoadClear(load);
                    return;
                }
            }
        }

    }

    public class PickStationConveyorInfo:StraightAccumulationConveyorInfo , IPickDropConvInfo
    {
        //public Elevator elevator;
        //public MultiShuttle parentMultiShuttle;

        public MultiShuttle parentMultiShuttle{ get; set; }
        public Elevator elevator { get; set; }
        public LevelID level { get; set; }

        Color IPickDropConvInfo.color
        {
            get{ return color;}
            set{ color = value;}
        }

        string IPickDropConvInfo.name
        {
            get{ return name;}
            set { name = value; }
        }

        float IPickDropConvInfo.thickness
        {
            get{ return thickness;}
            set{ thickness = value;}
        }
    }
}
