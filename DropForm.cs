////////////////////////////////////////////////////////////////////////////////
// The MIT License (MIT)
//
// Copyright (c) 2024 Tim Stair
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using System.Windows.Forms;
using Support.IO;
using Support.UI;

namespace I2I
{
    public partial class DropForm : Form, ILogger
    {
        private readonly ILogger m_zLogger;
        private readonly ImageConverter m_zImageConverter;
        private readonly IniManager m_zIniManager;

        public DropForm()
        {
            InitializeComponent();
            m_zIniManager = new IniManager("i2i", false);
            m_zLogger = this;
            m_zImageConverter = new ImageConverter(m_zLogger);
            Text = $"I2I {Application.ProductVersion}";
        }

        #region Form Events
        private void DropForm_Load(object sender, EventArgs e)
        {
            comboImageFormat.Items.AddRange(Enum.GetValues(typeof(ImageExportFormat))
                .Cast<object>()
                .ToArray());
            comboImageFormat.SelectedIndex = (int)ImageExportFormat.Png;

            lblDragDrop.Text +=
                $"{Environment.NewLine}{Environment.NewLine}Supported extensions: " +
                $"{string.Join(",", Enum.GetValues(typeof(ImageExportFormat)).Cast<ImageExportFormat>().ToList())}";

            var sFormIniData = m_zIniManager.GetValue(Name);
            if (!string.IsNullOrEmpty(sFormIniData))
            {
                IniManager.RestoreState(this, sFormIniData);
                IniManager.ValidateScreenPosition(this);
            }
        }

        private void DropForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_zIniManager.AutoFlush = false;
            m_zIniManager.SetValue(Name, IniManager.GetFormSettings(this));
            m_zIniManager.FlushIniSettings();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region Drag Drop Events

        private void txtSetsToRip_DragEnter(object sender, DragEventArgs e)
        {
            // Note the e.AllowedEffect field (not copy but link)
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Link
                : DragDropEffects.None;
        }

        private void txtSetsToRip_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            ((string[])e.Data.GetData(DataFormats.FileDrop))
                .ToList()
                .ForEach(sFile => 
                    m_zImageConverter.ConvertImageFile(sFile, (ImageExportFormat)comboImageFormat.SelectedIndex));
        }

        #endregion

        #region ILogger

        public void AddLogLine(string sLine)
        {
            AddLogLines(new[]{sLine});
        }

        public void AddLogLines(string[] arrayLines)
        {
            if (listBoxLog.InvokeActionIfRequired(() => AddLogLines(arrayLines)))
            {
                listBoxLog.BeginUpdate();
                foreach (string sLine in arrayLines)
                {
                    listBoxLog.SelectedIndex = listBoxLog.Items.Add(DateTime.Now.ToString("HH:mm:ss.ff") + "::" + sLine);
                }
                listBoxLog.SelectedIndex = -1;
                listBoxLog.EndUpdate();
            }
        }

        public void ClearLog()
        {
            listBoxLog.InvokeAction(() => listBoxLog.Items.Clear());
        }

        #endregion

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
