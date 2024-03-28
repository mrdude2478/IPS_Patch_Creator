using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

/*
 * https://stackoverflow.com/questions/20158714/getting-a-value-from-another-form-using-c-sharp
 */

namespace IPS_Patch_Creator
{
    public partial class Config : Form
    {
        public static string mydb = ("tools/config.db");
        public static string mydatabase = ("Data Source=" + mydb);

        public Config()
        {
            InitializeComponent();

            //do some database checks
            check_database(); //make sure the database exists -if not create it with default values.
            populate_values(); //read the database and populate the text boxes with the data from the database.
        }

        private void textBox_FSMIN_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox_FSMIN.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBox_FSMIN.Text = textBox_FSMIN.Text.Remove(textBox_FSMIN.Text.Length - 1);
            }
        }

        private void textBox_FSMAX_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox_FSMAX.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBox_FSMAX.Text = textBox_FSMAX.Text.Remove(textBox_FSMAX.Text.Length - 1);
            }
        }

        private void textBox_ESMIN_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox_ESMIN.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBox_ESMIN.Text = textBox_ESMIN.Text.Remove(textBox_ESMIN.Text.Length - 1);
            }
        }

        private void textBox_ESMAX_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox_ESMAX.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBox_ESMAX.Text = textBox_ESMAX.Text.Remove(textBox_ESMAX.Text.Length - 1);
            }
        }

        private void textBox_NFIM_MIN_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox_NFIM_MIN.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBox_NFIM_MIN.Text = textBox_NFIM_MIN.Text.Remove(textBox_NFIM_MIN.Text.Length - 1);
            }
        }

        private void textBox_NFIM_MAX_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox_NFIM_MAX.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBox_NFIM_MAX.Text = textBox_NFIM_MAX.Text.Remove(textBox_NFIM_MAX.Text.Length - 1);
            }
        }

        private void button_save_config_Click(object sender, EventArgs e)
        {
            try
            {
                save_config();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            textBox_FSMIN.Text = "3000000";
            textBox_FSMAX.Text = "3500000";
            textBox_ESMIN.Text = "410000";
            textBox_ESMAX.Text = "510000";
            textBox_NFIM_MIN.Text = "550000";
            textBox_NFIM_MAX.Text = "680000";
            save_config();
        }

        private void save_config()
        {
            try
            {
                update_database();
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void check_database()
        {
            try
            {
                if (!File.Exists(mydb))
                {
                    SQLiteConnection.CreateFile(mydb);

                    //make the tables.
                    database_make_tables();
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
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                //create the empty tables
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS FS(min TEXT, max Text)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ES(min TEXT, max Text)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS NFIM(min TEXT, max Text)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Theme(value TEXT)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS CheckboxState(es_override TEXT, es_patch_override Text, fs_override Text, fs_patch_override Text, nfim_override Text, nfim_patch_override Text, checkBox_extracted Text, checkBox_decrypted Text, checkBox_maindec_es Text, checkBox_maindec_es2 Text, checkBox_FS_clean Text, checkBox_nfim_cleanmain Text, checkBox_PatchesINI Text, checkBox_FS_PatchesINI Text)";
                cmd.ExecuteNonQuery();

                //populate empty tables with default values
                cmd.CommandText = "INSERT INTO FS(min, max) VALUES(@min, @max)";
                cmd.Parameters.AddWithValue("@min", "3000000");
                cmd.Parameters.AddWithValue("@max", "3500000");
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                cmd.CommandText = "INSERT INTO ES(min, max) VALUES(@min, @max)";
                cmd.Parameters.AddWithValue("@min", "410000");
                cmd.Parameters.AddWithValue("@max", "510000");
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                cmd.CommandText = "INSERT INTO NFIM(min, max) VALUES(@min, @max)";
                cmd.Parameters.AddWithValue("@min", "550000");
                cmd.Parameters.AddWithValue("@max", "680000");
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                cmd.CommandText = "INSERT INTO Theme(value) VALUES(@value)";
                cmd.Parameters.AddWithValue("@value", "0");
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                cmd.CommandText = "INSERT INTO CheckboxState(es_override, es_patch_override, fs_override, fs_patch_override, nfim_override, nfim_patch_override, checkBox_extracted, checkBox_decrypted, checkBox_maindec_es, checkBox_maindec_es2, checkBox_FS_clean, checkBox_nfim_cleanmain, checkBox_PatchesINI, checkBox_FS_PatchesINI) VALUES(@es_override, @es_patch_override, @fs_override, @fs_patch_override, @nfim_override, @nfim_patch_override, @checkBox_extracted, @checkBox_decrypted, @checkBox_maindec_es, @checkBox_maindec_es2, @checkBox_FS_clean, @checkBox_nfim_cleanmain, @checkBox_PatchesINI, @checkBox_FS_PatchesINI)";
                cmd.Parameters.AddWithValue("@es_override", "0");
                cmd.Parameters.AddWithValue("@es_patch_override", "0");
                cmd.Parameters.AddWithValue("@fs_override", "0");
                cmd.Parameters.AddWithValue("@fs_patch_override", "0");
                cmd.Parameters.AddWithValue("@nfim_override", "0");
                cmd.Parameters.AddWithValue("@nfim_patch_override", "0");
                
                cmd.Parameters.AddWithValue("@checkBox_extracted", "1");
                cmd.Parameters.AddWithValue("@checkBox_decrypted", "1");
                cmd.Parameters.AddWithValue("@checkBox_maindec_es", "1");
                cmd.Parameters.AddWithValue("@checkBox_maindec_es2", "1");
                cmd.Parameters.AddWithValue("@checkBox_FS_clean", "1");
                cmd.Parameters.AddWithValue("@checkBox_nfim_cleanmain", "1");
                cmd.Parameters.AddWithValue("@checkBox_PatchesINI", "1");
                cmd.Parameters.AddWithValue("@checkBox_FS_PatchesINI", "1");
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                con.Close(); //close the database...
                con.Dispose();

                //set empty text box values.
                textBox_FSMIN.Text = "3000000";
                textBox_FSMAX.Text = "3500000";
                textBox_ESMIN.Text = "410000";
                textBox_ESMAX.Text = "510000";
                textBox_NFIM_MIN.Text = "550000";
                textBox_NFIM_MAX.Text = "680000";

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void update_database()
        {
            try
            {
                //put data into the database tables....
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);

                string fsmin = textBox_FSMIN.Text.ToString();
                string fsmax = textBox_FSMAX.Text.ToString();
                string esmin = textBox_ESMIN.Text.ToString();
                string esmax = textBox_ESMAX.Text.ToString();
                string nfimmin = textBox_NFIM_MIN.Text.ToString();
                string nfimmax = textBox_NFIM_MAX.Text.ToString();

                if (textBox_FSMIN.Text == "" || textBox_FSMIN.Text == String.Empty || textBox_FSMIN.TextLength == 0)
                {
                    MessageBox.Show("Please set FS Min Value ");
                    return;
                }
                if (textBox_FSMAX.Text == "" || textBox_FSMAX.Text == String.Empty || textBox_FSMAX.TextLength == 0)
                {
                    MessageBox.Show("Please set FS Max Value ");
                    return;
                }
                if (textBox_ESMIN.Text == "" || textBox_ESMIN.Text == String.Empty || textBox_ESMIN.TextLength == 0)
                {
                    MessageBox.Show("Please set ES Min Value ");
                    return;
                }
                if (textBox_ESMAX.Text == "" || textBox_ESMAX.Text == String.Empty || textBox_ESMAX.TextLength == 0)
                {
                    MessageBox.Show("Please set ES Max Value ");
                    return;
                }
                if (textBox_NFIM_MIN.Text == "" || textBox_NFIM_MIN.Text == String.Empty || textBox_NFIM_MIN.TextLength == 0)
                {
                    MessageBox.Show("Please set NFIM Min Value ");
                    return;
                }
                if (textBox_NFIM_MAX.Text == "" || textBox_NFIM_MAX.Text == String.Empty || textBox_NFIM_MAX.TextLength == 0)
                {
                    MessageBox.Show("Please set NFIM Max Value ");
                    return;
                }

                else
                {
                    if (MessageBox.Show("Update Config", "Update!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        //update existsing values
                        cmd.CommandText = ("UPDATE FS SET max = '" + fsmax + "', min = '" + fsmin + "' WHERE rowid = 1");
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = ("UPDATE ES SET max = '" + esmax + "', min = '" + esmin + "' WHERE rowid = 1");
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = ("UPDATE NFIM SET max = '" + nfimmax + "', min = '" + nfimmin + "' WHERE rowid = 1");
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        return;
                    }
                }

                con.Close();
                con.Dispose();
                this.Close(); //close this page...
            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }

        private void populate_values()
        {
            try
            {
                //read the values from the database and then populate the textbox's
                using var con = new SQLiteConnection(mydatabase);
                con.Open();
                using var cmd = new SQLiteCommand(con);
                cmd.CommandText = "SELECT * from FS ORDER BY min COLLATE NOCASE ASC";

                SQLiteDataReader sqReader = cmd.ExecuteReader();
                while (sqReader.Read())
                {
                    string min = (sqReader.GetString(0));
                    string max = (sqReader.GetString(1));
                    textBox_FSMIN.Text = min;
                    textBox_FSMAX.Text = max;
                }
                sqReader.Close();

                //get data from database ES tables to populate the text box's
                cmd.CommandText = "SELECT * from ES ORDER BY min COLLATE NOCASE ASC";
                SQLiteDataReader sqReader2 = cmd.ExecuteReader();
                while (sqReader2.Read())
                {
                    string min = (sqReader2.GetString(0));
                    string max = (sqReader2.GetString(1));
                    textBox_ESMIN.Text = min;
                    textBox_ESMAX.Text = max;
                }
                sqReader2.Close();

                //get data from database NFIM tables to populate the text box's
                cmd.CommandText = "SELECT * from NFIM ORDER BY min COLLATE NOCASE ASC";
                SQLiteDataReader sqReader3 = cmd.ExecuteReader();
                while (sqReader3.Read())
                {
                    string min = (sqReader3.GetString(0));
                    string max = (sqReader3.GetString(1));
                    textBox_NFIM_MIN.Text = min;
                    textBox_NFIM_MAX.Text = max;
                }
                sqReader3.Close();

                //close the database;
                con.Close();
                con.Dispose();

            }

            catch (Exception error)
            {
                MessageBox.Show("Error is: " + error.Message);
            }
        }
    }
}
