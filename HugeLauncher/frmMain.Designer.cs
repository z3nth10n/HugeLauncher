namespace HugeLauncher
{
    partial class frmMain
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.btnDelClient = new System.Windows.Forms.Button();
            this.btnRuninsClient = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.btnDelServer = new System.Windows.Forms.Button();
            this.btnRunisServer = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.pbFileDownload = new System.Windows.Forms.ProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.pbTotalDownload = new System.Windows.Forms.ProgressBar();
            this.label4 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.opcionesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.establecerRutaDeInstalaciónToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.mostrarTutorialInicialToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.btnDelClient);
            this.tabPage1.Controls.Add(this.btnRuninsClient);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // btnDelClient
            // 
            resources.ApplyResources(this.btnDelClient, "btnDelClient");
            this.btnDelClient.Name = "btnDelClient";
            this.btnDelClient.UseVisualStyleBackColor = true;
            this.btnDelClient.Click += new System.EventHandler(this.btnDelClient_Click);
            // 
            // btnRuninsClient
            // 
            resources.ApplyResources(this.btnRuninsClient, "btnRuninsClient");
            this.btnRuninsClient.Name = "btnRuninsClient";
            this.btnRuninsClient.UseVisualStyleBackColor = true;
            this.btnRuninsClient.Click += new System.EventHandler(this.btnRuninsClient_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.btnDelServer);
            this.tabPage2.Controls.Add(this.btnRunisServer);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // btnDelServer
            // 
            resources.ApplyResources(this.btnDelServer, "btnDelServer");
            this.btnDelServer.Name = "btnDelServer";
            this.btnDelServer.UseVisualStyleBackColor = true;
            this.btnDelServer.Click += new System.EventHandler(this.btnDelServer_Click);
            // 
            // btnRunisServer
            // 
            resources.ApplyResources(this.btnRunisServer, "btnRunisServer");
            this.btnRunisServer.Name = "btnRunisServer";
            this.btnRunisServer.UseVisualStyleBackColor = true;
            this.btnRunisServer.Click += new System.EventHandler(this.btnRunisServer_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            resources.ApplyResources(this.comboBox1, "comboBox1");
            this.comboBox1.Name = "comboBox1";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // pbFileDownload
            // 
            resources.ApplyResources(this.pbFileDownload, "pbFileDownload");
            this.pbFileDownload.Name = "pbFileDownload";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // pbTotalDownload
            // 
            resources.ApplyResources(this.pbTotalDownload, "pbTotalDownload");
            this.pbTotalDownload.Name = "pbTotalDownload";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.opcionesToolStripMenuItem});
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // opcionesToolStripMenuItem
            // 
            this.opcionesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.establecerRutaDeInstalaciónToolStripMenuItem,
            this.mostrarTutorialInicialToolStripMenuItem});
            this.opcionesToolStripMenuItem.Name = "opcionesToolStripMenuItem";
            resources.ApplyResources(this.opcionesToolStripMenuItem, "opcionesToolStripMenuItem");
            // 
            // establecerRutaDeInstalaciónToolStripMenuItem
            // 
            this.establecerRutaDeInstalaciónToolStripMenuItem.Name = "establecerRutaDeInstalaciónToolStripMenuItem";
            resources.ApplyResources(this.establecerRutaDeInstalaciónToolStripMenuItem, "establecerRutaDeInstalaciónToolStripMenuItem");
            this.establecerRutaDeInstalaciónToolStripMenuItem.Click += new System.EventHandler(this.establecerRutaDeInstalaciónToolStripMenuItem_Click);
            // 
            // mostrarTutorialInicialToolStripMenuItem
            // 
            this.mostrarTutorialInicialToolStripMenuItem.Name = "mostrarTutorialInicialToolStripMenuItem";
            resources.ApplyResources(this.mostrarTutorialInicialToolStripMenuItem, "mostrarTutorialInicialToolStripMenuItem");
            this.mostrarTutorialInicialToolStripMenuItem.Click += new System.EventHandler(this.mostrarTutorialInicialToolStripMenuItem_Click);
            // 
            // frmMain
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label4);
            this.Controls.Add(this.pbTotalDownload);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.pbFileDownload);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmMain";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar pbFileDownload;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar pbTotalDownload;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnDelClient;
        private System.Windows.Forms.Button btnRuninsClient;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnDelServer;
        private System.Windows.Forms.Button btnRunisServer;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem opcionesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem establecerRutaDeInstalaciónToolStripMenuItem;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolStripMenuItem mostrarTutorialInicialToolStripMenuItem;
    }
}

