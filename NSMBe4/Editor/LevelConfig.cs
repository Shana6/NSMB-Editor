﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NSMBe4.Filesystem;

namespace NSMBe4 {
    public partial class LevelConfig : Form {
        public LevelConfig(NSMBLevel Level, NitroClass ROM) {
            InitializeComponent();
            this.Level = Level;
            this.ROM = ROM;
            tabControl1.SelectTab(0);

            LanguageManager.ApplyToContainer(this, "LevelConfig");

            string[] parsedlist;
            int index;

            // Add tilesets to list
            index = 0;
            parsedlist = new string[76];
            foreach (string name in LanguageManager.GetList("Tilesets")) {
                string trimmedname = name.Trim();
                if (trimmedname == "") continue;
                parsedlist[index] = trimmedname;
                index += 1;
            }

            tilesetComboBox.Items.AddRange(parsedlist);

            // Add foregrounds to list
            index = 0;
            parsedlist = new string[77];
            foreach (string name in LanguageManager.GetList("Foregrounds")) {
                string trimmedname = name.Trim();
                if (trimmedname == "") continue;
                parsedlist[index] = trimmedname;
                index += 1;
            }

            bgTopLayerComboBox.Items.AddRange(parsedlist);

            // Add backgrounds
            index = 0;
            parsedlist = new string[77];
            foreach (string name in LanguageManager.GetList("Backgrounds")) {
                string trimmedname = name.Trim();
                if (trimmedname == "") continue;
                parsedlist[index] = trimmedname;
                index += 1;
            }

            bgBottomLayerComboBox.Items.AddRange(parsedlist);

            // Load modifier lists
            ComboBox target = null;
            foreach (string name in LanguageManager.GetList("Modifiers")) {
                string trimmedname = name.Trim();
                if (trimmedname == "") continue;
                if (trimmedname[0] == '-') {
                    switch (trimmedname) {
                        case "-1": target = set1ComboBox; break;
                        case "-2": target = set2ComboBox; break;
                        case "-3": target = set3ComboBox; break;
                        case "-4": target = set4ComboBox; break;
                        case "-5": target = set5ComboBox; break;
                        case "-6": target = set6ComboBox; break;
                        case "-7": target = set7ComboBox; break;
                        case "-8": target = set8ComboBox; break;
                        case "-9": target = set9ComboBox; break;
                        case "-10": target = set10ComboBox; break;
                        case "-16": target = set16ComboBox; break;
                    }
                } else {
                    target.Items.Add(trimmedname);
                }
            }
        }

        private NSMBLevel Level;
        private NitroClass ROM;

        public delegate void SetDirtyFlagDelegate();
        public event SetDirtyFlagDelegate SetDirtyFlag;

        public delegate void ReloadTilesetDelegate();
        public event ReloadTilesetDelegate ReloadTileset;

        public delegate void RefreshMainWindowDelegate();
        public event RefreshMainWindowDelegate RefreshMainWindow;

        public void LoadSettings() {
            startEntranceUpDown.Value = Level.Blocks[0][0];
            midwayEntranceUpDown.Value = Level.Blocks[0][1];
            timeLimitUpDown.Value = Level.Blocks[0][4] | (Level.Blocks[0][5] << 8);
            levelWrapCheckBox.Checked = ((Level.Blocks[0][2] & 0x20) != 0);

            tilesetComboBox.SelectedIndex = Level.Blocks[0][0xC];
            int FGIndex = Level.Blocks[0][0x12];
            if (FGIndex == 255) FGIndex = bgTopLayerComboBox.Items.Count - 1;
            bgTopLayerComboBox.SelectedIndex = FGIndex;
            int BGIndex = Level.Blocks[0][6];
            if (BGIndex == 255) BGIndex = bgBottomLayerComboBox.Items.Count - 1;
            bgBottomLayerComboBox.SelectedIndex = BGIndex;

            ComboBox[] checkthese = new ComboBox[] {
                set1ComboBox, set2ComboBox, set3ComboBox, set4ComboBox,
                set5ComboBox, set6ComboBox, set7ComboBox, set8ComboBox,
                set9ComboBox, set10ComboBox, set16ComboBox
            };

            int[] checkthese_idx = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 15 };

            if (Level.Blocks[13].Length == 0) {
                // works around levels like 1-4 area 2 which have a blank modifier block
                Level.Blocks[13] = new byte[16];
            }

            for (int CheckIdx = 0; CheckIdx < checkthese.Length; CheckIdx++) {
                int valid = Level.Blocks[13][checkthese_idx[CheckIdx]];
                for (int ItemIdx = 0; ItemIdx < checkthese[CheckIdx].Items.Count; ItemIdx++) {
                    string Item = (string)(checkthese[CheckIdx].Items[ItemIdx]);
                    int cpos = Item.IndexOf(':');
                    int modifierval = int.Parse(Item.Substring(0, cpos));
                    if (modifierval == valid) {
                        checkthese[CheckIdx].SelectedIndex = ItemIdx;
                        break;
                    }
                }
            }

        }

        private void tilesetPreviewButton_Click(object sender, EventArgs e) {
            ushort GFXFile = NSMBDataHandler.GetFileIDFromTable(tilesetComboBox.SelectedIndex, NSMBDataHandler.Data.Table_TS_NCG);
            ushort PalFile = NSMBDataHandler.GetFileIDFromTable(tilesetComboBox.SelectedIndex, NSMBDataHandler.Data.Table_TS_NCL);

            GraphicsViewer gv = new GraphicsViewer();
            gv.SetPreferredWidth(256);
            gv.SetFile(ROM.ExtractFile(GFXFile));
            gv.SetPalette(ROM.ExtractFile(PalFile));
            gv.Show();
        }

        private void bgTopLayerPreviewButton_Click(object sender, EventArgs e) {
            if (bgTopLayerComboBox.SelectedIndex == bgTopLayerComboBox.Items.Count - 1) {
                MessageBox.Show(LanguageManager.Get("LevelConfig", "BlankBG"));
                return;
            }

            ushort GFXFile = NSMBDataHandler.GetFileIDFromTable(bgTopLayerComboBox.SelectedIndex, NSMBDataHandler.Data.Table_FG_NCG);
            ushort PalFile = NSMBDataHandler.GetFileIDFromTable(bgTopLayerComboBox.SelectedIndex, NSMBDataHandler.Data.Table_FG_NCL);
            ushort LayoutFile = NSMBDataHandler.GetFileIDFromTable(bgTopLayerComboBox.SelectedIndex, NSMBDataHandler.Data.Table_FG_NSC);

            if (GFXFile >= 2088 || PalFile >= 2088 || LayoutFile >= 2088) {
                MessageBox.Show(LanguageManager.Get("LevelConfig", "BrokenBG"));
                return;
            }

            ShowBackground(GFXFile, PalFile, LayoutFile, 256);
        }

        private void bgBottomLayerPreviewButton_Click(object sender, EventArgs e) {
            if (bgBottomLayerComboBox.SelectedIndex == bgBottomLayerComboBox.Items.Count - 1) {
                MessageBox.Show(LanguageManager.Get("LevelConfig", "BlankBG"));
                return;
            }

            ushort GFXFile = NSMBDataHandler.GetFileIDFromTable(bgBottomLayerComboBox.SelectedIndex, NSMBDataHandler.Data.Table_BG_NCG);
            ushort PalFile = NSMBDataHandler.GetFileIDFromTable(bgBottomLayerComboBox.SelectedIndex, NSMBDataHandler.Data.Table_BG_NCL);
            ushort LayoutFile = NSMBDataHandler.GetFileIDFromTable(bgBottomLayerComboBox.SelectedIndex, NSMBDataHandler.Data.Table_BG_NSC);

            if (GFXFile >= 2088 || PalFile >= 2088 || LayoutFile >= 2088) {
                MessageBox.Show(LanguageManager.Get("LevelConfig", "BrokenBG"));
                return;
            }

            ShowBackground(GFXFile, PalFile, LayoutFile, 576);
        }

        private void ShowBackground(ushort GFXFile, ushort PalFile, ushort LayoutFile, int TileOffset) {
            int FilePos;

            // First get the palette out
            byte[] ePalFile = FileSystem.LZ77_Decompress(ROM.ExtractFile(PalFile));
            Color[] Palette = new Color[512];

            for (int PalIdx = 0; PalIdx < 512; PalIdx++) {
                int ColourVal = ePalFile[PalIdx * 2] + (ePalFile[(PalIdx * 2) + 1] << 8);
                int cR = (ColourVal & 31) * 8;
                int cG = ((ColourVal >> 5) & 31) * 8;
                int cB = ((ColourVal >> 10) & 31) * 8;
                Palette[PalIdx] = Color.FromArgb(cR, cG, cB);
            }

            Palette[0] = Color.LightSlateGray;
            Palette[256] = Color.LightSlateGray;

            // Load graphics
            byte[] eGFXFile = FileSystem.LZ77_Decompress(ROM.ExtractFile(GFXFile));
            int TileCount = eGFXFile.Length / 64;
            Bitmap TilesetBuffer = new Bitmap(TileCount * 8, 16);

            FilePos = 0;
            for (int TileIdx = 0; TileIdx < TileCount; TileIdx++)
            {
                int TileSrcX = TileIdx * 8;
                for (int TileY = 0; TileY < 8; TileY++)
                {
                    for (int TileX = 0; TileX < 8; TileX++)
                    {
                        TilesetBuffer.SetPixel(TileSrcX + TileX, TileY, Palette[eGFXFile[FilePos]]);
                        TilesetBuffer.SetPixel(TileSrcX + TileX, TileY + 8, Palette[eGFXFile[FilePos] + 256]);
                        FilePos++;
                    }
                }
            }

            // Load layout
            byte[] eLayoutFile = FileSystem.LZ77_Decompress(ROM.ExtractFile(LayoutFile));
            int LayoutCount = eLayoutFile.Length / 2;
            Bitmap BG = new Bitmap(512, 512, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            Graphics BGGraphics = Graphics.FromImage(BG);
            BGGraphics.Clear(Color.LightSlateGray);

            FilePos = 0;
            int TileNum;
            byte ControlByte;
            Rectangle SrcRect;
            int SrcX = 0;
            int SrcY = 0;
            Bitmap fliptile = new Bitmap(8, 8);
            Graphics g = Graphics.FromImage(fliptile);
            for (int TileIdx = 0; TileIdx < LayoutCount; TileIdx++) {
                TileNum = eLayoutFile[FilePos];
                ControlByte = eLayoutFile[FilePos + 1];
                TileNum |= (ControlByte & 3) << 8;
                TileNum -= TileOffset;
                SrcRect = new Rectangle(TileNum * 8, (ControlByte & 16) != 0 ? 8 : 0, 8, 8);
                if ((ControlByte & 4) != 0 || (ControlByte & 8) != 0) {
                    g.DrawImage(TilesetBuffer, 0, 0, SrcRect, GraphicsUnit.Pixel);
                    if ((ControlByte & 4) != 0)
                        fliptile.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    if ((ControlByte & 8) != 0)
                        fliptile.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    BGGraphics.DrawImage(fliptile, SrcX, SrcY);
                } else {
                    BGGraphics.DrawImage(TilesetBuffer, SrcX, SrcY, SrcRect, GraphicsUnit.Pixel);
                }
                SrcX += 8;
                if (SrcX >= 512) { SrcX = 0; SrcY += 8; }
                FilePos += 2;
            }

            new ImagePreviewer(BG).Show();
        }

        private void OKButton_Click(object sender, EventArgs e) {
            Level.Blocks[0][0] = (byte)startEntranceUpDown.Value;
            Level.Blocks[0][1] = (byte)midwayEntranceUpDown.Value;
            Level.Blocks[0][4] = (byte)((int)timeLimitUpDown.Value & 255);
            Level.Blocks[0][5] = (byte)((int)timeLimitUpDown.Value >> 8);

            if (levelWrapCheckBox.Checked) {
                Level.Blocks[0][2] = (byte)(Level.Blocks[0][2] | 32);
            } else {
                Level.Blocks[0][2] = (byte)(Level.Blocks[0][2] & 223);
            }

            int oldTileset = Level.Blocks[0][0xC];

            Level.Blocks[0][0xC] = (byte)tilesetComboBox.SelectedIndex; // ncg
            Level.Blocks[3][4] = (byte)tilesetComboBox.SelectedIndex; // ncl

            int FGIndex = bgTopLayerComboBox.SelectedIndex;
            if (FGIndex == bgTopLayerComboBox.Items.Count - 1) FGIndex = 255;
            Level.Blocks[0][0x12] = (byte)FGIndex; // ncg
            Level.Blocks[4][4] = (byte)FGIndex; // ncl
            Level.Blocks[4][2] = (byte)FGIndex; // nsc

            int BGIndex = bgBottomLayerComboBox.SelectedIndex;
            if (BGIndex == bgBottomLayerComboBox.Items.Count - 1) BGIndex = 255;
            Level.Blocks[0][6] = (byte)BGIndex; // ncg
            Level.Blocks[2][4] = (byte)BGIndex; // ncl
            Level.Blocks[2][2] = (byte)BGIndex; // nsc

            if (oldTileset != Level.Blocks[0][0xC]) {
                ReloadTileset();
            }

            ComboBox[] checkthese = new ComboBox[] {
                set1ComboBox, set2ComboBox, set3ComboBox, set4ComboBox,
                set5ComboBox, set6ComboBox, set7ComboBox, set8ComboBox,
                set9ComboBox, set10ComboBox, set16ComboBox
            };

            int[] checkthese_idx = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 15 };

            for (int CheckIdx = 0; CheckIdx < checkthese.Length; CheckIdx++) {
                string Item = (string)(checkthese[CheckIdx].Items[checkthese[CheckIdx].SelectedIndex]);
                int cpos = Item.IndexOf(':');
                int modifierval = int.Parse(Item.Substring(0, cpos));
                Level.Blocks[13][checkthese_idx[CheckIdx]] = (byte)modifierval;
            }

            Level.CalculateSpriteModifiers();

            SetDirtyFlag();
            RefreshMainWindow();
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            Close();
        }
    }
}