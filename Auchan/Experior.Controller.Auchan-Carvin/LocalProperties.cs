using System.ComponentModel;
using System.Reflection;

namespace Experior.Controller.AuchanCarvin
{
    enum IdentificationMode
    {
        Normal,
        Manual,
        Enhanced
    }

    enum DepalMode
    {
        Normal,
        Exception
    }

    class LocalProperties
    {
        private AuchanCarvinRouting Routing;



        public LocalProperties(AuchanCarvinRouting routing)
        {
            Routing = routing;
        }

        private IdentificationMode _iDMode = IdentificationMode.Normal;
        [DisplayName("ID Station Mode")]
        [Description("Decide what mode the Identification Stations at CCRE01NP01 should be working in")]
        public IdentificationMode iDMode
        {
            get { return _iDMode; }
            set { _iDMode = value; }
        }

        private DepalMode _CCDE01WS01 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE01WS01
        {
            get { return _CCDE01WS01; }
            set { _CCDE01WS01 = value; }
        }

        private DepalMode _CCDE01WS02 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE01WS02
        {
            get { return _CCDE01WS02; }
            set { _CCDE01WS02 = value; }
        }


        private DepalMode _CCDE02WS01 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE02WS01
        {
            get { return _CCDE02WS01; }
            set { _CCDE02WS01 = value; }
        }

        private DepalMode _CCDE02WS02 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE02WS02
        {
            get { return _CCDE02WS02; }
            set { _CCDE02WS02 = value; }
        }


        private DepalMode _CCDE03WS01 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE03WS01
        {
            get { return _CCDE03WS01; }
            set { _CCDE03WS01 = value; }
        }

        private DepalMode _CCDE03WS02 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE03WS02
        {
            get { return _CCDE03WS02; }
            set { _CCDE03WS02 = value; }
        }


        private DepalMode _CCDE04WS01 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE04WS01
        {
            get { return _CCDE04WS01; }
            set { _CCDE04WS01 = value; }
        }

        private DepalMode _CCDE04WS02 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE04WS02
        {
            get { return _CCDE04WS02; }
            set { _CCDE04WS02 = value; }
        }


        private DepalMode _CCDE05WS01 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE05WS01
        {
            get { return _CCDE05WS01; }
            set { _CCDE05WS01 = value; }
        }

        private DepalMode _CCDE05WS02 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE05WS02
        {
            get { return _CCDE05WS02; }
            set { _CCDE05WS02 = value; }
        }


        private DepalMode _CCDE06WS01 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE06WS01
        {
            get { return _CCDE06WS01; }
            set { _CCDE06WS01 = value; }
        }

        private DepalMode _CCDE06WS02 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE06WS02
        {
            get { return _CCDE06WS02; }
            set { _CCDE06WS02 = value; }
        }

        private DepalMode _CCDE07WS01 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE07WS01
        {
            get { return _CCDE07WS01; }
            set { _CCDE07WS01 = value; }
        }

        private DepalMode _CCDE07WS02 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE07WS02
        {
            get { return _CCDE07WS02; }
            set { _CCDE07WS02 = value; }
        }


        private DepalMode _CCDE08WS01 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE08WS01
        {
            get { return _CCDE08WS01; }
            set { _CCDE08WS01 = value; }
        }

        private DepalMode _CCDE08WS02 = DepalMode.Normal;
        [Category("Depal Mode")]
        public DepalMode CCDE08WS02
        {
            get { return _CCDE08WS02; }
            set { _CCDE08WS02 = value; }
        }

        public float _StackFullTime = 1f;
        [DisplayName("Stack Full Timeout")]
        [Description("How often stacks are sent to the multishuttle when the stack becomes full")]
        public float StackFullTime
        {
            get { return _StackFullTime; }
            set { _StackFullTime = value; }
        }

        public float _StackBufferTime = 10f;
        [DisplayName("Stack Buffer Interval")]
        [Description("How often the unit filling telegram is sent")]
        public float StackBufferTime
        {
            get { return _StackBufferTime; }
            set
            {
                if (value < 200)
                {
                    _StackBufferTime = value;
                }
            }
        }

        public float _PickTimer = 5f;
        [DisplayName("Picking Rate")]
        [Description("How long does it take to palletise a single item")]
        public float PickTimer
        {
            get { return _PickTimer; }
            set
            {
                if (value < 100)
                {
                    _PickTimer = value;
                    Routing.PickerTimeChange(value);
                }
            }
        }
    }
}
