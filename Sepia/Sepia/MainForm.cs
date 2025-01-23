using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace Sepia
{
    public partial class MainForm : Form
    {
        [DllImport(@"C:\Users\adico\Desktop\Sepia\x64\Release\DLLCPP.dll")]
        public static extern void ApplySepiaFilterCpp(IntPtr pixelBuffer, int width, int bytesPerPixel, byte P, int startRow, int endRow, int stride);

        [DllImport(@"C:\Users\adico\Desktop\Sepia\x64\Release\DLLASM.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ApplySepiaFilterAsm(IntPtr pixelBuffer, int width, int bytesPerPixel, int stride, byte P, int startRow, int endRow);

        private Bitmap bitmap;
        private Bitmap originalBitmap;
        private int numberOfThreads;
        private int filterChoice;
        private byte sepiaParameter;
        private ProgressBar progressBar;
        private Label labelTime; // Label do wyświetlania czasu wątków

        public MainForm()
        {
            InitializeComponent();

            ComboBox comboThreads = Controls["comboThreads"] as ComboBox;
            if (comboThreads != null)
            {
                comboThreads.SelectedIndexChanged += ComboThreads_SelectedIndexChanged;
            }
        }

        private PictureBox pictureBoxOriginal;
        private PictureBox pictureBoxProcessed;

        private void InitializeComponent()
        {
            this.Text = "Sepia Filter GUI";
            this.ClientSize = new System.Drawing.Size(1000, 630);

            int centerX = this.ClientSize.Width / 2;

            Label labelFile = new Label()
            {
                Text = "Select BMP File:",
                AutoSize = true,
                Location = new Point(centerX - 120, 20)
            };
            this.Controls.Add(labelFile);

            Button buttonSelectFile = new Button()
            {
                Text = "Browse",
                Width = 100,
                Location = new Point(centerX, 15)
            };
            buttonSelectFile.Click += ButtonSelectFile_Click;
            this.Controls.Add(buttonSelectFile);

            pictureBoxOriginal = new PictureBox()
            {
                Width = 400,
                Height = 300,
                Location = new Point(centerX - 450, 60),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pictureBoxOriginal);

            pictureBoxProcessed = new PictureBox()
            {
                Width = 400,
                Height = 300,
                Location = new Point(centerX + 50, 60),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pictureBoxProcessed);

            Label labelThreads = new Label()
            {
                Text = "Number of Threads:",
                AutoSize = true,
                Location = new Point(centerX - 150, 380)
            };
            this.Controls.Add(labelThreads);

            ComboBox comboThreads = new ComboBox()
            {
                Name = "comboThreads",
                Width = 100,
                Location = new Point(centerX, 375)
            };
            comboThreads.Items.AddRange(new object[] { 1, 2, 4, 8, 16, 32, 64 });
            comboThreads.SelectedIndex = 0;
            comboThreads.DropDownStyle = ComboBoxStyle.DropDownList;
            comboThreads.SelectedIndexChanged += ComboThreads_SelectedIndexChanged;
            this.Controls.Add(comboThreads);
            numberOfThreads = 1;

            Label labelFilter = new Label()
            {
                Text = "Filter Type:",
                AutoSize = true,
                Location = new Point(centerX - 150, 420)
            };
            this.Controls.Add(labelFilter);

            RadioButton radioCpp = new RadioButton()
            {
                Text = "C++",
                AutoSize = true,
                Location = new Point(centerX - 30, 415)
            };

            RadioButton radioAsm = new RadioButton()
            {
                Text = "ASM",
                AutoSize = true,
                Location = new Point(centerX + 50, 415)
            };

            radioCpp.Checked = true;
            radioCpp.CheckedChanged += (s, e) => { if (radioCpp.Checked) filterChoice = 1; };
            radioAsm.CheckedChanged += (s, e) => { if (radioAsm.Checked) filterChoice = 2; };
            this.Controls.Add(radioCpp);
            this.Controls.Add(radioAsm);
            filterChoice = 1;

            Label labelP = new Label()
            {
                Text = "Sepia Parameter (20-40):",
                AutoSize = true,
                Location = new Point(centerX - 150, 460)
            };
            this.Controls.Add(labelP);

            NumericUpDown numericP = new NumericUpDown()
            {
                Width = 60,
                Location = new Point(centerX, 455),
                Minimum = 20,
                Maximum = 40,
                Value = 20
            };
            numericP.ValueChanged += (s, e) => { sepiaParameter = (byte)numericP.Value; };
            this.Controls.Add(numericP);
            sepiaParameter = 20;

            progressBar = new ProgressBar()
            {
                Width = 600,
                Height = 20,
                Location = new Point(centerX - 300, 500)
            };
            this.Controls.Add(progressBar);

            labelTime = new Label()
            {
                Text = "Processing Time: 0 ms",
                AutoSize = true,
                Location = new Point(centerX - 75, 540)
            };
            this.Controls.Add(labelTime);

            Button buttonSubmit = new Button()
            {
                Text = "Submit",
                Width = 100,
                Location = new Point(centerX - 50, 580)
            };

            buttonSubmit.Click += (s, e) =>
            {
                if (originalBitmap == null)
                {
                    MessageBox.Show("Please select a BMP file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bitmap = (Bitmap)originalBitmap.Clone();

                progressBar.Value = 0;
                ProcessImage();
                pictureBoxProcessed.Image = (Bitmap)bitmap.Clone();

                MessageBox.Show("Processing complete. Image saved to the desktop.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                progressBar.Value = 100;
            };
            this.Controls.Add(buttonSubmit);
        }

        private void ButtonSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "BMP Files (*.bmp)|*.bmp",
                Title = "Select BMP File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                originalBitmap = new Bitmap(openFileDialog.FileName);
                bitmap = (Bitmap)originalBitmap.Clone();
                pictureBoxOriginal.Image = (Bitmap)originalBitmap.Clone();
                MessageBox.Show($"File loaded: {openFileDialog.FileName}", "File Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ComboThreads_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo != null)
            {
                numberOfThreads = (int)combo.SelectedItem;
            }
        }

        private void ProcessImage()
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bitmapData.Scan0;
            int stride = bitmapData.Stride;

            byte[] pixelBuffer = new byte[stride * height];
            Marshal.Copy(ptr, pixelBuffer, 0, pixelBuffer.Length);
            IntPtr bufferPtr = Marshal.UnsafeAddrOfPinnedArrayElement(pixelBuffer, 0);

            int[] startIndices, endIndices;
            DivideDataForThreads(height, numberOfThreads, out startIndices, out endIndices);

            Thread[] threads = new Thread[numberOfThreads];
            Stopwatch threadStopwatch = Stopwatch.StartNew();

            for (int i = 0; i < numberOfThreads; i++)
            {
                int startRow = startIndices[i];
                int endRow = endIndices[i];

                threads[i] = new Thread(() =>
                {
                    if (filterChoice == 1)
                    {
                        ApplySepiaFilterCpp(bufferPtr, width, bytesPerPixel, sepiaParameter, startRow, endRow, stride);
                    }
                    else
                    {
                        ApplySepiaFilterAsm(bufferPtr, width, bytesPerPixel, stride, sepiaParameter, startRow, endRow);
                    }
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            threadStopwatch.Stop();
            double threadProcessingTime = threadStopwatch.Elapsed.TotalMilliseconds;

            Invoke(new Action(() =>
            {
                labelTime.Text = $"Thread Processing Time: {threadProcessingTime:F2} ms";
            }));

            Marshal.Copy(pixelBuffer, 0, ptr, pixelBuffer.Length);
            bitmap.UnlockBits(bitmapData);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outputPath = System.IO.Path.Combine(desktopPath, $"SepiaOutput.bmp");
            bitmap.Save(outputPath);
        }

        private void DivideDataForThreads(int totalRows, int numberOfThreads, out int[] startIndices, out int[] endIndices)
        {
            startIndices = new int[numberOfThreads];
            endIndices = new int[numberOfThreads];

            int baseChunkSize = totalRows / numberOfThreads;
            int extraRows = totalRows % numberOfThreads;

            int currentStart = 0;
            for (int i = 0; i < numberOfThreads; i++)
            {
                int chunkSize = baseChunkSize + (i < extraRows ? 1 : 0);
                startIndices[i] = currentStart;
                endIndices[i] = currentStart + chunkSize;
                currentStart = endIndices[i];
            }
        }
    }
}
