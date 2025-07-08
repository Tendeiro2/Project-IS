using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Publisher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            _ = InitializeApplication();
            _ = LoadApplicationRecords();
        }

        private async Task InitializeApplication()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44322/api/somiod/Switch");

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        XElement xml = new XElement("Application",
                            new XElement("Name", "Switch")

                        );

                        var content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml");

                        response = await client.PostAsync("https://localhost:44322/api/somiod", content);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Application 'Switch' created successfully.");
                        }
                        else
                        {
                            string errorMessage = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Failed to create 'Switch': {response.StatusCode} - {errorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing application: {ex.Message}");
            }
        }

        private async Task LoadApplicationRecords()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/xml");
                    client.DefaultRequestHeaders.Add("somiod-locate", "record");

                    HttpResponseMessage response = await client.GetAsync("https://localhost:44322/api/somiod/Lighting");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        XElement xmlResponse = XElement.Parse(responseContent);

                        var records = xmlResponse.Elements("Record");
                        listBox1.Items.Clear();

                        if (records.Any())
                        {
                            foreach (var record in records)
                            {
                                string recordName = record.Element("Name")?.Value;
                                listBox1.Items.Add($"Name: {recordName}"); 
                            }

                        }
                        else
                        {
                            listBox1.Items.Add("No records found.");
                        }
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        listBox1.Items.Clear();
                        listBox1.Items.Add($"Error: {response.StatusCode} - {errorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                listBox1.Items.Clear();
                listBox1.Items.Add($"Error connecting to the server: {ex.Message}");
            }
        }



        private async void button1_Click(object sender, EventArgs e)
        {
            XElement xml = new XElement("Record",
                new XElement("Name", "Record1"),
                new XElement("Content", "on")
            );

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/xml");

                    Console.WriteLine($"XML Enviado: {xml}");

                    var content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml");

                    HttpResponseMessage response = await client.PostAsync("https://localhost:44322/api/somiod/Lighting/light_bulb", content);

                    if (response.IsSuccessStatusCode)
                    {
                        await LoadApplicationRecords();
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Erro ao executar a operação: {response.StatusCode} - {errorMessage}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao servidor: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void button2_Click(object sender, EventArgs e)
        {

            XElement xml = new XElement("Record",
                new XElement("Name", "Record2"),
                new XElement("Content", "off")
            );

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/xml");

                    Console.WriteLine($"XML Enviado: {xml}");

                    var content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml");

                    HttpResponseMessage response = await client.PostAsync("https://localhost:44322/api/somiod/Lighting/light_bulb", content);

                    if (response.IsSuccessStatusCode)
                    {
                        await LoadApplicationRecords();
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Erro ao executar a operação: {response.StatusCode} - {errorMessage}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao servidor: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
            {
                MessageBox.Show("Please select a record to delete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedRecord = listBox1.SelectedItem.ToString();
            string recordName = selectedRecord.Split(':')[1].Trim();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/xml");

                    string deleteUrl = $"https://localhost:44322/api/somiod/Lighting/light_bulb/record/{recordName}";

                    HttpResponseMessage response = await client.DeleteAsync(deleteUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        await LoadApplicationRecords();
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to delete the record: {response.StatusCode} - {errorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to the server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
