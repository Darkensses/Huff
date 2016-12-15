using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using Microsoft.Win32;
using WPFSoundVisualizationLib;

namespace Huff
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string path = "";
        
        System.ComponentModel.BackgroundWorker backWorker = null;
        BinaryWriter bw = null;        
        WaveWriter ww = null;
        WaveForm wf = null;
        int stream = 0;

        System.Drawing.Graphics gWave = null;
        System.Drawing.Bitmap bWave = null;       
        
                     
        double MAX_AMP = Math.Pow(2, 16) / 2 - 1;
        const double amplitude = 1;

        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        Un4seen.Bass.Misc.Visuals spectrum;
        System.Drawing.Bitmap bmp;

        OpenFileDialog ofd = null;

        public MainWindow()
        {
            InitializeComponent();
            BassNet.Registration(Settings1.Default.userbass, Settings1.Default.keybass);
            spectrum = new Visuals();            

            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += timer_Tick;                                     

            //Objeto que sirve para hacer operaciones en otro hilo
            #region BackgroundWorker backWorker
            backWorker = new System.ComponentModel.BackgroundWorker();
            backWorker.WorkerReportsProgress = true;
            backWorker.WorkerSupportsCancellation = true;
            backWorker.DoWork += backWorker_DoWork;
            backWorker.ProgressChanged += backWorker_ProgressChanged;
            backWorker.RunWorkerCompleted += backWorker_RunWorkerCompleted;
            #endregion

            //Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);            
        }

        void timer_Tick(object sender, EventArgs e)
        {
            lblCurrentTime.Content = Utils.FixTimespan(BassEngine.Instance.ChannelPosition, "MMSSFFF");
            bmp = BassEngine.Instance.Spectrum(typeSpectrum, spectrum, (int)imgWave.Width, (int)imgWave.Height);            
            
            if (bmp != null)
            {
                MemoryStream ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Position = 0;
                BitmapImage bitmapWave = new BitmapImage();
                bitmapWave.BeginInit();
                bitmapWave.StreamSource = ms;
                bitmapWave.EndInit();

                imgWave.Source = bitmapWave;
            }

            if(Utils.FixTimespan(BassEngine.Instance.ChannelPosition,"MMSSFFF")==Utils.FixTimespan(BassEngine.Instance.TotalTime,"MMSSFFF"))
            {
                togglePlay.IsChecked = false;
                timer.Stop();
            }


        }

        private void butAdd_Click(object sender, RoutedEventArgs e)
        {
            ofd = new OpenFileDialog();
            ofd.Filter = "Wave Files (*.wav ; *.mp3) | *.wav;*.mp3";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (ofd.ShowDialog() == true) 
            {
                path = ofd.FileName;
                BassEngine.Instance.OpenFile(path);
                stream = Bass.BASS_StreamCreateFile(path, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);
                waveWFTL.RegisterSoundPlayer(BassEngine.Instance);
            
                //Enable Buttons
                butHuff.IsEnabled = togglePlay.IsEnabled = toggleSwitch.IsEnabled = butStop.IsEnabled = true;                

                tbkPath.Text = path;
                tbkProgress.Text = "Ready";
                tbkInfoStatus.Text = "We're ready to invoke to Huffman from the other world";
                tbkProgressStatus.Text = "...";

                lblCurrentTime.Content = "00:00.000";
                lblTotalTime.Content = Utils.FixTimespan(BassEngine.Instance.ChannelLength, "MMSSFFF");
            }             
        }

        void soundEngine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Nothing
        }
        
        private void butHuff_Click(object sender, RoutedEventArgs e)
        {
            // SE PUEDE IMPLEMENTAR EL OBJETO BackgroundWorker DE LA SIGUIENTE MANERA (OPCIONAL):
            /*backWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(
                delegate(object o, System.ComponentModel.DoWorkEventArgs args)
                {                                         
                });
            */            

            xAxis = 0;
            canvasWavComp.Children.Clear();
            // La tarea se ejecuta de maner asíncrona
            backWorker.RunWorkerAsync();            
            toggleSwitch.IsChecked = true;
            butHuff.IsEnabled = false;
            butRepeat.IsEnabled = true;
            tbkInfoStatus.Text = "Your Huffman File will be save on the Desktop [Code.huf]";
            lblInfoCompress.Content = "Wait...";
        }

        double xAxis = 0;
        double entropy = 0;
        double entcount = 0;
        double entauxlbl = 0;
        void backWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            System.ComponentModel.BackgroundWorker b = sender as System.ComponentModel.BackgroundWorker;
            int samples = 32;
            short[] buffer = new short[samples];
            bw = new BinaryWriter(File.Open(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/Code.huf", FileMode.Create));

            stream = Bass.BASS_StreamCreateFile(path, 0L, 0L, BASSFlag.BASS_STREAM_DECODE);            

            ww = new WaveWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/HufSound.wav", stream, true);
            int mult = 0;
            long len = Bass.BASS_ChannelGetLength(stream, BASSMode.BASS_POS_BYTES);

            while (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                int length = Bass.BASS_ChannelGetData(stream, buffer, samples * 2);                
                mult++;
                b.ReportProgress((int)(((samples * mult) * 100) / len * 2));
                List<short> listBuffer = new List<short>();
                HuffmanTree tree = new HuffmanTree();
                if (length > 0)
                {
                    listBuffer.AddRange(buffer);
                    short[] auxbuf = new short[buffer.Length];
                    auxbuf = buffer;
                    canvasWavComp.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new Action(delegate
                    {
                        //Whole Wave
                        //double xScale = canvasWavComp.Width / samples; 

                        //Formula by Manuel García. Dude you're amazing. 
                        //NOTE: multiply by 2 'cos I supoused some relation with Nyquist Theorem
                        double xScale = (canvasWavComp.Width * samples) / len * 2;

                        double yScale = (canvasWavComp.Height / (double)(amplitude * 2)) * ((double)amplitude / MAX_AMP);
                        Polyline graphLine = new Polyline();
                                                
                        //This Line is used to move on the x axis
                        Canvas.SetLeft(graphLine, xAxis);                

                        graphLine.Stroke = new SolidColorBrush(Color.FromRgb(244,67,54));
                        graphLine.StrokeThickness = 2;                        
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            graphLine.Points.Add(new Point(xScale * i, (canvasWavComp.Height / 2) - (buffer[i] * yScale)));                                                                                                                                            
                        }                                                                        
                        xAxis += xScale;
                        //canvasWavComp.Children.Clear();
                        canvasWavComp.Children.Add(graphLine);

                    }));
                    double entaux = 0;
                    foreach (var sym in listBuffer.GroupBy(i => i))
                    {
                        NodeHuf nodeHuf = new NodeHuf();
                        nodeHuf.Symbol = sym.Key;
                        nodeHuf.Frequency = sym.Count();
                        nodeHuf.Right = nodeHuf.Left = null;
                        tree.Add(nodeHuf);
                        double prob = (double)nodeHuf.Frequency / samples;
                        //entropy -= prob * (Math.Log(prob) / Math.Log(2));
                        entaux += prob * Math.Log(1 / (prob), 2);
                        entauxlbl = entaux;
                    }
                    entropy += entaux;
                    entcount++;                    
                    tree.Build();


                    //Encode                    
                    System.Collections.BitArray encoded = tree.Encode(auxbuf);
                    byte[] arrayBytes = new byte[encoded.Length / 8 + (encoded.Length % 8 == 0 ? 0 : 1)];
                    encoded.CopyTo(arrayBytes, 0);
                    File.WriteAllBytes("Compress.bin", arrayBytes);

                    //Decode
                    byte[] data;
                    Stream fs = File.OpenRead("Compress.bin");
                    data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                    System.Collections.BitArray bitDec = new System.Collections.BitArray(data);

                    short[] decoded = tree.Decode(bitDec);
                    if (decoded.Length > 0) { ww.Write(decoded, length); }
                    bw.Write(data);
                    fs.Close();
                }
            }
            //Delete temporaly file
            File.Delete("Compress.bin");

            //Close de Stream WAV ww
            ww.Close();

            //If you not add this line, the backgroundworker will be restat but when file is create again 
            //there will be an incongruence because the bw never was closed.
            bw.Close();

            entropy /= entcount;// (len / samples);   
            entcount = 0;
        }
        
        void backWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            //lblProgress.Content = string.Format("{0}% Completed", e.ProgressPercentage);
            tbkProgress.Text = string.Format("{0}% Completed", e.ProgressPercentage);
            lblProgressStatus.Content = string.Format("Progress {0}%", e.ProgressPercentage);
            //lblEntropy.Content = "Entropy: " + entauxlbl.ToString("0.000");
        }

        void backWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //lblProgress.Content = "Complete!";
            tbkProgress.Text = "Complete!";
            tbkInfoStatus.Text = "Everything it's ok";
            tbkProgressStatus.Text = "Complete!";
            butHuff.IsEnabled = true;
            lblEntropy.Content = string.Format("Entropy: {0:0.000} bit per symbol",entropy);
            butRepeat.IsEnabled = false;
            FileInfo fori = new FileInfo(path);
            FileInfo fcom = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/Code.huf");
            lblInfoCompress.Content = string.Format("Wav Size: {0} | Huf Size: {1}. {2}% Compress", BytesToString(fori.Length), BytesToString(fcom.Length), (fcom.Length * 100 / fori.Length));
        }

        static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB"}; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + " " + suf[place];
        }

        private void togglePlay_Checked(object sender, RoutedEventArgs e)
        {
            BassEngine.Instance.Volume((float)sliderVol.Value / 100);
            BassEngine.Instance.Play();
            bmp = BassEngine.Instance.Spectrum(typeSpectrum, spectrum, (int)imgWave.Width, (int)imgWave.Height);
            timer.Start();
        }    


        private void togglePlay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utils.FixTimespan(BassEngine.Instance.ChannelPosition, "MMSS") == Utils.FixTimespan(BassEngine.Instance.TotalTime, "MMSS"))
                return;
            BassEngine.Instance.Pause();
        }

        private void butStop_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.FixTimespan(BassEngine.Instance.ChannelPosition, "MMSS") == Utils.FixTimespan(BassEngine.Instance.TotalTime, "MMSS"))
                return;
            BassEngine.Instance.Stop();
            togglePlay.IsChecked = false;
        }

        private void winMain_Closed(object sender, EventArgs e)
        {
            if (backWorker != null)
            {
                backWorker.CancelAsync();
                ww.Close();
                if (stream != 0) Bass.BASS_Free();
            }
        }

        private void sliderVol_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderVol.IsLoaded)
            {                
                if (sliderVol.Value == 0)
                {
                    if (path != "")BassEngine.Instance.Volume(0.0f);
                    imgVol.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/volzerored.png"));
                }
                else
                {
                    if (path != "") BassEngine.Instance.Volume((float)sliderVol.Value / 100.0f);
                    if (sliderVol.Value > 0 && sliderVol.Value < 30) imgVol.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/vollowred.png"));
                    if (sliderVol.Value >= 30 && sliderVol.Value < 66) imgVol.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/volmidred.png"));                    
                    if (sliderVol.Value >= 66 && sliderVol.Value <= 100) imgVol.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/volmaxred.png"));
                }
            }
        }

        private void toggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            imgHuff.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/visiblered.png"));
            System.Windows.Media.Effects.BlurEffect blur = new System.Windows.Media.Effects.BlurEffect();
            blur.Radius = 15;
            grid011.Effect = blur;
            gridAbout.Effect = blur;
            grid011.Children.Remove(imgHuff);
            grid011.Children.Remove(toggleSwitch);
            grid02.Children.Add(imgHuff);
            grid02.Children.Add(toggleSwitch);
            grid02.Visibility = Visibility.Visible;
        }

        private void toggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            imgHuff.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/hidered.png"));
            grid02.Children.Remove(imgHuff);
            grid02.Children.Remove(toggleSwitch);
            grid011.Children.Add(imgHuff);
            grid011.Children.Add(toggleSwitch);
            grid02.Visibility = Visibility.Hidden;
            grid011.Effect = null;
            gridAbout.Effect = null;
        }

        int typeSpectrum = 1;
        private void imgWave_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (BassEngine.Instance.IsPlaying || togglePlay.IsChecked == false)
                if (typeSpectrum >= 6) typeSpectrum = 1;
                else typeSpectrum++;
        }

        private void canvasTopWin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            string uri = e.Uri.AbsoluteUri;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri));
            e.Handled = true;
        }

        private void butMin_Click(object sender, RoutedEventArgs e)
        {
            winMain.WindowState = WindowState.Minimized;
        }

        private void butClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            if (backWorker.IsBusy) backWorker.CancelAsync();
            Bass.BASS_Free();
        }

        private void butRepeat_Click(object sender, RoutedEventArgs e)
        {
            if (backWorker.IsBusy) backWorker.CancelAsync();
            bw.Close();
            ww.Close();
        }          
                   
       
    }
}
