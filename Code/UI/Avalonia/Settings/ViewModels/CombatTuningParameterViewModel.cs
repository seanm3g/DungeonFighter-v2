using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RPGGame.Tuning;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    /// <summary>Bindable row for one combat tuning registry parameter (slider + text box).</summary>
    public sealed class CombatTuningParameterViewModel : INotifyPropertyChanged
    {
        private readonly CombatTuningParameter parameter;
        private Action<double>? onValueCommitted;
        private double _value;
        private bool _suppressCommit;
        private string? _pendingValueText;

        public CombatTuningParameterViewModel(CombatTuningParameter parameter, Action<double>? onValueCommitted = null)
        {
            this.parameter = parameter;
            this.onValueCommitted = onValueCommitted;
            Id = parameter.Id;
            Label = parameter.Label;
            Affects = parameter.Affects;
            FilterKey = parameter.FilterKey;
            Minimum = parameter.Minimum;
            Maximum = parameter.Maximum;
            TickFrequency = parameter.TickFrequency;
            IsImplemented = parameter.IsImplemented;
            _value = Math.Clamp(parameter.GetValue(), Minimum, Maximum);
        }

        public string Id { get; }
        public string Label { get; }
        public string Affects { get; }
        public string DisplayAffects => IsImplemented ? Affects : $"[Unimplemented] {Affects}";
        public string? FilterKey { get; }
        public bool IsImplemented { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public double TickFrequency { get; }

        public double Value
        {
            get => _value;
            set
            {
                double clamped = Math.Clamp(value, Minimum, Maximum);
                if (Math.Abs(_value - clamped) < 1e-9)
                    return;

                _value = clamped;
                _pendingValueText = null;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ValueText));
                if (!_suppressCommit)
                    onValueCommitted?.Invoke(_value);
            }
        }

        public string ValueText
        {
            get => _pendingValueText ?? TuningValueFormatter.Format(_value, Minimum, Maximum, TickFrequency);
            set
            {
                _pendingValueText = value;
                OnPropertyChanged();
                if (!TuningValueFormatter.TryParse(value, out double parsed))
                    return;

                _pendingValueText = null;
                Value = Math.Clamp(parsed, Minimum, Maximum);
            }
        }

        /// <summary>Parses any in-progress text box text into <see cref="Value"/> (e.g. before Save).</summary>
        public void FlushPendingText()
        {
            if (string.IsNullOrWhiteSpace(_pendingValueText))
                return;

            if (TuningValueFormatter.TryParse(_pendingValueText, out double parsed))
            {
                _suppressCommit = true;
                try
                {
                    _pendingValueText = null;
                    Value = Math.Clamp(parsed, Minimum, Maximum);
                }
                finally
                {
                    _suppressCommit = false;
                }
            }
            else
            {
                _pendingValueText = null;
                OnPropertyChanged(nameof(ValueText));
            }
        }

        /// <summary>Replace the commit handler (e.g. wire master slider after panel construction).</summary>
        public void SetValueCommittedHandler(Action<double>? handler) => onValueCommitted = handler;

        /// <summary>Reload from config without re-pushing to the backing parameter.</summary>
        public void ReloadFromConfig()
        {
            _suppressCommit = true;
            try
            {
                Value = parameter.GetValue();
            }
            finally
            {
                _suppressCommit = false;
            }
        }

        public void CommitToConfig() => parameter.SetValue(Value);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
