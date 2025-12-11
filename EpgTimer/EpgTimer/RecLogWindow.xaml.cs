using EpgTimer.Common;
using EpgTimer.DefineClass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EpgTimer
{
    /// <summary>
    /// PopupWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class RecLogWindow : Window
    {

        static readonly string _trimWord_Default = @"(
\[[^\]]+\]
| (【[^】]+】)+
| (［[^］]+］)+
| ^(\(５\．１\)|\(5\.1\))
|^▽
| (◆|▼).+$
| ＜[^＞]+＞
| （[^）]+）
| \([^\)]+\)
| 出演：.+$
|「|」|『|』
| 🈟 | 🈞 | 🈔 | 🈑 | 🈖 | 🈕 | 🅍 | 🅊 | 🈙 | 🅂  | 🈡 | 🈓 | 🈥 | 🈠 | 🈚 | 🈢
)+";
        static readonly string _trimWord_NG = @"!";    //  Regexで問題となる文字
        MainWindow _mainWindow = Application.Current.MainWindow as MainWindow;
        EpgEventInfo _epgEventInfo;
        RecFileInfo _recFileInfo;
        List<RecLogItem> _resultList = new List<RecLogItem>();
        MenuItem _menuItem = new MenuItem() { };
        SolidColorBrush _background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
        SolidColorBrush _background_Selected = new SolidColorBrush(Colors.LightYellow);
        /// <summary>
        /// EPG.IDが一致したRecLogItem
        /// </summary>
        RecLogItem _selectedRecLogItem = null;

        #region - Constructor -
        #endregion

        public RecLogWindow(Window owner0)
        {
            InitializeComponent();
            //
            Owner = owner0;
        }

        #region - Method -
        #endregion

        public static void searchByWeb(string txtKey0)
        {
            string txtKey1 = trimKeyword(txtKey0);
            string uriStr1 = Settings.Instance.MenuSet.SearchURI + System.Uri.EscapeDataString(txtKey1);
            try
            {
                System.Diagnostics.Process.Start(uriStr1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("'検索のURI'の設定を確認してください。");
            }
        }

        public void showResult(ReserveData reserveData0)
        {
            reset();
            if (Settings.Instance.RecLog_SearchLog_IsEnabled)
            {
                _selectedRecLogItem = _mainWindow.recLogView.db_RecLog.exists(RecLogItem.RecodeStatuses.ALL,
                   reserveData0.OriginalNetworkID, reserveData0.TransportStreamID, reserveData0.ServiceID, reserveData0.EventID, reserveData0.StartTime);
                if (_selectedRecLogItem != null)
                {
                    switch (_selectedRecLogItem.recodeStatus)
                    {
                        case RecLogItem.RecodeStatuses.録画完了:
                            menuItem_ChangeStatus_NONE.IsEnabled = true;
                            menuItem_ChangeStatus_Recorded.IsEnabled = false;
                            break;
                        default:
                            menuItem_ChangeStatus_NONE.IsEnabled = false;
                            menuItem_ChangeStatus_Recorded.IsEnabled = true;
                            break;
                    }
                }
                search(reserveData0.Title, _selectedRecLogItem);
            }
            else
            {
                drawText(RecLogView.notEnabledMessage);
            }
            show();
        }

        public void showResult(EpgEventInfo epgEventInfo0)
        {
            reset();
            if (Settings.Instance.RecLog_SearchLog_IsEnabled)
            {
                _epgEventInfo = epgEventInfo0;
                _selectedRecLogItem = _mainWindow.recLogView.db_RecLog.exists(RecLogItem.RecodeStatuses.ALL,
                   epgEventInfo0.original_network_id, epgEventInfo0.transport_stream_id, epgEventInfo0.service_id, epgEventInfo0.event_id, epgEventInfo0.start_time);
                if (_selectedRecLogItem != null)
                {
                    switch (_selectedRecLogItem.recodeStatus)
                    {
                        case RecLogItem.RecodeStatuses.録画完了:
                            menuItem_ChangeStatus_NONE.IsEnabled = true;
                            menuItem_ChangeStatus_Recorded.IsEnabled = false;
                            break;
                        default:
                            menuItem_ChangeStatus_NONE.IsEnabled = false;
                            menuItem_ChangeStatus_Recorded.IsEnabled = true;
                            break;
                    }
                }
                search(epgEventInfo0.ShortInfo.event_name, _selectedRecLogItem, epgEventInfo0.ContentInfo);
            }
            else
            {
                drawText(RecLogView.notEnabledMessage);
            }
            show();
        }

        public void showResult(RecFileInfo recFileInfo0)
        {
            reset();
            if (Settings.Instance.RecLog_SearchLog_IsEnabled)
            {
                _recFileInfo = recFileInfo0;
                _selectedRecLogItem = _mainWindow.recLogView.db_RecLog.exists(recFileInfo0);
                if (_selectedRecLogItem != null)
                {
                    switch (_selectedRecLogItem.recodeStatus)
                    {
                        case RecLogItem.RecodeStatuses.録画完了:
                            menuItem_ChangeStatus_NONE.IsEnabled = true;
                            menuItem_ChangeStatus_Recorded.IsEnabled = false;
                            break;
                        default:
                            menuItem_ChangeStatus_NONE.IsEnabled = false;
                            menuItem_ChangeStatus_Recorded.IsEnabled = true;
                            break;
                    }
                }
                search(recFileInfo0.Title, _selectedRecLogItem);
            }
            else
            {
                drawText(RecLogView.notEnabledMessage);
            }
            show();
        }

        void search(string searchWord0, RecLogItem selectedRecLogItem = null, EpgContentInfo epgContentInfo0 = null)
        {
            string selectedItem1 = null;
            string searchWord1 = trimKeyword(searchWord0);
            _resultList = _mainWindow.recLogView.getRecLogList(searchWord1, Settings.Instance.RecLogWindow_SearchResultLimit, epgContentInfo0: epgContentInfo0);
            List<string> lines1 = new List<string>();
            if (0 < _resultList.Count)
            {
                foreach (RecLogItem item in _resultList)
                {
                    string line1 = "[" + item.recodeStatus_Abbr + "]" + "[’" + item.epgEventInfoR.start_time.ToString("yy/MM/dd") + "] " +
                        item.epgEventInfoR.ShortInfo.event_name;
                    if (selectedRecLogItem != null && selectedRecLogItem.ID == item.ID)
                    {
                        selectedItem1 = line1;
                    }
                    else
                    {
                        lines1.Add(line1);
                    }
                }
            }
            else
            {
                lines1.Add("(NOT FOUND)");
            }
            //
            //if (string.IsNullOrEmpty(selectedItem1))
            //{
            //    richTextBox_SelectedItem.Visibility = Visibility.Collapsed;
            //}
            //else
            {
                richTextBox_SelectedItem.Visibility = Visibility.Visible;
                drawText(richTextBox_SelectedItem, new List<string>() { selectedItem1 }, _background_Selected);
            }
            textBox.Text = searchWord1;
            drawText(lines1);
        }

        /// <summary>
        /// 前後の記号を取り除く
        /// </summary>
        /// <param name="txtKey0"></param>
        /// <returns></returns>
        public static string trimKeyword(string txtKey0, string trimWord0 = "")
        {
            string txtKey1 = txtKey0.Trim();
            if (trimWord0 == "")
            {
                trimWord0 = trimWord;
            }
            trimWord0 = trimWord0.Trim();
            if (!string.IsNullOrEmpty(trimWord0))
            {
                trimWord0 += "|";
            }
            trimWord0 += _trimWord_NG;
            Regex rgx1 = new Regex(trimWord0, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            string txtKey2 = rgx1.Replace(txtKey1, " ").Trim();

            return txtKey2;
        }

        void drawText(string text0)
        {
            drawText(new List<string>() { text0 });
        }

        void drawText(List<string> texts0)
        {
            drawText(richTextBox, texts0, _background);
        }

        void drawText(RichTextBox rtBox0, List<string> texts0, SolidColorBrush background0)
        {
            rtBox0.Document.Blocks.Clear();
            foreach (var text1 in texts0)
            {
                Paragraph paragraph1 =
                    new Paragraph(
                        new Run(text1))
                    {
                        Background = background0
                    };
                rtBox0.Document.Blocks.Add(paragraph1);
            }
        }

        void show() {
            GetCursorPos(out POINT mousePoint);
            // OwnerのDPIスケールを取得
            double dpiScale = 1.0;
            if (Owner != null) {
                PresentationSource source = PresentationSource.FromVisual(Owner);
                if (source != null) {
                    dpiScale = source.CompositionTarget.TransformToDevice.M11;
                }
            }
            // DPIスケールを考慮してWPF座標に変換
            double wpfX = mousePoint.X / dpiScale;
            double wpfY = mousePoint.Y / dpiScale;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = wpfX - 50;
            Top = wpfY - 50;
            base.ShowDialog();
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT {
            public int X;
            public int Y;
        }

        void hide()
        {
            this.Visibility = Visibility.Collapsed;
        }

        void reset()
        {
            menuItem_ChangeStatus_Recorded.IsEnabled = true;
            richTextBox_SelectedItem.Visibility = Visibility.Visible;
            _resultList.Clear();
            _epgEventInfo = null;
            _recFileInfo = null;
            _selectedRecLogItem = null;
            richTextBox.Document.Blocks.Clear();
        }

        #region - Property -
        #endregion

        double Right
        {
            get { return Left + Width; }
        }

        double Bottom
        {
            get { return Top + Height; }
        }

        DB_RecLog db_RecLog
        {
            get { return _mainWindow.recLogView.db_RecLog; }
        }

        public bool isTrimWordEditor
        {
            get { return this._isTrimWordEditor; }
            set
            {
                this._isTrimWordEditor = value;
                if (value)
                {
                    drawText(trimWord);
                    richTextBox.IsReadOnly = false;
                    panel_TrimWord.Visibility = Visibility.Visible;
                    richTextBox_SelectedItem.Visibility = Visibility.Visible;
                    if (_epgEventInfo != null)
                    {
                        if (_epgEventInfo.ShortInfo != null)
                        {
                            drawText(richTextBox_SelectedItem, new List<string>() { _epgEventInfo.ShortInfo.event_name }, _background);
                        }
                    }
                    else if (_recFileInfo != null)
                    {
                        drawText(richTextBox_SelectedItem, new List<string>() { _recFileInfo.Title }, _background);
                    }
                }
                else
                {
                    richTextBox.IsReadOnly = true;
                    panel_TrimWord.Visibility = Visibility.Hidden;
                    reset();
                }
            }
        }
        bool _isTrimWordEditor = false;

        static string trimWord
        {
            get
            {
                if (_trimWord == null)
                {
                    if (Settings.Instance.RecLog_TrimWord == null)
                    {
                        _trimWord = _trimWord_Default;
                    }
                    else
                    {
                        _trimWord = Settings.Instance.RecLog_TrimWord;
                    }
                }
                return _trimWord;
            }
            set { _trimWord = value; }
        }
        static string _trimWord = null;

        #region - Event Handler -
        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            // EpgTimerとともに終了する
            e.Cancel = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            //
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    break;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            //
            if (isTrimWordEditor) { return; }
            //
            Point pnt_Client1 = Mouse.GetPosition(this);
            Point pnt_Screen1 = PointToScreen(pnt_Client1);

            double left1 = Left + border.Margin.Left;
            double right1 = Right - border.Margin.Right;
            double top1 = Top + border.Margin.Top;
            double bottom1 = Bottom - border.Margin.Bottom;

            if (pnt_Screen1.X < left1 || right1 < pnt_Screen1.X || pnt_Screen1.Y < top1 || bottom1 < pnt_Screen1.Y)
            {
                hide();
            }
        }

        void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    richTextBox_SelectedItem.Visibility = Visibility.Collapsed;
                    menuItem_ChangeStatus_Recorded.IsEnabled = false;
                    search(textBox.Text);
                    break;
            }
        }

        void menuItem_ChangeStatus_Recorded_Click(object sender, RoutedEventArgs e)
        {
            DateTime lastUpdate1 = DateTime.Now;
            RecLogItem recLogItem1 = null;
            if (_epgEventInfo != null)
            {
                recLogItem1 = db_RecLog.exists(_epgEventInfo);
                if (recLogItem1 == null)
                {
                    recLogItem1 = new RecLogItem()
                    {
                        lastUpdate = lastUpdate1,
                        recodeStatus = RecLogItem.RecodeStatuses.録画完了,
                        epgEventInfoR = new EpgEventInfoR(_epgEventInfo, lastUpdate1)
                    };
                    db_RecLog.insert(recLogItem1);
                }
                else
                {
                    recLogItem1.recodeStatus = RecLogItem.RecodeStatuses.録画完了;
                    db_RecLog.update(recLogItem1);
                }
            }
            else if (_recFileInfo != null)
            {
                recLogItem1 = db_RecLog.exists(_recFileInfo);
                if (recLogItem1 == null)
                {
                    recLogItem1 = db_RecLog.insert(_recFileInfo, lastUpdate1);
                }
                else
                {
                    recLogItem1.recodeStatus = RecLogItem.RecodeStatuses.録画完了;
                    db_RecLog.update(recLogItem1);
                }
            }
            if (recLogItem1 != null)
            {
                string line1 = "[録]" + "[’" + recLogItem1.epgEventInfoR.start_time.ToString("yy/MM/dd") + "] " + recLogItem1.epgEventInfoR.ShortInfo.event_name;
                drawText(richTextBox_SelectedItem, new List<string>() { line1 }, _background_Selected);
                menuItem_ChangeStatus_NONE.IsEnabled = true;
                menuItem_ChangeStatus_Recorded.IsEnabled = false;
            }
        }

        private void menuItem_ChangeStatus_NONE_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecLogItem != null)
            {
                _selectedRecLogItem.recodeStatus = RecLogItem.RecodeStatuses.NONE;
                db_RecLog.delete(new RecLogItem[] { _selectedRecLogItem });
                drawText(richTextBox_SelectedItem, new List<string>() { "" }, _background_Selected);
                menuItem_ChangeStatus_NONE.IsEnabled = false;
                menuItem_ChangeStatus_Recorded.IsEnabled = true;
            }
        }

        private void richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_recFileInfo != null && 0 < _resultList.Count)
            {
                e.Handled = true;
            }
        }

        private void menu_TextBox_TrimWord_Click(object sender, RoutedEventArgs e)
        {
            isTrimWordEditor = true;
        }

        private void button_TrimWord_Save_Click(object sender, RoutedEventArgs e)
        {
            string text1 = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
            Settings.Instance.RecLog_TrimWord = text1;
            Settings.SaveToXmlFile();
            trimWord = text1;

            isTrimWordEditor = false;
        }

        private void button_TrimWord_Test_Click(object sender, RoutedEventArgs e)
        {
            string text1 = new TextRange(richTextBox_SelectedItem.Document.ContentStart, richTextBox_SelectedItem.Document.ContentEnd).Text;
            string rgxStr1 = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
            textBox.Text = trimKeyword(text1, rgxStr1);
        }

        private void button_TrimWord_Default_Click(object sender, RoutedEventArgs e)
        {
            drawText(_trimWord_Default);
        }

        private void button_TrimWord_Cancel_Click(object sender, RoutedEventArgs e)
        {
            isTrimWordEditor = false;
        }

    }
}
