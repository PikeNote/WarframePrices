using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace WarframePrice
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void bunifuDropdown1_onItemSelected(object sender, EventArgs e)
        {
            switch (bunifuDropdown1.selectedIndex)
            {
                case 0:
                    Form1.switchItemOne = true;
                    Form1.switchItemTwo = false;
                    Form1.switchItemThree = false;
                    Form1.switchItemFour = false;
                    break;
                case 1:
                    Form1.switchItemOne = false;
                    Form1.switchItemTwo = true;
                    Form1.switchItemThree = false;
                    Form1.switchItemFour = false;
                    break;
                case 2:
                    Form1.switchItemOne = false;
                    Form1.switchItemTwo = false;
                    Form1.switchItemThree = true;
                    Form1.switchItemFour = false;
                    break;
                case 3:
                    Form1.switchItemOne = false;
                    Form1.switchItemTwo = false;
                    Form1.switchItemThree = false;
                    Form1.switchItemFour = true;
                    break;
                default:
                    break;
            }
                
        }

        private void bunifuImageButton2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
