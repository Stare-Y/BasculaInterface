using BasculaInterface.Services;
using Core.Domain.Entities.Identity;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BasculaInterface.Models
{
    public class WeightEntryDetailRow : INotifyPropertyChanged
    {
        // role-driven-terminal-modes: this is a plain POCO row, not DI-constructed (many instances
        // per rendered list), so it resolves ISessionService statically, same pattern already used
        // by MainPage/WeightingScreen for the same service.
        private static TerminalMode CurrentTerminalMode =>
            (MauiProgram.ServiceProvider.GetService(typeof(ISessionService)) as ISessionService)
                ?.CurrentUser?.TerminalMode ?? TerminalMode.Main;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int Id { get; set; } = 0;
        public int OrderIndex { get; set; } = 0;

        private double _tare = 0;
        public double Tare
        {
            get => _tare;
            set
            {
                if (_tare != value)
                {
                    _tare = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TareValue));
                    OnPropertyChanged(nameof(IsRowComplete));
                }
            }
        }

        public bool IsGranel { get; set; } = true;
        public string TareHeader => IsGranel ? "Tara" : string.Empty;
        public string TareValue => IsGranel ? Tare.ToString() + " kg" : string.Empty;

        private double _weight = 0;
        public double Weight
        {
            get => _weight;
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WeightValue));
                    OnPropertyChanged(nameof(IsRowComplete));
                }
            }
        }

        public string WeightHeader => IsGranel ? "Peso" : "Cantidad";
        public string WeightValue => IsGranel ? Weight.ToString() + " kg" : RequiredAmount?.ToString() ?? "0";

        private double? _secondaryTare;
        public double? SecondaryTare
        {
            get => _secondaryTare;
            set
            {
                if (_secondaryTare != value)
                {
                    _secondaryTare = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _weightedBy;
        public string? WeightedByDecorated
        {
            get => _weightedBy.IsNullOrEmpty() ? string.Empty : $"Pesado por: {_weightedBy}";
            set
            {
                if (_weightedBy != value)
                {
                    _weightedBy = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WeightedBy));
                }
            }
        }

        public string? WeightedBy => _weightedBy;
        private string _description = string.Empty;
        public int? FK_WeightedProductId { get; set; } = null;
        public bool IsSecondaryTerminal => CurrentTerminalMode == TerminalMode.Secondary;

        /// <summary>
        /// Gates the row's "⋮" change-product menu (issue #122): allowed for the main terminal
        /// (i.e. not "terminal secundaria") in either "Solo Pedidos" mode or the default/"main"
        /// mode where no terminal mode is enabled at all. Never allowed for a secondary terminal
        /// or an "OnlyFinished" ("Solo Concluidos") terminal.
        /// </summary>
        public bool CanChangeProductMenu =>
            CurrentTerminalMode != TerminalMode.Secondary &&
            CurrentTerminalMode != TerminalMode.OnlyFinished;

        private double? _requiredAmount = null;
        public double? RequiredAmount
        {
            get => _requiredAmount;
            set
            {
                if (_requiredAmount != value)
                {
                    _requiredAmount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RequiredAmountText));
                    OnPropertyChanged(nameof(WeightValue));
                }
            }
        }

        private int? _costales = null;
        public int? Costales
        {
            get => _costales;
            set
            {
                if (_costales != value)
                {
                    _costales = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RequiredCostalesText));
                }
            }
        }

        public double? ProductPrice { get; set; } = null;

        private bool _isLoaded = true;
        public bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                if (_isLoaded != value)
                {
                    _isLoaded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsRowComplete));
                }
            }
        }

        public bool IsRowComplete => !IsGranel || (Weight > 0 && Tare > 0 && IsLoaded);

        public string RequiredAmountText => RequiredAmount > 0 && IsGranel
            ? "Cantidad Solicitada: " + RequiredAmount.Value.ToString("F2") + " kg."
            : string.Empty;

        public string RequiredCostalesText => Costales.HasValue
            ? "Costales: " + Costales.Value.ToString()
            : string.Empty;

        public string Description
        {
            get => _description;
            set => _description = value ?? string.Empty;
        }
    }
}
