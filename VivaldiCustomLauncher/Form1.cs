using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VivaldiCustomLauncher
{
    public partial class Form1 : Form
    {
        private const string ImageFileExecutionOptionsKey =
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

        private const string HijackableExecutableFilename = "vivaldi.exe";

        public Form1()
        {
            InitializeComponent();

            checkBox1.Checked = IsHijacking();
        }

        private bool IsHijacking()
        {
            using (RegistryKey imageFileExecutionOptionsKey = Registry.LocalMachine.OpenSubKey(ImageFileExecutionOptionsKey, false))
            using (RegistryKey vivaldiKey = imageFileExecutionOptionsKey.OpenSubKey(HijackableExecutableFilename, false))
            {
                string debuggerValue = vivaldiKey?.GetValue("Debugger") as string;
                return debuggerValue?.Equals(GetCurrentProgramAbsolutePath(), StringComparison.InvariantCultureIgnoreCase) ?? false;
            }
        }

        private static string GetCurrentProgramAbsolutePath()
        {
            return Environment.CommandLine;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool shouldHijack = checkBox1.Checked;

            using (RegistryKey imageFileExecutionOptionsKey = Registry.LocalMachine.OpenSubKey(ImageFileExecutionOptionsKey, true))
            {
                if (shouldHijack)
                {
                    using (RegistryKey vivaldiKey = imageFileExecutionOptionsKey.CreateSubKey(HijackableExecutableFilename))
                    {
                        vivaldiKey.SetValue("Debugger", GetCurrentProgramAbsolutePath()); //right value?
                    }
                }
                else
                {
                    imageFileExecutionOptionsKey.DeleteSubKey(HijackableExecutableFilename, false);
                }
            }

            Close();
        }
    }
}