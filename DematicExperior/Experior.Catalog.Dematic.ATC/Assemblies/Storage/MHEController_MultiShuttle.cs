﻿using System;
using System.Collections.Generic;
using System.Linq;
using Experior.Dematic.Base;
using System.ComponentModel;
using Experior.Catalog.Dematic.Storage.MultiShuttle.Assemblies;
using Experior.Core.Assemblies;
using System.Drawing;
using Dematic.ATC;
using Experior.Dematic.Base.Devices;
using Experior.Core;
using Experior.Core.Loads;

namespace Experior.Catalog.Dematic.ATC.Assemblies.Storage
{
    public class MHEController_Multishuttle : BaseATCController, IController, IMultiShuttleController
    {
        MHEController_MultishuttleATCInfo mHEController_MultishuttleInfo;
        List<MultiShuttle> multishuttles = new List<MultiShuttle>();
        List<MHEControl> controls = new List<MHEControl>();
        MHEControl_MultiShuttle control;
        List<Timer> RunningPSTimers = new List<Timer>();

        public MHEController_Multishuttle(MHEController_MultishuttleATCInfo info) : base(info)
        {
            mHEController_MultishuttleInfo = info;
        }

        public override void Reset()
        {
            waitForSecondTransportAtPs.Clear();
            ignoreTelegrams.Clear();
            RunningPSTimers.Clear();
            base.Reset();
        }

        public override void HandleTelegrams(string[] telegramFields, TelegramTypes type)
        {
            switch (type)
            {
                case TelegramTypes.StartTransportTelegram:
                case TelegramTypes.MultishuttleStartTransportTelegram:
                    StartTransportTelegramReceived(telegramFields);
                    break;

                case TelegramTypes.StartMultipleTransportTelegram:
                case TelegramTypes.MultishuttleStartMultipleTransportTelegram:
                    StartMultipleTransportTelegramReceived(telegramFields);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This method is called after TUDR messages are sent at the pick stations, a timer is started, 
        /// if the TUMI is not received after 30s the messages are resent.  
        /// </summary>
        /// <param name="LoadPosB"></param>
        public void PickStationLock(IATCCaseLoadType LoadPosB)
        {
            Timer PickStationResendTimer = new Timer(30);
            RunningPSTimers.Add(PickStationResendTimer);
            PickStationResendTimer.UserData = new object[] { LoadPosB };
            PickStationResendTimer.OnElapsed += PickStationResendTimer_OnElapsed;
            PickStationResendTimer.Start();
        }

        private void PickStationResendTimer_OnElapsed(Timer sender)
        {
            RunningPSTimers.Remove(sender);
            sender.OnElapsed -= PickStationResendTimer_OnElapsed;
            sender.Dispose();

            IATCCaseLoadType LoadPosB = (IATCCaseLoadType)(((object[])sender.UserData)[0]);
            RemoveIgnoreCase(LoadPosB);
            if (LoadPosB.UserData == null) return;

            var psConv = LoadPosB.Route.Parent.Parent as PickStationConveyor;
            var elevator = psConv.Elevator;
            var tasks = elevator.ElevatorTasks;
            if (tasks.Any(t => t.LoadB_ID == LoadPosB.TUIdent) || (elevator.CurrentTask != null && elevator.CurrentTask.LoadB_ID == LoadPosB.TUIdent))
            {
                Log.Write($"{Name}: PickStationResendTimer_OnElapsed for tote at PS but elevator already ordered to pick only the front tote. Timeout ignored.");
                return;
            }

            SendTelegram((string)LoadPosB.UserData, true);
            PickStationLock(LoadPosB);
        }

        private void StartMultipleTransportTelegramReceived(string[] telegramFields)
        {
            string currentLoc = string.Empty;
            DematicActionPoint locA = null, locB = null;
            MultiShuttle ms = null;

            TelegramTypes type = telegramFields.GetTelegramType();

            List<string> indexTags;
            List<string> messageBodies = Telegrams.DeMultaplise(telegramFields, telegramFields.GetTelegramType(), out indexTags);
            string[] messageBodySplit = messageBodies.First().Split(',');//don't care which message is used as both on the same conveyor
            string source = messageBodySplit.GetFieldValue(TelegramFields.sources);
            string destLoc0 = telegramFields.GetFieldValue(TelegramFields.destinations, "[0]");
            string destLoc1 = telegramFields.GetFieldValue(TelegramFields.destinations, "[1]");
            string tuIdent0 = telegramFields.GetFieldValue(TelegramFields.tuIdents, "[0]");
            string tuIdent1 = telegramFields.GetFieldValue(TelegramFields.tuIdents, "[1]");
            string aisle = GetPSDSLocFields(source, PSDSRackLocFields.Aisle);
            string side = GetPSDSLocFields(source, PSDSRackLocFields.Side);
            string destA = null, destB = null;

            // takes the form aasyyxz: aa=aisle, s = side, yy = level, x = conv type see enum ConveyorTypes , Z = loc A or B e.g. 01R05OA
            string loc = string.Format("{0}{1}{2}P", aisle, side, GetPSDSLocFields(source, PSDSRackLocFields.Level));

            ms = GetMultishuttleFromAisleNum(loc + "A");
            locA = ms.ConveyorLocations.Find(x => x.LocName == loc + "A");
            locB = ms.ConveyorLocations.Find(x => x.LocName == loc + "B");

            if ((locA != null && locA.Active) && (locB != null && locB.Active))
            {

                IATCCaseLoadType caseA = locA.ActiveLoad as IATCCaseLoadType;
                IATCCaseLoadType caseB = locB.ActiveLoad as IATCCaseLoadType;
                //Stop the timer if the load matches any loads that are waiting at the pick station

                foreach (Timer timer in RunningPSTimers)
                {
                    IATCCaseLoadType LoadPosB = (IATCCaseLoadType)(((object[])timer.UserData)[0]);

                    if (LoadPosB == caseB)
                    {
                        RunningPSTimers.Remove(timer);
                        timer.Stop();
                        timer.OnElapsed -= PickStationResendTimer_OnElapsed;
                        timer.Dispose();
                        break;
                    }
                }

                string messageA = messageBodies.Find(x => x.Contains(locA.ActiveLoad.Identification));
                string messageB = messageBodies.Find(x => x.Contains(locB.ActiveLoad.Identification));

                if (destLoc0 == null || destLoc1 == null)
                {
                    Log.Write(string.Format("{0}: Invalid destinations sent in StartMultipleTransportTelegram", Name), Color.Red);
                    return;
                }

                if (telegramFields.GetFieldValue(TelegramFields.destinations, "[0]").LocationType() == LocationTypes.DS)  //Assume that both going to DS
                {
                    destA = string.Format("{0}{1}{2}{3}A", aisle,
                                                          side,
                                                          GetPSDSLocFields(destLoc0, PSDSRackLocFields.Level),
                                                          GetPSDSLocFields(destLoc0, PSDSRackLocFields.ConvType));
                    destB = destA;
                }
                else if (telegramFields.GetFieldValue(TelegramFields.destination).LocationType() == LocationTypes.RackConv)
                {
                    //need to calculate which way around the loads are on the PS and ensure that the elevator task is correct
                    if (((IATCCaseLoadType)(locA.ActiveLoad)).TUIdent == tuIdent0)
                    {
                        destA = string.Format("{0}{1}{2}{3}B", aisle, side, GetLocFields(destLoc0, PSDSRackLocFields.Level), GetLocFields(destLoc0, PSDSRackLocFields.ConvType));

                    }
                    else if (((IATCCaseLoadType)(locA.ActiveLoad)).TUIdent == tuIdent1)
                    {
                        destA = string.Format("{0}{1}{2}{3}B", aisle, side, GetLocFields(destLoc1, PSDSRackLocFields.Level), GetLocFields(destLoc1, PSDSRackLocFields.ConvType));
                    }
                    else
                    {
                        Log.Write(string.Format("Error {0}: The tuIdent at the PS does not match the tuIdent in the StartMultipleTransportTelegram (Position A"), Color.Orange);
                        return;
                    }

                    if (((IATCCaseLoadType)(locB.ActiveLoad)).TUIdent == tuIdent0)
                    {
                        destB = string.Format("{0}{1}{2}{3}B", aisle, side, GetLocFields(destLoc0, PSDSRackLocFields.Level), GetLocFields(destLoc0, PSDSRackLocFields.ConvType));

                    }
                    else if (((IATCCaseLoadType)(locB.ActiveLoad)).TUIdent == tuIdent1)
                    {
                        destB = string.Format("{0}{1}{2}{3}B", aisle, side, GetLocFields(destLoc1, PSDSRackLocFields.Level), GetLocFields(destLoc1, PSDSRackLocFields.ConvType));
                    }
                    else
                    {
                        Log.Write(string.Format("Error {0}: The tuIdent at the PS does not match the tuIdent in the StartMultipleTransportTelegram (Position A"), Color.Orange);
                        return;
                    }
                }
                else
                {
                    destA = GetRackDestinationFromDCIBinLocation(messageA);
                    destB = GetRackDestinationFromDCIBinLocation(messageB);
                }

                if (messageA != null && messageB != null)
                {
                    ElevatorTask et = new ElevatorTask(locA.ActiveLoad.Identification, locB.ActiveLoad.Identification)
                    {
                        LoadCycle = Cycle.Double,
                        Flow = TaskType.Infeed,
                        SourceLoadA = locA.LocName,
                        SourceLoadB = locB.LocName,
                        DestinationLoadA = destA,
                        DestinationLoadB = destB,
                        UnloadCycle = Cycle.Single
                    };
                    if (et.DestinationLoadA == et.DestinationLoadB)
                    {
                        et.UnloadCycle = Cycle.Double;
                    }

                    UpDateLoadParameters(messageA.Split(','), (IATCCaseLoadType)locA.ActiveLoad);
                    UpDateLoadParameters(messageB.Split(','), (IATCCaseLoadType)locB.ActiveLoad);

                    ms.elevators.First(x => x.ElevatorName == side + aisle).ElevatorTasks.Add(et);
                    //ms.elevators[side+aisle].ElevatorTasks.Add(et);
                }
                else
                {
                    Log.Write("ERROR: Load ids from telegram do not match active load ids in StartMultipleTransportTelegramReceived", Color.Red);
                    return;
                }
            }
            else
            {
                Log.Write("ERROR: Can't find locations or can't find loads on the locations named in StartMultipleTransportTelegramReceived", Color.Red);
            }
        }

        private Dictionary<IATCCaseLoadType, Timer> waitForSecondTransportAtPs = new Dictionary<IATCCaseLoadType, Timer>();
        private HashSet<IATCCaseLoadType> ignoreTelegrams = new HashSet<IATCCaseLoadType>();

        public void RemoveIgnoreCase(IATCCaseLoadType caseload)
        {
            ignoreTelegrams.Remove(caseload);
        }

        private void StartTransportTelegramReceived(string[] telegramFields)
        {
            try
            {
                if (Core.Environment.InvokeRequired)
                {
                    Core.Environment.Invoke(() => StartTransportTelegramReceived(telegramFields));
                    return;
                }

                //Look for the load somewhere in the model, if the load is found then change it's status, if not create it at the source location
                IATCCaseLoadType caseLoad = (IATCCaseLoadType)Case_Load.GetCaseFromIdentification(telegramFields.GetFieldValue(TelegramFields.tuIdent));

                if (caseLoad != null)
                {
                    if (telegramFields.GetFieldValue(TelegramFields.source).LocationType() == LocationTypes.PS)
                    {
                        //Check how many loads are at the pickstation, first i need to find the pick station

                        string sourceLoc = telegramFields.GetFieldValue(TelegramFields.source);
                        string destLoc = telegramFields.GetFieldValue(TelegramFields.destination);
                        string aisle = GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Aisle);
                        string side = GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Side);
                        string psLevel = GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Level);

                        string psA = string.Format("{0}{1}{2}{3}A", aisle, side, psLevel, GetPSDSLocFields(sourceLoc, PSDSRackLocFields.ConvType));
                        string psB = string.Format("{0}{1}{2}{3}B", aisle, side, psLevel, GetPSDSLocFields(sourceLoc, PSDSRackLocFields.ConvType));

                        MultiShuttle ms = GetMultishuttleFromAisleNum(psA);
                        PickStationConveyor psConv = ms.PickStationConveyors.Find(x => x.Name == string.Format("{0}{1}PS{2}", aisle, side, psLevel));

                        //Check how many loads are on the pickstation if there is only 1 then send a single mission
                        if (psConv.LocationA.Active && psConv.LocationB.Active)
                        {
                            IATCCaseLoadType caseA = psConv.LocationA.ActiveLoad as IATCCaseLoadType;
                            IATCCaseLoadType caseB = psConv.LocationB.ActiveLoad as IATCCaseLoadType;
                            //Stop the timer if the load matches any loads that are waiting at the pick station

                            if (ignoreTelegrams.Contains(caseB))
                            {
                                Log.Write($"{Name} Ignoring transport recieved at PS. Load B {caseB.TUIdent} received transport");
                                return;
                            }

                            if (ignoreTelegrams.Contains(caseA))
                            {
                                Log.Write($"{Name} Ignoring transport recieved at PS. Load B {caseA.TUIdent} received transport");
                                return;
                            }

                            foreach (Timer timer in RunningPSTimers)
                            {
                                IATCCaseLoadType LoadPosB = (IATCCaseLoadType)(((object[])timer.UserData)[0]);

                                if (LoadPosB == caseB)
                                {
                                    RunningPSTimers.Remove(timer);
                                    timer.Stop();
                                    timer.OnElapsed -= PickStationResendTimer_OnElapsed;
                                    timer.Dispose();
                                    break;
                                }
                            }

                            //This is a double move to the elevator so don't send a single
                            if (caseB.TUIdent == telegramFields.GetFieldValue(TelegramFields.tuIdent)) //LocationB just set the destination
                            {
                                Log.Write($"{Name} two loads at PS. Load B {caseB.TUIdent} received transport");
                                UpDateLoadParameters(telegramFields, caseB);
                                caseB.Destination = telegramFields.GetFieldValue(TelegramFields.destination);
                                caseB.Source = telegramFields.GetFieldValue(TelegramFields.source);
                                //MRP 29-01-2019: only wait 10 sec for second transport at PS.
                                if (!waitForSecondTransportAtPs.ContainsKey(caseA))
                                {
                                    var timer = new Timer(10);
                                    waitForSecondTransportAtPs[caseA] = timer;
                                    timer.UserData = telegramFields;
                                    timer.OnElapsed += SecondCaseTimeout;
                                    timer.Start();
                                }

                                //MRP 24-10-2018 send load arrived for load A when we got transport telegram for load B
                                string bodyA = (string)(caseA.UserData);
                                SendTelegram(bodyA, true); //position A load 
                                return;
                            }
                            else if (caseA.TUIdent == telegramFields.GetFieldValue(TelegramFields.tuIdent)) 
                                //LocationA Should be the second message so create the elevator task
                            {
                                Log.Write($"{Name} two loads at PS. Load A {caseA.TUIdent} received transport");

                                //MRP 29-01-2019: only wait 10 sec for second transport at PS.
                                //Stop waiting for transport - we got a transport
                                if (waitForSecondTransportAtPs.TryGetValue(caseA, out var timer))
                                {
                                    timer.Stop();
                                    timer.OnElapsed -= SecondCaseTimeout;
                                    timer.Dispose();
                                    waitForSecondTransportAtPs.Remove(caseA);
                                }

                                var elevator = ms.elevators.First(x => x.ElevatorName == side + aisle);
                                var tasks = elevator.ElevatorTasks;
                                if (tasks.Any(t => t.LoadB_ID == caseB.TUIdent) || (elevator.CurrentTask != null && elevator.CurrentTask.LoadB_ID == caseB.TUIdent))
                                {
                                    Log.Write($"{Name}: Transport mission received for second tote at PS but elevator already ordered to pick only the front tote. Transport ignored.");
                                    return;
                                }

                                UpDateLoadParameters(telegramFields, caseA);
                                caseA.Destination = telegramFields.GetFieldValue(TelegramFields.destination);
                                caseA.Source = telegramFields.GetFieldValue(TelegramFields.source);

                                string DestLoadA, DestLoadB;

                                if (caseA.Destination.LocationType() == LocationTypes.DS && GetPSDSLocFields(caseA.Destination, PSDSRackLocFields.Side) != side)
                                {
                                    //First check if the destination is to a drop station... check if the drop station is on this elevator
                                    //If not choose a destination to level 1 and remember the load route 
                                    DestLoadA = string.Format("{0}{1}{2}{3}B",
                                                        GetPSDSLocFields(caseA.Source, PSDSRackLocFields.Aisle),
                                                        GetPSDSLocFields(caseA.Source, PSDSRackLocFields.Side),
                                                        FindLevelForReject(ms),
                                                        "I");
                                }
                                else
                                {

                                    DestLoadA = string.Format("{0}{1}{2}{3}B",
                                                            aisle,
                                                            side,
                                                            GetLocFields(caseA.Destination, PSDSRackLocFields.Level),
                                                            GetLocFields(caseA.Destination, PSDSRackLocFields.ConvType));
                                }

                                if (caseB.Destination.LocationType() == LocationTypes.DS && GetPSDSLocFields(caseB.Destination, PSDSRackLocFields.Side) != side)
                                {
                                    //First check if the destination is to a drop station... check if the drop station is on this elevator
                                    //If not choose a destination to level 1 and remember the load route 
                                    DestLoadB = string.Format("{0}{1}{2}{3}B",
                                                        GetPSDSLocFields(caseB.Source, PSDSRackLocFields.Aisle),
                                                        GetPSDSLocFields(caseB.Source, PSDSRackLocFields.Side),
                                                        FindLevelForReject(ms),
                                                        "I");
                                }
                                else
                                {
                                    DestLoadB = string.Format("{0}{1}{2}{3}B",
                                                            aisle,
                                                            side,
                                                            GetLocFields(caseB.Destination, PSDSRackLocFields.Level),
                                                            GetLocFields(caseB.Destination, PSDSRackLocFields.ConvType));
                                }

                                ElevatorTask et = new ElevatorTask(caseA.TUIdent, caseB.TUIdent)
                                {
                                    LoadCycle = Cycle.Double,
                                    Flow = TaskType.Infeed,
                                    SourceLoadA = psA,
                                    SourceLoadB = psB,
                                    DestinationLoadA = DestLoadA,
                                    DestinationLoadB = DestLoadB,
                                    UnloadCycle = Cycle.Single
                                };
                                if (et.DestinationLoadA == et.DestinationLoadB)
                                {
                                    et.UnloadCycle = Cycle.Double;
                                }

                                ms.elevators.First(x => x.ElevatorName == side + aisle).ElevatorTasks.Add(et);
                            }
                            else
                            {
                                Log.Write(string.Format("Error: None of the tuIdents match any of the loads at the PS on StartTransportTelegram"), Color.Orange);
                            }
                        }
                        else if (psConv.LocationB.Active)
                        {            
                            if (ignoreTelegrams.Contains(caseLoad))
                            {
                                Log.Write($"{Name} Ignoring transport received at PS. Load B {caseLoad.TUIdent} received transport");
                                return;
                            }

                            if (psConv.LocationB.ActiveLoad != caseLoad)
                            {
                                Log.Write($"{Name}: start transport telegram ignored. Load at location B {psConv.LocationB.ActiveLoad.Identification} is not equal to load id in message {caseLoad.TUIdent}!");
                                return;
                            }

                            var caseB = psConv.LocationB.ActiveLoad as IATCCaseLoadType;
                            if (caseB != caseLoad)
                            {
                                Log.Write($"{Name} Ignoring transport received at PS. Load at B {caseB.TUIdent} is not equal to telegram ID: {caseLoad.TUIdent}");
                                return;
                            }

                            Log.Write($"{Name} single loads at PS. Load B {caseLoad.TUIdent} received transport");
                            UpDateLoadParameters(telegramFields, caseLoad);
                            SingleLoadAtPS(telegramFields, caseLoad);
                        }
                        else
                        {
                            Log.Write($"{Name}: start transport telegram ignored. No load at location B!");
                        }
                    }
                    else if (telegramFields.GetFieldValue(TelegramFields.source).LocationType() == LocationTypes.RackConv && telegramFields.GetFieldValue(TelegramFields.source).RackConvType() == RackConvTypes.Out)
                    {
                        UpDateLoadParameters(telegramFields, caseLoad);
                        LoadAtRackConv(telegramFields, caseLoad);
                    }
                    else if (telegramFields.GetFieldValue(TelegramFields.source).LocationType() == LocationTypes.RackConv && telegramFields.GetFieldValue(TelegramFields.source).RackConvType() == RackConvTypes.In &&
                            caseLoad.Location == telegramFields.GetFieldValue(TelegramFields.source)) //Mission for load at Infeed Rack conveyor
                    {
                        UpDateLoadParameters(telegramFields, caseLoad);
                        ShuttleTask st = new ShuttleTask();
                        string destination = caseLoad.Destination;
                        string source = telegramFields.GetFieldValue(TelegramFields.source);
                        string aisle = GetRackLocFields(source, PSDSRackLocFields.Aisle);
                        string side = GetRackLocFields(source, PSDSRackLocFields.Side);
                        int level;
                        int.TryParse(GetRackLocFields(source, PSDSRackLocFields.Level), out level); //assign out parameter

                        string location = string.Format("{0}{1}{2}IB", aisle, side, level.ToString().PadLeft(2, '0'));

                        MultiShuttle ms = GetMultishuttleFromAisleNum(location);

                        st.LoadID = caseLoad.Identification;
                        st.Source = location;
                        st.Level = level;

                        if (destination.LocationType() == LocationTypes.BinLocation)
                        {
                            st.Destination = DCIbinLocToMultiShuttleLoc(destination, out level, ms);
                        }
                        else if (destination.LocationType() == LocationTypes.RackConv && destination.RackConvType() == RackConvTypes.Out)
                        {
                            st.Destination = string.Format("{0}{1}{2}OA", aisle,
                                           GetRackLocFields(destination, PSDSRackLocFields.Side),
                                           level.ToString().PadLeft(2, '0'));
                        }
                        else
                        {
                            Log.Write("WARNING: Arrived at infeed rack and destination is NOT a binlocation or RackOut Conveyor.", Color.Red);
                            return;
                        }
                        ms.shuttlecars[level].ShuttleTasks.Add(st);

                    }
                    else
                    {
                        Log.Write(string.Format("{0}: MultishuttleStartTransportTelegram received but the load was found elsewhere in the model ({1}) message ignored", Name, caseLoad.Location), Color.Red);
                    }
                }
                //Anything FROM a binloc will not have a load until the shuttle gets to the bin location at this point it will be created.
                else if (telegramFields.GetFieldValue(TelegramFields.source).LocationType() == LocationTypes.BinLocation)
                {
                    CreateShuttleTask(telegramFields);
                }
                else if (telegramFields.GetFieldValue(TelegramFields.source).LocationType() == LocationTypes.BinLocation && telegramFields.GetFieldValue(TelegramFields.destination).LocationType() == LocationTypes.DS) //It's a shuffle move
                {
                    SingleLoadOut(telegramFields);
                }
                else
                {
                    Log.Write("Error: Load not found in StartTransportTelegramReceived", Color.Red);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString());
                if (ex.InnerException != null)
                {
                    Log.Write(ex.InnerException.ToString());
                }
            }
        }

        private void SecondCaseTimeout(Timer sender)
        {
            //Don't wait for a second transport at PS any more. Just pick the front load B and move load A to the front of PS.
            sender.OnElapsed -= SecondCaseTimeout;
            var telegramFields = sender.UserData as string[];
            var sourceLoc = telegramFields.GetFieldValue(TelegramFields.source);
            var aisle = GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Aisle);
            var side = GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Side);
            var psLevel = GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Level);
            var psA = string.Format("{0}{1}{2}{3}A", aisle, side, psLevel, GetPSDSLocFields(sourceLoc, PSDSRackLocFields.ConvType));
            var ms = GetMultishuttleFromAisleNum(psA);
            var psConv = ms.PickStationConveyors.Find(x => x.Name == string.Format("{0}{1}PS{2}", aisle, side, psLevel));
            //Get the case load from the telegram which was received 10 sec ago.
            var caseLoad = (IATCCaseLoadType)Case_Load.GetCaseFromIdentification(telegramFields.GetFieldValue(TelegramFields.tuIdent));

            if (caseLoad == null)
            {
                Log.Write($"{Name}: 10 second timeout failed. Could not find load B that triggered 10 sec timeout!");
                return;
            }

            if (psConv.TransportSection.Route.Loads.Count != 2)
            {
                Log.Write($"{Name}: 10 second timeout failed. PS does not contain 2 totes!");
                return;
            }
           
            if (!psConv.LocationA.Active)
            {
                Log.Write($"{Name}: 10 second timeout failed. No load at PS A.");
                return;
            }

            if (!psConv.LocationB.Active)
            {
                Log.Write($"{Name}: 10 second timeout failed. No load at PS B.");
                return;
            }

            var caseB = psConv.LocationB.ActiveLoad as IATCCaseLoadType;
            var caseA = psConv.LocationA.ActiveLoad as IATCCaseLoadType;
     
            if (caseLoad != caseB)
            {
                //Front load and "telegram load" does not match!
                Log.Write($"{Name}: 10 second timeout failed. Load at PS B {caseB.TUIdent} is not equal to load in timeout telegram {caseLoad.TUIdent} ");
                return;
            }

            if (!waitForSecondTransportAtPs.ContainsKey(caseA))
            {
                Log.Write($"{Name}: 10 second timeout failed. PS load A is not in the dictionary! Load at PS A {caseA.TUIdent}. Load at PS B {caseB.TUIdent} ");
                return;
            }

            if (waitForSecondTransportAtPs[caseA] != sender)
            {
                Log.Write($"{Name}: 10 second timeout failed. PS load A is not the one that started the timer! Load at PS A {caseA.TUIdent}. Load at PS B {caseB.TUIdent} ");
                return;
            }

            var elevator = ms.elevators.First(x => x.ElevatorName == side + aisle);
            var tasks = elevator.ElevatorTasks;
            if (tasks.Any(t => t.LoadB_ID == caseB.TUIdent) || (elevator.CurrentTask != null && elevator.CurrentTask.LoadB_ID == caseB.TUIdent))
            {
                Log.Write($"{Name}: Timeout for second tote at PS but elevator already ordered to pick only the front tote. Timeout ignored.");
                return;
            }

            Log.Write($"{Name}: 10 second timeout. No transport received for second tote {caseA.TUIdent}. Elevator will pick the front load {caseB.TUIdent}.");
            sender.Dispose();
            waitForSecondTransportAtPs.Remove(caseA);

            //Remove any PS timer (There shouldn't be any as we got a transport mission for case B!)
            foreach (Timer timer in RunningPSTimers)
            {
                IATCCaseLoadType LoadPosB = (IATCCaseLoadType)(((object[])timer.UserData)[0]);

                if (LoadPosB == caseB)
                {
                    RunningPSTimers.Remove(timer);
                    timer.Stop();
                    timer.OnElapsed -= PickStationResendTimer_OnElapsed;
                    timer.Dispose();
                    break;
                }
            }

            ignoreTelegrams.Add(caseA);
            SingleLoadAtPS(telegramFields, caseB);
        }

        private string FindLevelForReject(MultiShuttle ms)
        {
            string destLevel = "01";
            foreach (RackConveyor rackConveyor in ms.RackConveyors.Reverse<RackConveyor>())
            {
                if (rackConveyor.RackConveyorType == MultiShuttleDirections.Infeed)
                {
                    if (rackConveyor.TransportSection.Route.Loads.Count < 2)
                    {
                        destLevel = rackConveyor.Level.ToString("00");
                    }
                }
            }
            return destLevel;
        }

        private void LoadAtRackConv(string[] telegramFields, IATCCaseLoadType caseLoad)
        {
            string sourceLevel = BaseATCController.GetRackLocFields(caseLoad.Location, PSDSRackLocFields.Level);
            string destLevel = BaseATCController.GetPSDSLocFields(caseLoad.Destination, PSDSRackLocFields.Level);
            string aisle = BaseATCController.GetRackLocFields(caseLoad.Location, PSDSRackLocFields.Aisle);
            string side = BaseATCController.GetRackLocFields(caseLoad.Location, PSDSRackLocFields.Side);

            //Set somedata on the load
            caseLoad.Location = telegramFields.GetFieldValue(TelegramFields.source);
            caseLoad.Destination = telegramFields.GetFieldValue(TelegramFields.destination);

            int dropIndex = 0;
            if (int.TryParse(telegramFields.GetFieldValue(TelegramFields.dropIndex), out dropIndex))
            {
                caseLoad.DropIndex = dropIndex;
            }
            else
            {
                //Log.Write(string.Format("Did not manage to set the dropIndex {0}_{1}", telegramFields.GetFieldValue(TelegramFields.dropIndex), telegramFields.GetFieldValue(TelegramFields.tuIdent)));
            }

            //char side = (char)e._locationName.Side();
            // create an elevator task     
            ElevatorTask et;
            //Check if the message is to the drop station or to another Rack In Level
            string dest = telegramFields.GetFieldValue(TelegramFields.destination);
            if (dest.LocationType() == LocationTypes.RackConv) //Rack-In conveyor
            {
                et = new ElevatorTask(null, caseLoad.Identification)
                {
                    SourceLoadB = string.Format("{0}{1}{2}{3}B", aisle, side, sourceLevel, (char)ConveyorTypes.OutfeedRack),
                    DestinationLoadB = string.Format("{0}{1}{2}{3}B", aisle, side, GetLocFields(dest, PSDSRackLocFields.Level), GetLocFields(dest, PSDSRackLocFields.ConvType)), // aasyyxz: a=aisle, s = side, yy = level, x = input or output, Z = loc A or B e.g. 01R05OA 
                    DropIndexLoadB = dropIndex,
                    LoadCycle = Cycle.Single,
                    UnloadCycle = Cycle.Single,
                    Flow = TaskType.HouseKeep
                };
            }
            else
            {
                et = new ElevatorTask(null, caseLoad.Identification)
                {
                    SourceLoadB = string.Format("{0}{1}{2}{3}B", aisle, side, sourceLevel, (char)ConveyorTypes.OutfeedRack),
                    DestinationLoadB = string.Format("{0}{1}{2}{3}A", aisle, side, destLevel, (char)ConveyorTypes.Drop), // aasyyxz: a=aisle, s = side, yy = level, x = input or output, Z = loc A or B e.g. 01R05OA 
                    DropIndexLoadB = dropIndex,
                    LoadCycle = Cycle.Single,
                    UnloadCycle = Cycle.Single,
                    Flow = TaskType.Outfeed
                };
            }

            string elevatorName = string.Format("{0}{1}", side, aisle);
            MultiShuttle ms = GetMultishuttleFromAisleNum(aisle);
            Elevator elevator = ms.elevators.First(x => x.ElevatorName == elevatorName);
            elevator.ElevatorTasks.Add(et);
        }

        /// <summary>
        /// A single load out of a bin location to a drop station
        /// </summary>
        private void SingleLoadOut(string[] telegramFields)
        {
            CreateShuttleTask(telegramFields);
        }

        /// <summary>
        /// Shuffle Move
        /// </summary>
        /// <param name="telegramFields"></param>
        private void CreateShuttleTask(string[] telegramFields) //TODO should this go into the control object?
        {
            int level;
            string destination = string.Empty;
            string source = telegramFields.GetFieldValue("source"); //GetFieldValue by string not enum (faster)

            MultiShuttle ms = GetMultishuttleFromAisleNum(GetBinLocField(source, BinLocFields.Aisle));
            ShuttleTask st = new ShuttleTask();

            st.Source = DCIbinLocToMultiShuttleLoc(source, out level, ms);
            string tlgDest = telegramFields.GetFieldValue("destination");

            if (tlgDest.LocationType() == LocationTypes.BinLocation)//A shuffle move
            {
                destination = DCIbinLocToMultiShuttleLoc(tlgDest, out level, ms);
            }
            else if (tlgDest.LocationType() == LocationTypes.DS)//A move to a drop station
            {

                // takes the form aasyyxz: aa=aisle, s = side, yy = level, x = conv type see enum ConveyorTypes , Z = loc A or B e.g. 01R05OA
                destination = string.Format("{0}{1}{2}OA", GetPSDSLocFields(tlgDest, PSDSRackLocFields.Aisle),
                                                           GetPSDSLocFields(tlgDest, PSDSRackLocFields.Side),
                                                           level.ToString().PadLeft(2, '0'));
            }
            else if (tlgDest.LocationType() == LocationTypes.RackConv)
            {
                destination = DCIrackLocToMultiShuttleRackLoc(level, tlgDest);//Create the rack location destination for the shuttle task
            }

            st.Destination = destination;
            st.Level = level;
            st.LoadID = telegramFields.GetFieldValue("tuIdent");
            st.caseData = SetUpCaseMissionDataSet(telegramFields);
            ms.shuttlecars[level].ShuttleTasks.Add(st);
        }

        public static string DCIrackLocToMultiShuttleRackLoc(int level, string dest, string pos = "A")
        {
            // takes the form aasyyxz: aa=aisle, s = side, yy = level, x = conv type see enum ConveyorTypes , Z = loc A or B e.g. 01R05OA
            return string.Format("{0}{1}{2}O{3}", GetRackLocFields(dest, PSDSRackLocFields.Aisle),
                                                   GetRackLocFields(dest, PSDSRackLocFields.Side),
                                                   level.ToString().PadLeft(2, '0'),
                                                   pos);
        }

        public static string DCILocToMultiShuttleConvLoc(int level, string dest, string pos = "A")
        {
            // takes the form aasyyxz: aa=aisle, s = side, yy = level, x = conv type see enum ConveyorTypes , Z = loc A or B e.g. 01R05OA
            return string.Format("{0}{1}{2}O{3}", GetLocFields(dest, PSDSRackLocFields.Aisle),
                                                  GetLocFields(dest, PSDSRackLocFields.Side),
                                                  level.ToString().PadLeft(2, '0'),
                                                  pos);
        }

        /// <summary>
        /// Tasks are split into elevator task and shuttle task this method gives you the corrent rack location (i.e. the destination for an infeed elevator task)
        /// </summary>
        /// <param name="messageA">Can be either a full message or just the destination</param>
        /// <param name="multipal">We need to know if it is multipal as we need to know if we need to extract destinations or destination</param>
        /// <returns>A rack destination that the assembly will understand</returns>
        private string GetRackDestinationFromDCIBinLocation(string messageA, bool multipal = true)
        {
            string dest = null, source = null;

            if (multipal)
            {
                source = messageA.Split(',').GetFieldValue(TelegramFields.sources);
                dest = messageA.Split(',').GetFieldValue(TelegramFields.destinations);
            }
            else if (!multipal)
            {
                source = messageA.Split(',').GetFieldValue(TelegramFields.source);
                dest = messageA.Split(',').GetFieldValue(TelegramFields.destination);
            }

            //Need to make the intermediate position to the rack conveyor
            //takes the form aasyyxz: aa=aisle, s = side, yy = level, x = conv type see enum ConveyorTypes , Z = loc A or B e.g. 01R05OA
            //The actual final destination from the message is stored on a load parameter and this will be used when creating the shuttle task

            MultiShuttle ms = GetMultishuttleFromAisleNum(GetPSDSLocFields(source, PSDSRackLocFields.Aisle));
            string destinationLoc = string.Format("{0}{1}{2}IA", GetPSDSLocFields(source, PSDSRackLocFields.Aisle), GetPSDSLocFields(source, PSDSRackLocFields.Side), GetBinLocField(dest, BinLocFields.YLoc));

            DematicActionPoint ap = ms.ConveyorLocations.Find(x => x.LocName == destinationLoc);

            if (ap == null)
            {
                Log.Write("Can't find location in MHEController_Multishuttle.GetRackDestinationFromDCIBinLocation.", Color.Red);
                return null;
            }

            return destinationLoc;
        }

        /// <summary>
        /// Takes a dci bin location in the form MS011005080152 and converts it to a shuttletask location (sxxxyydd: Side, xxx location, yy = level, dd = depth)
        /// 
        /// </summary>
        public string DCIbinLocToMultiShuttleLoc(string dciLoc, out int _level, MultiShuttle ms)
        {
            string side = GetBinLocField(dciLoc, BinLocFields.Side);

            if (side == "1") { side = "L"; }
            else if (side == "2") { side = "R"; }

            string yLoc = GetBinLocField(dciLoc, BinLocFields.YLoc);
            int.TryParse(yLoc, out _level); //assign out parameter

            string xLoc = ((MHEControl_MultiShuttle)ms.ControllerProperties).XLocConverter(GetBinLocField(dciLoc, BinLocFields.XLoc), GetBinLocField(dciLoc, BinLocFields.RasterPos));

            // takes the form  sxxxyydd: Side, xxx location, yy = level, dd = depth
            return string.Format("{0}{1}{2}{3}", side,
                                                 xLoc,  //Need control object here to get properties of raster position and type 
                                                 yLoc,
                                                 GetBinLocField(dciLoc, BinLocFields.Depth));
        }


        //[BG] Has added to the base controller, made it indexable and changed the name to CreateATCCaseData

        /// <summary>
        /// Creates the ATC dataset that the load will hold
        /// </summary>
        private ATCCaseData SetUpCaseMissionDataSet(string[] telegramFields)
        {
            ATCCaseData cData = new ATCCaseData();
            float length, width, height, weight;

            float.TryParse(telegramFields.GetFieldValue("length"), out length);
            float.TryParse(telegramFields.GetFieldValue("width"), out width);
            float.TryParse(telegramFields.GetFieldValue("height"), out height);
            float.TryParse(telegramFields.GetFieldValue("weight"), out weight);

            // Only set dimension values if a real value is sent, otherwise maintain the default
            if (length > 0)
                cData.Length = length / 1000;
            if (length > 0)
                cData.Width = width / 1000;
            if (length > 0)
                cData.Height = height / 1000;
            if (length > 0)
                cData.Weight = weight;

            cData.colour = LoadColor(telegramFields.GetFieldValue("color"));

            cData.TUIdent = telegramFields.GetFieldValue("tuIdent");
            cData.TUType = telegramFields.GetFieldValue("tuType");
            cData.mts = telegramFields.GetFieldValue("mts");
            cData.presetStateCode = telegramFields.GetFieldValue("presetStateCode");
            cData.source = telegramFields.GetFieldValue("source");
            cData.destination = telegramFields.GetFieldValue("destination");

            return cData;
        }

        private void SingleLoadAtPS(string[] telegramFields, IATCCaseLoadType caseLoad)
        {
            // Rack Location for an ElevatorTask takes the form:  aasyyxz: aa=aisle, s = side, yy = level, x = conv type see enum ConveyorTypes , Z = loc A or B e.g. 01R05OA e.g. 01R05OA
            // Source location for a shuttleTask takes the form: sxxxyydd: Side, xxx location, yy = level, dd = depth
            // Elevator Format saa s = Side ( L or R), aa = aisle

            foreach (Timer timer in RunningPSTimers)
            {
                IATCCaseLoadType LoadPosB = (IATCCaseLoadType)(((object[])timer.UserData)[0]);

                if (LoadPosB == caseLoad)
                {
                    RunningPSTimers.Remove(timer);
                    timer.Stop();
                    timer.OnElapsed -= PickStationResendTimer_OnElapsed;
                    timer.Dispose();
                    break;
                }
            }

            string sourceLoc = telegramFields.GetFieldValue(TelegramFields.source);
            string destLoc = telegramFields.GetFieldValue(TelegramFields.destination);
            string aisle = GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Aisle);
            string side = GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Side);
            string dest = null;
            string sourceLoadB = string.Format("{0}{1}{2}{3}B", aisle,    //Single load will always be at B
                                                                side,
                                                                GetPSDSLocFields(sourceLoc, PSDSRackLocFields.Level),
                                                                GetPSDSLocFields(sourceLoc, PSDSRackLocFields.ConvType));

            MultiShuttle ms = GetMultishuttleFromAisleNum(sourceLoadB);

            if (telegramFields.GetFieldValue(TelegramFields.destination).LocationType() == LocationTypes.DS)
            {
                //First check if the destination is to a drop station... check if the drop station is on this elevator
                if (GetPSDSLocFields(destLoc, PSDSRackLocFields.Side) != side)
                {
                    //If not choose a destination to level 1 and remember the load route 
                    dest = string.Format("{0}{1}{2}{3}A", aisle, side, FindLevelForReject(ms), "I");
                }
                else
                {
                    dest = string.Format("{0}{1}{2}{3}A",
                                        aisle,
                                        side,
                                        GetPSDSLocFields(destLoc, PSDSRackLocFields.Level),
                                        GetPSDSLocFields(destLoc, PSDSRackLocFields.ConvType));
                }


            }
            else if (telegramFields.GetFieldValue(TelegramFields.destination).LocationType() == LocationTypes.RackConv)
            {
                dest = string.Format("{0}{1}{2}{3}B", aisle,
                                                      side,
                                                      GetLocFields(destLoc, PSDSRackLocFields.Level),
                                                      GetLocFields(destLoc, PSDSRackLocFields.ConvType));
            }
            else
            {
                dest = GetRackDestinationFromDCIBinLocation(string.Join(",", telegramFields), false);
            }


            ElevatorTask et = new ElevatorTask(null, caseLoad.TUIdent)
            {
                SourceLoadB = sourceLoadB,
                DestinationLoadB = dest,
                LoadCycle = Cycle.Single,
                UnloadCycle = Cycle.Single,
                Flow = TaskType.Infeed
            };

            ms.elevators.First(x => x.ElevatorName == side + aisle).ElevatorTasks.Add(et);
            //ms.elevators[side+aisle].ElevatorTasks.Add(et);
        }

        /// <summary>
        /// Uses a Mission Data Set to identify the correct aisle (multishuttle
        /// </summary>
        /// <param name="location">Telegram in the form of the Mission Data Set</param>
        /// <returns>A multishuttle that is controlled by this controller</returns>
        private MultiShuttle GetMultishuttleFromAisleNum(string location)
        {
            int aNum;
            int.TryParse(location.Substring(0, 2), out aNum);
            if (aNum == 0) return null;

            var aisleNum = multishuttles.Where(x => x.AisleNumber == aNum);
            if (aisleNum.Count() > 1)
            {
                Log.Write("ERROR: List of multishuttles [Experior.Catalog.Dematic.ATC.Assemblies.Storage.MHEController_Multishuttle.multishuttles] do not have unique aisle numbers.", Color.Red);
                Log.Write("List of multishuttles accessed in Experior.Catalog.Dematic.ATC.Assemblies.Storage.MHEController_Multishuttle.GetMultishuttleFromAisleNum().", Color.Red);
                return null;
            }
            else if (aisleNum.Any())
            {
                return aisleNum.First();
            }
            else
            {
                Log.Write("ERROR: Multishuttle not found in list of multishuttles [Experior.Catalog.Dematic.ATC.Assemblies.Storage.MHEController_Multishuttle.multishuttles].", Color.Red);
                Log.Write("Maybe aisle number from message does not match avaiable aisle numbers in avaiable multishuttles.", Color.Red);
            }

            return null;
        }

        public MHEControl CreateMHEControl(IControllable assem, ProtocolInfo info)
        {
            multishuttles.Add(assem as MultiShuttle);

            MHEControl protocolConfig = null;  //generic plc config object

            if (assem is MultiShuttle)
            {
                protocolConfig = CreateMHEControlGeneric<MultiShuttleATCInfo, MHEControl_MultiShuttle>(assem, info);
            }
            else
            {
                Experior.Core.Environment.Log.Write("Can't create MHE Control, object is not defined in the 'CreateMHEControl' of the controller", Color.Red);
                return null;
            }
            //......other assemblies should be added here....do this with generics...correction better to do this with reflection...That is BaseController should use reflection
            //and not generics as we do not know the types at design time and it means that the above always has to be edited when adding a new MHE control object.
            protocolConfig.ParentAssembly = (Assembly)assem;
            controls.Add(protocolConfig);
            control = protocolConfig as MHEControl_MultiShuttle;
            return protocolConfig as MHEControl;
        }

        public void RemoveSSCCBarcode(string ULID)
        {
            throw new NotImplementedException();
        }

        public BaseCaseData GetCaseData()
        {
            return new ATCCaseData();
        }
    }

    [Serializable]
    [TypeConverter(typeof(MHEController_MultishuttleATCInfo))]
    public class MHEController_MultishuttleATCInfo : BaseATCControllerInfo
    {
    }
}
