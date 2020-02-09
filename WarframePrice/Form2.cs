using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace WarframePrice
{
    public partial class Form2 : Form
    {

        public Form2(JObject itemDatas)
        {
            InitializeComponent();
            JArray objData = (JArray)itemDatas["itemdata"];
            JArray pricedata = (JArray)itemDatas["pricedata"];

            Label[] titles = {Title1, Title2, Title3, Title4};
            Label[] prices = { Price1, Price2, Price3, Price4 };
            PictureBox[] picBox = { pictureBox1, pictureBox2, pictureBox3, pictureBox4 };

            for (int i=0; i<objData.Count;i++)
            {
                
                titles[i].Text = (string) objData[i];
                prices[i].Text = (string) pricedata[i];

                titles[i].Visible = true;
                prices[i].Visible = true;
                picBox[i].Visible = true;

            }

            if (Form1.switchItemOne)
            {
                RE.Text = "(90 Day Moving Average)";
            } else if (Form1.switchItemTwo || Form1.switchItemThree)
            {
                RE.Text = "(Min Buyer - Max Seller)";
            } else
            {
                RE.Text = "(2 Day Moving Average)";
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void bunifuImageButton1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
