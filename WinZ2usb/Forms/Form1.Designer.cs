namespace WinZ2usb
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.b_update = new System.Windows.Forms.Button();
            this.l_status = new System.Windows.Forms.Label();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuIco = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.закрытьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuIco.SuspendLayout();
            this.SuspendLayout();
            // 
            // b_update
            // 
            this.b_update.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.b_update.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.b_update.Image = ((System.Drawing.Image)(resources.GetObject("b_update.Image")));
            this.b_update.Location = new System.Drawing.Point(0, 0);
            this.b_update.Margin = new System.Windows.Forms.Padding(0);
            this.b_update.Name = "b_update";
            this.b_update.Size = new System.Drawing.Size(25, 25);
            this.b_update.TabIndex = 0;
            this.b_update.UseVisualStyleBackColor = true;
            this.b_update.Click += new System.EventHandler(this.b_update_Click);
            // 
            // l_status
            // 
            this.l_status.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.l_status.AutoSize = true;
            this.l_status.BackColor = System.Drawing.Color.DarkRed;
            this.l_status.ForeColor = System.Drawing.Color.Transparent;
            this.l_status.Location = new System.Drawing.Point(43, 1);
            this.l_status.MinimumSize = new System.Drawing.Size(35, 23);
            this.l_status.Name = "l_status";
            this.l_status.Size = new System.Drawing.Size(35, 23);
            this.l_status.TabIndex = 1;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuIco;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Z2 ридер";
            this.notifyIcon1.Visible = true;
            // 
            // contextMenuIco
            // 
            this.contextMenuIco.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.закрытьToolStripMenuItem});
            this.contextMenuIco.Name = "contextMenuIco";
            this.contextMenuIco.Size = new System.Drawing.Size(121, 26);
            // 
            // закрытьToolStripMenuItem
            // 
            this.закрытьToolStripMenuItem.Name = "закрытьToolStripMenuItem";
            this.закрытьToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.закрытьToolStripMenuItem.Text = "Закрыть";
            this.закрытьToolStripMenuItem.Click += new System.EventHandler(this.закрытьToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(80, 25);
            this.Controls.Add(this.l_status);
            this.Controls.Add(this.b_update);
            this.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(300, 0);
            this.MinimumSize = new System.Drawing.Size(0, 25);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Form1";
            this.TopMost = true;
            this.contextMenuIco.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button b_update;
        private System.Windows.Forms.Label l_status;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuIco;
        private System.Windows.Forms.ToolStripMenuItem закрытьToolStripMenuItem;
    }
}

