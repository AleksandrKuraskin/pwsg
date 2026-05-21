using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DfaSimulator
{
    public partial class MainWindow : Window
    {
        private readonly DfaManager _manager = new DfaManager();
        private readonly DfaRenderer _renderer;
        private readonly DfaSimulationEngine _simulation = new DfaSimulationEngine();
        
        private bool _isDragging = false;
        private bool _isUpdatingUI = false;
        private DfaState? _draggedState = null;
        private Point _dragStartOffset;
        private readonly DispatcherTimer _simTimer;

        public MainWindow()
        {
            InitializeComponent();
            _renderer = new DfaRenderer(DfaDrawCanvas);
            
            _simTimer = new DispatcherTimer();
            _simTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _simTimer.Tick += SimTimer_Tick;

            LoadInitialSetup();
        }

        private void LoadInitialSetup()
        {
            _manager.AddState(new DfaState { Id = "s0", Label = "q0", X = 180, Y = 240, IsInitial = true, IsAccepting = true, FillColor = "#F0FDF4", BorderColor = "#16A34A" });
            _manager.AddState(new DfaState { Id = "s1", Label = "q1", X = 380, Y = 240, IsInitial = false, IsAccepting = false, FillColor = "#F8FAFC", BorderColor = "#64748B" });

            _manager.Transitions.Add(new DfaTransition { Id = "t0", FromId = "s0", ToId = "s0", Symbols = new List<string> { "1" } });
            _manager.Transitions.Add(new DfaTransition { Id = "t1", FromId = "s0", ToId = "s1", Symbols = new List<string> { "0" } });
            _manager.Transitions.Add(new DfaTransition { Id = "t2", FromId = "s1", ToId = "s1", Symbols = new List<string> { "1" } });
            _manager.Transitions.Add(new DfaTransition { Id = "t3", FromId = "s1", ToId = "s0", Symbols = new List<string> { "0" } });

            InputWordTextBox.Text = "100";
            
            RefreshUI();
        }

        private void RefreshUI()
        {
            _renderer.Refresh(_manager, _simulation.SimActiveStateId, _simulation.SimActiveTransitionId, TransitionPath_Click);
            UpdateDropdowns();
            UpdateAlphabetPanel();
            UpdateStateEditorForm();
        }

        private void TransitionPath_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is Shape shape && shape.Tag is DfaTransition trans)
            {
                _manager.ActiveTransition = trans;
                _manager.ActiveState = null;
                ActiveTransitionDescBlock.Text = $"Z {_manager.States.FirstOrDefault(s => s.Id == trans.FromId)?.Label} do {_manager.States.FirstOrDefault(s => s.Id == trans.ToId)?.Label} na literach: {string.Join(",", trans.Symbols)}";
                ActiveTransitionBox.Visibility = Visibility.Visible;
                DfaDrawCanvas.Focus();
                RefreshUI();
            }
        }

        private void DfaDrawCanvas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.Source != DfaDrawCanvas) return;
            Point pt = e.GetPosition(DfaDrawCanvas);
            int idx = 0;
            while (_manager.States.Any(s => s.Label == $"q{idx}")) idx++;

            var state = new DfaState { Label = $"q{idx}", X = pt.X, Y = pt.Y, IsInitial = _manager.States.Count == 0 };
            _manager.AddState(state);
            _manager.ActiveState = state;
            _manager.ActiveTransition = null;
            ActiveTransitionBox.Visibility = Visibility.Collapsed;
            RefreshUI();
        }

        private void DfaDrawCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                DfaDrawCanvas_MouseDoubleClick(sender, e);
                return;
            }

            DependencyObject dep = e.OriginalSource as DependencyObject;
            while (dep != null && dep != DfaDrawCanvas)
            {
                if (dep is Ellipse circle && circle.Tag is DfaState state)
                {
                    _isDragging = true;
                    _draggedState = state;
                    
                    // CRITICAL: Calculate offset relative to the state's center coordinate
                    Point pt = e.GetPosition(DfaDrawCanvas);
                    _dragStartOffset = new Point(pt.X - state.X, pt.Y - state.Y);
                    
                    _manager.ActiveState = state;
                    _manager.ActiveTransition = null;
                    ActiveTransitionBox.Visibility = Visibility.Collapsed;
                    
                    Mouse.Capture(DfaDrawCanvas);
                    
                    RefreshUI();
                    e.Handled = true;
                    return;
                }
                dep = VisualTreeHelper.GetParent(dep);
            }
            _manager.ActiveState = null;
            _manager.ActiveTransition = null;
            ActiveTransitionBox.Visibility = Visibility.Collapsed;
            RefreshUI();
        }

        private void DfaDrawCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _draggedState = null;
            DfaDrawCanvas.ReleaseMouseCapture();
        }

        private void DfaDrawCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedState == null) return;
            
            // Safety: Cancel drag if mouse is not physically pressed
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _isDragging = false;
                _draggedState = null;
                Mouse.Capture(null);
                return;
            }

            Point pt = e.GetPosition(DfaDrawCanvas);
            
            // Apply coordinates relatively using the stored offset
            _draggedState.X = pt.X - _dragStartOffset.X;
            _draggedState.Y = pt.Y - _dragStartOffset.Y;
            
            _renderer.Refresh(_manager, _simulation.SimActiveStateId, _simulation.SimActiveTransitionId, TransitionPath_Click);
        }

        private void UpdateStateEditorForm()
        {
            if (_manager.ActiveState != null)
            {
                _isUpdatingUI = true;
                NoStateWarningBlock.Visibility = Visibility.Collapsed;
                StateEditorForm.Visibility = Visibility.Visible;
                StateLabelTextBox.Text = _manager.ActiveState.Label;
                StateIsInitialChk.IsChecked = _manager.ActiveState.IsInitial;
                StateIsAcceptingChk.IsChecked = _manager.ActiveState.IsAccepting;
                RadiusSlider.Value = _manager.ActiveState.Radius;
                BorderWidthSlider.Value = _manager.ActiveState.BorderWidth;
                FillColorComboBox.Text = _manager.ActiveState.FillColor.ToUpper();
                StrokeColorComboBox.Text = _manager.ActiveState.BorderColor.ToUpper();
                _isUpdatingUI = false;
            }
            else
            {
                NoStateWarningBlock.Visibility = Visibility.Visible;
                StateEditorForm.Visibility = Visibility.Collapsed;
            }
        }

        private void StateLabelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _manager.ActiveState == null) return;
            _manager.ActiveState.Label = StateLabelTextBox.Text;
            UpdateDropdowns();
            _renderer.Refresh(_manager, _simulation.SimActiveStateId, _simulation.SimActiveTransitionId, TransitionPath_Click);
        }

        private void StateCheckChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUI || _manager.ActiveState == null) return;

            bool isInitialChecked = StateIsInitialChk.IsChecked == true;
            bool isAcceptingChecked = StateIsAcceptingChk.IsChecked == true;

            // Enforcement: Cannot uncheck the only initial state
            if (!isInitialChecked && _manager.ActiveState.IsInitial)
            {
                _isUpdatingUI = true;
                StateIsInitialChk.IsChecked = true;
                _isUpdatingUI = false;
                return;
            }

            _manager.ActiveState.IsInitial = isInitialChecked;
            _manager.ActiveState.IsAccepting = isAcceptingChecked;

            if (_manager.ActiveState.IsInitial) 
                _manager.EnsureSingleInitial(_manager.ActiveState.Id);

            RefreshUI();
        }

        private void StateColorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUI || _manager.ActiveState == null) return;
            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
            {
                string colorString = item.Content.ToString() ?? "#FFFFFF";
                if (cb == FillColorComboBox) _manager.ActiveState.FillColor = colorString;
                else _manager.ActiveState.BorderColor = colorString;
                _renderer.Refresh(_manager, _simulation.SimActiveStateId, _simulation.SimActiveTransitionId, TransitionPath_Click);
            }
        }

        private void StateSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUI || _manager.ActiveState == null) return;
            if (sender == RadiusSlider)
            {
                _manager.ActiveState.Radius = RadiusSlider.Value;
                if (RadiusLabel != null) RadiusLabel.Text = $"Promień ({Math.Round(RadiusSlider.Value)}px)";
            }
            else if (sender == BorderWidthSlider)
            {
                _manager.ActiveState.BorderWidth = BorderWidthSlider.Value;
                if (BorderWidthLabel != null) BorderWidthLabel.Text = $"Grubość ({Math.Round(BorderWidthSlider.Value)}px)";
            }
            _renderer.Refresh(_manager, _simulation.SimActiveStateId, _simulation.SimActiveTransitionId, TransitionPath_Click);
        }

        private void DeleteState_Click(object sender, RoutedEventArgs e)
        {
            if (_manager.ActiveState == null) return;
            _manager.DeleteState(_manager.ActiveState);
            _manager.ActiveState = null;
            RefreshUI();
        }

        private void UpdateDropdowns()
        {
            var items = _manager.States.Select(s => new { Id = s.Id, Display = s.Label }).ToList();
            TransFromComboBox.ItemsSource = items;
            TransFromComboBox.DisplayMemberPath = "Display";
            TransFromComboBox.SelectedValuePath = "Id";
            TransToComboBox.ItemsSource = items;
            TransToComboBox.DisplayMemberPath = "Display";
            TransToComboBox.SelectedValuePath = "Id";
            if (items.Count > 0 && TransFromComboBox.SelectedIndex == -1)
            {
                TransFromComboBox.SelectedIndex = 0;
                TransToComboBox.SelectedIndex = Math.Min(1, items.Count - 1);
            }
        }

        private void AddTransition_Click(object sender, RoutedEventArgs e)
        {
            if (TransFromComboBox.SelectedValue == null || TransToComboBox.SelectedValue == null) return;
            string symbolsStr = TransSymbolsTextBox.Text.Trim();
            if (string.IsNullOrEmpty(symbolsStr)) { MessageBox.Show("Etykieta przejścia nie może być pusta."); return; }
            List<string> enteredSymbols = symbolsStr.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (enteredSymbols.Count == 0) return;

            string fId = TransFromComboBox.SelectedValue.ToString();
            string tId = TransToComboBox.SelectedValue.ToString();
            var existing = _manager.Transitions.FirstOrDefault(t => t.FromId == fId && t.ToId == tId);
            if (existing != null) existing.Symbols = existing.Symbols.Union(enteredSymbols).Distinct().ToList();
            else _manager.Transitions.Add(new DfaTransition { FromId = fId, ToId = tId, Symbols = enteredSymbols });

            RefreshUI();
        }

        private void DeleteTransition_Click(object sender, RoutedEventArgs e)
        {
            if (_manager.ActiveTransition == null) return;
            _manager.Transitions.Remove(_manager.ActiveTransition);
            _manager.ActiveTransition = null;
            ActiveTransitionBox.Visibility = Visibility.Collapsed;
            RefreshUI();
        }

        private void UpdateAlphabetPanel()
        {
            var alpha = _manager.GetAlphabet();
            AlphabetBlock.Text = alpha.Count > 0 ? "Σ = { " + string.Join(", ", alpha.OrderBy(x => x)) + " }" : "Brak przejść";
            UpdateSimulation();
        }

        private void UpdateSimulation()
        {
            _simTimer.Stop();
            SimPlayBtn.Content = "▶ Uruchom";
            string inputWord = InputWordTextBox.Text.Trim();
            var alpha = _manager.GetAlphabet();
            bool invalidWord = inputWord.Any(c => !alpha.Contains(c.ToString()));

            if (invalidWord && inputWord.Length > 0) InputWordWarningBlock.Visibility = Visibility.Visible;
            else InputWordWarningBlock.Visibility = Visibility.Collapsed;

            _simulation.UpdateSimulation(inputWord, _manager);
            ApplyStepSimulation();
        }

        private void RenderWordTracePreview()
        {
            WordPreviewPanel.Children.Clear();
            string inputWord = InputWordTextBox.Text.Trim();
            if (inputWord.Length == 0)
            {
                WordPreviewPanel.Children.Add(new TextBlock { Text = "<puste słowo>", FontSize = 14, FontStyle = FontStyles.Italic, Foreground = Brushes.SlateGray });
                return;
            }
            for (int i = 0; i < inputWord.Length; i++)
            {
                bool isConsumed = i < _simulation.CurrentStepIndex;
                bool isCurrent = i == _simulation.CurrentStepIndex && _simulation.CurrentStepIndex < _simulation.SimSteps.Count - 1;
                Border chBorder = new Border {
                    Background = isCurrent ? _renderer.BrushFromHex("#FACC15") : isConsumed ? _renderer.BrushFromHex("#E2E8F0") : Brushes.Transparent,
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(2, 0, 2, 0)
                };
                chBorder.Child = new TextBlock {
                    Text = inputWord[i].ToString(), FontWeight = isCurrent ? FontWeights.ExtraBold : FontWeights.Bold, FontSize = 16,
                    Foreground = isConsumed ? _renderer.BrushFromHex("#94A3B8") : _renderer.BrushFromHex("#1E293B"),
                    TextDecorations = isConsumed ? TextDecorations.Strikethrough : null
                };
                WordPreviewPanel.Children.Add(chBorder);
            }
        }

        private void ApplyStepSimulation()
        {
            _simulation.ApplyStepSimulation(_manager);
            StepsListView.ItemsSource = _simulation.SimSteps.Take(_simulation.CurrentStepIndex + 1).ToList();
            StepsListView.ScrollIntoView(StepsListView.Items.Count > 0 ? StepsListView.Items[StepsListView.Items.Count - 1] : null);

            bool isAtEnd = _simulation.CurrentStepIndex == _simulation.SimSteps.Count - 1;
            if (_simulation.CurrentStepIndex > 0 || (_simulation.SimSteps.Count > 0 && isAtEnd))
            {
                VerdictCard.Visibility = Visibility.Visible;
                var currStep = _simulation.SimSteps[_simulation.CurrentStepIndex];
                var realState = _manager.States.FirstOrDefault(s => s.Label == currStep.StateId);
                if (isAtEnd)
                {
                    bool isAccepted = realState != null && realState.IsAccepting && currStep.StateId != "ERR";
                    VerdictStatusBorder.Background = isAccepted ? _renderer.BrushFromHex("#ECFDF5") : _renderer.BrushFromHex("#FEF2F2");
                    VerdictStatusBorder.BorderBrush = isAccepted ? _renderer.BrushFromHex("#059669") : _renderer.BrushFromHex("#EF4444");
                    VerdictTitleBlock.Text = isAccepted ? "Zaakceptowane!" : "Odrzucone";
                    VerdictTitleBlock.Foreground = isAccepted ? _renderer.BrushFromHex("#047857") : _renderer.BrushFromHex("#B91C1C");
                    VerdictDescBlock.Text = isAccepted ? $"Automat zatrzymał się w stanie akceptującym ({currStep.StateId})." :
                                            (currStep.StateId == "ERR" ? "Brak przejścia - słowo odrzucone." : $"Automat zakończył bieg w stanie nieakceptującym ({currStep.StateId}).");
                }
                else
                {
                    VerdictStatusBorder.Background = _renderer.BrushFromHex("#EFF6FF");
                    VerdictStatusBorder.BorderBrush = _renderer.BrushFromHex("#3B82F6");
                    VerdictTitleBlock.Text = "Obliczenia w toku...";
                    VerdictTitleBlock.Foreground = _renderer.BrushFromHex("#1D4ED8");
                    VerdictDescBlock.Text = $"Aktualny stan: {currStep.StateId}. Krok {_simulation.CurrentStepIndex} z {_simulation.SimSteps.Count - 1}.";
                }
            }
            else VerdictCard.Visibility = Visibility.Collapsed;

            RenderWordTracePreview();
            _renderer.Refresh(_manager, _simulation.SimActiveStateId, _simulation.SimActiveTransitionId, TransitionPath_Click);
        }

        private void SimStepNext_Click(object sender, RoutedEventArgs e) { if (_simulation.CurrentStepIndex < _simulation.SimSteps.Count - 1) { _simulation.CurrentStepIndex++; ApplyStepSimulation(); } }
        private void SimStepPrev_Click(object sender, RoutedEventArgs e) { if (_simulation.CurrentStepIndex > 0) { _simulation.CurrentStepIndex--; ApplyStepSimulation(); } }
        private void SimReset_Click(object sender, RoutedEventArgs e) { _simTimer.Stop(); SimPlayBtn.Content = "▶ Uruchom"; _simulation.CurrentStepIndex = 0; ApplyStepSimulation(); }
        private void SimPlay_Click(object sender, RoutedEventArgs e) { if (_simTimer.IsEnabled) { _simTimer.Stop(); SimPlayBtn.Content = "▶ Uruchom"; } else { if (_simulation.CurrentStepIndex >= _simulation.SimSteps.Count - 1) _simulation.CurrentStepIndex = 0; _simTimer.Start(); SimPlayBtn.Content = "⏸ Pauza"; } }
        private void SimTimer_Tick(object sender, EventArgs e) { if (_simulation.CurrentStepIndex < _simulation.SimSteps.Count - 1) { _simulation.CurrentStepIndex++; ApplyStepSimulation(); } else { _simTimer.Stop(); SimPlayBtn.Content = "▶ Uruchom"; } }
        private void SimSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (_simTimer != null) { _simTimer.Interval = TimeSpan.FromMilliseconds(SimSpeedSlider.Value); if (SpeedLabelBlock != null) SpeedLabelBlock.Text = $"Szybkość animacji: {Math.Round(SimSpeedSlider.Value)}ms"; } }
        private void InputWordTextBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateSimulation();

        private void ExportJson_Click(object sender, RoutedEventArgs e) => DfaPersistence.ExportJson(new AutomatonData { States = _manager.States, Transitions = _manager.Transitions });
        private void ImportJson_Click(object sender, RoutedEventArgs e) {
            var data = DfaPersistence.ImportJson();
            if (data != null) { _manager.States = data.States; _manager.Transitions = data.Transitions; _manager.ActiveState = null; _manager.ActiveTransition = null; ActiveTransitionBox.Visibility = Visibility.Collapsed; RefreshUI(); }
        }
        private void SavePng_Click(object sender, RoutedEventArgs e) => DfaPersistence.SavePng(DfaDrawCanvas);

        private void TabBtn_Checked(object sender, RoutedEventArgs e) {
            if (LabPanel == null || HomePanel == null) return;
            LabPanel.Visibility = TabLabBtn.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            HomePanel.Visibility = TabHomeBtn.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
        private void TabBtn_Click(object sender, RoutedEventArgs e) => DfaDrawCanvas.Focus();
        private void LoadEvenZerosPreset_Click(object sender, RoutedEventArgs e) { _manager.States.Clear(); _manager.Transitions.Clear(); LoadInitialSetup(); }
        private void LoadEndsWith01Preset_Click(object sender, RoutedEventArgs e) {
            _manager.States.Clear(); _manager.Transitions.Clear();
            _manager.AddState(new DfaState { Id = "q0", Label = "q0", X = 120, Y = 240, IsInitial = true, IsAccepting = false });
            _manager.AddState(new DfaState { Id = "q1", Label = "q1", X = 280, Y = 160, IsInitial = false, IsAccepting = false });
            _manager.AddState(new DfaState { Id = "q2", Label = "q2", X = 440, Y = 240, IsInitial = false, IsAccepting = true, FillColor = "#ECFDF5", BorderColor = "#059669" });
            _manager.Transitions.Add(new DfaTransition { Id = "tx0", FromId = "q0", ToId = "q0", Symbols = new List<string> { "1" } });
            _manager.Transitions.Add(new DfaTransition { Id = "tx1", FromId = "q0", ToId = "q1", Symbols = new List<string> { "0" } });
            _manager.Transitions.Add(new DfaTransition { Id = "tx2", FromId = "q1", ToId = "q1", Symbols = new List<string> { "0" } });
            _manager.Transitions.Add(new DfaTransition { Id = "tx3", FromId = "q1", ToId = "q2", Symbols = new List<string> { "1" } });
            _manager.Transitions.Add(new DfaTransition { Id = "tx4", FromId = "q2", ToId = "q0", Symbols = new List<string> { "1" } });
            _manager.Transitions.Add(new DfaTransition { Id = "tx5", FromId = "q2", ToId = "q1", Symbols = new List<string> { "0" } });
            InputWordTextBox.Text = "1001";
            RefreshUI();
        }
    }
}
