using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Title = resources.Language.HelpTitle ?? "Справка";
            TitleText.Text = resources.Language.HelpMainTitle ?? "Реализованные функции";

            var sections = new List<HelpSection>
            {
                new HelpSection(resources.Language.File ?? "Файл", new[]
                {
                    resources.Language.New ?? "Создать",
                    resources.Language.Open ?? "Открыть",
                    resources.Language.Save ?? "Сохранить",
                    resources.Language.SaveAs ?? "Сохранить как",
                    resources.Language.Exit ?? "Выход"
                }),
                new HelpSection(resources.Language.Edit ?? "Редактировать", new[]
                {
                    resources.Language.Undo ?? "Отменить (Ctrl+Z)",
                    resources.Language.Redo ?? "Вернуть (Ctrl+Y)",
                    resources.Language.Cut ?? "Вырезать (Ctrl+X)",
                    resources.Language.Copy ?? "Копировать (Ctrl+C)",
                    resources.Language.Paste ?? "Вставить (Ctrl+V)",
                    resources.Language.Delete ?? "Удалить",
                    resources.Language.SelectAll ?? "Выделить всё (Ctrl+A)"
                }),
                new HelpSection(resources.Language.Text ?? "Текст", new[]
                {
                    resources.Language.FontSize ?? "Изменение размера шрифта",
                    resources.Language.Other ?? "Другой..."
                }),
                new HelpSection(resources.Language.HelpTitle ?? "Справка", new[]
                {
                    resources.Language.HelpContent ?? "Вызов справки (F1)",
                    resources.Language.About ?? "О программе (Ctrl+F1)",
                    resources.Language.Start ?? "Пуск (F5)"
                })
            };

            ContentControl.ItemsSource = sections;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }

    public class HelpSection
    {
        public string Header { get; set; }
        public List<string> Items { get; set; }

        public HelpSection(string header, string[] items)
        {
            Header = header;
            Items = new List<string>(items);
        }
    }
}