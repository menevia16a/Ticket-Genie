using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Xml;

namespace Ticket_Genie
{
    public class TCSOAPService
    {
        /*
         * Returns the result of the command
         */
        public string Call(string command)
        {
            try
            {
                String requestBody = "<?xml version=\"1.0\" encoding=\"UTF - 8\"?><SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:SOAP-ENC=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:ns1=\"urn:TC\"><SOAP-ENV:Body><ns1:executeCommand><command>" + command + "</command></ns1:executeCommand></SOAP-ENV:Body></SOAP-ENV:Envelope>";

                var request = (HttpWebRequest)WebRequest.Create(string.Format("http://{0}:{1}", Properties.Settings.Default.SOAPHost, Properties.Settings.Default.SOAPPort));

                var data = Encoding.UTF8.GetBytes(requestBody);
                request.Method = "POST";
                request.ContentType = "text/xml";
                request.ContentLength = data.Length;
                request.Accept = "text/xml";
                request.Headers.Add("SOAPAction: executeCommand");
                var encodedPasswd = Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Properties.Settings.Default.SOAPUsername, Properties.Settings.Default.SOAPPassword));
                request.Headers.Add("Authorization: Basic " + Convert.ToBase64String(encodedPasswd));

                using (var stream = request.GetRequestStream())
                    stream.Write(data, 0, data.Length);

                var response = (HttpWebResponse)request.GetResponse();
                var rsStream = response.GetResponseStream();

                return ExtractResponseStringFromSOAPResponse(new StreamReader(rsStream).ReadToEnd());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Show the ConnectionSettingsWindow
                var connectionSettingsWindow = new ConnectionSettingsWindow();
                if (connectionSettingsWindow.ShowDialog() == true)
                {
                    // Update application connection settings from JSON
                    JsonTools.UpdateConnectionSettings();
                    // Call the function again
                    return Call(command);
                }
                else
                    return String.Empty;
            }   
        }

        private string ExtractResponseStringFromSOAPResponse(string xmlSOAPresponse)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlSOAPresponse);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmgr.AddNamespace("SOAP-ENC", "http://schemas.xmlsoap.org/soap/encoding/");
            nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            nsmgr.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            nsmgr.AddNamespace("ns1", "urn:TC");

            // Select a specific node
            XmlNode node = xml.SelectSingleNode("SOAP-ENV:Envelope/SOAP-ENV:Body/ns1:executeCommandResponse/result", nsmgr);

            return node.InnerText;
        }

        public bool ExecuteSOAPCommand(string command)
        {
            if (Call(command)?.Length == 0)
            {
                MessageBox.Show("Error executing SOAP command.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
