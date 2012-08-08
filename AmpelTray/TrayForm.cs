﻿using System;
using System.Configuration;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AmpelLib;
using AmpelTray.Properties;

namespace AmpelTray
{
    public class Worker
    {
        private readonly DisableIconDelegate _disableIconDelegate;
        // This method will be called when the thread is started.
        
        public Worker(DisableIconDelegate disableIconDelegate)
        {
            _disableIconDelegate = disableIconDelegate;
        }
        
        public void AmpelLoop()
        {
            var ag = new Ampel();
            var fb = new FBGateway();
            var ampelService = new AmpelService();

            while (!_shouldStop)
            {
                try
                {
                    ampelService.ToggleAmpel(ag, fb.GetAllProjects().FindAll(x => x.Group.Contains(ConfigurationManager.AppSettings.Get("ProjectsMask"))));

                }
                catch
                {
                    // _disableIconDelegate(); // todo create ToggleIconDelegate()
                    ag.Off();
                }

                Thread.Sleep(5000);
                GC.Collect();
            }
            ag.Off();
            Console.WriteLine(Resources.Worker_DoWork_worker_thread__terminating_gracefully_);
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }
        // Volatile is used as hint to the compiler that this data
        // member will be accessed by multiple threads.
        private volatile bool _shouldStop;
    }

    public delegate void DisableIconDelegate();

    public class TrayForm : Form
    {
        [STAThread]
        public static void Main()
        {
            Application.Run(new TrayForm());
        }

        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenu _trayMenu;
        private readonly Icon _iconEnabled = new Icon("ampel.ico");
        private readonly Icon _iconDisabled = new Icon("ampel_disabled.ico");
        private readonly Worker _workerObj;
        private readonly Thread _workerThread;

        public TrayForm()
        {
            _workerObj = new Worker(DisableIcon);
            _workerThread = new Thread(_workerObj.AmpelLoop);
            // Create a simple tray menu with only one item.
            _trayMenu = new ContextMenu();
            _trayMenu.MenuItems.Add("Exit", OnExit);

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.

            _trayIcon = new NotifyIcon
                           {
                               Text = Resources.TrayForm_TrayForm_AmpelTray,
                               Icon = _iconEnabled,
                               ContextMenu = _trayMenu,
                               Visible = true
                           };

            // Add menu to tray icon and show it.
        }

        public void DisableIcon()
        {
            if (_trayIcon != null && _trayIcon.Icon != _iconDisabled)
            {
                _trayIcon.Icon = _iconDisabled;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);

            _workerThread.Start();

        }

        private void OnExit(object sender, EventArgs e)
        {
            _workerObj.RequestStop();
            _workerThread.Join();
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                _trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}
