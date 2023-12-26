using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.IO;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Drawing.Imaging;

namespace CNSysInfo
{
    public partial class frmSysInfo : Form
    {
        public frmSysInfo()
        {
            InitializeComponent();
        }

        private void frmSysInfo_Load(object sender, EventArgs e)
        {
            txtSystemModel.Text = GetSystemProduct();
            txtSerialNumber.Text = GetLaptopSerialNumber();
            txtManufacturer.Text = GetManufacturer();
            txtMemory.Text = GetMemorySize();
            txtStorage.Text = DisplayStorageInformation();
            txtCPU.Text = GetCPUType();
            txtAssetTag.Text = GetAssetTag();
           

            try
            {
                // Get local date and time
                DateTime localDateTime = DateTime.Now;

                // Display the local date and time
                lblCurrentDateTime.Text = localDateTime.ToString();

                // Get local time zone information
                TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
                lblTimeZone.Text = localTimeZone.DisplayName;
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string GetLaptopSerialNumber()
        {
            string serialNumber = string.Empty;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
                ManagementObjectCollection managementObjects = searcher.Get();

                foreach (ManagementObject obj in managementObjects)
                {
                    serialNumber = obj["SerialNumber"].ToString();
                    break; // Assuming there's only one BIOS on the laptop
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return serialNumber;
        }
        private string GetSystemProduct()
        {
            string systemProduct = string.Empty;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct");
                ManagementObjectCollection managementObjects = searcher.Get();

                foreach (ManagementObject obj in managementObjects)
                {
                    systemProduct = obj["Name"].ToString();
                    break; // Assuming there's only one system product
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return systemProduct;
        }

        private string GetManufacturer()
        {
            string manufacturer = string.Empty;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                ManagementObjectCollection managementObjects = searcher.Get();

                foreach (ManagementObject obj in managementObjects)
                {
                    manufacturer = obj["Manufacturer"].ToString();
                    break; // Assuming there's only one computer system
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return manufacturer;
        }

        private string GetMemorySize()
        {
            string memorySize = string.Empty;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                ManagementObjectCollection managementObjects = searcher.Get();

                foreach (ManagementObject obj in managementObjects)
                {
                    // Convert bytes to gigabytes for a more readable result
                    ulong totalPhysicalMemoryBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                    double totalPhysicalMemoryGB = totalPhysicalMemoryBytes / (1024.0 * 1024.0 * 1024.0);

                    memorySize = $"{totalPhysicalMemoryGB:F2} GB";
                    break; // Assuming there's only one computer system
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return memorySize;
        }

        private string DisplayStorageInformation()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                ManagementObjectCollection managementObjects = searcher.Get();

                int diskCount = 0;
                ulong totalCapacityBytes = 0;

                foreach (ManagementObject obj in managementObjects)
                {
                    diskCount++;
                    // Convert bytes to gigabytes for a more readable result
                    ulong capacityBytes = Convert.ToUInt64(obj["Size"]);
                    totalCapacityBytes += capacityBytes;
                }

                double totalCapacityGB = totalCapacityBytes / (1024.0 * 1024.0 * 1024.0);

                return "Storage Disks: " + diskCount + " | Total Capacity: " + $"{totalCapacityGB:F2} GB";
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }

        private string GetCPUType()
        {
            string cpuType = string.Empty;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                ManagementObjectCollection managementObjects = searcher.Get();

                foreach (ManagementObject obj in managementObjects)
                {
                    cpuType = obj["Name"].ToString();
                    break; // Assuming there's only one processor
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return cpuType;
        }

        private string GetAssetTag()
        {
            string assetTag = string.Empty;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure");
                ManagementObjectCollection managementObjects = searcher.Get();

                foreach (ManagementObject obj in managementObjects)
                {
                    assetTag = obj["SMBIOSAssetTag"]?.ToString();
                    break; // Assuming there's only one system enclosure
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return assetTag;
        }
         
        private void SaveTextToFile(params string[] texts)
        {
            string fileName = $"SysInfo_{Environment.MachineName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    foreach (string text in texts)
                    {
                        writer.WriteLine(text);
                    }
                }

                MessageBox.Show("Text saved to file successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        private void TakeAndSaveScreenshot()
        {
            try
            {
                string fileName = $"SysInfo_{Environment.MachineName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

                using (Bitmap bitmap = new Bitmap(this.Width, this.Height))
                {
                    this.DrawToBitmap(bitmap, new Rectangle(0, 0, this.Width, this.Height));
                    bitmap.Save(filePath, ImageFormat.Png);
                }

                MessageBox.Show($"Screenshot saved to file: {fileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    
        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveTextToFile("Manufacturer: "+txtManufacturer.Text,"Model: "+txtSystemModel.Text,"Serial Number: "+txtSerialNumber.Text,"Memory: "+txtMemory.Text,"Storage: "+txtStorage.Text);
            TakeAndSaveScreenshot();
        }
    }
}
