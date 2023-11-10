using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Windows;

namespace Ticket_Genie
{
    public class TicketManager
    {
        private readonly TCSOAPService _tcSoapService;
        private readonly DBConnector _dbConnector;

        public TicketManager()
        {
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
        }

        // Reloads the GM tickets in-game
        public void UpdateTickets() { _tcSoapService.ExecuteSOAPCommand("reload gm_tickets"); }

        public void AppendResponse(int ticketID, string response)
        {
            // Append the response to the ticket
            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("UPDATE gm_ticket SET response = @response WHERE id = @ticketID", connection);
                    command.Parameters.AddWithValue("@response", response);
                    command.Parameters.AddWithValue("@accountID", Properties.Settings.Default.AccountID);
                    command.Parameters.AddWithValue("@ticketID", ticketID);
                    command.ExecuteNonQuery();

                    _dbConnector.CloseConnection(command);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void CompleteTicket(int ticketID)
        {
            // Complete the ticket
            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("UPDATE gm_ticket SET closedBy = @accountID, completed = 1, resolvedBy = @accountID WHERE id = @ticketID", connection);
                    command.Parameters.AddWithValue("@accountID", Properties.Settings.Default.CharacterGUID);
                    command.Parameters.AddWithValue("@ticketID", ticketID);
                    command.ExecuteNonQuery();

                    _dbConnector.CloseConnection(command);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void CloseTicket(int ticketID)
        {
            // Close the ticket
            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("UPDATE gm_ticket SET type = 1, closedBy = @accountID WHERE id = @ticketID", connection);
                    command.Parameters.AddWithValue("@accountID", Properties.Settings.Default.AccountID);
                    command.Parameters.AddWithValue("@ticketID", ticketID);
                    command.ExecuteNonQuery();

                    _dbConnector.CloseConnection(command);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public Ticket GetTicket(int id)
        {
            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT id, type, playerGuid, name, description, createTime, lastModifiedTime, closedBy, response, completed, viewed FROM gm_ticket WHERE id = @id", connection);
                    command.Parameters.AddWithValue("@id", id);
                    var reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        int type = reader.GetInt32(1);
                        int playerGuid = reader.GetInt32(2);
                        string name = reader.GetString(3);
                        string description = reader.GetString(4);
                        int createTime = reader.GetInt32(5);
                        int lastModifiedTime = reader.GetInt32(6);
                        string handledBy = "";
                        int closedBy = reader.GetInt32(7);
                        string response = reader.GetString(8);
                        int completed = reader.GetInt32(9);
                        int viewed = reader.GetInt32(10);

                        _dbConnector.CloseConnection(command, reader);

                        if (closedBy != 0)
                        {
                            // Translate the closedBy guid to a character name for handledBy
                            connection.Open();
                            command = new MySqlCommand("SELECT name FROM characters WHERE guid = @closedByGuid", connection);
                            command.Parameters.AddWithValue("@closedByGuid", closedBy);
                            reader = command.ExecuteReader();

                            if (reader.Read() && reader.HasRows)
                            {
                                handledBy = reader.GetString(0);
                            }

                            _dbConnector.CloseConnection(command, reader);
                        }

                        connection.Open();
                        command = new MySqlCommand("UPDATE gm_ticket SET viewed = @viewedCount WHERE id = @id", connection);
                        command.Parameters.AddWithValue("@viewedCount", viewed + 1);
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();

                        _dbConnector.CloseConnection(command);

                        return new Ticket
                        {
                            id = id,
                            type = type,
                            playerGUID = playerGuid,
                            name = name,
                            description = description,
                            createTime = createTime,
                            lastModifiedTime = lastModifiedTime,
                            handledBy = handledBy,
                            closedBy = closedBy,
                            response = response,
                            completed = completed,
                            viewed = viewed + 1
                        };
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return null;
            }
        }

        public IEnumerable<Ticket> GetAllTickets()
        {
            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT id, type, name, closedBy FROM gm_ticket", connection);
                    var reader = command.ExecuteReader();
                    var tickets = new List<Ticket>();

                    while (reader.Read())
                    {
                        int type = reader.GetInt32(1);
                        int closedBy = reader.GetInt32(3);

                        // Check if the ticket is already closed, and don't list
                        if (type == 0 && closedBy == 0)
                        {
                            tickets.Add(new Ticket
                            {
                                id = reader.GetInt32(0),
                                name = reader.GetString(2)
                            });
                        }
                    }

                    _dbConnector.CloseConnection(command, reader);

                    return tickets;
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return null;
            }
        }

        public IEnumerable<Ticket> GetTicketHistory()
        {
            // Get only closed/completed tickets connected to the player
            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT id, name FROM gm_ticket WHERE type IN (1, 2) AND name = @playerName", connection);
                    command.Parameters.AddWithValue("@playerName", Properties.Settings.Default.PlayerName);
                    var reader = command.ExecuteReader();
                    var tickets = new List<Ticket>();

                    while (reader.Read())
                    {
                        {
                            tickets.Add(new Ticket
                            {
                                id = reader.GetInt32(0),
                                name = reader.GetString(1)
                            });
                        }
                    }

                    _dbConnector.CloseConnection(command, reader);

                    return tickets;
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return null;
            }
        }
    }
}
