using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows; // For MessageBox

namespace Ticket_Genie
{
    [DataContract]
    public struct SQLConnectionSettingsInfo
    {
        [DataMember]
        public string hostname { get; set; }
        [DataMember]
        public int port { get; set; }
        [DataMember]
        public string characterDatabase { get; set; }
        [DataMember]
        public string authDatabase { get; set; }
        [DataMember]
        public string worldDatabase { get; set; }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string password { get; set; }
    }

    [DataContract]
    public struct SOAPConnectionSettingsInfo
    {
        [DataMember]
        public string hostname { get; set; }
        [DataMember]
        public int port { get; set; }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string password { get; set; }
    }

    public static class JsonTools
    {
        private static T DeserializeJSON<T>(FileStream stream)
        {
            try
            {
                if (stream.Length == 0)
                    throw new SerializationException("File is empty");
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                T obj = (T)serializer.ReadObject(stream);
                return obj;
            }
            finally
            {
                stream.Close();
            }
        }

        public static void SerializeSQLConnectionSettingsJSON<SQLConnectionSettingsInfo>(SQLConnectionSettingsInfo SQLSettings)
        {
            using (FileStream stream = File.Open("SQLConnectionSettings.json", FileMode.Create))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SQLConnectionSettingsInfo));
                serializer.WriteObject(stream, SQLSettings);
            }
        }

        public static void SerializeSOAPConnectionSettingsJSON<SOAPConnectionSettingsInfo>(SOAPConnectionSettingsInfo SOAPSettings)
        {
            using (FileStream stream = File.Open("SOAPConnectionSettings.json", FileMode.Create))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SOAPConnectionSettingsInfo));
                serializer.WriteObject(stream, SOAPSettings);
            }
        }

        private static SQLConnectionSettingsInfo DeserializeSQLConnectionSettingsJSON()
        {
            try
            {
                using (FileStream stream = File.Open("SQLConnectionSettings.json", FileMode.Open))
                {
                    return DeserializeJSON<SQLConnectionSettingsInfo>(stream);
                }
            }
            catch (SerializationException ex)
            {
                MessageBox.Show($"SQL settings file is invalid or empty. Default settings will be recreated.\n{ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                File.Delete("SQLConnectionSettings.json");
                File.Delete("SOAPConnectionSettings.json");
                CreateDefaultJsonFiles();
                using (FileStream stream = File.Open("SQLConnectionSettings.json", FileMode.Open))
                {
                    return DeserializeJSON<SQLConnectionSettingsInfo>(stream);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Could not read SQL settings file.\n{ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public static SOAPConnectionSettingsInfo DeserializeSOAPConnectionSettingsJSON()
        {
            try
            {
                using (FileStream stream = File.Open("SOAPConnectionSettings.json", FileMode.Open))
                {
                    return DeserializeJSON<SOAPConnectionSettingsInfo>(stream);
                }
            }
            catch (SerializationException ex)
            {
                MessageBox.Show($"SOAP settings file is invalid or empty. Default settings will be recreated.\n{ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                File.Delete("SQLConnectionSettings.json");
                File.Delete("SOAPConnectionSettings.json");
                CreateDefaultJsonFiles();
                using (FileStream stream = File.Open("SOAPConnectionSettings.json", FileMode.Open))
                {
                    return DeserializeJSON<SOAPConnectionSettingsInfo>(stream);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Could not read SOAP settings file.\n{ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public static Task<SQLConnectionSettingsInfo> GetSQLConnectionSettingsAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                return DeserializeSQLConnectionSettingsJSON();
            });
        }

        public static Task<SOAPConnectionSettingsInfo> GetSOAPConnectionSettingsAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                return DeserializeSOAPConnectionSettingsJSON();
            });
        }

        public static void CreateDefaultJsonFiles()
        {
            SQLConnectionSettingsInfo defaultSQLSettings = new SQLConnectionSettingsInfo
            {
                hostname = Properties.Settings.Default.SQLHost,
                port = Properties.Settings.Default.SQLPort,
                characterDatabase = Properties.Settings.Default.CharacterDB,
                authDatabase = Properties.Settings.Default.AuthDB,
                worldDatabase = Properties.Settings.Default.WorldDB,
                username = Properties.Settings.Default.SQLUsername,
                password = Properties.Settings.Default.SQLPassword
            };

            SOAPConnectionSettingsInfo defaultSOAPSettings = new SOAPConnectionSettingsInfo
            {
                hostname = Properties.Settings.Default.SOAPHost,
                port = Properties.Settings.Default.SOAPPort,
                username = Properties.Settings.Default.SOAPUsername,
                password = Properties.Settings.Default.SOAPPassword
            };

            SerializeSQLConnectionSettingsJSON<SQLConnectionSettingsInfo>(defaultSQLSettings);
            SerializeSOAPConnectionSettingsJSON<SOAPConnectionSettingsInfo>(defaultSOAPSettings);
        }

        public static void UpdateConnectionSettings()
        {
            // Load SQL connection settings from JSON and save to the application settings
            SQLConnectionSettingsInfo sqlSettings = DeserializeSQLConnectionSettingsJSON();
            Properties.Settings.Default.SQLHost = sqlSettings.hostname;
            Properties.Settings.Default.SQLPort = sqlSettings.port;
            Properties.Settings.Default.CharacterDB = sqlSettings.characterDatabase;
            Properties.Settings.Default.AuthDB = sqlSettings.authDatabase;
            Properties.Settings.Default.WorldDB = sqlSettings.worldDatabase;
            Properties.Settings.Default.SQLUsername = sqlSettings.username;
            Properties.Settings.Default.SQLPassword = sqlSettings.password;
            // Load SOAP connection settings from JSON and save to the application settings
            SOAPConnectionSettingsInfo soapSettings = DeserializeSOAPConnectionSettingsJSON();
            Properties.Settings.Default.SOAPHost = soapSettings.hostname;
            Properties.Settings.Default.SOAPPort = soapSettings.port;
            Properties.Settings.Default.SOAPUsername = soapSettings.username;
            Properties.Settings.Default.SOAPPassword = soapSettings.password;
            Properties.Settings.Default.Save(); // Save all the settings
        }
    }
}
