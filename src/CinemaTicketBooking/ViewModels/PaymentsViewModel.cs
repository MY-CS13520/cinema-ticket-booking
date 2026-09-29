using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>Mark pending sales as paid, or refund a payment and release the seats.</summary>
public partial class PaymentsViewModel : ViewModelBase
{
    private readonly IPaymentService _payments;
    private List<PaymentRow> _all = [];

    public PaymentsViewModel(IPaymentService payments)
    {
        _payments = payments;
    }

    public ObservableCollection<PaymentRow> Payments { get; } = [];

    public IReadOnlyList<string> Filters { get; } = ["All", "Pending", "Paid", "Refunded", "Voided"];

    [ObservableProperty]
    private string _filter = "All";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MarkPaidCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefundCommand))]
    private PaymentRow? _selectedPayment;

    [ObservableProperty]
    private bool _hasPayments;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand(CanExecute = nameof(CanMarkPaid))]
    private Task MarkPaidAsync() => GuardAsync(async () =>
    {
        if (SelectedPayment is not PaymentRow payment)
            throw new CinemaValidationException("Select a payment.");

        await _payments.MarkPaidAsync(payment.Id);
        await ReloadAsync();
        ShowStatus($"{payment.BookingCode} marked paid.");
    });

    [RelayCommand(CanExecute = nameof(CanRefund))]
    private Task RefundAsync() => GuardAsync(async () =>
    {
        if (SelectedPayment is not PaymentRow payment)
            throw new CinemaValidationException("Select a payment.");

        if (!Confirm($"Refund {payment.Amount:C} on {payment.BookingCode}? The booking is cancelled and the seats go back on sale."))
            return;

        await _payments.RefundAsync(payment.Id);
        await ReloadAsync();
        ShowStatus($"{payment.BookingCode} refunded.");
    });

    protected override Task LoadAsync() => GuardAsync(ReloadAsync);

    partial void OnFilterChanged(string value) => ApplyFilter(SelectedPayment?.Id);

    private bool CanMarkPaid() => SelectedPayment?.CanMarkPaid == true;

    private bool CanRefund() => SelectedPayment?.CanRefund == true;

    private async Task ReloadAsync()
    {
        _all = (await _payments.GetAsync(null)).ToList();
        ApplyFilter(SelectedPayment?.Id);
    }

    private void ApplyFilter(int? selectId)
    {
        var rows = Filter == "All"
            ? _all
            : _all.Where(payment => payment.Status == Filter);

        Payments.Clear();
        foreach (var row in rows)
            Payments.Add(row);

        SelectedPayment = Payments.FirstOrDefault(payment => payment.Id == selectId);
        HasPayments = Payments.Count > 0;
    }
}
