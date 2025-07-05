using System;
using System.Windows;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for ResponseWindow.xaml
    /// </summary>
    public partial class ResponseWindow : Window
    {
        private readonly TicketManager _ticketManager;

        // Event to notify when a ticket is completed
        public event Action TicketCompleted;

        public ResponseWindow()
        {
            InitializeComponent();
            _ticketManager = new TicketManager();
            Loaded += ResponseWindow_Loaded;
        }

        private void ResponseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Retreive ticket response and display it in the UI
            var ticket = _ticketManager.GetTicket(Properties.Settings.Default.CurrentTicketID);

            if (ticket != null) { TicketResponse.Text = ticket.response; }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) { Close(); }

        private void OnFinalizeClick(object sender, RoutedEventArgs e)
        {
            // Let the user know this will complete the ticket, effectively closing it and ask if they wish to proceed
            var result = MessageBox.Show("Are you sure you wish to complete this ticket?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                _ticketManager.AppendResponse(Properties.Settings.Default.CurrentTicketID, TicketResponse.Text);
                _ticketManager.UpdateTickets(); // Initial update to ensure response gets saved before completion
                _ticketManager.CompleteTicket(Properties.Settings.Default.CurrentTicketID);
                _ticketManager.UpdateTickets();
                MessageBox.Show($"Ticket: {Properties.Settings.Default.CurrentTicketID} has been completed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                TicketCompleted?.Invoke(); // Raise event
                Close();
            }
        }
        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            // Save the reponse, but don't complete the ticket
            _ticketManager.AppendResponse(Properties.Settings.Default.CurrentTicketID, TicketResponse.Text);
            MessageBox.Show($"Ticket: {Properties.Settings.Default.CurrentTicketID} has been saved.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
