// Project: MapleSeedU
// File: MainWindow.xaml.cs
// Updated By: Scott Stamp <scott@hypermine.com>
// Updated On: 01/30/2017

using MapleSeedU.ViewModels;
using System;
using System.ComponentModel;
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

        private enum faceButton { A, B, X, Y };

        private faceButton launchButton = faceButton.A;

        public MainWindow()
        {
            InitializeComponent();

            pollingWorker.WorkerSupportsCancellation = true;
            pollingWorker.DoWork += new DoWorkEventHandler(pollingWorker_DoWork);
        }

        private void pollingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel)
            {
                if (reporterState.Poll()) Dispatcher.Invoke(new Action(UpdateState));
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
            }
        }

        private void FaceButtonPress(faceButton button)
        {
            if (button == launchButton)
            {
                var viewModel = (MainWindowViewModel)DataContext;
                if (viewModel.PlayTitleCommand.CanExecute(null))
                    viewModel.PlayTitleCommand.Execute(null);
            }
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            pollingWorker.RunWorkerAsync();
        }
    }
}