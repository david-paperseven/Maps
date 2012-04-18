using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using Maps.Helpers;

namespace Maps
{
    public partial class App : Application
    {
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            // Note that exceptions thrown by ApplicationBarItem.Click will not get caught here.
            UnhandledException += Application_UnhandledException;

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are being GPU accelerated with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;
            }

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            SaveState.Init();
            PersistentStorage.Init();
            Tombstone.Init();

            LoadPersistentStorage();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            SaveState.Init();
            PersistentStorage.Init();
            Tombstone.Init();

            LoadPersistentStorage();
            LoadTombstoneData();

        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            Tombstone.Instance.Tombstoning();
            SaveTombstoneData();
            SavePersistentStorage();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            SavePersistentStorage();
        }

        private void SaveTombstoneData()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = new IsolatedStorageFileStream("tombstone.txt",
                                                            FileMode.Create,
                                                            FileAccess.Write,
                                                            store))
            {
                var serializer = new XmlSerializer(typeof(Tombstone));
                serializer.Serialize(stream, Tombstone.Instance);
            }
        }

        private void LoadTombstoneData()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = new IsolatedStorageFileStream("tombstone.txt", FileMode.OpenOrCreate, FileAccess.Read, store))
            using (var reader = new StreamReader(stream))
            {
                if (!reader.EndOfStream)
                {
                    var serializer = new XmlSerializer(typeof(Tombstone));
                    Tombstone.Instance.SetState((Tombstone)serializer.Deserialize(reader));
                }
            }
        }

        private void SavePersistentStorage()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = new IsolatedStorageFileStream("persistent.txt",
                                                            FileMode.Create,
                                                            FileAccess.Write,
                                                            store))
            {
                var serializer = new XmlSerializer(typeof(PersistentStorage));
                serializer.Serialize(stream, PersistentStorage.Instance);
            }
        }

        private void LoadPersistentStorage()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = new IsolatedStorageFileStream("persistent.txt", FileMode.OpenOrCreate, FileAccess.Read, store))
            using (var reader = new StreamReader(stream))
            {
                if (!reader.EndOfStream)
                {
                    var serializer = new XmlSerializer(typeof(PersistentStorage));
                    PersistentStorage.Instance.SetState((PersistentStorage)serializer.Deserialize(reader));
                }
                else
                {
                    PersistentStorage.Instance.Reset();
                }
            }
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        private void SliderButtonClick(object sender, RoutedEventArgs e)
        {
            SaveState.Instance.appgenparameters.SliderDelta(0);
        }

        private void Slider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            SaveState.Instance.appgenparameters.SliderDelta(e.HorizontalChange);
        }
    }
}