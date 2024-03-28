/*
    Software created by MrDude - please leave this information intact if you intend to mod or use
    or distibute any or all parts of this program. Or at least give me a mention in your program if
    you decide to use any of this code.
    
    https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP.CSharpExamples/UploadDirectory.cs
    https://www.c-sharpcorner.com/article/display-sub-directories-and-files-in-treeview/
*/

using FluentFTP;
using FluentFTP.Rules;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
//using System.Xml.Linq;
//using System.Reflection;
//using System.ComponentModel;
//using System.Threading.Tasks;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.ScrollBar;

namespace IPS_Patch_Creator
{
    public partial class Main : Form
    {
        //variables so we can access and set data from wilcards database form
        public static Main instance;
        public TextBox tb_espatch;
        public TextBox tb_fs_patch1;
        public TextBox tb_fs_patch2;
        public TextBox tb_nfim_patch1;
        public TextBox tb_nfim_patch2;
        public TextBox tb_es_override;
        public TextBox tb_fs1_override;
        public TextBox tb_fs2_override;
        public TextBox tb_nfim_override;
        //end of variable so we can access and set data from wilcards database form

        public static string mydb = ("tools/config.db");
        public static string mydatabase = ("Data Source=" + mydb);

        public static string linkdb = ("tools/links.db");
        public static string linkdatabase = ("Data Source=" + linkdb);

        public static string ftpdb = ("tools/ftp.db");
        public static string ftpdatabase = ("Data Source=" + ftpdb);

        public static string wildacarddb = ("tools/wildcards.db");
        public static string wildcarddatabase = ("Data Source=" + wildacarddb);

        /**************************************************/
        /**************** START OF VARIABLES **************/
        /**************************************************/
        string Version = "1.5.7";
        string Loaderpath; //create variable for package3 file path location.
        string shaValue; //create a variable to store the sha256 value.
        string[] goodArray; //create an array to store firmware files we want to check.
        string NCA_dir; //create a variable to store the firmware nca file location.
        string NCA_file; //create a variable to store the NCA file we want to make a patch for.
        string sdk; //create a variable to store the firmware sdk version.
        string BuildID; //create a variable to store the nca main's build id.
        string FW_Path; //Variable for drag and drop.
        string FAT; //variable to store the nca file name.
        string EXFAT;  //variable to store the nca file name.
        string FATSHA; //Variable for FS IPS patch names.
        string EXFATSHA; //Variable for FS IPS patch names.
        string Fat_patches_ini1; //Store string for patches.ini fat instruction 1
        string Fat_patches_ini2; //Store string for patches.ini fat instruction 1
        string ExFat_patches_ini1; //Store string for patches.ini exfat instruction 1
        string ExFat_patches_ini2; //Store string for patches.ini exfat instruction 2
        int patch_offset; //create a variable to store the ips patch offset.
        int minimum; //variable value used to search for firmware files.
        int maximum; //variable value used to search for firmware files.
        int cancel = 0; //variable value used to skip running routines if the user presses the cancel button.
        int fs_array = 0;
        int ES2 = 0; //variable so we can reuse so ES code.
        int FATPatch1;
        int FATPatch2;
        int ExFatPatch1;
        int ExFatPatch2;
        int ES2_LOC1; //Es2 Patch 1 location
        int ES2_LOC2; //Es2 Patch 2 location
        int ES2_LOC3; //Es2 Patch 3 location
        UInt32 ES2_revloc; //variable to store bitshit data for es2 patches
        UInt32 ES2_revloc3; //variable to store bitshit data for es2 patches
        int theme; //Set default theme
        int killfsroutine = 0; //stop patches.ini being written if offset search failed
        int killesroutine = 0; //stop ips patches being written if offset search in es2 failed
        int killnfim = 0;
        int clean_message = 1;
        //Set size limits for nca files to process - read from config database.
        int ESmin;
        int ESmax;
        int FSmin;
        int FSmax;
        int NFIMmin;
        int NFIMmax;
        //------ ftp variables ------//
        string ip;
        string port;
        string user;
        string pass;
        /**************************************************/
        /***************** END OF VARIABLES ***************/
        /**************************************************/

        public Main()
        {
            InitializeComponent();
            instance = this;

            bool exists = System.IO.Directory.Exists("./tools");
            if (!exists)
            {
                System.IO.Directory.CreateDirectory("./tools");
            }
            if (!File.Exists("./tools/hactool.exe"))
            {
                MessageBox.Show("Hactool is missing.\n\nYour web browser will now open. Download the release, extract hactool and place it in the tools directory.", "Hactool Missing!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                try
                {
                    string myval = "https://github.com/mrdude2478/IPS_Patch_Creator/releases";
                    System.Diagnostics.Process.Start(myval);
                }

                catch (Exception error)
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }

            //get data from textboxes so we can send to the wilcards form
            tb_espatch = textBox_es_patch;
            tb_fs_patch1 = textBox_fs_patch1;
            tb_fs_patch2 = textBox_fs_patch2;
            tb_nfim_patch1 = textBox_nfim_patch1;
            tb_nfim_patch2 = textBox_nfim_patch2;
            tb_es_override = textBox_es_override;
            tb_fs1_override = textBox_fs1_override;
            tb_fs2_override = textBox_fs2_overide;
            tb_nfim_override = textBox_nfim_override;

            comboBox1_additems();

            if (!File.Exists(mydb))
            {
                config();
            }

            //read config and set variables....
            read_config();

            //set theme
            if (theme == 1)
            {
                black_and_white();
            }

            richTextBox_IPS_Patch_Creator.Text = "Loader IPS patch information.";
            richTextBox_IPS_Patch_Creator.Text += "\n\n" + "Loader patches are required to:" +
                "\nRun NRO forwarders." +
                "\nRun installed XCI games." +
                "\n" +
                "\nLoader patches are NOT required to:" +
                "\nRun installed NSP games. (if a valid ticket exists)" +
                "\nRun homebrew." +
                "\nInstall NSP or XCI files.";

            richTextBox_ES.Text = "ES IPS patch information.";
            richTextBox_ES.Text += "\n\n" + "ES patches are required to:" +
                "\nRun installed NSP files." +
                "\nInstall NSP files without errors." +
                "\n" +
                "\nES patches are NOT required to:" +
                "\nRun installed XCI files." +
                "\nRun installed NRO forwarders." +
                "\nRun homebrew." +
                "\nInstall XCI files.";

            richTextBox_ES2.Text = "ES2 IPS patch information. (Use only for firmware 9.0.1 and above)";
            richTextBox_ES2.Text += "\n\n" + "Eticket Service patches are for running raw and untouched NSP files and installed pirated games.\n(See previous tab for more information.)" +
                "\n\nYou only need to run this if the previous ES tab failed to generate a working patch.";

            richTextBox_FS.Text = "FS IPS patch information." +
                "\n" +
                "\nFS patches are required to:" +
                "\nRun installed XCI games." +
                "\nRun NRO forwarders." +
                "\n(If FS patches are missing or wrong, you will get a corruption error when trying to run the installed XCI or NRO forwarder." +
                "You will also need to re-install the XCI or NRO forwarder if this happens)" +
                "\n" +
                "\nFS patches are NOT required to:" +
                "\nRun installed NSP files with valid tickets." +
                "\nRun homebrew." +
                "\nInstall NSP or XCI files. (that contain valid headers)";

            richTextBox_NFIM.Text = "NFIM IPS patch information.";
            richTextBox_NFIM.Text += "\n\n" + "NFIM patches skip the connection test to allow connections to networks without internet access or dns wildcard blocks (e.g. *nintendo*).";
            richTextBox_NFIM.Text += "\n\n" + "(All current firmware versions are supported)";

            console_box.Text = "This page is for testing new routines.";

            //remove unused tabs until the code is completed.
            tabControl1.Controls.Remove(tabPage_test);
            tabControl1.Controls.Remove(tabPage_info); //hide until the user needs it.

            if (!File.Exists("tools/keys.dat"))
            {
                if (MessageBox.Show("Tools/keys.dat does not exist. Would you like me to generate a template file for you to fill in?", "IPS Patch Creator", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    make_key_template();
                }
            }
            else
            {
                string filetext = File.ReadAllText("tools/keys.dat");
                richTextBox_keys.Text = filetext;
            }
        }

        /**************************************************/
        /*************** START OF Loader CODE *************/
        /**************************************************/

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            load();
            button1.Enabled = true;
        }

        private void button1_DragDrop(object sender, DragEventArgs e)
        {
            button1.Enabled = false;
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            try
            {
                patch_offset = 0; //clear the patch location
                richTextBox_IPS_Patch_Creator.Clear();
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Loaderpath = Path.GetFullPath(files[0]);
                bool myfile = Loaderpath.Contains("package3") || Loaderpath.Contains("fusee-secondary");
                if (myfile)
                {
                    //IPS_Patch_Creatorpath = IPS_Patch_Creatorpath.Replace(@"\", @"\\");
                    richTextBox_IPS_Patch_Creator.Clear();
                    FindBytes();
                }
                else
                {
                    MessageBox.Show("File not supported, use a package3 or fusee-secondary file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
            button1.Enabled = true;
        }

        private void button1_DragOver(object sender, DragEventArgs e)
        {
            try

            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Link;
                else
                    e.Effect = DragDropEffects.None;
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        public void load()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "package3|package3|fusee-secondary|fusee-secondary.bin|All Files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {

                    Loaderpath = openFileDialog.FileName;
                    bool myfile = Loaderpath.Contains("package3") || Loaderpath.Contains("fusee-secondary");
                    if (myfile)
                    {
                        patch_offset = 0; //clear the patch location
                        richTextBox_IPS_Patch_Creator.Clear();
                        richTextBox_IPS_Patch_Creator.Text += "Selected file: " + Loaderpath + "\n";
                        FindBytes();
                    }

                    else
                    {
                        MessageBox.Show("File not supported, use a package3 or fusee-secondary file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void FindBytes()
        {
            try
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                byte[] ByteBuffer = File.ReadAllBytes(Loaderpath);

                //old search routine - faster but causes errors if search fails.
                int i;
                int j;
                byte[] StringBytes = Encoding.UTF8.GetBytes("oader"); //new atmosphere uses uppercase L so skip looking for that to avoid problems

                for (i = 0; i <= (ByteBuffer.Length - StringBytes.Length); i++)
                {
                    if (ByteBuffer[i] == StringBytes[0])
                    {
                        for (j = 1; j < StringBytes.Length && ByteBuffer[i + j] == StringBytes[j]; j++) ;
                        if (j == StringBytes.Length)
                            break;
                    }
                }

                int startpos = (i - 17); // where the ascii text "oader" is found (minus 17 bytes)
                if (startpos != 0)
                {

                    int sizeloc = (startpos + 4); //size of the IPS_Patch_Creator is found 4 bytes from the startpos
                    var location = new byte[4];
                    var size = new byte[4];

                    //MessageBox.Show(i.ToString()); //only used for debugging.

                    //get IPS_Patch_Creator start location
                    using (BinaryReader reader = new BinaryReader(new FileStream(Loaderpath, FileMode.Open)))
                    {
                        reader.BaseStream.Seek(startpos, SeekOrigin.Begin);
                        reader.Read(location, 0, 4);

                        //get the size of the IPS_Patch_Creator
                        reader.BaseStream.Seek(sizeloc, SeekOrigin.Begin);
                        reader.Read(size, 0, 4);
                    }


                    //convert bytes to int:
                    var IPS_Patch_Creatorsize = BitConverter.ToUInt32(size, 0);
                    var startloc = BitConverter.ToUInt32(location, 0);
                    var end = (IPS_Patch_Creatorsize + startloc);

                    //call code to dump the IPS_Patch_Creator here now we know the size and start and end locations.
                    using (var output = File.Create("dumped_Loader")) //set path of dumped_IPS_Patch_Creator
                    using (var input = File.OpenRead(Loaderpath)) //open package3 so we can dump the IPS_Patch_Creator
                    {
                        // if there's timing issues with the dumped IPS_Patch_Creator not being created fast enough - put this in a new thread.
                        AppendChunk(output, input, startloc, end); //dump the IPS_Patch_Creator
                    }

                    //revese location bytes
                    Array.Reverse(location);
                    string location_hex = BitConverter.ToString(location);

                    //reverse end location bytes
                    byte[] endlocation = BitConverter.GetBytes(end); //convert end int location to byte array
                                                                     //MessageBox.Show(String.Join("",endlocation.Select(x => x.ToString("X2"))));
                    Array.Reverse(endlocation);

                    //Not used - but can mod byte array manually....for changing instructions.
                    // UInt32 revloc = ReverseBytes(end);

                    richTextBox_IPS_Patch_Creator.Text += "Loader size: " + IPS_Patch_Creatorsize.ToString() + " bytes";
                    richTextBox_IPS_Patch_Creator.Text += "\n" + "Loader start offset: 0x" + location_hex.Replace("-", "");
                    richTextBox_IPS_Patch_Creator.Text += "\n" + "Loader end offset: 0x" + String.Join("", endlocation.Select(x => x.ToString("X2")));

                    //IPS_Patch_Creator should be dumped now - so let's get the sha256 value;
                    Sha256();

                    //Now we can decrypt the dumped IPS_Patch_Creator...
                    Decrypt_IPS_Patch_Creator();

                    //now try to find the patch location in the decypted IPS_Patch_Creator
                    FindIPS_Patch_CreatorPatch();

                    //write ips patch
                    WriteIPS();

                    //remove decrypted IPS_Patch_Creator now.
                    if (checkBox_decrypted.Checked)
                    {
                        File.Delete("dec-Loader.bin");
                        richTextBox_IPS_Patch_Creator.Text += "\n" + "Decrypted Loader cleaned up";
                    }

                    //Show patches ini info
                    IPS_Patch_Creator_PatchesINI();

                    richTextBox_IPS_Patch_Creator.Text += "\n" + "Finished";
                    SystemSounds.Asterisk.Play();

                }
                else
                {
                    richTextBox_IPS_Patch_Creator.ForeColor = Color.Red;
                    richTextBox_IPS_Patch_Creator.Text += "Loader search pattern was not found.\n\nYou should use Atmosphere version 0.8.5 or newer.\n\nIf searching fails on new Atmosphere versions this program will need updated.";
                    SystemSounds.Exclamation.Play();
                }

                watch.Stop();
                string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                richTextBox_IPS_Patch_Creator.Text += " - " + ($"time taken: {Time}");
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }

        }

        private void makedirs()
        {
            try
            {
                if (!Directory.Exists("atmosphere"))
                {
                    Directory.CreateDirectory("atmosphere");
                }

                if (!Directory.Exists("atmosphere\\kip_patches"))
                {
                    Directory.CreateDirectory("atmosphere\\kip_patches");
                }

                if (!Directory.Exists("atmosphere\\kip_patches\\loader_patches"))
                {
                    Directory.CreateDirectory("atmosphere\\kip_patches\\loader_patches");
                    richTextBox_IPS_Patch_Creator.Text += "\n" + "Patch directories created";
                }

                if (checkBox_PatchesINI.Checked)
                {
                    if (!Directory.Exists("bootloader"))
                    {
                        Directory.CreateDirectory("bootloader");
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void WriteIPS()
        {
            try
            {
                if (patch_offset > 0)
                {
                    //create Atmosphere ips directory if it doesn't exist.
                    makedirs();
                    using (var stream = File.Open("atmosphere\\kip_patches\\loader_patches\\" + shaValue + ".ips", FileMode.Create))
                    {
                        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                        {
                            byte[] StartBytes = new byte[] { 0x50, 0x41, 0x54, 0x43, 0x48 };
                            byte[] EndBytes = new byte[] { 0x00, 0x01, 0x00, 0x45, 0x4F, 0x46 };
                            byte[] Middlebytes = BitConverter.GetBytes(patch_offset);

                            writer.Write(StartBytes);

                            //stupid code for testing = also reverse bytes
                            if (patch_offset > 0 && patch_offset <= 16777215)
                            {
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            if (patch_offset > 16777215)
                            {
                                writer.Write(Middlebytes[3]);
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            //end of stupid code
                            writer.Write(EndBytes);
                            richTextBox_IPS_Patch_Creator.Text += "\n" + "IPS patch written";
                        }
                    }
                }
                else
                {
                    richTextBox_IPS_Patch_Creator.ForeColor = Color.Red;
                    richTextBox_IPS_Patch_Creator.Text += "\n" + "Unable to find the pattern offset so the IPS patch was not created";
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void FindIPS_Patch_CreatorPatch()
        {
            try
            {
                int x;
                int y;
                byte[] ByteBuffer = File.ReadAllBytes("dec-Loader.bin");
                byte[] StringBytes = new byte[8] { 0x01, 0xC0, 0xBE, 0x12, 0x1F, 0x00, 0x01, 0x6B };


                for (x = 0; x <= (ByteBuffer.Length - StringBytes.Length); x++)
                {
                    if (ByteBuffer[x] == StringBytes[0])
                    {
                        for (y = 1; y < StringBytes.Length && ByteBuffer[x + y] == StringBytes[y]; y++) ;
                        if (y == StringBytes.Length)
                            break;
                    }
                }

                //clear the arrays.
                Array.Clear(ByteBuffer, 0, ByteBuffer.Length);
                Array.Clear(ByteBuffer, 0, StringBytes.Length);

                patch_offset = (x + 6);
                string hex = patch_offset.ToString("X8");

                richTextBox_IPS_Patch_Creator.Text += "\n" + "Patch location: 0x" + hex;
                //label_patch.Text = patchloc.ToString();

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void Decrypt_IPS_Patch_Creator()
        {
            try
            {
                Process process = new Process();
                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    process.StartInfo.FileName = "tools\\hactoolnet.exe";
                    process.StartInfo.Arguments = " -t kip1 --uncompressed dec-Loader.bin dumped_Loader";
                }
                else
                {
                    process.StartInfo.FileName = "tools\\hactool.exe";
                    process.StartInfo.Arguments = " --disableoutput --intype=kip1 --uncompressed=dec-Loader.bin dumped_Loader";
                }
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit(); //make sure hactool has completed the decryption.
                richTextBox_IPS_Patch_Creator.Text += "\n" + "Loader decrypted";

                //we may as well remove the dumped IPS_Patch_Creator here as it's not needed anymore
                if (checkBox_extracted.Checked)
                {
                    File.Delete("dumped_Loader");
                    richTextBox_IPS_Patch_Creator.Text += "\n" + "Extracted Loader cleaned up";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void Sha256()
        {
            try
            {
                using (SHA256 mySHA256 = SHA256.Create())
                {
                    using (BinaryReader read = new BinaryReader(new FileStream("dumped_Loader", FileMode.Open)))
                    {
                        read.BaseStream.Position = 0;
                        byte[] hashValue = mySHA256.ComputeHash(read.BaseStream);
                        shaValue = String.Join("", hashValue.Select(x => x.ToString("X2")));
                        richTextBox_IPS_Patch_Creator.Text += "\n" + "Sha256: " + shaValue;
                        //note - we should name our ips patch with the shaValue from above.
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void IPS_Patch_Creator_PatchesINI()
        {
            try
            {
                int location = patch_offset - 256;
                string hexval = location.ToString("X2");
                string shaval = shaValue.Substring(0, 16);
                richTextBox_IPS_Patch_Creator.Text += "\n\n" + "******* Info for patches.ini *******";
                richTextBox_IPS_Patch_Creator.Text += "\n" + "[Loader:" + shaval + "]";
                richTextBox_IPS_Patch_Creator.Text += "\n" + ".nosigchk=0:0x" + hexval + ":0x1:01,00";
                richTextBox_IPS_Patch_Creator.Text += "\n" + "*************************************";
                richTextBox_IPS_Patch_Creator.Text += "\n";

                //create patches.ini
                string inipath = "bootloader\\patches.ini";
                if (checkBox_PatchesINI.Checked)
                {
                    if (!File.Exists(inipath))
                    {
                        var stream = File.Open("bootloader\\patches.ini", FileMode.Create);
                        stream.Close();
                    }
                    using (StreamWriter sw = File.AppendText(inipath))
                    {
                        sw.WriteLine("[Loader:" + shaval + "]");
                        sw.WriteLine(".nosigchk=0:0x" + hexval + ":0x1:01,00");
                        sw.WriteLine();
                        sw.Close();
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private static void AppendChunk(Stream output, Stream input, long start, long end)
        {
            long xsize = end - start;
            byte[] buffer = new byte[1 * 1024]; // Copy 1K at a time
            input.Position = start;
            while (xsize > 0)
            {
                int bytesRead = input.Read(buffer, 0, (int)Math.Min(xsize, buffer.Length));
                if (bytesRead <= 0)
                {
                    throw new EndOfStreamException("Not enough data");
                }
                output.Write(buffer, 0, bytesRead);
                xsize -= bytesRead;
            }
        }

        /**************************************************/
        /**************** END OF Loader CODE **************/
        /**************************************************/

        /**************************************************/
        /************* START OF TOOLSTRIP CODE ************/
        /**************************************************/
        private void onlineInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Form Help = new Help();
                this.Hide();
                Help.ShowDialog();
                this.Show();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void exploreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string mydir = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = mydir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void atmosphereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string myval = "https://github.com/Atmosphere-NX/Atmosphere/releases";
                System.Diagnostics.Process.Start(myval);
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void hekateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string myval = "https://github.com/CTCaer/hekate/releases";
                System.Diagnostics.Process.Start(myval);
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void hactoolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string myval = "https://github.com/SciresM/hactool/releases";
                System.Diagnostics.Process.Start(myval);
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void hactoolnetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string myval = "https://github.com/Thealexbarney/LibHac/releases";
                System.Diagnostics.Process.Start(myval);
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void sysPatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string myval = "https://github.com/ITotalJustice/sys-patch/releases";
                System.Diagnostics.Process.Start(myval);
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            richTextBox_IPS_Patch_Creator.Copy();
        }

        private void copyToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            richTextBox_ES.Copy();
        }

        private void copyToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            richTextBox_ES2.Copy();
        }

        private void copyToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            richTextBox_FS.Copy();
        }

        private void copyToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            richTextBox_NFIM.Copy();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            richTextBox_keys.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox_keys.Paste();
        }

        private void weblinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Form web = new Weblinks();
                this.Hide();
                web.ShowDialog();
                this.Show();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void programInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!tabControl1.TabPages.Contains(tabPage_info))
            {
                tabControl1.Controls.Add(tabPage_info);
            }
            tabControl1.SelectedTab = tabControl1.TabPages["tabPage_info"];

            //lets put some text in about the program!
            richTextBox_info.Clear();
            richTextBox_info.Text += "IPS Patch Creator Version: " + Version + " - Created by MrDude";
            richTextBox_info.Text += "\n\n" + "F.U Ninty! When you mess with people's sigpatch repo's, expect some pushback from the scene!";
            richTextBox_info.Text += "\n\n" + "Thanks to the following (In no particular order):";
            richTextBox_info.Text += "\n" + "Crckd - for hash and decryption information.";
            richTextBox_info.Text += "\n" + "ShadowOne333 - for algo information.";
            richTextBox_info.Text += "\n" + "DarkMatterCore - for technical NCA information.";
            richTextBox_info.Text += "\n" + "Unknown hacker - for bitshifting information.";
            richTextBox_info.Text += "\n" + "Impeeza - for testing.";
            richTextBox_info.Text += "\n" + "LyuboA - for testing.";
            richTextBox_info.Text += "\n\n" + "Release thread: " + "https://gbatemp.net/threads/info-on-sha-256-hashes-on-fs-patches.581550/";
        }

        private void whiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                black_and_white();
                set_theme_config();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                green_and_black();
                set_theme_config();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void green_and_black()
        {
            try
            {
                theme = 0;
                richTextBox_IPS_Patch_Creator.BackColor = Color.Black;
                richTextBox_IPS_Patch_Creator.ForeColor = Color.Lime;
                richTextBox_FS.BackColor = Color.Black;
                richTextBox_FS.ForeColor = Color.Lime;
                richTextBox_ES.BackColor = Color.Black;
                richTextBox_ES.ForeColor = Color.Lime;
                richTextBox_ES2.BackColor = Color.Black;
                richTextBox_ES2.ForeColor = Color.Lime;
                richTextBox_NFIM.BackColor = Color.Black;
                richTextBox_NFIM.ForeColor = Color.Lime;
                richTextBox_keys.BackColor = Color.Black;
                richTextBox_keys.ForeColor = Color.Lime;
                richTextBox_info.BackColor = Color.Black;
                richTextBox_info.ForeColor = Color.Lime;
                richTextBox_Base64.BackColor = Color.Black;
                richTextBox_Base64.ForeColor = Color.Lime;

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void black_and_white()
        {
            try
            {
                theme = 1;
                richTextBox_IPS_Patch_Creator.BackColor = Color.White;
                richTextBox_IPS_Patch_Creator.ForeColor = Color.Black;
                richTextBox_FS.BackColor = Color.White;
                richTextBox_FS.ForeColor = Color.Black;
                richTextBox_ES.BackColor = Color.White;
                richTextBox_ES.ForeColor = Color.Black;
                richTextBox_ES2.BackColor = Color.White;
                richTextBox_ES2.ForeColor = Color.Black;
                richTextBox_NFIM.BackColor = Color.White;
                richTextBox_NFIM.ForeColor = Color.Black;
                richTextBox_keys.BackColor = Color.White;
                richTextBox_keys.ForeColor = Color.Black;
                richTextBox_info.BackColor = Color.White;
                richTextBox_info.ForeColor = Color.Black;
                richTextBox_Base64.BackColor = Color.White;
                richTextBox_Base64.ForeColor = Color.Black;
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void set_theme_config()
        {
            try
            {
                //Store value in config database
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                string themeval = theme.ToString();
                cmd.CommandText = ("UPDATE Theme SET value = '" + themeval + "' WHERE rowid = 1");
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void config()
        {
            try
            {
                Form config = new Config();
                this.Hide();
                config.ShowDialog();
                this.Show();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        /**************************************************/
        /************* END OF TOOLSTRIP CODE **************/
        /**************************************************/

        /**************************************************/
        /*************** START OF TEST CODE ***************/
        /**************************************************/

        private void button_CBZ_Click(object sender, EventArgs e)
        {
            string inst = "20010034";
            UInt32 intValue = UInt32.Parse(inst, System.Globalization.NumberStyles.HexNumber);
            UInt32 revloc = CBZ(intValue); //bitshifts 0x20010034 to make it 0x09000014
            byte[] bytes = BitConverter.GetBytes(revloc); //convert above int value into a byte array.
            string hex = BitConverter.ToString(bytes).Replace("-", ""); //convert byte array to string.
            console_box.Text = "Bitshifting test to convert 0x20010034 to make it 0x09000014";
            console_box.Text += "\n\n" + "0x" + hex;
        }

        private void button_mov_Click(object sender, EventArgs e)
        {
            string inst = "36D28152";
            UInt32 intValue = UInt32.Parse(inst, System.Globalization.NumberStyles.HexNumber);
            UInt32 revloc = MOV(intValue); //bitshifts 0x36D28152 to make it 0x1F2003D5 (NOP instruction)
            byte[] bytes = BitConverter.GetBytes(revloc); //convert above int value into a byte array.
            string hex = bytes[3].ToString(); //returns dec 82, which is the same as 0x52
            if (bytes[3] == 82)
            {
                console_box.Text = "Bitshifting test shifts last 2 bytes of 36D28152 to first 2 bytes and checks the value is equal to 82 (0x52)";
                console_box.Text += "\n\n" + "Test passed so we can replace hex with another instruction such as 0x1F2003D5 - (NOP Instruction)";
            }
            else
            {
                console_box.Text = "Bitshifting test shifts last 2 bytes of 36D28152 to first 2 bytes and then shows the int value of those bytes if they didn't equal 82 (0x52)";
                console_box.Text += "\n\n" + "0x" + hex;
            }
        }

        private void button_wildcard_Click(object sender, EventArgs e)
        {
            try
            {
                /*
                //byte[] StringBytes = new byte[] { 0x02, 0x4D, 0x00, 0x68, 0x6B, 0x6A };
                //string str = string.Join(",", StringBytes);
                //richTextBox1.Text = str;
                //string find = "2,??,0,???,107,106";
                */

                string str = "024D00686B6A686A";
                string find = "024D00686B....6A";

                if (Regex.IsMatch(str, find))
                {
                    console_box.Text = "Test for wildcard searching - string found!";
                }
                else
                {
                    console_box.Text = "Test for wildcard searching - string not found!";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        public static UInt32 ReverseBytes(UInt32 value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        public static UInt32 CBZ(UInt32 value)
        {
            value = ReverseBytes(value); //reverse bytes or we get the wrong thing...
            return (0x14 << 24) | ((value >> 5) & 0x7FFFF);
        }

        public static UInt32 MOV(UInt32 value)
        {
            return (value << 24); //check return number is 0x52 (Dec 82) (MOV instruction)
        }

        /**************************************************/
        /**************** END OF TEST CODE ****************/
        /**************************************************/

        /**************************************************/
        /**************** START OF ES CODE ****************/
        /**************************************************/

        private void button_es_files_Click(object sender, EventArgs e)
        {
            ES2 = 0;
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            ES_start();
        }

        private void ES_start()
        {
            button_es_files.Enabled = false;
            read_config(); //read the database to make sure the values are updated
            try
            {
                richTextBox_ES.Clear();
                minimum = ESmin;
                maximum = ESmax;
                cancel = 0; //reset this value before searching for firmware files

                sdk = ""; //clear sdk version
                patch_offset = 0; //reset patch offset

                //get the firmware folder, and create an array of nca files to check.
                fwsearch();

                //set up timer
                var watch = new System.Diagnostics.Stopwatch();

                //continue running code if the user didn't cancel searching for firmware
                if (cancel == 0)
                {
                    //start timer
                    watch.Start();

                    richTextBox_ES.Text += "\n";

                    //do stuff to the array
                    foreach (var item in goodArray)
                    {
                        richTextBox_ES.Text += "Files to process: " + goodArray.Length.ToString() + "\n";

                        //use hactool to find the nca we want.
                        es_find();

                        //remove item from array when we are done with it
                        int indexToRemove = 0; //remove item from index 0 in our list.
                        goodArray = goodArray.Where((source, index) => index != indexToRemove).ToArray();

                        if (console_box.Text.Contains("found"))
                        {
                            break;
                        }
                    }

                    //lets check keys.dat exists or we won't be able to decrypt anything....
                    if (!File.Exists("tools/keys.dat"))
                    {
                        MessageBox.Show("Keys.dat is missing", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    else
                    {
                        //we should now have the nca file name we want to manipulate (NCA_file variable), so we can do the other calls from here.

                        //check the sdk version of firmware.
                        foreach (string line in console_box.Lines)
                        {
                            if (line.Contains("SDKVersion:"))
                            {
                                sdk = line.ToString();
                                richTextBox_ES.Text += "\n" + sdk;
                                break;
                            }
                        }

                        //quit if sdk version is wrong.
                        string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                        int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                        if (SDKVersion < 9300 || SDKVersion == 82990 || SDKVersion == 011290)
                        {
                            richTextBox_ES.ForeColor = Color.Red;
                            richTextBox_ES.Text += "\n" + "Firmware is too old for FS patching, download pre-made patches instead\n\n";
                            MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            button_es_files.Enabled = true;
                            return;
                        }

                        //clear the console box since we don't need it now as we have all the info we need.
                        console_box.Clear();

                        //We should probably put a check here so this code doesn't run if we didn't find the sdk version
                        if (sdk != "")
                        {
                            //extract nca main file
                            Extract_nca();

                            //get the build ID from main_dec - starts at offset 0x40 and is 0x14 long.
                            if (File.Exists("main_dec"))
                            {
                                Get_build();
                            }
                            else
                            {
                                richTextBox_ES.ForeColor = Color.Red;
                                richTextBox_ES.Text = "Unable to decrypt, did you update your keys?\n\nTry adjusting the ES size limits in the config settings if firmware is greater than 15.0.0.";
                                button_es_files.Enabled = true;
                                SystemSounds.Exclamation.Play();
                                return;
                            }

                            //Now we know the build ID and SDK version and unpacked the nca to get the decrypted main file - we can search for offsets...
                            ES_Offset_Search();

                            //We should now know where to patch, so make some folders to store the patch.
                            ES_Patch_Folders();

                            //Everything should now be in place to make a patch
                            ES_Patch_Creation();

                            //clean up decrypted main file how we know the offsets
                            if (checkBox_maindec_es.Checked)
                            {
                                if (File.Exists("main_dec"))
                                {
                                    File.Delete("main_dec");
                                    richTextBox_ES.Text += "\n\n" + "Decrypted file removed: main_dec";
                                }
                            }

                            richTextBox_ES.Text += "\n\n" + "Finished";
                            SystemSounds.Asterisk.Play();

                        }

                        else
                        {
                            richTextBox_ES.ForeColor = Color.Red;
                            richTextBox_ES.Text += "\n" + "Unable to find the sdk version. Did you select a folder that contains nca files?\n\nPossibly you need to update keys.dat as I can't decrypt the firmware";
                            SystemSounds.Exclamation.Play();
                        }
                    }

                    watch.Stop();
                    string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                    richTextBox_ES.Text += " - " + ($"time taken: {Time}");
                }
            }

            catch (Exception error)
            {
                string warning = error.Message;
                if (warning == "Input string was not in a correct format.")
                {
                    MessageBox.Show("Did you use the correct keys in keys.dat?", "Can't Decrypt the firmware", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }

            button_es_files.Enabled = true;
        }

        private void ES_Patch_Creation()
        {
            try
            {
                string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                int SDKVersion = Int32.Parse(strippedsdk);

                if (patch_offset > 0)
                {
                    using (var stream = File.Open("atmosphere\\exefs_patches\\es_patches\\" + BuildID + ".ips", FileMode.Create))
                    {
                        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                        {
                            byte[] StartBytes = new byte[] { 0x50, 0x41, 0x54, 0x43, 0x48 };
                            byte[] EndBytes = new byte[] { 0x45, 0x4F, 0x46 };
                            byte[] Middlebytes = BitConverter.GetBytes(patch_offset);
                            byte[] PatchBytes = new byte[] { 0xE0, 0x03, 0x1F, 0xAA };
                            byte[] PaddingBytes = new byte[] { 0x00, 0x04 };


                            if (SDKVersion >= 14300) //#fw 14.0.0 or higher patch override code
                            {
                                if (checkBox_es_patch_override.Checked == true)
                                {
                                    //get text from es override text box
                                    string hexstring = textBox_es_patch.Text;
                                    uint patch = uint.Parse(hexstring, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch = ReverseBytes(patch);
                                    PatchBytes = BitConverter.GetBytes(patch);
                                }
                            }

                            writer.Write(StartBytes);

                            //if (patch_offset > 65535 && patch_offset <= 16777215)
                            if (patch_offset > 0 && patch_offset <= 16777215)
                            {
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            if (patch_offset > 16777215)
                            {
                                writer.Write(Middlebytes[3]);
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            //end of stupid code
                            writer.Write(PaddingBytes);
                            writer.Write(PatchBytes);
                            writer.Write(EndBytes);
                            richTextBox_ES.Text += "\n" + "ES ips patch written";
                        }
                    }
                }

                else
                {
                    richTextBox_ES.ForeColor = Color.Red;
                    richTextBox_ES.Text += "\n" + "Couldn't find patch offsets so ES ips patch was not written";
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void ES_Patch_Folders()
        {
            try
            {
                if (!Directory.Exists("atmosphere"))
                {
                    Directory.CreateDirectory("atmosphere");
                }

                if (!Directory.Exists("atmosphere\\exefs_patches"))
                {
                    Directory.CreateDirectory("atmosphere\\exefs_patches");
                }

                if (!Directory.Exists("atmosphere\\exefs_patches\\es_patches"))
                {
                    Directory.CreateDirectory("atmosphere\\exefs_patches\\es_patches");
                    if (ES2 == 0)
                    {
                        richTextBox_ES.Text += "\n\n" + "Missing es patch directories created";
                    }
                    else
                    {
                        richTextBox_ES2.Text += "\n\n" + "Missing es patch directories created";
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void ES_Offset_Search()
        {
            try
            {
                byte[] ByteBuffer = File.ReadAllBytes("main_dec");
                byte[] pattern = new byte[] { };
                int x;
                int y;
                int toggle = 0; //add a toggle so we can switch between wildcard searching or specific bytes.
                string find = ""; //variable pattern to use for wildcards.

                //Remove the text and periods from the sdk variable
                string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                if (SDKVersion > 0 & SDKVersion < 9300 || SDKVersion == 011290 || SDKVersion == 82990) //#fix out of bounds error
                {
                    toggle = 2;
                }

                else if (SDKVersion >= 9300 & SDKVersion < 10400)
                {
                    pattern = new byte[] { 0xFF, 0x97, 0xE0, 0x03, 0x13, 0xAA, 0xFD, 0x7B,
                                           0x48, 0xA9, 0xF4, 0x4F, 0x47, 0xA9, 0xFF, 0x43,
                                           0x02, 0x91, 0xC0, 0x03, 0x5F, 0xD6, 0x00, 0x00,
                                           0x00, 0x00 };

                    toggle = 0;
                }

                else if (SDKVersion == 10400)
                {
                    if (BuildID == "03E4EB5556B98B327D1353E8AA2C7ADF2C544470") //id for firmware 10.0.4
                    {
                        pattern = new byte[] { 0xFF, 0x97, 0xE0, 0x03, 0x13, 0xAA, 0xFD, 0x7B,
                                               0x48, 0xA9, 0xF4, 0x4F, 0x47, 0xA9, 0xFF, 0x43,
                                               0x02, 0x91, 0xC0, 0x03, 0x5F, 0xD6, 0x00, 0x00,
                                               0x00, 0x00 };

                        toggle = 0;
                    }

                    else
                    {
                        //wildcard patterns start here - always convert to lower case or regex won't work.
                        find = ("FF97.......A9........FFC3").ToLower();
                        find = find.Replace(".", "..");
                        toggle = 1;
                    }
                }

                else if (SDKVersion > 10400 & SDKVersion < 14300) //#start from fw 10.2.0 to 13.1.0
                {
                    find = ("FF97.......A9........FFC3").ToLower();
                    find = find.Replace(".", "..");
                    toggle = 1;
                }

                else if (SDKVersion >= 14300 & SDKVersion < 16200) //#fw 14.0.0 or higher
                {
                    if (checkBox_ES_override.Checked == true)
                    {
                        //get text from es override text box
                        string es_overide = textBox_es_override.Text.ToLower();
                        find = (es_overide).ToLower();
                    }
                    else
                    {
                        find = ("FF97......52A9........FFC30491C0035FD6").ToLower();
                        find = find.Replace(".", "..");
                    }
                    toggle = 1;
                }

                else if (SDKVersion >= 16200 & SDKVersion < 18300)
                {
                    if (checkBox_ES_override.Checked == true)
                    {
                        //get text from es override text box
                        string es_overide = textBox_es_override.Text.ToLower();
                        find = (es_overide).ToLower();
                    }
                    else
                    {
                        find = ("FF97......52A9........FFC30491C0035FD6").ToLower();
                        find = find.Replace(".", "..");
                    }
                    toggle = 1;
                }

                else if (SDKVersion >= 18300)
                {
                    if (checkBox_ES_override.Checked == true)
                    {
                        //get text from es override text box
                        string es_overide = textBox_es_override.Text.ToLower();
                        find = (es_overide).ToLower();
                    }
                    else
                    {
                        find = ("FF97......2A....5.A9................FF.30491C0035FD6").ToLower();
                        //find = find.Replace(".", "..");
                    }
                    toggle = 1;
                }

                //toggle error check here if pattern is empty or not defined yet.
                if (toggle == 0)
                {
                    for (x = 0; x <= (ByteBuffer.Length - pattern.Length); x++)
                    {
                        if (ByteBuffer[x] == pattern[0])
                        {
                            for (y = 1; y < pattern.Length && ByteBuffer[x + y] == pattern[y]; y++) ;
                            if (y == pattern.Length)
                                break;
                        }
                    }

                    patch_offset = (x + 2);
                    string hex = patch_offset.ToString("X8");
                    richTextBox_ES.Text += "\n" + "Search pattern found at offset: 0x" + x.ToString("X8");
                    richTextBox_ES.Text += "\n" + "Patch offset location: 0x" + hex;
                }

                else if (toggle == 1)
                {
                    //convert byte array Bytebuffer into a long hex string
                    StringBuilder hex = new StringBuilder(ByteBuffer.Length * 2);

                    foreach (byte b in ByteBuffer)
                        hex.AppendFormat("{0:x2}", b);

                    string str = hex.ToString();

                    if (checkBox_ES_override.Checked == true)
                    {
                        find = find.Replace(" ", "");
                    }

                    Match match = Regex.Match(str, find);
                    if (match.Success)
                    {
                        int index = match.Index;
                        index = index / 2; //make sure we divide by 2 again as we multiplied above...
                        patch_offset = index + 2;
                        richTextBox_ES.Text += "\n" + "Wildcard search pattern found at offset: 0x" + index.ToString("X8");
                        richTextBox_ES.Text += "\n" + "Probable patch offset location: 0x" + patch_offset.ToString("X8");

                    }
                    else
                    {
                        richTextBox_ES.ForeColor = Color.Red;
                        richTextBox_ES.Text += "\n" + "Hex search pattern was not found :-(";
                    }
                }

                else if (toggle == 2)
                {
                    richTextBox_ES.Text += "\n" + "Firmware is too old for ES2 patching, download pre-made patches instead";
                    MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("ES Offset Search - Error is: " + error.Message);
            }
        }

        private void Get_build()
        {
            try
            {
                //routine to extract build ID from main_dec.
                BuildID = ""; //clear build id
                if (ES2 == 0)
                {
                    richTextBox_ES.Text += "\n\n" + "Trying to get the Build ID for the ips patch";
                }
                else
                {
                    richTextBox_ES2.Text += "\n\n" + "Trying to get the Build ID for the ips patch";
                }
                string StarHex = "0x40";
                string SizeHex = "0x14";
                int start = Convert.ToInt32(StarHex, 16);
                int size = Convert.ToInt32(SizeHex, 16);

                var id = new byte[size];
                using (BinaryReader reader = new BinaryReader(new FileStream("main_dec", FileMode.Open)))
                {
                    reader.BaseStream.Seek(start, SeekOrigin.Begin);
                    reader.Read(id, 0, size);
                }

                string location_hex = BitConverter.ToString(id);
                BuildID = location_hex.Replace("-", "");
                if (BuildID != "")
                {
                    if (ES2 == 0)
                    {
                        richTextBox_ES.Text += "\n" + "Build ID: " + BuildID + "\n";
                    }
                    else
                    {
                        richTextBox_ES2.Text += "\n" + "Build ID: " + BuildID + "\n";
                    }
                }
                else
                {
                    if (ES2 == 0)
                    {
                        richTextBox_ES.ForeColor = Color.Red;
                        richTextBox_ES.Text += "\n" + "Unable to find the Build ID...IPS patch will not be created";
                    }
                    else
                    {
                        richTextBox_ES.ForeColor = Color.Red;
                        richTextBox_ES2.Text += "\n" + "Unable to find the Build ID...IPS patch will not be created";
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Get Build - Error is: " + error.Message);
            }
        }

        private void Extract_nca()
        {
            try
            {
                Process nca = new Process();
                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    nca.StartInfo.FileName = "tools//hactoolnet.exe";
                    nca.StartInfo.Arguments = " -k tools//keys.dat -t nca --exefsdir .  --disablekeywarns " + '"' + NCA_dir + NCA_file + '"';
                }
                else
                {
                    nca.StartInfo.FileName = "tools//hactool.exe";
                    nca.StartInfo.Arguments = " --keyset=tools//keys.dat --intype=nca --exefsdir=.  --disableoutput " + '"' + NCA_dir + NCA_file + '"';
                }

                nca.StartInfo.UseShellExecute = false;
                nca.StartInfo.RedirectStandardOutput = false;
                nca.StartInfo.CreateNoWindow = true;
                //* Start process to extract the main
                nca.Start();
                nca.WaitForExit();
                nca.Close();

                if (ES2 == 0)
                {
                    richTextBox_ES.Text += "\n\n" + "Trying to extract main from " + NCA_file;
                }
                else
                {
                    richTextBox_ES2.Text += "\n\n" + "Trying to extract main from " + NCA_file;
                }


                if (File.Exists("main"))
                {
                    if (ES2 == 0)
                    {
                        richTextBox_ES.Text += "\n" + "Nca main extracted - trying to decrypt it now....";
                    }

                    else
                    {
                        richTextBox_ES2.Text += "\n" + "Nca main extracted - trying to decrypt it now....";
                    }

                    //decrypt extracted main.
                    Process ext_main = new Process();
                    ext_main.StartInfo.FileName = "tools//hactool.exe";
                    ext_main.StartInfo.Arguments = " --disableoutput -t nso --uncompressed=main_dec main";
                    ext_main.StartInfo.UseShellExecute = false;
                    ext_main.StartInfo.RedirectStandardOutput = false;
                    ext_main.StartInfo.CreateNoWindow = true;
                    //* Start process to decrypt
                    ext_main.Start();
                    ext_main.WaitForExit();
                    ext_main.Close();

                    if (File.Exists("main_dec"))
                    {
                        File.Delete("main");
                        if (ES2 == 0)
                        {
                            richTextBox_ES.Text += "\n\n" + "Decryption complete, uneeded file removed: main";
                        }
                        else
                        {
                            richTextBox_ES2.Text += "\n\n" + "Decryption complete, uneeded file removed: main";
                        }
                    }
                }

                if (File.Exists("main.npdm"))
                {
                    File.Delete("main.npdm");
                    if (ES2 == 0)
                    {
                        richTextBox_ES.Text += "\n" + "Uneeded file removed: main.npdm";
                    }
                    else
                    {
                        richTextBox_ES2.Text += "\n" + "Uneeded file removed: main.npdm";
                    }

                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Extract NCA - Error is: " + error.Message);
            }
        }

        private void fwsearch()
        {
            try
            {
                goodArray = new string[] { }; //empty array.

                FolderBrowserDialog folderDlg = new FolderBrowserDialog();
                folderDlg.Description = "Select the folder that contains your switch firmware";
                folderDlg.ShowNewFolderButton = false;
                // Show the FolderBrowserDialog.  
                DialogResult result = folderDlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    cancel = 0;
                    DirectoryInfo firmware_dir = new DirectoryInfo(folderDlg.SelectedPath);

                    FileInfo[] Files = firmware_dir.GetFiles("*.nca");
                    string str = "Files in firm folder:\n";

                    foreach (FileInfo file in Files)
                    {
                        if (file.Length > minimum && file.Length <= maximum && (!file.Name.Contains("cnmt.nca")))
                        {
                            goodArray = goodArray.Append(file.Name).ToArray();
                        }

                        //str = str + file.Name + " - " + file.Length + " bytes\n";
                        //richTextBox1.Text = str;
                    }

                    if (ES2 == 0)
                    {
                        richTextBox_ES.Text += "Possible nca files to check between " + minimum.ToString() + " bytes and " + maximum.ToString() + " bytes: " + goodArray.Length.ToString() + "\n\n";
                    }

                    else
                    {
                        richTextBox_ES2.Text += "Possible nca files to check between " + minimum.ToString() + " bytes and " + maximum.ToString() + " bytes: " + goodArray.Length.ToString() + "\n\n";
                    }

                    //print out files we have in the array.
                    foreach (var item in goodArray)
                    {
                        str = item + "\n";

                        if (ES2 == 0)
                        {
                            richTextBox_ES.Text += str;
                        }
                        else
                        {
                            richTextBox_ES2.Text += str;
                        }
                    }

                    NCA_dir = firmware_dir.ToString().Replace("\\", "//") + "//";
                }

                else
                {
                    cancel = 1;
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void es_find()
        {
            try
            {
                NCA_file = goodArray[0].ToString();
                console_box.Clear();
                //* Create your Process
                Process process = new Process();
                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    process.StartInfo.FileName = "tools//hactoolnet.exe";
                    process.StartInfo.Arguments = " -k tools//keys.dat --disablekeywarns " + '"' + NCA_dir + NCA_file + '"';
                }
                else
                {
                    process.StartInfo.FileName = "tools//hactool.exe";
                    process.StartInfo.Arguments = " --keyset=tools//keys.dat --disableoutput " + '"' + NCA_dir + NCA_file + '"';
                }
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                //* Start process
                process.Start();
                //* Read the other one synchronously
                string output = process.StandardOutput.ReadToEnd();
                console_box.Text += output.Replace(" ", "");
                if (console_box.Text.Contains("TitleID:0100000000000033"))
                {
                    console_box.Text += "found";
                    string nca_path = (NCA_dir + NCA_file).Replace("//", "\\");
                    long length = new System.IO.FileInfo(nca_path).Length;
                    if (ES2 == 0)
                    {
                        richTextBox_ES.Text += "ES title found in " + NCA_file + " (" + length.ToString() + " bytes)\n";
                        richTextBox_ES.Text += "Skipped: " + (goodArray.Count() - 1).ToString() + " files\n";
                    }
                    else
                    {
                        richTextBox_ES2.Text += "ES title found in " + NCA_file + " (" + length.ToString() + " bytes)\n";
                        richTextBox_ES2.Text += "Skipped: " + (goodArray.Count() - 1).ToString() + " files\n";
                    }
                }
                process.WaitForExit();
                process.Close();
            }

            catch (Exception error)
            {
                MessageBox.Show("ES Find - Error is: " + error.Message);
            }
        }

        private void button_es_files_DragDrop(object sender, DragEventArgs e)
        {
            ES2 = 0;
            read_config(); //read the database to make sure the values are updated
            button_es_files.Enabled = false;
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            try
            {
                richTextBox_ES.Clear();
                minimum = ESmin;
                maximum = ESmax;

                sdk = ""; //clear sdk version
                patch_offset = 0; //reset patch offset

                //set up timer
                var watch = new System.Diagnostics.Stopwatch();

                //get the firmware folder, and create an array of nca files to check.
                try
                {
                    //start the timer
                    watch.Start();

                    goodArray = new string[] { }; //empty array.

                    DirectoryInfo firmware_dir = new DirectoryInfo(FW_Path);
                    FileInfo[] Files = firmware_dir.GetFiles("*.nca");
                    string str = "Files in firm folder:\n";

                    foreach (FileInfo file in Files)
                    {
                        if (file.Length > minimum && file.Length <= maximum && (!file.Name.Contains("cnmt.nca")))
                        {
                            goodArray = goodArray.Append(file.Name).ToArray();
                        }

                        //str = str + file.Name + " - " + file.Length + " bytes\n";
                        //richTextBox1.Text = str;
                    }

                    richTextBox_ES.Text += "Possible nca files to check between " + minimum.ToString() + " bytes and " + maximum.ToString() + " bytes: " + goodArray.Length.ToString() + "\n\n";

                    //print out files we have in the array.
                    foreach (var item in goodArray)
                    {
                        str = item + "\n";
                        richTextBox_ES.Text += str;
                    }

                    NCA_dir = firmware_dir.ToString().Replace("\\", "//") + "//";
                }

                catch (Exception error)
                {
                    MessageBox.Show("Error is: " + error.Message);
                }

                richTextBox_ES.Text += "\n";

                //do stuff to the array
                foreach (var item in goodArray)
                {
                    richTextBox_ES.Text += "Files to process: " + goodArray.Length.ToString() + "\n";

                    //use hactool to find the nca we want.
                    es_find();

                    //remove item from array when we are done with it
                    int indexToRemove = 0; //remove item from index 0 in our list.
                    goodArray = goodArray.Where((source, index) => index != indexToRemove).ToArray();

                    if (console_box.Text.Contains("found"))
                    {
                        break;
                    }
                }

                //lets check keys.dat exists or we won't be able to decrypt anything....
                if (!File.Exists("tools/keys.dat"))
                {
                    MessageBox.Show("Keys.dat is missing", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                else
                {
                    //we should now have the nca file name we want to manipulate (NCA_file variable), so we can do the other calls from here.

                    //check the sdk version of firmware.
                    foreach (string line in console_box.Lines)
                    {
                        if (line.Contains("SDKVersion:"))
                        {
                            sdk = line.ToString();
                            richTextBox_ES.Text += "\n" + sdk;
                            break;
                        }
                    }

                    //quit if sdk version is wrong.
                    string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                    int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                    if (SDKVersion < 9300 || SDKVersion == 82990 || SDKVersion == 011290)
                    {
                        richTextBox_ES.ForeColor = Color.Red;
                        richTextBox_ES.Text += "\n" + "Firmware is too old for FS patching, download pre-made patches instead\n\n";
                        MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        button_es_files.Enabled = true;
                        return;
                    }

                    //clear the console box since we don't need it now as we have all the info we need.
                    console_box.Clear();

                    //We should probably put a check here so this code doesn't run if we didn't find the sdk version
                    if (sdk != "")
                    {
                        //extract nca main file
                        Extract_nca();

                        //get the build ID from main_dec - starts at offset 0x40 and is 0x14 long.
                        if (File.Exists("main_dec"))
                        {
                            Get_build();
                        }
                        else
                        {
                            richTextBox_ES.ForeColor = Color.Red;
                            richTextBox_ES.Text = "Unable to decrypt, did you update your keys?\n\nTry adjusting the ES size limits in the config settings if firmware is greater than 15.0.0.";
                            button_es_files.Enabled = true;
                            SystemSounds.Exclamation.Play();
                            return;
                        }

                        //Now we know the build ID and SDK version and unpacked the nca to get the decrypted main file - we can search for offsets...
                        ES_Offset_Search();

                        //We should now know where to patch, so make some folders to store the patch.
                        ES_Patch_Folders();

                        //Everything should now be in place to make a patch
                        ES_Patch_Creation();

                        //clean up decrypted main file how we know the offsets
                        if (checkBox_maindec_es.Checked)
                        {
                            if (File.Exists("main_dec"))
                            {
                                File.Delete("main_dec");
                                richTextBox_ES.Text += "\n\n" + "Decrypted file removed: main_dec";
                            }
                        }

                        richTextBox_ES.Text += "\n\n" + "Finished";
                        SystemSounds.Asterisk.Play();

                    }

                    else
                    {
                        richTextBox_ES.ForeColor = Color.Red;
                        richTextBox_ES.Text += "\n" + "Unable to find the sdk version. Did you select a folder that contains nca files?\n\nPossibly you need to update keys.dat as I can't decrypt the firmware";
                        SystemSounds.Exclamation.Play();
                    }
                }

                //stop the timer
                watch.Stop();
                string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                richTextBox_ES.Text += " - " + ($"time taken: {Time}");
            }

            catch (Exception error)
            {
                string warning = error.Message;
                if (warning == "Input string was not in a correct format.")
                {
                    MessageBox.Show("Did you use the correct keys in keys.dat?", "Can't Decrypt the firmware", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }
            button_es_files.Enabled = true;
        }

        private void button_es_files_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                DragDropEffects effects = DragDropEffects.None;
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                    if (Directory.Exists(path))
                    {
                        FW_Path = path;
                        effects = DragDropEffects.Copy;
                    }

                }

                e.Effect = effects;
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }

        }

        /**************************************************/
        /***************** END OF ES CODE *****************/
        /**************************************************/

        /**************************************************/
        /**************** START OF ES2 CODE ***************/
        /**************************************************/
        private void button_es2_files_Click(object sender, EventArgs e)
        {
            ES2 = 1;
            read_config(); //read the database to make sure the values are updated
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            button_es2_files.Enabled = false;
            try
            {
                killesroutine = 0;
                richTextBox_ES2.Clear();
                minimum = ESmin;
                maximum = ESmax;
                cancel = 0; //reset this value before searching for firmware files

                sdk = ""; //clear sdk version
                patch_offset = 0; //reset patch offset

                //get the firmware folder, and create an array of nca files to check.
                fwsearch();

                //set up timer
                var watch = new System.Diagnostics.Stopwatch();

                //continue running code if the user didn't cancel searching for firmware
                if (cancel == 0)
                {
                    //start timer
                    watch.Start();

                    richTextBox_ES2.Text += "\n";

                    //do stuff to the array
                    foreach (var item in goodArray)
                    {
                        richTextBox_ES2.Text += "Files to process: " + goodArray.Length.ToString() + "\n";

                        //use hactool to find the nca we want.
                        es_find();

                        //remove item from array when we are done with it
                        int indexToRemove = 0; //remove item from index 0 in our list.
                        goodArray = goodArray.Where((source, index) => index != indexToRemove).ToArray();

                        if (console_box.Text.Contains("found"))
                        {
                            break;
                        }
                    }

                    //lets check keys.dat exists or we won't be able to decrypt anything....
                    if (!File.Exists("tools/keys.dat"))
                    {
                        MessageBox.Show("Keys.dat is missing", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    else
                    {
                        //we should now have the nca file name we want to manipulate (NCA_file variable), so we can do the other calls from here.

                        //check the sdk version of firmware.
                        foreach (string line in console_box.Lines)
                        {
                            if (line.Contains("SDKVersion:"))
                            {
                                sdk = line.ToString();
                                richTextBox_ES2.Text += "\n" + sdk;
                                break;
                            }
                        }

                        //quit if sdk version is wrong.
                        string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                        int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                        if (SDKVersion < 9300 || SDKVersion == 82990 || SDKVersion == 011290)
                        {
                            richTextBox_ES2.ForeColor = Color.Red;
                            richTextBox_ES2.Text += "\n" + "Firmware is too old for FS patching, download pre-made patches instead\n\n";
                            MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            button_es2_files.Enabled = true;
                            return;
                        }

                        //clear the console box since we don't need it now as we have all the info we need.
                        console_box.Clear();
                        if (sdk != "")
                        {
                            sdk = sdk.Replace(".", "").Replace("SDKVersion:", "");
                            int sdk_ver = Int32.Parse(sdk);

                            //We should probably put a check here so this code doesn't run if we didn't find the sdk version
                            if (sdk_ver >= 9300 & sdk_ver != 82990 & sdk_ver != 011290)
                            {
                                //extract nca main file
                                Extract_nca();

                                //get the build ID from main_dec - starts at offset 0x40 and is 0x14 long.
                                if (File.Exists("main_dec"))
                                {
                                    Get_build();
                                }
                                else
                                {
                                    richTextBox_ES2.ForeColor = Color.Red;
                                    richTextBox_ES2.Text = "Unable to decrypt, did you update your keys?\n\nTry adjusting the ES size limits in the config settings if firmware is greater than 15.0.0.";
                                    button_es2_files.Enabled = true;
                                    SystemSounds.Exclamation.Play();
                                    return;
                                }

                                //Now we know the build ID and SDK version and unpacked the nca to get the decrypted main file - we can search for offsets...
                                es2_Offset_Search();

                                //Everything should now be in place to make a patch
                                if (killesroutine != 1)
                                {
                                    //We should now know where to patch, so make some folders to store the patch.
                                    ES_Patch_Folders();

                                    //Do some bitshifting...before making the patches.
                                    es2_bitshifting();

                                    //make the patches
                                    ES2_Patch_Creation();
                                }
                                
                                if (killesroutine == 1)
                                {
                                    richTextBox_ES2.Text += "\n\n" + "Not all the patch offsets were not found, no IPS files have been created!";
                                }

                                //clean up decrypted main file how we know the offsets
                                if (checkBox_maindec_es2.Checked)
                                {
                                    if (File.Exists("main_dec"))
                                    {
                                        File.Delete("main_dec");
                                        richTextBox_ES2.Text += "\n\n" + "Decrypted file removed: main_dec";
                                    }
                                }
                                richTextBox_ES2.Text += "\n\n" + "Finished";
                                SystemSounds.Asterisk.Play();
                            }

                            else
                            {
                                if (sdk_ver < 9300 || sdk_ver == 82990 || sdk_ver == 011290)
                                {
                                    richTextBox_ES2.ForeColor = Color.Red;
                                    richTextBox_ES2.Text += "\n" + "Firmware is too old for ES2 patching, download pre-made patches instead\n\n";
                                    MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    SystemSounds.Exclamation.Play();
                                }
                            }
                        }
                        else
                        {
                            richTextBox_ES2.ForeColor = Color.Red;
                            richTextBox_ES2.Text = "Unable to find the sdk version. Did you select a folder that contains nca files?\n\nPossibly you need to update keys.dat as I can't decrypt the firmware";
                            SystemSounds.Exclamation.Play();
                        }
                    }

                    //stop the timer
                    watch.Stop();
                    string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                    richTextBox_ES2.Text += " - " + ($"time taken: {Time}");
                }
            }

            catch (Exception error)
            {
                string warning = error.Message;
                if (warning == "Input string was not in a correct format.")
                {
                    MessageBox.Show("Did you use the correct keys in keys.dat?", "Can't Decrypt the firmware", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }

            button_es2_files.Enabled = true;
        }

        private void es2_Offset_Search()
        {
            try
            {
                //Clear variables.
                int ES2Patch1 = 0;
                int ES2Patch2 = 0;
                int ES2Patch3 = 0;
                ES2_LOC1 = 0;
                ES2_LOC2 = 0;
                ES2_LOC3 = 0;
                richTextBox_ES2.Text += "\n" + "Searching for ES2 ips patches\n";
                int toggle = 0; //add a toggle so we can switch between wildcard searching or specific bytes.
                string find = ""; //variable pattern to use for wildcards.
                string find2 = ""; //variable pattern to use for wildcards.
                //string find3 = ("1F90013128928052").ToLower(); //never changes
                string find3 = ("1F90013128.8052").ToLower(); //new in fw15 patch 1 = 0x64a78

                if (File.Exists("main_dec"))
                {
                    byte[] ByteBuffer = File.ReadAllBytes("main_dec");

                    //Remove the text and periods from the sdk variable
                    string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                    int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                    if (SDKVersion >= 9300 & SDKVersion < 15300)
                    {
                        if (SDKVersion >= 9300 & SDKVersion < 10400)
                        {
                            find = ("f3031faa02000014").ToLower();
                            find2 = ("c07240f9e1930091").ToLower();
                        }

                        else if (SDKVersion >= 10400 & SDKVersion < 11400)
                        {
                            find = ("e0230091..ff97").ToLower();
                            find2 = ("c07240f9e1930091").ToLower();
                        }

                        else if (SDKVersion >= 11400 & SDKVersion < 14300)
                        {
                            find = ("e0230091..ff97").ToLower();
                            find2 = ("c0fdff35a8c3").ToLower();
                        }

                        else if (SDKVersion >= 14300 & SDKVersion < 15300)
                        {
                            find = ("e0230091..ff97f3").ToLower();
                            find2 = ("c0fdff35a8c3").ToLower();
                        }

                        toggle = 0;
                    }

                    else if (SDKVersion >= 15300 & SDKVersion < 16200)
                    {
                        find = ("e0230091.faff97f3").ToLower(); //patch 3 0x64aa8
                        find2 = (".fdff35a8.").ToLower(); //patch 2 0x654a0
                        toggle = 0;
                    }

                    else if (SDKVersion >= 16200 & SDKVersion < 18300)
                    {
                        find = ("E0230091..FF97F303002AE0").ToLower(); //patch 3 0x64aa8
                        find2 = (".fdff35a8.").ToLower(); //patch 2 0x654a0
                        toggle = 0;
                    }

                    else if (SDKVersion >= 18300)
                    {
                        find = ("E0.0091..FF97F303002AE0").ToLower(); //
                        find2 = (".fdff35a8.").ToLower(); //
                        toggle = 0;
                    }

                    //toggle error check here if pattern is empty or not defined yet.
                    if (toggle == 0)
                    {
                        //convert byte array Bytebuffer into a long hex string
                        StringBuilder hex = new StringBuilder(ByteBuffer.Length * 2);

                        foreach (byte b in ByteBuffer)
                            hex.AppendFormat("{0:x2}", b);

                        string str = hex.ToString();
                        find = find.Replace(".", "..");
                        find2 = find2.Replace(".", "..");
                        find3 = find3.Replace(".", "..");

                        Match match = Regex.Match(str, find);
                        if (match.Success)
                        {
                            int index = match.Index;
                            index = index / 2; //make sure we divide by 2 again as we multiplied above...
                            ES2Patch1 = index - 4;
                            richTextBox_ES2.Text += "\n" + "Wildcard search pattern3 found at offset: 0x" + index.ToString("X8");
                            richTextBox_ES2.Text += "\n" + "Possible patch3 offset location: 0x" + ES2Patch1.ToString("X8");
                            ES2_LOC3 = ES2Patch1;

                        }
                        else
                        {
                            richTextBox_ES2.ForeColor = Color.Red;
                            richTextBox_ES2.Text += "\n" + "Hex search pattern 3 was not found :-(";
                            killesroutine = 1;
                        }

                        Match match2 = Regex.Match(str, find2);
                        if (match2.Success)
                        {
                            int index = match2.Index;
                            index = index / 2; //make sure we divide by 2 again as we multiplied above...
                            ES2Patch2 = index - 4;
                            richTextBox_ES2.Text += "\n" + "Wildcard search pattern2 found at offset: 0x" + index.ToString("X8");
                            richTextBox_ES2.Text += "\n" + "Possible patch2 offset location: 0x" + ES2Patch2.ToString("X8");
                            ES2_LOC2 = ES2Patch2;

                        }
                        else
                        {
                            richTextBox_ES2.ForeColor = Color.Red;
                            richTextBox_ES2.Text += "\n" + "Hex search pattern 2 was not found :-(";
                            killesroutine = 1;
                        }

                        //find first instance of pattern and store the index.
                        int amount = 2; //If the search pattern appears more than once, loop to the place we want to find the pattern.
                        var regex = new Regex(find3);
                        int position = 0;
                        string found = "";
                        for (int x = 0; x < amount; x++)
                        {
                            Match match3 = regex.Match(str, position);
                            if (match3.Success)
                            {
                                position = match3.Index + 1;
                                found = "found";
                            }
                        }

                        if (found.Equals("found"))
                        {
                            position /= 2; //make sure we divide by 2 again as we multiplied above...
                            ES2Patch3 = position - 4;
                            richTextBox_ES2.Text += "\n" + "Wildcard search pattern1 found at offset: 0x" + position.ToString("X8");
                            richTextBox_ES2.Text += "\n" + "Possible patch1 offset location: 0x" + ES2Patch3.ToString("X8");
                            ES2_LOC1 = ES2Patch3;
                        }

                        else
                        {
                            richTextBox_ES2.ForeColor = Color.Red;
                            richTextBox_ES2.Text += "\n" + "Hex search pattern 1 was not found :-(";
                            killesroutine = 1;
                        }
                    }
                }
                else
                {
                    richTextBox_ES2.ForeColor = Color.Red;
                    richTextBox_ES2.Text += "\n" + "main_dec was not found, unable to search for patches :-(";
                    killesroutine = 1;
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void es2_bitshifting()
        {
            try
            {
                ES2_revloc = 0; //clear variable
                ES2_revloc3 = 0; //clear variable

                //We only need to bitshift hex for ES2_LOC1 + ES2_LOC3 (ES2_LOC2 just gets a NOP)
                var patch1 = new byte[4];
                var patch3 = new byte[4];

                //get IPS_Patch_Creator start location
                using (BinaryReader reader = new BinaryReader(new FileStream("main_dec", FileMode.Open)))
                {
                    reader.BaseStream.Seek(ES2_LOC1, SeekOrigin.Begin);
                    reader.Read(patch1, 0, 4);

                    //get the size of the IPS_Patch_Creator
                    reader.BaseStream.Seek(ES2_LOC3, SeekOrigin.Begin);
                    reader.Read(patch3, 0, 4);
                }


                //convert bytes to int:
                var first = BitConverter.ToUInt32(patch1, 0);
                var third = BitConverter.ToUInt32(patch3, 0);

                uint rev_first = ReverseBytes(first);
                uint rev_third = ReverseBytes(third);

                //richTextBox_ES2.Text += "\n" + rev_third.ToString("X8");

                string inst = rev_first.ToString("X8");
                UInt32 intValue = UInt32.Parse(inst, System.Globalization.NumberStyles.HexNumber);
                ES2_revloc = CBZ(intValue); //bitshifts 0x20010034 to make it 0x09000014
                byte[] bytes1 = BitConverter.GetBytes(ES2_revloc); //convert above int value into a byte array.
                string hex = BitConverter.ToString(bytes1).Replace("-", ""); //convert byte array to string.

                string inst2 = rev_third.ToString("X8");
                UInt32 intValue2 = UInt32.Parse(inst2, System.Globalization.NumberStyles.HexNumber);
                ES2_revloc3 = CBZ(intValue2); //bitshifts 0x20010034 to make it 0x09000014
                byte[] bytes2 = BitConverter.GetBytes(ES2_revloc3); //convert above int value into a byte array.
                string hex2 = BitConverter.ToString(bytes2).Replace("-", ""); //convert byte array to string.

                richTextBox_ES2.Text += "\n\n" + "Patch 1 = 0x" + hex + " will be written to the ips file";
                richTextBox_ES2.Text += "\n" + "Patch 2 = 0x1F2003D5" + " will be written to the ips file";
                richTextBox_ES2.Text += "\n" + "Patch 3 = 0x" + hex2 + " will be written to the ips file";

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void ES2_Patch_Creation()
        {
            try
            {
                if (ES2_LOC1 > 0)
                {
                    using (var stream = File.Open("atmosphere\\exefs_patches\\es_patches\\" + BuildID + ".ips", FileMode.Create))
                    {
                        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                        {
                            byte[] StartBytes = new byte[] { 0x50, 0x41, 0x54, 0x43, 0x48 };
                            byte[] EndBytes = new byte[] { 0x45, 0x4F, 0x46 };
                            byte[] Patch1 = BitConverter.GetBytes(ES2_revloc);
                            byte[] Patch2 = { 0x1F, 0x20, 0x03, 0xD5 }; //NOP
                            byte[] Patch3 = BitConverter.GetBytes(ES2_revloc3);
                            byte[] Patch_location1 = BitConverter.GetBytes(ES2_LOC1);
                            byte[] Patch_location2 = BitConverter.GetBytes(ES2_LOC2);
                            byte[] Patch_location3 = BitConverter.GetBytes(ES2_LOC3);
                            byte[] PaddingBytes = new byte[] { 0x00, 0x04 };

                            writer.Write(StartBytes);

                            if (ES2_LOC1 > 0 && ES2_LOC1 <= 16777215)
                            {
                                writer.Write(Patch_location1[2]);
                                writer.Write(Patch_location1[1]);
                                writer.Write(Patch_location1[0]);
                            }

                            if (ES2_LOC1 > 16777215)
                            {
                                writer.Write(Patch_location1[3]);
                                writer.Write(Patch_location1[2]);
                                writer.Write(Patch_location1[1]);
                                writer.Write(Patch_location1[0]);
                            }

                            writer.Write(PaddingBytes);
                            //
                            writer.Write(Patch1);
                            //
                            if (ES2_LOC2 > 0 && ES2_LOC2 <= 16777215)
                            {
                                writer.Write(Patch_location2[2]);
                                writer.Write(Patch_location2[1]);
                                writer.Write(Patch_location2[0]);
                            }

                            if (ES2_LOC2 > 16777215)
                            {
                                writer.Write(Patch_location2[3]);
                                writer.Write(Patch_location2[2]);
                                writer.Write(Patch_location2[1]);
                                writer.Write(Patch_location2[0]);
                            }
                            //
                            writer.Write(PaddingBytes);
                            //
                            writer.Write(Patch2);
                            //
                            if (ES2_LOC3 > 0 && ES2_LOC3 <= 16777215)
                            {
                                writer.Write(Patch_location3[2]);
                                writer.Write(Patch_location3[1]);
                                writer.Write(Patch_location3[0]);
                            }

                            if (ES2_LOC3 > 16777215)
                            {
                                writer.Write(Patch_location3[3]);
                                writer.Write(Patch_location3[2]);
                                writer.Write(Patch_location3[1]);
                                writer.Write(Patch_location3[0]);
                            }
                            //
                            writer.Write(PaddingBytes);
                            //
                            writer.Write(Patch3);
                            //
                            writer.Write(EndBytes);

                            richTextBox_ES2.Text += "\n\n" + "ES2 ips patch written";
                        }
                    }
                }

                else
                {
                    richTextBox_ES2.ForeColor = Color.Red;
                    richTextBox_ES2.Text += "\n" + "Couldn't find patch offsets so ES2 ips patch was not written";
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void button_es2_files_DragDrop(object sender, DragEventArgs e)
        {
            ES2 = 1;
            read_config(); //read the database to make sure the values are updated
            button_es2_files.Enabled = false;
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            try
            {
                killesroutine = 0;
                richTextBox_ES2.Clear();
                minimum = ESmin;
                maximum = ESmax;

                sdk = ""; //clear sdk version

                //set up timer
                var watch = new System.Diagnostics.Stopwatch();

                //get the firmware folder, and create an array of nca files to check.
                try
                {
                    //start timer
                    watch.Start();

                    goodArray = new string[] { }; //empty array.

                    DirectoryInfo firmware_dir = new DirectoryInfo(FW_Path);
                    FileInfo[] Files = firmware_dir.GetFiles("*.nca");
                    string str = "Files in firm folder:\n";

                    foreach (FileInfo file in Files)
                    {
                        if (file.Length > minimum && file.Length <= maximum && (!file.Name.Contains("cnmt.nca")))
                        {
                            goodArray = goodArray.Append(file.Name).ToArray();
                        }

                        //str = str + file.Name + " - " + file.Length + " bytes\n";
                        //richTextBox1.Text = str;
                    }

                    richTextBox_ES2.Text += "Possible nca files to check between " + minimum.ToString() + " bytes and " + maximum.ToString() + " bytes: " + goodArray.Length.ToString() + "\n\n";

                    //print out files we have in the array.
                    foreach (var item in goodArray)
                    {
                        str = item + "\n";
                        richTextBox_ES2.Text += str;
                    }

                    NCA_dir = firmware_dir.ToString().Replace("\\", "//") + "//";
                }

                catch (Exception error)
                {
                    MessageBox.Show("Error is: " + error.Message);
                }

                richTextBox_ES2.Text += "\n";

                //do stuff to the array
                foreach (var item in goodArray)
                {
                    richTextBox_ES2.Text += "Files to process: " + goodArray.Length.ToString() + "\n";

                    //use hactool to find the nca we want.
                    es_find();

                    //remove item from array when we are done with it
                    int indexToRemove = 0; //remove item from index 0 in our list.
                    goodArray = goodArray.Where((source, index) => index != indexToRemove).ToArray();

                    if (console_box.Text.Contains("found"))
                    {
                        break;
                    }
                }

                //lets check keys.dat exists or we won't be able to decrypt anything....
                if (!File.Exists("tools/keys.dat"))
                {
                    MessageBox.Show("Keys.dat is missing", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                else
                {
                    //we should now have the nca file name we want to manipulate (NCA_file variable), so we can do the other calls from here.

                    //check the sdk version of firmware.
                    foreach (string line in console_box.Lines)
                    {
                        if (line.Contains("SDKVersion:"))
                        {
                            sdk = line.ToString();
                            richTextBox_ES2.Text += "\n" + sdk;
                            break;
                        }
                    }

                    //quit if sdk version is wrong.
                    string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                    int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                    if (SDKVersion < 9300 || SDKVersion == 82990 || SDKVersion == 011290)
                    {
                        richTextBox_ES2.ForeColor = Color.Red;
                        richTextBox_ES2.Text += "\n" + "Firmware is too old for FS patching, download pre-made patches instead\n\n";
                        MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        button_es2_files.Enabled = true;
                        return;
                    }

                    //clear the console box since we don't need it now as we have all the info we need.
                    console_box.Clear();

                    if (sdk != "")
                    {
                        sdk = sdk.Replace(".", "").Replace("SDKVersion:", "");
                        int sdk_ver = Int32.Parse(sdk);

                        //We should probably put a check here so this code doesn't run if we didn't find the sdk version
                        if (sdk_ver >= 9300 & sdk_ver != 82990 & sdk_ver != 011290)
                        {
                            //extract nca main file
                            Extract_nca();

                            //get the build ID from main_dec - starts at offset 0x40 and is 0x14 long.
                            if (File.Exists("main_dec"))
                            {
                                Get_build();
                            }
                            else
                            {
                                richTextBox_ES2.ForeColor = Color.Red;
                                richTextBox_ES2.Text = "Unable to decrypt, did you update your keys?\n\nTry adjusting the ES size limits in the config settings if firmware is greater than 15.0.0.";
                                button_es2_files.Enabled = true;
                                SystemSounds.Exclamation.Play();
                                return;
                            }

                            //Now we know the build ID and SDK version and unpacked the nca to get the decrypted main file - we can search for offsets...
                            es2_Offset_Search();

                            //Everything should now be in place to make a patch
                            if (killesroutine != 1)
                            {
                                //We should now know where to patch, so make some folders to store the patch.
                                ES_Patch_Folders();

                                //Do some bitshifting...before making the patches.
                                es2_bitshifting();

                                //make the patches
                                ES2_Patch_Creation();
                            }

                            if (killesroutine == 1)
                            {
                                richTextBox_ES2.Text += "\n\n" + "Not all the patch offsets were not found, no IPS files have been created!";
                            }

                            //clean up decrypted main file how we know the offsets
                            if (checkBox_maindec_es2.Checked)
                            {
                                if (File.Exists("main_dec"))
                                {
                                    File.Delete("main_dec");
                                    richTextBox_ES2.Text += "\n\n" + "Decrypted file removed: main_dec";
                                }
                            }
                            richTextBox_ES2.Text += "\n\n" + "Finished";
                            SystemSounds.Asterisk.Play();
                        }

                        else
                        {
                            if (sdk_ver < 9300 || sdk_ver == 82990 || sdk_ver == 011290)
                            {
                                richTextBox_ES2.ForeColor = Color.Red;
                                richTextBox_ES2.Text += "\n" + "Firmware is too old for ES2 patching, download pre-made patches instead\n\n";
                                MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                SystemSounds.Exclamation.Play();
                            }
                        }
                    }
                    else
                    {
                        richTextBox_ES2.ForeColor = Color.Red;
                        richTextBox_ES2.Text = "Unable to find the sdk version. Did you select a folder that contains nca files?\n\nPossibly you need to update keys.dat as I can't decrypt the firmware";
                        SystemSounds.Exclamation.Play();
                    }
                }

                //stop timer
                watch.Stop();
                string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                richTextBox_ES2.Text += " - " + ($"time taken: {Time}");
            }

            catch (Exception error)
            {
                string warning = error.Message;
                if (warning == "Input string was not in a correct format.")
                {
                    MessageBox.Show("Did you use the correct keys in keys.dat?", "Can't Decrypt the firmware", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }
            button_es2_files.Enabled = true;
        }

        private void button_es2_files_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                DragDropEffects effects = DragDropEffects.None;
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                    if (Directory.Exists(path))
                    {
                        FW_Path = path;
                        effects = DragDropEffects.Copy;
                    }

                }

                e.Effect = effects;
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }
        /**************************************************/
        /***************** END OF ES2 CODE ****************/
        /**************************************************/

        /**************************************************/
        /*************** START OF NFIM CODE ***************/
        /**************************************************/

        private void button_nfim_files_Click(object sender, EventArgs e)
        {
            button_nfim_files.Enabled = false;
            read_config(); //read the database to make sure the values are updated
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            try
            {
                killnfim = 0;
                richTextBox_NFIM.Clear();
                minimum = NFIMmin;
                maximum = NFIMmax;

                sdk = ""; //clear sdk version
                cancel = 0; //reset this value before searching for firmware files
                patch_offset = 0; //reset patch offset

                //get the firmware folder, and create an array of nca files to check.
                fwsearch_nfim();

                //set up the timer
                var watch = new System.Diagnostics.Stopwatch();

                //continue running code if the user didn't cancel searching for firmware
                if (cancel == 0)
                {
                    //start the timer
                    watch.Start();

                    richTextBox_NFIM.Text += "\n";

                    //do stuff to the array
                    foreach (var item in goodArray)
                    {
                        richTextBox_NFIM.Text += "Files to process: " + goodArray.Length.ToString() + "\n";

                        //use hactool to find the nca we want.
                        nfim_find();

                        //remove item from array when we are done with it
                        int indexToRemove = 0; //remove item from index 0 in our list.
                        goodArray = goodArray.Where((source, index) => index != indexToRemove).ToArray();

                        if (console_box.Text.Contains("found"))
                        {
                            break;
                        }
                    }

                    if (!console_box.Text.Contains("found"))
                    {
                        killnfim = 1;
                    }

                    if (killnfim != 0)
                    {
                        richTextBox_NFIM.ForeColor = Color.Red;
                        richTextBox_NFIM.Text = "Unable to find the NFIM nca file.\n\nIt's possible that you need to update keys.dat with the latest key to decrypt the file.";
                        SystemSounds.Exclamation.Play();
                        button_nfim_files.Enabled = true;
                        return;
                    }

                    //lets check keys.dat exists or we won't be able to decrypt anything....
                    if (!File.Exists("tools/keys.dat"))
                    {
                        MessageBox.Show("Keys.dat is missing", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    else
                    {
                        //we should now have the nca file name we want to manipulate (NCA_file variable), so we can do the other calls from here.

                        //check the sdk version of firmware.
                        foreach (string line in console_box.Lines)
                        {
                            if (line.Contains("SDKVersion:"))
                            {
                                sdk = line.ToString();
                                richTextBox_NFIM.Text += "\n" + sdk;
                                break;
                            }
                        }

                        //clear the console box since we don't need it now as we have all the info we need.
                        console_box.Clear();

                        //We should probably put a check here so this code doesn't run if we didn't find the sdk version
                        if (sdk != "")
                        {
                            //extract nca main file
                            nfim_Extract_nca();

                            //get the build ID from main_dec - starts at offset 0x40 and is 0x14 long.
                            if (File.Exists("main_dec"))
                            {
                                nfim_Get_build();
                            }
                            else
                            {
                                richTextBox_NFIM.ForeColor = Color.Red;
                                richTextBox_NFIM.Text = "Unable to decrypt, did you update your keys?\n\nTry adjusting the NFIM size limits in the config settings if firmware is greater than 15.0.0.";
                                button_nfim_files.Enabled = true;
                                SystemSounds.Exclamation.Play();
                                return;
                            }

                            //Now we know the build ID and SDK version and unpacked the nca to get the decrypted main file - we can search for offsets...
                            nfim_Offset_Search();

                            //We should now know where to patch, so make some folders to store the patch.
                            nfim_Patch_Folders();

                            //Everything should now be in place to make a patch
                            nfim_Patch_Creation();

                            //clean up decrypted main file how we know the offsets
                            if (checkBox_nfim_cleanmain.Checked)
                            {
                                if (File.Exists("main_dec"))
                                {
                                    File.Delete("main_dec");
                                    richTextBox_NFIM.Text += "\n\n" + "Decrypted file removed: main_dec";
                                }
                            }

                            richTextBox_NFIM.Text += "\n\n" + "Finished";
                            SystemSounds.Asterisk.Play();

                        }

                        else
                        {
                            richTextBox_NFIM.ForeColor = Color.Red;
                            richTextBox_NFIM.Text += "\n" + "Unable to find the sdk version. Did you select a folder that contains nca files?\n\nPossibly you need to update keys.dat as I can't decrypt the firmware";
                            SystemSounds.Exclamation.Play();
                        }
                    }

                    //stop the timer
                    watch.Stop();
                    string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                    richTextBox_NFIM.Text += " - " + ($"time taken: {Time}");
                }
            }

            catch (Exception error)
            {
                string warning = error.Message;
                if (warning == "Input string was not in a correct format.")
                {
                    MessageBox.Show("Did you use the correct keys in keys.dat?", "Can't Decrypt the firmware", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }
            button_nfim_files.Enabled = true;
        }

        private void fwsearch_nfim()
        {
            try
            {
                goodArray = new string[] { }; //empty array.

                FolderBrowserDialog folderDlg = new FolderBrowserDialog();
                folderDlg.Description = "Select the folder that contains your switch firmware";
                folderDlg.ShowNewFolderButton = false;
                // Show the FolderBrowserDialog.  
                DialogResult result = folderDlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    cancel = 0;
                    DirectoryInfo firmware_dir = new DirectoryInfo(folderDlg.SelectedPath);

                    FileInfo[] Files = firmware_dir.GetFiles("*.nca");
                    string str = "Files in firm folder:\n";

                    foreach (FileInfo file in Files)
                    {
                        if (file.Length > minimum && file.Length <= maximum && (!file.Name.Contains("cnmt.nca")))
                        {
                            goodArray = goodArray.Append(file.Name).ToArray();
                        }

                        //str = str + file.Name + " - " + file.Length + " bytes\n";
                        //richTextBox1.Text = str;
                    }

                    richTextBox_NFIM.Text += "Possible nca files to check between " + minimum.ToString() + " bytes and " + maximum.ToString() + " bytes: " + goodArray.Length.ToString() + "\n\n";

                    //print out files we have in the array.
                    foreach (var item in goodArray)
                    {
                        str = item + "\n";
                        richTextBox_NFIM.Text += str;
                    }

                    NCA_dir = firmware_dir.ToString().Replace("\\", "//") + "//";
                }

                else
                {
                    cancel = 1;
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void nfim_find()
        {
            try
            {
                NCA_file = goodArray[0].ToString();
                console_box.Clear();
                //* Create your Process
                Process process = new Process();
                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    process.StartInfo.FileName = "tools//hactoolnet.exe";
                    process.StartInfo.Arguments = " -k tools//keys.dat --disablekeywarns " + '"' + NCA_dir + NCA_file + '"';
                }
                else
                {
                    process.StartInfo.FileName = "tools//hactool.exe";
                    process.StartInfo.Arguments = " --keyset=tools//keys.dat --disableoutput " + '"' + NCA_dir + NCA_file + '"';
                }
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                //* Start process
                process.Start();
                //* Read the other one synchronously
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                process.Close();

                string TID = "";
                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    TID = ("010000000000000f").ToUpper();
                }
                else
                {
                    TID = ("010000000000000f");
                }
                console_box.Text += output.Replace(" ", "");
                if (console_box.Text.Contains("TitleID:" + TID))
                {
                    console_box.Text += "found";
                    string nca_path = (NCA_dir + NCA_file).Replace("//", "\\");
                    long length = new System.IO.FileInfo(nca_path).Length;
                    richTextBox_NFIM.Text += "NFIM title found in " + NCA_file + " (" + length.ToString() + " bytes)\n";
                    richTextBox_NFIM.Text += "Skipped: " + (goodArray.Count() - 1).ToString() + " files\n";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void nfim_Extract_nca()
        {
            try
            {
                Process nca = new Process();
                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    nca.StartInfo.FileName = "tools//hactoolnet.exe";
                    nca.StartInfo.Arguments = " -k tools//keys.dat -t nca --exefsdir .  --disablekeywarns " + '"' + NCA_dir + NCA_file + '"';
                }
                else
                {
                    nca.StartInfo.FileName = "tools//hactool.exe";
                    nca.StartInfo.Arguments = " --keyset=tools//keys.dat --intype=nca --exefsdir=.  --disableoutput " + '"' + NCA_dir + NCA_file + '"';
                }

                nca.StartInfo.UseShellExecute = false;
                nca.StartInfo.RedirectStandardOutput = false;
                nca.StartInfo.CreateNoWindow = true;
                //* Start process to extract the main
                nca.Start();
                nca.WaitForExit();
                nca.Close();

                richTextBox_NFIM.Text += "\n\n" + "Trying to extract main from " + NCA_file;

                if (File.Exists("main"))
                {
                    richTextBox_NFIM.Text += "\n" + "Nca main extracted - trying to decrypt it now....";

                    //decrypt extracted main.
                    Process ext_main = new Process();
                    ext_main.StartInfo.FileName = "tools//hactool.exe";
                    ext_main.StartInfo.Arguments = " --keyset=tools//keys.dat --intype=nso --disableoutput --uncompressed=main_dec main";
                    ext_main.StartInfo.UseShellExecute = false;
                    ext_main.StartInfo.RedirectStandardOutput = false;
                    ext_main.StartInfo.CreateNoWindow = true;
                    //* Start process to decrypt
                    ext_main.Start();
                    ext_main.WaitForExit();
                    ext_main.Close();

                    if (File.Exists("main_dec"))
                    {
                        File.Delete("main");
                        richTextBox_NFIM.Text += "\n\n" + "Decryption complete, uneeded file removed: main";
                    }
                }

                if (File.Exists("main.npdm"))
                {
                    File.Delete("main.npdm");
                    richTextBox_NFIM.Text += "\n" + "Uneeded file removed: main.npdm";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void nfim_Get_build()
        {
            try
            {
                //routine to extract build ID from main_dec.
                BuildID = ""; //clear build id
                richTextBox_NFIM.Text += "\n\n" + "Trying to get the Build ID for the ips patch";
                string StarHex = "0x40";
                string SizeHex = "0x14";
                int start = Convert.ToInt32(StarHex, 16);
                int size = Convert.ToInt32(SizeHex, 16);

                var id = new byte[size];
                using (BinaryReader reader = new BinaryReader(new FileStream("main_dec", FileMode.Open)))
                {
                    reader.BaseStream.Seek(start, SeekOrigin.Begin);
                    reader.Read(id, 0, size);
                }

                string location_hex = BitConverter.ToString(id);
                BuildID = location_hex.Replace("-", "");
                if (BuildID != "")
                {
                    richTextBox_NFIM.Text += "\n" + "Build ID: " + BuildID + "\n";
                }
                else
                {
                    richTextBox_NFIM.ForeColor = Color.Red;
                    richTextBox_NFIM.Text += "\n" + "Unable to find the Build ID...IPS patch will not be created";
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void nfim_Offset_Search()
        {
            try
            {
                byte[] ByteBuffer = File.ReadAllBytes("main_dec");
                int toggle = 0; //add a toggle so we can switch between wildcard searching or specific bytes.
                string find = ""; //variable pattern to use for wildcards.

                //Remove the text and periods from the sdk variable
                string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                if (SDKVersion < 1300 || SDKVersion == 011290)
                {
                    find = ("........FD....................0313AAE003.AA").ToLower();
                    find = find.Replace(".", "..");
                    toggle = 1;
                }

                else if (SDKVersion >= 1300 & SDKVersion < 7300)
                {
                    find = ("........FD...................F30314AAE00314AA9F").ToLower();
                    find = find.Replace(".", "..");
                    toggle = 1;
                }

                else if (SDKVersion >= 7300 & SDKVersion < 12300 || SDKVersion == 82990)
                {
                    find = ("........FD.................FF97F30314AAE00314AA9F").ToLower();
                    find = find.Replace(".", "..");
                    toggle = 1;
                }

                else if (SDKVersion >= 12300 & SDKVersion < 14300) // fw >12.x.x)
                {
                    find = ("FD7BBDA9F50B00F9FD030091F44F02A9F50301AAF40300AA.FBFF97F30314AAE00314AA9F").ToLower();
                    find = find.Replace(".", "..");
                    toggle = 1;
                }

                else if (SDKVersion >= 14300 & SDKVersion < 15300) // fw >12.x.x)
                {
                    if (checkBox_nfim_override.Checked == true)
                    {
                        string nfim_overide = textBox_nfim_override.Text.ToLower();
                        find = (nfim_overide);
                    }
                    else
                    {
                        find = ("........FD.................FF97F30314AAE00314AA9F").ToLower();
                        find = find.Replace(".", "..");
                    }
                    toggle = 1;
                }

                else if (SDKVersion >= 15300) // fw >15.x.x)
                {
                    if (checkBox_nfim_override.Checked == true)
                    {
                        string nfim_overide = textBox_nfim_override.Text.ToLower();
                        find = (nfim_overide);
                    }
                    else
                    {
                        find = ("FD...........................F30314AAE00314AA9F").ToLower();
                        find = find.Replace(".", "..");
                    }
                    toggle = 1;
                }

                //toggle error check here if pattern is empty or not defined yet.
                if (toggle == 1)
                {
                    //convert byte array Bytebuffer into a long hex string
                    StringBuilder hex = new StringBuilder(ByteBuffer.Length * 2);

                    foreach (byte b in ByteBuffer)
                        hex.AppendFormat("{0:x2}", b);

                    string str = hex.ToString();
                    
                    if (checkBox_nfim_override.Checked == true)
                    {
                        find = find.Replace(" ", "");
                    }

                    Match match = Regex.Match(str, find);
                    if (match.Success)
                    {
                        int index = match.Index;
                        index = index / 2; //make sure we divide by 2 again as we multiplied above...
                        patch_offset = index;
                        richTextBox_NFIM.Text += "\n" + "Wildcard search pattern found at offset: 0x" + index.ToString("X8");
                        richTextBox_NFIM.Text += "\n" + "Probable patch offset location: 0x" + patch_offset.ToString("X8");

                    }
                    else
                    {
                        richTextBox_NFIM.ForeColor = Color.Red;
                        richTextBox_NFIM.Text += "\n" + "Hex search pattern was not found :-(";
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void nfim_Patch_Folders()
        {
            try
            {
                if (!Directory.Exists("atmosphere"))
                {
                    Directory.CreateDirectory("atmosphere");
                }

                if (!Directory.Exists("atmosphere\\exefs_patches"))
                {
                    Directory.CreateDirectory("atmosphere\\exefs_patches");
                }

                if (!Directory.Exists("atmosphere\\exefs_patches\\nfim_ctest"))
                {
                    Directory.CreateDirectory("atmosphere\\exefs_patches\\nfim_ctest");
                    richTextBox_NFIM.Text += "\n\n" + "Missing nfim patch directories created";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void nfim_Patch_Creation()
        {
            try
            {
                string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                int SDKVersion = Int32.Parse(strippedsdk);

                if (patch_offset > 0)
                {
                    using (var stream = File.Open("atmosphere\\exefs_patches\\nfim_ctest\\" + BuildID + ".ips", FileMode.Create))
                    {
                        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                        {
                            byte[] StartBytes = new byte[] { 0x50, 0x41, 0x54, 0x43, 0x48 };
                            byte[] EndBytes = new byte[] { 0x45, 0x4F, 0x46 };
                            byte[] Middlebytes = BitConverter.GetBytes(patch_offset);
                            byte[] PatchBytes = new byte[] { 0xE0, 0x03, 0x1F, 0xAA };
                            byte[] PatchBytes2 = new byte[] { 0xC0, 0x03, 0x5F, 0xD6 };
                            byte[] PaddingBytes = new byte[] { 0x00, 0x08 };

                            if (SDKVersion >= 14300) //#fw 14.0.0 or higher patch override code
                            {
                                if (checkBox_nfim_patch_override.Checked == true)
                                {
                                    string hexstring = textBox_nfim_patch1.Text;
                                    uint patch = uint.Parse(hexstring, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch = ReverseBytes(patch);
                                    PatchBytes = BitConverter.GetBytes(patch);

                                    string hexstring2 = textBox_nfim_patch2.Text;
                                    uint patch2 = uint.Parse(hexstring2, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch2 = ReverseBytes(patch2);
                                    PatchBytes2 = BitConverter.GetBytes(patch2);
                                }
                            }

                            writer.Write(StartBytes);

                            //stupid code for testing = also reverse bytes
                            if (patch_offset > 0 && patch_offset <= 16777215)
                            {
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            if (patch_offset > 16777215)
                            {
                                writer.Write(Middlebytes[3]);
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            //end of stupid code
                            writer.Write(PaddingBytes);
                            writer.Write(PatchBytes);
                            writer.Write(PatchBytes2);
                            writer.Write(EndBytes);
                            richTextBox_NFIM.Text += "\n" + "NFIM ips patch written";

                            //richTextBox_NFIM.Text += "\nTEST - " + patch_offset.ToString();
                        }
                    }
                }

                else
                {
                    richTextBox_NFIM.ForeColor = Color.Red;
                    richTextBox_NFIM.Text += "\n" + "Couldn't find patch offsets so NFIM ips patch was not written";
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void button_nfim_files_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                DragDropEffects effects = DragDropEffects.None;
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                    if (Directory.Exists(path))
                    {
                        FW_Path = path;
                        effects = DragDropEffects.Copy;
                    }

                }

                e.Effect = effects;
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void button_nfim_files_DragDrop(object sender, DragEventArgs e)
        {
            button_nfim_files.Enabled = false;
            read_config(); //read the database to make sure the values are updated
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            try
            {
                killnfim = 0;
                richTextBox_NFIM.Clear();
                minimum = NFIMmin;
                maximum = NFIMmax;

                sdk = ""; //clear sdk version
                cancel = 0; //reset this value before searching for firmware files
                patch_offset = 0; //reset patch offset

                //set up the timer
                var watch = new System.Diagnostics.Stopwatch();

                //get the firmware folder, and create an array of nca files to check.
                try
                {
                    //start the timer
                    watch.Start();

                    goodArray = new string[] { }; //empty array.

                    DirectoryInfo firmware_dir = new DirectoryInfo(FW_Path);
                    FileInfo[] Files = firmware_dir.GetFiles("*.nca");
                    string str = "Files in firm folder:\n";

                    foreach (FileInfo file in Files)
                    {
                        if (file.Length > minimum && file.Length <= maximum && (!file.Name.Contains("cnmt.nca")))
                        {
                            goodArray = goodArray.Append(file.Name).ToArray();
                        }

                        //str = str + file.Name + " - " + file.Length + " bytes\n";
                        //richTextBox1.Text = str;
                    }

                    richTextBox_NFIM.Text += "Possible nca files to check between " + minimum.ToString() + " bytes and " + maximum.ToString() + " bytes: " + goodArray.Length.ToString() + "\n\n";

                    //print out files we have in the array.
                    foreach (var item in goodArray)
                    {
                        str = item + "\n";
                        richTextBox_NFIM.Text += str;
                    }

                    NCA_dir = firmware_dir.ToString().Replace("\\", "//") + "//";
                }

                catch (Exception error)
                {
                    MessageBox.Show("Error is: " + error.Message);
                }

                richTextBox_NFIM.Text += "\n";

                //do stuff to the array
                foreach (var item in goodArray)
                {
                    richTextBox_NFIM.Text += "Files to process: " + goodArray.Length.ToString() + "\n";

                    //use hactool to find the nca we want.
                    nfim_find();

                    //remove item from array when we are done with it
                    int indexToRemove = 0; //remove item from index 0 in our list.
                    goodArray = goodArray.Where((source, index) => index != indexToRemove).ToArray();

                    if (console_box.Text.Contains("found"))
                    {
                        break;
                    }
                }

                if (!console_box.Text.Contains("found"))
                {
                    killnfim = 1;
                }

                if (killnfim != 0)
                {
                    richTextBox_NFIM.ForeColor = Color.Red;
                    richTextBox_NFIM.Text = "Unable to find the NFIM nca file.\n\nIt's possible that you need to update keys.dat with the latest key to decrypt the file.";
                    SystemSounds.Exclamation.Play();
                    button_nfim_files.Enabled = true;
                    return;
                }

                //lets check keys.dat exists or we won't be able to decrypt anything....
                if (!File.Exists("tools/keys.dat"))
                {
                    MessageBox.Show("Keys.dat is missing", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                else
                {
                    //we should now have the nca file name we want to manipulate (NCA_file variable), so we can do the other calls from here.

                    //check the sdk version of firmware.
                    foreach (string line in console_box.Lines)
                    {
                        if (line.Contains("SDKVersion:"))
                        {
                            sdk = line.ToString();
                            richTextBox_NFIM.Text += "\n" + sdk;
                            break;
                        }
                    }

                    //clear the console box since we don't need it now as we have all the info we need.
                    console_box.Clear();

                    //We should probably put a check here so this code doesn't run if we didn't find the sdk version
                    if (sdk != "")
                    {
                        //extract nca main file
                        nfim_Extract_nca();

                        //get the build ID from main_dec - starts at offset 0x40 and is 0x14 long.
                        if (File.Exists("main_dec"))
                        {
                            nfim_Get_build();
                        }
                        else
                        {
                            richTextBox_NFIM.ForeColor = Color.Red;
                            richTextBox_NFIM.Text = "Unable to decrypt, did you update your keys?\n\nTry adjusting the NFIM size limits in the config settings if firmware is greater than 15.0.0.";
                            button_nfim_files.Enabled = true;
                            SystemSounds.Exclamation.Play();
                            return;
                        }

                        //Now we know the build ID and SDK version and unpacked the nca to get the decrypted main file - we can search for offsets...
                        nfim_Offset_Search();

                        //We should now know where to patch, so make some folders to store the patch.
                        nfim_Patch_Folders();

                        //Everything should now be in place to make a patch
                        nfim_Patch_Creation();

                        //clean up decrypted main file how we know the offsets
                        if (checkBox_nfim_cleanmain.Checked)
                        {
                            if (File.Exists("main_dec"))
                            {
                                File.Delete("main_dec");
                                richTextBox_NFIM.Text += "\n\n" + "Decrypted file removed: main_dec";
                            }
                        }

                        richTextBox_NFIM.Text += "\n\n" + "Finished";
                        SystemSounds.Asterisk.Play();

                    }

                    else
                    {
                        richTextBox_NFIM.ForeColor = Color.Red;
                        richTextBox_NFIM.Text += "\n" + "Unable to find the sdk version. Did you select a folder that contains nca files?\n\nPossibly you need to update keys.dat as I can't decrypt the firmware";
                        SystemSounds.Exclamation.Play();
                    }
                }

                //stop the timer
                watch.Stop();
                string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                richTextBox_NFIM.Text += " - " + ($"time taken: {Time}");
            }

            catch (Exception error)
            {
                string warning = error.Message;
                if (warning == "Input string was not in a correct format.")
                {
                    MessageBox.Show("Did you use the correct keys in keys.dat?", "Can't Decrypt the firmware", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }

            button_nfim_files.Enabled = true;
        }

        /**************************************************/
        /**************** END OF NFIM CODE ****************/
        /**************************************************/

        /**************************************************/
        /**************** START OF FS CODE ****************/
        /**************************************************/

        private void button_fs_files_Click(object sender, EventArgs e)
        {
            button_fs_files.Enabled = false;
            read_config(); //read the database to make sure the values are updated
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }
            try
            {
                killfsroutine = 0;
                richTextBox_FS.Clear();
                FAT = ""; //Reset if we already used this function because the variable will contain some data
                EXFAT = "";
                sdk = "";
                minimum = FSmin;
                maximum = FSmax;
                cancel = 0; //reset this value before searching for firmware files

                //get the firmware folder, and create an array of nca files to check
                FS_fwsearch();

                //setup timer...
                var watch = new System.Diagnostics.Stopwatch();

                //continue running code if the user didn't cancel searching for firmware
                if (cancel == 0)
                {
                    watch.Start(); //start timer
                    richTextBox_FS.Text += "\n";

                    //reset array search variable so we don't crash!
                    fs_array = 0;
                    foreach (var item in goodArray)
                    {
                        //use hactool to find the fat nca we want.
                        fs_fat_find();

                        if (console_box.Text.Contains("found"))
                        {
                            break;
                        }

                        fs_array++;
                    }

                    if (!checkBox_skipexfat.Checked)
                    {
                        //reset array search variable again, so we don't crash!
                        fs_array = 0;
                        foreach (var item in goodArray)
                        {
                            //use hactool to find the fat nca we want.
                            fs_exfat_find();

                            if (console_box.Text.Contains("found"))
                            {
                                break;
                            }

                            fs_array++;
                        }
                    }

                    //we are now finished with the array so empty it..
                    foreach (var item in goodArray)
                    {
                        int indexToRemove = 0; //remove item from index 0 in our list.
                        goodArray = goodArray.Where((source, index) => index != indexToRemove).ToArray();
                    }


                    //check the sdk version of firmware.
                    foreach (string line in console_box.Lines)
                    {
                        if (line.Contains("SDKVersion:"))
                        {
                            sdk = line.ToString();
                            richTextBox_FS.Text += "\n" + sdk;
                            break;
                        }
                    }

                    //quit if sdk version is wrong.
                    string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                    int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                    if (SDKVersion < 9300 || SDKVersion == 82990 || SDKVersion == 011290)
                    {
                        richTextBox_FS.ForeColor = Color.Red;
                        richTextBox_FS.Text += "\n" + "Firmware is too old for FS patching, download pre-made patches instead\n\n";
                        MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        button_fs_files.Enabled = true;
                        return;
                    }

                    //lets check keys.dat exists or we won't be able to decrypt anything....
                    if (!File.Exists("tools/keys.dat"))
                    {
                        MessageBox.Show("Keys.dat is missing", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    else
                    {
                        //clear the console box since we don't need it now as we have all the info we need.
                        console_box.Clear();

                        //We should probably put a check here so this code doesn't run if we didn't find the files
                        if (FAT != "")
                        {
                            //Show info that the GUI is not broken...
                            richTextBox_FS.Text += "\n\n" + "Please Wait - Extracting files now. This can take a few seconds!";

                            //create directories for extraction and the ips patch.
                            fs_create_folders();

                            if (File.Exists("tools\\hactoolnet.exe"))
                            {
                                //extract the fat nca we found
                                fs_nca_fat_extraction();

                                //extract the exfat nca we found
                                if (!checkBox_skipexfat.Checked)
                                {
                                    fs_nca_exfat_extraction();
                                }
                            }
                            else
                            {
                                //Use hactool batch way to extract nca files as it's faster.
                                fs_batch_extract();
                            }

                            //error check - exit routine if we were unable to decrypt the fs files.
                            if (!File.Exists("FS.kip1-FAT.DEC"))
                            {
                                richTextBox_FS.ForeColor = Color.Red;
                                richTextBox_FS.Text += "\n\nUnable to decrypt the FS files";
                                richTextBox_FS.Text += "\n\nIt's possible that you need to update keys.dat with the latest key to decrypt the files,";
                                richTextBox_FS.Text += " or you may need to adjust the FS NCA settings in the config page.";
                                button_fs_files.Enabled = true;
                                SystemSounds.Exclamation.Play();
                                clean_message = 0;
                                clean();
                                return;
                            }

                            //Get SHA256 values of FS.Kip1 from fat and exfat folder for our IPS patch names.
                            fs_fat_sha();
                            if (!checkBox_skipexfat.Checked)
                            {
                                fs_exfat_sha();
                            }

                            //We are finished with the temp files now so clean up.
                            fs_clean_temp();

                            //Now we have the decrypted file - we can search for offsets...
                            fs_fat_Offset_Search();
                            if (!checkBox_skipexfat.Checked)
                            {
                                fs_exfat_Offset_Search();
                            }

                            //Everything should now be in place to make a patch
                            fs_fat_Patch_Creation();
                            if (!checkBox_skipexfat.Checked)
                            {
                                fs_exfat_Patch_Creation();
                            }

                            if (killfsroutine != 1)
                            {
                                //show patches.ini info
                                richTextBox_FS.Text += "\n\n" + "***************** Info for patches.ini *****************";
                                fs_Fat_Patches_ini();
                                if (!checkBox_skipexfat.Checked)
                                {
                                    fs_ExFat_Patches_ini();
                                }
                                richTextBox_FS.Text += "\n**********************************************************";
                            }

                            //clean up decrypted files how we know the offsets
                            fs_dec_clean();

                            richTextBox_FS.Text += "\n\n" + "Finished";
                            SystemSounds.Asterisk.Play();
                        }

                        else
                        {
                            richTextBox_FS.ForeColor = Color.Red;
                            richTextBox_FS.Text += "\n" + "Unable to find the FS files. Did you select a folder that contains nca files?\n\nPossibly you need to update keys.dat as I can't decrypt the firmware";
                            SystemSounds.Exclamation.Play();
                        }
                    }

                    //clear the timer
                    watch.Stop();
                    string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                    richTextBox_FS.Text += " - " + ($"time taken: {Time}");
                }
            }

            catch (Exception error)
            {
                string warning = error.Message;
                if (warning == "Input string was not in a correct format.")
                {
                    MessageBox.Show("Did you use the correct keys in keys.dat?", "Can't Decrypt the firmware", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }

            button_fs_files.Enabled = true;
        }

        private void FS_fwsearch()
        {
            try
            {
                goodArray = new string[] { }; //empty array.

                FolderBrowserDialog folderDlg = new FolderBrowserDialog();
                folderDlg.Description = "Select the folder that contains your switch firmware";
                folderDlg.ShowNewFolderButton = false;
                // Show the FolderBrowserDialog.  
                DialogResult result = folderDlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    cancel = 0;
                    DirectoryInfo firmware_dir = new DirectoryInfo(folderDlg.SelectedPath);

                    FileInfo[] Files = firmware_dir.GetFiles("*.nca");
                    string str = "Files in firm folder:\n";

                    foreach (FileInfo file in Files)
                    {
                        if (file.Length > minimum && file.Length <= maximum && (!file.Name.Contains("cnmt.nca")))
                        {
                            goodArray = goodArray.Append(file.Name).ToArray();
                        }
                    }

                    richTextBox_FS.Text += "Possible nca files to check between " + minimum.ToString() + " bytes and " + maximum.ToString() + " bytes: " + goodArray.Length.ToString() + "\n\n";

                    //print out files we have in the array.
                    foreach (var item in goodArray)
                    {
                        str = item + "\n";
                        richTextBox_FS.Text += str;
                    }

                    NCA_dir = firmware_dir.ToString().Replace("\\", "//") + "//";
                }

                else
                {
                    cancel = 1;
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void FS_drag_fwsearch()
        {
            try
            {
                goodArray = new string[] { }; //empty array.

                DirectoryInfo firmware_dir = new DirectoryInfo(FW_Path);

                FileInfo[] Files = firmware_dir.GetFiles("*.nca");
                string str = "Files in firm folder:\n";

                foreach (FileInfo file in Files)
                {
                    if (file.Length > minimum && file.Length <= maximum && (!file.Name.Contains("cnmt.nca")))
                    {
                        goodArray = goodArray.Append(file.Name).ToArray();
                    }
                }

                richTextBox_FS.Text += "Possible nca files to check between " + minimum.ToString() + " bytes and " + maximum.ToString() + " bytes: " + goodArray.Length.ToString() + "\n\n";

                //print out files we have in the array.
                foreach (var item in goodArray)
                {
                    str = item + "\n";
                    richTextBox_FS.Text += str;
                }

                NCA_dir = firmware_dir.ToString().Replace("\\", "//") + "//";
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_fat_find()
        {
            try
            {
                NCA_file = goodArray[fs_array].ToString();
                console_box.Clear();
                //* Create your Process
                Process process = new Process();
                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    process.StartInfo.FileName = "tools//hactoolnet.exe";
                    process.StartInfo.Arguments = " -k tools//keys.dat --disablekeywarns " + '"' + NCA_dir + NCA_file + '"';
                }

                else
                {
                    process.StartInfo.FileName = "tools//hactool.exe";
                    process.StartInfo.Arguments = " --keyset=tools//keys.dat --disableoutput " + '"' + NCA_dir + NCA_file + '"';
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                //* Start process
                process.Start();
                //* Read the other one synchronously
                string output = process.StandardOutput.ReadToEnd();
                console_box.Text += output.Replace(" ", "");
                if (console_box.Text.Contains("TitleID:0100000000000819")) //fat
                {
                    console_box.Text += "found";
                    string nca_path = (NCA_dir + NCA_file).Replace("//", "\\");
                    long length = new System.IO.FileInfo(nca_path).Length;
                    richTextBox_FS.Text += "FAT FS title found in " + NCA_file + " (" + length.ToString() + " bytes)\n";
                    FAT = NCA_file; //store the found file in FAT variable
                }
                process.WaitForExit();
                process.Close();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_exfat_find()
        {
            try
            {
                NCA_file = goodArray[fs_array].ToString();
                console_box.Clear();
                //* Create your Process
                Process process = new Process();
                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    process.StartInfo.FileName = "tools//hactoolnet.exe";
                    process.StartInfo.Arguments = " -k tools//keys.dat --disablekeywarns " + '"' + NCA_dir + NCA_file + '"';
                }
                else
                {
                    process.StartInfo.FileName = "tools//hactool.exe";
                    process.StartInfo.Arguments = " --keyset=tools//keys.dat --disableoutput " + '"' + NCA_dir + NCA_file + '"';
                }
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                //* Start process
                process.Start();
                //* Read the other one synchronously
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                process.Close();

                console_box.Text += output.Replace(" ", "");
                string TID = ""; //hactool and hactoolnet display a case difference, so use this variable.

                if (File.Exists("tools\\hactoolnet.exe"))
                {
                    TID = ("010000000000081b").ToUpper(); //Hacktoolnet uses uppercase hex
                }

                else
                {
                    TID = ("010000000000081b"); //hactool uses lowercase hex
                }

                if (console_box.Text.Contains("TitleID:" + TID))
                {
                    console_box.Text += "found";
                    string nca_path = (NCA_dir + NCA_file).Replace("//", "\\");
                    long length = new System.IO.FileInfo(nca_path).Length;
                    richTextBox_FS.Text += "ExFAT FS title found in " + NCA_file + " (" + length.ToString() + " bytes)\n";
                    EXFAT = NCA_file; //store the found file in EXFAT variable
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_create_folders()
        {
            try
            {
                //create ips folders for the ips patch.
                if (!Directory.Exists("atmosphere"))
                {
                    Directory.CreateDirectory("atmosphere");
                }

                if (!Directory.Exists("atmosphere\\kip_patches"))
                {
                    Directory.CreateDirectory("atmosphere\\kip_patches");
                }

                if (!Directory.Exists("atmosphere\\kip_patches\\fs_patches"))
                {
                    Directory.CreateDirectory("atmosphere\\kip_patches\\fs_patches");
                    richTextBox_FS.Text += "\n\n" + "Missing fs patch directories created";
                }

                //create temp folders to exract the fat and exfat nca's into.
                if (!Directory.Exists("Temp"))
                {
                    Directory.CreateDirectory("Temp");
                }
                if (!Directory.Exists("Temp\\Fat"))
                {
                    Directory.CreateDirectory("Temp\\Fat");
                }
                if (!Directory.Exists("Temp\\ExFat"))
                {
                    Directory.CreateDirectory("Temp\\ExFat");
                }

                richTextBox_FS.Text += "\n\n" + "Temporary nca extraction directories created";

                if (checkBox_FS_PatchesINI.Checked)
                {
                    if (!Directory.Exists("bootloader"))
                    {
                        Directory.CreateDirectory("bootloader");
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_nca_fat_extraction()
        {
            try
            {
                richTextBox_FS.Text += "\n\n" + "Trying to extract fat files from " + FAT;

                Process nca = new Process();
                nca.StartInfo.FileName = "tools//hactoolnet.exe";
                nca.StartInfo.Arguments = " -t nca -k tools//keys.dat --section0dir Temp//Fat " + '"' + NCA_dir + FAT + '"';
                nca.StartInfo.UseShellExecute = false;
                nca.StartInfo.RedirectStandardOutput = false;
                nca.StartInfo.CreateNoWindow = true;
                nca.Start();
                nca.WaitForExit();
                nca.Close();

                if (File.Exists("Temp\\Fat\\nx\\package2"))
                {
                    richTextBox_FS.Text += "\n" + "Trying to extract (fat) INI1.bin from package2";
                    Process package2 = new Process();
                    package2.StartInfo.FileName = "tools//hactoolnet.exe";
                    package2.StartInfo.Arguments = " -k tools//keys.dat -t pk21 Temp//Fat//nx//package2 --outdir Temp//Fat//";
                    package2.StartInfo.UseShellExecute = false;
                    package2.StartInfo.RedirectStandardOutput = false;
                    package2.StartInfo.CreateNoWindow = true;
                    package2.Start();
                    package2.WaitForExit();
                    package2.Close();

                    if (File.Exists("Temp\\Fat\\INI1.bin"))
                    {
                        richTextBox_FS.Text += "\n" + "Trying to extract (fat) FS.kip1 from INI1.bin";
                        Process INI1 = new Process();
                        INI1.StartInfo.FileName = "tools//hactoolnet.exe";
                        INI1.StartInfo.Arguments = " -k tools//keys.dat -t ini1 Temp//Fat//INI1.bin --outdir Temp//Fat//";
                        INI1.StartInfo.UseShellExecute = false;
                        INI1.StartInfo.RedirectStandardOutput = false;
                        INI1.StartInfo.CreateNoWindow = true;
                        INI1.Start();
                        INI1.WaitForExit();
                        INI1.Close();

                        if (File.Exists("Temp\\Fat\\FS.kip1"))
                        {
                            richTextBox_FS.Text += "\n" + "Trying to decrypt (fat) FS.kip1";
                            Process Decrypt = new Process();
                            Decrypt.StartInfo.FileName = "tools//hactoolnet.exe";
                            Decrypt.StartInfo.Arguments = " -t kip1 --uncompressed FS.kip1-fat.dec Temp//Fat//FS.kip1";
                            Decrypt.StartInfo.UseShellExecute = false;
                            Decrypt.StartInfo.RedirectStandardOutput = false;
                            Decrypt.StartInfo.CreateNoWindow = true;
                            Decrypt.Start();
                            Decrypt.WaitForExit();
                            Decrypt.Close();

                            if (File.Exists("FS.kip1-fat.dec"))
                            {
                                richTextBox_FS.Text += "\n" + "Successfully decrypted (fat) FS.kip1";
                            }
                        }
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_nca_exfat_extraction()
        {
            try
            {
                richTextBox_FS.Text += "\n\n" + "Trying to extract exfat files from " + EXFAT;

                Process nca = new Process();
                nca.StartInfo.FileName = "tools//hactoolnet.exe";
                nca.StartInfo.Arguments = " -t nca -k tools//keys.dat --section0dir Temp//ExFat " + '"' + NCA_dir + EXFAT + '"';
                nca.StartInfo.UseShellExecute = false;
                nca.StartInfo.RedirectStandardOutput = false;
                nca.StartInfo.CreateNoWindow = true;
                nca.Start();
                nca.WaitForExit();
                nca.Close();

                if (File.Exists("Temp\\ExFat\\nx\\package2"))
                {
                    richTextBox_FS.Text += "\n" + "Trying to extract (exfat) INI1.bin from package2";
                    Process package2 = new Process();
                    package2.StartInfo.FileName = "tools//hactoolnet.exe";
                    package2.StartInfo.Arguments = " -k tools//keys.dat -t pk21 Temp//ExFat//nx//package2 --outdir Temp//ExFat//";
                    package2.StartInfo.UseShellExecute = false;
                    package2.StartInfo.RedirectStandardOutput = false;
                    package2.StartInfo.CreateNoWindow = true;
                    package2.Start();
                    package2.WaitForExit();
                    package2.Close();

                    if (File.Exists("Temp\\ExFat\\INI1.bin"))
                    {
                        richTextBox_FS.Text += "\n" + "Trying to extract (exfat) FS.kip1 from INI1.bin";
                        Process INI1 = new Process();
                        INI1.StartInfo.FileName = "tools//hactoolnet.exe";
                        INI1.StartInfo.Arguments = " -k tools//keys.dat -t ini1 Temp//ExFat//INI1.bin --outdir Temp//ExFat//";
                        INI1.StartInfo.UseShellExecute = false;
                        INI1.StartInfo.RedirectStandardOutput = false;
                        INI1.StartInfo.CreateNoWindow = true;
                        INI1.Start();
                        INI1.WaitForExit();
                        INI1.Close();

                        if (File.Exists("Temp\\ExFat\\FS.kip1"))
                        {
                            richTextBox_FS.Text += "\n" + "Trying to decrypt (exfat) FS.kip1";
                            Process Decrypt = new Process();
                            Decrypt.StartInfo.FileName = "tools//hactoolnet.exe";
                            Decrypt.StartInfo.Arguments = " -t kip1 --uncompressed FS.kip1-exfat.dec Temp//ExFat//FS.kip1";
                            Decrypt.StartInfo.UseShellExecute = false;
                            Decrypt.StartInfo.RedirectStandardOutput = false;
                            Decrypt.StartInfo.CreateNoWindow = true;
                            Decrypt.Start();
                            Decrypt.WaitForExit();
                            Decrypt.Close();

                            if (File.Exists("FS.kip1-exfat.dec"))
                            {
                                richTextBox_FS.Text += "\n" + "Successfully decrypted (exfat) FS.kip1";
                            }
                        }
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_fat_sha()
        {
            try
            {
                //clear names from the variable first
                FATSHA = "";
                if (File.Exists("Temp\\Fat\\FS.kip1"))
                {
                    using (SHA256 mySHA256 = SHA256.Create())
                    {
                        using (BinaryReader read = new BinaryReader(new FileStream("Temp\\Fat\\FS.kip1", FileMode.Open)))
                        {
                            read.BaseStream.Position = 0;
                            byte[] hashValue = mySHA256.ComputeHash(read.BaseStream);
                            FATSHA = String.Join("", hashValue.Select(x => x.ToString("X2")));
                            richTextBox_FS.Text += "\n" + "Fat Sha256: " + FATSHA;
                            //note - we should name our ips patch with the shaValue from above.
                        }
                    }
                }
                else
                {
                    richTextBox_FS.ForeColor = Color.Red;
                    richTextBox_FS.Text += "\n\n" + "unable to find Temp/Fat/FS.kip1";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_exfat_sha()
        {
            try
            {
                //clear names from the variable first
                EXFATSHA = "";
                if (File.Exists("Temp\\ExFat\\FS.kip1"))
                {
                    using (SHA256 mySHA256 = SHA256.Create())
                    {
                        using (BinaryReader read = new BinaryReader(new FileStream("Temp\\ExFat\\FS.kip1", FileMode.Open)))
                        {
                            read.BaseStream.Position = 0;
                            byte[] hashValue = mySHA256.ComputeHash(read.BaseStream);
                            EXFATSHA = String.Join("", hashValue.Select(x => x.ToString("X2")));
                            richTextBox_FS.Text += "\n" + "Exfat Sha256: " + EXFATSHA;
                            //note - we should name our ips patch with the shaValue from above.
                        }
                    }
                }
                else
                {
                    richTextBox_FS.ForeColor = Color.Red;
                    richTextBox_FS.Text += "\n\n" + "Unable to find Temp/ExFat/FS.kip1";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_clean_temp()
        {
            try
            {
                Directory.Delete("Temp", true);
                richTextBox_FS.Text += "\n\n" + "Temp files cleaned up";
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_dec_clean()
        {
            try
            {
                if (checkBox_FS_clean.Checked)
                {
                    if (File.Exists("FS.kip1-fat.dec"))
                    {
                        File.Delete("FS.kip1-fat.dec");
                    }
                    if (File.Exists("FS.kip1-exfat.dec"))
                    {
                        File.Delete("FS.kip1-exfat.dec");
                    }
                    richTextBox_FS.Text += "\n\n" + "Decrypted files cleaned up";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_fat_Offset_Search()
        {
            try
            {
                //Clear patch offsets.
                FATPatch1 = 0;
                FATPatch2 = 0;
                richTextBox_FS.Text += "\n\n" + "Searching for fat ips patches";

                if (File.Exists("FS.kip1-fat.dec"))
                {
                    //byte[] ByteBuffer = File.ReadAllBytes("FS.kip1-fat.dec").Skip(0).Take(1000).ToArray(); //only read the first 1000.
                    byte[] ByteBuffer = File.ReadAllBytes("FS.kip1-fat.dec").Skip(0).ToArray(); //skips to position to start reading from.
                    int toggle = 0; //add a toggle so we can switch between wildcard searching or specific bytes.
                    string find = ""; //variable pattern to use for wildcards.
                    string find2 = ""; //variable pattern to use for wildcards.

                    //Remove the text and periods from the sdk variable
                    string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                    int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                    if (SDKVersion < 9300 || SDKVersion == 82990 || SDKVersion == 011290)
                    {
                        toggle = 1;
                    }

                    else if (SDKVersion >= 9300 & SDKVersion < 14300)
                    {
                        find = ("1e42b91fc14271").ToLower();
                        find2 = (".94081C00121F05007181000054").ToLower();
                        find = find.Replace(".", "..");
                        find2 = find2.Replace(".", "..");
                        toggle = 0;
                    }

                    else if (SDKVersion >= 14300 & SDKVersion < 15300)
                    {
                        if (checkBox_fs_override.Checked == true)
                        {
                            string fs1 = textBox_fs1_override.Text.ToLower();
                            string fs2 = textBox_fs2_overide.Text.ToLower();
                            find = (fs1);
                            find2 = (fs2);
                        }
                        else
                        {
                            find = ("1e42b91fc14271").ToLower();
                            find2 = (".94081C00121F05007181000054").ToLower();
                            find = find.Replace(".", "..");
                            find2 = find2.Replace(".", "..");
                        }
                        toggle = 0;
                    }

                    else if (SDKVersion >= 15300 & SDKVersion < 16200)
                    {
                        if (checkBox_fs_override.Checked == true)
                        {
                            string fs1 = textBox_fs1_override.Text.ToLower();
                            string fs2 = textBox_fs2_overide.Text.ToLower();
                            find = (fs1);
                            find2 = (fs2);
                        }
                        else
                        {
                            find = ("9408...1F05.....54").ToLower(); //1C00121F0500714101
                            find2 = ("003688...1F").ToLower(); //0036883E
                            find = find.Replace(".", "..");
                            find2 = find2.Replace(".", "..");
                        }
                        toggle = 0;
                    }

                    else if (SDKVersion >= 16200 & SDKVersion < 17500)
                    {
                        if (checkBox_fs_override.Checked == true)
                        {
                            string fs1 = textBox_fs1_override.Text.ToLower();
                            string fs2 = textBox_fs2_overide.Text.ToLower();
                            find = (fs1);
                            find2 = (fs2);
                        }
                        else
                        {
                            find = ("94081C00121F050071.10.0054").ToLower(); //1C00121F0500714101
                            find2 = ("0036........1F....71....0054..1E48391F..0071").ToLower(); //0036883E
                            //find = find.Replace(".", "..");
                            //find2 = find2.Replace(".", "..");
                        }
                        toggle = 0;
                    }

                    else if (SDKVersion >= 17500)
                    {
                        if (checkBox_fs_override.Checked == true)
                        {
                            string fs1 = textBox_fs1_override.Text.ToLower();
                            string fs2 = textBox_fs2_overide.Text.ToLower();
                            find = (fs1);
                            find2 = (fs2);
                        }
                        else
                        {
                            find = ("94081C00121F050071.10.0054").ToLower(); //1C00121F0500714101
                            find2 = ("4010003602258052e0030191").ToLower(); //0036883E
                            //find = find.Replace(".", "..");
                            //find2 = find2.Replace(".", "..");
                        }
                        toggle = 0;
                    }

                    //toggle error check here if pattern is empty or not defined yet.
                    if (toggle == 0)
                    {
                        //convert byte array Bytebuffer into a long hex string
                        StringBuilder hex = new StringBuilder(ByteBuffer.Length * 2);

                        foreach (byte b in ByteBuffer)
                            hex.AppendFormat("{0:x2}", b);

                        string str = hex.ToString();
                        
                        if (checkBox_fs_override.Checked == true)
                        {
                            find = find.Replace(" ", "");
                            find2 = find2.Replace(" ", "");
                        }

                        Match match = Regex.Match(str, find);
                        if (match.Success)
                        {
                            int index = match.Index;
                            index = index / 2; //make sure we divide by 2 again as we multiplied above...
                            
                            if (SDKVersion < 15300)
                            {
                                if (checkBox_fs_override.Checked == true)
                                {
                                    FATPatch1 = index;
                                }
                                else
                                {
                                    FATPatch1 = index - 5;
                                }
                            }
                            else if (SDKVersion >= 15300 & SDKVersion < 17500)
                            {
                                if (checkBox_fs_override.Checked == true)
                                {
                                    FATPatch1 = index;
                                }
                                else
                                {
                                    FATPatch1 = index - 3;
                                }
                            }
                            
                            else if (SDKVersion >= 17500)
                            {
                                if (checkBox_fs_override.Checked == true)
                                {
                                    FATPatch1 = index;
                                }
                                else
                                {
                                    FATPatch1 = index -3;
                                }
                            }

                            richTextBox_FS.Text += "\n" + "Wildcard search pattern found at offset: 0x" + index.ToString("X8");
                            richTextBox_FS.Text += "\n" + "Probable patch1 offset location: 0x" + FATPatch1.ToString("X8");
                        }
                        else
                        {
                            richTextBox_FS.ForeColor = Color.Red;
                            richTextBox_FS.Text += "\n" + "Hex search pattern 1 was not found :-(";
                        }

                        Match match2 = Regex.Match(str, find2);
                        if (match2.Success)
                        {
                            int index = match2.Index;
                            index = index / 2; //make sure we divide by 2 again as we multiplied above...

                            if (SDKVersion < 17500)
                            {
                                if (checkBox_fs_override.Checked == true)
                                {
                                    FATPatch2 = index;
                                }
                                else
                                {
                                    FATPatch2 = index - 2;
                                }
                            }

                            else if (SDKVersion >= 17500)
                            {
                                if (checkBox_fs_override.Checked == true)
                                {
                                    FATPatch2 = index;
                                }
                                else
                                {
                                    FATPatch2 = index;
                                }
                            }




                            richTextBox_FS.Text += "\n" + "Wildcard search pattern2 found at offset: 0x" + index.ToString("X8");
                            richTextBox_FS.Text += "\n" + "Probable patch2 offset location: 0x" + FATPatch2.ToString("X8");

                        }
                        else
                        {
                            richTextBox_FS.ForeColor = Color.Red;
                            richTextBox_FS.Text += "\n" + "Hex search pattern 2 was not found :-(";
                        }
                    }

                    if (toggle == 1)
                    {
                        richTextBox_FS.Text += "\n" + "Firmware is too old for FS patching, download pre-made patches instead\n\n";
                        MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                else
                {
                    richTextBox_FS.ForeColor = Color.Red;
                    richTextBox_FS.Text += "\n" + "FS.kip1-fat.dec was not found, unable to search for patches :-(";
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_exfat_Offset_Search()
        {
            try
            {
                //Clear patch offsets.
                ExFatPatch1 = 0;
                ExFatPatch2 = 0;
                richTextBox_FS.Text += "\n\n" + "Searching for exfat ips patches";

                if (File.Exists("FS.kip1-exfat.dec"))
                {
                    byte[] ByteBuffer = File.ReadAllBytes("FS.kip1-exfat.dec");
                    int toggle = 0; //add a toggle so we can switch between wildcard searching or specific bytes.
                    string find = ""; //variable pattern to use for wildcards.
                    string find2 = ""; //variable pattern to use for wildcards.

                    //Remove the text and periods from the sdk variable
                    string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                    int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                    if (SDKVersion < 9300 || SDKVersion == 82990 || SDKVersion == 011290)
                    {
                        toggle = 1;
                    }

                    else if (SDKVersion >= 9300 & SDKVersion < 14300)
                    {

                        find = ("1e42b91fc14271").ToLower();
                        find2 = (".94081C00121F05007181000054").ToLower();
                        find = find.Replace(".", "..");
                        find2 = find2.Replace(".", "..");
                        toggle = 0;
                    }

                    else if (SDKVersion >= 14300 & SDKVersion < 15300)
                    {
                        if (checkBox_fs_override.Checked == true)
                        {
                            string fs1 = textBox_fs1_override.Text.ToLower();
                            string fs2 = textBox_fs2_overide.Text.ToLower();
                            find = (fs1);
                            find2 = (fs2);
                        }
                        else
                        {
                            find = ("1e42b91fc14271").ToLower();
                            find2 = (".94081C00121F05007181000054").ToLower();
                            find = find.Replace(".", "..");
                            find2 = find2.Replace(".", "..");
                        }
                        toggle = 0;
                    }

                    else if (SDKVersion >= 15300 & SDKVersion < 16200)
                    {
                        if (checkBox_fs_override.Checked == true)
                        {
                            string fs1 = textBox_fs1_override.Text.ToLower();
                            string fs2 = textBox_fs2_overide.Text.ToLower();
                            find = (fs1);
                            find2 = (fs2);
                        }
                        else
                        {
                            find = ("9408...1F05.....54").ToLower(); //1C00121F0500714101
                            find2 = ("003688...1F").ToLower(); //0036883E
                            find = find.Replace(".", "..");
                            find2 = find2.Replace(".", "..");
                        }
                        toggle = 0;
                    }

                    else if (SDKVersion >= 16200 & SDKVersion < 17500)
                    {
                        if (checkBox_fs_override.Checked == true)
                        {
                            string fs1 = textBox_fs1_override.Text.ToLower();
                            string fs2 = textBox_fs2_overide.Text.ToLower();
                            find = (fs1);
                            find2 = (fs2);
                        }
                        else
                        {
                            find = ("94081C00121F050071.10.0054").ToLower(); //1C00121F0500714101
                            find2 = ("0036........1F....71....0054..1E48391F..0071").ToLower(); //0036883E
                            //find = find.Replace(".", "..");
                            //find2 = find2.Replace(".", "..");
                        }
                        toggle = 0;
                    }

                    else if (SDKVersion >= 17500)
                    {
                        if (checkBox_fs_override.Checked == true)
                        {
                            string fs1 = textBox_fs1_override.Text.ToLower();
                            string fs2 = textBox_fs2_overide.Text.ToLower();
                            find = (fs1);
                            find2 = (fs2);
                        }
                        else
                        {
                            find = ("94081C00121F050071.10.0054").ToLower(); //1C00121F0500714101
                            find2 = ("4010003602258052e0030191").ToLower(); //0036883E
                            //find = find.Replace(".", "..");
                            //find2 = find2.Replace(".", "..");
                        }
                        toggle = 0;
                    }



                    //toggle error check here if pattern is empty or not defined yet.
                    if (toggle == 0)
                    {
                        //convert byte array Bytebuffer into a long hex string
                        StringBuilder hex = new StringBuilder(ByteBuffer.Length * 2);

                        foreach (byte b in ByteBuffer)
                            hex.AppendFormat("{0:x2}", b);

                        string str = hex.ToString();

                        if (checkBox_fs_override.Checked == true)
                        {
                            find = find.Replace(" ", "");
                            find2 = find2.Replace(" ", "");
                        }
                        
                        Match match = Regex.Match(str, find);
                        if (match.Success)
                        {
                            int index = match.Index;
                            index = index / 2; //make sure we divide by 2 again as we multiplied above...
                            if (SDKVersion < 15300)
                            {
                                if (checkBox_fs_override.Checked == true)
                                {
                                    ExFatPatch1 = index;
                                }
                                else
                                {
                                    ExFatPatch1 = index - 5;
                                }
                            }
                            else if (SDKVersion >= 15300)
                            {
                                if (checkBox_fs_override.Checked == true)
                                {
                                    ExFatPatch1 = index;
                                }
                                else
                                {
                                    ExFatPatch1 = index - 3;
                                }
                            }
                            richTextBox_FS.Text += "\n" + "Wildcard search pattern found at offset: 0x" + index.ToString("X8");
                            richTextBox_FS.Text += "\n" + "Probable patch1 offset location: 0x" + ExFatPatch1.ToString("X8");

                        }
                        else
                        {
                            richTextBox_FS.ForeColor = Color.Red;
                            richTextBox_FS.Text += "\n" + "Hex search pattern 1 was not found :-(";
                        }

                        Match match2 = Regex.Match(str, find2);
                        if (match2.Success)
                        {
                            int index = match2.Index;
                            index = index / 2; //make sure we divide by 2 again as we multiplied above...

                            if (SDKVersion < 17500)
                            {

                                if (checkBox_fs_override.Checked == true)
                                {
                                    ExFatPatch2 = index;
                                }
                                else
                                {
                                    ExFatPatch2 = index - 2;
                                }
                            }

                            else if (SDKVersion >= 17500)
                            {
                                if (checkBox_fs_override.Checked == true)
                                {
                                    ExFatPatch2 = index;
                                }
                                else
                                {
                                    ExFatPatch2 = index;
                                }
                            }

                            richTextBox_FS.Text += "\n" + "Wildcard search pattern2 found at offset: 0x" + index.ToString("X8");
                            richTextBox_FS.Text += "\n" + "Probable patch2 offset location: 0x" + ExFatPatch2.ToString("X8");

                        }
                        else
                        {
                            richTextBox_FS.ForeColor = Color.Red;
                            richTextBox_FS.Text += "\n" + "Hex search pattern 2 was not found :-(";
                        }
                    }

                    if (toggle == 1)
                    {
                        richTextBox_FS.Text += "\n" + "Firmware is too old for FS patching, download pre-made patches instead\n\n";
                        return;
                    }
                }
                else
                {
                    richTextBox_FS.ForeColor = Color.Red;
                    richTextBox_FS.Text += "\n" + "FS.kip1-exfat.dec was not found, unable to search for patches :-(";
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_fat_Patch_Creation()
        {
            try
            {
                string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                int SDKVersion = Int32.Parse(strippedsdk);

                if (FATPatch1 > 0)
                {
                    using (var stream = File.Open("atmosphere\\kip_patches\\fs_patches\\" + FATSHA + ".ips", FileMode.Create))
                    {
                        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                        {
                            byte[] StartBytes = new byte[] { 0x50, 0x41, 0x54, 0x43, 0x48 };
                            byte[] EndBytes = new byte[] { 0x45, 0x4F, 0x46 };
                            byte[] Middlebytes = BitConverter.GetBytes(FATPatch1);
                            byte[] Middlebytes2 = BitConverter.GetBytes(FATPatch2);
                            byte[] PatchBytes = new byte[] { 0x1F, 0x20, 0x03, 0xD5 };
                            byte[] PatchBytes2 = new byte[] { 0xE0, 0x03, 0x1F, 0x2A };
                            byte[] PaddingBytes = new byte[] { 0x00, 0x04 };

                            if (SDKVersion < 15300) //#fw 14.0.0 or higher patch override code
                            {
                                if (checkBox_fs_patch_override.Checked == true)
                                {
                                    string hexstring = textBox_fs_patch1.Text;
                                    uint patch = uint.Parse(hexstring, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch = ReverseBytes(patch);
                                    PatchBytes = BitConverter.GetBytes(patch);

                                    string hexstring2 = textBox_fs_patch2.Text;
                                    uint patch2 = uint.Parse(hexstring2, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch2 = ReverseBytes(patch2);
                                    PatchBytes2 = BitConverter.GetBytes(patch2);
                                }
                            }

                            else if (SDKVersion >= 15300 & SDKVersion < 17500) //#fw 15.0.0 or higher patch override code
                            {
                                if (checkBox_fs_patch_override.Checked == true)
                                {
                                    string hexstring = textBox_fs_patch1.Text;
                                    uint patch = uint.Parse(hexstring, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch = ReverseBytes(patch);
                                    PatchBytes2 = BitConverter.GetBytes(patch);

                                    string hexstring2 = textBox_fs_patch2.Text;
                                    uint patch2 = uint.Parse(hexstring2, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch2 = ReverseBytes(patch2);
                                    PatchBytes = BitConverter.GetBytes(patch2);
                                }
                            }

                            else if (SDKVersion >= 17500) //#fw 15.0.0 or higher patch override code
                            {
                                if (checkBox_fs_patch_override.Checked == true)
                                {
                                    string hexstring = textBox_fs_patch1.Text;
                                    uint patch = uint.Parse(hexstring, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch = ReverseBytes(patch);
                                    PatchBytes2 = BitConverter.GetBytes(patch);

                                    string hexstring2 = textBox_fs_patch2.Text;
                                    uint patch2 = uint.Parse(hexstring2, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch2 = ReverseBytes(patch2);
                                    PatchBytes = BitConverter.GetBytes(patch2);
                                }
                            }

                            writer.Write(StartBytes);

                            //stupid code for testing = also reverse bytes
                            if (FATPatch1 > 0 && FATPatch1 <= 16777215)
                            {
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            if (FATPatch1 > 16777215)
                            {
                                writer.Write(Middlebytes[3]);
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            writer.Write(PaddingBytes);
                            
                            if (SDKVersion < 15300) //#fw 14.0.0 or higher patch override code
                            {
                                writer.Write(PatchBytes);
                            }
                            else if (SDKVersion >= 15300 & SDKVersion < 17500) //fw 15.0.0 - swap patches over
                            {
                                writer.Write(PatchBytes2);
                            }

                            else if (SDKVersion >= 17500)
                            {
                                writer.Write(PatchBytes2);
                            }

                            if (FATPatch2 > 0 && FATPatch2 <= 16777215)
                            {
                                writer.Write(Middlebytes2[2]);
                                writer.Write(Middlebytes2[1]);
                                writer.Write(Middlebytes2[0]);
                            }

                            if (FATPatch2 > 16777215)
                            {
                                writer.Write(Middlebytes2[3]);
                                writer.Write(Middlebytes2[2]);
                                writer.Write(Middlebytes2[1]);
                                writer.Write(Middlebytes2[0]);
                            }

                            writer.Write(PaddingBytes);
                            
                            if (SDKVersion < 15300) //#fw 14.0.0 or higher patch override code
                            {
                                writer.Write(PatchBytes2);
                            }
                            else if (SDKVersion >= 15300 & SDKVersion < 17500) //fw 15.0.0 - swap patches over
                            {
                                writer.Write(PatchBytes);
                            }

                            else if (SDKVersion >= 17500)
                            {
                                writer.Write(PatchBytes);
                            }

                            writer.Write(EndBytes);
                            richTextBox_FS.Text += "\n\n" + "FS fat ips patch written";

                            //richTextBox_NFIM.Text += "\nTEST - " + patch_offset.ToString();
                        }
                    }
                }

                else
                {
                    richTextBox_FS.ForeColor = Color.Red;
                    richTextBox_FS.Text += "\n\n" + "Couldn't find patch offsets so FS fat ips patch was not written";
                    killfsroutine = 1;
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_exfat_Patch_Creation()
        {
            try
            {
                string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                int SDKVersion = Int32.Parse(strippedsdk);

                if (ExFatPatch1 > 0)
                {
                    using (var stream = File.Open("atmosphere\\kip_patches\\fs_patches\\" + EXFATSHA + ".ips", FileMode.Create))
                    {
                        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                        {
                            byte[] StartBytes = new byte[] { 0x50, 0x41, 0x54, 0x43, 0x48 };
                            byte[] EndBytes = new byte[] { 0x45, 0x4F, 0x46 };
                            byte[] Middlebytes = BitConverter.GetBytes(ExFatPatch1);
                            byte[] Middlebytes2 = BitConverter.GetBytes(ExFatPatch2);
                            byte[] PatchBytes = new byte[] { 0x1F, 0x20, 0x03, 0xD5 };
                            byte[] PatchBytes2 = new byte[] { 0xE0, 0x03, 0x1F, 0x2A };
                            byte[] PaddingBytes = new byte[] { 0x00, 0x04 };

                            if (SDKVersion < 15300) //#fw 14.0.0 or higher patch override code
                            {
                                if (checkBox_fs_patch_override.Checked == true)
                                {
                                    string hexstring = textBox_fs_patch1.Text;
                                    uint patch = uint.Parse(hexstring, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch = ReverseBytes(patch);
                                    PatchBytes = BitConverter.GetBytes(patch);

                                    string hexstring2 = textBox_fs_patch2.Text;
                                    uint patch2 = uint.Parse(hexstring2, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch2 = ReverseBytes(patch2);
                                    PatchBytes2 = BitConverter.GetBytes(patch2);
                                }
                            }

                            else if (SDKVersion >= 15300 & SDKVersion < 17500)
                            {
                                if (checkBox_fs_patch_override.Checked == true)
                                {
                                    string hexstring = textBox_fs_patch1.Text;
                                    uint patch = uint.Parse(hexstring, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch = ReverseBytes(patch);
                                    PatchBytes2 = BitConverter.GetBytes(patch);

                                    string hexstring2 = textBox_fs_patch2.Text;
                                    uint patch2 = uint.Parse(hexstring2, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch2 = ReverseBytes(patch2);
                                    PatchBytes = BitConverter.GetBytes(patch2);
                                }
                            }

                            else if (SDKVersion >= 17500) //#fw 17.0 - changed again....!
                            {
                                if (checkBox_fs_patch_override.Checked == true)
                                {
                                    string hexstring = textBox_fs_patch1.Text;
                                    uint patch = uint.Parse(hexstring, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch = ReverseBytes(patch);
                                    PatchBytes2 = BitConverter.GetBytes(patch);

                                    string hexstring2 = textBox_fs_patch2.Text;
                                    uint patch2 = uint.Parse(hexstring2, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    patch2 = ReverseBytes(patch2);
                                    PatchBytes = BitConverter.GetBytes(patch2);
                                }
                            }

                            writer.Write(StartBytes);

                            //stupid code for testing = also reverse bytes
                            if (ExFatPatch1 > 0 && ExFatPatch1 <= 16777215)
                            {
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            if (ExFatPatch1 > 16777215)
                            {
                                writer.Write(Middlebytes[3]);
                                writer.Write(Middlebytes[2]);
                                writer.Write(Middlebytes[1]);
                                writer.Write(Middlebytes[0]);
                            }

                            writer.Write(PaddingBytes);
                            
                            if (SDKVersion < 15300) //#fw 14.0.0 or higher patch override code
                            {
                                writer.Write(PatchBytes);
                            }
                            else if (SDKVersion >= 15300 & SDKVersion < 17500) //fw 15.0.0 - swap patches over
                            {
                                writer.Write(PatchBytes2);
                            }
                            else if (SDKVersion >= 17500) //fw 15.0.0 - swap patches over
                            {
                                writer.Write(PatchBytes2);
                            }

                            if (ExFatPatch2 > 0 && ExFatPatch2 <= 16777215)
                            {
                                writer.Write(Middlebytes2[2]);
                                writer.Write(Middlebytes2[1]);
                                writer.Write(Middlebytes2[0]);
                            }

                            if (ExFatPatch2 > 16777215)
                            {
                                writer.Write(Middlebytes2[3]);
                                writer.Write(Middlebytes2[2]);
                                writer.Write(Middlebytes2[1]);
                                writer.Write(Middlebytes2[0]);
                            }

                            writer.Write(PaddingBytes);
                            
                            if (SDKVersion < 15300) //#fw 14.0.0 or higher patch override code
                            {
                                writer.Write(PatchBytes2);
                            }
                            
                            else if (SDKVersion >= 15300 & SDKVersion < 17500) //fw 15.0.0 - swap patches over
                            {
                                writer.Write(PatchBytes);
                            }

                            else if (SDKVersion >= 17500)
                            {
                                writer.Write(PatchBytes);
                            }
                            writer.Write(EndBytes);
                            richTextBox_FS.Text += "\n" + "FS exfat ips patch written";
                        }
                    }
                }

                else
                {
                    richTextBox_FS.ForeColor = Color.Red;
                    richTextBox_FS.Text += "\n" + "Couldn't find patch offsets so FS exfat ips patch was not written";
                    killfsroutine = 1;
                }

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void button_fs_files_DragDrop(object sender, DragEventArgs e)
        {
            button_fs_files.Enabled = false;
            read_config(); //read the database to make sure the values are updated
            if (theme == 0)
            {
                green_and_black();
            }
            if (theme == 1)
            {
                black_and_white();
            }

            try
            {
                killfsroutine = 0;
                richTextBox_FS.Clear();
                FAT = ""; //Reset if we already used this function because the variable will contain some data
                EXFAT = "";
                sdk = "";
                minimum = FSmin;
                maximum = FSmax;
                cancel = 0; //reset this value before searching for firmware files

                //get the firmware folder, and create an array of nca files to check
                FS_drag_fwsearch();

                //Set up timer
                var watch = new System.Diagnostics.Stopwatch();

                //continue running code if the user didn't cancel searching for firmware
                if (cancel == 0)
                {
                    //start the timer
                    watch.Start();

                    richTextBox_FS.Text += "\n";

                    //reset array search variable so we don't crash!
                    fs_array = 0;
                    foreach (var item in goodArray)
                    {
                        //use hactool to find the fat nca we want.
                        fs_fat_find();

                        if (console_box.Text.Contains("found"))
                        {
                            break;
                        }

                        fs_array++;
                    }

                    //if skip searching for exfat if checkbox is not checked
                    if (!checkBox_skipexfat.Checked)
                    {
                        //reset array search variable again, so we don't crash!
                        fs_array = 0;
                        foreach (var item in goodArray)
                        {
                            //use hactool to find the fat nca we want.
                            fs_exfat_find();

                            if (console_box.Text.Contains("found"))
                            {
                                break;
                            }

                            fs_array++;
                        }
                    }

                    //we are now finished with the array so empty it..
                    foreach (var item in goodArray)
                    {
                        int indexToRemove = 0; //remove item from index 0 in our list.
                        goodArray = goodArray.Where((source, index) => index != indexToRemove).ToArray();
                    }


                    //check the sdk version of firmware.
                    foreach (string line in console_box.Lines)
                    {
                        if (line.Contains("SDKVersion:"))
                        {
                            sdk = line.ToString();
                            richTextBox_FS.Text += "\n" + sdk;
                            break;
                        }
                    }

                    //quit if sdk version is wrong.
                    string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                    int SDKVersion = Int32.Parse(strippedsdk); //convert sdk to int

                    if (SDKVersion < 9300 || SDKVersion == 82990 || SDKVersion == 011290)
                    {
                        richTextBox_FS.ForeColor = Color.Red;
                        richTextBox_FS.Text += "\n" + "Firmware is too old for FS patching, download pre-made patches instead\n\n";
                        MessageBox.Show("Only SDK versions 9.3.0.0 and above are supported by this tool. Download  pre-made sig patches.", "Firmware Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        button_fs_files.Enabled = true;
                        return;
                    }

                    //lets check keys.dat exists or we won't be able to decrypt anything....
                    if (!File.Exists("tools/keys.dat"))
                    {
                        MessageBox.Show("Keys.dat is missing", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    else
                    {
                        //clear the console box since we don't need it now as we have all the info we need.
                        console_box.Clear();

                        //We should probably put a check here so this code doesn't run if we didn't find the files
                        if (FAT != "")
                        {
                            //Show info that the GUI is not broken...
                            richTextBox_FS.Text += "\n\n" + "Please Wait - Extracting files now. This can take a few seconds!";

                            //create directories for extraction and the ips patch.
                            fs_create_folders();

                            if (File.Exists("tools\\hactoolnet.exe"))
                            {
                                //extract the fat nca we found
                                fs_nca_fat_extraction();

                                //extract the exfat nca we found
                                if (!checkBox_skipexfat.Checked)
                                {
                                    fs_nca_exfat_extraction();
                                }
                            }
                            else
                            {
                                //Use hactool batch way to extract nca files as it's faster.
                                fs_batch_extract();
                            }

                            //error check - exit routine if we were unable to decrypt the fs files.
                            if (!File.Exists("FS.kip1-FAT.DEC"))
                            {
                                richTextBox_FS.ForeColor = Color.Red;
                                richTextBox_FS.Text += "\n\nUnable to decrypt the FS files";
                                richTextBox_FS.Text += "\n\nIt's possible that you need to update keys.dat with the latest key to decrypt the files,";
                                richTextBox_FS.Text += " or you may need to adjust the FS NCA settings in the config page.";
                                button_fs_files.Enabled = true;
                                SystemSounds.Exclamation.Play();
                                clean_message = 0;
                                clean();
                                return;
                            }

                            //Get SHA256 values of FS.Kip1 from fat and exfat folder for our IPS patch names.
                            fs_fat_sha();
                            if (!checkBox_skipexfat.Checked)
                            {
                                fs_exfat_sha();
                            }

                            //We are finished with the temp files now so clean up.
                            fs_clean_temp();

                            //Now we have the decrypted file - we can search for offsets...
                            fs_fat_Offset_Search();
                            if (!checkBox_skipexfat.Checked)
                            {
                                fs_exfat_Offset_Search();
                            }

                            //Everything should now be in place to make a patch
                            fs_fat_Patch_Creation();
                            if (!checkBox_skipexfat.Checked)
                            {
                                fs_exfat_Patch_Creation();
                            }

                            if (killfsroutine != 1)
                            {
                                //show patches.ini info
                                richTextBox_FS.Text += "\n\n" + "***************** Info for patches.ini *****************";
                                fs_Fat_Patches_ini();
                                if (!checkBox_skipexfat.Checked)
                                {
                                    fs_ExFat_Patches_ini();
                                }
                                richTextBox_FS.Text += "\n**********************************************************";
                            }

                            //clean up decrypted files how we know the offsets
                            fs_dec_clean();

                            richTextBox_FS.Text += "\n\n" + "Finished";
                            SystemSounds.Asterisk.Play();
                        }

                        else
                        {
                            richTextBox_FS.ForeColor = Color.Red;
                            richTextBox_FS.Text += "\n" + "Unable to find the FS files. Did you select a folder that contains nca files?\n\nPossibly you need to update keys.dat as I can't decrypt the firmware";
                            SystemSounds.Exclamation.Play();
                        }
                    }

                    //Stop the timer
                    watch.Stop();
                    string Time = string.Format("{0} seconds", TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds.ToString());
                    richTextBox_FS.Text += " - " + ($"time taken: {Time}");
                }
            }

            catch (Exception error)
            {
                string warning = error.Message;
                if (warning == "Input string was not in a correct format.")
                {
                    MessageBox.Show("Did you use the correct keys in keys.dat?", "Can't Decrypt the firmware", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }

            button_fs_files.Enabled = true;
        }

        private void button_fs_files_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                DragDropEffects effects = DragDropEffects.None;
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                    if (Directory.Exists(path))
                    {
                        FW_Path = path;
                        effects = DragDropEffects.Copy;
                    }

                }

                e.Effect = effects;
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_batch_extract()
        {
            try
            {
                //create a batch file for extracting and decrypting kip1
                string batch = "run.bat";
                string path = (NCA_dir + FAT).Replace("//", "\\");
                string path2 = (NCA_dir + EXFAT).Replace("//", "\\");

                // Create the file, or overwrite if the file exists.
                using (StreamWriter sw = File.CreateText(batch))
                {
                    // Add some information to the file.
                    sw.Write("tools\\hactool -t nca --keyset=tools\\keys.dat --section0dir=Temp\\Fat " + '"' + path + '"' + " >NUL");
                    sw.WriteLine("\n" + "tools\\hactool.exe --keyset=tools\\keys.dat -t pk21 Temp\\Fat\\nx\\package2 --outdir=Temp\\Fat\\ >NUL");
                    sw.WriteLine("\n" + "tools\\hactool.exe --keyset=tools\\keys.dat -t ini1 Temp\\Fat\\INI1.bin --outdir=Temp\\Fat\\ >NUL");
                    sw.WriteLine("\n" + "tools\\hactool.exe --intype=kip1 --uncompressed=FS.kip1-FAT.DEC Temp\\Fat\\FS.kip1 >NUL");

                    sw.WriteLine("\n" + "tools\\hactool -t nca --keyset=tools\\keys.dat --section0dir=Temp\\ExFat " + '"' + path2 + '"' + " >NUL");
                    sw.WriteLine("\n" + "tools\\hactool.exe --keyset=tools\\keys.dat -t pk21 Temp\\ExFat\\nx\\package2 --outdir=Temp\\ExFat\\ >NUL");
                    sw.WriteLine("\n" + "tools\\hactool.exe --keyset=tools\\keys.dat -t ini1 Temp\\ExFat\\INI1.bin --outdir=Temp\\ExFat\\ >NUL");
                    sw.WriteLine("\n" + "tools\\hactool.exe --intype=kip1 --uncompressed=FS.kip1-ExFAT.DEC Temp\\ExFat\\FS.kip1 >NUL");
                }

                Process nca = new Process();
                nca.StartInfo.FileName = batch;
                nca.StartInfo.Arguments = " >NUL";
                nca.StartInfo.UseShellExecute = false;
                nca.StartInfo.RedirectStandardOutput = false;
                nca.StartInfo.CreateNoWindow = true;
                nca.Start();
                nca.WaitForExit();
                nca.Close();

                if (File.Exists("FS.kip1-FAT.DEC"))
                {
                    richTextBox_FS.Text += "\n\n" + "Successfully extracted files from " + FAT;
                }

                if (File.Exists("FS.kip1-ExFAT.DEC"))
                {
                    richTextBox_FS.Text += "\n" + "Successfully extracted files from " + EXFAT + "\n";
                }

                if (File.Exists(batch))
                {
                    File.Delete(batch);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_Fat_Patches_ini()
        {
            try
            {
                string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                int SDKVersion = Int32.Parse(strippedsdk);

                //code for patches.ini to grab the instruction to patch.
                var patch1 = new byte[4];
                var patch2 = new byte[4];

                using (BinaryReader reader = new BinaryReader(new FileStream("FS.kip1-fat.dec", FileMode.Open)))
                {
                    reader.BaseStream.Seek(FATPatch1, SeekOrigin.Begin);
                    reader.Read(patch1, 0, 4);
                    reader.BaseStream.Seek(FATPatch2, SeekOrigin.Begin);
                    reader.Read(patch2, 0, 4);
                }

                var first = BitConverter.ToUInt32(patch1, 0);
                uint rev_first = ReverseBytes(first);
                string inst = rev_first.ToString("X8");
                Fat_patches_ini1 = inst;

                var second = BitConverter.ToUInt32(patch2, 0);
                uint rev_second = ReverseBytes(second);
                string inst2 = rev_second.ToString("X8");
                Fat_patches_ini2 = inst2;

                //start writing info to the text box.

                int location = FATPatch1 - 256;
                int location2 = FATPatch2 - 256;
                string hexval = location.ToString("X2");
                string hexval2 = location2.ToString("X2");
                string shaval = FATSHA.Substring(0, 16);
                richTextBox_FS.Text += "\n" + "#FAT - " + sdk;
                richTextBox_FS.Text += "\n" + "[FS:" + shaval + "]";
                
                if (SDKVersion < 15300) //#fw 14.0.0 or higher patch override code
                {
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval + ":0x4:" + Fat_patches_ini1 + ",1F2003D5";
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval2 + ":0x4:" + Fat_patches_ini2 + ",E0031F2A";
                }
                else if (SDKVersion >= 15300 & SDKVersion < 17500)
                {
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval + ":0x4:" + Fat_patches_ini1 + ",E0031F2A";
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval2 + ":0x4:" + Fat_patches_ini2 + ",1F2003D5";
                }

                else if (SDKVersion >= 17500)
                {
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval + ":0x4:" + Fat_patches_ini1 + ",E0031F2A";
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval2 + ":0x4:" + Fat_patches_ini2 + ",1F2003D5";
                }



                string inipath = "bootloader\\patches.ini";
                if (checkBox_FS_PatchesINI.Checked)
                {
                    if (!File.Exists(inipath))
                    {
                        var stream = File.Open("bootloader\\patches.ini", FileMode.Create);
                        stream.Close();
                    }
                    using (StreamWriter sw = File.AppendText(inipath))
                    {
                        sw.WriteLine("#FAT - " + sdk);
                        sw.WriteLine("[FS:" + shaval + "]");
                        
                        if (SDKVersion < 15300)
                        {
                            sw.WriteLine(".nosigchk=0:0x" + hexval + ":0x4:" + Fat_patches_ini1 + ",1F2003D5");
                            sw.WriteLine(".nosigchk=0:0x" + hexval2 + ":0x4:" + Fat_patches_ini2 + ",E0031F2A");
                        }
                        else if (SDKVersion >= 15300 & SDKVersion < 17500)
                        {
                            sw.WriteLine(".nosigchk=0:0x" + hexval + ":0x4:" + Fat_patches_ini1 + ",E0031F2A");
                            sw.WriteLine(".nosigchk=0:0x" + hexval2 + ":0x4:" + Fat_patches_ini2 + ",1F2003D5");
                        }
                        else if (SDKVersion >= 17500)
                        {
                            sw.WriteLine(".nosigchk=0:0x" + hexval + ":0x4:" + Fat_patches_ini1 + ",E0031F2A");
                            sw.WriteLine(".nosigchk=0:0x" + hexval2 + ":0x4:" + Fat_patches_ini2 + ",1F2003D5");
                        }

                        sw.Close();
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void fs_ExFat_Patches_ini()
        {
            try
            {
                string strippedsdk = sdk.Replace("SDKVersion:", "").Replace(".", "");
                int SDKVersion = Int32.Parse(strippedsdk);

                //code for patches.ini to grab the instruction to patch.
                var patch1 = new byte[4];
                var patch2 = new byte[4];

                using (BinaryReader reader = new BinaryReader(new FileStream("FS.kip1-Exfat.dec", FileMode.Open)))
                {
                    reader.BaseStream.Seek(ExFatPatch1, SeekOrigin.Begin);
                    reader.Read(patch1, 0, 4);
                    reader.BaseStream.Seek(ExFatPatch2, SeekOrigin.Begin);
                    reader.Read(patch2, 0, 4);
                }

                var first = BitConverter.ToUInt32(patch1, 0);
                uint rev_first = ReverseBytes(first);
                string inst = rev_first.ToString("X8");
                ExFat_patches_ini1 = inst;

                var second = BitConverter.ToUInt32(patch2, 0);
                uint rev_second = ReverseBytes(second);
                string inst2 = rev_second.ToString("X8");
                ExFat_patches_ini2 = inst2;

                //start writing info to the text box.

                int location = ExFatPatch1 - 256;
                int location2 = ExFatPatch2 - 256;
                string hexval = location.ToString("X2");
                string hexval2 = location2.ToString("X2");
                string shaval = EXFATSHA.Substring(0, 16);
                richTextBox_FS.Text += "\n\n" + "#ExFAT - " + sdk;
                richTextBox_FS.Text += "\n" + "[FS:" + shaval + "]";
                
                if (SDKVersion < 15300)
                {
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval + ":0x4:" + ExFat_patches_ini1 + ",1F2003D5";
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval2 + ":0x4:" + ExFat_patches_ini2 + ",E0031F2A";
                }
                else if (SDKVersion >= 15300 & SDKVersion < 17500)
                {
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval + ":0x4:" + ExFat_patches_ini1 + ",E0031F2A";
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval2 + ":0x4:" + ExFat_patches_ini2 + ",1F2003D5";
                }
                else if (SDKVersion >= 17500)
                {
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval + ":0x4:" + ExFat_patches_ini1 + ",E0031F2A";
                    richTextBox_FS.Text += "\n" + ".nosigchk=0:0x" + hexval2 + ":0x4:" + ExFat_patches_ini2 + ",1F2003D5";
                }

                string inipath = "bootloader\\patches.ini";
                if (checkBox_FS_PatchesINI.Checked)
                {
                    if (!File.Exists(inipath))
                    {
                        var stream = File.Open("bootloader\\patches.ini", FileMode.Create);
                        stream.Close();
                    }
                    using (StreamWriter sw = File.AppendText(inipath))
                    {
                        sw.WriteLine("\n" + "#ExFAT - " + sdk);
                        sw.WriteLine("[FS:" + shaval + "]");
                        
                        if (SDKVersion < 15300)
                        {
                            sw.WriteLine(".nosigchk=0:0x" + hexval + ":0x4:" + ExFat_patches_ini1 + ",1F2003D5");
                            sw.WriteLine(".nosigchk=0:0x" + hexval2 + ":0x4:" + ExFat_patches_ini2 + ",E0031F2A");
                        }

                        else if (SDKVersion >= 15300 & SDKVersion < 17500)
                        {
                            sw.WriteLine(".nosigchk=0:0x" + hexval + ":0x4:" + ExFat_patches_ini1 + ",E0031F2A");
                            sw.WriteLine(".nosigchk=0:0x" + hexval2 + ":0x4:" + ExFat_patches_ini2 + ",1F2003D5");
                        }

                        else if (SDKVersion >= 17500)
                        {
                            sw.WriteLine(".nosigchk=0:0x" + hexval + ":0x4:" + ExFat_patches_ini1 + ",E0031F2A");
                            sw.WriteLine(".nosigchk=0:0x" + hexval2 + ":0x4:" + ExFat_patches_ini2 + ",1F2003D5");
                        }

                        sw.WriteLine();
                        sw.Close();
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        /**************************************************/
        /***************** END OF FS CODE *****************/
        /**************************************************/

        /**************************************************/
        /*************** START OF KEYS CODE ***************/
        /**************************************************/

        private void button_reload_keys_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists("tools/keys.dat"))
                {
                    MessageBox.Show("tools/keys.dat does not exist");
                }
                else
                {
                    string filetext = File.ReadAllText("tools/keys.dat");
                    richTextBox_keys.Text = filetext;
                }
            }
            catch
            {
                MessageBox.Show("Unable to show keys");
            }
        }

        private void button_sort_keys_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists("tools/keys.dat"))
                {
                    MessageBox.Show("tools/keys.dat does not exist");
                }
                else
                {
                    richTextBox_keys.Clear();
                    string inFile = "tools/keys.dat";
                    var contents = File.ReadAllLines(inFile);
                    Array.Sort(contents);
                    for (int i = 0; i < contents.Length;)
                    {
                        richTextBox_keys.Text += contents[i];
                        richTextBox_keys.Text += ("\n"); //add a newline to our array
                        i++;
                    }
                }
            }
            catch
            {
                MessageBox.Show("Unable to sort keys");
            }
        }

        private void button_write_keys_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("You are about to overwrite the contents of tools/keys.dat with the content show in this page.\n\nPress Yes to do this or No to preserve your current tools/keys.dat file.", "IPS Patch Creator", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    string RichTextBoxContents = richTextBox_keys.Text;
                    File.WriteAllText("tools/keys.dat", RichTextBoxContents);
                }
            }
            catch
            {
                MessageBox.Show("Unable to write to tools/keys.dat");
            }
        }

        private void make_key_template()
        {
            try
            {
                //generate a key template
                string path = @"tools\keys.dat";
                // This text is added only once to the file.
                if (!File.Exists(path))
                {
                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine("aes_kek_generation_source = ");
                        sw.WriteLine("aes_key_generation_source = ");
                        sw.WriteLine("header_key = ");
                        sw.WriteLine("key_area_key_application_00 = ");
                        sw.WriteLine("key_area_key_application_01 = ");
                        sw.WriteLine("key_area_key_application_02 = ");
                        sw.WriteLine("key_area_key_application_03 = ");
                        sw.WriteLine("key_area_key_application_04 = ");
                        sw.WriteLine("key_area_key_application_05 = ");
                        sw.WriteLine("key_area_key_application_06 = ");
                        sw.WriteLine("key_area_key_application_07 = ");
                        sw.WriteLine("key_area_key_application_08 = ");
                        sw.WriteLine("key_area_key_application_09 = ");
                        sw.WriteLine("key_area_key_application_0a = ");
                        sw.WriteLine("key_area_key_application_0b = ");
                        sw.WriteLine("key_area_key_application_0c = ");
                        sw.WriteLine("key_area_key_application_0d = ");
                        sw.WriteLine("key_area_key_application_0e = ");
                        sw.WriteLine("key_area_key_application_0f = ");
                        sw.WriteLine("package1_key_00 = ");
                        sw.WriteLine("package1_key_01 = ");
                        sw.WriteLine("package1_key_02 = ");
                        sw.WriteLine("package1_key_03 = ");
                        sw.WriteLine("package1_key_04 = ");
                        sw.WriteLine("package1_key_05 = ");
                        sw.WriteLine("package2_key_00 = ");
                        sw.WriteLine("package2_key_01 = ");
                        sw.WriteLine("package2_key_02 = ");
                        sw.WriteLine("package2_key_03 = ");
                        sw.WriteLine("package2_key_04 = ");
                        sw.WriteLine("package2_key_05 = ");
                        sw.WriteLine("package2_key_06 = ");
                        sw.WriteLine("package2_key_07 = ");
                        sw.WriteLine("package2_key_08 = ");
                        sw.WriteLine("package2_key_09 = ");
                        sw.WriteLine("package2_key_0a = ");
                        sw.WriteLine("package2_key_0b = ");
                        sw.WriteLine("package2_key_0c = ");
                        sw.WriteLine("package2_key_0d = ");
                        sw.WriteLine("package2_key_0e = ");
                        sw.WriteLine("package2_key_0f = ");
                    }
                }
                string filetext = File.ReadAllText("tools/keys.dat");
                richTextBox_keys.Text = filetext;
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        /**************************************************/
        /**************** END OF KEYS CODE ****************/
        /**************************************************/

        /**************************************************/
        /************** START OF BASE64 CODE **************/
        /**************************************************/

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private void button_decode_Click(object sender, EventArgs e)
        {
            try
            {
                string encoded = richTextBox_Base64.Text;
                string decoded = Base64Decode(encoded);
                richTextBox_Base64.Text = decoded;
            }

            catch
            {
                MessageBox.Show("Unable to Decode string", "Not a base64 encoded string", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            }
        }

        private void button_encode_Click(object sender, EventArgs e)
        {
            try
            {
                string decoded = richTextBox_Base64.Text;
                string encoded = Base64Encode(decoded);
                richTextBox_Base64.Text = encoded;
            }

            catch
            {
                MessageBox.Show("Unable to Encode string");
            }
        }

        private void button_clear_Click(object sender, EventArgs e)
        {
            try
            {
                richTextBox_Base64.Clear();
                richTextBox_Base64.Update();
            }

            catch
            {
                MessageBox.Show("Unable to clear");
            }
        }

        private void copyToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            richTextBox_Base64.Copy();
        }

        private void pasteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //richTextBox_Base64.Text = "";
            richTextBox_Base64.Text += Clipboard.GetText();
        }

        private void richTextBox_Base64_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        /**************************************************/
        /*************** END OF BASE64 CODE ***************/
        /**************************************************/

        //open the users default web browser if they click a url inside the program info richtext box
        private void richTextBox_info_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        //read items from the config database
        private void read_config()
        {
            try
            {
                //Set size limits for nca files to process - set default values if config file is missing
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);
                cmd.CommandText = "SELECT * from FS ORDER BY min COLLATE NOCASE ASC";

                SQLiteDataReader sqReader = cmd.ExecuteReader();
                while (sqReader.Read())
                {
                    string min = (sqReader.GetString(0));
                    string max = (sqReader.GetString(1));
                    FSmin = Int32.Parse(min);
                    FSmax = Int32.Parse(max);
                }
                sqReader.Close();

                //get data from database ES tables to populate the text box's
                cmd.CommandText = "SELECT * from ES ORDER BY min COLLATE NOCASE ASC";
                SQLiteDataReader sqReader2 = cmd.ExecuteReader();
                while (sqReader2.Read())
                {
                    string min = (sqReader2.GetString(0));
                    string max = (sqReader2.GetString(1));
                    ESmin = Int32.Parse(min);
                    ESmax = Int32.Parse(max);
                }
                sqReader2.Close();

                //get data from database NFIM tables to populate the text box's
                cmd.CommandText = "SELECT * from NFIM ORDER BY min COLLATE NOCASE ASC";
                SQLiteDataReader sqReader3 = cmd.ExecuteReader();
                while (sqReader3.Read())
                {
                    string min = (sqReader3.GetString(0));
                    string max = (sqReader3.GetString(1));
                    NFIMmin = Int32.Parse(min);
                    NFIMmax = Int32.Parse(max);
                }
                sqReader3.Close();

                //get data for the theme
                cmd.CommandText = "SELECT * from Theme ORDER BY value COLLATE NOCASE ASC";
                SQLiteDataReader sqReader4 = cmd.ExecuteReader();
                while (sqReader4.Read())
                {
                    string val = (sqReader4.GetString(0));
                    theme = Int32.Parse(val);
                }
                sqReader4.Close();

                //close the database;
                con.Close();
                con.Dispose();

                setcheckbox(); //now read the checkbox state and set those..

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        //read web link items from the weblinks database
        private void comboBox1_additems()
        {
            try
            {
                //check database file exists first
                if (!File.Exists("tools/links.db"))
                {
                    //best we create a database then or we will get a crash
                    linkdatabase_make_tables();
                }

                //connect to database
                using var con = new SQLiteConnection(linkdatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con)
                {
                    //CommandText = "SELECT * from links"
                    CommandText = "SELECT * from links ORDER BY name COLLATE NOCASE ASC"
                };

                SQLiteDataReader sqReader = cmd.ExecuteReader();

                //Build a list
                var dataSource = new List<Language>();

                while (sqReader.Read())
                {
                    string listitems = (sqReader.GetString(1)); //1 for names 2 for url
                    string listurl = (sqReader.GetString(2)); //1 for names 2 for url
                    dataSource.Add(new Language() { LinkName = listitems, LinkValue = listurl });
                }

                //Setup data binding
                this.comboBox1.DataSource = dataSource;
                this.comboBox1.DisplayMember = "LinkName";
                this.comboBox1.ValueMember = "LinkValue";

                // make it readonly
                this.comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;

                con.Close(); //disconnet from database
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void linkdatabase_make_tables()
        {
            try
            {
                using var con = new SQLiteConnection(linkdatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                //create the empty tables
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS links(id INTEGER, name Text, url Text, about Text)";
                cmd.ExecuteNonQuery();

                //populate empty tables with default values
                cmd.CommandText = "INSERT INTO links(id, name, url, about) VALUES(@id, @name, @url, @about)";
                cmd.Parameters.AddWithValue("@id", 1);
                cmd.Parameters.AddWithValue("@name", "Google");
                cmd.Parameters.AddWithValue("@url", "http://www.google.com");
                cmd.Parameters.AddWithValue("@about", "Google is a search engine");
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                con.Close(); //close the database...

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        //open users default web browser when they select an item from the combobox
        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string myval = (string)comboBox1.SelectedValue;
            System.Diagnostics.Process.Start(myval);
        }

        //refresh the weblinks in the combobox.
        private void comboBox1_MouseHover(object sender, EventArgs e)
        {
            comboBox1_additems();
        }

        private void nCASizesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            config();
        }

        private void fTPSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form FTP = new FTP();
            this.Hide();
            FTP.ShowDialog();
            this.Show();
        }

        private void sendIPSPatchesToSwitchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Visible = false;

            try
            {
                //make sure the ftp database exists!
                if (!File.Exists(ftpdb))
                {
                    Form FTP = new FTP();
                    this.Hide();
                    FTP.ShowDialog();
                    this.Show();
                }

                else
                {
                    //read the database for ip,port,username and password.
                    using var con = new SQLiteConnection(ftpdatabase);
                    con.Open();
                    using var cmd = new SQLiteCommand(con);
                    cmd.CommandText = "SELECT * from ftp";

                    SQLiteDataReader sqReader = cmd.ExecuteReader();
                    while (sqReader.Read())
                    {
                        ip = (sqReader.GetString(0));
                        port = (sqReader.GetString(1));
                        user = (sqReader.GetString(2));
                        pass = (sqReader.GetString(3));
                    }
                    sqReader.Close();

                    //make sure the Atmosphere folder exists now.
                    if (Directory.Exists("atmosphere"))
                    {
                        //cool the Atmosphere folder exists - ftp it to the switch - but check the ftp connection is ok fist.
                        //https://github.com/robinrodricks/FluentFTP/wiki
                        //https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP.CSharpExamples/UploadDirectory.cs

                        var client = new FtpClient
                        {
                            Host = ip,
                            Port = Int16.Parse(port),
                            Credentials = new NetworkCredential(user, pass),
                        };

                        client.ConnectTimeout = 1000; //wait 1 second

                        try
                        {
                            client.Connect();
                        }
                        catch
                        {
                            MessageBox.Show("Can't connect to the ftp, check your ftp settings", "FTP Connection Error!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                        }

                        if (client.IsConnected)
                        {
                            //set graphic so we know we are connected.
                            pictureBox1.Visible = true;

                            //send the ips files and folders, only send ips files less than 100 bytes
                            var rules = new List<FtpRule>{
                                new FtpFileExtensionRule(true, new List<string>{ "ips" }),
                                new FtpSizeRule(FtpOperator.LessThan, 100)
                            };
                            client.UploadDirectory("Atmosphere", "atmosphere", FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite, FtpVerify.OnlyChecksum, rules);

                            //once folders are sent - disconnect.
                            client.Disconnect();

                            //once disconnected - reset graphic.
                            pictureBox1.Visible = false;

                            //tell user the patches have been sent.
                            MessageBox.Show("Atmosphere folder IPS contents sent", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                        }

                        else
                        {
                            pictureBox1.Visible = false;
                        }
                    }

                    else
                    {
                        //IPS patches have probably not been generated yet - show a message to tell the user.
                        MessageBox.Show("No atmosphere folder was found, have you created the patches yet?", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    }

                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void wildcardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Form wild = new Wildcards();
                this.Hide();
                wild.ShowDialog();
                populatewclist(); //refresh database in wco tabe when we exit the wilcards database.
                this.Show();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void setcheckbox()
        {
            string es_override = "0";
            string es_patch_override = "0";
            string fs_override = "0";
            string fs_patch_override = "0";
            string nfim_override = "0";
            string nfim_patch_override = "0";
            string extracted = "1";
            string decrypted = "1";
            string maindec_es = "1";
            string maindec_es2 = "1";
            string FS_clean = "1";
            string nfim_cleanmain = "1";
            string PatchesINI = "1";
            string FS_PatchesINI = "1";


            try
            {
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);
                cmd.CommandText = "SELECT * from CheckboxState";

                SQLiteDataReader sqReader = cmd.ExecuteReader();
                while (sqReader.Read())
                {
                    es_override = (sqReader.GetString(0));
                    es_patch_override = (sqReader.GetString(1));
                    fs_override = (sqReader.GetString(2));
                    fs_patch_override = (sqReader.GetString(3));
                    nfim_override = (sqReader.GetString(4));
                    nfim_patch_override = (sqReader.GetString(5));
                    extracted = (sqReader.GetString(6));
                    decrypted = (sqReader.GetString(7));
                    maindec_es = (sqReader.GetString(8));
                    maindec_es2 = (sqReader.GetString(9));
                    FS_clean = (sqReader.GetString(10));
                    nfim_cleanmain = (sqReader.GetString(11));
                    PatchesINI = (sqReader.GetString(12));
                    FS_PatchesINI = (sqReader.GetString(13));
                }
                sqReader.Close();

                //close the database;
                con.Close();
                con.Dispose();

                //set checkbox state
                if (es_override == "1")
                {
                    checkBox_ES_override.Checked = true;
                }
                else
                {
                    checkBox_ES_override.Checked = false;
                }
                if (es_patch_override == "1")
                {
                    checkBox_es_patch_override.Checked = true;
                }
                else
                {
                    checkBox_es_patch_override.Checked = false;
                }
                if (fs_override == "1")
                {
                    checkBox_fs_override.Checked = true;
                }
                else
                {
                    checkBox_fs_override.Checked = false;
                }
                if (fs_patch_override == "1")
                {
                    checkBox_fs_patch_override.Checked = true;
                }
                else
                {
                    checkBox_fs_patch_override.Checked = false;
                }
                if (nfim_override == "1")
                {
                    checkBox_nfim_override.Checked = true;
                }
                else
                {
                    checkBox_nfim_override.Checked = false;
                }
                if (nfim_patch_override == "1")
                {
                    checkBox_nfim_patch_override.Checked = true;
                }
                else
                {
                    checkBox_nfim_patch_override.Checked = false;
                }
                if (extracted == "1")
                {
                    checkBox_extracted.Checked = true;
                }
                else
                {
                    checkBox_extracted.Checked = false;
                }
                if (decrypted == "1")
                {
                    checkBox_decrypted.Checked = true;
                }
                else
                {
                    checkBox_decrypted.Checked = false;
                }
                if (maindec_es == "1")
                {
                    checkBox_maindec_es.Checked = true;
                }
                else
                {
                    checkBox_maindec_es.Checked = false;
                }
                if (maindec_es2 == "1")
                {
                    checkBox_maindec_es2.Checked = true;
                }
                else
                {
                    checkBox_maindec_es2.Checked = false;
                }
                if (FS_clean == "1")
                {
                    checkBox_FS_clean.Checked = true;
                }
                else
                {
                    checkBox_FS_clean.Checked = false;
                }
                if (nfim_cleanmain == "1")
                {
                    checkBox_nfim_cleanmain.Checked = true;
                }
                else
                {
                    checkBox_nfim_cleanmain.Checked = false;
                }
                if (PatchesINI == "1")
                {
                    checkBox_PatchesINI.Checked = true;
                }
                else
                {
                    checkBox_PatchesINI.Checked = false;
                }
                if (FS_PatchesINI == "1")
                {
                    checkBox_PatchesINI.Checked = true;
                }
                else
                {
                    checkBox_FS_PatchesINI.Checked = false;
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_ES_override_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_ES_override.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET es_override = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET es_override = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_es_patch_override_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_es_patch_override.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET es_patch_override = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET es_patch_override = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_fs_override_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_fs_override.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET fs_override = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET fs_override = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_fs_patch_override_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_fs_patch_override.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET fs_patch_override = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET fs_patch_override = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_nfim_override_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_nfim_override.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET nfim_override = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET nfim_override = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_nfim_patch_override_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_nfim_patch_override.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET nfim_patch_override = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET nfim_patch_override = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_extracted_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_extracted.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_extracted = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_extracted = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_decrypted_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_decrypted.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_decrypted = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_decrypted = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_maindec_es_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_maindec_es.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_maindec_es = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_maindec_es = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_maindec_es2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_maindec_es2.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_maindec_es2 = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_maindec_es2 = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_FS_clean_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_FS_clean.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_FS_clean = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_FS_clean = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_nfim_cleanmain_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_nfim_cleanmain.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_nfim_cleanmain = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_nfim_cleanmain = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (tabControl1.SelectedTab.Name == "tabPage_WCO")
                {
                    //MessageBox.Show("Don't mess about with these values or checkboxes if you don't know what they are for!", "** A Warning For Noobs **", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (File.Exists(wildacarddb))
                    {
                        populatewclist();
                    }
                    else
                    {
                        MessageBox.Show("No wilcard database found, creating a basic database", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                        SQLiteConnection.CreateFile(wildacarddb);
                        //best make some tables now....
                        database_make_tables();
                        populatewclist();
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void database_make_tables()
        {
            try
            {
                using var con = new SQLiteConnection(wildcarddatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                //create the empty tables
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS wildcards(id INTEGER, FW_Version Text, es_patch Text, fs_patch1 Text, fs_patch2 Text, nfim_patch1 Text, nfim_patch2 Text, es_override Text, fs1_override Text, fs2_override Text, nfim_override Text, info Text)";
                cmd.ExecuteNonQuery();

                //populate empty tables with default values
                cmd.CommandText = "INSERT INTO wildcards(id, FW_Version, es_patch, fs_patch1, fs_patch2, nfim_patch1, nfim_patch2, es_override, fs1_override, fs2_override, nfim_override, info) VALUES(@id, @FW_Version, @es_patch, @fs_patch1, @fs_patch2, @nfim_patch1, @nfim_patch2, @es_override, @fs1_override, @fs2_override, @nfim_override, @info)";
                cmd.Parameters.AddWithValue("@id", 1);
                cmd.Parameters.AddWithValue("@FW_Version", "14.3.0.0");
                cmd.Parameters.AddWithValue("@es_patch", textBox_es_patch.Text);
                cmd.Parameters.AddWithValue("@fs_patch1", textBox_fs_patch1.Text);
                cmd.Parameters.AddWithValue("@fs_patch2", textBox_fs_patch2.Text);
                cmd.Parameters.AddWithValue("@nfim_patch1", textBox_nfim_patch1.Text);
                cmd.Parameters.AddWithValue("@nfim_patch2", textBox_nfim_patch2.Text);
                cmd.Parameters.AddWithValue("@es_override", textBox_es_override.Text);
                cmd.Parameters.AddWithValue("@fs1_override", textBox_fs1_override.Text);
                cmd.Parameters.AddWithValue("@fs2_override", textBox_fs2_overide.Text);
                cmd.Parameters.AddWithValue("@nfim_override", textBox_nfim_override.Text);
                cmd.Parameters.AddWithValue("@info", "The wilcards and patches above are taken from the current values shown in the WCO tab from the main menu.");

                cmd.Prepare();
                cmd.ExecuteNonQuery();
                con.Close(); //close the database...
                con.Dispose();

            }

            catch (Exception error)
            {
                MessageBox.Show("The error is: " + error.Message, "Eeeeek!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void populatewclist()
        {
            try
            {
                listView1.Clear(); //reset

                using var con = new SQLiteConnection(wildcarddatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con)
                {
                    CommandText = "SELECT * from wildcards ORDER BY FW_Version COLLATE NOCASE ASC"
                };

                SQLiteDataReader sqReader = cmd.ExecuteReader();

                listView1.View = View.Details;
                listView1.Columns.Add("SDK Version");

                while (sqReader.Read())
                {
                    int listid = (sqReader.GetInt32(0));
                    string id = listid.ToString();

                    string FW_Version = (sqReader.GetString(1));
                    string es_patch = (sqReader.GetString(2));
                    string fs_patch1 = (sqReader.GetString(3));
                    string fs_patch2 = (sqReader.GetString(4));
                    string nfim_patch1 = (sqReader.GetString(5));
                    string nfim_patch2 = (sqReader.GetString(6));
                    string es_override = (sqReader.GetString(7));
                    string fs1_override = (sqReader.GetString(8));
                    string fs2_override = (sqReader.GetString(9));
                    string nfim_override = (sqReader.GetString(10));
                    string info = (sqReader.GetString(11));
                    listView1.Items.Add(new ListViewItem(new string[] { FW_Version, es_patch, fs_patch1, fs_patch2, nfim_patch1, nfim_patch2, es_override, fs1_override, fs2_override, nfim_override, info, id }));
                }

                con.Close();
                con.Dispose();

                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }

            catch (Exception error)
            {
                MessageBox.Show("The error is: " + error.Message, "Eeeeek!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            sdk_click();
        }

        private void sdk_click()
        {
            try
            {
                if (listView1.SelectedItems.Count == 0)
                    return;
                ListViewItem item = listView1.SelectedItems[0];
                //fill the text boxes
                textBox_es_patch.Text = item.SubItems[1].Text;
                textBox_fs_patch1.Text = item.SubItems[2].Text;
                textBox_fs_patch2.Text = item.SubItems[3].Text;
                textBox_nfim_patch1.Text = item.SubItems[4].Text;
                textBox_nfim_patch2.Text = item.SubItems[5].Text;
                textBox_es_override.Text = item.SubItems[6].Text;
                textBox_fs1_override.Text = item.SubItems[7].Text;
                textBox_fs2_overide.Text = item.SubItems[8].Text;
                textBox_nfim_override.Text = item.SubItems[9].Text;
            }

            catch (Exception error)
            {
                MessageBox.Show("The error is: " + error.Message, "Eeeeek!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void sendPatchesiniToSwitchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Visible = false;

            try
            {
                //make sure the ftp database exists!
                if (!File.Exists(ftpdb))
                {
                    Form FTP = new FTP();
                    this.Hide();
                    FTP.ShowDialog();
                    this.Show();
                }

                else
                {
                    //read the database for ip,port,username and password.
                    using var con = new SQLiteConnection(ftpdatabase);
                    con.Open();
                    using var cmd = new SQLiteCommand(con);
                    cmd.CommandText = "SELECT * from ftp";

                    SQLiteDataReader sqReader = cmd.ExecuteReader();
                    while (sqReader.Read())
                    {
                        ip = (sqReader.GetString(0));
                        port = (sqReader.GetString(1));
                        user = (sqReader.GetString(2));
                        pass = (sqReader.GetString(3));
                    }
                    sqReader.Close();

                    //make sure the Atmosphere folder exists now.
                    if (Directory.Exists("bootloader"))
                    {

                        var client = new FtpClient
                        {
                            Host = ip,
                            Port = Int16.Parse(port),
                            Credentials = new NetworkCredential(user, pass),
                        };

                        client.ConnectTimeout = 1000; //wait 1 second

                        try
                        {
                            client.Connect();
                        }
                        catch
                        {
                            MessageBox.Show("Can't connect to the ftp, check your ftp settings", "FTP Connection Error!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                        }

                        if (client.IsConnected)
                        {
                            //set graphic so we know we are connected.
                            pictureBox1.Visible = true;

                            //send the ips files and folders, only send ips files less than 100 bytes
                            var rules = new List<FtpRule>{
                                new FtpFileExtensionRule(true, new List<string>{ "ini" }),
                                new FtpSizeRule(FtpOperator.LessThan, 500000)
                            };
                            client.UploadDirectory("bootloader", "bootloader", FtpFolderSyncMode.Update, FtpRemoteExists.AddToEnd, FtpVerify.OnlyChecksum, rules);

                            //once folders are sent - disconnect.
                            client.Disconnect();

                            //once disconnected - reset graphic.
                            pictureBox1.Visible = false;

                            //tell user the patches have been sent.
                            MessageBox.Show("Bootloader folder patches.ini sent", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                        }

                        else
                        {
                            pictureBox1.Visible = false;
                        }
                    }

                    else
                    {
                        //IPS patches have probably not been generated yet - show a message to tell the user.
                        MessageBox.Show("No bootloader folder was found, have you created the patches.ini file yet?", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    }

                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_FS_PatchesINI_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_FS_PatchesINI.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_FS_PatchesINI = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_FS_PatchesINI = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void checkBox_PatchesINI_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                if (checkBox_PatchesINI.Checked == true)
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_PatchesINI = 1");
                }
                else
                {
                    cmd.CommandText = ("UPDATE CheckboxState SET checkBox_PatchesINI = 0");
                }

                cmd.ExecuteNonQuery();
                con.Close();
                con.Dispose();
            }
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void removeAtmosphereFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var question = MessageBox.Show("Do you really want to remove the Atmosphere folder and contents?", "Clean IPS Patches", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (question == DialogResult.Yes)
            {
                try
                {
                    string folderPath = "atmosphere";
                    //Safety check for directory existence.
                    if (!Directory.Exists(folderPath))
                    {
                        return;
                    }
                    else
                    {
                        DirectoryInfo directory = new DirectoryInfo(folderPath);
                        directory.Delete(true);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }
        }

        private void removePatchesiniToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var question = MessageBox.Show("Do you really want to remove the bootloader folder and contents?", "Clean IPS Patches", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (question == DialogResult.Yes)
            {
                try
                {
                    string folderPath = "bootloader";
                    //Safety check for directory existence.
                    if (!Directory.Exists(folderPath))
                    {
                        return;
                    }
                    else
                    {
                        DirectoryInfo directory = new DirectoryInfo(folderPath);
                        directory.Delete(true);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show("Error is: " + error.Message);
                }
            }
        }

        private void cleanOldFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clean_message = 1;
            clean();
        }

        private void button_search_Click(object sender, EventArgs e)
        {
            try
            {
                Form search = new Form_search();
                this.Hide();
                search.ShowDialog();
                this.Show();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        void clean()
        {
            try
            {
                string dir_temp = "Temp";
                string dir_files = "files";
                string file_dumped_Loader = "dumped_Loader";
                string file_decloader = "dec-Loader.bin";
                string main_dec = "main_dec";
                string main = "main";
                string main_npdm = "main.npdm";
                string fat_kip = "FS.kip1-fat.dec";
                string exfat_kip = "FS.kip1-exfat.dec";


                if (File.Exists(exfat_kip))
                {
                    File.Delete(exfat_kip);
                }

                if (File.Exists(fat_kip))
                {
                    File.Delete(fat_kip);
                }

                if (File.Exists(main_npdm))
                {
                    File.Delete(main_npdm);
                }

                if (File.Exists(main))
                {
                    File.Delete(main);
                }

                if (File.Exists(main_dec))
                {
                    File.Delete(main_dec);
                }

                if (File.Exists(file_decloader))
                {
                    File.Delete(file_decloader);
                }

                if (File.Exists(file_dumped_Loader))
                {
                    File.Delete(file_dumped_Loader);
                }

                if (Directory.Exists(dir_temp))
                {
                    DirectoryInfo directory = new DirectoryInfo(dir_temp);
                    directory.Delete(true);
                }

                if (Directory.Exists(dir_files))
                {
                    DirectoryInfo directory = new DirectoryInfo(dir_files);
                    directory.Delete(true);
                }

                if (clean_message == 1)
                {
                    MessageBox.Show("Old temp and decrypted files have been cleaned up.", "Clean Up Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            
            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }
    }

    public class Language
    {
        public string LinkName { get; set; }
        public string LinkValue { get; set; }
    }
}