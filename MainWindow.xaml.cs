using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using System.Xml.Serialization;

using Paint.DataFromNN;
using NNLib;

namespace Paint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer dispatcherTimer;

        private bool painting = true;
        private readonly Probabilities probabilities;
        private NeuralNetwork net = null;

        

        public MainWindow()
        {
            InitializeComponent();

            Canvas1.DefaultDrawingAttributes.Width = 20;
            Canvas1.DefaultDrawingAttributes.Height = 20;
            Canvas1.DefaultDrawingAttributes.Color = Color.FromRgb(255, 255, 255);

            //  DispatcherTimer setup
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0,0,0,0,100);
            dispatcherTimer.Start();

            probabilities = (Probabilities) (Resources["probabilities"]);
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            Canvas1.Strokes.Clear();
        }


        private void Switch(object sender, RoutedEventArgs e)
        {
            if (painting)
            {
                painting = false;
                SwitchButton.Content = "Brush";
                Canvas1.DefaultDrawingAttributes.Color = Color.FromRgb(0, 0, 0);
            }
            else
            {
                painting = true;
                SwitchButton.Content = "Rubber";
                Canvas1.DefaultDrawingAttributes.Color = Color.FromRgb(255, 255, 255);
            }
        }

        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (net != null)
            {
                // copies the content of the InkCanvas 
                var bmpCopied = new RenderTargetBitmap(28, 28, 96, 96, PixelFormats.Default);
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    VisualBrush vb = new VisualBrush(Canvas1);
                    dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(28, 28)));
                }
                bmpCopied.Render(dv);
                
                // prepares all the arrays before converting to tensor
                int stride = bmpCopied.PixelWidth * (bmpCopied.Format.BitsPerPixel / 8);
                byte[] pixels = new byte[(int)bmpCopied.PixelHeight * stride];
                Tensor result = null;
                bmpCopied.CopyPixels(pixels, stride, 0);
                var pHeight = bmpCopied.PixelHeight;
                var pWidth = bmpCopied.PixelWidth;

                await Task.Run(() =>
                {
                    // the data are drawn only from the first channel
                    // since the only colors used are black and white
                    // the values in all the channels are either 0 or 255
                    // there are 4 channels, and we create 1 channel tensor
                    // therefore it suffices to copy every fourth pixel
                    
                    double[] realPixels = new double[pHeight* pWidth];
                    for (int i = 0; i < pixels.Length; i++)
                        if (i % 4 == 0)
                            realPixels[i / 4] = pixels[i] > 100 ? 1 : 0;
                    double[][] image = new double[1][];
                    image[0] = realPixels;

                    result = net.Predict(new Tensor(1, 28, 28, image));
                });

                // probabilities are bound to the histogram elements on
                // the wpf, therefore the probs are copied into probabilities
                // which reflects its changes to images the user sees
                var probs = new double[10];
                for (int i = 0; i < 10; i++)
                    probs[i] = result[0, 0, i, 0];

                probabilities.ChangeValues(probs);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".net";
            dlg.Filter = "net Files (*.net)|*.net";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and load the network
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                var serializer = new XmlSerializer(typeof(NeuralNetwork));
                using (var reader = new StreamReader(filename))
                    net = (NeuralNetwork)serializer.Deserialize(reader);

                ProbPanel.Visibility = Visibility.Visible;
            }
        }
    }
}
