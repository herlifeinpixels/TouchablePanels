using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Threading;

namespace helloworldTouch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Multitouch.Framework.WPF.Controls.Window
    {
        public MainWindow()
        {
            DataContext = this;
            Photos = new ObservableCollection<string>();
            InitializeComponent();
            //Thread.Sleep(10000);

            Multitouch.Framework.WPF.Controls.Window.BindingGroupProperty.ToString();

            changePhoto();

        }

        public ObservableCollection<string> Photos
        {
            get { return (ObservableCollection<string>)GetValue(PhotosProperty); }
            set { SetValue(PhotosProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Photos.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PhotosProperty =
            DependencyProperty.Register("Photos", typeof(ObservableCollection<string>), typeof(MainWindow));

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            foreach (string photo in Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "*.jpg").Take(5))
            {
                Photos.Add(photo);
            }

            //ThreadStart job = new ThreadStart(changePhoto);
            //Thread thread = new Thread(job);
            //thread.Start();

        }

        public void changePhoto() {
            //Photos.Clear();
            //Thread.Sleep(10000);
            foreach (string photo in Directory.GetFiles("c:\\CRAP", "*.jpg").Take(1))
            {
                Photos.Add(photo);
            }
        }
    }
}
