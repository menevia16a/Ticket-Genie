using System.Windows;

namespace Ticket_Genie
{
    public partial class InputDialog : Window
    {
        public string InputText { get; private set; }
        private InputDialog(string prompt, bool isPassword)
        {
            InitializeComponent();
            PromptText.Text = prompt;
            if (isPassword)
            {
                InputBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordBox.Focus();
            }
            else
            {
                InputBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                InputBox.Focus();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputBox.Visibility == Visibility.Visible ? InputBox.Text : PasswordBox.Password;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            InputText = null;
            DialogResult = false;
            Close();
        }

        public static string ShowDialog(string prompt, bool isPassword)
        {
            var dialog = new InputDialog(prompt, isPassword)
            {
                Owner = Application.Current?.MainWindow
            };
            return dialog.ShowDialog() == true ? dialog.InputText : null;
        }
    }
}
