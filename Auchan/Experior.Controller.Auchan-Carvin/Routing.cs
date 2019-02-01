using System.Collections.Generic;
using Experior.Core.Loads;
using Experior.Core.Routes;
using Experior.Dematic.Base;
using Experior.Catalog.Dematic.Pallet.Assemblies;
using Experior.Catalog.Dematic.ATC.Assemblies;
using Experior.Catalog.Dematic.ATC;
using Experior.Catalog.Dematic.Case.Components;
using Experior.Catalog.Dematic.ATC.Assemblies.CaseConveyor;
using Dematic.ATC;
using Experior.Catalog.Dematic.Case.Devices;
using Experior.Dematic.Base.Devices;
using Experior.Core;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Experior.Catalog;
using Experior.Catalog.Dematic.Pallet;
using System.Reflection;
using Experior.Catalog.Dematic.Pallet.Devices;
using Experior.Catalog.Dematic.ATC.Assemblies.PalletConveyor;
using Experior.Catalog.Dematic.Storage.MultiShuttle.Assemblies;

namespace Experior.Controller.AuchanCarvin
{
    public partial class AuchanCarvinRouting : Experior.Catalog.ControllerExtended
    {
        private const string LiftNamePrefix = "PCPA01IS0";
        private const string TUDRIdentPrefix = "CP";

        private int counter = 0;
        private bool a10DAStraight = true;
        private bool initialising = false;
        private MHEController_Case plc11 = Core.Assemblies.Assembly.Items["CC01"] as MHEController_Case;
        private MHEController_Case plc12 = Core.Assemblies.Assembly.Items["CC02"] as MHEController_Case;
        private MHEController_Pallet plc02 = Core.Assemblies.Assembly.Items["PC02"] as MHEController_Pallet;
        private EmulationATC atc = Core.Assemblies.Assembly.Items["ATC 1"] as EmulationATC;
        private PalletStraight palletStackBuffer = Core.Assemblies.Assembly.Items["PALLETSTACKBUFFER"] as PalletStraight;
        private StraightAccumulationConveyor stackBuffer = Core.Assemblies.Assembly.Items["TRAYSTACKBUFFER"] as StraightAccumulationConveyor;
        private List<MergeDivertConveyor> stackReturnPoints = new List<MergeDivertConveyor>();
        private Dictionary<string, bool> stackReturnAvailable = new Dictionary<string, bool>();
        private List<ATCTray> startTelegrams = new List<ATCTray>();
        private Random random = new Random();
        private Dictionary<string, bool> pecWorkAround = new Dictionary<string, bool>();
        private Dictionary<string, int> palletising = new Dictionary<string, int>();

        private Dictionary<string, StraightConveyor> Pushers = new Dictionary<string, StraightConveyor>();
        private Dictionary<string, List<IATCCaseLoadType>> PusherActiveLoads = new Dictionary<string, List<IATCCaseLoadType>>();
        private Dictionary<string, bool> PusherRelaseMode = new Dictionary<string, bool>();
        private Dictionary<string, bool> PusherWaitingMode = new Dictionary<string, bool>();
        private Dictionary<string, Timer> PusherPushTimer = new Dictionary<string, Timer>();
        private Dictionary<string, Timer> PusherLoadTimer = new Dictionary<string, Timer>();
        private Dictionary<string, IATCCaseLoadType> PusherLoadWaiting = new Dictionary<string, IATCCaseLoadType>();
        private Dictionary<string, int> PusherCurrentGroup = new Dictionary<string, int>();
        private Dictionary<string, int> PusherCurrentPallet = new Dictionary<string, int>();
        private Dictionary<string, bool> PushAnything = new Dictionary<string, bool>();
        private Dictionary<string, string> PusherGroupEnd = new Dictionary<string, string>();
        private Dictionary<string, string> PusherPalEnd = new Dictionary<string, string>();

        private Dictionary<string, Timer> PalletiserResendTimers = new Dictionary<string, Timer>();
        private Dictionary<string, string> PalletiserResendMessages = new Dictionary<string, string>();

        private List<string> PushersWaiting = new List<string>();
        private List<string> LiftsWaiting = new List<string>();

        private Dictionary<Load, int> StackDestination = new Dictionary<Load, int>();
        private int stackLineRoundRobin;
        private Dictionary<int, int> StackLineBufferCount = new Dictionary<int, int>();
        private Dictionary<string, tCarDest> tCarDestinations = new Dictionary<string, tCarDest>();

        private Regex regexCCPA00WS01 = new Regex(@"^CCPA0[1-8]WS01$"); // eg. CCPA01WS01, CCPA02WS01, CCPA03WS01
        private Regex regexCCPA00LU00 = new Regex(@"^CCPA0[1-8]LU0[1|2]$"); // eg. CCPA01LU01, CCPA01LU02 - CCPA08LU01, CCPA08LU02
        private Regex regexPA00WS01   = new Regex(@"^PA0[1-8]WS01$");       // eg. PA01WS01 - PA08WS01
        private Regex regexCCDE00LU00 = new Regex(@"^CCDE0[1-8]LU0[1|2]$"); // eg. CCDE01LU01, CCDE01LU02 - CCDE08LU01, CCDE08LU02
        private Regex regexCCDE00WS00 = new Regex(@"^CCDE0[1-8]WS0[1|2]$"); // eg. CCDE01WS01, CCDE01WS02 - CCDE08WS01, CCDE08WS02
        private Regex regexCCDE00CC00 = new Regex(@"^CCDE0[1-8]CC0[1|2]$"); // eg. CCDE01CC01, CCDE01CC02 - CCDE08CC01, CCDE08CC02
        private Regex regexPCPA01IS00 = new Regex(@"^PCPA01IS0[1-8]$"); // eg. PCPA01IS01, PCPA01IS02, PCPA01IS03, ...

        // Timers for resending TUDR
        private Dictionary<string, Timer> DropStationTimer = new Dictionary<string, Timer>();
        private Timer stackBufferCheck = new Timer(10); //10s for now

        //Tray stack return
        string lastRouting = "";
        Timer stackTimer = new Timer(1.5f);
        CasePhotocell stackFullPhotocell;
        Load stackFullLoad;

        public AuchanCarvinRouting() : base("AuchanCarvinRouting")
        {
            StandardConstructor();
            Lift.OnLiftRaisedStatic += Lift_OnLiftRaised;
            LiftTable.OnLoadOnDivertPoint += LiftTable_OnDivertPoint; 
            BaseATCController.OnOverrideTelegramReceived += BaseATCController_OnOverrideTelegramReceived;
            MergeDivertConveyor.OnDivertPoint += MergeDivertConveyor_OnDivertPoint;
            CasePhotocell.OnPhotocellStatusChangedRoutingScript += CasePhotocell_OnPhotocellStatusChanged;
            PalletPhotocell.OnPhotocellStatusChangedRoutingScript += PalletPhotocell_OnPhotocellStatusChanged;
            plc11.ControllerConnection.OnConnected += ControllerConnection_OnConnected;

            GetStackReturnPoints();

            ResetStackBufferCount();

            Core.Environment.Scene.OnLoaded += StartSystem;
            Core.Environment.Scene.OnResetCompleted += StartSystem;

            InitialisePushers();

            InitialiseDropStationTimers();

            InitialisePalletiserResend();

            tCarDestinations.Add("D1", new tCarDest((PalletStraight)Core.Assemblies.Assembly.Items["B02AA"]));
            tCarDestinations.Add("D2", new tCarDest((PalletStraight)Core.Assemblies.Assembly.Items["B04AA"]));
            tCarDestinations.Add("D3", new tCarDest((PalletStraight)Core.Assemblies.Assembly.Items["B06AA"]));
            tCarDestinations.Add("D4", new tCarDest((PalletStraight)Core.Assemblies.Assembly.Items["B08AA"]));

            palletStackBuffer.ThisRouteStatus.OnRouteStatusChanged += PalletStackBuffer_OnRouteStatusChanged;
            TCar.OnSourceLoadArrived += TCar_OnSourceLoadArrived;
            TCar.OnDestinationStatusChanged += TCar_OnDestinationStatusChanged;

            pecWorkAround.Add("PCPA01SP01", false);
            pecWorkAround.Add("PCPA01SP02", false);
            pecWorkAround.Add("PCPA01SP03", false);

            //Modifications to the multishuttle because a madman made the levels start at 4 not 1? WTF!!!!

            for (int i = 1; i < 9; i++)
            {
                MultiShuttle ms = Core.Assemblies.Assembly.Items[string.Format("Multi-Shuttle {0}", i.ToString())] as MultiShuttle;

                for (int s = 1; s < 4; s++)
                {
                    ms.shuttlecars[s].Vehicle.Visible = false;
                    ms.RackConveyors.Find(x => x.Name == string.Format("{0}R{1}O", i.ToString("00"), s.ToString("00"))).Visible = false;
                    ms.RackConveyors.Find(x => x.Name == string.Format("{0}L{1}O", i.ToString("00"), s.ToString("00"))).Visible = false;
                    ms.RackConveyors.Find(x => x.Name == string.Format("{0}R{1}I", i.ToString("00"), s.ToString("00"))).Visible = false;
                    ms.RackConveyors.Find(x => x.Name == string.Format("{0}L{1}I", i.ToString("00"), s.ToString("00"))).Visible = false;
                }
            }
            InitialisePalleting();

            stackTimer.OnElapsed += StackTimer_OnElapsed;

            stackBufferCheck.OnElapsed += StackBufferCheck_OnElapsed;
        }

        private void StackBufferCheck_OnElapsed(Timer sender)
        {
            stackBufferCheck.Stop();
            stackBufferCheck.Reset();
            stackBufferCheck.Timeout = localProperties._StackBufferTime;
            stackBufferCheck.Start();

            if (!initialising && plc11.plcConnected == true)
            {
                plc11.SendUnitFillingTelegram("CCDE01LO01", stackBuffer.LoadCount.ToString(), "28");
            }
        }

        public string GetPalletisingSortId(string key)
        {
            if(palletising.ContainsKey(key))
            {
                return palletising[key].ToString().PadLeft(6, '0');
            }
            return null;
        }

        private void AuchanCarvinRouting_OnElapsed(Timer sender)
        {
            //Resend the message

            plc02.SendTelegram(PalletiserResendMessages[string.Format("PCPA01IS0{0}", sender.UserData.ToString())], true);

            //ReStart the timer
            sender.Reset();
            sender.Start();
        }

        public void EndPalletOperation(string liftNumber)
        {
            // Get Lift
            Lift lift = Core.Assemblies.Assembly.Items[LiftNamePrefix + liftNumber] as Lift;
            if (lift != null && lift.Raised)
            {
                // Increment counter
                counter++;

                // Get pallet on the lift and add load
                IATCPalletLoadType atcPalletLoad = GetLiftLoad(lift);
                if (atcPalletLoad != null && plc02 != null)
                {
                    atcPalletLoad.AddLoad(0.9f, 1.6f, 1.3f);
                    atcPalletLoad.LoadWaitingForWCS = true;
                    // Lower the lift and wait for TUMI
                    lift.LowerLift();
                    // Set Ident and Location as expected by WMS
                    atcPalletLoad.TUIdent = TUDRIdentPrefix + AddLeadingZeros(8, counter);  // This identifier will be replaced by the TU identifier of TUMI telegram sent by WCS 
                    atcPalletLoad.Location = LiftNamePrefix + liftNumber;
                    atcPalletLoad.Source = LiftNamePrefix + liftNumber;
                    // Send Transport Request Telegram (TUDR)
                    string pall = string.Format("PA0{0}WS01", liftNumber);
                    string sortID = GetPalletisingSortId(pall) ?? "0";
                    string telegram = plc02.CreateTelegramFromLoad(TelegramTypes.TransportRequestTelegram, atcPalletLoad);
                    telegram = telegram.InsertField("sortLaneListName", sortID);
                    plc02.SendTelegram(telegram, true);

                    //Remember the message and Start the timer
                    string liftName = string.Format("PCPA01IS0{0}", liftNumber);
                    PalletiserResendMessages[liftName] = telegram;
                    PalletiserResendTimers[liftName].Reset();
                    PalletiserResendTimers[liftName].Start();
                }
            }
            if (lift != null && !lift.Raised)
            {
                // Add lift to list of waiting lifts so that when the lift is ready it can be checked and the operation completed
                LiftsWaiting.Add(liftNumber);
            }
        }

        private string AddLeadingZeros(int lengthRequired, int counter)
        {
            var output = "";
            var padRequired = lengthRequired - counter.ToString().Length;
            output = counter.ToString().PadLeft(padRequired, '0');
            return output;
        }

        private void Lift_OnLiftRaised(Lift sender, Load load)
        {
            load.Stop();
            // If the lift is waiting to end pallet operation then complete
            var liftNumber = sender.Name.Substring(9, 1);
            if (LiftsWaiting.Contains(liftNumber))
            {
                EndPalletOperation(liftNumber);
                LiftsWaiting.Remove(liftNumber);
            }
            ForcePalletising(liftNumber);
        }

        public void ForcePalletising(string liftNumber)
        {
            // Reset the SortId for ErgoPal so that loads that follow can proceed
            string palletisingKey = string.Format("PA0{0}WS01", liftNumber);
            palletising[palletisingKey] = 0;
            // If there are next sequenced loads stopped on outfeed then start them
            StraightBeltConveyor outfeed = Core.Assemblies.Assembly.Items["ErgoPal" + liftNumber] as StraightBeltConveyor;
            if (outfeed != null && outfeed.TransportSection.Route.Loads.Count > 0)
            {
                IEnumerable<Load> stoppedLoads = outfeed.TransportSection.Route.Loads.Where(x => x.Stopped);
                foreach (Load stoppedLoad in stoppedLoads)
                {
                    var telegramLoad = (IATCCaseLoadType)stoppedLoad;
                    telegramLoad.Location = string.Format("CCPA0{0}WS01", liftNumber);
                    plc12.SendTransportFinishedTelegram(telegramLoad);
                    stoppedLoad.Release();
                }
            }       
        }

        private void StartSystem()
        {
            initialising = true;

            ResetStackBufferCount();

            StraightConveyor trayStackBuffer = Core.Assemblies.Assembly.Items["TRAYSTACKBUFFER"] as StraightConveyor;
            for (int i = 1; i < 239; i++)
            {
                trayStackBuffer.DoubleClick();
            }

            // Fill buffer lines with stacks to begin with
            for (int i = 1; i < 17; i++)
            {
                palletStackBuffer.DoubleClick();
            }

            Timer startTimer = new Timer(480);
            startTimer.Start();
            startTimer.OnElapsed += StartTimer_OnElapsed;

            Core.Environment.Scene.Continue();
            Core.Environment.Time.ContinuouslyRunning = true;
            Core.Environment.Time.Scale = 1000;

            ResetPushers();

            ResetDropStationTimers();

            ResetPalletising();

            stackTimer.Reset();
            stackTimer.Start();

            stackBufferCheck.Reset();
            stackBufferCheck.Start();

            foreach (var position in tCarDestinations.Values)
            {
                position.inTransit = false;
            }
        }

        private void InitialisePalleting()
        {
            for (int i = 1; i < 9; i++)
            {
                string pall = string.Format("PA{0}WS01", i.ToString("00"));
                palletising.Add(pall, 0);
            }
        }

        private void ResetPalletising()
        {
            // Reset dictionary used for lifts
            foreach (string item in palletising.Keys.ToList())
            {
                palletising[item] = 0;
            }
            // Reset list used for lifts
            LiftsWaiting.Clear();
            // Reset lift counter
            counter = 0;
        }

        private void StartTimer_OnElapsed(Timer sender)
        {
            sender.OnElapsed -= StartTimer_OnElapsed;
            Core.Environment.Time.Scale = 1;
            initialising = false;
        }

        private void InitialisePushers()
        {
            for (int i = 1; i < 17; i++) //Increase as pushers are added to the model
            {
                string pusherName = string.Format("PUSHER{0}", i.ToString().PadLeft(2, '0'));
                Pushers.Add(pusherName, Core.Assemblies.Assembly.Items[pusherName] as StraightConveyor);
                PusherActiveLoads.Add(pusherName, new List<IATCCaseLoadType>());
                PusherRelaseMode.Add(pusherName, false);
                PusherWaitingMode.Add(pusherName, false);
                PusherPushTimer.Add(pusherName, new Timer(5));
                PusherLoadTimer.Add(pusherName, new Timer(15));
                PusherLoadWaiting.Add(pusherName, null);

                PusherPushTimer[pusherName].UserData = pusherName;
                PusherLoadTimer[pusherName].UserData = pusherName;

                PusherPushTimer[pusherName].OnElapsed += PusherPushTimer_OnElapsed;
                PusherLoadTimer[pusherName].OnElapsed += PusherLoadTimer_OnElapsed;
            }

            for (int i = 1; i < 17; i+=2)
            {
                string pusherName = string.Format("PUSHED{0}{1}", i.ToString().PadLeft(2, '0'), (i+1).ToString().PadLeft(2, '0'));
                PusherCurrentGroup.Add(pusherName, 0);
                PusherCurrentPallet.Add(pusherName, 0);
                PusherGroupEnd.Add(pusherName, "E");
                PusherPalEnd.Add(pusherName, "E");
                PushAnything.Add(pusherName, false);
            }
        }

        private void ClearPushers()
        {
            for (int i = 1; i < 17; i++) 
            {
                string pusherName = string.Format("PUSHER{0}", i.ToString().PadLeft(2, '0'));
                PusherPushTimer[pusherName].OnElapsed -= PusherPushTimer_OnElapsed;
                PusherPushTimer[pusherName].Dispose();
                PusherLoadTimer[pusherName].OnElapsed -= PusherLoadTimer_OnElapsed;
                PusherLoadTimer[pusherName].Dispose();
            }
            PusherLoadTimer.Clear();
            Pushers.Clear();
            PusherActiveLoads.Clear();
            PusherRelaseMode.Clear();
            PusherWaitingMode.Clear();
            PusherPushTimer.Clear();
            PusherLoadWaiting.Clear();
        }

        private void ResetPushers()
        {
            foreach (string pusher in Pushers.Keys)
            {
                PusherActiveLoads[pusher].Clear();
                PusherRelaseMode[pusher] = false;
                PusherWaitingMode[pusher] = false;
                PusherPushTimer[pusher].Reset();
                PusherLoadTimer[pusher].Reset();
                PusherLoadWaiting[pusher] = null;
                PushersWaiting.Clear();
                LiftsWaiting.Clear();
            }

            for (int i = 1; i < 17; i += 2)
            {
                string pusherName = string.Format("PUSHED{0}{1}", i.ToString().PadLeft(2, '0'), (i + 1).ToString().PadLeft(2, '0'));
                PusherCurrentGroup[pusherName] = 0;
                PusherCurrentPallet[pusherName] = 0;
                PusherGroupEnd[pusherName] = "E";
                PusherPalEnd[pusherName] = "E";
                PushAnything[pusherName] = false;
            }
        }
        private string OtherPusher(string pusher)
        {
            //Returns the name of the other pusher associated with this pusher
            switch (pusher.Substring(6, 2))
            {
                case "01": return "PUSHER02";
                case "02": return "PUSHER01";
                case "03": return "PUSHER04";
                case "04": return "PUSHER03";
                case "05": return "PUSHER06";
                case "06": return "PUSHER05";
                case "07": return "PUSHER08";
                case "08": return "PUSHER07";
                case "09": return "PUSHER10";
                case "10": return "PUSHER09";
                case "11": return "PUSHER12";
                case "12": return "PUSHER11";
                case "13": return "PUSHER14";
                case "14": return "PUSHER13";
                case "15": return "PUSHER16";
                case "16": return "PUSHER15";
            }
            return null;
        }

        private string PusherToCommPoint(string pusher)
        {
            switch (pusher)
            {
                case "PUSHER01": return "CCPA01LU01";
                case "PUSHER02": return "CCPA01LU02";
                case "PUSHER03": return "CCPA02LU01";
                case "PUSHER04": return "CCPA02LU02";
                case "PUSHER05": return "CCPA03LU01";
                case "PUSHER06": return "CCPA03LU02";
                case "PUSHER07": return "CCPA04LU01";
                case "PUSHER08": return "CCPA04LU02";
                case "PUSHER09": return "CCPA05LU01";
                case "PUSHER10": return "CCPA05LU02";
                case "PUSHER11": return "CCPA06LU01";
                case "PUSHER12": return "CCPA06LU02";
                case "PUSHER13": return "CCPA07LU01";
                case "PUSHER14": return "CCPA07LU02";
                case "PUSHER15": return "CCPA08LU01";
                case "PUSHER16": return "CCPA09LU02";
            }
            return null;
        }

        private string PushTooPoint(string pusher)
        {
            //Returns the name of the conveyor in the middle of the two pushers
            switch (pusher.Substring(6, 2))
            {
                case "01": return "PUSHED0102";
                case "02": return "PUSHED0102";
                case "03": return "PUSHED0304";
                case "04": return "PUSHED0304";
                case "05": return "PUSHED0506";
                case "06": return "PUSHED0506";
                case "07": return "PUSHED0708";
                case "08": return "PUSHED0708";
                case "09": return "PUSHED0910";
                case "10": return "PUSHED0910";
                case "11": return "PUSHED1112";
                case "12": return "PUSHED1112";
                case "13": return "PUSHED1314";
                case "14": return "PUSHED1314";
                case "15": return "PUSHED1516";
                case "16": return "PUSHED1516";
            }
            return null;

        }

        private string PushTooToAisle(string PushToo)
        {
            switch (PushToo)
            {
                case "PUSHED0102": return "Aisle 1";
                case "PUSHED0304": return "Aisle 2";
                case "PUSHED0506": return "Aisle 3";
                case "PUSHED0708": return "Aisle 4";
                case "PUSHED0910": return "Aisle 5";
                case "PUSHED1112": return "Aisle 6";
                case "PUSHED1314": return "Aisle 7";
                case "PUSHED1516": return "Aisle 8";
            }
            return null;
        }

        private void InitialiseDropStationTimers()
        {
            var commPoints = new string[] {
                "CCPA01LU01", "CCPA01LU02",
                "CCPA02LU01", "CCPA02LU02",
                "CCPA03LU01", "CCPA03LU02",
                "CCPA04LU01", "CCPA04LU02",
                "CCPA05LU01", "CCPA05LU02",
                "CCPA06LU01", "CCPA06LU02",
                "CCPA07LU01", "CCPA07LU02",
                "CCPA08LU01", "CCPA08LU02"
            };
            foreach (var commPoint in commPoints)
            {
                DropStationTimer.Add(commPoint, new Timer(180));
                DropStationTimer[commPoint].OnElapsed += DropStationTimer_OnElapsed;
            }
        }

        private void InitialisePalletiserResend()
        {
            for (int i = 1; i < 9; i++)
            {
                string loc = string.Format("PCPA01IS0{0}", i);
                PalletiserResendTimers.Add(loc, new Timer(30));
                PalletiserResendTimers[loc].UserData = i.ToString();
                PalletiserResendTimers[loc].AutoReset = false;
                PalletiserResendTimers[loc].OnElapsed += AuchanCarvinRouting_OnElapsed;

                PalletiserResendMessages.Add(loc, "");
            }
        }

        private void ResetDropStationTimers()
        {
            foreach (string timer in DropStationTimer.Keys)
            {
                DropStationTimer[timer].Reset();
            }
        }

        private void ClearDropStationTimers()
        {
            foreach (string timer in DropStationTimer.Keys)
            {
                DropStationTimer[timer].OnElapsed -= DropStationTimer_OnElapsed;
                DropStationTimer[timer].Dispose();
            }
            DropStationTimer.Clear();
        }

        #region Pallet Conveyor
        private void LiftTable_OnDivertPoint(LiftTable sender, Load load)
        {
            ATCEuroPallet pallet = load as ATCEuroPallet;

            if (sender.Name == "B10BA" || sender.Name == "B10CA")
            {
                load.Stop();
                LiftTable b10BA = Core.Assemblies.Assembly.Items["B10BA"] as LiftTable;
                LiftTable b10CA = Core.Assemblies.Assembly.Items["B10CA"] as LiftTable;
                LiftTable b10DA = Core.Assemblies.Assembly.Items["B10DA"] as LiftTable;
                List<string> b10BAConveyors = new List<string>() { "B10BB", "B10BC", "B10BD", "PCPA01SP01" };
                List<string> b10CAConveyors = new List<string>() { "B10CB", "B10CC", "B10CD", "PCPA01SP02" };
                List<string> b10DAConveyors = new List<string>() { "B10DB", "B10DC", "B10DD", "PCPA01SP03" };
                var b10BAConveyorsCount = LoadsOnConveyors(b10BAConveyors);
                var b10CAConveyorsCount = LoadsOnConveyors(b10CAConveyors);
                var b10DAConveyorsCount = LoadsOnConveyors(b10DAConveyors);
                if (sender.Name == "B10BA") // First divert point
                {
                    if (b10BAConveyorsCount < 4)
                    {
                        sender.RouteLoad(load, new List<Direction>() { Direction.Straight }, false);
                    }
                    else
                    {
                        if (b10CAConveyorsCount == 4 && b10CA.LoadCount > 0)
                        {
                            sender.RouteLoad(load, new List<Direction>() { Direction.Left, Direction.Straight }, false);
                        }
                        else
                        {
                            sender.RouteLoad(load, new List<Direction>() { Direction.Left }, false);
                        }
                    }
                }
                if (sender.Name == "B10CA") // Second divert point
                {
                    if (b10CAConveyorsCount < 4)
                    {
                        sender.RouteLoad(load, new List<Direction>() { Direction.Straight }, false);
                    }
                    else
                    {
                        if (b10DAConveyorsCount == 4 && b10DA.LoadCount > 0)
                        {
                            sender.RouteLoad(load, new List<Direction>() { Direction.Left, Direction.Straight }, false);
                        }
                        else
                        {
                            sender.RouteLoad(load, new List<Direction>() { Direction.Left }, false);
                        }
                    }
                }
            }
            
            if (sender.Name == "A10DA")
            {
                load.Stop();
                Stacker stackerA10DC = Core.Assemblies.Assembly.Items["A10DC"] as Stacker;
                if (a10DAStraight)
                {
                    if (stackerA10DC.LoadCount > 0)
                    {
                        sender.RouteLoad(load, new List<Direction>() { Direction.Left, Direction.Straight }, false);
                    }
                    else
                    {
                        sender.RouteLoad(load, new List<Direction>() { Direction.Straight }, false);
                    }
                    a10DAStraight = false; // alternate the load between the two aisles using this flag
                }
                else
                {
                    sender.RouteLoad(load, new List<Direction>() { Direction.Left }, false);
                    a10DAStraight = true; // alternate the load between the two aisles using this flag
                }
            }
        }

        private int LoadsOnConveyors(List<string> conveyors)
        {
            var count = 0;
            foreach (var item in conveyors)
            {
                PalletStraight conveyor = Core.Assemblies.Assembly.Items[item] as PalletStraight;
                if (conveyor.LoadCount > 0)
                {
                    count++;
                }
            }
            return count;
        }

        private IATCPalletLoadType GetLiftLoad(Lift lift)
        {
            IATCPalletLoadType atcPalletLoad = null;
            if (lift.LiftConveyor.TransportSection.Route.Loads.Count > 0)
            {
                atcPalletLoad = (IATCPalletLoadType)lift.LiftConveyor.TransportSection.Route.Loads.First();
            }
            return atcPalletLoad;
        }

        #endregion

        #region Pallet Conveyor

        private void PalletStackBuffer_OnRouteStatusChanged(object sender, RouteStatusChangedEventArgs e)
        {
            // Add new stack soon as the stack buffer becomes available
            if (e._available == RouteStatuses.Available)
            {
                palletStackBuffer.DoubleClick();
            }
        }

        private void TCar_OnSourceLoadArrived(TCar sender, Load load, DematicFixPoint sourceFixPoint)
        {
            if (sender.Name == "B01CA")
            {
                IATCPalletLoadType atcLoad = (IATCPalletLoadType)load;
                atcLoad.LoadWaitingForPLC = true;
                TCarTaskCheck(sender, sourceFixPoint, atcLoad);
            }
        }

        private void TCar_OnDestinationStatusChanged(TCar sender, RouteStatus routeStatus)
        {
            if (sender.Name == "B01CA")
            {
                if (routeStatus.Available == RouteStatuses.Available)
                {
                    //Need to find the front position on the tcar
                    LiftTable conveyor = (LiftTable)sender.SourceFixPoints["S1"].Attached.Parent;

                    if (conveyor.LoadActive && conveyor.TransportSection.Route.Loads.Count > 0)
                    {
                        IATCPalletLoadType load = (IATCPalletLoadType)conveyor.TransportSection.Route.Loads.ToList()[0];
                        if (load.LoadWaitingForPLC)
                        {
                            TCarTaskCheck(sender, sender.SourceFixPoints["S1"], load);
                        }
                    }
                }
            }
        }

        private void TCarTaskCheck(TCar sender, DematicFixPoint sourceFixPoint, IATCPalletLoadType load)
        {
            for (int i = 1; i < 5; i++)
            {
                string position = string.Format("D{0}", i.ToString());
                if (tCarDestinations[position].Conveyor.RouteAvailable == RouteStatuses.Available && !tCarDestinations[position].inTransit)
                {
                    TCarTask task = new TCarTask
                    {
                        Source = sourceFixPoint,
                        Destination = GetFixPoint(sender, position)
                    };
                    load.LoadWaitingForPLC = false;

                    sender.Tasks.Add(task);
                    tCarDestinations[position].inTransit = true;

                    break;
                }
            }
        }

        private DematicFixPoint GetFixPoint(TCar sender, string fpName)
        {
            return (DematicFixPoint)sender.FixPoints.Find(x => x.Name == fpName);
        }


        #endregion

        #region Case Conveyor
        private void ControllerConnection_OnConnected(Core.Communication.Connection connection)
        {
            //Send telegrams for all loads in the depal positions once PLC11 is connected
            foreach (ATCTray atcLoad in startTelegrams)
            {
                plc11.SendTransportRequestTelegram(atcLoad);
            }
            startTelegrams.Clear();
        }

        protected override void Arriving(INode node, Load load)
        {
            Match matchCCPA00LU00 = regexCCPA00LU00.Match(node.Name);
            Match matchCCDE00CC00 = regexCCDE00CC00.Match(node.Name);
            Match matchCCPA00WS01 = regexCCPA00WS01.Match(node.Name);
            if (matchCCDE00CC00.Success)
            {
                IATCCaseLoadType atcLoad = load as IATCCaseLoadType;
                atcLoad.Location = node.Name;
                
                if (!IsWeightException(atcLoad)) //Weight out of tolerance
                {
                    //Sunny Day
                    plc11.SendLocationArrivedTelegram((IATCCaseLoadType)load, (atcLoad.CaseWeight * 1000).ToString()); //Send TUNO
                }
            }
            else if (node.Name == "SEQTEST")
            {
                //Check the sequence coming out of the DMS
                Log.Write(string.Format("Sequence: {0}  Load: {1}", ((IATCCaseLoadType)load).DropIndex, ((IATCCaseLoadType)load).TUIdent));
            }
            else if (matchCCPA00LU00.Success)
            {
                // Send the first TUDR and wait for TUMI
                var telegramLoad = (IATCCaseLoadType)load;
                telegramLoad.Location = node.Name;
                plc12.SendTransportRequestTelegram(telegramLoad);
                load.Stop();
                // Add timer details to send TUDR a second time after 10 sec delay
                DropStationTimer[node.Name].UserData = new Tuple<string, Load>(node.Name, load);
                DropStationTimer[node.Name].Start();
            }
            else if (matchCCPA00WS01.Success)
            {
                // Check that the previous sort Id has been handled in ErgoPal
                // If it hasn't then stop the load and prevent the TURP and subsequent TULL from being sent
                var telegramLoad = (IATCCaseLoadType)load;
                var liftNumber = node.Name.Substring(5, 1);
                var key = string.Format("PA0{0}WS01", liftNumber);
                Lift lift = Core.Assemblies.Assembly.Items[LiftNamePrefix + liftNumber] as Lift;
                if (palletising.ContainsKey(key))
                {
                    if (LiftsWaiting.Contains(liftNumber) || !lift.Raised)
                    {
                        load.Stop();
                        return;
                    }
                    if (!Equals(palletising[key].ToString(), "0")
                        && !Equals(palletising[key].ToString(), telegramLoad.SortID.ToString()))
                    {
                        load.Stop();
                        return;
                    }
                }
                // Otherwise let the load continue and send the appropriate TURP
                telegramLoad.Location = telegramLoad.Destination = node.Name;
                plc12.SendTransportFinishedTelegram(telegramLoad);
            }
        }

        private void DropStationTimer_OnElapsed(Timer sender)
        {
            if (sender.UserData != null)
            {
                var userData = (Tuple<string, Load>)sender.UserData;
                var name = userData.Item1;
                var load = userData.Item2;
                // Send the second TUDR
                if (load != null)
                {
                    var telegramLoad = (IATCCaseLoadType)load;
                    telegramLoad.Location = name;
                    plc12.SendTransportRequestTelegram(telegramLoad);
                    // Restart the timer again so it will keep sending a TUDR until a TUMI is received
                    DropStationTimer[name].Start();
                }
            }
        }

        /// <summary>
        /// Returns true is exception telegram is sent
        /// </summary>
        /// <param name="atcLoad"></param>
        /// <returns></returns>
        private bool IsWeightException(IATCCaseLoadType atcLoad)
        {
            float maxWeight = 27.7f;
            if (!string.IsNullOrEmpty(atcLoad.ExceptionWeight))
            {
                int exWeight; int tol; int weight;
                if (int.TryParse(atcLoad.ExceptionWeight, out exWeight) &&
                    int.TryParse(atcLoad.ProjectFields["tolerance"], out tol))
                {
                    weight = (int)(atcLoad.CaseWeight * 1000);
                    int lowest = weight - tol;
                    int highest = weight + tol;
                    if (exWeight < lowest || exWeight > highest || exWeight > maxWeight)
                    {
                        atcLoad.StopLoad_WCSControl();

                        //Send the exception
                        float caseWeight = atcLoad.CaseWeight;

                        atcLoad.PresetStateCode = "ME";
                        atcLoad.CaseWeight = (float)exWeight / 1000;
                        plc11.SendTransportFinishedTelegram(atcLoad); //Send TUEX

                        //Reset the exception status
                        atcLoad.ExceptionWeight = "";
                        atcLoad.PresetStateCode = "OK";
                        atcLoad.CaseWeight = caseWeight;
                        Log.Write(string.Format("User Info {0}: The exception weight has been cleared after the TUEX has been sent", atcLoad.Location));

                        return true;
                    }
                }
            }
            return false;
        }

        private void MergeDivertConveyor_OnDivertPoint(MergeDivertConveyor sender, Load load)
        {
            if (sender.Name.Length == 10)
            {
                Match matchCCDE00WS00 = regexCCDE00WS00.Match(sender.Name);
                Match matchCCDE00LU00 = regexCCDE00LU00.Match(sender.Name);

                if (matchCCDE00WS00.Success) //At any of the depal positions stop the load and send a TransportRequest
                {
                    if (load is ATCTray && !((ATCTray)load).LoadWaitingForWCS)
                    {
                        load.Stop();

                        ATCTray atcLoad = load as ATCTray;
                        atcLoad.StopLoad_WCSControl();

                        atcLoad.PresetStateCode = "OK";
                        atcLoad.Location = sender.Name;

                        atcLoad.ProjectFields["tolerance"] = "00000000";

                        //populate the load with the exception weight if it exists
                        //UpdateExceptionWeight(sender.Name, atcLoad);


                        if (plc11.ControllerConnection.State == Core.Communication.State.Connected)
                        {
                            plc11.SendTransportRequestTelegram((ATCTray)load);
                        }
                        else
                        {
                            startTelegrams.Add((ATCTray)load);
                        }
                    }
                }
                else if (matchCCDE00LU00.Success) // CCDE01LU01, CCDE01LU02 - CCDE08LU01, CCDE08LU02
                {
                    load.Stop();
                    stackReturnAvailable[sender.Name] = true;
                    string pos = sender.Name.Substring(8, 2);
                    Direction defaultDir = Direction.Left;
                    RouteStatus nextRoute = sender.NextAvailableStatusLeft;
                    if (pos == "01")
                    {
                        defaultDir = Direction.Right;
                        nextRoute = sender.NextAvailableStatusRight;
                    }
                    //Always route the load, but re-route if the buffer becomes full
                    sender.RouteLoad(load, new List<Direction> { defaultDir }, false);
                }
                else if (sender.Name.Substring(0,8) == "STACKDIV")
                {
                    load.Stop();
                    int result;
                    if (int.TryParse(sender.Name.Substring(8, 2), out result))
                    {
                        //Choose destination at 1 if no destination exists
                        if ((result == 1 && !StackDestination.ContainsKey(load)))
                        {
                            stackLineRoundRobin++;
                            if (stackLineRoundRobin == 17)
                                stackLineRoundRobin = 1;
                            var minCount = StackLineBufferCount.Values.Min();
                            var lanesWithMinCount = StackLineBufferCount.Where(x => x.Value == minCount && x.Key >= stackLineRoundRobin).Select(x => x.Key).ToList();
                            if (!lanesWithMinCount.Any())
                            {
                                lanesWithMinCount = StackLineBufferCount.Where(x => x.Value == minCount).Select(x => x.Key).ToList();
                            }
                            var lane = lanesWithMinCount.First();
                            StackDestination[load] = lane;
                        }
                        else if (result > StackDestination[load])
                        {
                            //Destination is passed, choose a new destination
                            var downstream = StackLineBufferCount.Where(x => x.Key >= result).ToDictionary(x => x.Key, x => x.Value);
                            var minCount = downstream.Values.Min();
                            var lanesWithMinCount = downstream.Where(x => x.Value == minCount).Select(x => x.Key).ToList();
                            var lane = lanesWithMinCount.First();
                            StackDestination[load] = lane;
                        }

                        var destination = StackDestination[load];
                        if (result > destination && sender.NextAvailableStatusRight.Available == RouteStatuses.Available)
                        {
                            //destination is passed, try divert
                            sender.RouteLoad(load, new List<Direction> { Direction.Right }, false);
                        }
                        else if (result == destination && sender.NextAvailableStatusRight.Available == RouteStatuses.Available)
                        {
                            //this is the destination, divert if possible
                            sender.RouteLoad(load, new List<Direction> { Direction.Right }, false);
                        }
                        else if (result != destination && sender.NextAvailableStatusStraight.Available == RouteStatuses.Available)
                        {
                            //Go straight if possible
                            sender.RouteLoad(load, new List<Direction> { Direction.Straight }, false);
                        }
                        else
                        {
                            //Just do what you need to do
                            sender.RouteLoad(load, new List<Direction> { Direction.Right, Direction.Straight }, false);
                        }
                    }
                }
            }
        }

        private void CalculateSetWeight(string location, ATCTray atcLoad, int weight, int tolerance)
        {
            Type myType = typeof(LocalProperties);
            PropertyInfo info = myType.GetProperty(location);
            DepalMode mode = ((DepalMode)(info.GetValue(localProperties)));

            if (weight <= 2200) //Tray weight is 2.2Kg
            {
                atcLoad.CaseWeight = (float)(random.Next(2200, 27701)) / 1000;
            }
            else
            {
                if (tolerance > 0)
                {
                    int itolerance = random.Next(1, tolerance);
                    if (random.Next(0, 2) == 1) //make it negative
                    {
                        itolerance = -itolerance;
                        tolerance = -tolerance;
                    }
                    atcLoad.CaseWeight = (float)(weight + itolerance) / 1000;
                    if (mode == DepalMode.Exception)
                    {
                        atcLoad.ExceptionWeight = (weight + itolerance + tolerance).ToString();
                    }
                }
                else
                {
                    atcLoad.CaseWeight = (float)(weight) / 1000;

                    if (mode == DepalMode.Exception)
                    {
                        int itolerance = random.Next(500, 1000);
                        if (random.Next(0, 2) == 1) //make it negative
                        {
                            itolerance = -itolerance;
                        }
                        atcLoad.ExceptionWeight = (weight + itolerance).ToString();
                    }
                }
            }
        }

        private void GetStackReturnPoints()
        {
            for (int aisle = 1; aisle < 9; aisle++)
            {
                string position1 = string.Format("CCDE0{0}LU01", aisle.ToString());
                if (Core.Assemblies.Assembly.Items.ContainsKey(position1))
                {
                    MergeDivertConveyor position1Divert = Core.Assemblies.Assembly.Items[position1] as MergeDivertConveyor;
                    stackReturnPoints.Add(position1Divert);
                    stackReturnAvailable.Add(position1, true);
                }
                string position2 = string.Format("CCDE0{0}LU02", aisle.ToString());
                if (Core.Assemblies.Assembly.Items.ContainsKey(position2))
                {
                    MergeDivertConveyor position2Divert = Core.Assemblies.Assembly.Items[position2] as MergeDivertConveyor;
                    stackReturnPoints.Add(position2Divert);
                    stackReturnAvailable.Add(position2, true);
                }
            }
        }

        private void ResetStackBufferCount()
        {
            stackLineRoundRobin = 0;
            StackDestination.Clear();
            StackLineBufferCount.Clear();
            for (int line = 1; line < 17; line++)
            {
                StackLineBufferCount.Add(line, 1);
            }
        }

        private void StackTimer_OnElapsed(Timer sender)
        {
            stackTimer.Stop();
            stackTimer.Timeout = localProperties.StackFullTime;
            stackTimer.Start();
            if (stackFullPhotocell != null)
            {
                stackFullTrigger();
            }
        }

        void stackFullTrigger()
        {
            if (stackFullPhotocell.PhotocellStatus == PhotocellState.Blocked && stackFullLoad.Stopped)
            {
                //If the load becomes blocked, then send some into the DMS - Just routes one from each active station but might need more refinement
                List<MergeDivertConveyor> validReturnStacks = new List<MergeDivertConveyor>();
                foreach (MergeDivertConveyor pointDivert in stackReturnPoints)
                {
                    StraightAccumulationConveyor inbound = Core.Assemblies.Assembly.Items[string.Format("{0}_IN", pointDivert.Name)] as StraightAccumulationConveyor;
                    if (pointDivert.Active && stackReturnAvailable[pointDivert.Name] && inbound.LoadCount == 0)
                    {
                        validReturnStacks.Add(pointDivert);
                    }
                }

                if (validReturnStacks.Count > 0)
                {
                    //For now get a random load
                    MergeDivertConveyor returnPoint = validReturnStacks[random.Next(0, validReturnStacks.Count)];
                    if (returnPoint.Active)
                    {
                        //Send a TransportRequestTelegram (TUDR)
                        IATCCaseLoadType atcLoad = (IATCCaseLoadType)returnPoint.ActiveLoad;
                        atcLoad.Location = returnPoint.Name;
                        plc11.SendTransportRequestTelegram(atcLoad);
                        stackReturnAvailable[atcLoad.Location] = false; //Stops this position being used again
                    }
                }
            }
        }

        void CasePhotocell_OnPhotocellStatusChanged(object sender, PhotocellStatusChangedEventArgs e)
        {
            CasePhotocell photocell = sender as CasePhotocell;
            if (e._PhotocellStatus == PhotocellState.Blocked && photocell.Name.Length == 4 && photocell.Name.Substring(0,2) == "SE")
            {
                int result;
                if (int.TryParse(photocell.Name.Substring(2, 2), out result))
                {
                    StackLineBufferCount[result]++;
                    StackDestination.Remove(e._Load);
                }
            }
            else if (e._PhotocellStatus == PhotocellState.Clear && photocell.Name.Length == 4 && photocell.Name.Substring(0,2) == "SX")
            {
                int result;
                if (int.TryParse(photocell.Name.Substring(2, 2), out result))
                {
                    StackLineBufferCount[result]--;
                }
            }

            if (photocell.Name == "STACKFULL")
            {
                stackFullPhotocell = photocell;
                stackFullLoad = e._Load;
            }

            if (photocell.Name == "STACKFULL")
            { 

            }
            //else if (photocell.Name == "STACKEMPTY1")
            //{
            //    if (e._PhotocellStatus == PhotocellState.Clear)
            //    {
            //        //request from the DMS by sending a STFI telegram
            //        plc11.SendUnitFillingTelegram("CCDE01LO01", "1", "30");
            //    }
            //}
            //else if (photocell.Name == "STACKEMPTY14")
            //{
            //    if (e._PhotocellStatus == PhotocellState.Clear)
            //    {
            //        //request from the DMS by sending a STFI telegram
            //        plc11.SendUnitFillingTelegram("CCDE01LO01", "14", "30");
            //    }
            //}
            else if (photocell.Name.Length == 10 && photocell.Name.Substring(0, 8) == "EXPUSHER")
            {
                if (e._PhotocellStatus == PhotocellState.Blocked)
                {
                    PusherEnd_Enter((IATCCaseLoadType)e._Load, ((StraightConveyor)Core.Assemblies.Assembly.Items[photocell.Name.Substring(2, 8)]));
                }
                else if (e._PhotocellStatus == PhotocellState.Clear) //Load is exiting the pusher
                {
                    PusherEnd_Exit((IATCCaseLoadType)e._Load, ((StraightConveyor)Core.Assemblies.Assembly.Items[photocell.Name.Substring(2, 8)]));
                }
            }
            else if (photocell.Name.Length == 10 && photocell.Name.Substring(0, 8) == "ENPUSHER")
            {
                if (e._PhotocellStatus == PhotocellState.Blocked) //Load is entering the pusher
                {
                    PusherStart_Enter((IATCCaseLoadType)e._Load, ((StraightConveyor)Core.Assemblies.Assembly.Items[photocell.Name.Substring(2, 8)]));
                }
                else if (e._PhotocellStatus == PhotocellState.Clear)
                {
                    PusherStart_Exit((IATCCaseLoadType)e._Load, ((StraightConveyor)Core.Assemblies.Assembly.Items[photocell.Name.Substring(2, 8)]));
                }
            }
            else if (photocell.Name.Length == 12 && photocell.Name.Substring(0, 8) == "EXPUSHER")
            {
                if (e._PhotocellStatus == PhotocellState.Clear)
                {
                    PusherToo_Exit((IATCCaseLoadType)e._Load, ((StraightConveyor)Core.Assemblies.Assembly.Items[string.Format("PUSHED{0}", photocell.Name.Substring(8, 4))]));
                }
            }
            else if (regexPA00WS01.Match(photocell.Name).Success)
            {
                var atcLoad = (IATCCaseLoadType)e._Load;

                //Remeber what sort ID is being packed
                if (atcLoad != null && palletising.ContainsKey(photocell.Name))
                {
                    palletising[photocell.Name] = atcLoad.SortID;
                }

                if (e._PhotocellStatus == PhotocellState.Blocked)
                {
                    if (atcLoad != null && plc12 != null)
                    {
                        plc12.SendLocationLeftTelegram(atcLoad);
                        atcLoad.Dispose();
                    }
                }
                else if(e._PhotocellStatus == PhotocellState.LoadBlocked)
                {
                    atcLoad.Stop();
                }
            }
        }

        void PalletPhotocell_OnPhotocellStatusChanged(object sender, PhotocellStatusChangedEventArgs e)
        {
            PalletPhotocell photocell = sender as PalletPhotocell;
            string name = photocell.Parent.Name;

            if (pecWorkAround[name] == true)
            {
                pecWorkAround[name] = false;
                return;
            }

            if (e._PhotocellStatus == PhotocellState.Blocked && photocell.Name == "EXITPOINT")
            {
                IATCLoadType atcLoad = e._Load as IATCLoadType;
                atcLoad.Location = atcLoad.Destination = "PCPA01SP00";
                plc02.SendTransportFinishedTelegram(atcLoad);
                //plc02.SendLocationLeftTelegram(atcLoad);
                e._Load.Dispose();
                pecWorkAround[name] = true;
            }
        }

        bool BaseATCController_OnOverrideTelegramReceived(BaseATCController controller, string[] telegramFields)
        {
            if (telegramFields.GetTelegramType() == TelegramTypes.StartTransportTelegram)
            {
                string source = telegramFields.GetFieldValue(TelegramFields.source);

                Match matchCCDE00WS00 = regexCCDE00WS00.Match(source);
                Match matchCCDE00CC00 = regexCCDE00CC00.Match(source);
                Match matchCCPA00LU00 = regexCCPA00LU00.Match(source);
                Match matchPCPA01IS00 = regexPCPA01IS00.Match(source);
                Match matchCCDE00LU00 = regexCCDE00LU00.Match(source);

                if (matchCCDE00WS00.Success) //At the depal positions wait until the StartTransport is sent before releasing the load
                {
                    MergeDivertConveyor depal = Core.Assemblies.Assembly.Items[source] as MergeDivertConveyor;
                    IATCCaseLoadType atcLoad = depal.ActiveLoad as IATCCaseLoadType;
                    string telegramTuIdent = telegramFields.GetFieldValue(TelegramFields.tuIdent);
                    if (telegramTuIdent != null && telegramTuIdent == atcLoad.TUIdent)
                    {
                        ((Tray)atcLoad).Status = TrayStatus.Loaded;
                        int loadLength; int loadWidth; int loadHeight; int loadWeight = 0; int loadTolerance = 0;
                        int.TryParse(telegramFields.GetFieldValue(TelegramFields.length), out loadLength);
                        int.TryParse(telegramFields.GetFieldValue(TelegramFields.width), out loadWidth);
                        int.TryParse(telegramFields.GetFieldValue(TelegramFields.height), out loadHeight);
                        int.TryParse(telegramFields.GetFieldValue(TelegramFields.weight), out loadWeight);
                        int.TryParse(telegramFields.GetFieldValue("tolerance"), out loadTolerance);

                        ((Tray)atcLoad).LoadLength = (float)loadLength / 1000;
                        ((Tray)atcLoad).LoadWidth = (float)loadWidth / 1000;
                        ((Tray)atcLoad).LoadHeight = (float)loadHeight / 1000;
                        //((ATCTray)atcLoad).CaseWeight = (float)loadWeight / 1000;
                        //((ATCTray)atcLoad).CaseWeight = CalculateWeight(source, (ATCTray)atcLoad, loadWeight, loadTolerance);
                        CalculateSetWeight(source, (ATCTray)atcLoad, loadWeight, loadTolerance);


                        atcLoad.Destination = telegramFields.GetFieldValue(TelegramFields.destination);
                        atcLoad.ProjectFields["tolerance"] = telegramFields.GetFieldValue("tolerance");

                        atcLoad.ReleaseLoad_WCSControl();

                        depal.RouteLoad(depal.ActiveLoad, new List<Direction> { Direction.Straight }, false);

                        plc11.SendLocationLeftTelegram((IATCCaseLoadType)depal.ActiveLoad);
                    }
                    else
                    {
                        Log.Write(string.Format("Load ({0}) at {1}, not the same as the StartTransportTelegram ({2}) - teleram ignored", atcLoad.TUIdent, source, telegramTuIdent), System.Drawing.Color.Orange);
                    }
                    return true;
                }
                else if (matchCCDE00CC00.Success) //At the weigher, wait for start transport if the load was overweight
                {
                    StraightConveyor weigher = Core.Assemblies.Assembly.Items[source] as StraightConveyor;
                    IATCCaseLoadType atcLoad = (IATCCaseLoadType)weigher.TransportSection.Route.Loads.First.Value;
                    if (atcLoad != null)
                    {
                        if (!IsWeightException(atcLoad))
                        {
                            //Release the load
                            plc11.SendLocationArrivedTelegram(atcLoad, (atcLoad.CaseWeight * 1000).ToString()); //Send TUNO
                            atcLoad.ReleaseLoad_WCSControl();
                        }
                    }
                    else
                    {
                        Log.Write(string.Format("User Info {0}: Emulation did not handle TUMI correctly at weigher", source));
                    }
                    return true;
                }
                else if (matchCCPA00LU00.Success) // CCPA01LU01, CCPA01LU02 - CCPA08LU01, CCPA08LU02
                {
                    // Reset the timer to resend TUDR now that the TUMI has been received
                    DropStationTimer[source].UserData = null;
                    DropStationTimer[source].Stop();
                    DropStationTimer[source].Reset();
                    // Get rid of stacked trays
                    PusherStartTelegramReceived(telegramFields);
                    return true;
                }
                else if (matchPCPA01IS00.Success) // eg. PCPA01IS01, PCPA01IS02, PCPA01IS03, ...
                {
                    // Get Lift
                    string liftName = string.Format("PCPA01IS0{0}", source.Substring(9, 1));

                    Lift lift = Core.Assemblies.Assembly.Items[liftName] as Lift;
                    if (lift != null)
                    {
                        // Get Lift load
                        IATCPalletLoadType liftLoad = GetLiftLoad(lift);
                        if (liftLoad.LoadWaitingForWCS)
                        {
                            liftLoad.LoadWaitingForWCS = false;
                            // Set TUIdent and Destination to that sent from WMS
                            liftLoad.TUIdent = telegramFields.GetFieldValue(TelegramFields.tuIdent);
                            liftLoad.Destination = telegramFields.GetFieldValue(TelegramFields.destination);
                            //// Unblock the load
                            if (!lift.Raised)
                            {
                                lift.LiftConveyor.ThisRouteStatus.Available = RouteStatuses.Request;
                            }

                            PalletiserResendTimers[liftName].Stop();
                        }
                    }
                    return true;
                }
                else if (matchCCDE00LU00.Success)
                {
                    MergeDivertConveyor depal = Core.Assemblies.Assembly.Items[source] as MergeDivertConveyor;
                    ATCTray atcLoad = (ATCTray)Case_Load.GetCaseFromIdentification(telegramFields.GetFieldValue(TelegramFields.tuIdent));

                    if (atcLoad == null) { return false; }

                    if (atcLoad == (IATCCaseLoadType)depal.ActiveLoad)
                    {
                        depal.RouteLoad(atcLoad, new List<Direction> { Direction.Straight }, false);

                        int result = 0;
                        switch (depal.Name)
                        {
                            case "CCDE01LU01": result = 1; break;
                            case "CCDE01LU02": result = 2; break;
                            case "CCDE02LU01": result = 3; break;
                            case "CCDE02LU02": result = 4; break;
                            case "CCDE03LU01": result = 5; break;
                            case "CCDE03LU02": result = 6; break;
                            case "CCDE04LU01": result = 7; break;
                            case "CCDE04LU02": result = 8; break;
                            case "CCDE05LU01": result = 9; break;
                            case "CCDE05LU02": result = 10; break;
                            case "CCDE06LU01": result = 11; break;
                            case "CCDE06LU02": result = 12; break;
                            case "CCDE07LU01": result = 13; break;
                            case "CCDE07LU02": result = 14; break;
                            case "CCDE08LU01": result = 15; break;
                            case "CCDE08LU02": result = 16; break;
                        }

                        if (result != 0)
                        {
                            StackLineBufferCount[result]--;
                        }
                    }
                    else
                    {
                        Log.Write(string.Format("Load on transfer does not match the StartTransport - atcLoad {0}, conv {1}", atcLoad.TUIdent, source));
                    }
                }
            }
            else if (telegramFields.GetTelegramType() == TelegramTypes.SortLaneStartTransportTelegram)
            {
                //Log.Write("Debug: SortLaneStartTransportTelegram Received");

                string source = telegramFields.GetFieldValue(TelegramFields.source);
                if (!string.IsNullOrEmpty(source))
                {
                    // Reset the timer to resend TUDR now that the TUMI has been received
                    DropStationTimer[source].UserData = null;
                    DropStationTimer[source].Stop();
                    DropStationTimer[source].Reset();
                    // Release the load into the pusher
                    PusherSortTelegramReceived(telegramFields);
                }
            }
            else if (telegramFields.GetTelegramType() == TelegramTypes.SortLaneCloseSetTelegram)
            {
                int listName = 0; int setIndex = 0;
                int.TryParse(telegramFields.GetFieldValue(TelegramFields.sortLaneListName), out listName);
                int.TryParse(telegramFields.GetFieldValue(TelegramFields.sortLaneSetIndex), out setIndex);
                if (listName != 0 || setIndex != 0)
                {
                    List<string> pushMe = new List<string>();
                    foreach (var activeLoads in PusherActiveLoads)
                    {
                        List<IATCCaseLoadType> loadList = activeLoads.Value;

                        if (loadList.Count > 0 && loadList[0].SortID == listName && loadList[0].SortSequence == setIndex)
                        {
                            pushMe.Add(activeLoads.Key);
                        }
                    }

                    if (pushMe.Count == 1)
                    {
                        PusherActiveLoads[pushMe[0]].Last().SortInfo = "EI";
                        Push(Core.Assemblies.Assembly.Items[pushMe[0]] as StraightConveyor);
                    }
                    else if (pushMe.Count == 2)
                    {
                        if (pushMe[0] == OtherPusher(pushMe[1]))
                        {
                            Push(Core.Assemblies.Assembly.Items[pushMe[0]] as StraightConveyor);
                            PusherActiveLoads[pushMe[1]].Last().SortInfo = "EI";
                            Push(Core.Assemblies.Assembly.Items[pushMe[1]] as StraightConveyor);
                        }
                    }
                    else if (pushMe.Count != 0)
                    {
                        //There must be something wrong!
                        Log.Write(string.Format("Error SortLaneClosedSetTelegram unexpected number of pusher lanes matched sortLaneListName {0} and sortLaneSetIndex {1}", listName.ToString(), setIndex.ToString()), System.Drawing.Color.Red);
                    }
                }
                return true;
            }
            else if (telegramFields.GetTelegramType() == TelegramTypes.SubordinateTransportTelegram)
            {
                var destination = telegramFields.GetFieldValue(TelegramFields.destination);
                if (!string.IsNullOrEmpty(destination))
                {
                    var liftNumber = destination.Substring(5,1); // CCPA00WS01
                    EndPalletOperation(liftNumber);
                }
            }
            else if (telegramFields.GetTelegramType() == TelegramTypes.RequestStateChangeTelegram)
            {
                string location = telegramFields.GetFieldValue(TelegramFields.location);
                if (!string.IsNullOrEmpty(location))
                {
                    EndPalletOperation(location.Substring(9, 1));
                }
            }
            else if (telegramFields.GetTelegramType() == TelegramTypes.SetDeviceTelegram)
            {
                string location = telegramFields.GetFieldValue(TelegramFields.deviceId);
                if (!string.IsNullOrEmpty(location))
                {
                    EndPalletOperation(location.Substring(5, 1));
                }
            }
            return false;
        }
        #endregion

        #region Pusher Detray control
        private void PusherStartTelegramReceived(string [] telegramFields)
        {
            string source = telegramFields.GetFieldValue(TelegramFields.source);
            if (telegramFields.GetFieldValue(TelegramFields.destination) == "CCDE01LO01")
            {
                //Find the load and convert to a tray stack
                ATCTray atcLoad = (ATCTray)Case_Load.GetCaseFromIdentification(telegramFields.GetFieldValue(TelegramFields.tuIdent));
                atcLoad.Destination = telegramFields.GetFieldValue(TelegramFields.destination);
                atcLoad.Status = TrayStatus.Stacked;

                //Should the load stop or not
                //Trigger the pusher if there are things in it (as long as they are not stacks)
                //Also need to release this load when the push is complete
                string pusherName = GetPusherNameFromSource(source.Substring(5, 1), source.Substring(9, 1));
                if (PusherActiveLoads[pusherName].Count > 0 && PusherWaitingMode[pusherName] == false && PusherRelaseMode[pusherName] == false && !PushersWaiting.Contains(pusherName))
                {
                    PusherLoadWaiting[pusherName] = atcLoad;
                    Push(Core.Assemblies.Assembly.Items[pusherName] as StraightConveyor);
                }
                else
                {
                    atcLoad.ReleaseLoad_WCSControl();
                }
            }
            else //This is a sequenced load 
            {
                //Could have code here if Auchan team decide to use a starttransport message instead of a sort message
            }
        }

        private void PusherSortTelegramReceived(string[] telegramFields)
        {
            //Log.Write("Debug: PusherSortTelegramReceived");

            string source = telegramFields.GetFieldValue(TelegramFields.source);
            Match matchDepalCommsPoint = regexCCPA00LU00.Match(source.Substring(0, 10));
            if (matchDepalCommsPoint.Success)
            {
                //Log.Write("Debug: Load found");
                //find the load
                ATCTray atcLoad = (ATCTray)Case_Load.GetCaseFromIdentification(telegramFields.GetFieldValue(TelegramFields.tuIdent));
                if (atcLoad != null)
                {

                    //Log.Write("Debug: load not null");

                    atcLoad.Destination = telegramFields.GetFieldValue(TelegramFields.destination); //TODO: Think about this, this should happen automatically from the controller when the telegram is received!!!!!!!
                    int result = 0;
                    if (int.TryParse(telegramFields.GetFieldValue(TelegramFields.sortLaneListName), out result))
                    {
                        atcLoad.SortID = result;
                    }
                    result = 0;
                    if (int.TryParse(telegramFields.GetFieldValue(TelegramFields.sortLaneSetIndex), out result))
                    {
                        atcLoad.SortSequence = result;
                    }

                    atcLoad.SortInfo = telegramFields.GetFieldValue(TelegramFields.sortLaneSetComplete);

                    //Check to see if the other pusher can push now that loads have arrived here, this detects the last loads for a pallet.
                    string pusherName = GetPusherNameFromSource(source.Substring(5, 1), source.Substring(9, 1));

                    string pushWait = PushersWaiting.Contains(pusherName) ? "true" : "false";

                    Log.Write(string.Format("Debug SortSeq {0}: ActiveLoads {1}, PusherWaiting {2}, PusherWaitingMode {3},   PusherRealeaseMode {4}", 
                        pusherName, 
                        PusherActiveLoads[pusherName].Count.ToString(), 
                        pushWait, 
                        PusherWaitingMode[pusherName].ToString(), 
                        PusherRelaseMode[pusherName].ToString()));

                    if (PusherActiveLoads[pusherName].Count > 0 && (PusherActiveLoads[pusherName][0].SortSequence != atcLoad.SortSequence || PusherActiveLoads[pusherName][0].SortID != atcLoad.SortID)) //Sequence number has changed, trigger the push and hold the load
                    {
                        PusherLoadWaiting[pusherName] = atcLoad;
                        Push(Core.Assemblies.Assembly.Items[pusherName] as StraightConveyor);
                    }
                    else if (PusherActiveLoads[pusherName].Count == 0 || (!PushersWaiting.Contains(pusherName) && !PusherWaitingMode[pusherName] && !PusherRelaseMode[pusherName]))
                    {
                        atcLoad.ReleaseLoad_WCSControl(); //Just release into the next position
                        PusherLoadTimer[pusherName].Reset();
                        PusherLoadWaiting[pusherName] = null;
                    }
                    else
                    {
                        PusherLoadWaiting[pusherName] = atcLoad;
                    }
                }
                else
                {
                    Log.Write("Unable to retrieve load with Id : " + telegramFields.GetFieldValue(TelegramFields.tuIdent));
                }
            }
        }

        public void ManualPush(string PusherName)
        {
            if (Core.Environment.InvokeRequired)
            {
                Core.Environment.Invoke(() => ManualPush(PusherName));
                return;
            }

            //This is to perform a manual push for loads in the pusher
            if (!Push(Core.Assemblies.Assembly.Items[PusherName] as StraightConveyor))
            {
                Log.Write(string.Format("Did not manage to start the push {0}, reset the pusher sequence and try again", PusherToCommPoint(PusherName)));
                PushersWaiting.Remove(PusherName);
                PusherWaitingMode[PusherName] = false;
            }
            else
            {
                Log.Write(string.Format("Push has started on {0}", PusherToCommPoint(PusherName)));
            }
        }

        public void DebugPusher(string pusherName)
        {
            string pushWait = PushersWaiting.Contains(pusherName) ? "true" : "false";

            Log.Write(string.Format("Manual Debug SortSeq {0}: ActiveLoads {1}, PusherWaiting {2}, PusherWaitingMode {3},   PusherRealeaseMode {4}",
                pusherName,
                PusherActiveLoads[pusherName].Count.ToString(),
                pushWait,
                PusherWaitingMode[pusherName].ToString(),
                PusherRelaseMode[pusherName].ToString()));
        }

        public void ResetPusher(string PusherTooName)
        {
            PusherCurrentGroup[PusherTooName] = 0;
            PusherCurrentPallet[PusherTooName] = 0;
            PusherGroupEnd[PusherTooName] = "E";
            PusherPalEnd[PusherTooName] = "E";
            PushAnything[PusherTooName] = true;
            Log.Write(string.Format("Pushers {0} Reset", PushTooToAisle(PusherTooName)));
        }
        
        private string GetPusherNameFromSource(string aisle, string position)
        {
            int intPosition = 0;
            int.TryParse(position, out intPosition);
            switch (aisle)
            {
                case "1":
                    return string.Format("PUSHER0{0}", intPosition);
                case "2":
                    return string.Format("PUSHER0{0}", intPosition + 2);
                case "3":
                    return string.Format("PUSHER0{0}", intPosition + 4);
                case "4":
                    return string.Format("PUSHER0{0}", intPosition + 6);
                case "5":
                    if (intPosition == 1)
                    {
                        return string.Format("PUSHER0{0}", intPosition + 8);
                    }
                    else
                    {
                        return string.Format("PUSHER{0}", intPosition + 8);
                    }
                case "6": 
                    return string.Format("PUSHER1{0}", intPosition);
                case "7":
                    return string.Format("PUSHER1{0}", intPosition + 2);
                case "8":
                    return string.Format("PUSHER1{0}", intPosition + 4);
                default:
                    return "";
            }
        }

        private void PusherStart_Enter(IATCCaseLoadType atcLoad, StraightConveyor pusher)
        {
            PusherLoadTimer[pusher.Name].Reset();

            if (((ATCTray)atcLoad).Status == TrayStatus.Loaded)
            {
                PusherActiveLoads[pusher.Name].Add(atcLoad);
            }

            if (PusherActiveLoads[pusher.Name].Count >= 4)
            {
                Push(pusher);
            }
        }

        private void PusherStart_Exit(IATCCaseLoadType atcLoad, StraightConveyor pusher)
        {
            if (atcLoad == null)
            {
                return;
            }

            if (atcLoad.SortInfo != null && atcLoad.SortInfo.Substring(0,1) == "E")
            {
                Push(pusher);
            }
            else
            {
                if (((ATCTray)atcLoad).Status == TrayStatus.Loaded && !PusherWaitingMode[pusher.Name])
                {
                    PusherLoadTimer[pusher.Name].Reset();
                    PusherLoadTimer[pusher.Name].Start();
                }
            }
        }

        private void PusherEnd_Enter(IATCCaseLoadType atcLoad, StraightConveyor pusher)
        {
            if (atcLoad.Destination != "CCDE01LO01")
            {
                //Get the conveyor that the Photocell is on 
                if (PusherRelaseMode[pusher.Name] == false || PusherPushTimer[pusher.Name].Running)
                {
                    //The load is to be pushed so should be stopped
                    atcLoad.StopLoad_PLCControl();
                    if (PushersWaiting.Contains(pusher.Name))
                    {
                        Push(pusher);
                    }
                }
            }
        }

        private void PusherEnd_Exit(IATCCaseLoadType atcLoad, StraightConveyor pusher)
        {
            if (pusher.TransportSection.Route.Loads.Count == 0)
            {
                PusherActiveLoads[pusher.Name].Clear();
                PusherRelaseMode[pusher.Name] = false;

                if (PusherLoadWaiting[pusher.Name] != null)
                {
                    PusherLoadWaiting[pusher.Name].ReleaseLoad_WCSControl();
                }
            }
        }

        private void PusherLoadTimer_OnElapsed(Timer sender)
        {
            StraightConveyor pusher = Core.Assemblies.Assembly.Items[(string)sender.UserData] as StraightConveyor;
            StraightConveyor pushToPoint = Core.Assemblies.Assembly.Items[PushTooPoint((string)sender.UserData)] as StraightConveyor;
            if (PusherActiveLoads[pusher.Name].Count > 0)// && pushToPoint.LoadCount == 0)    
            {
                if (!Push(pusher))
                {
                    PusherLoadTimer[pusher.Name].Reset();
                    PusherLoadTimer[pusher.Name].Start();
                    PushersWaiting.Remove(pusher.Name);
                    PusherWaitingMode[pusher.Name] = false;

                    for (int i = 0; i < PusherActiveLoads[pusher.Name].Count; i++)
                    {
                        ATCTray tray = PusherActiveLoads[pusher.Name][i] as ATCTray;
                        tray.LoadColor = System.Drawing.Color.Yellow;
                    }
                }
            }
        }

        private void PusherPushTimer_OnElapsed(Timer sender)
        {
            string pusherName = (string)sender.UserData;

            //When the number of loads on the pusher = 4 then get rid of the loads and release 
            float position = 0.5f;
            foreach (IATCCaseLoadType atcLoad in PusherActiveLoads[(string)sender.UserData])
            {
                ((ATCTray)atcLoad).Status = TrayStatus.Empty;

                IATCCaseLoadType newLoad = CreateCaseLoad(atcLoad, string.Format("VLU{0}", atcLoad.TUIdent));
                newLoad.SortID = atcLoad.SortID;
                newLoad.SortSequence = atcLoad.SortSequence;
                //newLoad.TUIdent = string.Format("VLU{0}", newLoad.TUIdent);

                StraightConveyor pushTooPoint = Core.Assemblies.Assembly.Items[PushTooPoint((string)sender.UserData)] as StraightConveyor;
                pushTooPoint.TransportSection.Route.Add((Load)newLoad, pushTooPoint.Length - position);
                newLoad.Yaw = (float)Math.PI / 2;
                position += 0.55f;
            }

            //Set the end of sequence if it is the end of sequence
            PusherGroupEnd[PushTooPoint(pusherName)] = PusherActiveLoads[pusherName].Last().SortInfo.Substring(0, 1);
            PusherPalEnd[PushTooPoint(pusherName)] = PusherActiveLoads[pusherName].Last().SortInfo.Substring(1, 1);

            PushAnything[PushTooPoint(pusherName)] = false;
            PusherCurrentGroup[PushTooPoint(pusherName)] = PusherActiveLoads[pusherName][0].SortSequence;
            PusherCurrentPallet[PushTooPoint(pusherName)] = PusherActiveLoads[pusherName][0].SortID;

            PusherWaitingMode[pusherName] = false;
            PusherRelaseMode[pusherName] = true;
            //release the first load on the pusher
            PusherActiveLoads[pusherName][0].ReleaseLoad_PLCControl();

            Log.Write(string.Format("Push Complete: {0}, ListName {1}, SetIndex {2}, Load Count {3}, SeqEnd {4} PalEnd {5}", 
                pusherName, 
                PusherActiveLoads[pusherName][0].SortID, 
                PusherActiveLoads[pusherName][0].SortSequence, 
                PusherActiveLoads[pusherName].Count,
                PusherGroupEnd[PushTooPoint(pusherName)],
                PusherPalEnd[PushTooPoint(pusherName)]));
        }

        public bool Push(StraightConveyor pusher)
        {
            if (!PusherPushTimer[pusher.Name].Running && PusherActiveLoads[pusher.Name].Count > 0)
            {
                bool pusherWasWaiting = false;
                if (PushersWaiting.Contains(pusher.Name))
                {
                    PushersWaiting.Remove(pusher.Name);
                    pusherWasWaiting = true;
                }

                //check if the first load to push has reached the front photcell, if not then do not push until it gets there
                if (!PusherPushTimer[OtherPusher(pusher.Name)].Running && PusherActiveLoads[pusher.Name][0].LoadWaitingForPLC)
                {
                    PusherLoadTimer[pusher.Name].Reset();
                    PusherWaitingMode[pusher.Name] = true;

                    //Get the pushed conveyor and check if it's clear or not
                    StraightConveyor pushTooPoint = Core.Assemblies.Assembly.Items[PushTooPoint((string)pusher.Name)] as StraightConveyor;
                    if (//There is something to push
                        pushTooPoint.TransportSection.Route.Loads.Count == 0 &&
                        //AND SortSeq(Group) and SortID(Pallet) are the same on the pusher and the loads to be pushed
                        ((PusherCurrentGroup[PushTooPoint(pusher.Name)] == PusherActiveLoads[pusher.Name][0].SortSequence &&
                        PusherCurrentPallet[PushTooPoint(pusher.Name)] == PusherActiveLoads[pusher.Name][0].SortID) ||
                        //OR the previous group has completed AND
                        (PusherGroupEnd[PushTooPoint(pusher.Name)] == "E" &&
                        //EITHER the SortSeq(Group) is 1 higher than the last loads to be pushed on the same SortID(Pallet)
                        ((PusherCurrentGroup[PushTooPoint(pusher.Name)] + 1) == PusherActiveLoads[pusher.Name][0].SortSequence && PusherCurrentPallet[PushTooPoint(pusher.Name)] == PusherActiveLoads[pusher.Name][0].SortID) ||
                        //OR its a new SortID(Pallet) and the SortSeq(Group) is 1 on the loads to be pushed AND the last case of the pallet has been seen.
                        (PusherCurrentPallet[PushTooPoint(pusher.Name)] != PusherActiveLoads[pusher.Name][0].SortID && PusherActiveLoads[pusher.Name][0].SortSequence == 1 && PusherPalEnd[PushTooPoint(pusher.Name)] == "E")) ||
                        //OR PushAnything (i.e. the pusher has been reset)
                        PushAnything[PushTooPoint(pusher.Name)]))
                    {
                        PusherPushTimer[pusher.Name].Timeout = 5;
                        if (!pusherWasWaiting)
                        {
                            int count = pusher.TransportSection.Route.Loads.Count;
                            switch (count)
                            {
                                case 1: PusherPushTimer[pusher.Name].Timeout = 8; break;
                                case 2: PusherPushTimer[pusher.Name].Timeout = 7; break;
                                case 3: PusherPushTimer[pusher.Name].Timeout = 6; break;
                            }
                        }

                        //Changing the color of the loads so you can see that the push has started
                        for (int i = 0; i < PusherActiveLoads[pusher.Name].Count; i++)
                        {
                            ATCTray tray = PusherActiveLoads[pusher.Name][i] as ATCTray;
                            tray.LoadColor = System.Drawing.Color.Green;
                        }

                        PusherPushTimer[pusher.Name].Start();

                        return true;
                    }
                }
                //Otherwise we need to ensure that the loads wait until they can be pushed
                PushersWaiting.Add(pusher.Name);

                //Changing the color of the load so you can see if the loads want to push
                for (int i = 0; i < PusherActiveLoads[pusher.Name].Count; i++)
                {
                    ATCTray tray = PusherActiveLoads[pusher.Name][i] as ATCTray;
                    tray.LoadColor = System.Drawing.Color.Orange;
                }
            }
            return false;
        }

        private void PusherToo_Exit(IATCCaseLoadType atcLoad, StraightConveyor pushToo)
        {
            if (atcLoad == null) return;
            //If pushToo conveyor is empty then see if there is something waiting to push into it
            if (pushToo.TransportSection.Route.Loads.Count == 0)
            {
                string pushera = string.Format("PUSHER{0}", pushToo.Name.Substring(6, 2));
                string pusherb = string.Format("PUSHER{0}", pushToo.Name.Substring(8, 2));

                List<string> waitingPusher = PushersWaiting.FindAll(x => x == pushera || x == pusherb);
                if (waitingPusher.Count > 0)
                {
                    //Try to push whatever is waiting if any
                    foreach(string pusher in waitingPusher)
                    {
                        Push(Core.Assemblies.Assembly.Items[pusher] as StraightConveyor);
                    }
                    //waitingPusher = PushersWaiting.First(x => x == pushera || x == pusherb);
                    //Push(Core.Assemblies.Assembly.Items[waitingPusher] as StraightConveyor);
                }
            }
        }

        public void PusherTest()
        {
            foreach (string pusherName in PushersWaiting)
            {
                Log.Write(pusherName);
            }
        }

        public void PickerTimeChange(float time)
        {
            for (int i = 1; i < 9; i++)
            {
                string ergoPal = string.Format("ErgoPal{0}", i.ToString());
                StraightConveyor conveyor = Core.Assemblies.Assembly.Items[ergoPal] as StraightConveyor;

                foreach (Core.Assemblies.Assembly assembly in conveyor.Assemblies)
                {
                    if (assembly.Name == string.Format("PA0{0}WS01", i.ToString()))
                    {
                        CasePhotocell photocell = assembly as CasePhotocell;
                        photocell.BlockedTimeout = time;
                    }
                }
            }
        }
        #endregion

        #region Standard
        protected override void Received(Core.Communication.Connection connection, string telegram)
        {
            #region commom check and split telegram

            if (telegram == null)
                return;

            string[] telegramFields = telegram.Split(',');

            #endregion
        }

        private void ResetStandard()
        {
            Experior.Core.Environment.Scene.Reset();

            foreach (Core.Communication.Connection connection in Core.Communication.Connection.Items.Values)
            {
                connection.Disconnect();
            }
        }

        public override void Dispose()
        {
            Core.Environment.UI.Toolbar.Remove(Speed1);
            Core.Environment.UI.Toolbar.Remove(Speed2);
            Core.Environment.UI.Toolbar.Remove(Speed5);
            Core.Environment.UI.Toolbar.Remove(Speed10);
            Core.Environment.UI.Toolbar.Remove(Speed20);

            Core.Environment.UI.Toolbar.Remove(ResetButton);
            Core.Environment.UI.Toolbar.Remove(fps1);
            Core.Environment.UI.Toolbar.Remove(localProp);
            Core.Environment.UI.Toolbar.Remove(connectButt);
            Core.Environment.UI.Toolbar.Remove(disconnectButt);

            ClearDropStationTimers();

            ClearPushers();

            stackTimer.OnElapsed -= StackTimer_OnElapsed;
            stackBufferCheck.OnElapsed -= StackBufferCheck_OnElapsed;
            
            base.Dispose();
        }

        public ATCCaseLoad CreateCaseLoad(IATCCaseLoadType atcLoad, string identification)
        {
            return atc.CreateCaseLoad
                (
                    "CC02",
                    identification,
                    "",
                    atcLoad.Location,
                    "",
                    "OK",
                    "250",
                    "300",
                    "450",
                    "200",
                    "ltgray"
                );
        }


        #endregion
    }

    public class tCarDest
    {
        public tCarDest(PalletStraight conveyor)
        {
            Conveyor = conveyor;
            Conveyor.ThisRouteStatus.OnRouteStatusChanged += ThisRouteStatus_OnRouteStatusChanged;
        }

        private void ThisRouteStatus_OnRouteStatusChanged(object sender, RouteStatusChangedEventArgs e)
        {
            if (e._available == RouteStatuses.Blocked)
            {
                inTransit = false;
            }
        }

        public PalletStraight Conveyor;
        public bool inTransit = false;
    }
}


