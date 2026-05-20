using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Title = resources.Language.About ?? "О программе";
            AppNameText.Text = resources.Language.AboutText ?? "Текстовый редактор для языкового процессора\r\nВерсия 1.0\r\n@Авдюков Б.А.";

        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}