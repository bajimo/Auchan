using System;
using System.Collections.Generic;
using System.ComponentModel;
using Dematic.ATC;
using Experior.Catalog.Assemblies.Storage;
using Experior.Catalog.Dematic.Storage.PalletCrane.Assemblies;
using Experior.Core.Assemblies;
using Experior.Dematic.Base;

namespace Experior.Catalog.Dematic.ATC.Assemblies.Storage
{
    public class MHEController_PalletCrane : BaseATCController, IController, IPalletController
    {
        MHEController_PalletCraneATCInfo palletCraneATCInfo;
        List<MHEControl> controls = new List<MHEControl>();

        public MHEController_PalletCrane(MHEController_PalletCraneATCInfo info) : base(info)
        {
            palletCraneATCInfo = info;
        }

        public BasePalletData GetPalletData()
        {
            return new ATCPalletData();
        }

        public MHEControl CreateMHEControl(IControllable assem, ProtocolInfo info)
        {
            MHEControl protocolConfig = null;  //generic plc config object
            Dictionary<string, Type> dt = new Dictionary<string, Type>();

            if (assem is PalletCrane)
            {
                protocolConfig = CreateMHEControlGeneric<PalletCraneATCInfo, MHEControl_PalletCrane>(assem, info);
            }

            else
            {
                Experior.Core.Environment.Log.Write("Can't create MHE Control, object is not defined in the 'CreateMHEControl' of the controller");
                return null;
            }
            //......other assemblies should be added here....do this with generics...correction better to do this with reflection...That is BaseController should use reflection
            //and not generics as we do not know the types at design time and it means that the above always has to be edited when adding a new MHE control object.
            protocolConfig.ParentAssembly = (Assembly)assem;
            controls.Add(protocolConfig);
            return protocolConfig as MHEControl;
        }

        public override void HandleTelegrams(string[] telegramFields, TelegramTypes type)
        {
            if (controls.Count != 1)
            {
                Log.Write(string.Format("Controller {0} has no 'MHE_Control' or Too many 'MHE_Controls' have been created, cannot process the telegram, restart the model to resolve the issue", Name));
                return;
            }

            var control = controls[0] as MHEControl_PalletCrane;
            switch (type)
            {
                case TelegramTypes.StartTransportTelegram:
                    control.StartTransportTelegramReceived(telegramFields);
                    break;
                //case TelegramTypes.StartMultipleTransportTelegram:
                //    control.StartMultipleTransportTelegramReceived(telegramFields);
                //    break;
                case TelegramTypes.RequestStateTelegram:
                    control.RequestStateTelegramReceived(telegramFields);
                    break;
                default:
                    break;
            }
        }

        #region Send Telegrams
        public void SendLocationArrivedTelegram(IATCLoadType load)
        {
            string telegram = CreateTelegramFromLoad(TelegramTypes.LocationArrivedTelegram, load);
            SendTelegram(telegram, true);
        }

        public void SendTransportRequestTelegram(IATCLoadType load)
        {
            string telegram = CreateTelegramFromLoad(TelegramTypes.TransportRequestTelegram, load);
            SendTelegram(telegram, true);
        }
        #endregion

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void Reset()
        {
            base.Reset();
            foreach (MHEControl control in controls)
            {
                if (control is MHEControl_PalletCrane)
                {
                    ((MHEControl_PalletCrane)control).Reset();
                }
            }
        }

        public void RemoveSSCCBarcode(string ULID) { }
    }

    [Serializable]
    [TypeConverter(typeof(MHEController_PalletCraneATCInfo))]
    public class MHEController_PalletCraneATCInfo : BaseATCControllerInfo { }
}