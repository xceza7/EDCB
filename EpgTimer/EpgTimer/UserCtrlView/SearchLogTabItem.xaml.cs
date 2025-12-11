using EpgTimer.Common;
using EpgTimer.DefineClass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace EpgTimer.UserCtrlView
{
    /// <summary>
    /// ・DB_RecLogを使用
    /// ・自動予約登録更新に合わせて検索結果を更新
    /// ・
    /// </summary>
    public partial class SearchLogTabItem : UserControl
    {

        public event EventHandler tabHeaderChanged;

        ObservableCollection<SearchLogResultItem> _searchLogResultItems = new ObservableCollection<SearchLogResultItem>();
        List<DateItem> _dateItemList = new List<DateItem>();
        ObservableCollection<ContentKindInfo> _contentKindInfoList = new ObservableCollection<ContentKindInfo>();
        ObservableCollection<ServiceViewItem> _serviceList_Tere = new ObservableCollection<ServiceViewItem>();
        ObservableCollection<ServiceViewItem> _serviceList_BS = new ObservableCollection<ServiceViewItem>();
        ObservableCollection<ServiceViewItem> _serviceList_CS = new ObservableCollection<ServiceViewItem>();
        ObservableCollection<ServiceViewItem> _serviceList_1seg = new ObservableCollection<ServiceViewItem>();
        ObservableCollection<ServiceViewItem> _serviceList_Other = new ObservableCollection<ServiceViewItem>();

        ObservableCollection<SearchLogItem> _searchLogItems = new ObservableCollection<SearchLogItem>();
        /// <summary>
        /// 編集中
        /// </summary>
        SearchLogNotWordItem _notWordItem = null;
        /// <summary>
        /// ListView.ItemsSource
        /// </summary>
        ObservableCollection<SearchLogNotWordItem> _notWordItems = new ObservableCollection<SearchLogNotWordItem>();
        SolidColorBrush _brush_Transparent = new SolidColorBrush(Colors.Transparent);
        SolidColorBrush _brush_Yellow = new SolidColorBrush(Colors.Yellow);
        BackgroundWorker _bgw_UpdateTabItem = new BackgroundWorker();
        BackgroundWorker _bgw_UpdateSearchResults = new BackgroundWorker();
        BackgroundWorker _bgw_UpdateReserveInfo = new BackgroundWorker();
        BackgroundWorker _bgw_UpdateResultDB = new BackgroundWorker();
        bool _isEpgUpdated = true;
        int _counter_ReserveInfoUpdated = 0;
        SearchLogItem _logItem_Drag = null;
        MenuItem _menuItem_Move2Tab = new MenuItem() { Header = "タブ間移動" };
        MenuItem _menuItem_Move2Tab_None = new MenuItem() { Header = "(なし)" };
        /// <summary>
        /// 検索条件が変更された？
        /// </summary> 
        bool _isSearchLogItemEdited = false;
        MainWindow _mainWindow = Application.Current.MainWindow as MainWindow;

        #region - Constructor -
        #endregion

        public SearchLogTabItem()
        {
            InitializeComponent();
            //
            comboBox_Editor_Content.ItemsSource = CommonManager.ContentKindList;
            setServiceListView();
            comboBox_Editor_Filter_Genre.ItemsSource = CommonManager.ContentKindList;
            //
            init_bgw_UpdateResultDB();
            init_bgw_UpdateReserveInfo();
            init_bgw_UpdateTabItem();
            init_bgw_UpdateSearchResults();
            //
            listBox_Editor_Date.ItemsSource = _dateItemList;
            listBox_Editor_Content.ItemsSource = _contentKindInfoList;
            listView_Edit_NotWord.ItemsSource = _notWordItems;
            listView_service_Tera.ItemsSource = _serviceList_Tere;
            listView_service_BS.ItemsSource = _serviceList_BS;
            listView_service_CS.ItemsSource = _serviceList_CS;
            //
            listView_serviceEditor_Tera.ItemsSource = _serviceList_Tere;
            listView_serviceEditor_BS.ItemsSource = _serviceList_BS;
            listView_serviceEditor_CS.ItemsSource = _serviceList_CS;
            //
            listView_SearchLog.ItemsSource = _searchLogItems;
            listView_Result.ItemsSource = _searchLogResultItems;

            reset_ServiceEditor();
        }

        #region - Method -
        #endregion

        public static ScrollViewer GetScrollViewer(DependencyObject o)
        {
            // Return the DependencyObject if it is a ScrollViewer
            if (o is ScrollViewer)
            {
                return o as ScrollViewer;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        public void addSearchLogItem(List<SearchLogItem> searchLogItems0)
        {
            int listOrder1 = _searchLogItems.Count;
            foreach (var item1 in searchLogItems0)
            {
                listOrder1++;
                item1.listOrder = listOrder1;
                _searchLogItems.Add(item1);
            }
        }

        public void deleteSearchLogItem_All()
        {
            deleteSearchLogItem(_searchLogItems, false);
        }

        public void showTagHeaderEditor()
        {
            border_ChangeHeader.Visibility = Visibility.Visible;
        }

        public void update_EpgData()
        {
            _isEpgUpdated = true;
        }

        public void update_ReserveInfo()
        {
            _counter_ReserveInfoUpdated++;
            if (IsVisible)
            {
                if (_bgw_UpdateReserveInfo.IsBusy)
                {
                    Trace.WriteLine("_bgw_UpdateReserveInfo.IsBusy");
                }
                else
                {
                    _bgw_UpdateReserveInfo.RunWorkerAsync();
                }
            }
        }

        void updateResultDB()
        {
            if (_bgw_UpdateResultDB.IsBusy)
            {
                Trace.WriteLine("_bgw_UpdateResultDB.IsBusy");
            }
            else
            {
                _bgw_UpdateResultDB.RunWorkerAsync();
            }
        }

        void init_bgw_UpdateSearchResults()
        {
            _bgw_UpdateSearchResults.WorkerReportsProgress = true;
            _bgw_UpdateSearchResults.DoWork += delegate
            {
                db_SearchLog.getSearchResults(_searchLogItems, search, _bgw_UpdateSearchResults);
            };
            _bgw_UpdateSearchResults.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                progressBar_Search.Value = e.ProgressPercentage;
            };
            _bgw_UpdateSearchResults.RunWorkerCompleted += delegate
            {
                _isEpgUpdated = false;
                listView_SearchLog.SelectedItems.Clear();
                updateResultDB();
            };
        }

        void init_bgw_UpdateTabItem()
        {
            _bgw_UpdateTabItem.DoWork += delegate
            {
                foreach (var logItem1 in db_SearchLog.selectByTab(tabInfo.ID))
                {
                    if (!_searchLogItems.Contains(logItem1))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _searchLogItems.Add(logItem1);
                        }));
                    }
                }
            };
        }

        void init_bgw_UpdateReserveInfo()
        {
            _bgw_UpdateReserveInfo.RunWorkerCompleted += delegate
            {
                updateResultDB();
            };
            _bgw_UpdateReserveInfo.DoWork += delegate
            {
                while (0 < _counter_ReserveInfoUpdated)
                {
                    _counter_ReserveInfoUpdated = 0;
                    //
                    List<SearchLogItem> searchLogItems1;
                    if (logItem_Edit != null && !_searchLogItems.Contains(logItem_Edit))
                    {  // 登録せずに検索を実行した場合にも予約ステータスを表示させる
                        searchLogItems1 = new List<SearchLogItem>(_searchLogItems);
                        searchLogItems1.Add(logItem_Edit);
                    }
                    else
                    {
                        searchLogItems1 = _searchLogItems.ToList();
                    }
                    var reserveDict1 = new Dictionary<UInt64, ReserveData>();
                    foreach (ReserveData reserveData1 in CommonManager.Instance.DB.ReserveList.Values)
                    {
                        reserveDict1[reserveData1.Create64PgKey()] = reserveData1;
                    }
                    for (int i = 0; i < searchLogItems1.Count; i++)
                    {
                        foreach (SearchLogResultItem resultItem1 in searchLogItems1[i].resultItems)
                        {
                            ReserveData reserveData1;
                            if (reserveDict1.TryGetValue(resultItem1.epgEventInfoR.Create64PgKey(), out reserveData1))
                            {
                                if (reserveData1.RecSetting.IsEnable)
                                {
                                    resultItem1.recodeStatus = RecLogItem.RecodeStatuses.予約済み;
                                }
                                else
                                {
                                    resultItem1.recodeStatus = RecLogItem.RecodeStatuses.無効登録;
                                }
                            }
                            else
                            {
                                switch (resultItem1.recodeStatus)
                                {
                                    case RecLogItem.RecodeStatuses.予約済み:
                                    case RecLogItem.RecodeStatuses.無効登録:
                                        resultItem1.recodeStatus = RecLogItem.RecodeStatuses.NONE;  // 予約が削除された
                                        break;
                                }
                            }
                        }
                    }
                }
            };
        }

        void init_bgw_UpdateResultDB()
        {
            _bgw_UpdateResultDB.WorkerReportsProgress = true;
            _bgw_UpdateResultDB.DoWork += delegate
            {
                db_SearchLog.updateSearchResult(_searchLogItems, _bgw_UpdateResultDB);
            };
            _bgw_UpdateResultDB.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                progressBar_DB.Value = e.ProgressPercentage;
            };
            _bgw_UpdateResultDB.RunWorkerCompleted += delegate
            {
                panel_Progress_Search.Visibility = Visibility.Collapsed;
                panel_Progress_DB.Visibility = Visibility.Collapsed;
                changeVisibility_Resutltems();
            };
        }

        void searchAll()
        {
            if (_bgw_UpdateSearchResults.IsBusy)
            {
                Trace.WriteLine("_bgw_UpdateSearchResults.IsBusy");
            }
            else
            {
                progressBar_Search.Value = 0;
                progressBar_Search.Maximum = _searchLogItems.Count;
                panel_Progress_Search.Visibility = Visibility.Visible;
                progressBar_DB.Value = 0;
                progressBar_DB.Maximum = _searchLogItems.Count;
                panel_Progress_DB.Visibility = Visibility.Visible; ;
                _bgw_UpdateSearchResults.RunWorkerAsync();
            }
        }

        public void setServiceListView()
        {
            // サービス
            _serviceList_Tere.Clear();
            _serviceList_BS.Clear();
            _serviceList_CS.Clear();
            _serviceList_1seg.Clear();
            _serviceList_Other.Clear();
            foreach (EpgServiceInfo serviceInfo1 in ChSet5.ChListSelected)
            {
                ServiceViewItem svcItem1 = new ServiceViewItem(serviceInfo1);
                // 地デジ
                if ((0x7880 <= serviceInfo1.ONID && serviceInfo1.ONID <= 0x7FE8) &&
                    (serviceInfo1.service_type == 0x01 || serviceInfo1.service_type == 0xA5))
                {
                    svcItem1.IsSelected = (checkBox_Editor_Tere.IsChecked == true);
                    _serviceList_Tere.Add(svcItem1);
                }
                // BS
                if (serviceInfo1.ONID == 0x04 &&
                    (serviceInfo1.service_type == 0x01 || serviceInfo1.service_type == 0xA5))
                {
                    svcItem1.IsSelected = (checkBox_Editor_BS.IsChecked == true);
                    _serviceList_BS.Add(svcItem1);
                }
                // CS
                if ((serviceInfo1.ONID == 0x06 || serviceInfo1.ONID == 0x07) &&
                    (serviceInfo1.service_type == 0x01 || serviceInfo1.service_type == 0xA5))
                {
                    svcItem1.IsSelected = (checkBox_Editor_CS.IsChecked == true);
                    _serviceList_CS.Add(svcItem1);
                }
                // 1seg
                if ((0x7880 <= serviceInfo1.ONID && serviceInfo1.ONID <= 0x7FE8)
                    //&& item.ServiceInfo.partialReceptionFlag == 1
                    )
                {
                    _serviceList_1seg.Add(svcItem1);
                }
                // other
                if (serviceInfo1.ONID != 0x04 &&
                    serviceInfo1.ONID != 0x06 &&
                    serviceInfo1.ONID != 0x07 &&
                    !(0x7880 <= serviceInfo1.ONID && serviceInfo1.ONID <= 0x7FE8))
                {
                    _serviceList_Other.Add(svcItem1);
                }
            }
        }

        void setLogItem2Editor(SearchLogItem logItem0, bool isShowEditor0 = true)
        {
            if (logItem0 == null) { return; }
            if (logItem0.epgSearchKeyInfoS == null) { return; } // 検索ログアイテムをダブルクリックするとNullReferenceExceptionが発生することがあった。原因不明
            //
            logItem_Edit = logItem0;
            clearEditor();

            textBox_Editor_SeachLogName.Text = logItem0.name;
            var searchKey1 = logItem0.epgSearchKeyInfoS;
            checkBox_Editor_SearchLogName.IsChecked = (logItem0.name == searchKey1.andKey);
            textBox_Editor_AndKey.Text = searchKey1.andKey;
            textBox_Editor_NotKey.Text = searchKey1.notKey;
            checkBox_Editor_regExpFlag.IsChecked = (searchKey1.regExpFlag == 1);
            checkBox_Editor_aimaiFlag.IsChecked = (searchKey1.aimaiFlag == 1);
            checkBox_Editor_titleOnlyFlag.IsChecked = (searchKey1.titleOnlyFlag == 1);
            checkBox_Editor_caseFlag.IsChecked = (searchKey1.caseFlag == 1);

            foreach (EpgContentData item in searchKey1.contentList)
            {
                ContentKindInfo cki1;
                if (CommonManager.ContentKindDictionary.TryGetValue(item.Key, out cki1))
                {
                    _contentKindInfoList.Add(cki1);
                }
            }
            checkBox_Editor_notContent.IsChecked = (searchKey1.notContetFlag == 1);

            foreach (var item in _serviceList_Tere.Concat(_serviceList_BS).Concat(_serviceList_CS).Concat(_serviceList_1seg).Concat(_serviceList_Other))
            {
                item.IsSelected = (searchKey1.serviceList.Contains((long)item.Key));
            }

            foreach (EpgSearchDateInfo info in searchKey1.dateList)
            {
                //String viewText = "";

                //viewText = CommonManager.DayOfWeekDictionary[info.startDayOfWeek].DisplayName + " " + info.startHour.ToString("00") + ":" + info.startMin.ToString("00") +
                //    " ～ " + CommonManager.Instance.DayOfWeekDictionary[info.endDayOfWeek].DisplayName + " " + info.endHour.ToString("00") + ":" + info.endMin.ToString("00");

                DateItem item = new DateItem(info);
                //item.ViewText = viewText;

                _dateItemList.Add(item);
            }
            listBox_Editor_Date.Items.Refresh();
            checkBox_Editor_notDate.IsChecked = (searchKey1.notDateFlag == 1);

            switch (searchKey1.freeCAFlag)
            {
                case 1:
                    radioButton_free_2.IsChecked = true;
                    break;
                case 2:
                    radioButton_free_3.IsChecked = true;
                    break;
                default:
                    radioButton_free_1.IsChecked = true;
                    break;
            }

            textBox_Editor_chkDurationMin.Text = searchKey1.chkDurationMin.ToString();
            textBox_Editor_chkDurationMax.Text = searchKey1.chkDurationMax.ToString();

            _notWordItems.Clear();
            foreach (var item in logItem0.notWordItems_Get())
            {
                _notWordItems.Add(item);
            }

            if (isShowEditor0)
            {
                showEditor(updateButtonText0: "更新");
            }

            _isSearchLogItemEdited = false;
        }

        void showEditor(bool isShow0 = true, string updateButtonText0 = null)
        {
            clear_searchLogResultItems();
            //
            if (isShow0)
            {
                panel_Editor.Visibility = Visibility.Visible;
                panel_SearchLog.Visibility = Visibility.Collapsed;
            }
            else
            {
                listView_SearchLog.SelectedItem = null;
                panel_Editor.Visibility = Visibility.Collapsed;
                panel_SearchLog.Visibility = Visibility.Visible;
            }
            if (!string.IsNullOrEmpty(updateButtonText0))
            {
                button_Editor_UpdateSearchLogItem.Content = updateButtonText0;
            }
        }

        void clearEditor()
        {
            textBox_Editor_SeachLogName.Clear();
            checkBox_Editor_SearchLogName.IsChecked = true;
            textBox_Editor_AndKey.Clear();
            textBox_Editor_NotKey.Clear();
            checkBox_Editor_aimaiFlag.IsChecked = false;
            checkBox_Editor_caseFlag.IsChecked = false;
            checkBox_Editor_regExpFlag.IsChecked = false;
            checkBox_Editor_titleOnlyFlag.IsChecked = false;

            checkBox_Editor_notDate.IsChecked = false;
            toggleButtot_Editor_Mon.IsChecked = false;
            toggleButtot_Editor_Tue.IsChecked = false;
            toggleButtot_Editor_Wed.IsChecked = false;
            toggleButtot_Editor_Thu.IsChecked = false;
            toggleButtot_Editor_Fri.IsChecked = false;
            toggleButtot_Editor_Sat.IsChecked = false;
            toggleButtot_Editor_Sun.IsChecked = false;
            textBox_Edit_Time_Start.Text = "00:00";
            textBox_Edit_Time_End.Text = "23:59";
            _dateItemList.Clear();
            listBox_Editor_Date.Items.Refresh();
            textBox_Editor_chkDurationMax.Text = "0";
            textBox_Editor_chkDurationMin.Text = "0";

            comboBox_Editor_Content.SelectedIndex = 0;
            checkBox_Editor_notContent.IsChecked = false;
            _contentKindInfoList.Clear();

            toggleButton_Editor_Service.IsChecked = true;
            tabControl_Edit_Service.SelectedIndex = 0;
            checkBox_Editor_Tere.IsChecked = true;
            checkBox_Editor_BS.IsChecked = true;
            checkBox_Editor_CS.IsChecked = true;
            foreach (var item in _serviceList_Tere)
            {
                item.IsSelected = true;
            }
            foreach (var item in _serviceList_BS)
            {
                item.IsSelected = true;
            }
            foreach (var item in _serviceList_CS)
            {
                item.IsSelected = true;
            }

            radioButton_free_1.IsChecked = true;

            restoreNotWordEditor();
            _notWordItems.Clear();
            textBox_Edit_NotWord.Text = null;
            comboBox_Editor_Filter_Genre.SelectedItem = null;

            _isSearchLogItemEdited = false;
        }

        void deleteSelectedItem_Editor_Content()
        {
            List<ContentKindInfo> ckiList1 = new List<ContentKindInfo>();
            foreach (ContentKindInfo item1 in listBox_Editor_Content.SelectedItems)
            {
                ckiList1.Add(item1);
            }
            foreach (var item1 in ckiList1)
            {
                _contentKindInfoList.Remove(item1);
            }
        }

        void deleteSelectedItem_Editor_Date()
        {
            foreach (DateItem item1 in listBox_Editor_Date.SelectedItems)
            {
                _dateItemList.Remove(item1);
            }
            listBox_Editor_Date.Items.Refresh();
        }

        void enableButton(Button button0, Border border0, bool isEnabled0)
        {
            button0.IsEnabled = isEnabled0;
            if (isEnabled0)
            {
                border0.BorderBrush = _brush_Yellow;
            }
            else
            {
                border0.BorderBrush = _brush_Transparent;
            }
        }

        void search(SearchLogItem logItem0)
        {
            var epgList1 = new List<EpgEventInfo>();
            ErrCode err = CommonManager.CreateSrvCtrl().SendSearchPg(new List<EpgSearchKeyInfo>() { logItem0.epgSearchKeyInfoS }, ref epgList1);
            if (CommonManager.CmdErrMsgTypical(err, "検索") == false) return;
            //
            List<SearchLogResultItem> resultList1 = new List<SearchLogResultItem>();
            DateTime lastUpdate1 = DateTime.Now;
            logItem0.lastUpdate = lastUpdate1;
            foreach (EpgEventInfo epg1 in epgList1)
            {
                if (epg1.isBroadcasted()) { continue; }
                if (logItem0.matchFilteringCondition(epg1)) { continue; }
                //
                SearchLogResultItem result1 = null;
                foreach (var item in logItem0.resultItems.Where(x1 => x1.epgEventInfoR.Equals(epg1)))
                {
                    result1 = item;
                    break;
                }
                if (result1 == null)
                {
                    result1 = new SearchLogResultItem(logItem0, epg1, lastUpdate1);
                }
                else
                {
                    logItem0.resultItems.Remove(result1);
                    result1.epgSearchKeyInfo = logItem0.epgSearchKeyInfoS;
                    result1.epgEventInfoR = new EpgEventInfoR(epg1, lastUpdate1);
                }
                result1.updateReserveInfo();
                resultList1.Add(result1);
            }
            resultList1.Sort((x1, x2) =>
            {
                return x1.epgEventInfoR.start_time.CompareTo(x2.epgEventInfoR.start_time);
            });
            logItem0.resultItems.Clear();
            foreach (var item in resultList1)
            {
                logItem0.resultItems.Add(item);
            }
        }

        void openReserveDialog()
        {
            SearchLogResultItem resultItem1 = listView_Result.SelectedItem as SearchLogResultItem;
            if (resultItem1 == null) { return; }
            //
            resultItem1.epgEventInfoR.openReserveDialog(this);
        }

        void deleteSearchLogItem(ICollection<SearchLogItem> logItemList0, bool isConfirm0 = true)
        {
            if (!isConfirm0
                || MessageBox.Show("削除しますか？", "確認", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                foreach (SearchLogItem item1 in logItemList0.ToArray())
                {
                    db_SearchLog.delete(item1);
                    _searchLogItems.Remove(item1);
                }
            }
        }

        void changeVisibility_Resutltems()
        {
            foreach (var item1 in _searchLogResultItems)
            {
                if (isShowConfirmedResult)
                {
                    item1.isVisible = true;
                }
                else
                {
                    item1.isVisible = !item1.confirmed;
                    if (item1.confirmed && item1 == listView_Result.SelectedItem)
                    {
                        panel_Epg.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        void showSearchResults()
        {
            checkBox_Result_Confirmed.IsChecked = false;
            clear_searchLogResultItems();
            foreach (SearchLogItem logItem1 in listView_SearchLog.SelectedItems)
            {
                foreach (var resultItem1 in logItem1.resultItems)
                {
                    _searchLogResultItems.Add(resultItem1);
                }
            }
            changeVisibility_Resutltems();
        }

        void changeRecordStatus(RecLogItem.RecodeStatuses status0)
        {
            List<SearchLogResultItem> list1 = new List<SearchLogResultItem>();
            foreach (SearchLogResultItem item in listView_Result.SelectedItems)
            {
                item.recodeStatus = status0;
                list1.Add(item);
            }
            db_SearchLog.update_RecodeStatus(list1);
        }

        void search_AddNotWord(List<SearchLogItem> searchLogItems0)
        {
            ObservableCollection<SearchLogResultItem> searchLogResultItems1 = new ObservableCollection<SearchLogResultItem>();
            foreach (var logItem1 in searchLogItems0)
            {
                search(logItem1);
                foreach (var resultItem1 in logItem1.resultItems)
                {
                    searchLogResultItems1.Add(resultItem1);
                }
            }
            db_SearchLog.updateOrInsert(searchLogItems0);
            Dispatcher.BeginInvoke(new Action(() => {
                listView_Result.ItemsSource = _searchLogResultItems = searchLogResultItems1;
                changeVisibility_Resutltems();
            }));
        }

        void addNotWord(ref List<SearchLogItem> searchLogItems0, SearchLogResultItem resultItem0, string notWord0, bool isTitleOnly0)
        {
            SearchLogNotWordItem notWordItem1;
            if (logItem_Edit != null)
            {
                if (logItem_Edit.addNotWord(out notWordItem1, notWord0, isTitleOnly0))
                {
                    addLog("編集中の「" + logItem_Edit.name + "」にNotWord「" + notWordItem1.word + "」を追加");
                    add2NotWordItems(notWordItem1);
                    if (searchLogItems0.Count == 0)
                    {
                        searchLogItems0.Add(logItem_Edit);
                    }
                }
            }
            else
            {
                SearchLogItem logItem1 = _searchLogItems.Where(x1 => x1.ID == resultItem0.searchLogItemID).First();
                if (logItem1.addNotWord(out notWordItem1, notWord0, true))
                {
                    addLog("「" + logItem1.name + "」にNotWord「" + notWordItem1.word + "」を追加");
                    if (!searchLogItems0.Contains(logItem1))
                    {
                        searchLogItems0.Add(logItem1);
                    }
                }
            }
            if (notWordItem1 != null && 0 < notWordItem1.searchLogID)
            {
                db_SearchLog.insertNotWordItem(notWordItem1);
            }
        }

        /// <summary>
        /// 追加してNotWorrdでSort
        /// </summary>
        /// <param name="notWordItem0"></param>
        void add2NotWordItems(SearchLogNotWordItem notWordItem0)
        {
            List<SearchLogNotWordItem> notWordItemList1 = _notWordItems.ToList();
            notWordItemList1.Add(notWordItem0);
            _notWordItems.Clear();
            foreach (var item in notWordItemList1.OrderBy(x1 => x1.word))
            {
                _notWordItems.Add(item);
            }
            _isSearchLogItemEdited = true;
        }

        void deleteNotWordItem()
        {
            List<SearchLogNotWordItem> notWordItemList1 = new List<SearchLogNotWordItem>();
            foreach (SearchLogNotWordItem notWordItem1 in listView_Edit_NotWord.SelectedItems)
            {
                notWordItemList1.Add(notWordItem1);
            }
            foreach (var notWordItem1 in notWordItemList1)
            {
                _notWordItems.Remove(notWordItem1);
            }
            _isSearchLogItemEdited = true;
        }

        void editNotWordItem(SearchLogNotWordItem notWordItem0)
        {
            if (notWordItem0 == null) { return; }
            //
            _notWordItem = notWordItem0;
            if (_notWordItem.isFilteringByGanre)
            {
                comboBox_Editor_Filter_Genre.SelectedItem = _notWordItem.contentKindInfo;
            }
            else
            {
                textBox_Edit_NotWord.Text = _notWordItem.word;
            }
            _notWordItems.Remove(notWordItem0);
            _isSearchLogItemEdited = true;
        }

        EpgSearchKeyInfoS getEpgSearchKeyInfoFromEditor(long epgSearchKeyInfoID0)
        {
            EpgSearchKeyInfoS searchKey1 = new EpgSearchKeyInfoS()
            {
                lastUpdate = DateTime.Now,
                ID = epgSearchKeyInfoID0,
                andKey = textBox_Editor_AndKey.Text,
                notKey = textBox_Editor_NotKey.Text,
                regExpFlag = (byte)(checkBox_Editor_regExpFlag.IsChecked == true ? 1 : 0),
                aimaiFlag = (byte)(checkBox_Editor_aimaiFlag.IsChecked == true ? 1 : 0),
                titleOnlyFlag = (byte)(checkBox_Editor_titleOnlyFlag.IsChecked == true ? 1 : 0),
                caseFlag = (byte)(checkBox_Editor_caseFlag.IsChecked == true ? 1 : 0)
            };
            //
            foreach (ContentKindInfo info1 in _contentKindInfoList)
            {
                EpgContentData ecd1 = new EpgContentData();
                ecd1.content_nibble_level_1 = info1.Data.Nibble1;
                ecd1.content_nibble_level_2 = info1.Data.Nibble2;
                searchKey1.contentList.Add(ecd1);
            }
            searchKey1.notContetFlag = (byte)(checkBox_Editor_notContent.IsChecked == true ? 1 : 0);
            // サービス
            foreach (ServiceViewItem item1 in listView_service_Tera.Items)
            {
                if (item1.IsSelected == true)
                {
                    searchKey1.serviceList.Add((Int64)item1.Key);
                }
            }
            foreach (ServiceViewItem item1 in listView_service_BS.Items)
            {
                if (item1.IsSelected == true)
                {
                    searchKey1.serviceList.Add((Int64)item1.Key);
                }
            }
            foreach (ServiceViewItem item1 in listView_service_CS.Items)
            {
                if (item1.IsSelected == true)
                {
                    searchKey1.serviceList.Add((Int64)item1.Key);
                }
            }
            // Date
            searchKey1.dateList.Clear();
            foreach (DateItem item1 in listBox_Editor_Date.Items)
            {
                searchKey1.dateList.Add(item1.DateInfo);
            }
            searchKey1.notDateFlag = (byte)(checkBox_Editor_notDate.IsChecked == true ? 1 : 0);
            //
            if (radioButton_free_2.IsChecked == true)
            {
                //無料
                searchKey1.freeCAFlag = 1;
            }
            else if (radioButton_free_3.IsChecked == true)
            {
                //有料
                searchKey1.freeCAFlag = 2;
            }
            else
            {
                searchKey1.freeCAFlag = 0;
            }
            //
            searchKey1.chkDurationMin = MenuUtil.MyToNumerical(textBox_Editor_chkDurationMin, Convert.ToUInt16, ushort.MinValue);
            searchKey1.chkDurationMax = MenuUtil.MyToNumerical(textBox_Editor_chkDurationMax, Convert.ToUInt16, ushort.MinValue);

            return searchKey1;
        }

        void hideSelectedService()
        {
            ListView listView1 = (ListView)tabControl_Edit_Service.SelectedContent;
            List<ServiceViewItem> hiddenList1 = new List<ServiceViewItem>();
            foreach (ServiceViewItem item1 in listView1.SelectedItems)
            {
                item1.IsSelected = false;
                hiddenList1.Add(item1);
            }
            if (0 < hiddenList1.Count)
            {
                foreach (ServiceViewItem item1 in hiddenList1)
                {
                    ObservableCollection<ServiceViewItem> ServiceViewItems1 = listView1.ItemsSource as ObservableCollection<ServiceViewItem>;
                    ServiceViewItems1.Remove(item1);
                }
            }
        }

        void restoreNotWordEditor()
        {
            border_Edit_SearchWord.Visibility = Visibility.Visible;
            panel_Content_Service.Visibility = Visibility.Visible;
            border_Edit_Date.Visibility = Visibility.Visible;
            border_Edit_Scramble.Visibility = Visibility.Visible;
            //
            var scrollViewer_ListView1 = GetScrollViewer(listView_Edit_NotWord);
            if (scrollViewer_ListView1 != null)
            {
                scrollViewer_ListView1.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
            gridViewColumn_Edit_NotWord_Word.Width = 100;
        }

        void clear_searchLogResultItems()
        {
            _searchLogResultItems.Clear();
            listView_Result.clearSortDescriptions();
        }

        void addGenre2NotWord(ContentKindInfo info0)
        {
            if (info0 == null) { return; }
            //
            if (_notWordItem == null)
            {
                _notWordItem = new SearchLogNotWordItem();
            }
            _notWordItem.contentKindInfo = info0;
            if (_notWordItems.Where(x1 => x1.contentKindInfo == _notWordItem.contentKindInfo).Count() == 0)
            {
                add2NotWordItems(_notWordItem);
                listView_Edit_NotWord.SelectedItem = _notWordItem;
                listView_Edit_NotWord.ScrollIntoView(_notWordItem);
            }
            //
            comboBox_Editor_Filter_Genre.SelectedItem = null;
            _notWordItem = null;
            _isSearchLogItemEdited = true;
        }

        void reset_ServiceEditor()
        {
            toggleButton_serviceEditor_All.IsChecked = false;
            foreach (var item in new CheckBox[] { checkBox_serviceEditor_Tere, checkBox_serviceEditor_BS, checkBox_serviceEditor_CS })
            {
                item.IsChecked = false;
            }
        }

        void update_SearchLogItem_Service()
        {
            if (logItem_Edit == null) { return; }
            //
            logItem_Edit.epgSearchKeyInfoS = getEpgSearchKeyInfoFromEditor(logItem_Edit.epgSearchKeyInfoS.ID);
            db_SearchLog.updateOrInsert(logItem_Edit);
            logItem_Edit = null;
        }

        void addLog(string log0)
        {
            return;
            //
            string log1 = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + log0;
            listBox_DebugLog.Dispatcher.BeginInvoke(new Action(() =>
            {
                listBox_DebugLog.Items.Insert(0, log1);
                while (2000 < listBox_DebugLog.Items.Count)
                {
                    listBox_DebugLog.Items.RemoveAt(listBox_DebugLog.Items.Count - 1);
                }
            }));
        }

        #region - Property -
        #endregion

        public DB_SearchLog db_SearchLog { get; set; }

        public SearchLogView searchLogView { get; set; }

        public SearchLogTabInfo tabInfo
        {
            get { return _tabInfo; }
            set
            {
                _tabInfo = value;
                if (value != null)
                {
                    textBox_TabHeader.Text = value.header;
                }
            }
        }
        SearchLogTabInfo _tabInfo = null;

        public List<TabItem> tabItems { get; set; }

        public DB_RecLog db_RecLog { get; set; }

        RecLogWindow recLogWindow
        {
            get
            {
                if (_recLogWindow == null)
                {
                    _recLogWindow = new RecLogWindow(Window.GetWindow(this));
                }
                return _recLogWindow;
            }
        }
        RecLogWindow _recLogWindow = null;

        bool isShowConfirmedResult
        {
            get { return (toggleButton_Result_ShowConfirmed.IsChecked == true); }
            set { toggleButton_Result_ShowConfirmed.IsChecked = value; }
        }

        bool isUpdateView_Doing
        {
            get { return _isUpdateView_Doing; }
            set
            {
                _isUpdateView_Doing = value;
                if (value)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        messagePanel.Visibility = Visibility.Visible;
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        messagePanel.Visibility = Visibility.Collapsed;
                    }));
                }
            }
        }
        bool _isUpdateView_Doing = false;

        SearchLogItem logItem_Edit
        {
            get { return this._logItem_Edit; }
            set
            {
                this._logItem_Edit = value;
                string val1 = "null";
                if (value != null)
                {
                    val1 = value.name;
                }
                addLog("logItem_Editプロパティに「" + val1 + "」をセット");
            }
        }
        SearchLogItem _logItem_Edit = null;

        #region - Event Handler -
        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            toggleButton_Editor_Service.IsChecked = true;
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                toggleButton_ServieEditMode.IsChecked = false;
            }
            else
            {
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    if (isUpdateView_Doing) { return; }
                    isUpdateView_Doing = true;
                    //
                    bool isUpdated1 = false;
                    List<SearchLogItem> searchLogItems1 = db_SearchLog.selectByTab(tabInfo.ID);
                    if (searchLogItems1.Count != _searchLogItems.Count)
                    {
                        isUpdated1 = true;
                    }
                    else
                    {
                        foreach (var logItem1 in _searchLogItems)
                        {
                            if (!searchLogItems1.Exists(x1 => x1.ID == logItem1.ID && x1.lastUpdate == logItem1.lastUpdate))
                            {
                                isUpdated1 = true;
                                break;
                            }
                        }
                    }
                    //
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (isUpdated1)
                        {
                            _searchLogItems.Clear();
                            foreach (var logItem1 in searchLogItems1)
                            {
                                _searchLogItems.Add(logItem1);
                            }
                        }
                        //
                        if (_isEpgUpdated)
                        {
                            searchAll();
                        }
                        else if (0 < _counter_ReserveInfoUpdated)
                        {
                            update_ReserveInfo();
                        }
                        isUpdateView_Doing = false;
                    }));
                });
            }
        }

        private void menu_SearchLog_Del_Click(object sender, RoutedEventArgs e)
        {
            SearchLogItem logItem1 = listView_SearchLog.SelectedItem as SearchLogItem;
            deleteSearchLogItem(
                new List<SearchLogItem>() { logItem1 });
        }

        private void menu_SearchLog_Edit_Click(object sender, RoutedEventArgs e)
        {
            SearchLogItem logItem_Edit1 = listView_SearchLog.SelectedItem as SearchLogItem;
            setLogItem2Editor(logItem_Edit1);
        }

        private void listBox_Editor_Content_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    deleteSelectedItem_Editor_Content();
                    break;
                case Key.Escape:
                    listBox_Editor_Content.SelectedItem = null;
                    break;
            }
        }

        private void checkBox_Editor_Tere_Checked(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem item1 in _serviceList_Tere)
            {
                item1.IsSelected = true;
            }
        }

        private void checkBox_Editor_Tere_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem item1 in _serviceList_Tere)
            {
                item1.IsSelected = false;
            }
        }

        private void checkBox_BS_Checked(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem item1 in _serviceList_BS)
            {
                item1.IsSelected = true;
            }
        }

        private void checkBox_BS_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem item1 in _serviceList_BS)
            {
                item1.IsSelected = false;
            }
        }

        private void checkBox_CS_Checked(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem item1 in _serviceList_CS)
            {
                item1.IsSelected = true;
            }
        }

        private void checkBox_CS_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem item1 in _serviceList_CS)
            {
                item1.IsSelected = false;
            }
        }

        private void menu_listBox_content_Click(object sender, RoutedEventArgs e)
        {
            deleteSelectedItem_Editor_Content();
        }

        private void button_Editor_UpdateSearchLogItem_Click(object sender, RoutedEventArgs e)
        {
            SearchLogItem logItem_Edit1 = logItem_Edit;
            logItem_Edit = null;
            if (logItem_Edit1 == null)
            {
                throw new InvalidOperationException("button_Editor_UpdateSearchLogItem_Click(): logItem_Edit1 == null");
                logItem_Edit1 = new SearchLogItem(tabInfo.ID);
            }
            logItem_Edit1.name = textBox_Editor_SeachLogName.Text;
            EpgSearchKeyInfoS epgSearchKeyInfoS1 = getEpgSearchKeyInfoFromEditor(logItem_Edit1.epgSearchKeyInfoS.ID);
            logItem_Edit1.epgSearchKeyInfoS = epgSearchKeyInfoS1;
            logItem_Edit1.notWordItem_Replace(_notWordItems);
            //
            if (!_searchLogItems.Contains(logItem_Edit1))
            {
                _searchLogItems.Add(logItem_Edit1);
                logItem_Edit1.listOrder = _searchLogItems.Count;
            }

            if (_isSearchLogItemEdited)
            {
                search(logItem_Edit1);
            }

            clearEditor();

            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                int db_Count_NewItem1 = db_Count_NewItem1 = db_SearchLog.updateOrInsert(logItem_Edit1);
                if (0 < db_Count_NewItem1)
                {
                    addLog("SearchLogItem「" + logItem_Edit1.name + "」を新規作成");
                }
                else
                {
                    addLog("SearchLogItem「" + logItem_Edit1.name + "」を更新");
                }
            });

            showEditor(false);
        }

        private void textBox_Edit_Time_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox1 = sender as TextBox;
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Home:
                case Key.End:
                case Key.Left:
                case Key.Right:
                    return;
            }
            //
            e.Handled = true;
            int caretIndex1 = textBox1.CaretIndex;
            // 23:59
            // 01-2-34
            char[] chars1 = textBox1.Text.ToCharArray();
            bool isChangeValue1 = false;
            int i1 = -1;
            if ((int)Key.D0 <= (int)e.Key && (int)e.Key <= (int)Key.D9)
            {
                i1 = (int)e.Key - (int)Key.D0;
            }
            else if ((int)Key.NumPad0 <= (int)e.Key && (int)e.Key <= (int)Key.NumPad9)
            {
                i1 = (int)e.Key - (int)Key.NumPad0;
            }
            if (0 <= i1)
            {
                switch (caretIndex1)
                {
                    case 0:
                        if (0 <= i1 && i1 <= 2) { isChangeValue1 = true; }
                        break;
                    case 1:
                        if (chars1[0] == '2')
                        {
                            if (0 <= i1 && i1 <= 3) { isChangeValue1 = true; }
                        }
                        else
                        {
                            isChangeValue1 = true;
                        }
                        break;
                    case 3:
                        if (0 <= i1 && i1 <= 5) { isChangeValue1 = true; }
                        break;
                    case 4:
                        isChangeValue1 = true;
                        break;
                }
                if (isChangeValue1)
                {
                    chars1[caretIndex1] = i1.ToString()[0];
                    textBox1.Text = new String(chars1);
                    do
                    {
                        caretIndex1++;
                    } while (caretIndex1 == 2);
                }
            }
            textBox1.CaretIndex = caretIndex1;
            if (textBox1.Equals(textBox_Edit_Time_Start) && caretIndex1 == 5)
            {
                textBox_Edit_Time_End.Focus();
            }
        }

        private void button_SearchLog_AddNew_Click(object sender, RoutedEventArgs e)
        {
            clearEditor();
            showEditor(updateButtonText0: "追加");
        }

        private void menu_Service_Hide_Click(object sender, RoutedEventArgs e)
        {
            hideSelectedService();
        }

        private void button_Editor_AddDate_Click(object sender, RoutedEventArgs e)
        {
            string[] time_Start1 = textBox_Edit_Time_Start.Text.Split(':');
            string[] time_End1 = textBox_Edit_Time_End.Text.Split(':');
            ushort hour_Start1 = ushort.Parse(time_Start1[0]);
            ushort min_Start1 = ushort.Parse(time_Start1[1]);
            ushort hour_End1 = ushort.Parse(time_End1[0]);
            ushort min_End1 = ushort.Parse(time_End1[1]);
            Int32 start = hour_Start1 * 60 + min_Start1;
            Int32 end = hour_End1 * 60 + min_End1;

            var Add_week = new Action<ToggleButton, byte>((chbox, day) =>
            {
                if (chbox.IsChecked != true) return;
                //
                var info = new EpgSearchDateInfo();
                info.startDayOfWeek = day;
                info.startHour = hour_Start1;
                info.startMin = min_Start1;
                info.endDayOfWeek = info.startDayOfWeek;
                info.endHour = hour_End1;
                info.endMin = min_End1;
                if (end < start)
                {
                    //終了時間は翌日のものとみなす
                    info.endDayOfWeek = (byte)((info.endDayOfWeek + 1) % 7);
                }

                string viewText = "日月火水木金土"[info.startDayOfWeek] + " " + info.startHour.ToString("00") + ":" + info.startMin.ToString("00") +
                    " ～ " + "日月火水木金土"[info.endDayOfWeek] + " " + info.endHour.ToString("00") + ":" + info.endMin.ToString("00");

                var item = new DateItem(info);
                if (!_dateItemList.Contains(item))
                {
                    _dateItemList.Add(item);
                }
                _dateItemList.Sort(
                    (x1, x2) =>
                    {
                        EpgSearchDateInfo d1 = x1.DateInfo;
                        EpgSearchDateInfo d2 = x2.DateInfo;
                        int ret1 = d1.startDayOfWeek.CompareTo(d2.startDayOfWeek);
                        if (ret1 == 0)
                        {
                            ret1 = d1.startHour.CompareTo(d2.startHour);
                            if (ret1 == 0)
                            {
                                ret1 = d1.startMin.CompareTo(d2.startMin);
                            }
                        }
                        return ret1;
                    });
            });

            Add_week(toggleButtot_Editor_Mon, 1);
            Add_week(toggleButtot_Editor_Tue, 2);
            Add_week(toggleButtot_Editor_Wed, 3);
            Add_week(toggleButtot_Editor_Thu, 4);
            Add_week(toggleButtot_Editor_Fri, 5);
            Add_week(toggleButtot_Editor_Sat, 6);
            Add_week(toggleButtot_Editor_Sun, 0);

            listBox_Editor_Date.Items.Refresh();

            _isSearchLogItemEdited = true;
        }

        private void listBox_Editor_Date_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    deleteSelectedItem_Editor_Date();
                    break;
                case Key.Escape:
                    listBox_Editor_Date.SelectedItem = null;
                    break;
            }
        }

        private void menu_Editor_Date_Delete_Click(object sender, RoutedEventArgs e)
        {
            deleteSelectedItem_Editor_Date();
        }

        private void button_Editor_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSearchLogItemEdited
                || MessageBox.Show("変更は保存されません。よろしいですか？", "確認", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK) == MessageBoxResult.OK)
            {
                logItem_Edit = null;
                showEditor(false);
                clearEditor();
            }
        }

        private void button_Editor_Search_Click(object sender, RoutedEventArgs e)
        {
            if (logItem_Edit == null)
            {
                logItem_Edit = new SearchLogItem(tabInfo.ID);
            }
            logItem_Edit.epgSearchKeyInfoS = getEpgSearchKeyInfoFromEditor(logItem_Edit.epgSearchKeyInfoS.ID);
            logItem_Edit.notWordItem_Replace(_notWordItems);
            search(logItem_Edit);
            db_SearchLog.update_RecodeStatus(logItem_Edit, false);
            clear_searchLogResultItems();
            foreach (var resultItem1 in logItem_Edit.resultItems)
            {
                _searchLogResultItems.Add(resultItem1);
            }
            changeVisibility_Resutltems();
        }

        private void tabControl_Edit_Service_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    hideSelectedService();
                    break;
            }
        }

        private void toggleButton_Editor_Service_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Editor_Tere.IsChecked = true;
            checkBox_Editor_BS.IsChecked = true;
            checkBox_Editor_CS.IsChecked = true;
            toggleButton_Editor_Service.Content = "全て解除";
        }

        private void toggleButton_Editor_Service_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_Editor_Tere.IsChecked = false;
            checkBox_Editor_BS.IsChecked = false;
            checkBox_Editor_CS.IsChecked = false;
            toggleButton_Editor_Service.Content = "全て選択";
        }

        private void comboBox_Editor_Content_DropDownClosed(object sender, EventArgs e)
        {
            ContentKindInfo info1 = comboBox_Editor_Content.SelectedItem as ContentKindInfo;
            if (info1 == null) { return; }

            if (!_contentKindInfoList.Contains(info1))
            {
                add2ContentKindInfoList(info1);
            }

            _isSearchLogItemEdited = true;
        }

        void add2ContentKindInfoList(ContentKindInfo info0)
        {
            List<ContentKindInfo> infoList1 = _contentKindInfoList.ToList();
            infoList1.Add(info0);
            infoList1.Sort((x1, x2) => { return x1.Data.Key.CompareTo(x2.Data.Key); });
            _contentKindInfoList.Clear();
            foreach (var item in infoList1)
            {
                _contentKindInfoList.Add(item);
            }
        }

        private void toggleButton_Result_ShowConfirmed_Checked(object sender, RoutedEventArgs e)
        {
            menu_Result_ShowConfirmed.Header = "チェック済みアイテムを表示(_V)";
            changeVisibility_Resutltems();
        }

        private void toggleButton_Result_ShowConfirmed_Unchecked(object sender, RoutedEventArgs e)
        {
            menu_Result_ShowConfirmed.Header = "チェック済みアイテムを非表示(_V)";
            changeVisibility_Resutltems();
        }

        private void button_Result_Clear_Click(object sender, RoutedEventArgs e)
        {
            clear_searchLogResultItems();
        }

        private void listView_SearchLog_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    {
                        SearchLogItem logItem_Edit1 = listView_SearchLog.SelectedItem as SearchLogItem;
                        setLogItem2Editor(logItem_Edit1);
                    }
                    break;
                case Key.Delete:
                    {
                        List<SearchLogItem> logItemList1 = new List<SearchLogItem>();
                        foreach (SearchLogItem item1 in listView_SearchLog.SelectedItems)
                        {
                            logItemList1.Add(item1);
                        }
                        deleteSearchLogItem(logItemList1);
                    }
                    break;
                case Key.Escape:
                    listView_SearchLog.SelectedItem = null;
                    break;
            }
        }

        private void listView_Result_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        foreach (SearchLogResultItem item1 in listView_Result.SelectedItems)
                        {
                            item1.epgEventInfoR.reserveAdd_ChangeOnOff();
                        }
                    }
                    break;
                case Key.D:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        foreach (SearchLogResultItem item1 in listView_Result.SelectedItems)
                        {
                            item1.epgEventInfoR.reserveDelete();
                        }
                    }
                    break;
                case Key.Enter:
                    openReserveDialog();
                    break;
                case Key.Escape:
                    listView_Result.SelectedItem = null;
                    break;
            }
        }

        private void menu_Result_OpenReserveDialog_Click(object sender, RoutedEventArgs e)
        {
            openReserveDialog();
        }

        private void menu_Result_Reserve_Click(object sender, RoutedEventArgs e)
        {
            foreach (SearchLogResultItem item1 in listView_Result.SelectedItems)
            {
                item1.epgEventInfoR.reserveAdd_ChangeOnOff();
            }
        }

        private void menu_Resutl_Delete_Click(object sender, RoutedEventArgs e)
        {
            foreach (SearchLogResultItem item1 in listView_Result.SelectedItems)
            {
                item1.epgEventInfoR.reserveDelete();
            }
        }

        private void menu_Result_RecLog_Click(object sender, RoutedEventArgs e)
        {
            SearchLogResultItem resultItem1 = (SearchLogResultItem)listView_Result.SelectedItem;
            recLogWindow.showResult(resultItem1.epgEventInfoR);
        }

        private void menu_Result_SearchByWeb_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menu1 = (MenuItem)sender;
            SearchLogResultItem resultItem1 = menu1.DataContext as SearchLogResultItem;
            if (resultItem1 != null)
            {
                RecLogWindow.searchByWeb(resultItem1.tvProgramTitle);
            }
        }

        private void menu_Result_Jump2EpgView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menu1 = (MenuItem)sender;
            SearchLogResultItem resultItem1 = menu1.DataContext as SearchLogResultItem;
            if (resultItem1 != null)
            {
                SearchItem searchItem1 = new SearchItem(resultItem1.epgEventInfoR);
                BlackoutWindow.SelectedData = searchItem1;
                _mainWindow.moveTo_tabItem(CtxmCode.EpgView);
            }
        }

        private void menu_Result_HideConfirmed_Click(object sender, RoutedEventArgs e)
        {
            isShowConfirmedResult = !isShowConfirmedResult;
            changeVisibility_Resutltems();
        }

        private void menu_Result_Check_Click(object sender, RoutedEventArgs e)
        {
            List<SearchLogResultItem> resultItems1 = new List<SearchLogResultItem>();
            foreach (SearchLogResultItem item1 in listView_Result.SelectedItems)
            {
                resultItems1.Add(item1);
            }
            foreach (var item1 in resultItems1)
            {
                item1.confirmed = !item1.confirmed;
            }
            updateResultItem_ConfirmedProperty(resultItems1);
        }

        private void button_SearchLog_Update_Click(object sender, RoutedEventArgs e)
        {
            searchAll();
        }

        private void checkBox_Result_Confirmed_Click(object sender, RoutedEventArgs e)
        {
            foreach (var resultItem1 in _searchLogResultItems)
            {
                resultItem1.confirmed = (checkBox_Result_Confirmed.IsChecked == true);
            }
            updateResultItem_ConfirmedProperty(_searchLogResultItems);
        }

        void resultItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem1 = (ListViewItem)sender;
            if (listViewItem1 == null) { return; }
            SearchLogResultItem resultItem1 = listViewItem1.DataContext as SearchLogResultItem;
            // 
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    if (Keyboard.Modifiers == ModifierKeys.Alt)
                    {
                        // チェック済みフラグの操作
                        resultItem1.confirmed = !resultItem1.confirmed;
                        updateResultItem_ConfirmedProperty(resultItem1);
                    }
                    break;
                case MouseButton.Middle:
                    recLogWindow.showResult(resultItem1.epgEventInfoR);
                    break;
            }
        }

        void updateResultItem_ConfirmedProperty(SearchLogResultItem resultItem0)
        {
            updateResultItem_ConfirmedProperty(new List<SearchLogResultItem>() { resultItem0 });
        }

        void updateResultItem_ConfirmedProperty(ICollection<SearchLogResultItem> resultItems0)
        {
            List<SearchLogItem> searchLogItems1 = new List<SearchLogItem>();
            foreach (var resultItem1 in resultItems0)
            {
                foreach (SearchLogItem logItem1 in listView_SearchLog.SelectedItems)
                {
                    if (logItem1.ID == resultItem1.searchLogItemID)
                    {
                        if (!searchLogItems1.Contains(logItem1))
                        {
                            logItem1.lastUpdate = DateTime.Parse(DateTime.Now.ToString(DB.timeStampStrFormat));     // 誤差切り捨て
                            searchLogItems1.Add(logItem1);
                        }
                        break;
                    }
                }
            }
            db_SearchLog.update(searchLogItems1);
            db_SearchLog.updateSearchResult(resultItems0);
            changeVisibility_Resutltems();
        }

        void resultItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                openReserveDialog();
            }
        }

        private void listViewItem_SearchLog_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _logItem_Drag = ((ListViewItem)sender).Content as SearchLogItem;
            }
        }

        private void listViewItem_SearchLog_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_logItem_Drag == null) { return; }

            SearchLogItem logItem1 = ((ListViewItem)sender).Content as SearchLogItem;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (logItem1 != null && logItem1 != _logItem_Drag)
                {
                    int index1 = _searchLogItems.IndexOf(logItem1);
                    _searchLogItems.Remove(_logItem_Drag);
                    _searchLogItems.Insert(index1, _logItem_Drag);
                    //
                    _logItem_Drag.listOrder = index1 + 1;
                    logItem1.listOrder = _searchLogItems.IndexOf(logItem1) + 1;
                    db_SearchLog.update(
                        new List<SearchLogItem>() { _logItem_Drag, logItem1 });
                    listView_SearchLog.SelectedIndex = index1;
                }
            }
            else
            {
                _logItem_Drag = null;
            }
        }

        void listViewItem_SearchLog_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                SearchLogItem logItem_Edit1 = listView_SearchLog.SelectedItem as SearchLogItem;
                setLogItem2Editor(logItem_Edit1);
            }
        }

        /// <summary>
        /// キーボードによる選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_SearchLog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (SearchLogItem item1 in listView_SearchLog.SelectedItems)
            {
                addLog("SearchLogItem「" + item1.name + "」を選択");
            }
            if (toggleButton_ServieEditMode.IsChecked == true)
            {
                if (_isSearchLogItemEdited)
                {
                    update_SearchLogItem_Service();
                }
                reset_ServiceEditor();
            }
            //
            if (listView_SearchLog.SelectedItem == null)
            {
                clear_searchLogResultItems();
            }
            else if (toggleButton_ServieEditMode.IsChecked == true)
            {
                SearchLogItem logItem_Edit1 = listView_SearchLog.SelectedItem as SearchLogItem;
                setLogItem2Editor(logItem_Edit1, false);
            }
            else
            {
                showSearchResults();
            }
        }

        private void button_ChangeTabHeader_Click(object sender, RoutedEventArgs e)
        {
            tabInfo.header = textBox_TabHeader.Text;
            tabHeaderChanged?.Invoke(this, EventArgs.Empty);
            border_ChangeHeader.Visibility = Visibility.Collapsed;
        }

        private void button_ChangeTabHeader_Close_Click(object sender, RoutedEventArgs e)
        {
            border_ChangeHeader.Visibility = Visibility.Collapsed;
        }

        private void listViewItem_SearchLog_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            ListViewItem listViewItem1 = (ListViewItem)sender;
            ContextMenu cm1 = listViewItem1.ContextMenu;
            if (!cm1.Items.Contains(_menuItem_Move2Tab))
            {
                cm1.Items.Add(_menuItem_Move2Tab);
            }
            _menuItem_Move2Tab.Items.Clear();
            if (tabItems.Count == 1)
            {
                _menuItem_Move2Tab.Items.Add(_menuItem_Move2Tab_None);
            }
            else
            {
                foreach (TabItem tab1 in tabItems)
                {
                    SearchLogTabItem searchLogTab1 = (SearchLogTabItem)tab1.Content;
                    if (this == searchLogTab1) { continue; }

                    MenuItem mi1 = new MenuItem();
                    mi1.Header = tab1.Header;
                    mi1.Click += delegate
                    {
                        List<SearchLogItem> selectedItems1 = new List<SearchLogItem>();
                        foreach (SearchLogItem searchLogItem1 in listView_SearchLog.SelectedItems)
                        {
                            selectedItems1.Add(searchLogItem1);
                            searchLogItem1.tabID = searchLogTab1.tabInfo.ID;
                        }
                        foreach (SearchLogItem searchLogItem1 in selectedItems1)
                        {
                            _searchLogItems.Remove(searchLogItem1);
                        }
                        searchLogTab1.addSearchLogItem(selectedItems1);
                        db_SearchLog.update(selectedItems1);
                    };
                    _menuItem_Move2Tab.Items.Add(mi1);
                }
            }
        }

        private void menu_SearchLog_AddNew_Click(object sender, RoutedEventArgs e)
        {
            showEditor(updateButtonText0: "追加");
        }

        private void menu_SearchLog_SearchAll_Click(object sender, RoutedEventArgs e)
        {
            searchAll();
        }

        void resultItem_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            ListViewItem lvItem1 = sender as ListViewItem;
            foreach (MenuItem menuItem1 in lvItem1.ContextMenu.Items)
            {
                if ((string)menuItem1.Tag == "single")
                {
                    menuItem1.IsEnabled = (listView_Result.SelectedItems.Count == 1);
                }
            }
        }

        private void listView_Result_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            bool isExistResult1 = (0 < _searchLogResultItems.Count);
            foreach (MenuItem item1 in ctxMenu_Result.Items)
            {
                item1.IsEnabled = isExistResult1;
            }
        }

        private void checkBox_Editor_SearchLogName_Checked(object sender, RoutedEventArgs e)
        {
            textBox_Editor_SeachLogName.IsEnabled = false;
            if (textBox_Editor_AndKey != null)
            {
                textBox_Editor_SeachLogName.Text = textBox_Editor_AndKey.Text;
            }
        }

        private void checkBox_Editor_SearchLogName_Unchecked(object sender, RoutedEventArgs e)
        {
            textBox_Editor_SeachLogName.IsEnabled = true;
        }

        private void textBox_Editor_AndKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (checkBox_Editor_SearchLogName.IsChecked == true)
            {
                textBox_Editor_SeachLogName.Text = textBox_Editor_AndKey.Text;
            }
            _isSearchLogItemEdited = true;
        }

        private void menu_RecLog_ChangeStatus_NONE_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.NONE);
        }

        private void menu_RecLog_ChangeStatus_Reserve_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.予約済み);
        }

        private void menu_RecLog_ChangeStatus_Recorded_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.録画完了);
        }

        private void menu_RecLog_ChangeStatus_Error_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.録画異常);
        }

        private void menu_RecLog_ChangeStatus_Viewed_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.視聴済み);
        }

        private void menu_RecLog_ChangeStatus_Disabled_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.無効登録);
        }

        private void menu_RecLog_ChangeStatus_Unknown_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.不明);
        }

        private void listView_Result_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchLogResultItem resultItem1 = listView_Result.SelectedItem as SearchLogResultItem;
            if (resultItem1 == null)
            {
                panel_Epg.Visibility = Visibility.Collapsed;
            }
            else
            {
                panel_Epg.Visibility = Visibility.Visible;
                richTextBox_Epg.Document = resultItem1.getFlowDocumentOfEpg();
            }
        }

        private void button_Result_SearchTitleByRecLog_Click(object sender, RoutedEventArgs e)
        {
            new BlackoutWindow(Window.GetWindow(this)).showWindow("録画ログ検索");
            db_SearchLog.searchTitleByRecLog(_searchLogResultItems);
        }

        private void button_Edit_NotWord_Click(object sender, RoutedEventArgs e)
        {
            if (_notWordItem == null)
            {
                _notWordItem = new SearchLogNotWordItem();
            }
            _notWordItem.word = textBox_Edit_NotWord.Text;
            add2NotWordItems(_notWordItem);
            listView_Edit_NotWord.SelectedItem = _notWordItem;
            listView_Edit_NotWord.ScrollIntoView(_notWordItem);
            //
            textBox_Edit_NotWord.Text = null;
            _notWordItem = null;
            _isSearchLogItemEdited = true;
        }

        private void menu_Edit_NotWord_Delete_Click(object sender, RoutedEventArgs e)
        {
            deleteNotWordItem();
        }

        private void notWordItem_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    deleteNotWordItem();
                    break;
                case Key.Enter:
                    editNotWordItem(((ListViewItem)sender).DataContext as SearchLogNotWordItem);
                    break;
            }
        }

        void notWordItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            editNotWordItem(((ListViewItem)sender).DataContext as SearchLogNotWordItem);
        }

        private void checkBox_Edit_NotWord_IsTitleOnly_Checked(object sender, RoutedEventArgs e)
        {
            SearchLogNotWordItem notWordItem1 = ((CheckBox)sender).DataContext as SearchLogNotWordItem;
            notWordItem1.isTitleOnly = true;
        }

        private void checkBox_Edit_NotWord_IsTitleOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            SearchLogNotWordItem notWordItem1 = ((CheckBox)sender).DataContext as SearchLogNotWordItem;
            notWordItem1.isTitleOnly = false;
        }

        private void checkBox_Edit_NotWord_IsRegex_Checked(object sender, RoutedEventArgs e)
        {
            SearchLogNotWordItem notWordItem1 = ((CheckBox)sender).DataContext as SearchLogNotWordItem;
            notWordItem1.isRegex = true;
        }

        private void checkBox_Edit_NotWord_IsRegex_Unchecked(object sender, RoutedEventArgs e)
        {
            SearchLogNotWordItem notWordItem1 = ((CheckBox)sender).DataContext as SearchLogNotWordItem;
            notWordItem1.isRegex = false;
        }

        private void menu_Result_NotWord_Click(object sender, RoutedEventArgs e)
        {
            List<SearchLogResultItem> results1 = new List<SearchLogResultItem>();
            foreach (SearchLogResultItem item1 in listView_Result.SelectedItems) {
                results1.Add(item1);
            }
            Task.Run(() => {
                List<SearchLogItem> searchLogItems1 = new List<SearchLogItem>();
                foreach (SearchLogResultItem item1 in results1) {
                    string title1 = RecLogWindow.trimKeyword(item1.tvProgramTitle);
                    addNotWord(ref searchLogItems1, item1, title1, true);
                }
                search_AddNotWord(searchLogItems1);
            });
        }

        private void menu_Epg_NotWard_Click(object sender, RoutedEventArgs e)
        {
            string text1 = richTextBox_Epg.Selection.Text.Trim();
            if (string.IsNullOrWhiteSpace(text1)) { return; }

            SearchLogResultItem resultItem1 = (SearchLogResultItem)listView_Result.SelectedItem;
            List<SearchLogItem> searchLogItems1 = new List<SearchLogItem>();
            addNotWord(ref searchLogItems1, resultItem1, text1, false);
            search_AddNotWord(searchLogItems1);
        }

        private void menu_Edit_NotWord_Border_Maximize_Click(object sender, RoutedEventArgs e)
        {
            border_Edit_SearchWord.Visibility = Visibility.Collapsed;
            panel_Content_Service.Visibility = Visibility.Collapsed;
            border_Edit_Date.Visibility = Visibility.Collapsed;
            border_Edit_Scramble.Visibility = Visibility.Collapsed;
            //
            var scrollViewer_ListView1 = GetScrollViewer(listView_Edit_NotWord);
            if (scrollViewer_ListView1 != null)
            {
                scrollViewer_ListView1.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            }
            gridViewColumn_Edit_NotWord_Word.Width = 262;
        }

        private void menu_Edit_NotWord_Border_Restore_Click(object sender, RoutedEventArgs e)
        {
            restoreNotWordEditor();
        }

        private void border_Edit_NotWord_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (panel_Content_Service.Visibility == Visibility.Visible)
            {
                menu_Edit_NotWord_Border_Restore.IsEnabled = false;
                menu_Edit_NotWord_Border_Maximize.IsEnabled = true;
            }
            else
            {
                menu_Edit_NotWord_Border_Restore.IsEnabled = true;
                menu_Edit_NotWord_Border_Maximize.IsEnabled = false;
            }
        }

        private void checkBox_Edit_NotWord_IsTitleOnly_Header_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked1 = (((CheckBox)sender).IsChecked == true);
            foreach (var item in _notWordItems)
            {
                item.isTitleOnly = isChecked1;
            }

            _isSearchLogItemEdited = true;
        }

        private void checkBox_Edit_NotWord_IsRegex_Header_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked1 = (((CheckBox)sender).IsChecked == true);
            foreach (var item in _notWordItems)
            {
                item.isRegex = isChecked1;
            }

            _isSearchLogItemEdited = true;
        }

        private void menu_Edit_NotWord_EditWord_Click(object sender, RoutedEventArgs e)
        {
            editNotWordItem(((MenuItem)sender).DataContext as SearchLogNotWordItem);
        }

        private void menu_Epg_Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(richTextBox_Epg.Selection.Text);
        }

        private void menu_Epg_SearchByWeb_Click(object sender, RoutedEventArgs e)
        {
            RecLogWindow.searchByWeb(richTextBox_Epg.Selection.Text);
        }

        private void richTextBox_Epg_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            bool isEnabled1 = (string.IsNullOrWhiteSpace(richTextBox_Epg.Selection.Text) == false);
            foreach (MenuItem item1 in menu_Epg.Items)
            {
                item1.IsEnabled = isEnabled1;
            }
        }

        private void textBox_Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            _isSearchLogItemEdited = true;
        }

        private void checkBox_Editor_Click(object sender, RoutedEventArgs e)
        {
            _isSearchLogItemEdited = true;
        }

        private void button_Editor_Filter_Genre_Click(object sender, RoutedEventArgs e)
        {
            ContentKindInfo info1 = comboBox_Editor_Filter_Genre.SelectedItem as ContentKindInfo;
            addGenre2NotWord(info1);
        }

        private void checkBox_ResultItem_Confirmed_Click(object sender, RoutedEventArgs e)
        {
            SearchLogResultItem resultItem1 = ((CheckBox)sender).DataContext as SearchLogResultItem;
            updateResultItem_ConfirmedProperty(resultItem1);
            if (!isShowConfirmedResult && resultItem1.confirmed == true)
            {
                if (listView_Result.SelectedItem == resultItem1)
                {
                    listView_Result.SelectedItem = null;
                }
            }
        }

        private void menu_Result_AllConfirmed_Click(object sender, RoutedEventArgs e)
        {
            checkBox_Result_Confirmed.IsChecked = !checkBox_Result_Confirmed.IsChecked;
            checkBox_Result_Confirmed_Click(null, null);
        }

        private void menu_Resutl_AutoAdd_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menu1 = (MenuItem)sender;
            SearchLogResultItem resultItem1 = menu1.DataContext as SearchLogResultItem;
            if (resultItem1 != null)
            {
                MenuUtil.SendAutoAdd(resultItem1.epgEventInfoR);
            }
        }

        private void ctxMenu_ReusltItem_ContextMenuOpened(object sender, RoutedEventArgs e)
        {
            ContextMenu ctxMenu1 = sender as ContextMenu;
            SearchLogResultItem sri1 = ctxMenu1.DataContext as SearchLogResultItem;
            SearchLogItem logItem_Edit1 = listView_SearchLog.SelectedItem as SearchLogItem;
            if (logItem_Edit1 == null)
            {
                MessageBox.Show("SearchLogItem NOT Selected.", "失敗");
                return;
            }
            foreach (MenuItem item1 in ctxMenu1.Items)
            {
                switch (item1.Name)
                {
                    case "menu_Result_AddGenre":
                        {
                            MenuUtil.addGenre(item1, sri1.epgEventInfoR.ContentInfo.nibbleList, (contentKindInfo0) =>
                            {
                                if (logItem_Edit == null)
                                {
                                    setLogItem2Editor(logItem_Edit1);
                                }
                                foreach (ContentKindInfo cki1 in listBox_Editor_Content.Items)
                                {
                                    if (contentKindInfo0.Data.Nibble1 == cki1.Data.Nibble1 && contentKindInfo0.Data.Nibble2 == cki1.Data.Nibble2)
                                    {
                                        MessageBox.Show("すでに追加されています");
                                        return;
                                    }
                                }
                                add2ContentKindInfoList(contentKindInfo0);
                            });
                        }
                        break;
                    case "menu_Result_AddGenre_NotWord":
                        {
                            MenuUtil.addGenre(item1, sri1.epgEventInfoR.ContentInfo.nibbleList, (contentKindInfo0) =>
                            {
                                if (logItem_Edit == null)
                                {
                                    setLogItem2Editor(logItem_Edit1);
                                }
                                addGenre2NotWord(contentKindInfo0);
                            });
                        }
                        break;
                }
            }
        }

        private void toggleButton_ServieEditor_Click(object sender, RoutedEventArgs e)
        {
            listView_SearchLog.SelectedItem = null;
        }

        private void toggleButton_serviceEditor_All_Click(object sender, RoutedEventArgs e)
        {
            CheckBox[] cboxes1 = new CheckBox[] { checkBox_serviceEditor_Tere, checkBox_serviceEditor_BS, checkBox_serviceEditor_CS };
            bool isChecked1 = ((ToggleButton)sender).IsChecked == true;
            foreach (var item in cboxes1)
            {
                item.IsChecked = isChecked1;
            }
            foreach (var item in _serviceList_Tere.Concat(_serviceList_BS).Concat(_serviceList_CS))
            {
                item.IsSelected = isChecked1;
            }
            _isSearchLogItemEdited = true;
        }

        private void checkBox_servicceEditor_Tere_Click(object sender, RoutedEventArgs e)
        {
            _isSearchLogItemEdited = true;
            bool isChecked1 = ((CheckBox)sender).IsChecked == true;
            foreach (var item in _serviceList_Tere)
            {
                item.IsSelected = isChecked1;
            }
        }

        private void checkBox_servicceEditor_BS_Click(object sender, RoutedEventArgs e)
        {
            _isSearchLogItemEdited = true;
            bool isChecked1 = ((CheckBox)sender).IsChecked == true;
            foreach (var item in _serviceList_BS)
            {
                item.IsSelected = isChecked1;
            }
        }

        private void checkBox_servicceEditor_CS_Click(object sender, RoutedEventArgs e)
        {
            _isSearchLogItemEdited = true;
            bool isChecked1 = ((CheckBox)sender).IsChecked == true;
            foreach (var item in _serviceList_CS)
            {
                item.IsSelected = isChecked1;
            }
        }

        private void toggleButton_serviceEditor_All_Checked(object sender, RoutedEventArgs e)
        {
            toggleButton_serviceEditor_All.Content = "全て解除";
        }

        private void toggleButton_serviceEditor_All_Unchecked(object sender, RoutedEventArgs e)
        {
            toggleButton_serviceEditor_All.Content = "全て選択";
        }

        private void toggleButton_ServieEditMode_Checked(object sender, RoutedEventArgs e)
        {
            panel_serviceEditor.Visibility = Visibility.Visible;
            listView_Result.Visibility = Visibility.Collapsed;
        }

        private void toggleButton_ServieEditMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isSearchLogItemEdited)
            {
                update_SearchLogItem_Service();
            }
            panel_serviceEditor.Visibility = Visibility.Collapsed;
            listView_Result.Visibility = Visibility.Visible;
        }

        private void button_Result_Help_Click(object sender, RoutedEventArgs e)
        {
            searchLogView.showHelp();
        }

        private void menu_Edit_NotWord_RemoveAndkey_Click(object sender, RoutedEventArgs e)
        {
            logItem_Edit.removeAndkeyFromNotWords();
            //
            _notWordItems.Clear();
            foreach (var item in logItem_Edit.notWordItems_Get())
            {
                _notWordItems.Add(item);
            }
            _isSearchLogItemEdited = true;
        }

        #region - Inner Class -
        #endregion

        class DateItem
        {
            public DateItem(EpgSearchDateInfo info) { DateInfo = info; }
            public EpgSearchDateInfo DateInfo { get; private set; }
            public override string ToString() { return CommonManager.ConvertTimeText(DateInfo); }
        }
    }
}
