using ModAPI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class ProgressHandler : ViewModelBase
    {
        public delegate void ProgressDelegate(string action, float progress);
        public ProgressDelegate OnProgress;
        public delegate void FinishedDelegate();
        public FinishedDelegate OnFinish;
        public delegate void ErrorDelegate(string text);
        public ErrorDelegate OnError;
        
        private List<(ProgressHandler, float)> SubHandlers = new List<(ProgressHandler, float)>();

        private string _CurrentAction;
        public string CurrentAction
        {
            get => _CurrentAction; set => this.RaiseAndSetIfChanged<ProgressHandler, string>(ref _CurrentAction, value, "CurrentAction");
        }

        private bool _Finished = false;
        public bool Finished
        {
            get => _Finished; set => this.RaiseAndSetIfChanged<ProgressHandler, bool>(ref _Finished, value, "Finished");
        }
        private float _Progress;
        public float Progress
        {
            get => _Progress; set => this.RaiseAndSetIfChanged<ProgressHandler, float>(ref _Progress, value, "Progress");
        }

        public ProgressHandler()
        {
        }

        public void AddProgressHandler(ProgressHandler subHandler, float overallProgress)
        {
            SubHandlers.Add((subHandler, overallProgress));
            subHandler.OnProgress += OnMultiProgress;
            subHandler.OnFinish += () => { OnMultiProgress(null, 0f); };
        }

        private void OnMultiProgress(string action, float progress)
        {
            bool allFinished = true;
            float totalProgress = 0f;
            foreach (var subHandler in SubHandlers)
            {
                if (!subHandler.Item1.Finished)
                    allFinished = false;
                totalProgress += subHandler.Item1.Progress * subHandler.Item2;
            }
            CurrentAction = action;
            Progress = totalProgress;
            if (allFinished)
            {
                Finished = true;
                if (OnFinish != null)
                {
                    OnFinish();
                }
            }
            else if (OnProgress != null)
                OnProgress(action, totalProgress);
        }

        public void ChangeProgress(string action, float progress)
        {
            CurrentAction = action;
            Progress = progress;
            if (OnProgress != null)
                OnProgress(CurrentAction, progress);
        }

        public void Finish()
        {
            Finished = true;
            if (OnFinish != null)
                OnFinish();
        }

        public void Error(string text)
        {
            CurrentAction = "Error: " + text;
            if (OnError != null)
                OnError(text);
        }

        public void ChangeProgress(float progress)
        {
            Progress = progress;
            if (OnProgress != null)
                OnProgress(CurrentAction, progress);
        }
    }
}
