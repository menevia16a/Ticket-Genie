using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using MySql.Data.MySqlClient;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for AccountToolsWindow.xaml
    /// </summary>
    public partial class AccountToolsWindow : Window
    {
        private enum PlayerFaction : byte
        {
            Alliance = 0,
            Horde = 1,
            Invalid = 2
        }

        // Declare a dictionary to map races to factions
        private Dictionary<int, PlayerFaction> raceToFaction = new Dictionary<int, PlayerFaction>
        {
            { 1, PlayerFaction.Alliance }, // Human
            { 3, PlayerFaction.Alliance }, // Dwarf
            { 4, PlayerFaction.Alliance }, // Night Elf
            { 7, PlayerFaction.Alliance }, // Gnome
            { 11, PlayerFaction.Alliance }, // Draenei
            { 2, PlayerFaction.Horde }, // Orc
            { 5, PlayerFaction.Horde }, // Undead
            { 6, PlayerFaction.Horde }, // Tauren
            { 8, PlayerFaction.Horde }, // Troll
            { 10, PlayerFaction.Horde }, // Blood Elf
        };

        // Declare dictionaries to map city names to PortLocation objects
        private Dictionary<string, PortLocation> alliancePortLocations = new Dictionary<string, PortLocation>
        {
            { "Stormwind", new PortLocation { name = "Stormwind", positionX = -8877.911133f, positionY = 671.453857f, positionZ = 104.950226f, map = 0, orientation = 0.583116f } },
            { "Ironforge", new PortLocation { name = "Ironforge", positionX = -4839.234863f, positionY = -871.912659f, positionZ = 510.246735f, map = 0, orientation = 4.900395f } },
            { "Darnassus", new PortLocation { name = "Darnassus", positionX = 10139.388672f, positionY = 2214.697510f, positionZ = 1329.984741f, map = 1, orientation = 2.186495f } },
            { "Exodar", new PortLocation { name = "Exodar", positionX = -3734.079102f, positionY = -11694.936523f, positionZ = -105.742058f, map = 530, orientation = 3.260193f } }
        };
        
        private Dictionary<string, PortLocation> hordePortLocations = new Dictionary<string, PortLocation>
        {
            { "Orgrimmar", new PortLocation { name = "Orgrimmar", positionX = 1636.935791f, positionY = -4443.699707f, positionZ = 15.633880f, map = 1, orientation = 2.622632f } },
            { "Undercity", new PortLocation {name = "Undercity", positionX = 1644.060913f, positionY = 218.970184f, positionZ = -43.102631f, map = 0, orientation = 2.709491f} },
            { "Thunder Bluff", new PortLocation {name = "Thunder Bluff", positionX = -1307.720215f, positionY = 35.120102f, positionZ = 129.208679f, map = 1, orientation = 0.451448f} },
            { "Silvermoon City", new PortLocation {name = "Silvermoon City", positionX = 9683.867188f, positionY = -7383.734863f, positionZ = 22.795446f, map = 530, orientation = 1.505019f} },
        };

        private static PlayerFaction playerFaction;
        private PortLocation portLocation;
        private static int playerGUID;
        private static string playerName;
        private readonly TCSOAPService _tcSoapService;
        private readonly DBConnector _dbConnector;

        public AccountToolsWindow(int ticketCharacterGUID, string ticketPlayerName = "")
        {
            InitializeComponent();

            playerGUID = ticketCharacterGUID;
            playerName = ticketPlayerName;

            var connectionString = new MySqlConnectionStringBuilder
            {
                Server = Properties.Settings.Default.SQLHost,
                Port = (uint)Properties.Settings.Default.SQLPort,
                Database = Properties.Settings.Default.CharacterDB,
                UserID = Properties.Settings.Default.SQLUsername,
                Password = Properties.Settings.Default.SQLPassword
            }.ToString();

            _dbConnector = new DBConnector(connectionString);
            _tcSoapService = new TCSOAPService();
            Loaded += AccountToolsWindow_Loaded;
        }

        private void AccountToolsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Check if playerGUID is valid, if not then disable all TextBoxes except unbanning
            if (playerGUID == 0)
            {
                ReasonTextBox.IsEnabled = false;
                DurationTextBox.IsEnabled = false;
                SubjectTextBox.IsEnabled = false;
                MessageTextBox.IsEnabled = false;
                ItemIdTextBox1.IsEnabled = false;
                ItemIdTextBox2.IsEnabled = false;
                ItemIdTextBox3.IsEnabled = false;
                PortComboBox.IsEnabled = false;
            }
            else
            {
                // Get the player's faction and store it
                using (var connection = _dbConnector.GetConnection())
                {
                    try
                    {
                        connection.Open();
                        var command = new MySqlCommand("SELECT race FROM characters WHERE guid = @guid LIMIT 1", connection);
                        command.Parameters.AddWithValue("@guid", playerGUID);
                        var reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            // Check if the player is Alliance or Horde by their race id
                            int playerRace = reader.GetInt16(0);
                            playerFaction = (PlayerFaction)GetPlayerFaction(playerRace);

                            if (playerFaction == PlayerFaction.Invalid)
                            {
                                MessageBox.Show("Invalid player faction.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            else if (playerFaction == PlayerFaction.Alliance)
                            {
                                // Set the available porting cities to Alliance cities
                                PortComboBoxItem1.Content = alliancePortLocations.ElementAt(0).Key;
                                PortComboBoxItem2.Content = alliancePortLocations.ElementAt(1).Key;
                                PortComboBoxItem3.Content = alliancePortLocations.ElementAt(2).Key;
                                PortComboBoxItem4.Content = alliancePortLocations.ElementAt(3).Key;
                            }
                            else if (playerFaction == PlayerFaction.Horde)
                            {
                                // Set the available porting cities to Horde cities
                                PortComboBoxItem1.Content = hordePortLocations.ElementAt(0).Key;
                                PortComboBoxItem2.Content = hordePortLocations.ElementAt(1).Key;
                                PortComboBoxItem3.Content = hordePortLocations.ElementAt(2).Key;
                                PortComboBoxItem4.Content = hordePortLocations.ElementAt(3).Key;
                            }
                        }

                        _dbConnector.CloseConnection(reader);
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OnBanClick(object sender, RoutedEventArgs e)
        {
            if (playerGUID == 0)
                return;

            // Verify duration format
            string durationText = DurationTextBox.Text.Trim().ToLower();

            if (!Regex.IsMatch(durationText, @"^\d+d\d+h\d+m$") && !DurationTextBox.Text.StartsWith("-"))
            {
                MessageBox.Show("Invalid input format, negative value leads to permban, otherwise use a timestring like \"4d20h3s\".", "Wrong Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (DurationTextBox.Text.StartsWith("-"))
                DurationTextBox.Text = "-1";
            else
                DurationTextBox.Text = durationText;

            var banReason = ReasonTextBox.Text;
            var banDuration = DurationTextBox.Text;

            if (_tcSoapService.ExecuteSOAPCommand($"ban playeraccount {playerName} {banDuration} {banReason} by GM account {Properties.Settings.Default.AccountID}."))
                MessageBox.Show($"Player {playerName}'s account has been banned.\r\nReason: {banReason}", "Account Banned", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnUnbanClick(object sender, RoutedEventArgs e)
        {
            if (PlayerNameTextBox.Text == string.Empty)
                return;

            // Unban player account
            if (_tcSoapService.ExecuteSOAPCommand($"unban playeraccount {playerName}"))
                MessageBox.Show($"Player {playerName}'s account has been unbanned.", "Account Unbanned", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnPortClick(object sender, RoutedEventArgs e)
        {
            if (playerGUID == 0)
                return;

            // Check if player is logged in
            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT online FROM characters WHERE guid = @guid LIMIT 1", connection);
                    command.Parameters.AddWithValue("@guid", playerGUID);
                    var reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        // Check if the player is online
                        int playerOnline = reader.GetInt16(0);

                        if (playerOnline == 1)
                        {
                            MessageBox.Show("Player must be offline to be ported.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    _dbConnector.CloseConnection(reader);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Get the selected port location
            string portName = PortComboBox.Text;

            if (portName == string.Empty)
            {
                MessageBox.Show("Please select a port location.", "Porting Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the port location object
            switch (playerFaction)
            {
                case PlayerFaction.Alliance:
                    if (!alliancePortLocations.TryGetValue(portName, out portLocation))
                    {
                        MessageBox.Show("Invalid port location.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    break;
                case PlayerFaction.Horde:
                    if (!hordePortLocations.TryGetValue(portName, out portLocation))
                    {
                        MessageBox.Show("Invalid port location.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    break;
                case PlayerFaction.Invalid:
                    MessageBox.Show("Invalid player faction.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }

            // Port the player to the selected location
            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("UPDATE characters SET position_x = @positionX, position_y = @positionY, position_z = @positionZ, map = @map, orientation = @orientation WHERE guid = @guid", connection);
                    command.Parameters.AddWithValue("@positionX", portLocation.positionX);
                    command.Parameters.AddWithValue("@positionY", portLocation.positionY);
                    command.Parameters.AddWithValue("@positionZ", portLocation.positionZ);
                    command.Parameters.AddWithValue("@map", portLocation.map);
                    command.Parameters.AddWithValue("@orientation", portLocation.orientation);
                    command.Parameters.AddWithValue("@guid", playerGUID);
                    command.ExecuteNonQuery();

                    _dbConnector.CloseConnection();

                    MessageBox.Show($"Player {playerName} has been ported to {portName}.", "Port Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnMailClick(object sender, RoutedEventArgs e)
        {
            if (playerGUID == 0)
                return;

            // Verify fields have been filled
            if (SubjectTextBox.Text == string.Empty || MessageTextBox.Text == string.Empty)
            {
                MessageBox.Show("Both the subject and the message are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check ItemIdTextBoxes for item ids
            int itemID1 = 0;
            int itemID2 = 0;
            int itemID3 = 0;
            int itemCount = 0;

            if (ItemIdTextBox1.Text != string.Empty)
                int.TryParse(ItemIdTextBox1.Text, out itemID1);
            if (ItemIdTextBox2.Text != string.Empty)
                int.TryParse(ItemIdTextBox2.Text, out itemID2);
            if (ItemIdTextBox3.Text != string.Empty)
                int.TryParse(ItemIdTextBox3.Text, out itemID3);

            for (int i = 0; i <= 2; i++)
            {
                if (i == 0 && itemID1 != 0)
                {
                    itemCount++;
                    break;
                }
                else if (i == 1 && itemID2 != 0)
                {
                    itemCount++;
                    break;
                }
                else if (i == 2 && itemID3 != 0)
                {
                    itemCount++;
                    break;
                }
            }

            // Send mail to the player, including items if there are any
            if (itemCount != 0)
            {
                switch (itemCount)
                {
                    case 1:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:1"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItem: {itemID1}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case 2:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:1 {itemID2}:1"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItems: {itemID1}, {itemID2}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case 3:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:1 {itemID2}:1 {itemID3}:1"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItems: {itemID1}, {itemID2}, {itemID3}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            else
            {
                if (_tcSoapService.ExecuteSOAPCommand($"send mail {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\""))
                    MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private PlayerFaction GetPlayerFaction(int playerRace)
        {
            if (raceToFaction.TryGetValue(playerRace, out PlayerFaction faction))
                return faction;

            return PlayerFaction.Invalid;
        }
    }
}
