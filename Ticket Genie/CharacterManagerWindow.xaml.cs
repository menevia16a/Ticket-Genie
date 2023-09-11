using System.Windows;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for CharacterManagerWindow.xaml
    /// </summary>
    public partial class CharacterManagerWindow : Window
    {
        private readonly DBConnector _dbConnector;

        public CharacterManagerWindow()
        {
            InitializeComponent();

            var connectionString = new MySqlConnectionStringBuilder
            {
                Server = Properties.Settings.Default.SQLHost,
                Port = (uint)Properties.Settings.Default.SQLPort,
                Database = Properties.Settings.Default.CharacterDB,
                UserID = Properties.Settings.Default.SQLUsername,
                Password = Properties.Settings.Default.SQLPassword
            }.ToString();

            _dbConnector = new DBConnector(connectionString);
            Loaded += CharacterManagerWindow_Loaded;
        }

        public IEnumerable<Character> GetCharacters(int accountId)
        {
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT guid, name FROM characters WHERE account = @id", connection);
                command.Parameters.AddWithValue("@id", accountId);
                var reader = command.ExecuteReader();
                var characters = new List<Character>();
                while (reader.Read())
                {
                    var character = new Character
                    {
                        charGuid = reader.GetInt32(0),
                        name = reader.GetString(1)
                    };
                    characters.Add(character);
                }
                CharactersList.ItemsSource = characters;
                return characters;
            }
        }

        private void CharacterManagerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            int accountId = Properties.Settings.Default.AccountID;

            GetCharacters(accountId);
        }

        private void OnCharacterSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is Character selectedCharacter)
            {
                // Save selected character's GUID to Properties.Settings.Default.CharacterGUID
                Properties.Settings.Default.CharacterGUID = selectedCharacter.charGuid;
                Properties.Settings.Default.Save();
            }
        }

        private void OnSelectClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
