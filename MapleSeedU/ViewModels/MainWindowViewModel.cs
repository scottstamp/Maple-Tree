// Project: MapleSeedU
// File: Presenter.cs
// Updated By: Jared
// Updated By: Scott Stamp <scott@hypermine.com>
// Updated On: 01/30/2017

#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MapleSeedU.Models;
using MapleSeedU.Models.Settings;
using System.ComponentModel;
using XInputDotNetPure;
using System.Diagnostics;

#endregion

namespace MapleSeedU.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public static MainWindowViewModel Instance;

        public readonly string DbFile = Path.Combine(Path.GetTempPath(), "MapleTree.json");

        private string _status = "Github.com/Tsume/Maple-Tree";

        private TitleInfoEntry _titleInfoEntry;

        private ReporterState reporterState = new ReporterState(); // Game Controller reporter

        private static BackgroundWorker pollingWorker = new BackgroundWorker(); // Polling worker for game controller

        private DateTime lastInput = DateTime.Now; // DateTime var storing last input time (debouncing)

        private enum dpadButton { Up, Down, Left, Right };

        private enum faceButton { A, B, X, Y, Guide, Start, Back, LeftStick, RightStick, LeftShoulder, RightShoulder };

        private faceButton launchButton = faceButton.A;

        private bool isClosing;

        public MainWindowViewModel()
        {
            var dispatcherTimer = new DispatcherTimer(TimeSpan.Zero,
                DispatcherPriority.ApplicationIdle, OnLoadComplete,
                Application.Current.Dispatcher);

            if (Instance == null)
                Instance = this;

            pollingWorker.WorkerSupportsCancellation = true;
            pollingWorker.DoWork += new DoWorkEventHandler(pollingWorker_DoWork);

            UpdateDatabase();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            isClosing = true;
            pollingWorker.CancelAsync();
        }

        private void pollingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel && !isClosing)
            {
                if (reporterState.Poll()) { Application.Current.Dispatcher.Invoke(new Action(UpdateState)); }
            }
        }

        private void UpdateState()
        {
            var state = reporterState.LastActiveState;

            // 250ms debouncing for controller inputs
            if ((DateTime.Now - lastInput).Milliseconds > 250)
            {
                if (state.DPad.Up == ButtonState.Pressed) DPadButtonPress(dpadButton.Up);
                if (state.DPad.Down == ButtonState.Pressed) DPadButtonPress(dpadButton.Down);
                if (state.DPad.Left == ButtonState.Pressed) DPadButtonPress(dpadButton.Left);
                if (state.DPad.Right == ButtonState.Pressed) DPadButtonPress(dpadButton.Right);

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

        private void DPadButtonPress(dpadButton button)
        {
            var idx = TitleInfoEntries.IndexOf(TitleInfoEntry);

            if (button == dpadButton.Down)
                if (idx < TitleInfoEntries.Count - 1)
                    TitleInfoEntry = TitleInfoEntries[idx + 1];
            if (button == dpadButton.Up)
                if (idx > 0)
                    TitleInfoEntry = TitleInfoEntries[idx - 1];

            RaisePropertyChangedEvent("TitleInfoEntry");

            lastInput = DateTime.Now;
        }

        private void FaceButtonPress(faceButton button)
        {
            if (button == launchButton)
            {
                PlayTitle();
            }

            if (button == faceButton.Guide)
            {
                var fileName = Path.GetFileName(CemuPath.GetPath()).Replace(".exe", "");
                foreach (Process cemuProcess in Process.GetProcessesByName(fileName)) cemuProcess.Kill();
            }

            lastInput = DateTime.Now;
        }

        private void Reset()
        {
            CemuPath.ResetPath();
            LibraryPath.ResetPath();
        }

        public string Status {
            get { return _status; }
            set {
                _status = value;
                RaisePropertyChangedEvent("Status");
            }
        }

        public string LogText { get; set; }

        public CemuPath CemuPath => new CemuPath();

        private LibraryPath LibraryPath => new LibraryPath();

        public List<TitleInfoEntry> TitleInfoEntries => TitleInfoEntry.Entries;

        public TitleInfoEntry TitleInfoEntry {
            get { return _titleInfoEntry; }
            set {
                if (_titleInfoEntry == value) return;
                _titleInfoEntry = value;
                value?.UpdateTheme();
                RaisePropertyChangedEvent("TitleInfoEntry");
                RaisePropertyChangedEvent("BackgroundImage");
            }
        }

        public BitmapSource BackgroundImage => ImageAnalysis.FromBytes(TitleInfoEntry?.BootTex);

        public ICommand ResetCommand => new CommandHandler(Reset);

        public ICommand PlayTitleCommand => new CommandHandler(PlayTitle);

        public ICommand CacheUpdateCommand => new CommandHandler(async () => await ThemeUpdate(true));

        public bool CacheUpdateEnabled { get; set; } = true;

        private int _progressBarMax { get; set; } = 100;

        public int ProgressBarMax {
            get { return _progressBarMax; }
            set {
                _progressBarMax = value;
                RaisePropertyChangedEvent("ProgressBarMax");
            }
        }

        private int _progressBarCurrent { get; set; } = 100;

        public int ProgressBarCurrent {
            get { return _progressBarCurrent; }
            set {
                _progressBarCurrent = value;
                RaisePropertyChangedEvent("ProgressBarCurrent");
            }
        }

        private void LockControls()
        {
            CacheUpdateEnabled = false;
            RaisePropertyChangedEvent("CacheUpdateEnabled");
        }

        private void UnlockControls()
        {
            CacheUpdateEnabled = true;
            RaisePropertyChangedEvent("CacheUpdateEnabled");
            RaisePropertyChangedEvent("TitleInfoEntries");
        }

        private static async void OnLoadComplete(object sender, EventArgs e)
        {
            (sender as DispatcherTimer)?.Stop();

            pollingWorker.RunWorkerAsync();

            await Instance.ThemeUpdate();
        }

        private void UpdateDatabase()
        {
            if (File.Exists(DbFile) && DateTime.Now <= new FileInfo(DbFile).LastWriteTime.AddDays(1)) return;

            const string url = "https://wiiu.titlekeys.com/json";

            using (var wc = new WebClient()) {
                wc.DownloadFile(new Uri(url), DbFile);
            }
        }

        public static void WriteLine(string text)
        {
            Instance.LogText += $"[{DateTime.Now:T}] {text} {Environment.NewLine}";
            Instance.RaisePropertyChangedEvent("LogText");
        }

        private void PlayTitle()
        {
            TitleInfoEntry.PlayTitle();
        }

        private async Task LoadCache()
        {
            ProgressBarCurrent = 0;
            LockControls();

            string tempPath;
            if (!Directory.Exists(tempPath = Path.Combine(Path.GetTempPath(), "MapleTree")))
                Directory.CreateDirectory(tempPath);

            await Task.Run(() => {
                var files = Directory.GetFiles(tempPath);
                ProgressBarMax = files.Length;

                var list = new TitleInfoEntry[ProgressBarMax = files.Length];

                for (var i = 0; i < files.Length; i++)
                    try {
                        var file = files[i];
                        list[i] = TitleInfoEntry.LoadCache(file, true);
                        list[i].IsCached = true;
                        ProgressBarCurrent++;
                    }
                    catch (Exception e) {
                        WriteLine(e.StackTrace);
                    }

                TitleInfoEntry.Entries = new List<TitleInfoEntry>(list);
                TitleInfoEntry.Entries.Sort((t1, t2) => string.Compare(t1.Name, t2.Name, StringComparison.Ordinal));
            });

            UnlockControls();
        }

        private async Task ThemeUpdate(bool forceUpdate = false)
        {
            await Instance.LoadCache();

            ProgressBarCurrent = 0;
            LockControls();

            var files = Directory.GetFiles(LibraryPath.GetPath(), "*.rpx", SearchOption.AllDirectories);

            await Task.Run(() => {

                var list = new TitleInfoEntry[ProgressBarMax = files.Length];

                for (var i = 0; i < files.Length; i++) {
                    var file = files[i];

                    if (TitleInfoEntry.Entries == null) continue;
                    var entries = TitleInfoEntry.Entries.FindAll(entry => entry.Raw == file).Count;
                    if (entries > 0) continue;

                    list[i] = TitleInfoEntry.LoadCache(file, false);
                    TitleInfoEntry.Entries?.Add(list[i]);

                    ProgressBarCurrent++;
                }

                ProgressBarCurrent = 0;
                if (TitleInfoEntry.Entries == null) return;
                foreach (var t in TitleInfoEntry.Entries) {
                    if (forceUpdate) t.CacheTheme(true);
                    ProgressBarCurrent++;
                }
            });

            TitleInfoEntry = TitleInfoEntry.Entries[0];
            UnlockControls();

            Status = $"Library Path = {LibraryPath.GetPath()}";
        }
    }
}