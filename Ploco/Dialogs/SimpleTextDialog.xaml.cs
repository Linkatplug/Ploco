using System.Windows;

namespace Ploco.Dialogs
{
    public partial class SimpleTextDialog : Window
    {
        public string ResponseText => InputText.Text.Trim();

        public SimpleTextDialog(string title, string prompt, string initialValue)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputText.Text = initialValue;
            InputText.SelectAll();
            InputText.Focus();
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResponseText))
            {
                MessageBox.Show("Veuillez saisir une valeur.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
