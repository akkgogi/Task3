using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Data.SQLite;
using System.IO;

namespace Task3
{
    public partial class Form1 : Form
    {
        public string dbname = "ValCur.db3";
        public Form1()
        {
            string ParsingURL = @"https://cbr.ru/scripts/XML_daily.asp";
            InitializeComponent();
            CreateTable();
            int Length = Parse(ParsingURL);
            ComboBoxInitialising(Length);
        }

        private string CharParse(int id)
        {
            string output;

            using (var db = new SQLiteConnection("Data Source=" + dbname))
            {
                string cmdtext = $"select charcode from valutes where id like {id}";
                SQLiteCommand cmd = new SQLiteCommand(cmdtext, db);

                db.Open();
                output = (string)cmd.ExecuteScalar();
                cmd.CommandText = $"select name from valutes where id like {id}";
                output += " - " + (string)cmd.ExecuteScalar();
            }
            return output;
        }

        private void ComboBoxInitialising(int L)
        {
            for (int i = 0; i < L; i++)
            {
                comboBox1.Items.Add(CharParse(i));
                comboBox2.Items.Add(CharParse(i));
            }
        }

        private void CreateTable()
        {
            if (!File.Exists(dbname))
            {
                SQLiteConnection.CreateFile(dbname);
            }

            using (var db = new SQLiteConnection("Data Source=" + dbname))
            { 
                string cmdtext = "CREATE TABLE if not exists valutes(ID text primary key, nominal int not null, value double not null, charcode text not null, name text not null)";
                SQLiteCommand cmd = new SQLiteCommand(cmdtext, db);

                db.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private int Parse(string url)
        {
            var xml = XDocument.Load(url);
            int count = 1;

            using (var db = new SQLiteConnection("Data Source=" + dbname))
            {
                string cmdtext = "insert or replace into valutes(id, nominal, value, charcode, name) values(0, 1, 1, 'RUB', 'Российский рубль')";
                db.Open();
                SQLiteCommand cmd = new SQLiteCommand(cmdtext, db);
                cmd.ExecuteNonQuery();

                foreach (XElement Valute in xml.Element("ValCurs").Elements("Valute"))
                {
                    XElement CharCode = Valute.Element("CharCode");
                    XElement Nominal = Valute.Element("Nominal");
                    XElement Value = Valute.Element("Value");
                    XElement Name = Valute.Element("Name");

                    string ValueWithDot = "";
                    for (int i = 0; i < Value.Value.Length; i++)
                    {
                        if (Value.Value[i] == ',')
                        {
                            ValueWithDot += '.';
                        }
                        else
                        {
                            ValueWithDot += Value.Value[i];
                        }
                    }

                    cmdtext = $"insert or replace into valutes(ID, nominal, value, charcode, name) values({count}, {Nominal.Value},{ValueWithDot},'{CharCode.Value}','{Name.Value}')";
                    cmd.CommandText = cmdtext;

                    Console.WriteLine(cmd.CommandText);
                    cmd.ExecuteNonQuery();

                    count++;
                }
            }
            return count;
        }

        private double GetCurs(int id)
        {
            using (var db = new SQLiteConnection("Data Source=" + dbname))
            {
                string cmdtext = $"select value from valutes where id like {id}";
                SQLiteCommand cmd = new SQLiteCommand(cmdtext, db);

                db.Open();
                double value = (double)cmd.ExecuteScalar();

                cmdtext = $"select nominal from valutes where id like {id}";
                cmd.CommandText = cmdtext;
                int nominal = (int)cmd.ExecuteScalar();

                double result = Math.Round(value / nominal, 4);
                return result;
            }
        }

        private void MainConvert(object sender, EventArgs e)
        {
            int inputid;
            int outputid;

            try
            {
                inputid = comboBox1.SelectedIndex;
                outputid = comboBox2.SelectedIndex;

                double inputsum = Convert.ToDouble(textBox1.Text);

                double inputcurs = GetCurs(inputid);
                double outputcurs = GetCurs(outputid);

                double resultcurs = inputcurs / outputcurs;

                double resultsum = inputsum * resultcurs;

                textBox2.Text = resultsum.ToString();
            }
            catch (FormatException)
            {
                MessageBox.Show("Введите сумму переводимой валюты. Используйте точку вместо запятой при вводе десятичной дроби.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}