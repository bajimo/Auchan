using VirtualFlowController.Controllers;
using VirtualFlowController.Routings;
using Dematic.ATC;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using VirtualFlowController.Project_Controller;
using System.Timers;
using System.ComponentModel;

namespace VirtualFlowController.Core
{
    public class Auchan : ProjectController
    {
        AuchanUserControl userControl;
        ATCConveyor cc02 = vfc.AllControllers["CC02"] as ATCConveyor;
        ATCConveyor cc01 = vfc.AllControllers["PC11"] as ATCConveyor;
        ATCMultiShuttle msc1 = vfc.AllControllers["MSC1"] as ATCMultiShuttle;
        ATCMultiShuttle msc2 = vfc.AllControllers["MSC2"] as ATCMultiShuttle;
        ATCMultiShuttle msc3 = vfc.AllControllers["MSC3"] as ATCMultiShuttle;
        ATCMultiShuttle msc4 = vfc.AllControllers["MSC4"] as ATCMultiShuttle;
        ATCMultiShuttle msc5 = vfc.AllControllers["MSC5"] as ATCMultiShuttle;
        ATCMultiShuttle msc6 = vfc.AllControllers["MSC6"] as ATCMultiShuttle;
        ATCMultiShuttle msc7 = vfc.AllControllers["MSC7"] as ATCMultiShuttle;
        ATCMultiShuttle msc8 = vfc.AllControllers["MSC8"] as ATCMultiShuttle;

        Dictionary<string, AisleRouting> AisleRoutings = new Dictionary<string, AisleRouting>();
        Dictionary<string, int> AisleCount = new Dictionary<string, int>();
        Dictionary<string, int> SideGroupCount = new Dictionary<string, int>();
        Dictionary<string, int> SideGroupNumber = new Dictionary<string, int>();

        Dictionary<string, List<string>> LastOfSidePallet = new Dictionary<string, List<string>>(); //Add to the list if it's the last one
        Dictionary<string, string> LastDropIndex = new Dictionary<string, string>(); //Last ListName seen this side (Drop Index)
        Dictionary<string, string> LastIdent = new Dictionary<string, string>(); //Last Ident seen this side

        Regex regexCCPA00LU00 = new Regex(@"^CCPA0[1-8]LU0[1|2]$"); // eg. CCPA01LU01, CCPA01LU02 - CCPA08LU01, CCPA08LU02
        Regex regexCCDE00WS00 = new Regex(@"^CCDE0[1-8]WS0[1|2]$"); // eg. CCDE01WS01, CCDE01WS02 - CCDE08WS01, CCDE08WS02

        Dictionary<string, PusherTimer> PusherSeqDelay = new Dictionary<string, PusherTimer>();

        Dictionary<string, int[]> sequencing = new Dictionary<string, int[]>();
        Dictionary<string, sequence> pushSeq = new Dictionary<string, sequence>();

        List<string[]> DepalMemory = new List<string[]>();

        private class sequence
        {
            public int leftGroup = 2;
            public int rightGroup = 1;

            public int leftCount = 1;
            public int rightCount = 1;
        }

        //Now updated to Version 1 of the VFC
        public Auchan(XmlNode xmlNode) :base(xmlNode)
        {
            projectControl = new AuchanUserControl();
            userControl = projectControl as AuchanUserControl;

            cc02.statusChanged += aController_statusChanged;
            cc02.telegramReceived += aController_telegramReceived;
            cc01.telegramReceived += aController_telegramReceived;

            msc1.telegramReceived += MSC_telegramReceived;
            msc2.telegramReceived += MSC_telegramReceived;
            msc3.telegramReceived += MSC_telegramReceived;
            msc4.telegramReceived += MSC_telegramReceived;
            msc5.telegramReceived += MSC_telegramReceived;
            msc6.telegramReceived += MSC_telegramReceived;
            msc7.telegramReceived += MSC_telegramReceived;
            msc8.telegramReceived += MSC_telegramReceived;

            userControl.reset.Click += Reset_Click;
            userControl.start.Click += Start_Click;
            userControl.stop.Click += Stop_Click;

            for (int i = 1; i < 9; i++)
            {
                AisleRoutings.Add(string.Format("AISLE{0}", i.ToString().PadLeft(2, '0')), new AisleRouting());
                AisleCount.Add(string.Format("AISLE{0}", i.ToString().PadLeft(2, '0')), 0);

            }

            for (int ai = 1; ai < 9; ai++)
            {
                for (int pos = 0; pos < 3; pos++)
                {
                    string commPoint = string.Format("CCPA{0}LU{1}", ai.ToString("00"), pos.ToString("00"));
                    PusherSeqDelay.Add(commPoint, new PusherTimer());
                    PusherSeqDelay[commPoint].Elapsed += PusherTimer_Elapsed;
                    PusherSeqDelay[commPoint].CommPoint = commPoint;
                    SideGroupCount.Add(commPoint, 0);
                    SideGroupNumber.Add(commPoint, 0);
                    LastOfSidePallet.Add(commPoint, new List<string>());
                    LastDropIndex.Add(commPoint, "");
                    LastIdent.Add(commPoint, "");
                }
            }

            SetupSeuence();
        }

        private void MSC_telegramReceived(object sender, ControllerTelegramReceivedEventArgs e)
        {
            if (e._telegram.GetTelegramType() == TelegramTypes.MultishuttleTransportFinishedTelegram)
            {
                string dest = e._telegram.GetFieldValue(TelegramFields.destination);
                string dropIndex = e._telegram.GetFieldValue(TelegramFields.dropIndex);
                string tuIdent = e._telegram.GetFieldValue(TelegramFields.tuIdent);

                if (dest.Contains("DS10"))
                {
                    string aisle = dest.Substring(4, 2);
                    string side = dest.Substring(7, 1) == "R" ? "01" : "02"; //L or R; This will define the pusher R=LU01 and L=LU02
                    string commPoint = string.Format("CCPA{0}LU{1}", aisle, side);

                    //if the drop index to the last one has changed then the LastIdent is added to the list
                    if (dropIndex != LastDropIndex[commPoint])
                    {
                        LastOfSidePallet[commPoint].Add(LastIdent[commPoint]);
                    }
                    LastIdent[commPoint] = tuIdent;
                    LastDropIndex[commPoint] = dropIndex;
                }
            }
        }
        private void Start_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (Controller cont in vfc.AllControllers.Values)
            {
                if (cont is ATCMultiShuttle)
                {
                    ATCMultiShuttle DMS = cont as ATCMultiShuttle;
                    DMS.Stop_Click(this, new System.Windows.RoutedEventArgs());
                    DMS.Start_Click(this, new System.Windows.RoutedEventArgs());
                }
            }
        }

        private void Stop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (Controller cont in vfc.AllControllers.Values)
            {
                if (cont is ATCMultiShuttle)
                {
                    ATCMultiShuttle DMS = cont as ATCMultiShuttle;
                    DMS.Stop_Click(this, new System.Windows.RoutedEventArgs());
                }
            }

        }

        private void Reset_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetupSeuence();

            foreach (Controller cont in vfc.AllControllers.Values)
            {
                if (cont is ATCMultiShuttle)
                {
                    ATCMultiShuttle DMS = cont as ATCMultiShuttle;
                    DMS.SeqReset_Click(this, new System.Windows.RoutedEventArgs());
                    DMS.SeqClear_Click(this, new System.Windows.RoutedEventArgs());
                }
            }
        }

        void SetupSeuence()
        {
            pushSeq.Clear();
            for (int i = 1; i < 9; i++)
            {
                pushSeq.Add(i.ToString(), new sequence());
            }
        }

        void aController_statusChanged(object sender, ControllerStatusChangedEventArgs e)
        {
            
        }

        /// <summary>
        /// This tells you which count to be looking at
        /// </summary>
        /// <param name="pusherName"></param>
        /// <returns></returns>
        private string WhichCount(string pusherName)
        {
            switch (pusherName.Substring(8,2))
            {
                case "01": case "02": return "AISLE01";
                case "03": case "04": return "AISLE02";
                case "05": case "06": return "AISLE03";
                case "07": case "08": return "AISLE04";
                case "09": case "10": return "AISLE05";
                case "11": case "12": return "AISLE06";
                case "13": case "14": return "AISLE07";
                case "15": case "16": return "AISLE08";
            }
            return null;
        }

        private string WhichDMS(string pusherName)
        {
            switch (pusherName.Substring(8, 2))
            {
                case "01": case "02": return "MSC1";
                case "03": case "04": return "MSC2";
                case "05": case "06": return "MSC3";
                case "07": case "08": return "MSC4";
                case "09": case "10": return "MSC5";
                case "11": case "12": return "MSC6";
                case "13": case "14": return "MSC7";
                case "15": case "16": return "MSC8";
            }
            return null;
        }

        private void PusherTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PusherTimer timer = sender as PusherTimer;
            timer.Stop();

            SendSortTransportTelegram(timer.telegram, timer.destination, timer.sortID, timer.sortSeq, timer.sortInfo);
            timer.ResetData();
        }


       void Right(string aisle, out string pallet, out string group, out string sortInfo)
        {
            pallet = "1";
            group = pushSeq[aisle].rightGroup.ToString();
            sortInfo = "II";
            if (pushSeq[aisle].rightCount == 4)
            {
                sortInfo = "EI";
                pushSeq[aisle].rightCount = 0;
                pushSeq[aisle].rightGroup = pushSeq[aisle].rightGroup + 2;
            }
            pushSeq[aisle].rightCount++;
        }

        void Left(string aisle, out string pallet, out string group, out string sortInfo)
        {
            pallet = "1";
            group = pushSeq[aisle].leftGroup.ToString();
            sortInfo = "II";
            if (pushSeq[aisle].leftCount == 4)
            {
                sortInfo = "EI";
                pushSeq[aisle].leftCount = 0;
                pushSeq[aisle].leftGroup = pushSeq[aisle].leftGroup + 2;
            }
            pushSeq[aisle].leftCount++;
        }

        void aController_telegramReceived(object sender, ControllerTelegramReceivedEventArgs e)
        {
            if (e._telegram.GetTelegramType() == TelegramTypes.TransportRequestTelegram)
            {
                string location = e._telegram.GetFieldValue(TelegramFields.location);
                string tuIdent = e._telegram.GetFieldValue(TelegramFields.tuIdent);
                Match matchCCPA00LU00 = regexCCPA00LU00.Match(location);
                Match matchCCDE00WS00 = regexCCDE00WS00.Match(location);

                if (matchCCPA00LU00.Success)
                {
                    string aisle = location.Substring(5, 1);
                    string position = location.Substring(9, 1);
                    if (e._telegram.GetFieldValue(TelegramFields.dropIndex) == "" || e._telegram.GetFieldValue(TelegramFields.dropIndex) == "0000")
                    {
                        ATCStartTransport stack = vfc.AllRoutings[string.Format("CCPA0{0}LU0{1}_STACK", aisle, position)] as ATCStartTransport;
                        stack.triggerRouting(e._telegram);
                    }
                    else
                    {
                        string destination = string.Format("CCPA0{0}WS01", aisle, position);
                        string pallet;
                        string group;
                        string sortInfo;
                        if (position == "1")
                        {
                            Left(aisle, out pallet, out group, out sortInfo);
                        }
                        else
                        {
                            Right(aisle, out pallet, out group, out sortInfo);
                        }
                        SendSortTransportTelegram(e._telegram, destination, pallet, group, sortInfo);
                    }
                }
                else if (matchCCDE00WS00.Success && !EnableDepal)
                {
                    DepalMemory.Add(e._telegram);
                }
            }
        }

        private bool _PusherDelay = false;
        [DisplayName("Pusher Delay")]
        [Description("When loads arrive at the pusher sequence point, should there be a random delay between sending the messages or not")]
        [Category("Pushers")]
        public bool PusherDelay
        {
            get { return _PusherDelay; }
            set
               {
                _PusherDelay = value;
                UpdateConfig();

                if (!value)
                {
                    foreach (PusherTimer timer in PusherSeqDelay.Values)
                    {
                        if (timer.telegram != null)
                        {
                            timer.Stop();
                            SendSortTransportTelegram(timer.telegram, timer.destination, timer.sortID, timer.sortSeq, timer.sortInfo);
                            timer.ResetData();
                        }
                    }
                }
            }
        }

        private string _ListName;
        [DisplayName("List Name")]
        [Category("Sort Lane")]
        public string ListName
        {
            get { return _ListName; }
            set
            {
                _ListName = value;
                UpdateConfig();
            }
        }

        private string _SetIndex;
        [DisplayName("Set Index")]
        [Category("Sort Lane")]
        public string SetIndex
        {
            get { return _SetIndex; }
            set
            {
                _SetIndex = value;
                UpdateConfig();
            }
        }

        private bool _EnableDepal;
        [DisplayName("Enable Depal")]
        [Category("Exerciser")]
        public bool EnableDepal
        {
            get { return _EnableDepal; }
            set
            {
                _EnableDepal = value;
                UpdateConfig();
                if (value)
                {
                    //Find each routing and enable it
                    for (int a = 1; a < 9; a++)
                    {
                        ((ATCStartTransport)vfc.AllRoutings[string.Format("CCDE0{0}WS01", a)]).Distribution = DistributionATC.First;
                        ((ATCStartTransport)vfc.AllRoutings[string.Format("CCDE0{0}WS02", a)]).Distribution = DistributionATC.First;
                    }
                    //Send StartTransport for any waiting loads
                    foreach(string[] telegram in DepalMemory)
                    {
                        vfc.AllRoutings[telegram.GetFieldValue(TelegramFields.location)].triggerRouting(telegram);
                    }
                }
                else
                {
                    //Find each routing and disable it
                    for (int a = 1; a < 9; a++)
                    {
                        ((ATCStartTransport)vfc.AllRoutings[string.Format("CCDE0{0}WS01", a)]).Distribution = DistributionATC.None;
                        ((ATCStartTransport)vfc.AllRoutings[string.Format("CCDE0{0}WS02", a)]).Distribution = DistributionATC.None;
                    }
                }
            }
        }

        private void SendSortTransportTelegram(string[] Telegram, string destination, string sortID, string setCount, string setComplete)
        {
            string telegram = string.Empty.InsertType(TelegramTypes.SortLaneStartTransportTelegram);
            telegram = telegram.AppendField(TelegramFields.mts, "CC02");
            telegram = telegram.AppendField(TelegramFields.tuIdent, Telegram.GetFieldValue(TelegramFields.tuIdent));
            telegram = telegram.AppendField(TelegramFields.tuType, Telegram.GetFieldValue(TelegramFields.tuType));
            telegram = telegram.AppendField(TelegramFields.source, Telegram.GetFieldValue(TelegramFields.location));
            telegram = telegram.AppendField(TelegramFields.destination, destination);
            telegram = telegram.AppendField(TelegramFields.sortLaneListName, sortID);
            telegram = telegram.AppendField(TelegramFields.sortLaneSetIndex, setCount);
            telegram = telegram.AppendField(TelegramFields.sortLaneSetComplete, setComplete);
            telegram = telegram.AppendField(TelegramFields.color, vfc.systemConfiguration.DefaultLoadColour.ToString());
            cc02.Send(telegram);
        }

        private class AisleRouting
        {
            public int CurrentSequence = 1;
            public int GroupCount = 0;
            public Dictionary<string, int> SeqGroups = new Dictionary<string, int>();
        }

        public class PusherTimer : Timer
        {
            public string CommPoint;
            public string[] telegram;
            public string destination;
            public string sortID;
            public string sortSeq;
            public string sortInfo;
            public bool running;

            public void ResetData()
            {
                telegram = null;
                destination = null;
                sortID = null;
                sortSeq = null;
                sortInfo = null;
            }

            public void AddData(string[] Telegram, string Destination, string SortID, string SortSeq, string SortInfo)
            {
                telegram = Telegram;
                destination = Destination;
                sortID = SortID;
                sortSeq = SortSeq;
                sortInfo = SortInfo;
            }
        }
    }
}
