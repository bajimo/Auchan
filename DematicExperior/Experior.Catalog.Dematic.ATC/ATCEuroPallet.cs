﻿using System;
using System.Collections.Generic;
using Experior.Dematic.Base;
using System.ComponentModel;
using Experior.Core.Properties;
using Dematic.ATC;

namespace Experior.Catalog.Dematic.ATC
{
    public class ATCEuroPallet : EuroPallet, IATCPalletLoadType
    {     
        public ATCEuroPallet(EuroPalletInfo info): base(info)
        { }

        #region Public Overrides

        //SSCC barcode is not used for ATC control as the TU Ident is used instead
        [Browsable(false)]
        public override string SSCCBarcode
        {
            get
            {
                return base.SSCCBarcode;
            }
            set
            {
                base.SSCCBarcode = value;
            }
        }

        //Not interesting for the user (reduce noise)
        [Browsable(false)]
        public override float Angle
        {
            get
            {
                return base.Angle;
            }
            set
            {
                base.Angle = value;
            }
        }

        [Browsable(false)]
        public override string Identification
        {
            get
            {
                return base.Identification;
            }
            set
            {
                base.Identification = value;
            }
        }

        public override void DoubleClick()
        {
            if (ProjectFields.Count > 0 && ProjectFields.Count < 6)
            {
                ProjectFieldTools projectFields = new ATC.ProjectFieldTools(this);
                projectFields.ShowDialog();
            }
        }

        #endregion

        #region Implement IATCLoadType

        private Dictionary<string, string> _ProjectFields = new Dictionary<string, string>();
        [Category("ATC")]
        [DisplayName("ProjectFields")]
        [PropertyOrder(0)]
        [Experior.Core.Properties.AlwaysEditable]
        public Dictionary<string,string> ProjectFields
        {
            get { return _ProjectFields; }
            set { _ProjectFields = value; }
        }

        private string _TUIdent;
        [Category("ATC")]
        [DisplayName("TU Ident")]
        [ReadOnly(false)]
        [Experior.Core.Properties.AlwaysEditable]
        [PropertyOrder(1)]
        public string TUIdent
        {
            get { return _TUIdent; }
            set 
            { 
                _TUIdent = value;
                Identification = value;
            }
        }

        private string _TUType;
        [Category("ATC")]
        [DisplayName("TU Type")]
        [ReadOnly(false)]
        [Experior.Core.Properties.AlwaysEditable]
        [PropertyOrder(2)]
        public string TUType
        {
            get { return _TUType; }
            set { _TUType = value; }
        }

        private string _Source;
        [Category("ATC")]
        [DisplayName("Source")]
        [ReadOnly(false)]
        [Experior.Core.Properties.AlwaysEditable]
        [PropertyOrder(3)]
        public string Source
        {
            get { return _Source; }
            set { _Source = value; }
        }

        private string _Destination;
        [Category("ATC")]
        [DisplayName("Destination")]
        [ReadOnly(false)]
        [Experior.Core.Properties.AlwaysEditable]
        [PropertyOrder(4)]
        public string Destination
        {
            get { return _Destination; }
            set { _Destination = value; }
        }

        private string _PresetStateCode;
        [Category("ATC")]
        [DisplayName("PresetStateCode")]
        [ReadOnly(false)]
        [Experior.Core.Properties.AlwaysEditable]
        [PropertyOrder(5)]
        public string PresetStateCode
        {
            get { return _PresetStateCode; }
            set { _PresetStateCode = value; }
        }

        private string _Location;
        [Category("ATC")]
        [DisplayName("Last Location")]
        [ReadOnly(true)]
        [PropertyOrder(7)]
        public string Location
        {
            get { return _Location; }
            set { _Location = value; }
        }
        
        public string GetPropertyValueFromEnum(TelegramFields field)
        {
            switch (field)
            {
                case TelegramFields.tuIdent:     return TUIdent;
                case TelegramFields.tuType:      return TUType;
                //Pallet TelegramFields.mts:         return MTS;
                case TelegramFields.destination: return Destination;
                case TelegramFields.source:      return Source;
                case TelegramFields.location:    return Location;
                case TelegramFields.height:      return (Height * 1000).ToString();
                case TelegramFields.width:       return (Width * 1000).ToString();
                case TelegramFields.length:      return (Length * 1000).ToString();
                case TelegramFields.weight:      return (PalletWeight * 1000).ToString();
            }
            return null;
        }

        #endregion

        #region Implement IATCPalletLoadType

        //Weight on the load doesn't seem to work so have added this and controlled directly in ATC code when creating a load
        private float _PalletWeight;
        [Category("Size")]
        [DisplayName("Weight")]
        [PropertyOrder(10)]
        public float PalletWeight
        {
            get
            {
                return _PalletWeight;
            }
            set
            {
                _PalletWeight = value;
            }
        }

        /// <summary>
        /// Set the Yaw of the load based on the conveyor type
        /// </summary>
        public void SetYaw(PalletConveyorType conveyorType)
        {
            switch (conveyorType)
            {
                case PalletConveyorType.Chain:
                    Yaw = (float)(Math.PI / 2);
                    break;
                default:
                    Yaw = 0;
                    break;
            }
        }

        public void AddLoad(float width, float height, float length)
        {
            LoadLength = length;
            LoadHeight = height;
            LoadWidth = width;
            LoadPallet();
        }

        #endregion
    }
}
