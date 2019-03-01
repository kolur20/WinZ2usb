using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinZ2usb
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            ShowInTaskbar = false;
            Reader.Reader.InitializaeZ2(l_status);
            
        }

        private void b_update_Click(object sender, EventArgs e)
        {
            b_update.Image = 
        }

        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reader.Reader.Dispose();
            Close();
        }
    }
}
