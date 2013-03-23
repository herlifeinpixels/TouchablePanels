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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using Multitouch.Framework.WPF.Input;
using Multitouch.Framework.Input;
using System.Threading;
using Physics2DDotNet;

using Microsoft.Xna.Framework;

using Microsoft.Kinect;
using System.Runtime.InteropServices;

namespace helloworldTouch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Multitouch.Framework.WPF.Controls.Window
    {
        AirStreamPlayer.Publish newServer;
        private int NUM_PHOTOS = 1;

        KinectSensor nui;

        private Skeleton[] skeletonData;
        private Skeleton skeleton;
        private int skeletonId = -1;

        SkeletonPoint prevHandRight;
        SkeletonPoint prevHandLeft;

        private float prevRightHandXAngle;
        private float prevRightHandYAngle;

        private float prevLeftHandXAngle;
        private float prevLeftHandYAngle;

        private bool inCutting;

        //Animation
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        TimeSpan timerInterval;
        TimeSpan timelinePosition;

        //Used for selection gesture
        bool bInSelect = false;
        float selectStep = 0.0f;
        float selectThreshold = 0.16f;
        bool bGoRight = true;

        //This is for shoot gesture
        private bool bInShoot = false;
        private float shootStep = 0.0f;
        private float shootThreshold = 0.3f;

        //This is for exit gesture
        private bool bInExit = false;
        private float exitStep = 0.0f;
        private float exitThreshold = 0.4f;

        //This is for zoom gesture
        private bool bInZoomIn = false;
        private int originalFrameWidth = 640;
        private int originalFrameHeight = 480;
        private float zoomFactor = 1.0f;



        public MainWindow()
        {
            DataContext = this;
            Photos = new ObservableCollection<string>();
            Thread thread = new Thread(new ThreadStart(startServer));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            
            InitializeComponent();
      //CRAP      InitializeVariables();
      //CRAP      InitializeKinect();
            

            
        }

        private void Raise(string eventName)
        {
            //RoutedEventArgs args = new RoutedEventArgs();
            //args.RoutedEvent = MultitouchScreen.NewContactEvent;
            //RaiseEvent(args);

            //WindowInteropHelper helper = new WindowInteropHelper(this);
            //ContactHandler contactHandler = new ContactHandler(helper.Handle);

            UIElementsList list = MultitouchLogic.Current.UIlist;
            int i = 0; i++;
        }

        private void InitializeVariables()
        {

            inCutting = false;

            timerInterval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
          //  dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick); //CRAP
            dispatcherTimer.Interval = timerInterval;
        }

        private void InitializeKinect()
        {
            nui = (from sensorToCheck in KinectSensor.KinectSensors
                   where sensorToCheck.Status == KinectStatus.Connected
                   select sensorToCheck).FirstOrDefault();


            nui.DepthStream.Enable();
            //nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);
            //nui.SkeletonEngine.TransformSmooth = true;
            //nui.SkeletonStream.SmoothParameters = param;

            TransformSmoothParameters param = new TransformSmoothParameters();
            param.Smoothing = 0.2f;
            param.Correction = 0.0f;
            param.Prediction = 0.0f;
            param.JitterRadius = 0.2f;
            param.MaxDeviationRadius = 0.3f;

            nui.SkeletonStream.Enable(param);
            nui.Start();

            //nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
            nui.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinectSensor_AllFrameReady);
        }

        void startServer()
        {
            newServer = new AirStreamPlayer.Publish();
            newServer.ShowDialog();
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
            foreach (string photo in Directory.GetFiles("c://CRAP", "*.jpg").Take(1))
            {
                Photos.Add(photo);
            }
            WindowInteropHelper helper = new WindowInteropHelper(this);
            ContactHandler contactHandler = new ContactHandler(helper.Handle);
            //contactHandler.ContactRemoved += HandleContact;
            //contactHandler.NewContact += HandleContact;
           // contactHandler.Frame += HandleFrame;

            FileSystemWatcher fsw = new FileSystemWatcher();
            fsw.Path = "c:\\CRAP\\";
            fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fsw.Changed += new FileSystemEventHandler(OnChanged);
            fsw.Created += new FileSystemEventHandler(OnChanged);
            fsw.EnableRaisingEvents = true;
        }

        //public void HandleFrame(object source, FrameEventArgs args){
        //    Raise("julia");
        //}

        public void OnChanged(object source, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() => {
                Photos.Clear();
                foreach (string photo in Directory.GetFiles("c://CRAP", "*.jpg").Take(NUM_PHOTOS)){
                   Photos.Add(photo);
                  }
                base.OnInitialized(e);
                Raise("julia");
            }));
           
            NUM_PHOTOS++;
        }

        void kinectSensor_AllFrameReady(object sender, AllFramesReadyEventArgs e)
        {
            if (this.nui == null || !((KinectSensor)sender).SkeletonStream.IsEnabled)
                return;

            bool haveSkeletonData = false;


            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                if (skeletonFrame != null)
                {
                    if (this.skeletonData == null || this.skeletonData.Length != skeletonFrame.SkeletonArrayLength)
                    {
                        this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);

                    haveSkeletonData = true;
                }
            if (!haveSkeletonData)
                return;

            SortedList<float, int> depthSorted = new SortedList<float, int>();
            foreach (Skeleton data in this.skeletonData)
            {
                if (data.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    float valueZ = data.Position.Z;
                    while (depthSorted.ContainsKey(valueZ))
                    {
                        valueZ += 0.0001f;
                    }
                    depthSorted.Add(valueZ, data.TrackingId);
                }
            }
            if (depthSorted.Count == 0)
                return;

            if (skeletonId == -1 || skeletonId != depthSorted.Values[0])
            {
                nui.SkeletonStream.AppChoosesSkeletons = true;
                skeletonId = depthSorted.Values[0];
                nui.SkeletonStream.ChooseSkeletons(skeletonId);
                //bShowSkeletonTrackVideo = true;
                //SkeletonTrackVideoTimer = 0;
            }

            //Console.WriteLine("Got a skeleton");
            foreach (Skeleton data in this.skeletonData)
            {
                if (skeletonId == data.TrackingId)
                {
                    if (SkeletonTrackingState.Tracked == data.TrackingState)
                    {
                        SkeletonPoint headPos = data.Joints[JointType.Head].Position;
                        SkeletonPoint handRight = data.Joints[JointType.HandRight].Position;
                        SkeletonPoint elbowRight = data.Joints[JointType.ElbowRight].Position;
                        SkeletonPoint shoulderRight = data.Joints[JointType.ShoulderRight].Position;
                        SkeletonPoint hipRight = data.Joints[JointType.HipRight].Position;

                        //cosine law
                        float angleY = (float)Math.Acos((Math.Pow(Vector2.Distance(new Vector2(handRight.X, handRight.Z), new Vector2(elbowRight.X, elbowRight.Z)), 2)
                            + Math.Pow(Vector2.Distance(new Vector2(elbowRight.X, elbowRight.Z), new Vector2(shoulderRight.X, shoulderRight.Z)), 2)
                            - Math.Pow(Vector2.Distance(new Vector2(handRight.X, handRight.Z), new Vector2(shoulderRight.X, shoulderRight.Z)), 2))
                            / (2 * Vector2.Distance(new Vector2(handRight.X, handRight.Z), new Vector2(elbowRight.X, elbowRight.Z))
                            * Vector2.Distance(new Vector2(elbowRight.X, elbowRight.Z), new Vector2(shoulderRight.X, shoulderRight.Z))));

                        //cosine law
                        float angleX = (float)Math.Acos((Math.Pow(Vector2.Distance(new Vector2(handRight.Y, handRight.Z), new Vector2(shoulderRight.Y, shoulderRight.Z)), 2)
                            + Math.Pow(Vector2.Distance(new Vector2(shoulderRight.Y, shoulderRight.Z), new Vector2(hipRight.Y, hipRight.Z)), 2)
                            - Math.Pow(Vector2.Distance(new Vector2(handRight.Y, handRight.Z), new Vector2(hipRight.Y, hipRight.Z)), 2))
                            / (2 * Vector2.Distance(new Vector2(handRight.Y, handRight.Z), new Vector2(shoulderRight.Y, shoulderRight.Z))
                            * Vector2.Distance(new Vector2(shoulderRight.Y, shoulderRight.Z), new Vector2(hipRight.Y, hipRight.Z))));

                        float angleShoulderY = (float)Math.Acos((Math.Pow(Vector2.Distance(new Vector2(handRight.X, handRight.Z), new Vector2(shoulderRight.X, shoulderRight.Z)), 2)
                            + Math.Pow(Vector2.Distance(new Vector2(shoulderRight.X, shoulderRight.Z), new Vector2(hipRight.X, hipRight.Z)), 2)
                            - Math.Pow(Vector2.Distance(new Vector2(handRight.X, handRight.Z), new Vector2(hipRight.X, hipRight.Z)), 2))
                            / (2 * Vector2.Distance(new Vector2(handRight.X, handRight.Z), new Vector2(shoulderRight.X, shoulderRight.Z))
                            * Vector2.Distance(new Vector2(shoulderRight.X, shoulderRight.Z), new Vector2(hipRight.X, hipRight.Z))));

                        //Left hand - zoom
                        SkeletonPoint handLeft = data.Joints[JointType.HandLeft].Position;
                        SkeletonPoint elbowLeft = data.Joints[JointType.ElbowLeft].Position;
                        SkeletonPoint shoulderLeft = data.Joints[JointType.ShoulderLeft].Position;
                        SkeletonPoint hipLeft = data.Joints[JointType.HipLeft].Position;

                        //cosine law
                        float angleLeftY = (float)Math.Acos((Math.Pow(Vector2.Distance(new Vector2(handLeft.X, handLeft.Z), new Vector2(elbowLeft.X, elbowLeft.Z)), 2)
                            + Math.Pow(Vector2.Distance(new Vector2(elbowLeft.X, elbowLeft.Z), new Vector2(shoulderLeft.X, shoulderLeft.Z)), 2)
                            - Math.Pow(Vector2.Distance(new Vector2(handLeft.X, handLeft.Z), new Vector2(shoulderLeft.X, shoulderLeft.Z)), 2))
                            / (2 * Vector2.Distance(new Vector2(handLeft.X, handLeft.Z), new Vector2(elbowLeft.X, elbowLeft.Z))
                            * Vector2.Distance(new Vector2(elbowLeft.X, elbowLeft.Z), new Vector2(shoulderLeft.X, shoulderLeft.Z))));

                        //cosine law
                        float angleLeftX = (float)Math.Acos((Math.Pow(Vector2.Distance(new Vector2(handLeft.Y, handLeft.Z), new Vector2(shoulderLeft.Y, shoulderLeft.Z)), 2)
                            + Math.Pow(Vector2.Distance(new Vector2(shoulderLeft.Y, shoulderLeft.Z), new Vector2(hipLeft.Y, hipLeft.Z)), 2)
                            - Math.Pow(Vector2.Distance(new Vector2(handLeft.Y, handLeft.Z), new Vector2(hipLeft.Y, hipLeft.Z)), 2))
                            / (2 * Vector2.Distance(new Vector2(handLeft.Y, handLeft.Z), new Vector2(shoulderLeft.Y, shoulderLeft.Z))
                            * Vector2.Distance(new Vector2(shoulderLeft.Y, shoulderLeft.Z), new Vector2(hipLeft.Y, hipLeft.Z))));

                        float currRightHandXAngle = getAngleTrig(handRight.X, handRight.Z);
                        float currRightHandYAngle = getAngleTrig(handRight.Y, handRight.Z);

                        float currLeftHandXAngle = getAngleTrig(handLeft.X, handLeft.Z);
                        float currLeftHandYAngle = getAngleTrig(handLeft.Y, handLeft.Z);

                        //angleY is the angle of elbow, angleX is the angle of arm and spine

                        ////Zoom
                        //if (MathHelper.ToDegrees(angleY) > 145.0f && MathHelper.ToDegrees(angleX) > 30.0f && MathHelper.ToDegrees(angleX) < 150.0f &&
                        //    MathHelper.ToDegrees(angleLeftY) > 145.0f && MathHelper.ToDegrees(angleLeftX) > 30.0f && MathHelper.ToDegrees(angleLeftX) < 150.0f)
                        //{
                        //    float currHandDistance = Vector2.Distance(new Vector2(handRight.X, handRight.Y), new Vector2(handLeft.X, handLeft.Y));
                        //    float prevHandDistance = Vector2.Distance(new Vector2(prevHandRight.X, prevHandRight.Y), new Vector2(prevHandLeft.X, prevHandLeft.Y));

                        //    //float newZoomLevel = (float)map.ZoomLevel - ((prevHandDistance - currHandDistance));
                        //    //if (newZoomLevel > initialZoom)
                        //    //    map.ZoomLevel = newZoomLevel;
                        //    //else
                        //    //    map.ZoomLevel = initialZoom;
                        //    if (prevHandDistance > currHandDistance) //zoom out
                        //    {
                        //        if (!bInZoomIn) //exit
                        //        {
                        //            if (!bInExit)
                        //            {
                        //                exitStep = 0;
                        //                bInExit = true;
                        //            }

                        //            if (bInExit)
                        //            {
                        //                exitStep += prevHandDistance - currHandDistance;
                        //                if (exitStep > exitThreshold)
                        //                {
                        //                    changeStatus(ProgramStatus.inSelection, 0);
                        //                }
                        //            }
                        //        }
                        //        else if ((status == ProgramStatus.inPicture) && bInZoomIn) //zoom out
                        //        {
                        //            zoomFactor += (prevHandDistance - currHandDistance) * 0.01f;
                        //            pictureFrame.Width /= zoomFactor;
                        //            pictureFrame.Height /= zoomFactor;
                        //            if (pictureFrame.Width < originalFrameWidth)
                        //            {
                        //                bInZoomIn = false;
                        //                zoomFactor = 1.0f;
                        //                changeStatus(ProgramStatus.inSelection, 0);
                        //            }
                        //        }
                        //    }
                        //    else if (prevHandDistance <= currHandDistance) //zoom in
                        //    {
                        //        if (status == ProgramStatus.inPicture)
                        //        {
                        //            zoomFactor = 1 + (currHandDistance - prevHandDistance) * 2.0f;
                        //            pictureFrame.Width *= zoomFactor;
                        //            pictureFrame.Height *= zoomFactor;
                        //            bInZoomIn = true;
                        //        }
                        //    }

                        //}
                        //else
                        //{

                        //    //Select picture or movie
                        //    //if (MathHelper.ToDegrees(angleY) > 120.0f && MathHelper.ToDegrees(angleX) > 30.0f && MathHelper.ToDegrees(angleX) < 150.0f && MathHelper.ToDegrees(angleShoulderY) < 80 && MathHelper.ToDegrees(angleShoulderY) > 10)
                        //    if (MathHelper.ToDegrees(angleY) > 120.0f)
                        //    {
                        //        if (status == ProgramStatus.inSelection)
                        //        {
                        //            if (!bInSelect)
                        //            {
                        //                selectStep = 0;
                        //                bInSelect = true;
                        //                if (handRight.X > prevHandRight.X)
                        //                    bGoRight = true;
                        //                else if (handRight.X < prevHandRight.X)
                        //                    bGoRight = false;
                        //            }

                        //            if (bInSelect)
                        //            {
                        //                if (handRight.X > prevHandRight.X) //go right
                        //                {
                        //                    if (bGoRight) //continue
                        //                    {
                        //                        selectStep += (handRight.X - prevHandRight.X);
                        //                        if (selectStep > selectThreshold)
                        //                        {
                        //                            if (coverFlow1.SelectedIndex > 0)
                        //                                coverFlow1.SelectedIndex -= 1;

                        //                            selectStep = 0;
                        //                        }
                        //                    }
                        //                    else //user change from left to right 
                        //                    {
                        //                        bGoRight = true;
                        //                        selectStep = (handRight.X - prevHandRight.X);
                        //                        if (selectStep > selectThreshold)
                        //                        {
                        //                            if (coverFlow1.SelectedIndex > 0)
                        //                                coverFlow1.SelectedIndex -= 1;

                        //                            selectStep = 0;
                        //                        }
                        //                    }
                        //                }
                        //                else if (handRight.X < prevHandRight.X) //go left
                        //                {
                        //                    if (!bGoRight)
                        //                    {
                        //                        selectStep += (prevHandRight.X - handRight.X);
                        //                        if (selectStep > selectThreshold)
                        //                        {

                        //                            if (coverFlow1.SelectedIndex < coverFlowItemCount - 1)
                        //                                coverFlow1.SelectedIndex += 1;
                        //                            selectStep = 0;

                        //                        }
                        //                    }
                        //                    else //from right to left
                        //                    {
                        //                        bGoRight = false;
                        //                        selectStep = (prevHandRight.X - handRight.X);
                        //                        if (selectStep > selectThreshold)
                        //                        {

                        //                            if (coverFlow1.SelectedIndex < coverFlowItemCount - 1)
                        //                                coverFlow1.SelectedIndex += 1;
                        //                            selectStep = 0;


                        //                        }

                        //                    }
                        //                }
                        //            }
                        //        }
                        //        else if (status == ProgramStatus.inVideo)
                        //        {

                        //        }
                        //        else if (status == ProgramStatus.inPicture)
                        //        {

                        //        }

                        //    }
                        //    else
                        //    {
                        //        bInSelect = false;
                        //        selectStep = 0;
                        //    }

                        //    //Shoot, choose picture or video
                        //    if (MathHelper.ToDegrees(angleX) > 70.0f && MathHelper.ToDegrees(angleX) < 170.0f)
                        //    {
                        //        if (handRight.Y >= prevHandRight.Y)
                        //        {
                        //            if (!bInShoot)
                        //            {
                        //                shootStep = 0;
                        //                bInShoot = true;
                        //            }


                        //            if (bInShoot)
                        //            {
                        //                shootStep += (handRight.Y - prevHandRight.Y);
                        //                if (shootStep > shootThreshold)
                        //                {
                        //                    SelectItem();
                        //                    bInShoot = false;
                        //                    shootStep = 0;
                        //                }
                        //            }
                        //        }
                        //        else
                        //        {
                        //            bInShoot = false;
                        //            shootStep = 0;
                        //        }
                        //    }
                        //}

                        //

                        prevHandRight = handRight;
                        prevRightHandYAngle = currRightHandYAngle;
                        prevRightHandXAngle = currRightHandXAngle;

                        prevHandLeft = handLeft;
                        prevLeftHandXAngle = currLeftHandXAngle;
                        prevLeftHandYAngle = currLeftHandYAngle;
                    }
                    else
                    {
                        skeletonId = -1;
                        //playerId = -1;
                        nui.SkeletonStream.AppChoosesSkeletons = false;
                    }
                    break;
                }

            }


        }

        private float getAngleTrig(float x, float y)
        {
            if (x == 0.0f)
            {
                if (y == 0.0f)
                    return (float)((3 * Math.PI) / 2.0f);
                else
                    return (float)(Math.PI / 2.0f);
            }
            else if (y == 0.0f)
            {
                if (x < 0)
                    return (float)Math.PI;
                else
                    return 0;
            }
            if (y > 0.0f)
            {
                if (x > 0.0f)
                    return (float)Math.Atan(y / x);
                else
                    return (float)(Math.PI - Math.Atan(y / -x));
            }
            else
            {
                if (x > 0.0f)
                    return (float)(2.0f * Math.PI - Math.Atan(-y / x));
                else
                {
                    return (float)(Math.PI + (float)Math.Atan(-y / -x));
                }
            }
        }
    }
}
