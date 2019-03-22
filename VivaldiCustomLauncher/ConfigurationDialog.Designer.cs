namespace VivaldiCustomLauncher
{
    partial class ConfigurationDialog
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
            this.hijackCheckBox = new System.Windows.Forms.CheckBox();
            this.SaveButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // hijackCheckBox
            // 
            this.hijackCheckBox.AutoSize = true;
            this.hijackCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.hijackCheckBox.Location = new System.Drawing.Point(12, 12);
            this.hijackCheckBox.Name = "hijackCheckBox";
            this.hijackCheckBox.Size = new System.Drawing.Size(134, 18);
            this.hijackCheckBox.TabIndex = 0;
            this.hijackCheckBox.Text = "Hijack Vivaldi image";
            this.hijackCheckBox.UseVisualStyleBackColor = true;
            // 
            // SaveButton
            // 
            this.SaveButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.SaveButton.Location = new System.Drawing.Point(12, 35);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(122, 23);
            this.SaveButton.TabIndex = 1;
            this.SaveButton.Text = "Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.OnSaveButtonClick);
            // 
            // ConfigurationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(146, 70);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.hijackCheckBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ConfigurationDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Vivaldi Custom Launcher";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox hijackCheckBox;
        private System.Windows.Forms.Button SaveButton;
    }
}

