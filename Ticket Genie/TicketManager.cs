using MySql.Data.MySqlClient;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ServiceModel.Configuration;
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
            _tcSoapService.ExecuteSOAPCommand($"ticket complete {ticketID}");

            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("UPDATE gm_ticket SET closedBy = @characterGuid, comment = @characterGuid, resolvedBy = @characterGuid WHERE id = @ticketID", connection);
                    command.Parameters.AddWithValue("@characterGuid", Properties.Settings.Default.CharacterGUID);
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
            _tcSoapService.ExecuteSOAPCommand($"ticket close {ticketID}");

            using (var connection = _dbConnector.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("UPDATE gm_ticket SET closedBy = @characterGUID WHERE id = @ticketID", connection);
                    command.Parameters.AddWithValue("@characterGuid", Properties.Settings.Default.CharacterGUID);
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
                    var command = new MySqlCommand("SELECT id, type, playerGuid, name, description, createTime, lastModifiedTime, closedBy, comment, response, completed, viewed, resolvedBy FROM gm_ticket WHERE id = @id", connection);
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
                        string comment = reader.GetString(8);
                        string response = reader.GetString(9);
                        int completed = reader.GetInt32(10);
                        int viewed = reader.GetInt32(11);
                        int resolvedBy = reader.GetInt32(12);

                        int resolvedByGMGuid = 0;

                        _dbConnector.CloseConnection(command, reader);

                        if (comment?.Length != 0)
                            int.TryParse(comment, out resolvedByGMGuid);

                        if (closedBy != 0 && comment?.Length == 0) { handledBy = resolvedBy == 0 ? GuidToName(closedBy) : GuidToName(resolvedBy); }
                        else if (resolvedBy == 0 && comment?.Length != 0 && resolvedByGMGuid != 0)
                        {
                            connection.Open();
                            command = new MySqlCommand("UPDATE gm_ticket SET comment = '', resolvedBy = @resolvedByGMGuid WHERE id = @id", connection);
                            command.Parameters.AddWithValue("@resolvedByGMGuid", resolvedByGMGuid);
                            command.Parameters.AddWithValue("@id", id);
                            command.ExecuteNonQuery();

                            _dbConnector.CloseConnection(command);

                            handledBy = GuidToName(resolvedByGMGuid);
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
                            viewed = viewed + 1,
                            resolvedBy = resolvedBy
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
                    var command = new MySqlCommand("SELECT id, type, name, closedBy, resolvedBy FROM gm_ticket", connection);
                    var reader = command.ExecuteReader();
                    var tickets = new List<Ticket>();

                    while (reader.Read())
                    {
                        int type = reader.GetInt32(1);
                        int closedBy = reader.GetInt32(3);
                        int resolvedBy = reader.GetInt32(4);

                        // Check if the ticket is already closed, and don't list
                        if (type == 0 && closedBy == 0 && resolvedBy == 0)
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

        // Translate a given guid in to a character's name
        private string GuidToName(int guid)
        {
            string name = "";

            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT name FROM characters WHERE guid = @GMGuid", connection);
                command.Parameters.AddWithValue("@GMGuid", guid);
                var reader = command.ExecuteReader();

                if (reader.Read() && reader.HasRows)
                {
                    name = reader.GetString(0);
                }

                _dbConnector.CloseConnection(command, reader);
            }

            return name;
        }
    }
}
