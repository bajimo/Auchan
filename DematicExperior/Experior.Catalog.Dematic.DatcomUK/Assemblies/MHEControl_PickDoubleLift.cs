﻿using Experior.Catalog.Dematic.Case.Components;
using Experior.Core.Assemblies;
using Experior.Core.Properties;
using Experior.Dematic.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Drawing;
using Experior.Core;
using Experior.Core.Loads;

namespace Experior.Catalog.Dematic.DatcomUK.Assemblies
{
    class MHEControl_PickDoubleLift : MHEControl
    {
        private PickDoubleLiftDatcomInfo transferDatcomInfo;
        private PickDoubleLift theLift;
        private CasePLC_Datcom casePLC;

        public MHEControl_PickDoubleLift(PickDoubleLiftDatcomInfo info, PickDoubleLift lift)
        {
            Info               = info;  // set this to save properties 
            transferDatcomInfo = info;
            theLift            = lift;
            casePLC            = lift.Controller as CasePLC_Datcom;

            //Add event subscriptions here
            theLift.OnArrivedAtPosition1 += TheLift_OnArrivedAtPosition1;
            theLift.OnArrivedAtPosition2 += TheLift_OnArrivedAtPosition2;
            casePLC.OnCallForwardTelegramReceived += CasePLC_OnCallForwardTelegramReceived;
        }

        public MHEControl_PickDoubleLift()
        {

        }

        public override void Dispose()
        {
            //Add event un-subscriptions here
            theLift.OnArrivedAtPosition1 -= TheLift_OnArrivedAtPosition1;
            theLift.OnArrivedAtPosition2 -= TheLift_OnArrivedAtPosition2;

            theLift = null;
            transferDatcomInfo = null;
        }   

        private void TheLift_OnArrivedAtPosition1(object sender, LiftArrivalArgs e)
        {
            casePLC.SendDivertConfirmation(Pos1Name, ((Case_Load)e._load).SSCCBarcode);
        }

        private void TheLift_OnArrivedAtPosition2(object sender, LiftArrivalArgs e)
        {
            casePLC.SendDivertConfirmation(Pos2Name, ((Case_Load)e._load).SSCCBarcode);
        }

        private void CasePLC_OnCallForwardTelegramReceived(object sender, CallForwardEventArgs e)
        {
            if (e._location == Pos1Name && theLift.Upper1Barcode != null && theLift.Upper1Barcode == e._barcode)
            {
                theLift.SendAwayPosition1();
            }

            if (e._location == Pos2Name && theLift.Upper2Barcode != null && theLift.Upper2Barcode == e._barcode)
            {
                theLift.SendAwayPosition2();
            }
        }

        [DisplayName("Position 1 Name")]
        [Description("Name of the Right Hand Side Conveyor - from picker poin of view")]
        [PropertyOrder(1)]
        public string Pos1Name
        {
            get { return transferDatcomInfo.pos1Name; }
            set { transferDatcomInfo.pos1Name = value; }
        }

        [DisplayName("Position 2 Name")]
        [Description("Name of the Left Hand Side Conveyor - from picker point of view")]
        [PropertyOrder(2)]
        public string Pos2Name
        {
            get { return transferDatcomInfo.pos2Name; }
            set { transferDatcomInfo.pos2Name = value; }
        }
    }

    [Serializable]
    [XmlInclude(typeof(PickDoubleLiftDatcomInfo))]
    public class PickDoubleLiftDatcomInfo : ProtocolInfo
    {
        public string pos1Name;
        public string pos2Name;
    }
}
