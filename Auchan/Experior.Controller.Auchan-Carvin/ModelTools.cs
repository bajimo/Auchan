using Experior.Controller.AuchanCarvin;
using System.Windows.Forms;

namespace Experior.Controller
{
    public partial class ModelTools : Form
    {
        AuchanCarvinRouting routingScript;

        public ModelTools(AuchanCarvinRouting routing)
        {
            InitializeComponent();
            routingScript = routing;
        }

        private void btnLift_Click(object sender, System.EventArgs e)
        {
            // Get lift number
            Button button = (Button)sender;
            var liftNumber = button.Text;
            // Call End Pallet Operation from routing script
            routingScript.EndPalletOperation(liftNumber);
        }

        private void reset_Click(object sender, System.EventArgs e)
        {
            //This is to reset the sequence number on the push too position
            routingScript.ResetPusher(string.Format("PUSHED{0}", ((Button)sender).Name.Substring(6, 4)));
        }

        private void manualPsuh_Click(object sender, System.EventArgs e)
        {
            routingScript.ManualPush(string.Format("PUSHER{0}", ((Button)sender).Name.Substring(8, 2)));
        }

        private void debugPusher_Click(object sender, System.EventArgs e)
        {
            routingScript.DebugPusher(string.Format("PUSHER{0}", ((Button)sender).Name.Substring(8, 2)));
        }

        private void forcepalletising_Click(object sender, System.EventArgs e)
        { 
            routingScript.ForcePalletising(((Button)sender).Name.Substring(8, 1)); //btnPallnn
        }
        
    }
}
