using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TicketManager _ticketManager;

        public MainWindow()
        {
            InitializeComponent();

            var connectionSettingsWindow = new ConnectionSettingsWindow();

            if (!File.Exists("SQLConnectionSettings.json") || !File.Exists("SOAPConnectionSettings.json"))
                if (connectionSettingsWindow.ShowDialog() == true)
                    JsonTools.CreateDefaultJsonFiles(); // Check if JSON files exist, if not then create them

            _ticketManager = new TicketManager();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AccountID = 0;
            Properties.Settings.Default.CharacterGUID = 0;
            Properties.Settings.Default.CurrentTicketID = 0;
            Properties.Settings.Default.Save();

            JsonTools.UpdateConnectionSettings(); // Update application connection settings from JSON

            var loginWindow = new LoginWindow();

            if (loginWindow.ShowDialog() == true)
            {
                if (loginWindow.GetLoginSuccess())
                {
                    // Retrieve all tickets and display them on the UI
                    var tickets = _ticketManager.GetAllTickets();

                    if (tickets == null)
                        return;

                    foreach (var ticket in tickets)
                    {
                        // Add the ticket to the left side list on the main window
                        Ticket listItem = new Ticket();
                        listItem.id = ticket.id;
                        listItem.name = ticket.name;
                        TicketList.Items.Add(listItem);
                    }

                    MessageBox.Show("All of the current tickets have been loaded.", "Ticket List", MessageBoxButton.OK, MessageBoxImage.Information);

                }
                else
                    Application.Current.Shutdown();
            }
            else
                Application.Current.Shutdown();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) { Application.Current.Shutdown(); } // Terminate the process on window close

        private void OnTicketSelected(object sender, RoutedEventArgs e)
        {
            // Check if a character has been selected, if not then send a message
            if (Properties.Settings.Default.CharacterGUID == 0 && TicketList.SelectedIndex != -1)
            {
                Properties.Settings.Default.CurrentTicketID = 0;
                Properties.Settings.Default.Save();
                TicketList.SelectedIndex = -1;
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (TicketList.SelectedIndex == -1)
                return;

            // Retrieve selected ticket
            if (sender is ListBox listBox && listBox.SelectedItem is Ticket selectedTicket)
            {
                var ticketDetails = _ticketManager.GetTicket(selectedTicket.id);

                if (ticketDetails != null)
                {
                    // Convert Linux timestamp to readable date
                    var creationTime = DateTimeOffset.FromUnixTimeSeconds(ticketDetails.createTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    var lastModifiedTime = DateTimeOffset.FromUnixTimeSeconds(ticketDetails.lastModifiedTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                    // Update the UI ticket info
                    TicketID.Text = ticketDetails.id.ToString();
                    TicketName.Text = ticketDetails.name;
                    PlayerGUID.Text = ticketDetails.playerGUID.ToString();
                    ViewedCount.Text = ticketDetails.viewed.ToString();
                    Creation.Text = creationTime;
                    LastModified.Text = lastModifiedTime;
                    TicketDescription.Text = ticketDetails.description;
                    Properties.Settings.Default.CurrentTicketID = ticketDetails.id;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void RefreshTicketList()
        {
            // Trigger ticket list updating
            TicketList.Items.Clear();
            TicketID.Text = "";
            TicketName.Text = "";
            TicketDescription.Text = "";
            var tickets = _ticketManager.GetAllTickets();
            Properties.Settings.Default.CurrentTicketID = 0;
            Properties.Settings.Default.Save();

            if (tickets == null)
                return;

            foreach (var ticket in tickets)
            {
                Ticket listItem = new Ticket();
                listItem.id = ticket.id;
                listItem.name = ticket.name;
                TicketList.Items.Add(listItem);
            }
        }

        private void OnCharacterManagerClick(object sender, RoutedEventArgs e)
        {
            // Open the Character Manager window to select a character
            var characterManager = new CharacterManagerWindow();
            characterManager.ShowDialog();
        }

        private void OnConnectionSettingsClick(object sender, RoutedEventArgs e)
        {
            // Open the Connection Settings window to select a character
            var connectionSettings = new ConnectionSettingsWindow();
            connectionSettings.ShowDialog();
        }

        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            // Open the AboutWindow
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            RefreshTicketList();
            MessageBox.Show("The list of tickets has been updated.", "Ticket List Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnRespondClick(object sender, RoutedEventArgs e)
        {
            // If no ticket is selected, send a message
            if (TicketID.Text?.Length == 0)
            {
                MessageBox.Show("Please select a ticket first.", "No Ticket Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Display GM Response window
            var gmResponseWindow = new ResponseWindow();
            gmResponseWindow.ShowDialog();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            var ticketID = Properties.Settings.Default.CurrentTicketID;
            
            if (ticketID == 0)
            {
                MessageBox.Show("Please select a ticket first.", "No Ticket Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ask for confirmation
            var result = MessageBox.Show("Are you sure you want to close this ticket?", "Close Ticket", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Close the ticket
                _ticketManager.CloseTicket(ticketID);
                _ticketManager.UpdateTickets();
                RefreshTicketList();
                MessageBox.Show("The ticket has been closed.", "Ticket Closed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnAccountToolsClick(object sender, RoutedEventArgs e)
        {
            var ticketID = Properties.Settings.Default.CurrentTicketID;

            // Pass player GUID and Name to the account tools window if a ticket is selected
            if (ticketID != 0)
            {
                Ticket ticket = _ticketManager.GetTicket(ticketID);

                Properties.Settings.Default.PlayerGUID = ticket.playerGUID;
                Properties.Settings.Default.PlayerName = ticket.name;
                Properties.Settings.Default.Save();
            }

            var accountToolsWindow = new AccountToolsWindow();

            accountToolsWindow.ShowDialog();
        }
    }
}
