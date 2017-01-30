// Project: MapleSeedU
// File: MainWindow.xaml.cs
// Updated By: Scott Stamp <scott@hypermine.com>
// Updated On: 01/30/2017

using MapleSeedU.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using XInputDotNetPure;

namespace MapleSeedU.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ReporterState reporterState = new ReporterState(); // Game Controller reporter

        private static BackgroundWorker pollingWorker = new BackgroundWorker(); // Polling worker for game controller

        private DateTime lastInput = DateTime.Now; // DateTime var storing last input time (debouncing)

        private enum faceButton { A, B, X, Y, Guide, Start, Back, LeftStick, RightStick, LeftShoulder, RightShoulder };

        private faceButton launchButton = faceButton.A;

        private bool isClosing;

        public MainWindow()
        {
            InitializeComponent();

            pollingWorker.WorkerSupportsCancellation = true;
            pollingWorker.DoWork += new DoWorkEventHandler(pollingWorker_DoWork);
        }

        private void pollingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel && !isClosing)
            {
                if (reporterState.Poll()) { Dispatcher.Invoke(new Action(UpdateState)); }
            }
        }

        private void UpdateState()
        {
            var state = reporterState.LastActiveState;

            if ((DateTime.Now - lastInput).Milliseconds > 250)
            {
                if (state.DPad.Up == ButtonState.Pressed) // D-Pad Up
                {
                    if (cmbGames.SelectedIndex > 0)
                        cmbGames.SelectedIndex--;

                    lastInput = DateTime.Now;
                }

                if (state.DPad.Down == ButtonState.Pressed) // D-Pad Down
                {
                    if (cmbGames.SelectedIndex < cmbGames.Items.Count)
                        cmbGames.SelectedIndex++;

                    lastInput = DateTime.Now;
                }

                if (state.Buttons.A == ButtonState.Pressed) FaceButtonPress(faceButton.A);
                if (state.Buttons.B == ButtonState.Pressed) FaceButtonPress(faceButton.B);
                if (state.Buttons.X == ButtonState.Pressed) FaceButtonPress(faceButton.X);
                if (state.Buttons.Y == ButtonState.Pressed) FaceButtonPress(faceButton.Y);
                // use this to exit Cemu, it's the "Xbox" button on a XBone controller
                if (state.Buttons.Guide == ButtonState.Pressed) FaceButtonPress(faceButton.Guide);
                if (state.Buttons.Start == ButtonState.Pressed) FaceButtonPress(faceButton.Start);
                if (state.Buttons.Back == ButtonState.Pressed) FaceButtonPress(faceButton.Back);
                if (state.Buttons.LeftStick == ButtonState.Pressed) FaceButtonPress(faceButton.LeftStick);
                if (state.Buttons.RightStick == ButtonState.Pressed) FaceButtonPress(faceButton.RightStick);
                if (state.Buttons.LeftShoulder == ButtonState.Pressed) FaceButtonPress(faceButton.LeftShoulder);
                if (state.Buttons.RightShoulder == ButtonState.Pressed) FaceButtonPress(faceButton.RightShoulder);
            }
        }

        private void FaceButtonPress(faceButton button)
        {
            var viewModel = (MainWindowViewModel)DataContext;
            if (button == launchButton)
            {
                if (viewModel.PlayTitleCommand.CanExecute(null))
                    viewModel.PlayTitleCommand.Execute(null);
            }

            if (button == faceButton.Guide)
            {
                var fileName = Path.GetFileName(viewModel.CemuPath.GetPath()).Replace(".exe", "");
                foreach (Process cemuProcess in Process.GetProcessesByName(fileName)) cemuProcess.Kill();
            }

            lastInput = DateTime.Now;
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            pollingWorker.RunWorkerAsync();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            isClosing = true;
            pollingWorker.CancelAsync();
        }
    }
}