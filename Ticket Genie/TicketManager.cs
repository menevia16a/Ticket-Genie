using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Ticket_Genie
{
    public class TicketManager
    {
        private readonly TCSOAPService _tcSoapService;
        private readonly DBConnector _dbConnector;

        public TicketManager() { _dbConnector = new DBConnector(Properties.Settings.Default.CharactersDB); }

        // Reloads the GM tickets in-game
        public void UpdateTickets() { _tcSoapService.Call("reload gm_ticket"); }

        public void AppendResponse(int ticketID, string response)
        {
            // Append the response to the ticket
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("UPDATE gm_ticket SET response = @response WHERE id = @ticketID", connection);
                command.Parameters.AddWithValue("@response", response);
                command.Parameters.AddWithValue("@accountID", Properties.Settings.Default.AccountID);
                command.Parameters.AddWithValue("@ticketID", ticketID);
                command.ExecuteNonQuery();
            }
        }

        public void CompleteTicket(int ticketID)
        {
            // Complete the ticket
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("UPDATE gm_ticket SET type = 1, closedBy = @accountID, completed = 1, resolvedBy = @accountID WHERE id = @ticketID", connection);
                command.Parameters.AddWithValue("@accountID", Properties.Settings.Default.AccountID);
                command.Parameters.AddWithValue("@ticketID", ticketID);
                command.ExecuteNonQuery();
            }
        }

        public void CloseTicket(int ticketID)
        {
            // Close the ticket
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("UPDATE gm_ticket SET type = 1, closedBy = @accountID WHERE id = @ticketID", connection);
                command.Parameters.AddWithValue("@accountID", Properties.Settings.Default.AccountID);
                command.Parameters.AddWithValue("@ticketID", ticketID);
                command.ExecuteNonQuery();
            }
        }

        public Ticket GetTicket(int id)
        {
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT id, type, playerGuid, name, description, createTime, lastModifiedTime, closedBy, response, completed, viewed FROM gm_ticket WHERE id = @id", connection);
                command.Parameters.AddWithValue("@id", id);
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    int type = reader.GetInt32(1);

                    // Check if the ticket is already closed, and don't list
                    if (type == 0)
                    {
                        int playerGuid = reader.GetInt32(2);
                        string name = reader.GetString(3);
                        string description = reader.GetString(4);
                        int createTime = reader.GetInt32(5);
                        int lastModifiedTime = reader.GetInt32(6);
                        int closedBy = reader.GetInt32(7);
                        string response = reader.GetString(8);
                        int completed = reader.GetInt32(9);
                        int viewed = reader.GetInt32(10);

                        reader.Close();

                        var command2 = new MySqlCommand("UPDATE gm_ticket SET viewed = @viewedCount WHERE id = @id", connection);
                        command2.Parameters.AddWithValue("@viewedCount", viewed + 1);
                        command2.Parameters.AddWithValue("@id", id);
                        command2.ExecuteNonQuery();

                        return new Ticket
                        {
                            id = id,
                            type = type,
                            playerGUID = playerGuid,
                            name = name,
                            description = description,
                            createTime = createTime,
                            lastModifiedTime = lastModifiedTime,
                            closedBy = closedBy,
                            response = response,
                            completed = completed,
                            viewed = viewed + 1
                        };
                    }
                }

                return null;
            }
        }

        public IEnumerable<Ticket> GetAllTickets()
        {
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT id, type, name, closedBy FROM gm_ticket", connection);
                var reader = command.ExecuteReader();
                var tickets = new List<Ticket>();

                while (reader.Read())
                {
                    int type = reader.GetInt32(1);

                    // Check if the ticket is already closed, and don't list
                    if (type == 0)
                    {
                        tickets.Add(new Ticket
                        {
                            id = reader.GetInt32(0),
                            name = reader.GetString(2)
                        });
                    }
                }

                return tickets;
            }
        }
    }
}
