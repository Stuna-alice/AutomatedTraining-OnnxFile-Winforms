using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;

namespace UICloudTraining
{
    public partial class CloudUI : Form
    {
        private const string credentialsPath = @"C:\Users\FCB22080106\OneDrive\Desktop\UICloudTraining\api_key\credentials.json";
        private const string folderID = "1P78-aXnjqXUv6rwJG6-r6zc3ZjuB5463";
        private readonly string customDirectory = @"C:\Users\FCB22080106\OneDrive\Desktop\UICloudTraining\Dataset";
        private List<string> uploadedFiles = new List<string>(); // List to store uploaded file paths

        public CloudUI()
        {
            InitializeComponent();
        }

        private async void UploadBTN_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog searchFile = new OpenFileDialog();
                searchFile.ShowDialog();

                fileName_tb.Text = Path.GetFileName(searchFile.FileName);
                fileExtension_tb.Text = Path.GetExtension(searchFile.FileName);
                filePath_tb.Text = Path.GetFullPath(searchFile.FileName);

                string filePath = filePath_tb.Text;
                long fileLength = new FileInfo(filePath).Length;
                double fileSizeInMegabytes = (double)fileLength / (1024 * 1024);
                fileSize_tb.Text = fileSizeInMegabytes.ToString("N2");

                if (!Directory.Exists(customDirectory))
                {
                    Directory.CreateDirectory(customDirectory);
                }

                string filename = Path.GetFileName(searchFile.FileName);
                string destinationPath = Path.Combine(customDirectory, filename);

                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(searchFile.FileName);
                uploadedFiles.Add(destinationPath);

                File.Copy(searchFile.FileName, destinationPath);

                if (File.Exists(destinationPath))
                {
                    connections_tx.Text = "✔️ Connected to Google Drive API";
                    connections_tx.BackColor = Color.DarkGreen;
                    uploadfile_tx.Text = "Uploading: " + filename;
                    uploadfile_tx.BackColor = Color.DarkOrange;


                    ProgressBar loadingbar2 = progressBar2; // Replace with your actual ProgressBar name
                    loadingbar2.Value = 0; // Reset progress bar value before uploading
                    await UploadFilestoGoogleDriveAsync(credentialsPath, folderID, destinationPath, loadingbar2);

                    uploadfile_tx.Text = "✔️ Successfully uploaded: " + filename;
                    uploadfile_tx.BackColor = Color.DarkGreen;

                    Console.WriteLine("File uploaded to Google Drive successfully");

                    string textFilePath = Path.Combine(customDirectory, filenameWithoutExtension + ".txt");


                    await UploadFilestoGoogleDriveAsync(credentialsPath, folderID, textFilePath, loadingbar2);

                    Console.WriteLine("Text file uploaded to Google Drive successfully");
                }
                else
                {
                    connections_tx.Text = "❌ Not connected to Google Drive API";
                    connections_tx.BackColor = Color.Red;
                    uploadfile_tx.Text = "❌ File was not uploaded";
                    uploadfile_tx.BackColor = Color.Red;
                    Console.WriteLine("File was not copied successfully");
                }
            }
            catch (Exception ex)
            {
                connections_tx.Text = "❌ Not connected to Google Drive API";
                connections_tx.BackColor = Color.Red;
                uploadfile_tx.Text = "❌ File has duplicate in localStorage ";
                uploadfile_tx.BackColor = Color.Red;
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        //dropdown button 


        private async void Generate_btn_Click(object sender, EventArgs e)
        {
            try
            {
                // Set the default value in the ComboBox

                if (txtDropdown.SelectedItem == null)
                {
                    txtDropdown.SelectedItem = "640";
                    MessageBox.Show("Image size set to default (640).", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }


                // Validate if batch_tb.Text is an integer
                if (!int.TryParse(batch_tb.Text, out int batch))
                {
                    userinput_tx.Text = "❌Please enter a valid integer for Batch";
                    userinput_tx.BackColor = Color.Red;
                }
                // Validate if epoch_tb.Text is an integer
                else if (!int.TryParse(epoch_tb.Text, out int epoch))
                {
                    userinput_tx.Text = "❌Please enter a valid integer for Epoch";
                    userinput_tx.BackColor = Color.Red;
                }
                // Validate if both batch and epoch values are provided
                else if (string.IsNullOrWhiteSpace(batch_tb.Text) || string.IsNullOrWhiteSpace(epoch_tb.Text))
                {
                    userinput_tx.Text = "❌Please enter values for both Batch and Epoch";
                    userinput_tx.BackColor = Color.Red;
                }
                else
                {
                    // All parameters are correct, display a success message
                    userinput_tx.Text = "✔️ All parameters are correct";
                    userinput_tx.BackColor = Color.DarkGreen;

                    // Text file
                    foreach (string uploadedFile in uploadedFiles)
                    {
                        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(uploadedFile);
                        string userFile = filenameWithoutExtension + ".txt";

                        // Construct the full file path
                        string filePath = Path.Combine(@"C:\Users\FCB22080106\OneDrive\Desktop\UICloudTraining\Dataset", userFile);

                        // Get the selected value from the ComboBox
                        string selectedValue = txtDropdown.SelectedItem.ToString();

                        // Write Directory and fileName
                        using (TextWriter txtfile = new StreamWriter(filePath))
                        {
                            // Write inside txt with new lines
                            txtfile.WriteLine("imageSize:" + selectedValue);
                            txtfile.WriteLine("batch:" + batch_tb.Text);
                            txtfile.WriteLine("epoch:" + epoch_tb.Text);
                        }

                        // Get the ProgressBar control from the form
                        ProgressBar progressBar = loading_Bar; //loading Id UI

                        // Upload text file to Google Drive
                        await UploadFilestoGoogleDriveAsync(credentialsPath, folderID, filePath, progressBar);

                        txtConnection.Text = "✔️ Successfully uploaded:\n " + userFile;
                        txtConnection.BackColor = Color.DarkGreen;

                        Console.WriteLine("Text file uploaded to Google Drive successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                userinput_tx.Text = "❌ An error occurred: " + ex.Message;
                userinput_tx.BackColor = Color.Red;
                Console.WriteLine("Error: " + ex.Message);
            }
        }


        // ...

        private async Task UploadFilestoGoogleDriveAsync(string credentialsPath, string folderID, string filePath, ProgressBar progressBar)
        {

            progressBar.BackColor = Color.Red; // Set progress bar color
            var stopwatch = new Stopwatch();

            try
            {
                GoogleCredential credential;
                using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(new[] { DriveService.ScopeConstants.DriveFile });
                }

                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Drive Upload Console"
                });

                var fileMetaData = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath), // Use the file name as the name of the uploaded file
                    Parents = new List<string> { folderID }
                };

                using (var streamFile = new FileStream(filePath, FileMode.Open))
                {
                    var request = service.Files.Create(fileMetaData, streamFile, "");
                    stopwatch.Start();

                    request.ProgressChanged += async (IUploadProgress progress) =>
                    {
                        switch (progress.Status)
                        {
                            case UploadStatus.Starting:
                                break;
                            case UploadStatus.Completed:
                                progressBar.Invoke((MethodInvoker)(() =>
                                {
                                    progressBar.Value = 100;
                                    progressBar.BackColor = Color.DarkGreen; // Set progress bar color to green
                                }));
                                break;
                            case UploadStatus.Uploading:
                                progressBar.Invoke((MethodInvoker)(() =>
                                {
                                    // Calculate elapsed time in seconds
                                    var elapsedTimeInSeconds = stopwatch.Elapsed.TotalSeconds;

                                    // Calculate upload speed in bytes per second
                                    var uploadSpeed = progress.BytesSent / elapsedTimeInSeconds;

                                    // Calculate estimated time remaining in seconds
                                    var estimatedTimeRemaining = (streamFile.Length - progress.BytesSent) / uploadSpeed;

                                    // Update label text with formatted time
                                    var formattedTime = FormatTime(elapsedTimeInSeconds);
                                    timeLabel.Invoke((MethodInvoker)(() =>
                                    {
                                        timeLabel.Text = "Elapsed Time: " + formattedTime;
                                    }));

                                    // Ensure the progress bar value stays within the valid range (0 to 100)
                                    var calculatedValue = (int)(elapsedTimeInSeconds * 100 / estimatedTimeRemaining);
                                    progressBar.Value = Math.Min(100, Math.Max(0, calculatedValue));
                                }));
                                break;
                        }
                    };

                    await request.UploadAsync();
                }

                stopwatch.Stop();
                Console.WriteLine("File uploaded to Google Drive successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // Handle exception, display an error message, etc.
                // Ensure you're updating the UI on the UI thread if needed.
                // Example: MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string FormatTime(double totalSeconds)
        {
            var timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        // ...



        private void Local_Click(object sender, EventArgs e)
        {
            // Specify the path to the folder you want to open
            string folderPath = @"C:\Users\FCB22080106\OneDrive\Desktop\UICloudTraining\Dataset";

            // Check if the folder exists before attempting to open it
            if (System.IO.Directory.Exists(folderPath))
            {
                // Use Process.Start to open the folder with the default file explorer
                Process.Start(folderPath);
            }
            else
            {
                MessageBox.Show("The specified folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Restartbtn_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void Exitbtn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


    }
}