﻿using MySql.Data.MySqlClient;
using System.Windows;
using System.Linq;
using System.Windows.Input;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly DBConnector _dbConnector;
        private static bool loginSuccess = false;

        public LoginWindow()
        {
            InitializeComponent();

            UsernameTextBox.Focus(); // Automatically focus the username field

            var connectionString = new MySqlConnectionStringBuilder
            {
                Server = Properties.Settings.Default.SQLHost,
                Port = (uint)Properties.Settings.Default.SQLPort,
                Database = Properties.Settings.Default.AuthDB,
                UserID = Properties.Settings.Default.SQLUsername,
                Password = Properties.Settings.Default.SQLPassword
            }.ToString();

            _dbConnector = new DBConnector(connectionString);
            UsernameTextBox.KeyDown += UsernameTextBox_KeyDown;
        }

        private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnClickLogin(sender, e);
        }

        public bool GetLoginSuccess() { return loginSuccess; }

        private void OnClickLogin(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.ToUpper();

            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT id, username FROM account WHERE username = @username", connection);
                    command.Parameters.AddWithValue("@username", username);
                    var reader = command.ExecuteReader();

                    int accountID = 0;
                    string usernameFromDB = "";

                    if (reader.Read())
                    {
                        accountID = reader.GetInt32(0);
                        usernameFromDB = reader.GetString(1).ToUpper();
                        reader.Close();
                    }

                    var command2 = new MySqlCommand("SELECT SecurityLevel FROM account_access WHERE AccountID = @id", connection);
                    command2.Parameters.AddWithValue("@id", accountID);
                    var reader2 = command2.ExecuteReader();
                    if (reader2.HasRows && reader2.Read())
                    {
                        // Store security level
                        int securityLevel = reader2.GetInt32(0);

                        reader2.Close();

                        if (usernameFromDB == username && securityLevel > 1) // Verify username and security level
                        {
                            var command3 = new MySqlCommand("SELECT TicketGeniePin FROM account_access WHERE AccountID = @id", connection);
                            command3.Parameters.AddWithValue("@id", accountID);
                            var reader3 = command3.ExecuteReader();

                            if (reader3.Read())
                            {
                                var password = reader3.GetValue(0);

                                if (string.IsNullOrEmpty(password.ToString()))
                                {
                                    // Prompt user to set a PIN
                                    reader3.Close();
                                    MessageBox.Show("First login detected. Please set a 4 digit PIN", "Pin Setup", MessageBoxButton.OK, MessageBoxImage.Information);

                                    string pin = InputDialog.ShowDialog("Input 4 digit PIN", true);

                                    if (pin != null && pin.Count() == 4)
                                    {
                                        var command4 = new MySqlCommand("UPDATE account_access SET TicketGeniePin = @pin WHERE AccountID = @id", connection);
                                        command4.Parameters.AddWithValue("@pin", pin);
                                        command4.Parameters.AddWithValue("@id", accountID);
                                        command4.ExecuteNonQuery();
                                        Properties.Settings.Default.AccountID = accountID;
                                        Properties.Settings.Default.Save();
                                        loginSuccess = true;
                                        DialogResult = true;
                                        Close();
                                    }
                                    else { MessageBox.Show("Invalid PIN", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                                }
                                else
                                {
                                    string pin = InputDialog.ShowDialog("Input 4 digit PIN", true);

                                    if (pin != null && password.ToString() == pin)
                                    {
                                        Properties.Settings.Default.AccountID = accountID;
                                        Properties.Settings.Default.Save();
                                        loginSuccess = true;
                                        DialogResult = true;
                                        Close();
                                    }
                                    else { MessageBox.Show("Invalid PIN", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                                }
                            }
                        }
                        else { MessageBox.Show("Invalid username or insufficient security level", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }

                        _dbConnector.CloseConnection(command, reader);
                    }
                    else
                    {
                        loginSuccess = false;
                        DialogResult = false;

                        _dbConnector.CloseConnection(command, reader);
                        Close();
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    loginSuccess = false;
                    DialogResult = false;
                    Close();
                }
            }
        }
    }
}
