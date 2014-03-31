using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace handsoff
{
    public partial class Options : Form
    {
        public event BasicEvent OKClicked;
        public event BasicEvent CancelClicked;

        public Options()
        {
            Icon = Properties.Resources.TouchOn;

            InitializeComponent();

            Version version = new Version(Application.ProductVersion);
            label2.Text = Application.ProductName + " " + version.Major + "." + version.Minor + " © 2014 Abdelmadjid Hammou. All Rights Reserved.";

            comboBox1.DisplayMember = "name";
            comboBox1.ValueMember = "instancePath";
        }

        public List<Device> devices
        {
            get
            {
                return comboBox1.DataSource as List<Device>;
            }

            set
            {
                comboBox1.DataSource = value;
            }
        }

        public string selectedDevice
        {
            get 
            {
                return comboBox1.SelectedValue.ToString();
            }

            set 
            { 
                comboBox1.SelectedValue = value; 
            }
            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            updateSettings();
            Close();

            if (OKClicked != null)
            {
                OKClicked(this);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();

            if (CancelClicked != null)
            {
                CancelClicked(this);
            }
        }

        private void updateSettings()
        {
            Properties.Settings defaultSettings = Properties.Settings.Default;

            defaultSettings.controlledDevice = comboBox1.SelectedValue.ToString();
        }

    }
}
