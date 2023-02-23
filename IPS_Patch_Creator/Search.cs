using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace IPS_Patch_Creator
{
    public partial class Form_search : Form
    {
        string filepath = "";
        int reset = 0;
        public Form_search()
        {
            InitializeComponent();
        }

        private void button_clear_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            textBox1.Clear();
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            load();
        }

        public void load()
        {
            try
            {
                richTextBox1.ForeColor = Color.LimeGreen;
                filepath = "";
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "All Files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filepath = openFileDialog.FileName;
                    richTextBox1.Clear();
                    richTextBox1.Text += "Selected file: " + filepath + "\n";
                }
                else
                {
                    richTextBox1.Text += "\n\nNo file chosen";
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void FindFirst()
        {
            try
            {
                String find = textBox1.Text.ToLower();
                if (find == "" || find.Length == 0)
                {
                    richTextBox1.Text = "Enter something to search for!";
                }
                else
                {
                    richTextBox1.Text += "Searching for: " + find + "\n";
                    if (!File.Exists(filepath))
                    {
                        richTextBox1.Text += "\n" + "Load a file to search through!";
                    }
                    else
                    {
                        byte[] ByteBuffer = File.ReadAllBytes(filepath).Skip(0).ToArray(); //start at position (skip) 0 in file
                        //byte[] ByteBuffer = File.ReadAllBytes("FS.kip1-fat.dec").Skip(0).Take(255).ToArray(); //only read the first FF bytes.

                        //convert byte array Bytebuffer into a long hex string
                        StringBuilder hex = new StringBuilder(ByteBuffer.Length * 2);
                        foreach (byte b in ByteBuffer)
                            hex.AppendFormat("{0:x2}", b);

                        string str = hex.ToString();
                        find = find.Replace(" ", ""); //remove spaces
                        //find = find.Replace(".", "..");
                        int bytefindlength = (find.Length / 2);

                        Match match = Regex.Match(str, find);
                        if (match.Success)
                        {
                            int index = match.Index;
                            index = index / 2; //make sure we divide by 2 again as we multiplied above...
                            richTextBox1.Text += "\n" + "Wildcard Length: " + find.Length.ToString() + " (" + bytefindlength.ToString() + " Bytes)" + "\n";
                            richTextBox1.Text += "\n" + "Buffer Size: " + ByteBuffer.Length.ToString() + " Bytes\n";
                            richTextBox1.Text += "\n" + "Pattern first found at offset: 0x" + index.ToString("X8") + " (" + index.ToString() + ")";
                        }
                        else
                        {
                            richTextBox1.ForeColor = Color.Red;
                            richTextBox1.Text += "\n" + "Search pattern was not found :-(";
                        }
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void FindAll()
        {
            try
            {
                String find = textBox1.Text.ToLower();
                if (find == "" || find.Length == 0)
                {
                    richTextBox1.Text = "Enter something to search for!";
                }
                else
                {
                    richTextBox1.Text += "Searching for: " + find + "\n";
                    if (!File.Exists(filepath))
                    {
                        richTextBox1.Text += "\n" + "Load a file to search through!";
                    }
                    else
                    {
                        byte[] ByteBuffer = File.ReadAllBytes(filepath).Skip(0).ToArray(); //start at position (skip) 0 in file
                        StringBuilder hex = new StringBuilder(ByteBuffer.Length * 2);
                        foreach (byte b in ByteBuffer)
                            hex.AppendFormat("{0:x2}", b);

                        string str = hex.ToString();
                        find = find.Replace(" ", ""); //remove spaces
                        //find = find.Replace(".", "..");
                        int occurence = 0;


                        Match match = Regex.Match(str, find);
                        if (match.Success)
                        {
                            while (match.Success)
                            {
                                int index = match.Index;
                                index = index / 2; //make sure we divide by 2 again as we multiplied above...
                                richTextBox1.Text += "\nPattern found at offset: 0x" + index.ToString("X8") + " (" + index.ToString() + ")";
                                occurence++;
                                match = match.NextMatch();
                            }
                            richTextBox1.Text += "\nPattern found: " + occurence.ToString() + " times";
                        }
                        else
                        {
                            richTextBox1.ForeColor = Color.Red;
                            richTextBox1.Text += "\n" + "Search pattern was not found :-(";
                        }
                    }
                }
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void button_find_Click(object sender, EventArgs e)
        {
            richTextBox1.ForeColor = Color.LimeGreen;
            richTextBox1.Clear();
            FindFirst();
        }

        private void Find_All_Click(object sender, EventArgs e)
        {
            richTextBox1.ForeColor = Color.LimeGreen;
            richTextBox1.Clear();
            FindAll();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //https://www.eso.org/~ndelmott/ascii.html
            char c = e.KeyChar;
            if (c != '\b' && c != ' ' && c != '.' && !((c <= 0x66 && c >= 61) || (c <= 0x46 && c >= 0x41) || (c >= 0x30 && c <= 0x39) || (c == 0x16) || (c == 0x03)))
            {
                e.Handled = true;
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBox1.SelectedText);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            char[] allowedChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' , '.', ' ' };

            foreach (char character in textBox1.Text.ToUpper().ToArray())
            {
                if (!allowedChars.Contains(character))
                {
                    textBox1.Clear();
                    reset = 1;
                }
            }
            
            if (reset > 0)
            {
                MessageBox.Show("Only hex values, spaces and periods are allowed in this box","You made a mistake",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            reset = 0;
        }
    }
}
