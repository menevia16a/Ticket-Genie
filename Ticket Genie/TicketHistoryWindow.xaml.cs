using Essy.Tools.InputBox;
using MySql.Data.MySqlClient;
using System.Windows;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for TicketHistory.xaml
    /// </summary>
    public partial class TicketHistoryWindow : Window
    {
        private static int playerGUID;
        private static string playerName;
        private static bool isValidPlayer;

        private readonly DBConnector _dbConnectorCharacters;

        public TicketHistoryWindow()
        {
            InitializeComponent();

            isValidPlayer = false;

            var charactersConnectionString = new MySqlConnectionStringBuilder
            {
                Server = Properties.Settings.Default.SQLHost,
                Port = (uint)Properties.Settings.Default.SQLPort,
                Database = Properties.Settings.Default.CharacterDB,
                UserID = Properties.Settings.Default.SQLUsername,
                Password = Properties.Settings.Default.SQLPassword
            }.ToString();

            _dbConnectorCharacters = new DBConnector(charactersConnectionString);

            if (Properties.Settings.Default.PlayerGUID == 0)
            {
                MessageBox.Show("No player was selected, you will be prompted to specify a player's name.", "No Player Detected", MessageBoxButton.OK, MessageBoxImage.Information);
                Properties.Settings.Default.PlayerName = InputBox.ShowInputBox("Please enter a player's name", false);
                Properties.Settings.Default.Save();

                if (Properties.Settings.Default.PlayerName?.Length == 0) { InvalidPlayer(); }
                else
                {
                    // Verify name given is actually a player
                    using (var connection = _dbConnectorCharacters.GetConnection())
                    {
                        try
                        {
                            connection.Open();
                            var command = new MySqlCommand("SELECT guid, name FROM characters WHERE name = @name LIMIT 1", connection);
                            command.Parameters.AddWithValue("@name", Properties.Settings.Default.PlayerName);
                            var reader = command.ExecuteReader();

                            if (reader.Read() && reader.HasRows)
                            {
                                playerGUID = reader.GetInt32(0);
                                playerName = reader.GetString(1);
                            }

                            _dbConnectorCharacters.CloseConnection(reader);

                            if (playerGUID == 0)
                                InvalidPlayer();
                            else
                                isValidPlayer = true;
                        }
                        catch (MySqlException ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                isValidPlayer = true;
                playerGUID = Properties.Settings.Default.PlayerGUID;
                playerName = Properties.Settings.Default.PlayerName;
            }

            Loaded += TicketHistoryWindow_Loaded;
        }

        private void TicketHistoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isValidPlayer)
                Close();
        }

        private void TicketHisoryWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Reset PlayerGUID and PlayerName settings
            Properties.Settings.Default.PlayerGUID = 0;
            Properties.Settings.Default.PlayerName = "";
            Properties.Settings.Default.Save();
        }

        private void InvalidPlayer()
        {
            isValidPlayer = false;

            MessageBox.Show("Could not find a player associated with that name, the Account Tools window will now close.", "No Player Found", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
