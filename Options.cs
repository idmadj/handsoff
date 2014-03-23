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
        public Options()
        {
            InitializeComponent();
        }

        public List<String> devices
        {
            get
            {
                return comboBox1.DataSource as List<string>;
            }

            set
            {
                comboBox1.DataSource = value;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
