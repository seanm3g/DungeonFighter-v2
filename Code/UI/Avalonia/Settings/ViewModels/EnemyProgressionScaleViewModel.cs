using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    /// <summary>Bindable row for one enemy progression scale slider.</summary>
    public sealed class EnemyProgressionScaleViewModel : INotifyPropertyChanged
    {
        private readonly Action<double>? onValueCommitted;
        private double _value;
        private bool _suppressCommit;

        public EnemyProgressionScaleViewModel(
            string id,
            string label,
            double minimum,
            double maximum,
            double tickFrequency,
            Action<double>? onValueCommitted = null)
        {
            Id = id;
            Label = label;
            Minimum = minimum;
            Maximum = maximum;
            TickFrequency = tickFrequency;
            this.onValueCommitted = onValueCommitted;
            _value = Math.Clamp(1.0, minimum, maximum);
        }

        public string Id { get; }
        public string Label { get; }
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
                OnPropertyChanged();
                OnPropertyChanged(nameof(ValueText));
                if (!_suppressCommit)
                    onValueCommitted?.Invoke(_value);
            }
        }

        public string ValueText
        {
            get => TuningValueFormatter.Format(_value, Minimum, Maximum, TickFrequency);
            set
            {
                if (!TuningValueFormatter.TryParse(value, out double parsed))
                    return;

                Value = Math.Clamp(parsed, Minimum, Maximum);
            }
        }

        public void SetValueSilently(double value)
        {
            _suppressCommit = true;
            try
            {
                Value = value;
            }
            finally
            {
                _suppressCommit = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
