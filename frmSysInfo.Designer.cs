namespace CNSysInfo
{
    partial class frmSysInfo
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSysInfo));
            this.txtSerialNumber = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtSystemModel = new System.Windows.Forms.TextBox();
            this.txtManufacturer = new System.Windows.Forms.TextBox();
            this.txtMemory = new System.Windows.Forms.TextBox();
            this.txtStorage = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtBatteryHealth = new System.Windows.Forms.TextBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.txtCPU = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.txtAssetTag = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.gbHardware = new System.Windows.Forms.GroupBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.pbBattery = new CNSysInfo.ModernProgressBar();
            this.pbStorage = new CNSysInfo.ModernProgressBar();
            this.pbMemory = new CNSysInfo.ModernProgressBar();
            this.lblSpecRatingValue = new System.Windows.Forms.Label();
            this.txtScreenInfo = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtGPU = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtBiosVersion = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtUptime = new System.Windows.Forms.TextBox();
            this.gbSoftware = new System.Windows.Forms.GroupBox();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.txtPCName = new System.Windows.Forms.TextBox();
            this.txtWinActivationStatus = new System.Windows.Forms.TextBox();
            this.txtTimeZone = new System.Windows.Forms.TextBox();
            this.txtOS = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.functionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.controlPanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.computerManagementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.diskManagementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deviceManagerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.servicesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.taskManagerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.networkConnectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.programsAndFeaturesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.diskCleanupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.forceRestartOutlookToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.forceRestartExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.forceRebootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lblversion = new System.Windows.Forms.Label();
            this.gbHardware.SuspendLayout();
            this.gbSoftware.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtSerialNumber
            // 
            this.txtSerialNumber.BackColor = System.Drawing.Color.White;
            this.txtSerialNumber.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSerialNumber.Location = new System.Drawing.Point(125, 85);
            this.txtSerialNumber.Margin = new System.Windows.Forms.Padding(2);
            this.txtSerialNumber.Name = "txtSerialNumber";
            this.txtSerialNumber.ReadOnly = true;
            this.txtSerialNumber.Size = new System.Drawing.Size(606, 27);
            this.txtSerialNumber.TabIndex = 1;
            this.txtSerialNumber.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 87);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Serial Number:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 53);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(113, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Model Number:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 21);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 20);
            this.label3.TabIndex = 3;
            this.label3.Text = "Manufacturer:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 294);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 20);
            this.label4.TabIndex = 4;
            this.label4.Text = "Memory:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 396);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 20);
            this.label5.TabIndex = 5;
            this.label5.Text = "Storage:";
            // 
            // txtSystemModel
            // 
            this.txtSystemModel.BackColor = System.Drawing.Color.White;
            this.txtSystemModel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSystemModel.Location = new System.Drawing.Point(125, 53);
            this.txtSystemModel.Margin = new System.Windows.Forms.Padding(2);
            this.txtSystemModel.Name = "txtSystemModel";
            this.txtSystemModel.ReadOnly = true;
            this.txtSystemModel.Size = new System.Drawing.Size(606, 27);
            this.txtSystemModel.TabIndex = 6;
            this.txtSystemModel.TabStop = false;
            // 
            // txtManufacturer
            // 
            this.txtManufacturer.BackColor = System.Drawing.Color.White;
            this.txtManufacturer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtManufacturer.Location = new System.Drawing.Point(125, 21);
            this.txtManufacturer.Margin = new System.Windows.Forms.Padding(2);
            this.txtManufacturer.Name = "txtManufacturer";
            this.txtManufacturer.ReadOnly = true;
            this.txtManufacturer.Size = new System.Drawing.Size(606, 27);
            this.txtManufacturer.TabIndex = 7;
            this.txtManufacturer.TabStop = false;
            // 
            // txtMemory
            // 
            this.txtMemory.BackColor = System.Drawing.Color.White;
            this.txtMemory.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtMemory.Location = new System.Drawing.Point(125, 294);
            this.txtMemory.Margin = new System.Windows.Forms.Padding(2);
            this.txtMemory.Name = "txtMemory";
            this.txtMemory.ReadOnly = true;
            this.txtMemory.Size = new System.Drawing.Size(606, 27);
            this.txtMemory.TabIndex = 8;
            this.txtMemory.TabStop = false;
            // 
            // txtStorage
            // 
            this.txtStorage.BackColor = System.Drawing.Color.White;
            this.txtStorage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtStorage.Location = new System.Drawing.Point(125, 398);
            this.txtStorage.Margin = new System.Windows.Forms.Padding(2);
            this.txtStorage.Multiline = true;
            this.txtStorage.Name = "txtStorage";
            this.txtStorage.ReadOnly = true;
            this.txtStorage.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtStorage.Size = new System.Drawing.Size(606, 50);
            this.txtStorage.TabIndex = 9;
            this.txtStorage.TabStop = false;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(9, 346);
            this.label12.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(59, 20);
            this.label12.TabIndex = 25;
            this.label12.Text = "Battery:";
            // 
            // txtBatteryHealth
            // 
            this.txtBatteryHealth.BackColor = System.Drawing.Color.White;
            this.txtBatteryHealth.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBatteryHealth.Location = new System.Drawing.Point(125, 346);
            this.txtBatteryHealth.Margin = new System.Windows.Forms.Padding(2);
            this.txtBatteryHealth.Name = "txtBatteryHealth";
            this.txtBatteryHealth.ReadOnly = true;
            this.txtBatteryHealth.Size = new System.Drawing.Size(606, 27);
            this.txtBatteryHealth.TabIndex = 26;
            this.txtBatteryHealth.TabStop = false;
            // 
            // btnExport
            // 
            this.btnExport.BackColor = System.Drawing.Color.White;
            this.btnExport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExport.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(136)))));
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Location = new System.Drawing.Point(585, 738);
            this.btnExport.Margin = new System.Windows.Forms.Padding(2);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(165, 40);
            this.btnExport.TabIndex = 11;
            this.btnExport.Text = "Export Data";
            this.btnExport.UseVisualStyleBackColor = false;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label7.Location = new System.Drawing.Point(8, 754);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(306, 25);
            this.label7.TabIndex = 13;
            this.label7.Text = "Email: eladwy.ahmed@gmail.com";
            // 
            // txtCPU
            // 
            this.txtCPU.BackColor = System.Drawing.Color.White;
            this.txtCPU.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCPU.Location = new System.Drawing.Point(125, 181);
            this.txtCPU.Margin = new System.Windows.Forms.Padding(2);
            this.txtCPU.Name = "txtCPU";
            this.txtCPU.ReadOnly = true;
            this.txtCPU.Size = new System.Drawing.Size(606, 27);
            this.txtCPU.TabIndex = 15;
            this.txtCPU.TabStop = false;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 181);
            this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(39, 20);
            this.label8.TabIndex = 14;
            this.label8.Text = "CPU:";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.panel1.Location = new System.Drawing.Point(-1, -6);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(812, 12);
            this.panel1.TabIndex = 17;
            // 
            // txtAssetTag
            // 
            this.txtAssetTag.BackColor = System.Drawing.Color.White;
            this.txtAssetTag.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAssetTag.Location = new System.Drawing.Point(125, 117);
            this.txtAssetTag.Margin = new System.Windows.Forms.Padding(2);
            this.txtAssetTag.Name = "txtAssetTag";
            this.txtAssetTag.ReadOnly = true;
            this.txtAssetTag.Size = new System.Drawing.Size(606, 27);
            this.txtAssetTag.TabIndex = 20;
            this.txtAssetTag.TabStop = false;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(9, 119);
            this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(74, 20);
            this.label10.TabIndex = 19;
            this.label10.Text = "Asset Tag:";
            // 
            // gbHardware
            // 
            this.gbHardware.Controls.Add(this.label16);
            this.gbHardware.Controls.Add(this.label15);
            this.gbHardware.Controls.Add(this.label14);
            this.gbHardware.Controls.Add(this.pbBattery);
            this.gbHardware.Controls.Add(this.pbStorage);
            this.gbHardware.Controls.Add(this.pbMemory);
            this.gbHardware.Controls.Add(this.lblSpecRatingValue);
            this.gbHardware.Controls.Add(this.txtScreenInfo);
            this.gbHardware.Controls.Add(this.label13);
            this.gbHardware.Controls.Add(this.txtGPU);
            this.gbHardware.Controls.Add(this.label9);
            this.gbHardware.Controls.Add(this.txtBiosVersion);
            this.gbHardware.Controls.Add(this.label11);
            this.gbHardware.Controls.Add(this.label3);
            this.gbHardware.Controls.Add(this.txtSerialNumber);
            this.gbHardware.Controls.Add(this.txtAssetTag);
            this.gbHardware.Controls.Add(this.label1);
            this.gbHardware.Controls.Add(this.label10);
            this.gbHardware.Controls.Add(this.label2);
            this.gbHardware.Controls.Add(this.label4);
            this.gbHardware.Controls.Add(this.label6);
            this.gbHardware.Controls.Add(this.label5);
            this.gbHardware.Controls.Add(this.txtSystemModel);
            this.gbHardware.Controls.Add(this.txtManufacturer);
            this.gbHardware.Controls.Add(this.txtCPU);
            this.gbHardware.Controls.Add(this.txtMemory);
            this.gbHardware.Controls.Add(this.label8);
            this.gbHardware.Controls.Add(this.label12);
            this.gbHardware.Controls.Add(this.txtBatteryHealth);
            this.gbHardware.Controls.Add(this.txtStorage);
            this.gbHardware.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gbHardware.Location = new System.Drawing.Point(12, 204);
            this.gbHardware.Margin = new System.Windows.Forms.Padding(2);
            this.gbHardware.Name = "gbHardware";
            this.gbHardware.Padding = new System.Windows.Forms.Padding(2);
            this.gbHardware.Size = new System.Drawing.Size(741, 526);
            this.gbHardware.TabIndex = 30;
            this.gbHardware.TabStop = false;
            this.gbHardware.Text = "Hardware";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(9, 448);
            this.label16.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(101, 20);
            this.label16.TabIndex = 32;
            this.label16.Text = "Used Storage:";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(9, 373);
            this.label15.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(107, 20);
            this.label15.TabIndex = 31;
            this.label15.Text = "Battery Health:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(9, 321);
            this.label14.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(104, 20);
            this.label14.TabIndex = 30;
            this.label14.Text = "Used memory:";
            // 
            // pbBattery
            // 
            this.pbBattery.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.pbBattery.ForeColor = System.Drawing.Color.Black;
            this.pbBattery.Location = new System.Drawing.Point(125, 378);
            this.pbBattery.Margin = new System.Windows.Forms.Padding(4);
            this.pbBattery.Maximum = 100;
            this.pbBattery.Name = "pbBattery";
            this.pbBattery.ProgressBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.pbBattery.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.pbBattery.ShowPercentage = true;
            this.pbBattery.Size = new System.Drawing.Size(606, 15);
            this.pbBattery.TabIndex = 27;
            this.pbBattery.Value = 0;
            // 
            // pbStorage
            // 
            this.pbStorage.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.pbStorage.ForeColor = System.Drawing.Color.Black;
            this.pbStorage.Location = new System.Drawing.Point(125, 453);
            this.pbStorage.Margin = new System.Windows.Forms.Padding(4);
            this.pbStorage.Maximum = 100;
            this.pbStorage.Name = "pbStorage";
            this.pbStorage.ProgressBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.pbStorage.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.pbStorage.ShowPercentage = true;
            this.pbStorage.Size = new System.Drawing.Size(606, 15);
            this.pbStorage.TabIndex = 28;
            this.pbStorage.Value = 0;
            // 
            // pbMemory
            // 
            this.pbMemory.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.pbMemory.ForeColor = System.Drawing.Color.Black;
            this.pbMemory.Location = new System.Drawing.Point(125, 326);
            this.pbMemory.Margin = new System.Windows.Forms.Padding(4);
            this.pbMemory.Maximum = 100;
            this.pbMemory.Name = "pbMemory";
            this.pbMemory.ProgressBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.pbMemory.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.pbMemory.ShowPercentage = true;
            this.pbMemory.Size = new System.Drawing.Size(606, 15);
            this.pbMemory.TabIndex = 29;
            this.pbMemory.Value = 0;
            // 
            // lblSpecRatingValue
            // 
            this.lblSpecRatingValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.lblSpecRatingValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSpecRatingValue.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSpecRatingValue.ForeColor = System.Drawing.Color.Black;
            this.lblSpecRatingValue.Location = new System.Drawing.Point(128, 473);
            this.lblSpecRatingValue.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSpecRatingValue.Name = "lblSpecRatingValue";
            this.lblSpecRatingValue.Size = new System.Drawing.Size(606, 42);
            this.lblSpecRatingValue.TabIndex = 0;
            this.lblSpecRatingValue.Text = "Evaluating...";
            this.lblSpecRatingValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtScreenInfo
            // 
            this.txtScreenInfo.BackColor = System.Drawing.Color.White;
            this.txtScreenInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtScreenInfo.Location = new System.Drawing.Point(125, 262);
            this.txtScreenInfo.Margin = new System.Windows.Forms.Padding(2);
            this.txtScreenInfo.Name = "txtScreenInfo";
            this.txtScreenInfo.ReadOnly = true;
            this.txtScreenInfo.Size = new System.Drawing.Size(606, 27);
            this.txtScreenInfo.TabIndex = 26;
            this.txtScreenInfo.TabStop = false;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(9, 262);
            this.label13.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(56, 20);
            this.label13.TabIndex = 25;
            this.label13.Text = "Screen:";
            // 
            // txtGPU
            // 
            this.txtGPU.BackColor = System.Drawing.Color.White;
            this.txtGPU.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtGPU.Location = new System.Drawing.Point(125, 213);
            this.txtGPU.Margin = new System.Windows.Forms.Padding(2);
            this.txtGPU.Multiline = true;
            this.txtGPU.Name = "txtGPU";
            this.txtGPU.ReadOnly = true;
            this.txtGPU.Size = new System.Drawing.Size(606, 44);
            this.txtGPU.TabIndex = 24;
            this.txtGPU.TabStop = false;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 213);
            this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(104, 20);
            this.label9.TabIndex = 23;
            this.label9.Text = "Graphics Card:";
            this.label9.Click += new System.EventHandler(this.label9_Click);
            // 
            // txtBiosVersion
            // 
            this.txtBiosVersion.BackColor = System.Drawing.Color.White;
            this.txtBiosVersion.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBiosVersion.Location = new System.Drawing.Point(125, 149);
            this.txtBiosVersion.Margin = new System.Windows.Forms.Padding(2);
            this.txtBiosVersion.Name = "txtBiosVersion";
            this.txtBiosVersion.ReadOnly = true;
            this.txtBiosVersion.Size = new System.Drawing.Size(606, 27);
            this.txtBiosVersion.TabIndex = 22;
            this.txtBiosVersion.TabStop = false;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(9, 149);
            this.label11.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(44, 20);
            this.label11.TabIndex = 21;
            this.label11.Text = "BIOS:";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(9, 473);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(107, 42);
            this.label6.TabIndex = 5;
            this.label6.Text = "ELADWYSOFT Specs Score:";
            // 
            // txtUptime
            // 
            this.txtUptime.BackColor = System.Drawing.Color.White;
            this.txtUptime.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUptime.Location = new System.Drawing.Point(6, 88);
            this.txtUptime.Margin = new System.Windows.Forms.Padding(2);
            this.txtUptime.Name = "txtUptime";
            this.txtUptime.ReadOnly = true;
            this.txtUptime.Size = new System.Drawing.Size(725, 27);
            this.txtUptime.TabIndex = 28;
            this.txtUptime.TabStop = false;
            this.txtUptime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // gbSoftware
            // 
            this.gbSoftware.Controls.Add(this.txtUserName);
            this.gbSoftware.Controls.Add(this.txtPCName);
            this.gbSoftware.Controls.Add(this.txtUptime);
            this.gbSoftware.Controls.Add(this.txtWinActivationStatus);
            this.gbSoftware.Controls.Add(this.txtTimeZone);
            this.gbSoftware.Controls.Add(this.txtOS);
            this.gbSoftware.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gbSoftware.Location = new System.Drawing.Point(12, 40);
            this.gbSoftware.Margin = new System.Windows.Forms.Padding(2);
            this.gbSoftware.Name = "gbSoftware";
            this.gbSoftware.Padding = new System.Windows.Forms.Padding(2);
            this.gbSoftware.Size = new System.Drawing.Size(741, 161);
            this.gbSoftware.TabIndex = 31;
            this.gbSoftware.TabStop = false;
            this.gbSoftware.Text = "Software";
            // 
            // txtUserName
            // 
            this.txtUserName.BackColor = System.Drawing.Color.White;
            this.txtUserName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUserName.Location = new System.Drawing.Point(419, 120);
            this.txtUserName.Margin = new System.Windows.Forms.Padding(2);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.ReadOnly = true;
            this.txtUserName.Size = new System.Drawing.Size(312, 27);
            this.txtUserName.TabIndex = 22;
            this.txtUserName.TabStop = false;
            this.txtUserName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtPCName
            // 
            this.txtPCName.BackColor = System.Drawing.Color.White;
            this.txtPCName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPCName.Location = new System.Drawing.Point(6, 120);
            this.txtPCName.Margin = new System.Windows.Forms.Padding(2);
            this.txtPCName.Name = "txtPCName";
            this.txtPCName.ReadOnly = true;
            this.txtPCName.Size = new System.Drawing.Size(407, 27);
            this.txtPCName.TabIndex = 21;
            this.txtPCName.TabStop = false;
            this.txtPCName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtWinActivationStatus
            // 
            this.txtWinActivationStatus.BackColor = System.Drawing.Color.White;
            this.txtWinActivationStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtWinActivationStatus.Location = new System.Drawing.Point(627, 24);
            this.txtWinActivationStatus.Margin = new System.Windows.Forms.Padding(2);
            this.txtWinActivationStatus.Name = "txtWinActivationStatus";
            this.txtWinActivationStatus.ReadOnly = true;
            this.txtWinActivationStatus.Size = new System.Drawing.Size(104, 27);
            this.txtWinActivationStatus.TabIndex = 20;
            this.txtWinActivationStatus.TabStop = false;
            this.txtWinActivationStatus.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtTimeZone
            // 
            this.txtTimeZone.BackColor = System.Drawing.Color.White;
            this.txtTimeZone.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTimeZone.Location = new System.Drawing.Point(6, 56);
            this.txtTimeZone.Margin = new System.Windows.Forms.Padding(2);
            this.txtTimeZone.Name = "txtTimeZone";
            this.txtTimeZone.ReadOnly = true;
            this.txtTimeZone.Size = new System.Drawing.Size(725, 27);
            this.txtTimeZone.TabIndex = 18;
            this.txtTimeZone.TabStop = false;
            this.txtTimeZone.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtOS
            // 
            this.txtOS.BackColor = System.Drawing.Color.White;
            this.txtOS.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOS.Location = new System.Drawing.Point(6, 24);
            this.txtOS.Margin = new System.Windows.Forms.Padding(2);
            this.txtOS.Name = "txtOS";
            this.txtOS.Size = new System.Drawing.Size(617, 27);
            this.txtOS.TabIndex = 9;
            this.txtOS.TabStop = false;
            this.txtOS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.White;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Location = new System.Drawing.Point(4, 91);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(612, 17);
            this.textBox1.TabIndex = 19;
            this.textBox1.TabStop = false;
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBox1.Visible = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.functionsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menuStrip1.Size = new System.Drawing.Size(765, 30);
            this.menuStrip1.TabIndex = 32;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // functionsToolStripMenuItem
            // 
            this.functionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.controlPanelToolStripMenuItem,
            this.computerManagementToolStripMenuItem,
            this.diskManagementToolStripMenuItem,
            this.deviceManagerToolStripMenuItem,
            this.servicesToolStripMenuItem,
            this.taskManagerToolStripMenuItem,
            this.networkConnectionsToolStripMenuItem,
            this.programsAndFeaturesToolStripMenuItem,
            this.diskCleanupToolStripMenuItem,
            this.forceRestartOutlookToolStripMenuItem,
            this.forceRestartExplorerToolStripMenuItem,
            this.forceRebootToolStripMenuItem,
            this.copyToClipboardToolStripMenuItem});
            this.functionsToolStripMenuItem.Name = "functionsToolStripMenuItem";
            this.functionsToolStripMenuItem.Size = new System.Drawing.Size(126, 26);
            this.functionsToolStripMenuItem.Text = "Quick Functions";
            // 
            // controlPanelToolStripMenuItem
            // 
            this.controlPanelToolStripMenuItem.Name = "controlPanelToolStripMenuItem";
            this.controlPanelToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.controlPanelToolStripMenuItem.Text = "Control Panel";
            this.controlPanelToolStripMenuItem.Click += new System.EventHandler(this.controlPanelToolStripMenuItem_Click);
            // 
            // computerManagementToolStripMenuItem
            // 
            this.computerManagementToolStripMenuItem.Name = "computerManagementToolStripMenuItem";
            this.computerManagementToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.computerManagementToolStripMenuItem.Text = "Computer Management";
            this.computerManagementToolStripMenuItem.Click += new System.EventHandler(this.computerManagementToolStripMenuItem_Click);
            // 
            // diskManagementToolStripMenuItem
            // 
            this.diskManagementToolStripMenuItem.Name = "diskManagementToolStripMenuItem";
            this.diskManagementToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.diskManagementToolStripMenuItem.Text = "Disk Management";
            this.diskManagementToolStripMenuItem.Click += new System.EventHandler(this.diskManagementToolStripMenuItem_Click);
            // 
            // deviceManagerToolStripMenuItem
            // 
            this.deviceManagerToolStripMenuItem.Name = "deviceManagerToolStripMenuItem";
            this.deviceManagerToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.deviceManagerToolStripMenuItem.Text = "Device Manager";
            this.deviceManagerToolStripMenuItem.Click += new System.EventHandler(this.deviceManagerToolStripMenuItem_Click);
            // 
            // servicesToolStripMenuItem
            // 
            this.servicesToolStripMenuItem.Name = "servicesToolStripMenuItem";
            this.servicesToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.servicesToolStripMenuItem.Text = "Services";
            this.servicesToolStripMenuItem.Click += new System.EventHandler(this.servicesToolStripMenuItem_Click);
            // 
            // taskManagerToolStripMenuItem
            // 
            this.taskManagerToolStripMenuItem.Name = "taskManagerToolStripMenuItem";
            this.taskManagerToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.taskManagerToolStripMenuItem.Text = "Task Manager";
            this.taskManagerToolStripMenuItem.Click += new System.EventHandler(this.taskManagerToolStripMenuItem_Click);
            // 
            // networkConnectionsToolStripMenuItem
            // 
            this.networkConnectionsToolStripMenuItem.Name = "networkConnectionsToolStripMenuItem";
            this.networkConnectionsToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.networkConnectionsToolStripMenuItem.Text = "Network Connections";
            this.networkConnectionsToolStripMenuItem.Click += new System.EventHandler(this.networkConnectionsToolStripMenuItem_Click);
            // 
            // programsAndFeaturesToolStripMenuItem
            // 
            this.programsAndFeaturesToolStripMenuItem.Name = "programsAndFeaturesToolStripMenuItem";
            this.programsAndFeaturesToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.programsAndFeaturesToolStripMenuItem.Text = "Programs and Features";
            this.programsAndFeaturesToolStripMenuItem.Click += new System.EventHandler(this.programsAndFeaturesToolStripMenuItem_Click);
            // 
            // diskCleanupToolStripMenuItem
            // 
            this.diskCleanupToolStripMenuItem.Name = "diskCleanupToolStripMenuItem";
            this.diskCleanupToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.diskCleanupToolStripMenuItem.Text = "Disk Cleanup";
            this.diskCleanupToolStripMenuItem.Click += new System.EventHandler(this.diskCleanupToolStripMenuItem_Click);
            // 
            // forceRestartOutlookToolStripMenuItem
            // 
            this.forceRestartOutlookToolStripMenuItem.Name = "forceRestartOutlookToolStripMenuItem";
            this.forceRestartOutlookToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.forceRestartOutlookToolStripMenuItem.Text = "Force Restart Outlook";
            this.forceRestartOutlookToolStripMenuItem.Click += new System.EventHandler(this.forceRestartOutlookToolStripMenuItem_Click);
            // 
            // forceRestartExplorerToolStripMenuItem
            // 
            this.forceRestartExplorerToolStripMenuItem.Name = "forceRestartExplorerToolStripMenuItem";
            this.forceRestartExplorerToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.forceRestartExplorerToolStripMenuItem.Text = "Force Restart Explorer";
            this.forceRestartExplorerToolStripMenuItem.Click += new System.EventHandler(this.forceRestartExplorerToolStripMenuItem_Click);
            // 
            // forceRebootToolStripMenuItem
            // 
            this.forceRebootToolStripMenuItem.Name = "forceRebootToolStripMenuItem";
            this.forceRebootToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.forceRebootToolStripMenuItem.Text = "Force Reboot";
            this.forceRebootToolStripMenuItem.Click += new System.EventHandler(this.forceRebootToolStripMenuItem_Click);
            // 
            // copyToClipboardToolStripMenuItem
            // 
            this.copyToClipboardToolStripMenuItem.Name = "copyToClipboardToolStripMenuItem";
            this.copyToClipboardToolStripMenuItem.Size = new System.Drawing.Size(250, 26);
            this.copyToClipboardToolStripMenuItem.Text = "Copy to Clipboard";
            this.copyToClipboardToolStripMenuItem.Click += new System.EventHandler(this.copyToClipboardToolStripMenuItem_Click);
            // 
            // lblversion
            // 
            this.lblversion.AutoSize = true;
            this.lblversion.Location = new System.Drawing.Point(22, 732);
            this.lblversion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblversion.Name = "lblversion";
            this.lblversion.Size = new System.Drawing.Size(86, 20);
            this.lblversion.TabIndex = 33;
            this.lblversion.Text = "SysInfo v1.3";
            // 
            // frmSysInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(765, 786);
            this.Controls.Add(this.lblversion);
            this.Controls.Add(this.gbSoftware);
            this.Controls.Add(this.gbHardware);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.menuStrip1);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(746, 613);
            this.Name = "frmSysInfo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SysInfo";
            this.Load += new System.EventHandler(this.frmSysInfo_Load);
            this.gbHardware.ResumeLayout(false);
            this.gbHardware.PerformLayout();
            this.gbSoftware.ResumeLayout(false);
            this.gbSoftware.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtSerialNumber;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtSystemModel;
        private System.Windows.Forms.TextBox txtManufacturer;
        private System.Windows.Forms.TextBox txtMemory;
        private System.Windows.Forms.TextBox txtStorage;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtBatteryHealth;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtCPU;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox txtAssetTag;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.GroupBox gbHardware;
        private System.Windows.Forms.GroupBox gbSoftware;
        private System.Windows.Forms.TextBox txtOS;
        private System.Windows.Forms.TextBox txtTimeZone;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem functionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem forceRestartOutlookToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem forceRestartExplorerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem forceRebootToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem diskManagementToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deviceManagerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem networkConnectionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem controlPanelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem programsAndFeaturesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem computerManagementToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem diskCleanupToolStripMenuItem;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox txtWinActivationStatus;
        private System.Windows.Forms.TextBox txtBiosVersion;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ToolStripMenuItem taskManagerToolStripMenuItem;
        private System.Windows.Forms.TextBox txtPCName;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.ToolStripMenuItem servicesToolStripMenuItem;
        private System.Windows.Forms.TextBox txtGPU;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ToolStripMenuItem copyToClipboardToolStripMenuItem;
        private System.Windows.Forms.TextBox txtScreenInfo;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtUptime;
        private System.Windows.Forms.Label lblversion;
        private System.Windows.Forms.Label lblSpecRatingValue;
        private System.Windows.Forms.Label label6;
        private ModernProgressBar pbMemory;
        private ModernProgressBar pbStorage;
        private ModernProgressBar pbBattery;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label16;
    }
}
