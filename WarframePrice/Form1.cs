using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using NHotkey.WindowsForms;
using Tesseract;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;


namespace WarframePrice
{
    public partial class Form1 : Form
    {
        private readonly HashSet<string> relicItems = new HashSet<string>();

        private readonly HttpClient client = new HttpClient();

        private readonly Dictionary<string, Item> itemLookup = new Dictionary<string, Item>();

        public static bool switchItemOne = true;
        public static bool switchItemTwo = false;
        public static bool switchItemThree = false;
        public static bool switchItemFour = false;

        struct Item
        {
            public string prettyName;
            public string urlName;
        }

        public Form1()
        {
            client.DefaultRequestHeaders.Add("Accept", "application/xhtml+xml, */*");
            client.DefaultRequestHeaders.Add("Platform", "pc");
            client.DefaultRequestHeaders.Add("Language", "en");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36");
            HotkeyManager.Current.AddOrReplace("TakeImage", Keys.Oem5, OnTakeImage);
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            HttpResponseMessage ptResponse = await client.GetAsync("https://raw.githubusercontent.com/WFCD/warframe-drop-data/gh-pages/data/relics.json");
            ptResponse.EnsureSuccessStatusCode();
            string jsonPT = await ptResponse.Content.ReadAsStringAsync();

            JObject jsonPTS = JObject.Parse(jsonPT);
            JArray relicSet = (JArray)jsonPTS["relics"];

            foreach (var relicItem in relicSet)
            {
                foreach (var reward in relicItem["rewards"])
                {
                    relicItems.Add((string)reward["itemName"]);
                }
            }

            HttpResponseMessage response = await client.GetAsync("https://api.warframe.market/v1/items");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JObject jsonO = JObject.Parse(responseBody);
            JArray jsonDat = (JArray)jsonO["payload"]["items"];

            foreach (var item in jsonDat)
            {
                var itemName = (string)item["item_name"];
                TextInfo cultInfo = new CultureInfo("en-US", false).TextInfo;
                string output = cultInfo.ToTitleCase(itemName);

                if (relicItems.Contains(itemName + " Blueprint"))
                {
                    itemName += " Blueprint";
                }
                else if (relicItems.Contains(output))
                {
                    itemName = output;
                }
                else if (itemName == "Kavasa Prime Collar Blueprint")
                {
                    itemName = "Kavasa Prime Kubrow Collar Blueprint";
                }
                else if (itemName == "Kavasa Prime Collar Buckle")
                {
                    itemName = "Kavasa Prime Buckle";
                }
                else if (itemName == "Kavasa Prime Collar Band")
                {
                    itemName = "Kavasa Prime Band";
                }
                else if (itemName == "Amber Ayatan Star")
                {
                    itemName = "Ayatan Amber Star";
                }

                if (relicItems.Contains(itemName))
                {
                    Item itemStruct;
                    itemStruct.prettyName = itemName;
                    itemStruct.urlName = (string)item["url_name"];
                    this.itemLookup.Add(this.PrepareString(itemName), itemStruct);
                }

            }
        }

        private readonly Regex removeNonAlphanumerics = new Regex("[^a-z0-9]");
        private string PrepareString(string str)
        {
            str = str.ToLower();
            str = this.removeNonAlphanumerics.Replace(str, "");
            str = str.Replace("l", "i");
            str = str.Replace("0", "o");
            str = str.Replace("1", "i");
            return str;
        }

        private bool CheckLine(Bitmap bitmap, int x, int sectionWidth, bool bottomLineOnly, out Item item)
        {
            int y, height;
            if (bottomLineOnly)
            {
                y = bitmap.Height / 2;
                height = bitmap.Height - bitmap.Height / 2;
            }
            else
            {
                y = 0;
                height = bitmap.Height;
            }
            if (x + sectionWidth > bitmap.Width)
            {
                sectionWidth = bitmap.Width - x;
            }
            Page p = this.ocr.Process(bitmap, new Rect(x, y, sectionWidth, height));
            string text = this.PrepareString(p.GetText());
            p.Dispose();
            if (this.itemLookup.ContainsKey(text))
            {
                this.itemLookup.TryGetValue(text, out item);
                return true;
            }
            item = new Item();
            return false;
        }

        private bool CheckSection(Bitmap bitmap, int x, int sectionWidth, out Item item)
        {
            if (this.CheckLine(bitmap, x, sectionWidth, true, out item))
            {
                return true;
            }
            return this.CheckLine(bitmap, x, sectionWidth, false, out item);
        }

        private readonly TesseractEngine ocr = new TesseractEngine("", "eng", EngineMode.TesseractAndLstm);

        private async void OnTakeImage(object sender, NHotkey.HotkeyEventArgs e)
        {
            int cutWidth = Screen.PrimaryScreen.Bounds.Width / 2;
            int sectionWidth = (int)Math.Round(0.24475 * cutWidth);
            int spacingWidth = (int)Math.Round(0.007 * cutWidth);
            int cutHeight = sectionWidth / 5;
            Bitmap bitmap = new Bitmap(cutWidth, cutHeight);
            Graphics g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(cutWidth / 2, 413, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
            List<Item> ActiveList = new List<Item>(4);
            Item item;

            for (int i = 0; i < 4; i++)
            {
                if (this.CheckSection(bitmap, i * (sectionWidth + spacingWidth), sectionWidth, out item))
                {
                    ActiveList.Add(item);
                }
            }
            if (ActiveList.Count == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (this.CheckSection(bitmap, i * (sectionWidth + spacingWidth) + sectionWidth / 2, sectionWidth, out item))
                    {
                        ActiveList.Add(item);
                    }
                }
            }
            bitmap.Dispose();

            if (ActiveList.Count == 0)
                return;

            JObject itemData = new JObject();
            JArray itemlist = new JArray();
            JArray pricelist = new JArray();

            List<Task<HttpResponseMessage>> responses = new List<Task<HttpResponseMessage>>(ActiveList.Count);

            for (int i = 0; i < ActiveList.Count; i++)
            {
                var itemName = ActiveList[i].urlName;

                if (switchItemOne || switchItemFour)
                {
                    responses.Add(client.GetAsync($"https://api.warframe.market/v1/items/{itemName}/statistics"));
                }
                else
                {
                    responses.Add(client.GetAsync($"https://api.warframe.market/v1/items/{itemName}/orders"));
                }
            }

            for (int i = 0; i < ActiveList.Count; i++)
            {
                var itemName = ActiveList[i].urlName;

                try
                {
                    if (switchItemOne)
                    {
                        HttpResponseMessage response = await responses[i];
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();

                        JObject jsonO = JObject.Parse(responseBody);

                        itemlist.Add(ActiveList[i].prettyName);

                        var records = (JArray)jsonO["payload"]["statistics_closed"]["90days"];

                        pricelist.Add(records[records.Count - 1]["moving_avg"]);
                    }
                    else if (switchItemTwo || switchItemThree)
                    {
                        HttpResponseMessage response = await responses[i];
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();

                        JObject jsonO = JObject.Parse(responseBody);

                        JArray jsonD = (JArray)jsonO["payload"]["orders"];
                        JArray jsonS = new JArray();
                        JArray jsonB = new JArray();

                        for (int io = 0; io < jsonD.Count; io++)
                        {
                            if ((bool)jsonD[io]["visible"])
                            {
                                if (!switchItemThree)
                                {
                                    if ((string)jsonD[io]["user"]["status"] == "online" || (string)jsonD[io]["user"]["status"] == "ingame")
                                    {
                                        if ((string)jsonD[io]["order_type"] == "buy")
                                        {
                                            jsonB.Add(jsonD[io]);
                                        }
                                        else
                                        {
                                            jsonS.Add(jsonD[io]);
                                        }
                                    }

                                }
                                else
                                {
                                    if ((string)jsonD[io]["order_type"] == "buy")
                                    {
                                        jsonB.Add(jsonD[io]);
                                    }
                                    else
                                    {
                                        jsonS.Add(jsonD[io]);
                                    }
                                }

                            }
                        }
                        var itemPrice = new JArray(jsonS.OrderBy(obj => (int)obj["platinum"]));
                        var buyerPrice = new JArray(jsonB.OrderBy(obj => (int)obj["platinum"]));
                        itemlist.Add(ActiveList[i].prettyName);

                        var bPrice = 0;
                        var sPrice = 0;

                        if ((int)buyerPrice.Count != 0)
                        {
                            bPrice = (int)buyerPrice[(int)buyerPrice.Count - 1]["platinum"];
                        }
                        if ((int)itemPrice.Count != 0)
                        {
                            sPrice = (int)itemPrice[0]["platinum"];
                        }
                        if (bPrice == 1 && sPrice == 1)
                        {
                            pricelist.Add("1");
                        }
                        else
                        {
                            pricelist.Add($"{bPrice}-{sPrice}");
                        }

                    } else
                    {
                        HttpResponseMessage response = await responses[i];
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();

                        JObject jsonO = JObject.Parse(responseBody);

                        itemlist.Add(ActiveList[i].prettyName);

                        var records = (JArray)jsonO["payload"]["statistics_closed"]["48hours"];

                        pricelist.Add(records[records.Count - 1]["moving_avg"]);
                    }
                }
                catch (InvalidCastException ee)
                {
                    Debug.Write(ee);
                }

            }

            itemData.Add("itemdata", itemlist);
            itemData.Add("pricedata", pricelist);

            FormCollection fc = Application.OpenForms;

            Form frmc = Application.OpenForms["Form2"];

            if (frmc != null)
                frmc.Close();

            Form2 form = new Form2(itemData);
            form.Show();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - form.Width,
             Screen.PrimaryScreen.WorkingArea.Height - form.Height);
        }

        private void bunifuImageButton1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void bunifuImageButton2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void bunifuImageButton3_Click(object sender, EventArgs e)
        {
            Form frmc = Application.OpenForms["cont"];

            if (frmc == null)
            {
                Form3 form = new Form3();
                form.Show();
            }
        }
    }


}