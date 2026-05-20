using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static WpfApp1.AutomatonSearch;
using MessageBox = System.Windows.MessageBox;

namespace WpfApp1
{
    public class EditorTab : INotifyPropertyChanged
    {
        private string _filePath;
        public ICSharpCode.AvalonEdit.TextEditor Editor { get; set; }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        public string FileName => string.IsNullOrEmpty(FilePath) ? "Новый документ" : Path.GetFileName(FilePath);

        public TextDocument Document { get; } = new TextDocument();

        public ICommand CloseCommand { get; }

        public EditorTab()
        {
            CloseCommand = new RelayCommand(Close);
        }

        private void Close()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.CloseTab(this);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<SearchResults> _searchResults = new ObservableCollection<SearchResults>();
        private ObservableCollection<SyntaxError> _syntaxErrors = new ObservableCollection<SyntaxError>();
        private ObservableCollection<SyntaxError> _semanticErrors = new ObservableCollection<SyntaxError>();
        private string _astText = string.Empty;


        private static double _currentFontSize = 12;

        public ObservableCollection<EditorTab> Tabs { get; } = new ObservableCollection<EditorTab>();
        private Dictionary<MenuItem, string> _originalHeaders = new Dictionary<MenuItem, string>();
        private HashSet<ICSharpCode.AvalonEdit.TextEditor> _subscribedEditors = new HashSet<ICSharpCode.AvalonEdit.TextEditor>();

        private EditorTab _selectedTab;
        public EditorTab SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab == value) return;
                var old = _selectedTab;
                _selectedTab = value;
                System.Diagnostics.Debug.WriteLine($"SelectedTab setter: old={(old == null ? "null" : old.FileName + "#" + old.GetHashCode())} -> new={(value == null ? "null" : value.FileName + "#" + value.GetHashCode())}");
                OnPropertyChanged(nameof(SelectedTab));
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateStatusBar();
                    try
                    {
                        var ed = GetCurrentEditor();
                        ed?.Focus();
                    }
                    catch { }
                }), DispatcherPriority.Background);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            SyntaxErrorGrid.ItemsSource = _syntaxErrors;
            SemanticErrorGrid.ItemsSource = _semanticErrors;
            SearchDataGrid.ItemsSource = _searchResults;

            this.DataContext = this;

            Tabs.Add(new EditorTab());
            SelectedTab = Tabs[0];

            SaveOriginalHeaders();

            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru-RU");
            UpdateUI();

            SetDefaultLanguageCheck();

            if (DataGrid1 != null)
                DataGrid1.FontSize = _currentFontSize;

            UpdateStatusBar();

            this.Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    var editor = GetCurrentEditor();
                    if (editor != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Window.Loaded: editor found for first tab");
                        editor.Focus();
                    }
                    UpdateStatusBar();
                }));
            };
            Tabs.CollectionChanged += (s, e) => ClearSearchResults();
        }
        private void ClearSearchResults()
        {
            _searchResults.Clear();
            txtSearchCount.Text = "Найдено: 0";
        }
        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string filePath in files)
                {
                    if (File.Exists(filePath))
                    {
                        OpenFileInNewTab(filePath);
                    }
                }
            }
        }
        private void OpenFileInNewTab(string filePath)
        {
            try
            {
                foreach (var tab in Tabs)
                {
                    if (tab.FilePath == filePath)
                    {
                        SelectedTab = tab;
                        System.Diagnostics.Debug.WriteLine($"OpenFileInNewTab: file already open, selecting tab '{tab.FileName}'");
                        return;
                    }
                }

                var newTab = new EditorTab { FilePath = filePath };
                newTab.Document.Text = File.ReadAllText(filePath);
                Tabs.Add(newTab);
                System.Diagnostics.Debug.WriteLine($"OpenFileInNewTab: added tab for '{newTab.FileName}', selecting it");
                SelectedTab = newTab;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SetDefaultLanguageCheck()
        {
            foreach (var mainItem in mainMenu.Items)
            {
                if (mainItem is MenuItem menuItem && menuItem.Header.ToString() == "Справка")
                {
                    foreach (var subItem in menuItem.Items)
                    {
                        if (subItem is MenuItem langMenu && langMenu.Header.ToString() == "Язык")
                        {
                            foreach (var langItem in langMenu.Items)
                            {
                                if (langItem is MenuItem mi && mi.Header.ToString() == "Русский")
                                {
                                    mi.IsChecked = true;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
        private ICSharpCode.AvalonEdit.TextEditor CurrentEditor => SelectedTab?.Editor;

        private void DumpVisualTree(DependencyObject obj, int level)
        {
            if (obj == null) return;
            string indent = new string(' ', level * 2);
            System.Diagnostics.Debug.WriteLine($"{indent}{obj.GetType().Name}");
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DumpVisualTree(VisualTreeHelper.GetChild(obj, i), level + 1);
            }
        }
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public void CloseTab(EditorTab tab)
        {
            if (Tabs.Contains(tab))
            {
                Tabs.Remove(tab);
                if (Tabs.Count == 0)
                {
                    Tabs.Add(new EditorTab());
                }
                UpdateStatusBar();
            }
        }

        private void new_file_Click(object sender, RoutedEventArgs e)
        {
            Tabs.Add(new EditorTab());
            System.Diagnostics.Debug.WriteLine($"new_file_Click: added new tab, total tabs={Tabs.Count}");
            SelectedTab = Tabs[Tabs.Count - 1];
            UpdateStatusBar();
            ClearSearchResults();
        }

        private void open_file_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { };
            if (ofd.ShowDialog() == true)
            {
                var newTab = new EditorTab { FilePath = ofd.FileName };
                newTab.Document.Text = File.ReadAllText(ofd.FileName);
                Tabs.Add(newTab);
                SelectedTab = newTab;
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => UpdateStatusBar()));
            }
            ClearSearchResults();
        }

        private void save_file_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTab == null) return;

            if (string.IsNullOrEmpty(SelectedTab.FilePath))
            {
                save_as_file_Click(sender, e);
                return;
            }

            File.WriteAllText(SelectedTab.FilePath, SelectedTab.Document.Text);
        }

        private void save_as_file_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTab == null) return;

            var sfd = new SaveFileDialog
            {
                Filter = "Text files(*.txt)|*.txt|C# files (*.cs)|*.cs|Xaml files (*.xaml)|*.xaml|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, SelectedTab.Document.Text);
                SelectedTab.FilePath = sfd.FileName;
                UpdateStatusBar();
            }
        }

        private void exitApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            CurrentEditor?.Undo();
        }

        private void front_Click(object sender, RoutedEventArgs e)
        {
            CurrentEditor?.Redo();
        }

        private void cut_Click(object sender, RoutedEventArgs e)
        {
            CurrentEditor?.Cut();
        }

        private void copy_Click(object sender, RoutedEventArgs e)
        {
            CurrentEditor?.Copy();
        }

        private void paste_Click(object sender, RoutedEventArgs e)
        {
            CurrentEditor?.Paste();
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentEditor != null)
                CurrentEditor.SelectedText = "";
        }

        private void selectAll_Click(object sender, RoutedEventArgs e)
        {

            var editor = CurrentEditor;
            editor?.SelectAll();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"TabControl_SelectionChanged: SelectedTab={(SelectedTab == null ? "null" : SelectedTab.FileName + "#" + SelectedTab.GetHashCode())}");
            var ed = GetCurrentEditor();
            if (ed != null)
            {
                try { ed.Focus(); } catch { }
                System.Diagnostics.Debug.WriteLine($"TabControl_SelectionChanged: focused editor={(ed == null ? "null" : ed.GetHashCode().ToString())}");
            }
            UpdateStatusBar();
            ClearSearchResults();
        }

        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            var editor = sender as ICSharpCode.AvalonEdit.TextEditor;
            if (editor?.DataContext is EditorTab tab)
            {
                tab.Editor = editor;
                editor.FontSize = _currentFontSize;
                UpdateStatusBar();
            }
        }
        private void about_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void questions_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }
        private void SetFontSize_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Tag is string sizeStr && double.TryParse(sizeStr, out double size))
            {
                foreach (var tab in Tabs)
                    if (tab.Editor != null)
                        tab.Editor.FontSize = size;

                if (DataGrid1 != null)
                    DataGrid1.FontSize = size;

                var parent = menuItem.Parent as MenuItem;
                if (parent != null)
                {
                    foreach (var item in parent.Items)
                    {
                        if (item is MenuItem mi)
                            mi.IsChecked = false;
                    }
                }

                _currentFontSize = size;
                menuItem.IsChecked = true;
                UpdateStatusBar();
            }
        }
        private void SetCustomFontSize_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = string.IsNullOrEmpty(resources.Language.FontSize)
                    ? "Размер шрифта"
                    : resources.Language.FontSize,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrEmpty(resources.Language.EnterSize)
                    ? "Введите размер (6-48):"
                    : resources.Language.EnterSize,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var textBox = new TextBox { Text = CurrentEditor?.FontSize.ToString() ?? "12" };
            textBox.SelectAll();
            panel.Children.Add(textBox);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var okBtn = new Button
            {
                Content = resources.Language.OK ?? "OK",
                Width = 60,
                Height = 22,
                Margin = new Thickness(0, 0, 5, 0),
                IsDefault = true
            };

            var cancelBtn = new Button
            {
                Content = resources.Language.Cancel ?? "Отмена",
                Width = 60,
                Height = 22,
                IsCancel = true
            };

            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            panel.Children.Add(btnPanel);
            dialog.Content = panel;

            okBtn.Click += (s, args) =>
            {
                if (double.TryParse(textBox.Text, out double size))
                {
                    size = Math.Max(6, Math.Min(48, size));

                    foreach (var tab in Tabs)
                        if (tab.Editor != null)
                            tab.Editor.FontSize = size;

                    if (DataGrid1 != null)
                        DataGrid1.FontSize = size;

                    if (mainMenu.Items[2] is MenuItem textMenu &&
                        textMenu.Items[7] is MenuItem fontSizeMenu)
                    {
                        foreach (var menuItem in fontSizeMenu.Items)
                        {
                            if (menuItem is MenuItem mi && mi.Header.ToString() != resources.Language.Other)
                            {
                                mi.IsChecked = false;
                            }
                        }
                    }

                    _currentFontSize = size;
                    UpdateStatusBar();

                    dialog.Close();
                }
                else
                {
                    MessageBox.Show(
                        resources.Language.ErrorInvalidNumber ?? "Введите число!",
                        resources.Language.FontSize ?? "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };

            dialog.ShowDialog();
        }
        private void SetRussian_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru-RU");

            UpdateUI();

            ((MenuItem)sender).IsChecked = true;
            foreach (var item in ((MenuItem)((MenuItem)sender).Parent).Items)
            {
                if (item is MenuItem mi && mi != sender)
                    mi.IsChecked = false;
            }
            UpdateStatusBar();
        }

        private void SetEnglish_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

            UpdateUI();

            ((MenuItem)sender).IsChecked = true;
            foreach (var item in ((MenuItem)((MenuItem)sender).Parent).Items)
            {
                if (item is MenuItem mi && mi != sender)
                    mi.IsChecked = false;
            }
            UpdateStatusBar();
        }
        private void SetMongolian_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("mn-MN");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("mn-MN");

            UpdateUI();

            ((MenuItem)sender).IsChecked = true;
            foreach (var item in ((MenuItem)((MenuItem)sender).Parent).Items)
            {
                if (item is MenuItem mi && mi != sender)
                    mi.IsChecked = false;
            }
            UpdateStatusBar();
        }
        private void UpdateUI()
        {
            this.Title = resources.Language.WindowTitle;

            UpdateMenuItems(mainMenu);
            UpdateToolTips();
        }

        private void UpdateToolTips()
        {
            create.ToolTip = resources.Language.New;
            open.ToolTip = resources.Language.Open;
            save.ToolTip = resources.Language.Save;
            back.ToolTip = resources.Language.Undo;
            front.ToolTip = resources.Language.Redo;
            copy.ToolTip = resources.Language.Copy;
            paste.ToolTip = resources.Language.Paste;
            question.ToolTip = resources.Language.HelpContent;
            about.ToolTip = resources.Language.About;
        }

        private void UpdateMenuItems(ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items)
            {
                if (item is MenuItem menuItem)
                {
                    if (_originalHeaders.TryGetValue(menuItem, out string originalHeader))
                    {
                        string translated = GetTranslation(originalHeader);

                        if (menuItem.Header.ToString() != translated)
                        {
                            menuItem.Header = translated;
                            System.Diagnostics.Debug.WriteLine($"Переведено: {originalHeader} -> {translated}");
                        }
                    }
                    else
                    {
                        _originalHeaders[menuItem] = menuItem.Header.ToString();
                        System.Diagnostics.Debug.WriteLine($"Экстренно сохранён: {menuItem.Header}");
                    }

                    if (menuItem.Items.Count > 0)
                        UpdateMenuItems(menuItem);
                }
            }
        }
        private void SaveOriginalHeaders()
        {
            SaveOriginalHeadersRecursive(mainMenu);
        }

        private void SaveOriginalHeadersRecursive(ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items)
            {
                if (item is MenuItem menuItem)
                {
                    if (!_originalHeaders.ContainsKey(menuItem))
                    {
                        _originalHeaders[menuItem] = menuItem.Header.ToString();
                        System.Diagnostics.Debug.WriteLine($"Сохранён оригинал: {menuItem.Header}");
                    }

                    if (menuItem.Items.Count > 0)
                        SaveOriginalHeadersRecursive(menuItem);
                }
            }
        }
        private string GetTranslation(string russianText)
        {
            var translationMap = new Dictionary<string, string>
    {
        {"Файл", "File"},
        {"Правка", "Edit"},
        {"Текст", "Text"},
        {"Пуск", "Start"},
        {"Справка", "Help"},

        {"Новый файл", "New"},
        {"Создать", "New"},
        {"Открыть", "Open"},
        {"Сохранить", "Save"},
        {"Сохранить как", "SaveAs"},
        {"Выход", "Exit"},

        {"Назад", "Undo"},
        {"Заново", "Redo"},
        {"Вырезать", "Cut"},
        {"Копировать", "Copy"},
        {"Вставить", "Paste"},
        {"Удалить", "Delete"},
        {"Выделить всё", "SelectAll"},

        {"Постановка задачи", "TaskStatement"},
        {"Грамматика", "Grammar"},
        {"Классификация грамматики", "GrammarClassification"},
        {"Метод анализа", "AnalysisMethod"},
        {"Тестовый пример", "TestExample"},
        {"Список литературы", "References"},
        {"Исходный код программы", "SourceCode"},
        {"Размер шрифта", "FontSize"},

        {"Другой...", "Other"},

        {"Вызов справки", "HelpContent"},
        {"О программе", "About"},
        {"Язык", "MenuLanguage"},
        {"Русский", "Russian"},
        {"English", "English"},
        {"Монгольский", "Mongolian"},

        {"Позиция", "Position"},
        {"Код", "Code"},
        {"Ошибка", "Error"},
    };

            if (translationMap.ContainsKey(russianText))
            {
                var resource = resources.Language.ResourceManager.GetString(translationMap[russianText]);
                return resource ?? russianText;
            }

            return russianText;
        }
        private ICSharpCode.AvalonEdit.TextEditor GetCurrentEditor()
        {
            if (SelectedTab == null) return null;

            if (SelectedTab.Editor != null && SelectedTab.Editor.DataContext == SelectedTab)
            {
                if (!_subscribedEditors.Contains(SelectedTab.Editor))
                {
                    SelectedTab.Editor.TextArea.Caret.PositionChanged += (s, e) => UpdateStatusBar();
                    SelectedTab.Editor.Document.TextChanged += (s, e) => UpdateStatusBar();
                    _subscribedEditors.Add(SelectedTab.Editor);
                    System.Diagnostics.Debug.WriteLine($"GetCurrentEditor: subscribed to existing editor for {SelectedTab.FileName}");
                }
                return SelectedTab.Editor;
            }

            ICSharpCode.AvalonEdit.TextEditor found = null;

            var content = tabControl.SelectedContent;
            if (content is ICSharpCode.AvalonEdit.TextEditor te)
                found = te;
            else if (content is FrameworkElement fe)
                found = FindVisualChild<ICSharpCode.AvalonEdit.TextEditor>(fe);

            if (found == null)
                found = FindVisualChild<ICSharpCode.AvalonEdit.TextEditor>(this);

            if (found != null && found.DataContext is EditorTab tab && tab == SelectedTab)
            {
                SelectedTab.Editor = found;
                if (!_subscribedEditors.Contains(found))
                {
                    found.TextArea.Caret.PositionChanged += (s, e) => UpdateStatusBar();
                    found.Document.TextChanged += (s, e) => UpdateStatusBar();
                    _subscribedEditors.Add(found);
                    System.Diagnostics.Debug.WriteLine($"Subscribed to editor events for tab '{tab.FileName}'");
                }
                return found;
            }

            return null;
        }
        private void UpdateStatusBar()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateStatusBar: SelectedTab={(SelectedTab == null ? "null" : SelectedTab.FileName + "#" + SelectedTab.GetHashCode())}");
            }
            catch { }
            if (SelectedTab == null)
            {
                statusText.Text = resources.Language.StatusNoFile;
                cursorPosition.Text = $"{resources.Language.StatusLine}: -  {resources.Language.StatusColumn}: -";
                fileInfo.Text = $"{resources.Language.StatusChars}: 0  {resources.Language.StatusLines}: 0";
                fontSizeStatus.Text = $"{resources.Language.StatusFontSize}: -";
                return;
            }

            var doc = SelectedTab.Document;
            int charCount = doc.TextLength;
            int lineCount = doc.LineCount;
            fileInfo.Text = $"{resources.Language.StatusChars}: {charCount}  {resources.Language.StatusLines}: {lineCount}";
            statusText.Text = string.IsNullOrEmpty(SelectedTab.FilePath)
                ? resources.Language.StatusNewDocument
                : $"{resources.Language.StatusFile}: {Path.GetFileName(SelectedTab.FilePath)}";

            var editor = GetCurrentEditor();
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateStatusBar: SelectedTab.Editor={(SelectedTab.Editor == null ? "null" : SelectedTab.Editor.GetHashCode().ToString())}; GetCurrentEditor={(editor == null ? "null" : editor.GetHashCode().ToString())}");
            }
            catch { }
            if (editor != null)
            {
                int line = editor.TextArea.Caret.Line;
                int column = editor.TextArea.Caret.Column;
                cursorPosition.Text = $"{resources.Language.StatusLine}: {line}  {resources.Language.StatusColumn}: {column}";
                fontSizeStatus.Text = $"{resources.Language.StatusFontSize}: {editor.FontSize}pt";
            }
            else
            {
                cursorPosition.Text = $"{resources.Language.StatusLine}: 1  {resources.Language.StatusColumn}: 1";
                fontSizeStatus.Text = $"{resources.Language.StatusFontSize}: {_currentFontSize}pt";
            }
        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTab == null) return;
            string sourceCode = SelectedTab.Document.Text;

            // 1. Лексический анализ
            var scanner = new Scanner();
            List<Lexem> lexems = scanner.Analyze(sourceCode);
            DataGrid1.ItemsSource = lexems;

            // 2. Синтаксический анализ
            var parser = new Parser();
            List<SyntaxError> syntaxErrors = parser.Parse(lexems);

            _syntaxErrors.Clear();
            foreach (var err in syntaxErrors)
                _syntaxErrors.Add(err);

            _semanticErrors.Clear();

            ProgramNode ast = null;

            // 3. Семантический анализ (если синтаксических ошибок нет)
            if (syntaxErrors.Count == 0)
            {
                var semantic = new SemanticParser();
                try
                {
                    var result = semantic.Analyze(lexems);
                    ast = result.Ast;
                    var semSynErrors = result.SyntaxErrors;
                    var semSemErrors = result.SemanticErrors;

                    foreach (var err in semSynErrors)
                        _syntaxErrors.Add(err);
                    foreach (var err in semSemErrors)
                        _semanticErrors.Add(err);
                }
                catch (Exception ex)
                {

                    statusText.Text = $"Семантический анализатор: {ex.Message}";
                    ast = null;
                }

                int totalErrors = _syntaxErrors.Count + _semanticErrors.Count;
                errorCountText.Text = $"Ошибок: {totalErrors}";

                if (totalErrors == 0)
                    statusText.Text = "Ошибок не обнаружено";
                else
                    statusText.Text = $"Синтаксических: {_syntaxErrors.Count}, семантических: {_semanticErrors.Count}";

                if (ast != null && ast.Children.Count > 0)
                    AstOutput.Text = ast.Print();
                else
                    AstOutput.Text = "AST не построен (нет корректной структуры).";
            }
            else
            {
                errorCountText.Text = $"Ошибок: {syntaxErrors.Count}";
                statusText.Text = "Обнаружены синтаксические ошибки, семантический анализ пропущен";
                AstOutput.Text = "AST не построен из-за синтаксических ошибок.";
            }

            if (syntaxErrors.Count == 0 && ast != null)
            {
                var forNode = FindForNode(ast);
                if (forNode != null)
                {
                    try
                    {
                        var irGen = new IRGenerator();
                        var irCode = irGen.Generate(forNode);

                        IrBeforeBox.Text = string.Join(Environment.NewLine, irCode.Select(i => i.ToString()));

                        var afterUnrolling = Optimizer.LoopUnrolling(irCode);
                        var afterDeadLabels = Optimizer.RemoveDeadLabels(afterUnrolling);

                        IrAfterBox.Text = string.Join(Environment.NewLine, afterDeadLabels.Select(i => i.ToString()));
                    }
                    catch (Exception ex)
                    {
                        IrBeforeBox.Text = $"Ошибка генерации IR: {ex.Message}";
                        IrAfterBox.Text = "";
                    }
                }
                else
                {
                    IrBeforeBox.Text = "Не найден узел ForNode в AST. Генерация IR возможна только для цикла for.";
                    IrAfterBox.Text = "";
                }
            }
            else
            {
                IrBeforeBox.Text = "Синтаксические ошибки – генерация IR невозможна.";
                IrAfterBox.Text = "";
            }
        }

        private ForNode FindForNode(AstNode node)
        {
            if (node is ForNode f) return f;
            foreach (var child in node.Children)
            {
                var result = FindForNode(child);
                if (result != null) return result;
            }
            return null;
        }
        private void DataGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGrid1.SelectedItem is Lexem lexem && lexem.IsError)
            {
                var editor = GetCurrentEditor();
                if (editor != null && SelectedTab != null)
                {
                    var document = SelectedTab.Document;
                    int offset = document.GetOffset(lexem.Line, lexem.StartPos);
                    editor.Select(offset, 0);
                    editor.ScrollToLine(lexem.Line);
                    editor.Focus();
                }
            }
        }
        private void SyntaxErrorGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SyntaxErrorGrid.SelectedItem is SyntaxError error && error.Line > 0 && error.Column > 0)
            {
                var editor = GetCurrentEditor();
                if (editor != null && SelectedTab != null)
                {
                    var document = SelectedTab.Document;
                    int offset = document.GetOffset(error.Line, error.Column);
                    editor.Select(offset, 0);
                    editor.ScrollToLine(error.Line);
                    editor.Focus();
                }
            }
        }

        private void SemanticErrorGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SemanticErrorGrid.SelectedItem is SyntaxError error && error.Line > 0 && error.Column > 0)
            {
                var editor = GetCurrentEditor();
                if (editor != null && SelectedTab != null)
                {
                    var document = SelectedTab.Document;
                    int offset = document.GetOffset(error.Line, error.Column);
                    editor.Select(offset, 0);
                    editor.ScrollToLine(error.Line);
                    editor.Focus();
                }
            }
        }
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            var editor = GetCurrentEditor();
            if (editor == null || string.IsNullOrEmpty(editor.Text))
            {
                statusText.Text = "Нет текста для поиска";
                ClearSearchResults();
                return;
            }

            string text = editor.Text;
            var selectedItem = cmbSearchType.SelectedItem as ComboBoxItem;
            string tag = selectedItem?.Tag?.ToString();

            _searchResults.Clear();

            try
            {
                if (tag == "PythonComments")
                {
                    var autoResults = AutomatonSearch.FindTimeOccurrences(text);
                    foreach (var res in autoResults)
                        _searchResults.Add(res);
                }
                else
                {
                    string pattern = GetSelectedPattern();
                    if (string.IsNullOrEmpty(pattern))
                    {
                        MessageBox.Show("Выберите тип поиска", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var regex = new Regex(pattern, RegexOptions.Multiline);
                    var matches = regex.Matches(text);
                    foreach (Match match in matches)
                    {
                        int offset = match.Index;
                        var location = editor.Document.GetLocation(offset);
                        _searchResults.Add(new SearchResults
                        {
                            FoundText = match.Value,
                            Position = $"строка {location.Line}, столбец {location.Column}",
                            Length = match.Length,
                            Offset = offset,
                            Line = location.Line,
                            Column = location.Column
                        });
                    }
                }

                txtSearchCount.Text = $"Найдено: {_searchResults.Count}";
                statusText.Text = _searchResults.Count > 0 ? $"Найдено {_searchResults.Count} совпадений" : "Совпадений не найдено";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetSelectedPattern()
        {
            var selectedItem = cmbSearchType.SelectedItem as ComboBoxItem;
            string tag = selectedItem?.Tag?.ToString();
            switch (tag)
            {
                case "WordsNotEndingWithS":

                    return @"\b\w+(?<![sS])\b";

                case "PythonComments":

                    return @"#.*$";

                case "Time":

                    return @"\b(?:[01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]\b";

                default:
                    return null;
            }
        }
        private void SearchDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchDataGrid.SelectedItem is SearchResults result)
            {
                var editor = GetCurrentEditor();
                if (editor != null)
                {
                    editor.Select(result.Offset, result.Length);
                    editor.ScrollToLine(result.Line);
                    editor.Focus();
                }
            }
        }
    }
}