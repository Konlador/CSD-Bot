namespace CSD_Bot
{
    partial class Form1
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
            this.HistoryLog = new System.Windows.Forms.TextBox();
            this.PictureBox = new System.Windows.Forms.PictureBox();
            this.StatusBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // HistoryLog
            // 
            this.HistoryLog.Location = new System.Drawing.Point(12, 38);
            this.HistoryLog.Multiline = true;
            this.HistoryLog.Name = "HistoryLog";
            this.HistoryLog.ReadOnly = true;
            this.HistoryLog.Size = new System.Drawing.Size(800, 70);
            this.HistoryLog.TabIndex = 0;
            // 
            // PictureBox
            // 
            this.PictureBox.Location = new System.Drawing.Point(12, 114);
            this.PictureBox.Name = "PictureBox";
            this.PictureBox.Size = new System.Drawing.Size(800, 55);
            this.PictureBox.TabIndex = 1;
            this.PictureBox.TabStop = false;
            // 
            // StatusBox
            // 
            this.StatusBox.Location = new System.Drawing.Point(12, 12);
            this.StatusBox.Name = "StatusBox";
            this.StatusBox.ReadOnly = true;
            this.StatusBox.Size = new System.Drawing.Size(799, 20);
            this.StatusBox.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(823, 184);
            this.Controls.Add(this.StatusBox);
            this.Controls.Add(this.PictureBox);
            this.Controls.Add(this.HistoryLog);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "CSD Bot";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox HistoryLog;
        private System.Windows.Forms.PictureBox PictureBox;
        private System.Windows.Forms.TextBox StatusBox;
    }
}

