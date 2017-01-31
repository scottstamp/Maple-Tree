// Project: MapleSeedU
// File: Presenter.cs
// Updated By: Jared
// Updated By: Scott Stamp <scott@hypermine.com>
// Updated On: 01/30/2017

#region usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MapleSeedL.Models;
using MapleSeedL.Models.Settings;
using MapleSeedL.Models.XInput;
using XInputDotNetPure;

#endregion

namespace MapleSeedL.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public static MainWindowViewModel Instance;

        // Polling worker for game controller
        private static readonly BackgroundWorker PollingWorker = new BackgroundWorker();

        // Game Controller reporter
        private readonly ReporterState _reporterState = new ReporterState();

        // DateTime var storing last input time (debouncing)
        private DateTime _lastInput = DateTime.Now;

        public readonly string DbFile = Path.Combine(Path.GetTempPath(), "MapleTree.json");

        private readonly FaceButton launchButton = FaceButton.A;

        private bool _isClosing;

        private string _status = "Github.com/Tsume/Maple-Tree";

        private TitleInfoEntry _titleInfoEntry;

        public MainWindowViewModel()
        {
            new DispatcherTimer(TimeSpan.Zero,
                DispatcherPriority.ApplicationIdle, OnLoadComplete,
                Application.Current.Dispatcher);

            if (Instance == null)
                Instance = this;

            PollingWorker.WorkerSupportsCancellation = true;
            PollingWorker.DoWork += pollingWorker_DoWork;

            UpdateDatabase();
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                RaisePropertyChangedEvent("Status");
            }
        }

        public string LogText { get; set; }

        public CemuPath CemuPath => new CemuPath();

        private LibraryPath LibraryPath => new LibraryPath();

        public List<TitleInfoEntry> TitleInfoEntries => TitleInfoEntry.Entries;

        public TitleInfoEntry TitleInfoEntry
        {
            get { return _titleInfoEntry; }
            set
            {
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

        public int ProgressBarMax
        {
            get { return _progressBarMax; }
            set
            {
                _progressBarMax = value;
                RaisePropertyChangedEvent("ProgressBarMax");
            }
        }

        private int _progressBarCurrent { get; set; } = 100;

        public int ProgressBarCurrent
        {
            get { return _progressBarCurrent; }
            set
            {
                _progressBarCurrent = value;
                RaisePropertyChangedEvent("ProgressBarCurrent");
            }
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _isClosing = true;
            PollingWorker.CancelAsync();
        }

        private void pollingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel && !_isClosing)
                if (_reporterState.Poll()) Application.Current.Dispatcher.Invoke(UpdateState);
        }

        private void UpdateState()
        {
            var state = _reporterState.LastActiveState;

            // 250ms debouncing for controller inputs
            if ((DateTime.Now - _lastInput).Milliseconds > 250)
            {
                if (state.DPad.Up == ButtonState.Pressed) DPadButtonPress(DpadButton.Up);
                if (state.DPad.Down == ButtonState.Pressed) DPadButtonPress(DpadButton.Down);
                if (state.DPad.Left == ButtonState.Pressed) DPadButtonPress(DpadButton.Left);
                if (state.DPad.Right == ButtonState.Pressed) DPadButtonPress(DpadButton.Right);

                if (state.Buttons.A == ButtonState.Pressed) FaceButtonPress(FaceButton.A);
                if (state.Buttons.B == ButtonState.Pressed) FaceButtonPress(FaceButton.B);
                if (state.Buttons.X == ButtonState.Pressed) FaceButtonPress(FaceButton.X);
                if (state.Buttons.Y == ButtonState.Pressed) FaceButtonPress(FaceButton.Y);

                // use this to exit Cemu, it's the "Xbox" button on a XBone controller
                if (state.Buttons.Guide == ButtonState.Pressed) FaceButtonPress(FaceButton.Guide);
                if (state.Buttons.Start == ButtonState.Pressed) FaceButtonPress(FaceButton.Start);
                if (state.Buttons.Back == ButtonState.Pressed) FaceButtonPress(FaceButton.Back);

                if (state.Buttons.LeftStick == ButtonState.Pressed) FaceButtonPress(FaceButton.LeftStick);
                if (state.Buttons.RightStick == ButtonState.Pressed) FaceButtonPress(FaceButton.RightStick);
                if (state.Buttons.LeftShoulder == ButtonState.Pressed) FaceButtonPress(FaceButton.LeftShoulder);
                if (state.Buttons.RightShoulder == ButtonState.Pressed) FaceButtonPress(FaceButton.RightShoulder);
            }
        }

        private void DPadButtonPress(DpadButton button)
        {
            var idx = TitleInfoEntries.IndexOf(TitleInfoEntry);

            if (button == DpadButton.Down)
                if (idx < TitleInfoEntries.Count - 1)
                    TitleInfoEntry = TitleInfoEntries[idx + 1];
            if (button == DpadButton.Up)
                if (idx > 0)
                    TitleInfoEntry = TitleInfoEntries[idx - 1];

            RaisePropertyChangedEvent("TitleInfoEntry");

            _lastInput = DateTime.Now;
        }

        private void FaceButtonPress(FaceButton button)
        {
            if (button == launchButton)
                PlayTitle();

            if (button == FaceButton.Guide)
            {
                var fileName = Path.GetFileName(CemuPath.GetPath())?.Replace(".exe", "");
                foreach (var cemuProcess in Process.GetProcessesByName(fileName)) cemuProcess.Kill();
            }

            _lastInput = DateTime.Now;
        }

        private void Reset()
        {
            CemuPath.ResetPath();
            LibraryPath.ResetPath();
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

            PollingWorker.RunWorkerAsync();

            await Instance.ThemeUpdate();
        }

        private void UpdateDatabase()
        {
            if (File.Exists(DbFile) && DateTime.Now <= new FileInfo(DbFile).LastWriteTime.AddDays(1)) return;

            const string url = "https://wiiu.titlekeys.com/json";

            using (var wc = new WebClient())
            {
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

            await Task.Run(() =>
            {
                var files = Directory.GetFiles(tempPath);
                ProgressBarMax = files.Length;

                var list = new TitleInfoEntry[ProgressBarMax = files.Length];

                for (var i = 0; i < files.Length; i++)
                    try
                    {
                        var file = files[i];
                        list[i] = TitleInfoEntry.LoadCache(file, true);
                        list[i].IsCached = true;
                        ProgressBarCurrent++;
                    }
                    catch (Exception e)
                    {
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

            await Task.Run(() =>
            {
                var list = new TitleInfoEntry[ProgressBarMax = files.Length];

                for (var i = 0; i < files.Length; i++)
                {
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
                foreach (var t in TitleInfoEntry.Entries)
                {
                    if (forceUpdate) t.CacheTheme(true);
                    ProgressBarCurrent++;
                }
            });

            TitleInfoEntry = TitleInfoEntry.Entries[0];
            UnlockControls();

            Status = $"Library Path = {LibraryPath.GetPath()}";
        }

        private enum DpadButton
        {
            Up,
            Down,
            Left,
            Right
        }

        private enum FaceButton
        {
            A,
            B,
            X,
            Y,
            Guide,
            Start,
            Back,
            LeftStick,
            RightStick,
            LeftShoulder,
            RightShoulder
        }
    }
}