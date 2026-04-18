namespace AudioBookManager
{
    partial class AudioBookManager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new System.Windows.Forms.Label();
            textBox1 = new System.Windows.Forms.TextBox();
            textBox2 = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            btnLoadFolder = new System.Windows.Forms.Button();
            btnLoadFile = new System.Windows.Forms.Button();
            textBox3 = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            textBox4 = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            colBookNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colBookTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colBitrate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            BookScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Artist = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Album = new System.Windows.Forms.DataGridViewTextBoxColumn();
            textBox5 = new System.Windows.Forms.TextBox();
            btnRenameFile = new System.Windows.Forms.Button();
            folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            btnExistingFolder = new System.Windows.Forms.Button();
            btnReset = new System.Windows.Forms.Button();
            btnBrowseExistingFolder = new System.Windows.Forms.Button();
            btnGoodreads = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(14, 12);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(72, 15);
            label1.TabIndex = 0;
            label1.Text = "Default Path";
            // 
            // textBox1
            // 
            textBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBox1.Location = new System.Drawing.Point(118, 8);
            textBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(1293, 23);
            textBox1.TabIndex = 1;
            // 
            // textBox2
            // 
            textBox2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBox2.Location = new System.Drawing.Point(118, 38);
            textBox2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBox2.Name = "textBox2";
            textBox2.Size = new System.Drawing.Size(1293, 23);
            textBox2.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(14, 42);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(92, 15);
            label2.TabIndex = 2;
            label2.Text = "Downloadt Path";
            // 
            // btnLoadFolder
            // 
            btnLoadFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnLoadFolder.Location = new System.Drawing.Point(14, 69);
            btnLoadFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnLoadFolder.Name = "btnLoadFolder";
            btnLoadFolder.Size = new System.Drawing.Size(511, 27);
            btnLoadFolder.TabIndex = 4;
            btnLoadFolder.Text = "Carregar Pasta";
            btnLoadFolder.UseVisualStyleBackColor = true;
            btnLoadFolder.Click += Button1_Click;
            // 
            // btnLoadFile
            // 
            btnLoadFile.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnLoadFile.Location = new System.Drawing.Point(532, 69);
            btnLoadFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnLoadFile.Name = "btnLoadFile";
            btnLoadFile.Size = new System.Drawing.Size(562, 27);
            btnLoadFile.TabIndex = 5;
            btnLoadFile.Text = "Carregar Arquivo";
            btnLoadFile.UseVisualStyleBackColor = true;
            btnLoadFile.Click += Button2_Click;
            // 
            // textBox3
            // 
            textBox3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBox3.Location = new System.Drawing.Point(118, 104);
            textBox3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBox3.Name = "textBox3";
            textBox3.Size = new System.Drawing.Size(1293, 23);
            textBox3.TabIndex = 7;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(14, 107);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(41, 15);
            label3.TabIndex = 6;
            label3.Text = "Artista";
            // 
            // textBox4
            // 
            textBox4.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBox4.Location = new System.Drawing.Point(118, 134);
            textBox4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBox4.Name = "textBox4";
            textBox4.Size = new System.Drawing.Size(1293, 23);
            textBox4.TabIndex = 9;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(14, 137);
            label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(43, 15);
            label4.TabIndex = 8;
            label4.Text = "Album";
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colBookNumber, colBookTitle, colBitrate, BookScore, Artist, Album });
            dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            dataGridView1.Location = new System.Drawing.Point(14, 193);
            dataGridView1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new System.Drawing.Size(1398, 271);
            dataGridView1.TabIndex = 10;
            // 
            // colBookNumber
            // 
            colBookNumber.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            colBookNumber.DataPropertyName = "BookNumber";
            colBookNumber.HeaderText = "BookNumber";
            colBookNumber.Name = "colBookNumber";
            colBookNumber.Width = 103;
            // 
            // colBookTitle
            // 
            colBookTitle.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            colBookTitle.DataPropertyName = "BookTitle";
            colBookTitle.HeaderText = "BookTitle";
            colBookTitle.Name = "colBookTitle";
            // 
            // colBitrate
            // 
            colBitrate.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            colBitrate.DataPropertyName = "Bitrate";
            colBitrate.HeaderText = "Bitrate";
            colBitrate.Name = "colBitrate";
            colBitrate.ReadOnly = true;
            colBitrate.Width = 66;
            // 
            // BookScore
            // 
            BookScore.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            BookScore.DataPropertyName = "BookScore";
            BookScore.HeaderText = "BookScore";
            BookScore.Name = "BookScore";
            BookScore.ReadOnly = true;
            BookScore.Width = 88;
            // 
            // Artist
            // 
            Artist.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            Artist.DataPropertyName = "Artist";
            Artist.HeaderText = "Artist";
            Artist.Name = "Artist";
            Artist.Width = 60;
            // 
            // Album
            // 
            Album.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            Album.DataPropertyName = "Album";
            Album.HeaderText = "Album";
            Album.Name = "Album";
            Album.Width = 68;
            // 
            // textBox5
            // 
            textBox5.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBox5.Location = new System.Drawing.Point(14, 471);
            textBox5.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBox5.Multiline = true;
            textBox5.Name = "textBox5";
            textBox5.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            textBox5.Size = new System.Drawing.Size(1397, 147);
            textBox5.TabIndex = 11;
            // 
            // btnRenameFile
            // 
            btnRenameFile.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnRenameFile.Location = new System.Drawing.Point(14, 625);
            btnRenameFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnRenameFile.Name = "btnRenameFile";
            btnRenameFile.Size = new System.Drawing.Size(1398, 27);
            btnRenameFile.TabIndex = 12;
            btnRenameFile.Text = "Renomear";
            btnRenameFile.UseVisualStyleBackColor = true;
            btnRenameFile.Click += Button3_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.Multiselect = true;
            // 
            // btnExistingFolder
            // 
            btnExistingFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnExistingFolder.Location = new System.Drawing.Point(14, 163);
            btnExistingFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnExistingFolder.Name = "btnExistingFolder";
            btnExistingFolder.Size = new System.Drawing.Size(951, 27);
            btnExistingFolder.TabIndex = 13;
            btnExistingFolder.Text = "Carregar Pasta Existente";
            btnExistingFolder.UseVisualStyleBackColor = true;
            btnExistingFolder.Click += Button4_Click;
            // 
            // btnReset
            // 
            btnReset.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnReset.Location = new System.Drawing.Point(1101, 69);
            btnReset.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnReset.Name = "btnReset";
            btnReset.Size = new System.Drawing.Size(310, 27);
            btnReset.TabIndex = 14;
            btnReset.Text = "Reset";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += Button5_Click;
            // 
            // btnBrowseExistingFolder
            // 
            btnBrowseExistingFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnBrowseExistingFolder.Location = new System.Drawing.Point(1175, 163);
            btnBrowseExistingFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnBrowseExistingFolder.Name = "btnBrowseExistingFolder";
            btnBrowseExistingFolder.Size = new System.Drawing.Size(237, 27);
            btnBrowseExistingFolder.TabIndex = 15;
            btnBrowseExistingFolder.Text = "Browse";
            btnBrowseExistingFolder.UseVisualStyleBackColor = true;
            btnBrowseExistingFolder.Click += Button6_Click;
            // 
            // btnGoodreads
            // 
            btnGoodreads.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnGoodreads.Location = new System.Drawing.Point(972, 163);
            btnGoodreads.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnGoodreads.Name = "btnGoodreads";
            btnGoodreads.Size = new System.Drawing.Size(196, 27);
            btnGoodreads.TabIndex = 16;
            btnGoodreads.Text = "GoodReads";
            btnGoodreads.UseVisualStyleBackColor = true;
            btnGoodreads.Click += Button7_Click;
            // 
            // AudioBookManager
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1426, 653);
            Controls.Add(btnGoodreads);
            Controls.Add(btnBrowseExistingFolder);
            Controls.Add(btnReset);
            Controls.Add(btnExistingFolder);
            Controls.Add(btnRenameFile);
            Controls.Add(textBox5);
            Controls.Add(dataGridView1);
            Controls.Add(textBox4);
            Controls.Add(label4);
            Controls.Add(textBox3);
            Controls.Add(label3);
            Controls.Add(btnLoadFile);
            Controls.Add(btnLoadFolder);
            Controls.Add(textBox2);
            Controls.Add(label2);
            Controls.Add(textBox1);
            Controls.Add(label1);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "AudioBookManager";
            Text = "AudioBook Manager";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnLoadFolder;
        private System.Windows.Forms.Button btnLoadFile;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Button btnRenameFile;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnExistingFolder;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnBrowseExistingFolder;
        private System.Windows.Forms.Button btnGoodreads;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBookNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBookTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBitrate;
        private System.Windows.Forms.DataGridViewTextBoxColumn BookScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn Artist;
        private System.Windows.Forms.DataGridViewTextBoxColumn Album;
    }
}

