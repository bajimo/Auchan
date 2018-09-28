using Experior.Core.Loads;
using Experior.Core.Routes;
using Experior.Catalog.Dematic.Case.Components;
using System.Drawing;
using Experior.Catalog.Dematic.Case;
using Experior.Dematic.Base;
using System;
using Experior.Dematic.Base.Devices;
using System.Collections.Generic;

namespace Experior.Catalog.Dematic.Storage.MultiShuttle.Assemblies
{
    public class ElevatorConveyor : StraightConveyor//, ITransferLoad
    {
        private DematicActionPoint locationA = new DematicActionPoint();
        private DematicActionPoint locationB = new DematicActionPoint();
        public int Level = 0; //always 0 for elevator        
        public float height;
        public Elevator Elevator;
        public bool UnLoading = false; //we need to tag outfeeding as there is no difference between picking up 2 at differnt levels and dropping off at differnt levels

        public ElevatorConveyor(ElevatorConveyorInfo info) : base(info)
        {
            Elevator = info.Elevator;
            LocationA.LocName = string.Format("{0}{1}{2}{3}{4}", Elevator.AisleNumber.ToString().PadLeft(2, '0'), (char)Elevator.Side, "00", (char)ConveyorTypes.Elevator, "A");
            LocationB.LocName = string.Format("{0}{1}{2}{3}{4}", Elevator.AisleNumber.ToString().PadLeft(2, '0'), (char)Elevator.Side, "00", (char)ConveyorTypes.Elevator, "B");

            Route.InsertActionPoint(locationB, Route.Length / 2 + Route.Length / 4);
            Route.InsertActionPoint(locationA, Route.Length / 2 - Route.Length / 4);

            LocationA.OnEnter += LocationA_OnEnter;
            LocationB.OnEnter += LocationB_OnEnter;

            Elevator.ParentMultiShuttle.ConveyorLocations.Add(LocationA);
            Elevator.ParentMultiShuttle.ConveyorLocations.Add(LocationB);
            Leaving.OnEnter += ExitPoint_OnEnter;
            Entering.OnEnter += Entering_OnEnter;

            Entering.Name = "EnterPoint";
            Leaving.Name = "ExitPoint";

            Entering.Distance = 0.000001f;  //HACK this is a dodgy workarround. When switching to the Entering from a previous conveyors exit
                                            //if the conveyor has had its local yaw changed then it gets into an infinite loop somehow needs more investigation...Localyaw may be a red herring.... in fact it almost certinally is!!
                                            //ListSolutionExplorer = true;
        }

        void LocationB_OnEnter(ActionPoint sender, Load load)
        {
            if (Elevator.CurrentTask == null)
            {
                load.Stop();
                return;
            }

            if (Elevator.CurrentTask.Flow == TaskType.Infeed) //|| Elevator.CurrentTask.Flow == TaskType.HouseKeep)
            {
                if (Elevator.CurrentTask.NumberOfLoadsInTask == 1)
                {
                    Elevator.ParentMultiShuttle.ArrivedAtElevatorConvPosB(new ArrivedOnElevatorEventArgs(Elevator.CurrentTask, null, (Case_Load)load, Elevator, LocationB.LocName));
                    load.Stop();

                    StraightConveyor sC = Elevator.CurrentTask.GetSourceConvOfLoad((Case_Load)Load.Get(Elevator.CurrentTask.LoadB_ID), false);

                    if (sC != null)
                    {
                        sC.RouteAvailable = RouteStatuses.Request;
                    }
                    else
                    {
                        Log.Write("Error in ElevatorConveyor.LocationB_OnEnter() can't find Source Conv", Color.Red);
                        return;
                    }

                    Elevator.MoveElevator(Elevator.CurrentTask.GetDestConvOfLoad((Case_Load)load));
                }
                else if (Elevator.CurrentTask.NumberOfLoadsInTask == 2 )
                {     
                    load.Stop();

                    if (UnLoading)
                    {
                        Elevator.MoveElevator(Elevator.CurrentTask.GetDestConvOfLoad((Case_Load)load));
                    }
                    else
                    {
                        InfeedDoubleArrived();
                    }
                }
            }
            else if (Elevator.CurrentTask.Flow == TaskType.Outfeed)
            {
                //picking up 2 loads from different levels and this is the first load                
                if (Elevator.CurrentTask.LoadCycle == Cycle.Single && Elevator.CurrentTask.NumberOfLoadsInTask == 2 && TransportSection.Route.Loads.Count == 1)
                {
                    load.Stop();
                    Elevator.ParentMultiShuttle.ArrivedAtElevatorConvPosB(new ArrivedOnElevatorEventArgs(Elevator.CurrentTask, null, (Case_Load)load, Elevator, LocationB.LocName));
                }
                else if (Elevator.CurrentTask.LoadCycle == Cycle.Single || (Elevator.CurrentTask.NumberOfLoadsInTask == 2 && TransportSection.Route.Loads.Count == 2))
                {
                    load.Stop();  // stop if single or the first load of a double
                    Elevator.ParentMultiShuttle.ArrivedAtElevatorConvPosB(new ArrivedOnElevatorEventArgs(Elevator.CurrentTask, null, (Case_Load)load, Elevator, LocationB.LocName));
                }

                if (UnLoading || (Elevator.CurrentTask.TasksLoadsArrivedOnElevator == Elevator.CurrentTask.NumberOfLoadsInTask)) // last load of a double outfeed
                {
                    load.Stop();
                    Elevator.MoveElevator(); // The load at B is the first drop off    
                }
            }
            else if (Elevator.CurrentTask.Flow == TaskType.HouseKeep) //Intralevel moves
            {
                Elevator.ParentMultiShuttle.ArrivedAtElevatorConvPosB(new ArrivedOnElevatorEventArgs(Elevator.CurrentTask, null, (Case_Load)load, Elevator, LocationB.LocName));
                load.Stop();

                StraightConveyor sC = Elevator.CurrentTask.GetSourceConvOfLoad((Case_Load)Load.Get(Elevator.CurrentTask.LoadB_ID), false);

                if (sC != null)
                {
                    sC.RouteAvailable = RouteStatuses.Request;
                }
                else
                {
                    Log.Write("Error in ElevatorConveyor.LocationB_OnEnter() can't find Source Conv", Color.Red);
                    return;
                }

                StraightConveyor moveDest = Elevator.CurrentTask.GetDestConvOfLoad((Case_Load)load);
                float pointless = moveDest.Length; //please crash and give me access to the code

                Elevator.MoveElevator(Elevator.CurrentTask.GetDestConvOfLoad((Case_Load)load));
            }
        }

        void LocationA_OnEnter(ActionPoint sender, Load load)
        {
            if (Elevator.CurrentTask == null) return;

            if (Elevator.CurrentTask.RelevantElevatorTask(load))
            {
                Elevator.CurrentTask.TasksLoadsArrivedOnElevator++;
            }

            Elevator.ParentMultiShuttle.ArrivedAtElevatorConvPosA(new ArrivedOnElevatorEventArgs(Elevator.CurrentTask, (Case_Load)load, null, Elevator, LocationA.LocName));

            load.Stop();
            
            if (Elevator.CurrentTask.Flow == TaskType.Infeed)
            {
                if (Elevator.CurrentTask.NumberOfLoadsInTask == 1 || Elevator.CurrentTask.NumberOfLoadsInTask == 2 && TransportSection.Route.Loads.Count == 1) // first load of 2 or only loading 1
                {
                    load.Release();
                }
                else if (TransportSection.Route.Loads.Count == 2)
                {
                    InfeedDoubleArrived();
                }
            }
            else if (Elevator.CurrentTask.Flow == TaskType.Outfeed || Elevator.CurrentTask.Flow == TaskType.HouseKeep)
            {
                if (TransportSection.Route.Loads.Count == 1)
                {
                    load.Release();
                }

                if (Elevator.CurrentTask.NumberOfLoadsInTask == 2 && TransportSection.Route.Loads.Count == 2)
                {
                    if (Elevator.CurrentTask.TasksLoadsArrivedOnElevator == Elevator.CurrentTask.NumberOfLoadsInTask)
                    {
                        UnLoading = true;
                        Elevator.MoveElevator();
                    }
                }
                else if (Elevator.CurrentTask.LoadCycle == Cycle.Single)
                {
                    if (Elevator.CurrentTask.NumberOfLoadsInTask == 1)
                    {
                        UnLoading = true;
                    }
                    else
                    {
                        Elevator.MoveElevator();
                    }
                }

                //Release from the outfeed rack conv if picked up a single and there is one behind on the B location
                Case_Load theload = load as Case_Load;

                if (theload.Identification == Elevator.CurrentTask.LoadB_ID)
                {
                    string locA = string.Format("{0}A", Elevator.CurrentTask.SourceLoadB.Substring(0, Elevator.CurrentTask.SourceLoadB.Length - 1));
                    Elevator.ParentMultiShuttle.ConveyorLocations.Find(x => x.LocName == locA).Release();
                }
                else
                {
                    string locA = string.Format("{0}A", Elevator.CurrentTask.SourceLoadA.Substring(0, Elevator.CurrentTask.SourceLoadA.Length - 1));
                    Elevator.ParentMultiShuttle.ConveyorLocations.Find(x => x.LocName == locA).Release();
                }
            }                       
        }

        private void InfeedDoubleArrived()
        {
            if (LocationA.Active && LocationB.Active)
            {
                Elevator.ParentMultiShuttle.ArrivedAtElevatorConvPosA(new ArrivedOnElevatorEventArgs(Elevator.CurrentTask, (Case_Load)LocationA.ActiveLoad, (Case_Load)LocationB.ActiveLoad, Elevator, LocationA.LocName));
                Elevator.ParentMultiShuttle.ArrivedAtElevatorConvPosB(new ArrivedOnElevatorEventArgs(Elevator.CurrentTask, (Case_Load)LocationA.ActiveLoad, (Case_Load)LocationB.ActiveLoad, Elevator, LocationB.LocName));

                StraightConveyor sC = Elevator.CurrentTask.GetSourceConvOfLoad((Case_Load)LocationA.ActiveLoad, false);
                if (sC != null)
                {
                    sC.RouteAvailable = RouteStatuses.Request;
                }
                else
                {
                    Log.Write("Error in ElevatorConveyor.InfeedDoubleArrived() Can't find Source location of load", Color.Red);
                }

                StraightConveyor dC = Elevator.CurrentTask.GetDestConvOfLoad((Case_Load)LocationB.ActiveLoad);
                if (dC != null)
                {
                    Elevator.MoveElevator(Elevator.CurrentTask.GetDestConvOfLoad((Case_Load)LocationB.ActiveLoad));
                }
                else
                {
                    Log.Write("Error in ElevatorConveyor.InfeedDoubleArrived() Can't find Dest location of load", Color.Red);
                }
            }
        }

        public Route Route
        {
            get { return TransportSection.Route; }
        }

        public DematicActionPoint LocationA
        {
            get { return locationA; }
            set { locationA = value; }
        }

        public DematicActionPoint LocationB
        {
            get { return locationB; }
            set { locationB = value; }
        }

        public float ConveyorLength
        {
            get { return Length; }
            set
            {
                if (value <= 0) { return; }                    

                Length = value;
                LocationA.Distance = Route.Length / 2 - Route.Length / 4;
                LocationB.Distance = Route.Length / 2 + Route.Length / 4;
            }
        }
        
        /// <summary>
        /// Transfer the load to the next conveyor by switching on APs
        /// </summary>
        void ExitPoint_OnEnter(ActionPoint sender, Load load)
        {
            try
            {
                if (Elevator.CurrentTask != null)
                {
                    load.Switch(Elevator.CurrentTask.GetDestConvOfLoad((Case_Load)load).Entering);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString(), Color.Red);
                Log.Write(string.Format("Load: {0}, Name: {1}",load.Identification, Name), Color.Red);
                Experior.Core.Environment.Scene.Pause();
            }
        }

        void Entering_OnEnter(ActionPoint sender, Load load)
        {
            if (Route.Motor.Direction == Core.Motors.Motor.Directions.Backward)
            {
                if (Elevator.CurrentTask != null)
                {
                    load.Switch(Elevator.CurrentTask.GetDestConvOfLoad((Case_Load)load).Entering);
                }
            }
        }

        public override void Dispose()
        {
            LocationA.OnEnter -= LocationA_OnEnter;
            LocationB.OnEnter -= LocationB_OnEnter;
            Leaving.OnEnter   -= ExitPoint_OnEnter;

            base.Dispose();
        }
    }

    public class ElevatorConveyorInfo : StraightConveyorInfo
    {
        public Elevator Elevator;
    }
}