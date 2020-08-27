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
            picture.SizeMode = PictureBoxSizeMode.StretchImage;
            picture.Image = Properties.Resources.load;
            //picture.Load(Properties.Settings.Default.ImName);
            //picture.Load("load1.gif");
            

        }



        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reader.Reader.Dispose();
            Reader.Reader.LoggerMessage("Закрытие приложеия...");
            Close();
        }

        private void picture_Click(object sender, EventArgs e)
        {
            picture.Image = Properties.Resources.load1;
            //picture.Load(Properties.Settings.Default.GifName);
            t_load.Interval = 5000;
            t_load.Start();
            Reader.Reader.InitializaeZ2(l_status);
        }

        private void t_load_Tick(object sender, EventArgs e)
        {
            picture.Image = Properties.Resources.load;
            //picture.Load(Properties.Settings.Default.ImName);
            t_load.Stop();
        }
    }
}
