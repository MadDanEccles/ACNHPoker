﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ACNHPoker
{
    public partial class map : Form
    {
        private static Socket s;
        private USBBot bot;

        private DataTable source;
        private DataTable recipeSource;
        private DataTable flowerSource;
        private DataTable variationSource;
        private DataTable favSource;
        private DataTable fieldSource;
        private floorSlot selectedButton = null;
        private floorSlot[] floorSlots;
        private Form1 main;
        private variation selection = null;
        private miniMap MiniMap = null;
        public bulkSpawn bulk = null;
        private int counter = 0;

        private DataGridViewRow lastRow = null;
        private string imagePath;

        private const string csvFolder = @"csv\";
        private const string fieldFile = @"field.csv";
        private const string fieldPath = csvFolder + fieldFile;

        private Dictionary<string, string> OverrideDict;

        private int anchorX = -1;
        private int anchorY = -1;

        private DataTable currentDataTable;
        private bool sound;

        byte[] Layer1 = null;
        byte[] Layer2 = null;
        byte[] Acre = null;

        public map(Socket S, USBBot Bot, string itemPath, string recipePath, string flowerPath, string variationPath, string favPath, Form1 Main, string ImagePath, Dictionary<string, string> overrideDict, bool Sound)
        {
            try
            {
                s = S;
                bot = Bot;
                if (File.Exists(itemPath))
                    source = loadItemCSV(itemPath);
                if (File.Exists(recipePath))
                    recipeSource = loadItemCSV(recipePath);
                if (File.Exists(flowerPath))
                    flowerSource = loadItemCSV(flowerPath);
                if (File.Exists(variationPath))
                    variationSource = loadItemCSV(variationPath);
                if (File.Exists(favPath))
                    favSource = loadItemCSV(favPath, false);
                if (File.Exists(fieldPath))
                    fieldSource = loadItemCSV(fieldPath);
                main = Main;
                imagePath = ImagePath;
                OverrideDict = overrideDict;
                sound = Sound;
                floorSlots = new floorSlot[49];

                InitializeComponent();

                foreach (floorSlot btn in BtnPanel.Controls.OfType<floorSlot>())
                {
                    int i = int.Parse(btn.Tag.ToString());
                    floorSlots[i] = btn;
                }

                if (source != null)
                {
                    fieldGridView.DataSource = source;

                    //set the ID row invisible
                    fieldGridView.Columns["id"].Visible = false;
                    fieldGridView.Columns["iName"].Visible = false;
                    fieldGridView.Columns["jpn"].Visible = false;
                    fieldGridView.Columns["tchi"].Visible = false;
                    fieldGridView.Columns["schi"].Visible = false;
                    fieldGridView.Columns["kor"].Visible = false;
                    fieldGridView.Columns["fre"].Visible = false;
                    fieldGridView.Columns["ger"].Visible = false;
                    fieldGridView.Columns["spa"].Visible = false;
                    fieldGridView.Columns["ita"].Visible = false;
                    fieldGridView.Columns["dut"].Visible = false;
                    fieldGridView.Columns["rus"].Visible = false;
                    fieldGridView.Columns["color"].Visible = false;

                    //select the full row and change color cause windows blue sux
                    fieldGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                    fieldGridView.DefaultCellStyle.BackColor = Color.FromArgb(255, 47, 49, 54);
                    fieldGridView.DefaultCellStyle.ForeColor = Color.FromArgb(255, 114, 105, 110);
                    fieldGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 57, 60, 67);

                    fieldGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 57, 60, 67);
                    fieldGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                    fieldGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 57, 60, 67);

                    fieldGridView.EnableHeadersVisualStyles = false;

                    //create the image column
                    DataGridViewImageColumn imageColumn = new DataGridViewImageColumn
                    {
                        Name = "Image",
                        HeaderText = "Image",
                        ImageLayout = DataGridViewImageCellLayout.Zoom
                    };
                    fieldGridView.Columns.Insert(13, imageColumn);
                    imageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

                    fieldGridView.Columns["eng"].Width = 195;
                    fieldGridView.Columns["jpn"].Width = 195;
                    fieldGridView.Columns["tchi"].Width = 195;
                    fieldGridView.Columns["schi"].Width = 195;
                    fieldGridView.Columns["kor"].Width = 195;
                    fieldGridView.Columns["fre"].Width = 195;
                    fieldGridView.Columns["ger"].Width = 195;
                    fieldGridView.Columns["spa"].Width = 195;
                    fieldGridView.Columns["ita"].Width = 195;
                    fieldGridView.Columns["dut"].Width = 195;
                    fieldGridView.Columns["rus"].Width = 195;
                    fieldGridView.Columns["Image"].Width = 128;

                    fieldGridView.Columns["eng"].HeaderText = "Name";
                    fieldGridView.Columns["jpn"].HeaderText = "Name";
                    fieldGridView.Columns["tchi"].HeaderText = "Name";
                    fieldGridView.Columns["schi"].HeaderText = "Name";
                    fieldGridView.Columns["kor"].HeaderText = "Name";
                    fieldGridView.Columns["fre"].HeaderText = "Name";
                    fieldGridView.Columns["ger"].HeaderText = "Name";
                    fieldGridView.Columns["spa"].HeaderText = "Name";
                    fieldGridView.Columns["ita"].HeaderText = "Name";
                    fieldGridView.Columns["dut"].HeaderText = "Name";
                    fieldGridView.Columns["rus"].HeaderText = "Name";

                    currentDataTable = source;
                }

                this.BringToFront();
                this.Focus();
                this.KeyPreview = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void fetchMapBtn_Click(object sender, EventArgs e)
        {
            btnToolTip.RemoveAll();

            if ((s == null || s.Connected == false) & bot == null)
            {
                MessageBox.Show("Please connect to the switch first");
                return;
            }

            Thread LoadThread = new Thread(delegate () { fetchMap(Utilities.mapZero, Utilities.mapZero + Utilities.mapSize); });
            LoadThread.Start();
        }

        private void fetchMap(UInt32 layer1Address, UInt32 layer2Address)
        {
            showMapWait(42 * 2, "Fetching Map...");

            Layer1 = Utilities.getMapLayer(s, bot, layer1Address, ref counter);
            Layer2 = Utilities.getMapLayer(s, bot, layer2Address, ref counter);
            Acre = Utilities.getAcre(s, bot);

            if (MiniMap == null)
                MiniMap = new miniMap(Layer1, Acre);

            miniMapBox.BackgroundImage = MiniMap.combineMap(MiniMap.drawBackground(), MiniMap.drawItemMap());


            byte[] Coordinate = Utilities.getCoordinate(s, bot);
            int x = BitConverter.ToInt32(Coordinate, 0);
            int y = BitConverter.ToInt32(Coordinate, 4);

            anchorX = x - 0x24;
            anchorY = y - 0x18;

            if (anchorX < 3 || anchorY < 3 || anchorX > 108 || anchorY > 92)
                return;

            this.Invoke((MethodInvoker)delegate
            {
                displayAnchor(getMapColumns(anchorX, anchorY));

                xCoordinate.Text = x.ToString();
                yCoordinate.Text = y.ToString();
                enableBtn();
                fetchMapBtn.Visible = false;
            });

            hideMapWait();
        }

        private byte[][] getMapColumns(int x, int y)
        {
            byte[] Layer;
            if (layer1Btn.Checked)
                Layer = Layer1;
            else if (layer2Btn.Checked)
                Layer = Layer2;
            else
                return null;

            byte[][] floorByte = new byte[14][];
            for (int i = 0; i < 14; i++)
            {
                floorByte[i] = new byte[0x70];
                Buffer.BlockCopy(Layer, ((x - 3) * 2 + i) * 0x600 + (y - 3) * 0x10, floorByte[i], 0x0, 0x70);
            }

            return floorByte;
        }

        private void moveAnchor(int x, int y)
        {
            btnToolTip.RemoveAll();

            xCoordinate.Text = x.ToString();
            yCoordinate.Text = y.ToString();

            anchorX = x;
            anchorY = y;

            displayAnchor(getMapColumns(anchorX, anchorY));

            if (selectedButton != null)
                selectedButton.BackColor = System.Drawing.Color.LightSeaGreen;
        }

        private void displayAnchor(byte[][] floorByte)
        {
            miniMapBox.Image = MiniMap.drawSelectSquare(anchorX * 2, anchorY * 2);

            BtnSetup(floorByte[0], floorByte[1], (anchorX - 3), (anchorY - 3), floor1, floor2, floor3, floor4, floor5, floor6, floor7, 0, false);
            BtnSetup(floorByte[2], floorByte[3], (anchorX - 2), (anchorY - 3), floor8, floor9, floor10, floor11, floor12, floor13, floor14, 1, false);
            BtnSetup(floorByte[4], floorByte[5], (anchorX - 1), (anchorY - 3), floor15, floor16, floor17, floor18, floor19, floor20, floor21, 2, false);
            BtnSetup(floorByte[6], floorByte[7], (anchorX - 0), (anchorY - 3), floor22, floor23, floor24, floor25, floor26, floor27, floor28, 3, true);
            BtnSetup(floorByte[8], floorByte[9], (anchorX + 1), (anchorY - 3), floor29, floor30, floor31, floor32, floor33, floor34, floor35, 4, false);
            BtnSetup(floorByte[10], floorByte[11], (anchorX + 2), (anchorY - 3), floor36, floor37, floor38, floor39, floor40, floor41, floor42, 5, false);
            BtnSetup(floorByte[12], floorByte[13], (anchorX + 3), (anchorY - 3), floor43, floor44, floor45, floor46, floor47, floor48, floor49, 6, false);

            resetBtnColor();
        }

        private void BtnSetup(byte[] b, byte[] b2, int x, int y, floorSlot slot1, floorSlot slot2, floorSlot slot3, floorSlot slot4, floorSlot slot5, floorSlot slot6, floorSlot slot7, int colume, Boolean anchor = false)
        {
            byte[] idBytes = new byte[2];
            byte[] flag1Bytes = new byte[1];
            byte[] flag2Bytes = new byte[1];
            byte[] dataBytes = new byte[4];

            byte[] part2IdBytes = new byte[4];
            byte[] part2DataBytes = new byte[4];
            byte[] part3IdBytes = new byte[4];
            byte[] part3DataBytes = new byte[4];
            byte[] part4IdBytes = new byte[4];
            byte[] part4DataBytes = new byte[4];

            byte[] idFull = new byte[4];

            floorSlot currentBtn = null;

            for (int i = 0; i < 7; i++)
            {
                Buffer.BlockCopy(b, (i * 0x10) + 0x0, idBytes, 0x0, 0x2);
                Buffer.BlockCopy(b, (i * 0x10) + 0x2, flag2Bytes, 0x0, 0x1);
                Buffer.BlockCopy(b, (i * 0x10) + 0x3, flag1Bytes, 0x0, 0x1);
                Buffer.BlockCopy(b, (i * 0x10) + 0x4, dataBytes, 0x0, 0x4);

                Buffer.BlockCopy(b, (i * 0x10) + 0x8, part2IdBytes, 0x0, 0x4);
                Buffer.BlockCopy(b, (i * 0x10) + 0xC, part2DataBytes, 0x0, 0x4);
                Buffer.BlockCopy(b2, (i * 0x10) + 0x0, part3IdBytes, 0x0, 0x4);
                Buffer.BlockCopy(b2, (i * 0x10) + 0x4, part3DataBytes, 0x0, 0x4);
                Buffer.BlockCopy(b2, (i * 0x10) + 0x8, part4IdBytes, 0x0, 0x4);
                Buffer.BlockCopy(b2, (i * 0x10) + 0xC, part4DataBytes, 0x0, 0x4);

                string itemID = Utilities.flip(Utilities.ByteToHexString(idBytes));
                string flag2 = Utilities.ByteToHexString(flag2Bytes);
                string flag1 = Utilities.ByteToHexString(flag1Bytes);
                string itemData = Utilities.flip(Utilities.ByteToHexString(dataBytes));

                string part2Id = Utilities.flip(Utilities.ByteToHexString(part2IdBytes));
                string part2Data = Utilities.flip(Utilities.ByteToHexString(part2DataBytes));
                string part3Id = Utilities.flip(Utilities.ByteToHexString(part3IdBytes));
                string part3Data = Utilities.flip(Utilities.ByteToHexString(part3DataBytes));
                string part4Id = Utilities.flip(Utilities.ByteToHexString(part4IdBytes));
                string part4Data = Utilities.flip(Utilities.ByteToHexString(part4DataBytes));

                if (i == 0)
                {
                    currentBtn = slot1;
                    setBtn(slot1, itemID, itemData, part2Id, part2Data, part3Id, part3Data, part4Id, part4Data, flag1, flag2);
                }
                else if (i == 1)
                {
                    currentBtn = slot2;
                    setBtn(slot2, itemID, itemData, part2Id, part2Data, part3Id, part3Data, part4Id, part4Data, flag1, flag2);
                }
                else if (i == 2)
                {
                    currentBtn = slot3;
                    setBtn(slot3, itemID, itemData, part2Id, part2Data, part3Id, part3Data, part4Id, part4Data, flag1, flag2);
                }
                else if (i == 3)
                {
                    currentBtn = slot4;
                    setBtn(slot4, itemID, itemData, part2Id, part2Data, part3Id, part3Data, part4Id, part4Data, flag1, flag2);
                    if (anchor)
                    {
                        //slot4.BackColor = Color.Red;
                    }
                }
                else if (i == 4)
                {
                    currentBtn = slot5;
                    setBtn(slot5, itemID, itemData, part2Id, part2Data, part3Id, part3Data, part4Id, part4Data, flag1, flag2);
                }
                else if (i == 5)
                {
                    currentBtn = slot6;
                    setBtn(slot6, itemID, itemData, part2Id, part2Data, part3Id, part3Data, part4Id, part4Data, flag1, flag2);
                }
                else if (i == 6)
                {
                    currentBtn = slot7;
                    setBtn(slot7, itemID, itemData, part2Id, part2Data, part3Id, part3Data, part4Id, part4Data, flag1, flag2);
                }

                currentBtn.mapX = x;
                currentBtn.mapY = y + i;
            }
        }

        private void setBtn(floorSlot btn, string itemID, string itemData, string part2Id, string part2Data, string part3Id, string part3Data, string part4Id, string part4Data, string flag1, string flag2)
        {
            string Name = GetNameFromID(itemID, source);
            UInt16 ID = Convert.ToUInt16("0x" + itemID, 16);
            UInt32 Data = Convert.ToUInt32("0x" + itemData, 16);
            UInt32 IntP2Id = Convert.ToUInt32("0x" + part2Id, 16);
            UInt32 IntP2Data = Convert.ToUInt32("0x" + part2Data, 16);
            UInt32 IntP3Id = Convert.ToUInt32("0x" + part3Id, 16);
            UInt32 IntP3Data = Convert.ToUInt32("0x" + part3Data, 16);
            UInt32 IntP4Id = Convert.ToUInt32("0x" + part4Id, 16);
            UInt32 IntP4Data = Convert.ToUInt32("0x" + part4Data, 16);

            string P1Id = itemID;
            string P2Id = Utilities.turn2bytes(part2Id);
            string P3Id = Utilities.turn2bytes(part3Id);
            string P4Id = Utilities.turn2bytes(part4Id);

            string P1Data = Utilities.turn2bytes(itemData);
            string P2Data = Utilities.turn2bytes(part2Data);
            string P3Data = Utilities.turn2bytes(part3Data);
            string P4Data = Utilities.turn2bytes(part4Data);

            string Path1;
            string Path2;
            string Path3;
            string Path4;

            if (P1Id == "FFFD")
                Path1 = GetImagePathFromID(P1Data, source);
            else if (P1Id == "16A2")
            {
                Path1 = GetImagePathFromID(P1Data, recipeSource, Data);
                Name = GetNameFromID(P1Data, recipeSource);
            }
            else
                Path1 = GetImagePathFromID(itemID, source, Data);

            if (P2Id == "FFFD")
                Path2 = GetImagePathFromID(P2Data, source);
            else
                Path2 = GetImagePathFromID(P2Id, source, IntP2Data);

            if (P3Id == "FFFD")
                Path3 = GetImagePathFromID(P3Data, source);
            else
                Path3 = GetImagePathFromID(P3Id, source, IntP3Data);

            if (P4Id == "FFFD")
                Path4 = GetImagePathFromID(P4Data, source);
            else
                Path4 = GetImagePathFromID(P4Id, source, IntP4Data);

            btn.setup(Name, ID, Data, IntP2Id, IntP2Data, IntP3Id, IntP3Data, IntP4Id, IntP4Data, Path1, Path2, Path3, Path4, "", flag1, flag2);
        }

        private UInt32 getAddress(int x, int y)
        {
            return (UInt32)(Utilities.mapZero + (0xC00 * x) + (0x10 * (y)));
        }

        private void moveRightBtn_Click(object sender, EventArgs e)
        {
            int newX = anchorX + 1;
            int newY = anchorY;

            if (newX < 3 || newY < 3 || newX > 108 || newY > 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            moveAnchor(newX, newY);
        }

        private void moveLeftBtn_Click(object sender, EventArgs e)
        {
            int newX = anchorX - 1;
            int newY = anchorY;

            if (newX < 3 || newY < 3 || newX > 108 || newY > 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            moveAnchor(newX, newY);
        }

        private void moveDownBtn_Click(object sender, EventArgs e)
        {
            int newX = anchorX;
            int newY = anchorY + 1;

            if (newX < 3 || newY < 3 || newX > 108 || newY > 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            moveAnchor(newX, newY);
        }

        private void moveUpBtn_Click(object sender, EventArgs e)
        {
            int newX = anchorX;
            int newY = anchorY - 1;

            if (newX < 3 || newY < 3 || newX > 108 || newY > 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            moveAnchor(newX, newY);
        }

        private void moveUpRightBtn_Click(object sender, EventArgs e)
        {
            int newX = anchorX + 1;
            int newY = anchorY - 1;

            if (newX < 3 || newY < 3 || newX > 108 || newY > 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            moveAnchor(newX, newY);
        }

        private void moveDownRightBtn_Click(object sender, EventArgs e)
        {
            int newX = anchorX + 1;
            int newY = anchorY + 1;

            if (newX < 3 || newY < 3 || newX > 108 || newY > 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            moveAnchor(newX, newY);
        }

        private void moveDownLeftBtn_Click(object sender, EventArgs e)
        {
            int newX = anchorX - 1;
            int newY = anchorY + 1;

            if (newX < 3 || newY < 3 || newX > 108 || newY > 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            moveAnchor(newX, newY);
        }

        private void moveUpLeftBtn_Click(object sender, EventArgs e)
        {
            int newX = anchorX - 1;
            int newY = anchorY - 1;

            if (newX < 3 || newY < 3 || newX > 108 || newY > 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            moveAnchor(newX, newY);
        }

        private void moveUp7Btn_Click(object sender, EventArgs e)
        {
            if (anchorY <= 3)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            int newX = anchorX;
            int newY = anchorY - 7;

            if (newY < 3)
                newY = 3;

            moveAnchor(newX, newY);
        }

        private void moveRight7Btn_Click(object sender, EventArgs e)
        {
            if (anchorX >= 108)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            int newX = anchorX + 7;
            int newY = anchorY;

            if (newX > 108)
                newX = 108;

            moveAnchor(newX, newY);
        }

        private void moveDown7Btn_Click(object sender, EventArgs e)
        {
            if (anchorY >= 92)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            int newX = anchorX;
            int newY = anchorY + 7;

            if (newY > 92)
                newY = 92;

            moveAnchor(newX, newY);
        }

        private void moveLeft7Btn_Click(object sender, EventArgs e)
        {
            if (anchorX <= 3)
            {
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            int newX = anchorX - 7;
            int newY = anchorY;

            if (newX < 3)
                newX = 3;

            moveAnchor(newX, newY);
        }

        private void floor_MouseHover(object sender, EventArgs e)
        {
            var button = (floorSlot)sender;

            string locked;
            if (button.locked)
                locked = "✓ True";
            else
                locked = "✘ False";
            btnToolTip.SetToolTip(button,
                                    button.itemName +
                                    "\n\n" + "" +
                                    "ID : " + Utilities.precedingZeros(button.itemID.ToString("X"), 4) + "\n" +
                                    "Count : " + Utilities.precedingZeros(button.itemData.ToString("X"), 8) + "\n" +
                                    "Flag1 : 0x" + button.flag1 + "\n" +
                                    "Flag2 : 0x" + button.flag2 + "\n" +
                                    "Coordinate : " + button.mapX + " " + button.mapY + "\n\n" +
                                    "Part2 : " + button.part2.ToString("X") + " " + Utilities.precedingZeros(button.part2Data.ToString("X"), 8) + "\n" +
                                    "Part3 : " + button.part3.ToString("X") + " " + Utilities.precedingZeros(button.part3Data.ToString("X"), 8) + "\n" +
                                    "Part4 : " + button.part4.ToString("X") + " " + Utilities.precedingZeros(button.part4Data.ToString("X"), 8) + "\n" +
                                    "Locked : " + locked
                                    );
        }

        private string removeNumber(string filename)
        {
            char[] MyChar = { '0', '1', '2', '3', '4' };
            return filename.Trim(MyChar);
        }

        public string GetImagePathFromID(string itemID, DataTable source, UInt32 data = 0)
        {
            if (source == null)
            {
                return "";
            }

            if (fieldSource != null)
            {
                string path;

                DataRow FieldRow = fieldSource.Rows.Find(itemID);
                if (FieldRow != null)
                {
                    string imageName = FieldRow[1].ToString();

                    if (OverrideDict.ContainsKey(imageName))
                    {
                        path = imagePath + OverrideDict[imageName] + ".png";
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }

            }

            DataRow row = source.Rows.Find(itemID);
            DataRow VarRow = null;
            if (variationSource != null)
                VarRow = variationSource.Rows.Find(itemID);

            if (row == null)
            {
                return ""; //row not found
            }
            else
            {

                string path;
                if (VarRow != null & source != recipeSource)
                {
                    path = imagePath + VarRow["iName"] + ".png";
                    if (File.Exists(path))
                    {
                        return path;
                    }
                    string main = (data & 0xF).ToString();
                    string sub = (((data & 0xFF) - (data & 0xF)) / 0x20).ToString();
                    //Debug.Print("data " + data.ToString("X") + " Main " + main + " Sub " + sub);
                    path = imagePath + VarRow["iName"] + "_Remake_" + main + "_" + sub + ".png";
                    if (File.Exists(path))
                    {
                        return path;
                    }

                }

                string imageName = row[1].ToString();

                if (OverrideDict.ContainsKey(imageName))
                {
                    path = imagePath + OverrideDict[imageName] + ".png";
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                path = imagePath + imageName + ".png";
                if (File.Exists(path))
                {
                    return path;
                }
                else
                {
                    path = imagePath + imageName + "_Remake_0_0.png";
                    if (File.Exists(path))
                    {
                        return path;
                    }
                    else
                    {
                        path = imagePath + removeNumber(imageName) + ".png";
                        if (File.Exists(path))
                        {
                            return path;
                        }
                        else
                        {
                            return "";
                        }
                    }
                }
            }
        }

        public string GetNameFromID(string itemID, DataTable table)
        {
            if (fieldSource != null)
            {
                DataRow FieldRow = fieldSource.Rows.Find(itemID);
                if (FieldRow != null)
                {
                    return (string)FieldRow["name"];
                }
            }

            if (table == null)
            {
                return "";
            }

            DataRow row = table.Rows.Find(itemID);

            if (row == null)
            {
                return ""; //row not found
            }
            else
            {
                //row found set the index and find the name
                return (string)row["eng"];
            }
        }

        private void fieldGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            AddImage(fieldGridView, e);
        }

        private void AddImage(DataGridView Grid, DataGridViewCellFormattingEventArgs e)
        {
            if (Grid.Columns["Image"] == null)
                return;
            if (e.RowIndex >= 0 && e.RowIndex < Grid.Rows.Count)
            {
                if (e.ColumnIndex == Grid.Columns["Image"].Index)
                {
                    string path;
                    string imageName = Grid.Rows[e.RowIndex].Cells["iName"].Value.ToString();

                    if (OverrideDict.ContainsKey(imageName))
                    {
                        path = imagePath + OverrideDict[imageName] + ".png";
                        if (File.Exists(path))
                        {
                            Image img = Image.FromFile(path);
                            //e.CellStyle.BackColor = Color.Green;
                            e.Value = img;

                            return;
                        }
                    }

                    path = imagePath + imageName + ".png";
                    if (File.Exists(path))
                    {
                        Image img = Image.FromFile(path);
                        e.Value = img;
                    }
                    else
                    {
                        path = imagePath + imageName + "_Remake_0_0.png";
                        if (File.Exists(path))
                        {
                            Image img = Image.FromFile(path);
                            e.CellStyle.BackColor = Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(77)))), ((int)(((byte)(162)))));
                            e.Value = img;
                        }
                        else
                        {
                            path = imagePath + removeNumber(imageName) + ".png";
                            if (File.Exists(path))
                            {
                                Image img = Image.FromFile(path);
                                e.Value = img;
                            }
                            else
                            {
                                e.CellStyle.BackColor = Color.Red;
                            }
                        }
                    }
                }
            }
        }

        private void fieldGridView_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if (me.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (lastRow != null)
                {
                    lastRow.Height = 22;
                }

                if (e.RowIndex > -1)
                {
                    lastRow = fieldGridView.Rows[e.RowIndex];
                    fieldGridView.Rows[e.RowIndex].Height = 128;

                    if (currentDataTable == source)
                    {
                        string id = fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString();
                        string name = fieldGridView.Rows[e.RowIndex].Cells["eng"].Value.ToString();

                        IdTextbox.Text = id;
                        HexTextbox.Text = "00000000";

                        selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), 0x0, GetImagePathFromID(id, source), true, "");
                    }
                    else if (currentDataTable == recipeSource)
                    {
                        string id = "16A2"; // Recipe;
                        string name = fieldGridView.Rows[e.RowIndex].Cells["eng"].Value.ToString();
                        string hexValue = fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString();

                        IdTextbox.Text = id;
                        HexTextbox.Text = Utilities.precedingZeros(hexValue, 8);

                        selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(hexValue, recipeSource), true, "");
                    }
                    else if (currentDataTable == flowerSource)
                    {
                        string id = fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString();
                        string name = fieldGridView.Rows[e.RowIndex].Cells["eng"].Value.ToString();
                        string hexValue = fieldGridView.Rows[e.RowIndex].Cells["value"].Value.ToString();

                        IdTextbox.Text = id;
                        HexTextbox.Text = Utilities.precedingZeros(hexValue, 8);

                        selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(id, source), true, "");

                    }
                    else if (currentDataTable == favSource)
                    {
                        string id = fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString();
                        string name = fieldGridView.Rows[e.RowIndex].Cells["Name"].Value.ToString();
                        string hexValue = fieldGridView.Rows[e.RowIndex].Cells["value"].Value.ToString();

                        IdTextbox.Text = id;
                        HexTextbox.Text = Utilities.precedingZeros(hexValue, 8);

                        selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(id, source, Convert.ToUInt32("0x" + hexValue, 16)), true, "");
                    }
                    else if (currentDataTable == fieldSource)
                    {
                        string id = fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString();
                        string name = fieldGridView.Rows[e.RowIndex].Cells["Name"].Value.ToString();
                        string hexValue = fieldGridView.Rows[e.RowIndex].Cells["value"].Value.ToString();

                        IdTextbox.Text = id;
                        HexTextbox.Text = Utilities.precedingZeros(hexValue, 8);

                        selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(id, fieldSource), true, "");
                    }
                    if (selection != null)
                    {
                        selection.receiveID(Utilities.precedingZeros(selectedItem.fillItemID(), 4), "eng");
                    }
                    //updateSelectedItemInfo(selectedItem.displayItemName(), selectedItem.displayItemID(), selectedItem.displayItemData());

                }
            }
            else if (me.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (lastRow != null)
                {
                    lastRow.Height = 22;
                }

                if (e.RowIndex > -1)
                {
                    lastRow = fieldGridView.Rows[e.RowIndex];
                    fieldGridView.Rows[e.RowIndex].Height = 128;

                    string name = selectedItem.displayItemName();
                    string id = selectedItem.displayItemID();
                    string path = selectedItem.getPath();

                    if (IdTextbox.Text != "")
                    {
                        if (IdTextbox.Text == "315A" || IdTextbox.Text == "1618") // Wall-Mounted
                        {
                            HexTextbox.Text = Utilities.precedingZeros("00" + fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString(), 8);
                            selectedItem.setup(name, Convert.ToUInt16(id, 16), Convert.ToUInt32("0x" + HexTextbox.Text, 16), path, true, GetImagePathFromID(fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString(), source));
                        }
                        else
                        {
                            HexTextbox.Text = Utilities.precedingZeros(fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString(), 8);
                            selectedItem.setup(name, Convert.ToUInt16(id, 16), Convert.ToUInt32("0x" + HexTextbox.Text, 16), path, true, GetNameFromID(fieldGridView.Rows[e.RowIndex].Cells["id"].Value.ToString(), source));
                        }

                        if (selection != null)
                        {
                            selection.receiveID(Utilities.turn2bytes(selectedItem.fillItemData()), "eng");
                        }
                    }

                }
            }
        }

        private void itemModeBtn_Click(object sender, EventArgs e)
        {
            itemModeBtn.BackColor = Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(255)))));
            recipeModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            flowerModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            favModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            fieldModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));

            if (itemSearchBox.Text != "Search")
            {
                itemSearchBox.Clear();
            }

            fieldGridView.Columns.Remove("Image");

            if (source != null)
            {
                fieldGridView.DataSource = source;

                //set the ID row invisible
                fieldGridView.Columns["id"].Visible = false;
                fieldGridView.Columns["iName"].Visible = false;
                fieldGridView.Columns["jpn"].Visible = false;
                fieldGridView.Columns["tchi"].Visible = false;
                fieldGridView.Columns["schi"].Visible = false;
                fieldGridView.Columns["kor"].Visible = false;
                fieldGridView.Columns["fre"].Visible = false;
                fieldGridView.Columns["ger"].Visible = false;
                fieldGridView.Columns["spa"].Visible = false;
                fieldGridView.Columns["ita"].Visible = false;
                fieldGridView.Columns["dut"].Visible = false;
                fieldGridView.Columns["rus"].Visible = false;
                fieldGridView.Columns["color"].Visible = false;

                DataGridViewImageColumn imageColumn = new DataGridViewImageColumn
                {
                    Name = "Image",
                    HeaderText = "Image",
                    ImageLayout = DataGridViewImageCellLayout.Zoom
                };
                fieldGridView.Columns.Insert(13, imageColumn);
                imageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

                fieldGridView.Columns["eng"].Width = 195;
                fieldGridView.Columns["jpn"].Width = 195;
                fieldGridView.Columns["tchi"].Width = 195;
                fieldGridView.Columns["schi"].Width = 195;
                fieldGridView.Columns["kor"].Width = 195;
                fieldGridView.Columns["fre"].Width = 195;
                fieldGridView.Columns["ger"].Width = 195;
                fieldGridView.Columns["spa"].Width = 195;
                fieldGridView.Columns["ita"].Width = 195;
                fieldGridView.Columns["dut"].Width = 195;
                fieldGridView.Columns["rus"].Width = 195;
                fieldGridView.Columns["Image"].Width = 128;

                fieldGridView.Columns["eng"].HeaderText = "Name";
                fieldGridView.Columns["jpn"].HeaderText = "Name";
                fieldGridView.Columns["tchi"].HeaderText = "Name";
                fieldGridView.Columns["schi"].HeaderText = "Name";
                fieldGridView.Columns["kor"].HeaderText = "Name";
                fieldGridView.Columns["fre"].HeaderText = "Name";
                fieldGridView.Columns["ger"].HeaderText = "Name";
                fieldGridView.Columns["spa"].HeaderText = "Name";
                fieldGridView.Columns["ita"].HeaderText = "Name";
                fieldGridView.Columns["dut"].HeaderText = "Name";
                fieldGridView.Columns["rus"].HeaderText = "Name";

                currentDataTable = source;
            }

            FlagTextbox.Text = "20";
        }

        private void recipeModeBtn_Click(object sender, EventArgs e)
        {
            itemModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            recipeModeBtn.BackColor = Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(255)))));
            flowerModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            favModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            fieldModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));

            if (itemSearchBox.Text != "Search")
            {
                itemSearchBox.Clear();
            }

            fieldGridView.Columns.Remove("Image");

            if (recipeSource != null)
            {
                fieldGridView.DataSource = recipeSource;

                fieldGridView.Columns["id"].Visible = false;
                fieldGridView.Columns["iName"].Visible = false;
                fieldGridView.Columns["jpn"].Visible = false;
                fieldGridView.Columns["tchi"].Visible = false;
                fieldGridView.Columns["schi"].Visible = false;
                fieldGridView.Columns["kor"].Visible = false;
                fieldGridView.Columns["fre"].Visible = false;
                fieldGridView.Columns["ger"].Visible = false;
                fieldGridView.Columns["spa"].Visible = false;
                fieldGridView.Columns["ita"].Visible = false;
                fieldGridView.Columns["dut"].Visible = false;
                fieldGridView.Columns["rus"].Visible = false;

                DataGridViewImageColumn imageColumn = new DataGridViewImageColumn
                {
                    Name = "Image",
                    HeaderText = "Image",
                    ImageLayout = DataGridViewImageCellLayout.Zoom
                };
                fieldGridView.Columns.Insert(13, imageColumn);
                imageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

                fieldGridView.Columns["eng"].Width = 195;
                fieldGridView.Columns["jpn"].Width = 195;
                fieldGridView.Columns["tchi"].Width = 195;
                fieldGridView.Columns["schi"].Width = 195;
                fieldGridView.Columns["kor"].Width = 195;
                fieldGridView.Columns["fre"].Width = 195;
                fieldGridView.Columns["ger"].Width = 195;
                fieldGridView.Columns["spa"].Width = 195;
                fieldGridView.Columns["ita"].Width = 195;
                fieldGridView.Columns["dut"].Width = 195;
                fieldGridView.Columns["rus"].Width = 195;
                fieldGridView.Columns["Image"].Width = 128;

                fieldGridView.Columns["eng"].HeaderText = "Name";
                fieldGridView.Columns["jpn"].HeaderText = "Name";
                fieldGridView.Columns["tchi"].HeaderText = "Name";
                fieldGridView.Columns["schi"].HeaderText = "Name";
                fieldGridView.Columns["kor"].HeaderText = "Name";
                fieldGridView.Columns["fre"].HeaderText = "Name";
                fieldGridView.Columns["ger"].HeaderText = "Name";
                fieldGridView.Columns["spa"].HeaderText = "Name";
                fieldGridView.Columns["ita"].HeaderText = "Name";
                fieldGridView.Columns["dut"].HeaderText = "Name";
                fieldGridView.Columns["rus"].HeaderText = "Name";

                currentDataTable = recipeSource;
            }

            FlagTextbox.Text = "00";
        }

        private void flowerModeBtn_Click(object sender, EventArgs e)
        {
            itemModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            recipeModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            flowerModeBtn.BackColor = Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(255)))));
            favModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            fieldModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));

            if (itemSearchBox.Text != "Search")
            {
                itemSearchBox.Clear();
            }

            fieldGridView.Columns.Remove("Image");

            if (flowerSource != null)
            {
                fieldGridView.DataSource = flowerSource;

                fieldGridView.Columns["id"].Visible = false;
                fieldGridView.Columns["iName"].Visible = false;
                fieldGridView.Columns["jpn"].Visible = false;
                fieldGridView.Columns["tchi"].Visible = false;
                fieldGridView.Columns["schi"].Visible = false;
                fieldGridView.Columns["kor"].Visible = false;
                fieldGridView.Columns["fre"].Visible = false;
                fieldGridView.Columns["ger"].Visible = false;
                fieldGridView.Columns["spa"].Visible = false;
                fieldGridView.Columns["ita"].Visible = false;
                fieldGridView.Columns["dut"].Visible = false;
                fieldGridView.Columns["rus"].Visible = false;
                fieldGridView.Columns["value"].Visible = false;

                DataGridViewImageColumn imageColumn = new DataGridViewImageColumn
                {
                    Name = "Image",
                    HeaderText = "Image",
                    ImageLayout = DataGridViewImageCellLayout.Zoom
                };
                fieldGridView.Columns.Insert(13, imageColumn);
                imageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

                fieldGridView.Columns["eng"].Width = 195;
                fieldGridView.Columns["jpn"].Width = 195;
                fieldGridView.Columns["tchi"].Width = 195;
                fieldGridView.Columns["schi"].Width = 195;
                fieldGridView.Columns["kor"].Width = 195;
                fieldGridView.Columns["fre"].Width = 195;
                fieldGridView.Columns["ger"].Width = 195;
                fieldGridView.Columns["spa"].Width = 195;
                fieldGridView.Columns["ita"].Width = 195;
                fieldGridView.Columns["dut"].Width = 195;
                fieldGridView.Columns["rus"].Width = 195;
                fieldGridView.Columns["Image"].Width = 128;

                fieldGridView.Columns["eng"].HeaderText = "Name";
                fieldGridView.Columns["jpn"].HeaderText = "Name";
                fieldGridView.Columns["tchi"].HeaderText = "Name";
                fieldGridView.Columns["schi"].HeaderText = "Name";
                fieldGridView.Columns["kor"].HeaderText = "Name";
                fieldGridView.Columns["fre"].HeaderText = "Name";
                fieldGridView.Columns["ger"].HeaderText = "Name";
                fieldGridView.Columns["spa"].HeaderText = "Name";
                fieldGridView.Columns["ita"].HeaderText = "Name";
                fieldGridView.Columns["dut"].HeaderText = "Name";
                fieldGridView.Columns["rus"].HeaderText = "Name";

                currentDataTable = flowerSource;
            }

            FlagTextbox.Text = "20";
        }

        private void favModeBtn_Click(object sender, EventArgs e)
        {
            itemModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            recipeModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            flowerModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            favModeBtn.BackColor = Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(255)))));
            fieldModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));

            if (itemSearchBox.Text != "Search")
            {
                itemSearchBox.Clear();
            }

            fieldGridView.Columns.Remove("Image");

            if (favSource != null)
            {
                fieldGridView.DataSource = favSource;

                fieldGridView.Columns["id"].Visible = false;
                fieldGridView.Columns["iName"].Visible = false;
                fieldGridView.Columns["Name"].Visible = true;
                fieldGridView.Columns["value"].Visible = false;

                DataGridViewImageColumn imageColumn = new DataGridViewImageColumn
                {
                    Name = "Image",
                    HeaderText = "Image",
                    ImageLayout = DataGridViewImageCellLayout.Zoom
                };
                fieldGridView.Columns.Insert(4, imageColumn);
                imageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

                fieldGridView.Columns["Name"].Width = 195;
                fieldGridView.Columns["Image"].Width = 128;

                currentDataTable = favSource;
            }

            FlagTextbox.Text = "20";
        }

        private void fieldModeBtn_Click(object sender, EventArgs e)
        {
            itemModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            recipeModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            flowerModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            favModeBtn.BackColor = Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            fieldModeBtn.BackColor = Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(255)))));

            if (itemSearchBox.Text != "Search")
            {
                itemSearchBox.Clear();
            }

            fieldGridView.Columns.Remove("Image");

            if (favSource != null)
            {
                fieldGridView.DataSource = fieldSource;

                fieldGridView.Columns["id"].Visible = false;
                fieldGridView.Columns["iName"].Visible = false;
                fieldGridView.Columns["name"].Visible = true;
                fieldGridView.Columns["value"].Visible = false;

                DataGridViewImageColumn imageColumn = new DataGridViewImageColumn
                {
                    Name = "Image",
                    HeaderText = "Image",
                    ImageLayout = DataGridViewImageCellLayout.Zoom
                };
                fieldGridView.Columns.Insert(3, imageColumn);
                imageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

                fieldGridView.Columns["name"].Width = 195;
                fieldGridView.Columns["Image"].Width = 128;

                fieldGridView.Columns["name"].HeaderText = "Name";

                currentDataTable = fieldSource;
            }

            FlagTextbox.Text = "00";
        }

        private void floor_MouseDown(object sender, MouseEventArgs e)
        {
            var button = (floorSlot)sender;

            selectedButton = button;

            resetBtnColor();

            if (Control.ModifierKeys == Keys.Shift)
            {
                selectedItem_Click(sender, e);
            }
            else if (Control.ModifierKeys == Keys.Alt)
            {
                deleteItem(button);
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
            }
        }

        private void resetBtnColor()
        {
            foreach (floorSlot btn in BtnPanel.Controls.OfType<floorSlot>())
            {
                if (layer1Btn.Checked)
                    btn.setBackColor(true);
                else
                    btn.setBackColor(false);

            }

            if (selectedButton != null)
            {
                selectedButton.BackColor = System.Drawing.Color.LightSeaGreen;
            }
        }

        private void selectedItem_Click(object sender, EventArgs e)
        {
            if (selectedButton == null)
            {
                MessageBox.Show("Please select a slot!");
                return;
            }
            if (IdTextbox.Text == "" || HexTextbox.Text == "" || FlagTextbox.Text == "")
            {
                return;
            }

            string address1;
            string address2;
            string address3;
            string address4;

            if (layer1Btn.Checked)
            {
                address1 = getAddress(selectedButton.mapX, selectedButton.mapY).ToString("X");
                address2 = (getAddress(selectedButton.mapX, selectedButton.mapY) + 0x600).ToString("X");
                address3 = (getAddress(selectedButton.mapX, selectedButton.mapY) + Utilities.mapOffset).ToString("X");
                address4 = (getAddress(selectedButton.mapX, selectedButton.mapY) + 0x600 + Utilities.mapOffset).ToString("X");
            }
            else if (layer2Btn.Checked)
            {
                address1 = (getAddress(selectedButton.mapX, selectedButton.mapY) + Utilities.mapSize).ToString("X");
                address2 = (getAddress(selectedButton.mapX, selectedButton.mapY) + 0x600 + Utilities.mapSize).ToString("X");
                address3 = (getAddress(selectedButton.mapX, selectedButton.mapY) + Utilities.mapOffset + Utilities.mapSize).ToString("X");
                address4 = (getAddress(selectedButton.mapX, selectedButton.mapY) + 0x600 + Utilities.mapOffset + Utilities.mapSize).ToString("X");
            }
            else
                return;

            string itemID = Utilities.precedingZeros(IdTextbox.Text, 4);
            string itemData = Utilities.precedingZeros(HexTextbox.Text, 8);
            string flag2 = Utilities.precedingZeros(FlagTextbox.Text, 2);

            Utilities.dropItem(s, bot, address1, address2, address3, address4, itemID, itemData, "00", flag2);
            setBtn(selectedButton, itemID, itemData, "0000FFFD", "0100" + itemID, "0000FFFD", "0001" + itemID, "0000FFFD", "0101" + itemID, "00", flag2);
            updataData(selectedButton.mapX, selectedButton.mapY, itemID, itemData, flag2);
            resetBtnColor();
            if (sound)
                System.Media.SystemSounds.Asterisk.Play();

        }

        private DataTable loadItemCSV(string filePath, bool key = true)
        {
            var dt = new DataTable();

            File.ReadLines(filePath).Take(1)
                .SelectMany(x => x.Split(new[] { " ; " }, StringSplitOptions.RemoveEmptyEntries))
                .ToList()
                .ForEach(x => dt.Columns.Add(x.Trim()));

            File.ReadLines(filePath).Skip(1)
                .Select(x => x.Split(new[] { " ; " }, StringSplitOptions.RemoveEmptyEntries))
                .ToList()
                .ForEach(line => dt.Rows.Add(line));

            if (key)
            {
                if (dt.Columns.Contains("id"))
                    dt.PrimaryKey = new DataColumn[1] { dt.Columns["id"] };
            }

            return dt;
        }

        private void itemSearchBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (fieldGridView.DataSource != null)
                {
                    if (currentDataTable == source)
                    {
                        (fieldGridView.DataSource as DataTable).DefaultView.RowFilter = string.Format("eng" + " LIKE '%{0}%'", EscapeLikeValue(itemSearchBox.Text));
                    }
                    else if (currentDataTable == recipeSource)
                    {
                        (fieldGridView.DataSource as DataTable).DefaultView.RowFilter = string.Format("eng" + " LIKE '%{0}%'", EscapeLikeValue(itemSearchBox.Text));
                    }
                    else if (currentDataTable == flowerSource)
                    {
                        (fieldGridView.DataSource as DataTable).DefaultView.RowFilter = string.Format("eng" + " LIKE '%{0}%'", EscapeLikeValue(itemSearchBox.Text));
                    }
                    else if (currentDataTable == favSource)
                    {
                        (fieldGridView.DataSource as DataTable).DefaultView.RowFilter = string.Format("name" + " LIKE '%{0}%'", EscapeLikeValue(itemSearchBox.Text));
                    }
                    else if (currentDataTable == fieldSource)
                    {
                        (fieldGridView.DataSource as DataTable).DefaultView.RowFilter = string.Format("name" + " LIKE '%{0}%'", EscapeLikeValue(itemSearchBox.Text));
                    }
                }
            }
            catch
            {
                itemSearchBox.Clear();
            }
        }

        public static string EscapeLikeValue(string valueWithoutWildcards)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < valueWithoutWildcards.Length; i++)
            {
                char c = valueWithoutWildcards[i];
                if (c == '*' || c == '%' || c == '[' || c == ']')
                    sb.Append("[").Append(c).Append("]");
                else if (c == '\'')
                    sb.Append("''");
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private void itemSearchBox_Click(object sender, EventArgs e)
        {
            if (itemSearchBox.Text == "Search")
            {
                itemSearchBox.Text = "";
                itemSearchBox.ForeColor = Color.White;
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripItem item = (sender as ToolStripItem);
            if (item != null)
            {
                if (item.Owner is ContextMenuStrip owner)
                {
                    var btn = (floorSlot)owner.SourceControl;

                    string address1;
                    string address2;
                    string address3;
                    string address4;

                    if (layer1Btn.Checked)
                    {
                        address1 = getAddress(btn.mapX, btn.mapY).ToString("X");
                        address2 = (getAddress(btn.mapX, btn.mapY) + 0x600).ToString("X");
                        address3 = (getAddress(btn.mapX, btn.mapY) + Utilities.mapOffset).ToString("X");
                        address4 = (getAddress(btn.mapX, btn.mapY) + 0x600 + Utilities.mapOffset).ToString("X");
                    }
                    else if (layer2Btn.Checked)
                    {
                        address1 = (getAddress(btn.mapX, btn.mapY) + Utilities.mapSize).ToString("X");
                        address2 = (getAddress(btn.mapX, btn.mapY) + 0x600 + Utilities.mapSize).ToString("X");
                        address3 = (getAddress(btn.mapX, btn.mapY) + Utilities.mapOffset + Utilities.mapSize).ToString("X");
                        address4 = (getAddress(btn.mapX, btn.mapY) + 0x600 + Utilities.mapOffset + Utilities.mapSize).ToString("X");
                    }
                    else
                        return;

                    Utilities.deleteFloorItem(s, bot, address1, address2, address3, address4);
                    updataData(selectedButton.mapX, selectedButton.mapY);

                    btn.reset();
                    btnToolTip.RemoveAll();
                    if (selectedButton != null)
                        selectedButton.BackColor = System.Drawing.Color.LightSeaGreen;
                    if (sound)
                        System.Media.SystemSounds.Asterisk.Play();
                }
            }
        }

        private void copyItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripItem item = (sender as ToolStripItem);
            if (item != null)
            {
                if (item.Owner is ContextMenuStrip owner)
                {
                    var btn = (floorSlot)owner.SourceControl;
                    string id = Utilities.precedingZeros(btn.itemID.ToString("X"), 4);
                    string name = btn.Name;
                    string hexValue = Utilities.precedingZeros(btn.itemData.ToString("X"), 8);
                    string flag1 = btn.flag1;
                    string flag2 = btn.flag2;

                    IdTextbox.Text = id;
                    HexTextbox.Text = hexValue;
                    FlagTextbox.Text = flag2;

                    if (id == "16A2")
                        selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(Utilities.turn2bytes(hexValue), recipeSource), true, "", flag1, flag2);
                    else
                        selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(id, source, Convert.ToUInt32("0x" + hexValue, 16)), true, "", flag1, flag2);
                    if (sound)
                        System.Media.SystemSounds.Asterisk.Play();
                }
            }
        }

        private void Hex_KeyPress(object sender, KeyPressEventArgs e)
        {
            char c = e.KeyChar;
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            {
                e.Handled = true;
            }
            if (c >= 'a' && c <= 'f') e.KeyChar = char.ToUpper(c);
        }

        private void Hex_KeyUp(object sender, KeyEventArgs e)
        {
            if (IdTextbox.Text.Equals(string.Empty) || HexTextbox.Text.Equals(string.Empty))
                return;

            string itemID = Utilities.precedingZeros(IdTextbox.Text, 4);
            string itemData = Utilities.precedingZeros(HexTextbox.Text, 8);
            string flag2 = Utilities.precedingZeros(FlagTextbox.Text, 2);

            if (itemID.Equals("315A") || itemID.Equals("1618"))
            {
                selectedItem.setup(GetNameFromID(itemID, source), Convert.ToUInt16("0x" + itemID, 16), Convert.ToUInt32("0x" + itemData, 16), GetImagePathFromID(itemID, source), true, GetImagePathFromID(Utilities.turn2bytes(itemData), source), "00", flag2);
            }
            else if (itemID.Equals("16A2"))
            {
                selectedItem.setup(GetNameFromID(itemID, recipeSource), Convert.ToUInt16("0x" + itemID, 16), Convert.ToUInt32("0x" + itemData, 16), GetImagePathFromID(Utilities.turn2bytes(itemData), recipeSource), true, "", "00", flag2);
            }
            else
            {
                selectedItem.setup(GetNameFromID(itemID, source), Convert.ToUInt16("0x" + itemID, 16), Convert.ToUInt32("0x" + itemData, 16), GetImagePathFromID(itemID, source), true, "", "00", flag2);
            }
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            if (anchorX < 0 || anchorY < 0)
                return;

            disableBtn();

            Thread LoadThread = new Thread(delegate () { refreshMap(Utilities.mapZero, Utilities.mapZero + Utilities.mapSize); });
            LoadThread.Start();
        }

        private void refreshMap(UInt32 layer1Address, UInt32 layer2Address)
        {
            showMapWait(42 * 2, "Fetching Map...");

            Layer1 = Utilities.getMapLayer(s, bot, layer1Address, ref counter);
            Layer2 = Utilities.getMapLayer(s, bot, layer2Address, ref counter);

            if (layer1Btn.Checked)
                miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer1);
            else
                miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer2);

            this.Invoke((MethodInvoker)delegate
            {
                displayAnchor(getMapColumns(anchorX, anchorY));
                enableBtn();
            });

            if (sound)
                System.Media.SystemSounds.Asterisk.Play();

            hideMapWait();
        }

        private void fillRemainBtn_Click(object sender, EventArgs e)
        {
            if (anchorX < 0 || anchorY < 0)
                return;

            if (IdTextbox.Text == "" || HexTextbox.Text == "" || FlagTextbox.Text == "")
            {
                return;
            }

            string itemID = Utilities.precedingZeros(IdTextbox.Text, 4);
            string itemData = Utilities.precedingZeros(HexTextbox.Text, 8);
            string flag2 = Utilities.precedingZeros(FlagTextbox.Text, 2);

            disableBtn();

            Thread fillRemainThread = new Thread(delegate () { fillRemain(itemID, itemData, flag2); });
            fillRemainThread.Start();
        }

        private void fillRemain(string itemID, string itemData, string flag2)
        {
            showMapWait(14, "Filling Empty Tiles...");

            byte[][] b = new byte[14][];

            UInt32 address = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 3)) + (0x10 * (anchorY - 3)));

            byte[] readFloor = Utilities.ReadByteArray8(s, address, 0x4E70);
            byte[] curFloor = new byte[1568];

            Buffer.BlockCopy(readFloor, 0x0, curFloor, 0x0, 0x70);
            Buffer.BlockCopy(readFloor, 0x600, curFloor, 0x70, 0x70);
            Buffer.BlockCopy(readFloor, 0xC00, curFloor, 0xE0, 0x70);
            Buffer.BlockCopy(readFloor, 0x1200, curFloor, 0x150, 0x70);
            Buffer.BlockCopy(readFloor, 0x1800, curFloor, 0x1C0, 0x70);
            Buffer.BlockCopy(readFloor, 0x1E00, curFloor, 0x230, 0x70);
            Buffer.BlockCopy(readFloor, 0x2400, curFloor, 0x2A0, 0x70);
            Buffer.BlockCopy(readFloor, 0x2A00, curFloor, 0x310, 0x70);
            Buffer.BlockCopy(readFloor, 0x3000, curFloor, 0x380, 0x70);
            Buffer.BlockCopy(readFloor, 0x3600, curFloor, 0x3F0, 0x70);
            Buffer.BlockCopy(readFloor, 0x3C00, curFloor, 0x460, 0x70);
            Buffer.BlockCopy(readFloor, 0x4200, curFloor, 0x4D0, 0x70);
            Buffer.BlockCopy(readFloor, 0x4800, curFloor, 0x540, 0x70);
            Buffer.BlockCopy(readFloor, 0x4E00, curFloor, 0x5B0, 0x70);

            bool[,] isEmpty = new bool[7, 7];

            int emptyspace = numOfEmpty(curFloor, ref isEmpty);

            fillFloor(ref b, curFloor, isEmpty, itemID, itemData, flag2);

            UInt32 address1;
            UInt32 address2;
            UInt32 address3;
            UInt32 address4;
            UInt32 address5;
            UInt32 address6;
            UInt32 address7;

            if (layer1Btn.Checked)
            {
                address1 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 3)) + (0x10 * (anchorY - 3)));
                address2 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 2)) + (0x10 * (anchorY - 3)));
                address3 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 1)) + (0x10 * (anchorY - 3)));
                address4 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 0)) + (0x10 * (anchorY - 3)));
                address5 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 1)) + (0x10 * (anchorY - 3)));
                address6 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 2)) + (0x10 * (anchorY - 3)));
                address7 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 3)) + (0x10 * (anchorY - 3)));
            }
            else if (layer2Btn.Checked)
            {
                address1 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 3)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address2 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 2)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address3 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 1)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address4 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 0)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address5 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 1)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address6 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 2)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address7 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 3)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
            }
            else
                return;

            Utilities.dropColume(s, bot, address1, address1 + 0x600, b[0], b[1], ref counter);
            Utilities.dropColume(s, bot, address2, address2 + 0x600, b[2], b[3], ref counter);
            Utilities.dropColume(s, bot, address3, address3 + 0x600, b[4], b[5], ref counter);
            Utilities.dropColume(s, bot, address4, address4 + 0x600, b[6], b[7], ref counter);
            Utilities.dropColume(s, bot, address5, address5 + 0x600, b[8], b[9], ref counter);
            Utilities.dropColume(s, bot, address6, address6 + 0x600, b[10], b[11], ref counter);
            Utilities.dropColume(s, bot, address7, address7 + 0x600, b[12], b[13], ref counter);

            Utilities.dropColume(s, bot, address1 + Utilities.mapOffset, address1 + 0x600 + Utilities.mapOffset, b[0], b[1], ref counter);
            Utilities.dropColume(s, bot, address2 + Utilities.mapOffset, address2 + 0x600 + Utilities.mapOffset, b[2], b[3], ref counter);
            Utilities.dropColume(s, bot, address3 + Utilities.mapOffset, address3 + 0x600 + Utilities.mapOffset, b[4], b[5], ref counter);
            Utilities.dropColume(s, bot, address4 + Utilities.mapOffset, address4 + 0x600 + Utilities.mapOffset, b[6], b[7], ref counter);
            Utilities.dropColume(s, bot, address5 + Utilities.mapOffset, address5 + 0x600 + Utilities.mapOffset, b[8], b[9], ref counter);
            Utilities.dropColume(s, bot, address6 + Utilities.mapOffset, address6 + 0x600 + Utilities.mapOffset, b[10], b[11], ref counter);
            Utilities.dropColume(s, bot, address7 + Utilities.mapOffset, address7 + 0x600 + Utilities.mapOffset, b[12], b[13], ref counter);

            this.Invoke((MethodInvoker)delegate
            {
                BtnSetup(b[0], b[1], anchorX - 3, anchorY - 3, floor1, floor2, floor3, floor4, floor5, floor6, floor7, 0, false);
                BtnSetup(b[2], b[3], anchorX - 2, anchorY - 3, floor8, floor9, floor10, floor11, floor12, floor13, floor14, 0, false);
                BtnSetup(b[4], b[5], anchorX - 1, anchorY - 3, floor15, floor16, floor17, floor18, floor19, floor20, floor21, 0, false);
                BtnSetup(b[6], b[7], anchorX - 0, anchorY - 3, floor22, floor23, floor24, floor25, floor26, floor27, floor28, 0, false);
                BtnSetup(b[8], b[9], anchorX + 1, anchorY - 3, floor29, floor30, floor31, floor32, floor33, floor34, floor35, 0, false);
                BtnSetup(b[10], b[11], anchorX + 2, anchorY - 3, floor36, floor37, floor38, floor39, floor40, floor41, floor42, 0, false);
                BtnSetup(b[12], b[13], anchorX + 3, anchorY - 3, floor43, floor44, floor45, floor46, floor47, floor48, floor49, 0, false);
            });

            updataData(anchorX, anchorY, b);

            this.Invoke((MethodInvoker)delegate
            {
                resetBtnColor();
                enableBtn();
            });

            if (sound)
                System.Media.SystemSounds.Asterisk.Play();

            hideMapWait();
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (anchorX < 0 || anchorY < 0)
                {
                    return;
                }

                SaveFileDialog file = new SaveFileDialog()
                {
                    Filter = "New Horizons Grid (*.nhg)|*.nhg",
                };

                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                string savepath;

                if (config.AppSettings.Settings["LastSave"].Value.Equals(string.Empty))
                    savepath = Directory.GetCurrentDirectory() + @"\save";
                else
                    savepath = config.AppSettings.Settings["LastSave"].Value;

                if (Directory.Exists(savepath))
                {
                    file.InitialDirectory = savepath;
                }
                else
                {
                    file.InitialDirectory = @"C:\";
                }

                if (file.ShowDialog() != DialogResult.OK)
                    return;

                string[] temp = file.FileName.Split('\\');
                string path = "";
                for (int i = 0; i < temp.Length - 1; i++)
                    path = path + temp[i] + "\\";

                config.AppSettings.Settings["LastSave"].Value = path;
                config.Save(ConfigurationSaveMode.Minimal);

                UInt32 address = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 3)) + (0x10 * (anchorY - 3)));

                byte[] b = Utilities.ReadByteArray8(s, address, 0x4E70);
                byte[] save = new byte[1568];

                Buffer.BlockCopy(b, 0x0, save, 0x0, 0x70);
                Buffer.BlockCopy(b, 0x600, save, 0x70, 0x70);
                Buffer.BlockCopy(b, 0xC00, save, 0xE0, 0x70);
                Buffer.BlockCopy(b, 0x1200, save, 0x150, 0x70);
                Buffer.BlockCopy(b, 0x1800, save, 0x1C0, 0x70);
                Buffer.BlockCopy(b, 0x1E00, save, 0x230, 0x70);
                Buffer.BlockCopy(b, 0x2400, save, 0x2A0, 0x70);
                Buffer.BlockCopy(b, 0x2A00, save, 0x310, 0x70);
                Buffer.BlockCopy(b, 0x3000, save, 0x380, 0x70);
                Buffer.BlockCopy(b, 0x3600, save, 0x3F0, 0x70);
                Buffer.BlockCopy(b, 0x3C00, save, 0x460, 0x70);
                Buffer.BlockCopy(b, 0x4200, save, 0x4D0, 0x70);
                Buffer.BlockCopy(b, 0x4800, save, 0x540, 0x70);
                Buffer.BlockCopy(b, 0x4E00, save, 0x5B0, 0x70);

                File.WriteAllBytes(file.FileName, save);
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
            }
            catch
            {
                if (s != null)
                {
                    s.Close();
                }
                return;
            }
        }

        private void loadBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (anchorX < 0 || anchorY < 0)
                {
                    return;
                }
                OpenFileDialog file = new OpenFileDialog()
                {
                    Filter = "New Horizons Grid (*.nhg)|*.nhg|New Horizons Inventory(*.nhi) | *.nhi|All files (*.*)|*.*",
                };

                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                string savepath;

                if (config.AppSettings.Settings["LastLoad"].Value.Equals(string.Empty))
                    savepath = Directory.GetCurrentDirectory() + @"\save";
                else
                    savepath = config.AppSettings.Settings["LastLoad"].Value;

                if (Directory.Exists(savepath))
                {
                    file.InitialDirectory = savepath;
                }
                else
                {
                    file.InitialDirectory = @"C:\";
                }

                if (file.ShowDialog() != DialogResult.OK)
                    return;

                string[] temp = file.FileName.Split('\\');
                string path = "";
                for (int i = 0; i < temp.Length - 1; i++)
                    path = path + temp[i] + "\\";

                config.AppSettings.Settings["LastLoad"].Value = path;
                config.Save(ConfigurationSaveMode.Minimal);

                byte[] data = File.ReadAllBytes(file.FileName);
                bool nhi;

                if (file.FileName.Contains(".nhi"))
                    nhi = true;
                else
                    nhi = false;

                disableBtn();

                btnToolTip.RemoveAll();
                Thread LoadThread = new Thread(delegate () { loadFloor(data, nhi); });
                LoadThread.Start();
            }
            catch
            {
                if (s != null)
                {
                    s.Close();
                }
                return;
            }
        }

        private void loadFloor(byte[] data, bool nhi)
        {
            showMapWait(14, "Loading...");

            byte[][] b = new byte[14][];

            if (nhi)
            {
                byte[][] item = processNHI(data);

                UInt32 address = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 3)) + (0x10 * (anchorY - 3)));

                byte[] readFloor = Utilities.ReadByteArray8(s, address, 0x4E70);
                byte[] curFloor = new byte[1568];

                Buffer.BlockCopy(readFloor, 0x0, curFloor, 0x0, 0x70);
                Buffer.BlockCopy(readFloor, 0x600, curFloor, 0x70, 0x70);
                Buffer.BlockCopy(readFloor, 0xC00, curFloor, 0xE0, 0x70);
                Buffer.BlockCopy(readFloor, 0x1200, curFloor, 0x150, 0x70);
                Buffer.BlockCopy(readFloor, 0x1800, curFloor, 0x1C0, 0x70);
                Buffer.BlockCopy(readFloor, 0x1E00, curFloor, 0x230, 0x70);
                Buffer.BlockCopy(readFloor, 0x2400, curFloor, 0x2A0, 0x70);
                Buffer.BlockCopy(readFloor, 0x2A00, curFloor, 0x310, 0x70);
                Buffer.BlockCopy(readFloor, 0x3000, curFloor, 0x380, 0x70);
                Buffer.BlockCopy(readFloor, 0x3600, curFloor, 0x3F0, 0x70);
                Buffer.BlockCopy(readFloor, 0x3C00, curFloor, 0x460, 0x70);
                Buffer.BlockCopy(readFloor, 0x4200, curFloor, 0x4D0, 0x70);
                Buffer.BlockCopy(readFloor, 0x4800, curFloor, 0x540, 0x70);
                Buffer.BlockCopy(readFloor, 0x4E00, curFloor, 0x5B0, 0x70);

                bool[,] isEmpty = new bool[7, 7];

                int emptyspace = numOfEmpty(curFloor, ref isEmpty);

                if (emptyspace < item.Length)
                {
                    DialogResult dialogResult = MessageBox.Show("Empty tiles around anchor : " + emptyspace + "\n" +
                                                                "Number of items to Spawn : " + item.Length + "\n" +
                                                                "\n" +
                                                                "Press  [Yes]  to clear the floor and spawn the items " + "\n" +
                                                                "or  [No]  to cancel the spawn." + "\n" + "\n" +
                                                                "[Warning] You will lose your items on the ground!"
                                                                , "Not enough empty tiles!", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        fillFloor(ref b, item);
                    }
                    else
                    {
                        if (sound)
                            System.Media.SystemSounds.Asterisk.Play();
                        return;
                    }
                }
                else
                {
                    fillFloor(ref b, curFloor, isEmpty, item);
                }

            }
            else
            {
                for (int i = 0; i < 14; i++)
                {
                    b[i] = new byte[112];
                    for (int j = 0; j < 112; j++)
                    {
                        b[i][j] = data[j + 112 * i];
                    }
                }
            }

            UInt32 address1;
            UInt32 address2;
            UInt32 address3;
            UInt32 address4;
            UInt32 address5;
            UInt32 address6;
            UInt32 address7;

            if (layer1Btn.Checked)
            {
                address1 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 3)) + (0x10 * (anchorY - 3)));
                address2 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 2)) + (0x10 * (anchorY - 3)));
                address3 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 1)) + (0x10 * (anchorY - 3)));
                address4 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 0)) + (0x10 * (anchorY - 3)));
                address5 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 1)) + (0x10 * (anchorY - 3)));
                address6 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 2)) + (0x10 * (anchorY - 3)));
                address7 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 3)) + (0x10 * (anchorY - 3)));
            }
            else if (layer2Btn.Checked)
            {
                address1 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 3)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address2 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 2)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address3 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 1)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address4 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX - 0)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address5 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 1)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address6 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 2)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
                address7 = (UInt32)(Utilities.mapZero + (0xC00 * (anchorX + 3)) + (0x10 * (anchorY - 3))) + Utilities.mapSize;
            }
            else
                return;

            Utilities.dropColume(s, bot, address1, address1 + 0x600, b[0], b[1], ref counter);
            Utilities.dropColume(s, bot, address2, address2 + 0x600, b[2], b[3], ref counter);
            Utilities.dropColume(s, bot, address3, address3 + 0x600, b[4], b[5], ref counter);
            Utilities.dropColume(s, bot, address4, address4 + 0x600, b[6], b[7], ref counter);
            Utilities.dropColume(s, bot, address5, address5 + 0x600, b[8], b[9], ref counter);
            Utilities.dropColume(s, bot, address6, address6 + 0x600, b[10], b[11], ref counter);
            Utilities.dropColume(s, bot, address7, address7 + 0x600, b[12], b[13], ref counter);

            Utilities.dropColume(s, bot, address1 + Utilities.mapOffset, address1 + 0x600 + Utilities.mapOffset, b[0], b[1], ref counter);
            Utilities.dropColume(s, bot, address2 + Utilities.mapOffset, address2 + 0x600 + Utilities.mapOffset, b[2], b[3], ref counter);
            Utilities.dropColume(s, bot, address3 + Utilities.mapOffset, address3 + 0x600 + Utilities.mapOffset, b[4], b[5], ref counter);
            Utilities.dropColume(s, bot, address4 + Utilities.mapOffset, address4 + 0x600 + Utilities.mapOffset, b[6], b[7], ref counter);
            Utilities.dropColume(s, bot, address5 + Utilities.mapOffset, address5 + 0x600 + Utilities.mapOffset, b[8], b[9], ref counter);
            Utilities.dropColume(s, bot, address6 + Utilities.mapOffset, address6 + 0x600 + Utilities.mapOffset, b[10], b[11], ref counter);
            Utilities.dropColume(s, bot, address7 + Utilities.mapOffset, address7 + 0x600 + Utilities.mapOffset, b[12], b[13], ref counter);

            this.Invoke((MethodInvoker)delegate
            {
                BtnSetup(b[0], b[1], anchorX - 3, anchorY - 3, floor1, floor2, floor3, floor4, floor5, floor6, floor7, 0, false);
                BtnSetup(b[2], b[3], anchorX - 2, anchorY - 3, floor8, floor9, floor10, floor11, floor12, floor13, floor14, 0, false);
                BtnSetup(b[4], b[5], anchorX - 1, anchorY - 3, floor15, floor16, floor17, floor18, floor19, floor20, floor21, 0, false);
                BtnSetup(b[6], b[7], anchorX - 0, anchorY - 3, floor22, floor23, floor24, floor25, floor26, floor27, floor28, 0, false);
                BtnSetup(b[8], b[9], anchorX + 1, anchorY - 3, floor29, floor30, floor31, floor32, floor33, floor34, floor35, 0, false);
                BtnSetup(b[10], b[11], anchorX + 2, anchorY - 3, floor36, floor37, floor38, floor39, floor40, floor41, floor42, 0, false);
                BtnSetup(b[12], b[13], anchorX + 3, anchorY - 3, floor43, floor44, floor45, floor46, floor47, floor48, floor49, 0, false);
            });

            updataData(anchorX, anchorY, b);

            this.Invoke((MethodInvoker)delegate
            {
                resetBtnColor();
                enableBtn();
            });

            if (sound)
                System.Media.SystemSounds.Asterisk.Play();

            hideMapWait();
        }

        private byte[][] processNHI(byte[] data)
        {
            byte[] tempItem = new byte[8];
            bool[] isItem = new bool[40];
            int numOfitem = 0;

            for (int i = 0; i < 40; i++)
            {
                Buffer.BlockCopy(data, 0x8 * i, tempItem, 0, 8);
                if (!Utilities.ByteToHexString(tempItem).Equals("FEFF000000000000"))
                {
                    isItem[i] = true;
                    numOfitem++;
                }
            }

            byte[][] item = new byte[numOfitem][];
            int itemNum = 0;
            for (int j = 0; j < 40; j++)
            {
                if (isItem[j])
                {
                    item[itemNum] = new byte[8];
                    Buffer.BlockCopy(data, 0x8 * j, item[itemNum], 0, 8);
                    itemNum++;
                }
            }

            return item;
        }

        private int numOfEmpty(byte[] data, ref bool[,] isEmpty)
        {
            byte[] tempItem = new byte[16];
            byte[] tempItem2 = new byte[16];
            int num = 0;

            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    Buffer.BlockCopy(data, 0xE0 * i + 0x10 * j, tempItem, 0, 16);
                    if (Utilities.ByteToHexString(tempItem).Equals("FEFF000000000000FEFF000000000000"))
                    {
                        Buffer.BlockCopy(data, 0xE0 * i + 0x10 * j + 0x70, tempItem2, 0, 16);
                        if (Utilities.ByteToHexString(tempItem2).Equals("FEFF000000000000FEFF000000000000"))
                        {
                            isEmpty[i, j] = true;
                            num++;
                        }
                    }
                }
            }
            return num;
        }

        private void fillFloor(ref byte[][] b, byte[] cur, bool[,] isEmpty, byte[][] item)
        {
            int itemNum = 0;

            for (int i = 0; i < 14; i++)
            {
                b[i] = new byte[112];
            }

            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (isEmpty[i, j] && itemNum < item.Length)
                    {
                        transformToFloorItem(ref b[i * 2], ref b[i * 2 + 1], j, item[itemNum]);
                        itemNum++;
                    }
                    else
                    {
                        Buffer.BlockCopy(cur, 0xE0 * i + 0x10 * j, b[i * 2], 0x10 * j, 16);
                        Buffer.BlockCopy(cur, 0xE0 * i + 0x10 * j + 0x70, b[i * 2 + 1], 0x10 * j, 16);
                    }
                }
            }
        }

        private void fillFloor(ref byte[][] b, byte[][] item)
        {
            int itemNum = 0;
            byte[] emptyLeft = Utilities.stringToByte("FEFF000000000000FEFF000000000000");
            byte[] emptyRight = Utilities.stringToByte("FEFF000000000000FEFF000000000000");

            for (int i = 0; i < 14; i++)
            {
                b[i] = new byte[112];
            }

            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (itemNum < item.Length)
                    {
                        transformToFloorItem(ref b[i * 2], ref b[i * 2 + 1], j, item[itemNum]);
                        itemNum++;
                    }
                    else
                    {
                        Buffer.BlockCopy(emptyLeft, 0, b[i * 2], 0x10 * j, 16);
                        Buffer.BlockCopy(emptyRight, 0, b[i * 2 + 1], 0x10 * j, 16);
                    }
                }
            }
        }

        private void fillFloor(ref byte[][] b, byte[] cur, bool[,] isEmpty, string itemID, string itemData, string flag2)
        {
            int itemNum = 0;

            for (int i = 0; i < 14; i++)
            {
                b[i] = new byte[112];
            }

            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (isEmpty[i, j])
                    {
                        transformToFloorItem(ref b[i * 2], ref b[i * 2 + 1], j, itemID, itemData, flag2);
                        itemNum++;
                    }
                    else
                    {
                        Buffer.BlockCopy(cur, 0xE0 * i + 0x10 * j, b[i * 2], 0x10 * j, 16);
                        Buffer.BlockCopy(cur, 0xE0 * i + 0x10 * j + 0x70, b[i * 2 + 1], 0x10 * j, 16);
                    }
                }
            }
        }

        private void transformToFloorItem(ref byte[] b1, ref byte[] b2, int slot, byte[] item)
        {
            byte[] slotBytes = new byte[2];
            byte[] flag1Bytes = new byte[1];
            byte[] flag2Bytes = new byte[1];
            byte[] dataBytes = new byte[4];

            Buffer.BlockCopy(item, 0x0, slotBytes, 0, 2);
            Buffer.BlockCopy(item, 0x3, flag1Bytes, 0, 1);
            Buffer.BlockCopy(item, 0x2, flag2Bytes, 0, 1);
            Buffer.BlockCopy(item, 0x4, dataBytes, 0, 4);

            string itemID = Utilities.flip(Utilities.ByteToHexString(slotBytes));
            string itemData = Utilities.flip(Utilities.ByteToHexString(dataBytes));
            string flag1 = Utilities.ByteToHexString(flag1Bytes);
            string flag2 = "20";

            byte[] dropItemLeft = Utilities.stringToByte(Utilities.buildDropStringLeft(itemID, itemData, flag1, flag2));
            byte[] dropItemRight = Utilities.stringToByte(Utilities.buildDropStringRight(itemID));

            /*
            Debug.Print(Utilities.ByteToHexString(b1));
            Debug.Print(Utilities.ByteToHexString(b2));
            Debug.Print(Utilities.ByteToHexString(dropItemLeft));
            Debug.Print(Utilities.ByteToHexString(dropItemRight));
            */

            Buffer.BlockCopy(dropItemLeft, 0, b1, slot * 0x10, 16);
            Buffer.BlockCopy(dropItemRight, 0, b2, slot * 0x10, 16);

            /*
            Debug.Print(Utilities.ByteToHexString(b1));
            Debug.Print(Utilities.ByteToHexString(b2));
            Debug.Print(Utilities.ByteToHexString(item));
            */
        }

        private void transformToFloorItem(ref byte[] b1, ref byte[] b2, int slot, string itemID, string itemData, string flag2)
        {
            string flag1 = "00";

            byte[] dropItemLeft = Utilities.stringToByte(Utilities.buildDropStringLeft(itemID, itemData, flag1, flag2));
            byte[] dropItemRight = Utilities.stringToByte(Utilities.buildDropStringRight(itemID));

            Buffer.BlockCopy(dropItemLeft, 0, b1, slot * 0x10, 16);
            Buffer.BlockCopy(dropItemRight, 0, b2, slot * 0x10, 16);
        }

        private void deleteItem(floorSlot btn)
        {
            string address1;
            string address2;
            string address3;
            string address4;

            if (layer1Btn.Checked)
            {
                address1 = getAddress(btn.mapX, btn.mapY).ToString("X");
                address2 = (getAddress(btn.mapX, btn.mapY) + 0x600).ToString("X");
                address3 = (getAddress(btn.mapX, btn.mapY) + Utilities.mapOffset).ToString("X");
                address4 = (getAddress(btn.mapX, btn.mapY) + 0x600 + Utilities.mapOffset).ToString("X");
            }
            else if (layer2Btn.Checked)
            {
                address1 = (getAddress(btn.mapX, btn.mapY) + Utilities.mapSize).ToString("X");
                address2 = (getAddress(btn.mapX, btn.mapY) + 0x600 + Utilities.mapSize).ToString("X");
                address3 = (getAddress(btn.mapX, btn.mapY) + Utilities.mapOffset + Utilities.mapSize).ToString("X");
                address4 = (getAddress(btn.mapX, btn.mapY) + 0x600 + Utilities.mapOffset + Utilities.mapSize).ToString("X");
            }
            else
                return;

            Utilities.deleteFloorItem(s, bot, address1, address2, address3, address4);
            updataData(selectedButton.mapX, selectedButton.mapY);

            btn.reset();
            btnToolTip.RemoveAll();
        }

        private void copyItem(floorSlot btn)
        {
            string id = Utilities.precedingZeros(btn.itemID.ToString("X"), 4);
            string name = btn.Name;
            string hexValue = Utilities.precedingZeros(btn.itemData.ToString("X"), 8);
            string flag1 = btn.flag1;
            string flag2 = btn.flag2;

            IdTextbox.Text = id;
            HexTextbox.Text = hexValue;
            FlagTextbox.Text = flag2;

            if (id == "16A2")
                selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(Utilities.turn2bytes(hexValue), recipeSource), true, "", flag1, flag2);
            else
                selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(id, source), true, "", flag1, flag2);
        }

        private void KeyboardKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.ToString() == "F2" || e.KeyCode.ToString() == "Insert")
            {
                if (selectedButton != null & (s != null || bot != null))
                {
                    selectedItem_Click(sender, e);
                }
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
            }
            else if (e.KeyCode.ToString() == "F1") // Delete
            {
                if (selectedButton != null & (s != null || bot != null))
                {
                    deleteItem(selectedButton);
                }
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
            }
            else if (e.KeyCode.ToString() == "F3") // Copy
            {
                if (selectedButton != null & (s != null || bot != null))
                {
                    copyItem(selectedButton);
                }
                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
            }
            else if (e.KeyCode.ToString() == "End")
            {
                if (fieldGridView.Rows.Count <= 0)
                {
                    return;
                }
                else if (fieldGridView.Rows.Count == 1)
                {
                    lastRow = fieldGridView.Rows[fieldGridView.CurrentRow.Index];
                    fieldGridView.Rows[fieldGridView.CurrentRow.Index].Height = 160;

                    KeyPressSetup(fieldGridView.CurrentRow.Index);
                }
                else if (fieldGridView.CurrentRow.Index + 1 < fieldGridView.Rows.Count)
                {
                    if (lastRow != null)
                    {
                        lastRow.Height = 22;
                    }
                    lastRow = fieldGridView.Rows[fieldGridView.CurrentRow.Index + 1];
                    fieldGridView.Rows[fieldGridView.CurrentRow.Index + 1].Height = 160;

                    KeyPressSetup(fieldGridView.CurrentRow.Index + 1);
                    fieldGridView.CurrentCell = fieldGridView.Rows[fieldGridView.CurrentRow.Index + 1].Cells[fieldGridView.CurrentCell.ColumnIndex];
                }

            }
            else if (e.KeyCode.ToString() == "Home")
            {
                if (fieldGridView.Rows.Count <= 0)
                {
                    return;
                }
                else if (fieldGridView.Rows.Count == 1)
                {
                    lastRow = fieldGridView.Rows[fieldGridView.CurrentRow.Index];
                    fieldGridView.Rows[fieldGridView.CurrentRow.Index].Height = 160;

                    KeyPressSetup(fieldGridView.CurrentRow.Index);
                }
                else if (fieldGridView.CurrentRow.Index > 0)
                {
                    if (lastRow != null)
                    {
                        lastRow.Height = 22;
                    }

                    lastRow = fieldGridView.Rows[fieldGridView.CurrentRow.Index - 1];
                    fieldGridView.Rows[fieldGridView.CurrentRow.Index - 1].Height = 160;

                    KeyPressSetup(fieldGridView.CurrentRow.Index - 1);
                    fieldGridView.CurrentCell = fieldGridView.Rows[fieldGridView.CurrentRow.Index - 1].Cells[fieldGridView.CurrentCell.ColumnIndex];
                }
            }
        }

        private void KeyPressSetup(int index)
        {
            if (currentDataTable == source)
            {
                string id = fieldGridView.Rows[index].Cells["id"].Value.ToString();
                string name = fieldGridView.Rows[index].Cells["eng"].Value.ToString();

                IdTextbox.Text = id;
                HexTextbox.Text = "00000000";

                selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), 0x0, GetImagePathFromID(id, source), true, "");
            }
            else if (currentDataTable == recipeSource)
            {
                string id = "16A2"; // Recipe;
                string name = fieldGridView.Rows[index].Cells["eng"].Value.ToString();
                string hexValue = fieldGridView.Rows[index].Cells["id"].Value.ToString();

                IdTextbox.Text = id;
                HexTextbox.Text = Utilities.precedingZeros(hexValue, 8);

                selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(hexValue, recipeSource), true, "");
            }
            else if (currentDataTable == flowerSource)
            {
                string id = fieldGridView.Rows[index].Cells["id"].Value.ToString();
                string name = fieldGridView.Rows[index].Cells["eng"].Value.ToString();
                string hexValue = fieldGridView.Rows[index].Cells["value"].Value.ToString();

                IdTextbox.Text = id;
                HexTextbox.Text = Utilities.precedingZeros(hexValue, 8);

                selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(id, source), true, "");

            }
            else if (currentDataTable == favSource)
            {
                string id = fieldGridView.Rows[index].Cells["id"].Value.ToString();
                string name = fieldGridView.Rows[index].Cells["Name"].Value.ToString();
                string hexValue = fieldGridView.Rows[index].Cells["value"].Value.ToString();

                IdTextbox.Text = id;
                HexTextbox.Text = Utilities.precedingZeros(hexValue, 8);

                selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(id, source), true, "");
            }
            else if (currentDataTable == fieldSource)
            {
                string id = fieldGridView.Rows[index].Cells["id"].Value.ToString();
                string name = fieldGridView.Rows[index].Cells["Name"].Value.ToString();
                string hexValue = fieldGridView.Rows[index].Cells["value"].Value.ToString();

                IdTextbox.Text = id;
                HexTextbox.Text = Utilities.precedingZeros(hexValue, 8);

                selectedItem.setup(name, Convert.ToUInt16("0x" + id, 16), Convert.ToUInt32("0x" + hexValue, 16), GetImagePathFromID(id, fieldSource), true, "");
            }
        }

        private void updataData(int x, int y, string itemID, string itemData, string flag2)
        {
            byte[] Left = Utilities.stringToByte(Utilities.buildDropStringLeft(itemID, itemData, "00", flag2));
            byte[] Right = Utilities.stringToByte(Utilities.buildDropStringRight(itemID));

            if (layer1Btn.Checked)
            {
                Buffer.BlockCopy(Left, 0, Layer1, x * 0xC00 + y * 0x10, 16);
                Buffer.BlockCopy(Right, 0, Layer1, x * 0xC00 + 0x600 + y * 0x10, 16);
                miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer1);
            }
            else if (layer2Btn.Checked)
            {
                Buffer.BlockCopy(Left, 0, Layer2, x * 0xC00 + y * 0x10, 16);
                Buffer.BlockCopy(Right, 0, Layer2, x * 0xC00 + 0x600 + y * 0x10, 16);
                miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer2);
            }
        }

        private void updataData(int x, int y)
        {
            byte[] Left = Utilities.stringToByte(Utilities.buildDropStringLeft("FFFE", "00000000", "00", "00", true));
            byte[] Right = Utilities.stringToByte(Utilities.buildDropStringRight("FFFE", true));

            if (layer1Btn.Checked)
            {
                Buffer.BlockCopy(Left, 0, Layer1, x * 0xC00 + y * 0x10, 16);
                Buffer.BlockCopy(Right, 0, Layer1, x * 0xC00 + 0x600 + y * 0x10, 16);
                miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer1);
            }
            else if (layer2Btn.Checked)
            {
                Buffer.BlockCopy(Left, 0, Layer2, x * 0xC00 + y * 0x10, 16);
                Buffer.BlockCopy(Right, 0, Layer2, x * 0xC00 + 0x600 + y * 0x10, 16);
                miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer2);
            }
        }

        public void updataData(int x, int y, byte[][] newData, bool topleft = true)
        {
            if (topleft)
            {
                for (int i = 0; i < newData.Length / 2; i++)
                {
                    if (layer1Btn.Checked)
                    {
                        Buffer.BlockCopy(newData[i * 2], 0, Layer1, (x - 3 + i) * 0xC00 + (y - 3) * 0x10, newData[i * 2].Length);
                        Buffer.BlockCopy(newData[i * 2 + 1], 0, Layer1, (x - 3 + i) * 0xC00 + 0x600 + (y - 3) * 0x10, newData[i * 2 + 1].Length);
                        miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer1);
                    }
                    else if (layer2Btn.Checked)
                    {
                        Buffer.BlockCopy(newData[i * 2], 0, Layer2, (x - 3 + i) * 0xC00 + (y - 3) * 0x10, newData[i * 2].Length);
                        Buffer.BlockCopy(newData[i * 2 + 1], 0, Layer2, (x - 3 + i) * 0xC00 + 0x600 + (y - 3) * 0x10, newData[i * 2 + 1].Length);
                        miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer2);
                    }
                }
            }
            else
            {
                for (int i = 0; i < newData.Length / 2; i++)
                {
                    if (layer1Btn.Checked)
                    {
                        Buffer.BlockCopy(newData[i * 2], 0, Layer1, (x + i) * 0xC00 + (y) * 0x10, newData[i * 2].Length);
                        Buffer.BlockCopy(newData[i * 2 + 1], 0, Layer1, (x + i) * 0xC00 + 0x600 + (y) * 0x10, newData[i * 2 + 1].Length);
                        miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer1);
                    }
                    else if (layer2Btn.Checked)
                    {
                        Buffer.BlockCopy(newData[i * 2], 0, Layer2, (x + i) * 0xC00 + (y) * 0x10, newData[i * 2].Length);
                        Buffer.BlockCopy(newData[i * 2 + 1], 0, Layer2, (x + i) * 0xC00 + 0x600 + (y) * 0x10, newData[i * 2 + 1].Length);
                        miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer2);
                    }
                }
            }
        }

        private void map_FormClosed(object sender, FormClosedEventArgs e)
        {
            main.Map = null;
            if (selection != null)
            {
                selection.Close();
                selection = null;
            }
        }

        private void layer1Btn_Click(object sender, EventArgs e)
        {
            functionPanel.Enabled = true;
            miniMapBox.BackgroundImage = null;
            miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer1);
            displayAnchor(getMapColumns(anchorX, anchorY));
            resetBtnColor();
        }

        private void layer2Btn_Click(object sender, EventArgs e)
        {
            functionPanel.Enabled = false;
            miniMapBox.BackgroundImage = null;
            miniMapBox.BackgroundImage = MiniMap.refreshItemMap(Layer2);
            displayAnchor(getMapColumns(anchorX, anchorY));
            resetBtnColor();
        }

        private void variationButton_Click(object sender, EventArgs e)
        {
            if (selection == null)
            {
                openVariationMenu();
            }
            else
            {
                closeVariationMenu();
            }
        }

        private void openVariationMenu()
        {
            selection = new variation(115);
            selection.Show();
            selection.Location = new System.Drawing.Point(this.Location.X + 533, this.Location.Y + 660);
            string id = Utilities.precedingZeros(selectedItem.fillItemID(), 4);
            if (id == "315A" || id == "1618")
            {
                selection.receiveID(Utilities.turn2bytes(selectedItem.fillItemData()), "eng");
            }
            else
            {
                selection.receiveID(Utilities.precedingZeros(selectedItem.fillItemID(), 4), "eng");
            }
            selection.mapform = this;
            variationBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(255)))));
        }

        private void closeVariationMenu()
        {
            if (selection != null)
            {
                selection.Dispose();
                selection = null;
                variationBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(137)))), ((int)(((byte)(218)))));
            }
        }

        public void ReceiveVariation(inventorySlot select, int type = 0)
        {
            if (type == 0) //Left click
            {
                selectedItem.setup(select);
                //updateSelectedItemInfo(selectedItem.displayItemName(), selectedItem.displayItemID(), selectedItem.displayItemData());
                IdTextbox.Text = Utilities.precedingZeros(selectedItem.fillItemID(), 4);
                HexTextbox.Text = Utilities.precedingZeros(selectedItem.fillItemData(), 8);
            }
            else if (type == 1) // Right click
            {
                if (IdTextbox.Text == "315A" || IdTextbox.Text == "1618")
                {
                    string count = translateVariationValue(select.fillItemData()) + Utilities.precedingZeros(select.fillItemID(), 4);
                    selectedItem.setup(GetNameFromID(Utilities.turn2bytes(IdTextbox.Text), source), Convert.ToUInt16("0x" + IdTextbox.Text, 16), Convert.ToUInt32("0x" + count, 16), GetImagePathFromID(Utilities.turn2bytes(IdTextbox.Text), source), true, select.getPath(), selectedItem.getFlag1(), selectedItem.getFlag2());
                    HexTextbox.Text = count;
                }
            }
        }

        private string translateVariationValue(string input)
        {
            int hexValue = Convert.ToUInt16("0x" + input, 16);
            int firstHalf = 0;
            int secondHalf = 0;
            string output;

            if (hexValue <= 0x7)
            {
                return Utilities.precedingZeros(input, 4);
            }
            else if (hexValue <= 0x27)
            {
                firstHalf = (0x20 / 4);
                secondHalf = (hexValue - 0x20);
            }
            else if (hexValue <= 0x47)
            {
                firstHalf = (0x40 / 4);
                secondHalf = (hexValue - 0x40);
            }
            else if (hexValue <= 0x67)
            {
                firstHalf = (0x60 / 4);
                secondHalf = (hexValue - 0x60);
            }
            else if (hexValue <= 0x87)
            {
                firstHalf = (0x80 / 4);
                secondHalf = (hexValue - 0x80);
            }
            else if (hexValue <= 0xA7)
            {
                firstHalf = (0xA0 / 4);
                secondHalf = (hexValue - 0xA0);
            }
            else if (hexValue <= 0xC7)
            {
                firstHalf = (0xC0 / 4);
                secondHalf = (hexValue - 0xC0);
            }
            else if (hexValue <= 0xE7)
            {
                firstHalf = (0xE0 / 4);
                secondHalf = (hexValue - 0xE0);
            }

            output = Utilities.precedingZeros((firstHalf + secondHalf).ToString("X"), 4);
            return output;
        }

        private void map_LocationChanged(object sender, EventArgs e)
        {
            if (selection != null)
            {
                selection.Location = new System.Drawing.Point(this.Location.X + 533, this.Location.Y + 660);
            }
        }

        private void miniMapBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Debug.Print(e.X.ToString() + " " + e.Y.ToString());

                int x;
                int y;

                if (e.X / 2 < 3)
                    x = 3;
                else if (e.X / 2 > 108)
                    x = 108;
                else
                    x = e.X / 2;

                if (e.Y / 2 < 3)
                    y = 3;
                else if (e.Y / 2 > 92)
                    y = 92;
                else
                    y = e.Y / 2;

                anchorX = x;
                anchorY = y;

                xCoordinate.Text = x.ToString();
                yCoordinate.Text = y.ToString();
                selectedButton = null;
                displayAnchor(getMapColumns(anchorX, anchorY));
            }
        }

        private void miniMapBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                int x;
                int y;

                if (e.X / 2 < 3)
                    x = 3;
                else if (e.X / 2 > 108)
                    x = 108;
                else
                    x = e.X / 2;

                if (e.Y / 2 < 3)
                    y = 3;
                else if (e.Y / 2 > 92)
                    y = 92;
                else
                    y = e.Y / 2;

                anchorX = x;
                anchorY = y;

                xCoordinate.Text = x.ToString();
                yCoordinate.Text = y.ToString();
                selectedButton = null;
                displayAnchor(getMapColumns(anchorX, anchorY));
            }
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                if (counter <= MapProgressBar.Maximum)
                    MapProgressBar.Value = counter;
                else
                    MapProgressBar.Value = MapProgressBar.Maximum;
            });
        }

        private void showMapWait(int size, string msg = "")
        {
            this.Invoke((MethodInvoker)delegate
            {
                WaitMessagebox.SelectionAlignment = HorizontalAlignment.Center;
                WaitMessagebox.Text = msg;
                counter = 0;
                MapProgressBar.Maximum = size + 5;
                MapProgressBar.Value = counter;
                PleaseWaitPanel.Visible = true;
                ProgressTimer.Start();
            });
        }

        private void hideMapWait()
        {
            this.Invoke((MethodInvoker)delegate
            {
                PleaseWaitPanel.Visible = false;
                ProgressTimer.Stop();
            });
        }

        private void disableBtn()
        {
            BtnPanel.Enabled = false;
            functionPanel.Enabled = false;
            moveRightBtn.Enabled = false;
            moveLeftBtn.Enabled = false;
            moveUpBtn.Enabled = false;
            moveDownBtn.Enabled = false;
            moveUpRightBtn.Enabled = false;
            moveUpLeftBtn.Enabled = false;
            moveDownRightBtn.Enabled = false;
            moveDownLeftBtn.Enabled = false;
            moveRight7Btn.Enabled = false;
            moveLeft7Btn.Enabled = false;
            moveUp7Btn.Enabled = false;
            moveDown7Btn.Enabled = false;
        }

        private void enableBtn()
        {
            BtnPanel.Enabled = true;
            functionPanel.Enabled = true;
            moveRightBtn.Enabled = true;
            moveLeftBtn.Enabled = true;
            moveUpBtn.Enabled = true;
            moveDownBtn.Enabled = true;
            moveUpRightBtn.Enabled = true;
            moveUpLeftBtn.Enabled = true;
            moveDownRightBtn.Enabled = true;
            moveDownLeftBtn.Enabled = true;
            moveRight7Btn.Enabled = true;
            moveLeft7Btn.Enabled = true;
            moveUp7Btn.Enabled = true;
            moveDown7Btn.Enabled = true;
        }

        private void saveTopngToolStripMenuItem_Click(object sender, EventArgs e)
        {
            miniMap big = new miniMap(Layer1, Acre, 4);
                SaveFileDialog file = new SaveFileDialog()
                {
                    Filter = "Portable Network Graphics (*.png)|*.png",
                };

                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                string savepath;

                if (config.AppSettings.Settings["LastSave"].Value.Equals(string.Empty))
                    savepath = Directory.GetCurrentDirectory() + @"\save";
                else
                    savepath = config.AppSettings.Settings["LastSave"].Value;

                if (Directory.Exists(savepath))
                {
                    file.InitialDirectory = savepath;
                }
                else
                {
                    file.InitialDirectory = @"C:\";
                }

                if (file.ShowDialog() != DialogResult.OK)
                    return;

                string[] temp = file.FileName.Split('\\');
                string path = "";
                for (int i = 0; i < temp.Length - 1; i++)
                    path = path + temp[i] + "\\";

                config.AppSettings.Settings["LastSave"].Value = path;
                config.Save(ConfigurationSaveMode.Minimal);

            big.combineMap(big.drawBackground(), big.drawItemMap()).Save(file.FileName);

                if (sound)
                    System.Media.SystemSounds.Asterisk.Play();
        }

        private void bulkSpawnBtn_Click(object sender, EventArgs e)
        {
            if (bulk == null)
                bulk = new bulkSpawn(s, bot, Layer1, Layer2, Acre, anchorX, anchorY, this, sound);
            bulk.ShowDialog();
        }
    }
}
