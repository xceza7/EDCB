﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer.EpgView
{
    public class EpgViewData
    {
        //表示形式間で番組表定義と番組リストを共有する
        //EpgTimerNWで検索絞り込みを使用時に多少効果があるくらいだが‥
        public EpgViewData()
        {
            EpgTabInfo = new CustomEpgTabInfo();
            viewFunc = new EpgDataView.EpgDataViewInterface(null);
            ClearEventList();
        }
        public void ClearEventList()
        {
            ServiceEventList = new List<EpgServiceEventInfo>();
            EventUIDList = new Dictionary<UInt64, EpgEventInfo>();
            IsEpgLoaded = false;
        }
        public EpgDataView.EpgDataViewInterface viewFunc { get; set; }
        public CustomEpgTabInfo EpgTabInfo { get; set; }
        public bool HasKey(UInt64 key) { return KeyList.Contains(key); }
        public IEnumerable<UInt64> KeyList { get { return IsEpgLoaded ? ServiceEventList.Select(info => info.serviceInfo.Key) : CommonManager.Instance.DB.ExpandSpecialKey(EpgTabInfo.ViewServiceList); } }
        public bool IsEpgLoaded { get; private set; }
        public List<EpgServiceEventInfo> ServiceEventList { get; private set; }
        public Dictionary<UInt64, EpgEventInfo> EventUIDList { get; private set; }

        public EpgViewPeriod Period = EpgViewPeriod.DefPeriod;
        public bool IsDefPeriod = true;

        public bool ReloadEpgData(EpgViewPeriod newPeriod = null, bool noMsg = false)
        {
            try
            {
                newPeriod = newPeriod ?? EpgViewPeriod.DefPeriod;
                if (Period.Equals(newPeriod) == true && IsEpgLoaded == true) return true;

                if (CommonManager.Instance.WaitingSrvReady == true)
                {
                    StatusManager.StatusNotifySet("EpgTimerSrv準備完了待ち");
                    return false;
                }
                if (CommonManager.Instance.IsConnected == false) return false;

                ErrCode err;
                var serviceDic = new Dictionary<UInt64, EpgServiceAllEventInfo>();
                if (EpgTabInfo.SearchMode == false)
                {
                    err = CommonManager.Instance.DB.LoadEpgData(ref serviceDic, newPeriod, EpgTabInfo.ViewServiceList);
                }
                else
                {
                    //番組情報の検索
                    err = CommonManager.Instance.DB.SearchPgLists(EpgTabInfo.GetSearchKeyReloadEpg().IntoList(), ref serviceDic, newPeriod);
                }
                if ((noMsg && err != ErrCode.CMD_SUCCESS) 
                    || CommonManager.CmdErrMsgTypical(err, "EPGデータの取得", err == ErrCode.CMD_ERR_BUSY ?
                    "EPGデータの読み込みを行える状態ではありません。\r\n(EPGデータ読み込み中など)" :
                    "エラーが発生しました。\r\nEPGデータが読み込まれていない可能性があります。") == false) return false;

                //並び順はViewServiceListによる。eventListはこの後すぐ作り直すのでとりあえずそのままもらう。
                ServiceEventList = CommonManager.Instance.DB.ExpandSpecialKey(EpgTabInfo.ViewServiceList, serviceDic.Values.Select(info => info.serviceInfo))
                    .Where(id => serviceDic.ContainsKey(id)).Select(id => serviceDic[id])
                    .Select(info => new EpgServiceEventInfo { serviceInfo = info.serviceInfo, eventList = info.eventMergeList.ToList() }).ToList();

                EventUIDList = new Dictionary<ulong, EpgEventInfo>();
                var viewContentMatchingHash = new HashSet<UInt32>(EpgTabInfo.ViewContentList.Select(d => d.MatchingKeyList).SelectMany(x => x));
                foreach (EpgServiceEventInfo item in ServiceEventList)
                {
                    item.eventList = item.eventList.FindAll(eventInfo =>
                        //開始時間未定を除外
                        (eventInfo.StartTimeFlag != 0)

                        //自動登録されたりするので、サービス別番組表では表示させる
                        //&& (eventInfo.IsGroupMainEvent == true)

                        //表示抑制
                        && (eventInfo.IsOver(newPeriod.Start) == false && eventInfo.PgStartTime < newPeriod.End)

                        //ジャンル絞り込み
                        && (ViewUtil.ContainsContent(eventInfo, viewContentMatchingHash, EpgTabInfo.ViewNotContentFlag) == true)
                    );
                    item.eventList.ForEach(data => EventUIDList[data.CurrentPgUID()] = data);
                }

                IsEpgLoaded = true;
                Period = newPeriod.DeepClone();
                IsDefPeriod = Period.Equals(EpgViewPeriod.DefPeriod);
                return true;
            }
            catch (Exception ex) { CommonUtil.DispatcherMsgBoxShow(ex.Message + "\r\n" + ex.StackTrace); }
            return false;
        }
    }

    public class EpgViewState { public int viewMode; }

    public class EpgViewBase : DataItemViewBase
    {
        public static event ViewUpdatedHandler ViewReserveUpdated = null;

        protected CmdExeReserve mc; //予約系コマンド集
        protected bool ReloadReserveInfoFlg = true;
        protected bool RefreshMenuFlg = true;

        protected EpgViewState restoreState = null;
        protected class StateBase : EpgViewState
        {
            public DateTime? scrollTime = null;
            public EpgViewPeriod period = null;
            public bool? isDefPeriod = null;
            public bool? isJumpDate = null;

            public StateBase() { }
            public StateBase(EpgViewBase view)
            {
                viewMode = view.viewMode;
                period = view.ViewPeriod.DeepClone();
                isDefPeriod = view.IsDataDefPeriod;
            }
        }
        public virtual void SetViewState(EpgViewState data) { restoreState = data; }
        public virtual EpgViewState GetViewState() { return new StateBase(this); }
        protected StateBase RestoreState { get { return restoreState as StateBase ?? new StateBase(); } }

        //表示形式間で番組表定義と番組リストを共有する
        //EpgTimerNWで検索絞り込みを使用時に多少効果があるくらいだが‥
        protected EpgViewData viewData = new EpgViewData();
        protected EpgDataView.EpgDataViewInterface viewFunc { get { return viewData.viewFunc; } }
        protected int viewMode = 0;//最初に設定した後は固定するコード。
        public void SetViewData(EpgViewData data, int mode)
        {
            viewData = data;
            viewMode = mode;
            ViewPeriod = viewData.Period;
        }
        protected CustomEpgTabInfo viewInfo { get { return viewData.EpgTabInfo; } }
        protected virtual bool viewCustNeedTimeOnly { get { return viewInfo.NeedTimeOnlyBasic; } }
        protected bool IsDataDefPeriod { get { return viewData.IsDefPeriod; } }
        protected EpgViewPeriod ViewPeriod = EpgViewPeriod.DefPeriod;
        protected EpgViewPeriod DataPeriod { get { return viewData.Period; } }
        protected bool IsJumpPanelOpened { get { return viewFunc.IsJumpPanelOpened; } set { viewFunc.IsJumpPanelOpened = value; } }
        protected List<EpgServiceEventInfo> serviceEventList { get { return viewData.ServiceEventList; } }
        protected List<EpgServiceInfo> serviceListOrderAdjust
        {
            get
            {
                var grpList = new SortedList<ulong, EpgServiceInfo>();
                var ordered = new List<EpgServiceInfo>();
                var back = new EpgServiceInfo();
                foreach (EpgServiceInfo info in serviceEventList.Select(item => item.serviceInfo))
                {
                    if (info.ONID != back.ONID || info.TSID != back.TSID || info.SID > back.SID)
                    {
                        ordered.AddRange(grpList.Values);
                        grpList.Clear();
                        back = info;
                    }
                    grpList[info.SID] = info;
                }
                ordered.AddRange(grpList.Values);
                return ordered;
            }
        }

        protected virtual void InitCommand()
        {
            base.updateInvisible = true;

            //ビューコードの登録
            mBinds.View = CtxmCode.EpgView;

            //コマンド集の初期化
            mc = new CmdExeReserve(this);

            //コマンド集にないものを登録
            mc.AddReplaceCommand(EpgCmds.ViewChgSet, (sender, e) => viewFunc.ViewSetting(-1));
            mc.AddReplaceCommand(EpgCmds.ViewChgReSet, (sender, e) => viewFunc.ViewSetting(-2));
            mc.AddReplaceCommand(EpgCmds.ViewChgMode, mc_ViewChgMode);

            //コマンド集を振り替えるもの
            mc.AddReplaceCommand(EpgCmds.JumpTable, mc_JumpTable);
        }

        //表示設定関係
        protected void mc_ViewChgMode(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var param = e.Parameter as EpgCmdParam;
                if (param == null || param.ID == viewMode) return;

                //BlackWindowに状態を登録。
                //コマンド集の機能による各ビューの共用メソッド。
                BlackoutWindow.SelectedData = mc.GetJumpTabItem();

                viewFunc.ViewSetting(param.ID);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
        protected void mc_JumpTable(object sender, ExecutedRoutedEventArgs e)
        {
            var param = e.Parameter as EpgCmdParam;
            if (param == null) return;

            param.ID = 0;//実際は設定するまでもなく、初期値0。
            BlackoutWindow.NowJumpTable = true;
            mc_ViewChgMode(sender, e);

            //EPG画面でのフォーカス対策。若干ウィンドウの表示タイミングが微妙だが、とりあえずこれで解決する。
            //切替え自体は上のmc_ViewChgMode()が行っており、このコードはスプラッシュを表示するだけ。
            Dispatcher.BeginInvoke(new Action(() =>
            {
                new BlackoutWindow(ViewUtil.MainWindow).showWindow(ViewUtil.MainWindow.tabItem_epg.Header.ToString());
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void UpdateMenu(bool refesh = true)
        {
            RefreshMenuFlg |= refesh;
            RefreshMenu();
        }
        protected void RefreshMenu()
        {
            if (RefreshMenuFlg == true && this.IsVisible == true)
            {
                RefreshMenuInfo();
                RefreshMenuFlg = false;
            }
        }
        protected virtual void RefreshMenuInfo()
        {
            mc.EpgInfoOpenMode = Settings.Instance.EpgInfoOpenMode;
        }

        /// 保存関係
        public virtual void SaveViewData() { }

        //表示期間変更関係
        //mode: -1=戻る、+1=進む
        protected TimeJumpView jumpPanel = null;
        protected TimeMoveView movePanel = null;
        protected Button buttonNow = null;
        protected void SetControlsPeriod(TimeJumpView tj, TimeMoveView tm, Button bn)
        {
            jumpPanel = tj;
            movePanel = tm;
            buttonNow = bn;

            jumpPanel.JumpDateClick += JumpDate;
            movePanel.OpenToggleClick += MovePanel_OpenToggleClick;
            movePanel.MoveButtonClick += MovePanel_MoveButtonClick;
            movePanel.MoveButtonToolTipOpen += MovePanel_MoveButtonTooltip;
            buttonNow.Click += (sender, e) => NowTimeClick(true);
        }

        private void MovePanel_OpenToggleClick(bool isOpen)
        {
            IsJumpPanelOpened = isOpen;
            RefreshMoveButtonStatus();
        }
        protected virtual void MovePanel_MoveButtonClick(int mode)
        {
            SetJumpState();
            JumpDate(MoveTimeTarget(mode));
        }
        protected virtual void SetJumpState() { }
        protected EpgViewPeriod MoveTimeTarget(int mode)
        {
            var start = ViewPeriod.Start;
            if (Settings.Instance.EpgArcStartSunday && start.DayOfWeek != DayOfWeek.Sunday)
            {
                start += TimeSpan.FromDays(mode * (mode < 0 ? EpgViewPeriod.InitDays : ViewPeriod.Days));
                var offset = (int)start.DayOfWeek;
                if (offset != 0)
                {
                    start += TimeSpan.FromDays(-offset + (mode < 0 ? 7 : 0));
                }
            }
            else
            {
                start += TimeSpan.FromDays(mode * (mode < 0 ? EpgViewPeriod.InitMoveDays : ViewPeriod.MoveDays));
            }
            return start >= EpgViewPeriod.InitStart ? EpgViewPeriod.DefPeriod : new EpgViewPeriod(start);
        }
        public void MovePanel_MoveButtonTooltip(Button btn, ToolTipEventArgs e, int mode)
        {
            e.Handled = !btn.IsEnabled;
            btn.ToolTip = MoveTimeTarget(mode).ConvertText();
        }
        public void JumpDate(EpgViewPeriod period = null)
        {
            period = period ?? EpgViewPeriod.DefPeriod;
            if (period.Equals(ViewPeriod)) return;
            ViewPeriod = period.DeepClone();
            UpdateInfo(true);
        }
        protected virtual void RefreshMoveButtonStatus()
        {
            buttonNow.Content = IsDataDefPeriod ? "現在" : "初期\r\n表示";
            buttonNow.ToolTip = IsDataDefPeriod ? "現在時刻へスクロール" : EpgViewPeriod.DefPeriod.ConvertText();
            buttonNow.Tag = IsDataDefPeriod ? null : "jump";

            movePanel.SetButtonEnabled(
                ViewPeriod.Start > CommonManager.Instance.DB.EventTimeMin,
                ViewPeriod.End < (IsDataDefPeriod ? ViewPeriod.End : EpgViewPeriod.DefPeriod.End),
                IsJumpPanelOpened);

            jumpPanel.Visibility = IsJumpPanelOpened ? Visibility.Visible : Visibility.Collapsed;
            jumpPanel.SetDate(ViewPeriod, CommonManager.Instance.DB.EventTimeMin);
        }
        protected virtual void NowTimeClick(bool buttonAction)
        {
            if (buttonAction && buttonNow.Tag != null)
            {
                JumpDate();
                return;
            }
            MoveNowTime();
        }
        protected virtual void MoveNowTime() { }

        /// <summary>
        /// 予約情報更新通知
        /// </summary>
        public void UpdateReserveInfo(bool immediately = true)
        {
            ReloadReserveInfoFlg = true;
            if (immediately == true) ReloadReserveInfo();
        }
        protected void ReloadReserveInfo()
        {
            if (ReloadReserveInfoFlg == true)
            {
                ReloadReserveInfoFlg = !ReloadReserveInfoData();
                if (ViewReserveUpdated != null) ViewReserveUpdated(this, true);
                UpdateStatus();
            }
        }
        protected bool ReloadReserveInfoData()
        {
            CommonManager.Instance.DB.ReloadRecFileInfo();//起動直後用
            ReloadReserveViewItem();
            return true;
        }
        protected virtual void ReloadReserveViewItem() { }

        /// <summary>EPGデータ更新</summary>
        protected override bool ReloadInfoData()
        {
            EpgViewPeriod newPeriod = RestoreState.isDefPeriod == true ? EpgViewPeriod.DefPeriod : RestoreState.period ?? ViewPeriod;
            if (!viewData.ReloadEpgData(newPeriod, !this.IsVisible)) return false;
            ViewPeriod = DataPeriod.DeepClone();
            RefreshMoveButtonStatus();

            ReloadReserveInfoFlg = true;
            ReloadProgramViewItem();
            if (ReloadReserveInfoFlg == true) ReloadReserveInfoFlg = !ReloadReserveInfoData();
            restoreState = null;
            return true;
        }
        protected virtual void ReloadProgramViewItem() { }

        protected override void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible == false) return;

            RefreshMenu();
            JumpDate(BlackoutWindow.HasItemData ? SearchJumpPeriod(BlackoutWindow.ItemData.PgStartTime) : DataPeriod);
            ReloadInfo();//JumpDate()が実行された場合は、何もしない
            ReloadReserveInfo();//JumpDate() or ReloadInfo()が実行された場合は、何もしない

            if (BlackoutWindow.HasItemData)
            {
                //「番組表へジャンプ」の場合、またはオプションで指定のある場合に強調表示する。
                var isMarking = (BlackoutWindow.NowJumpTable || Settings.Instance.DisplayNotifyEpgChange) ? JumpItemStyle.JumpTo : JumpItemStyle.None;
                bool mgs = MoveToItem(BlackoutWindow.SelectedItem, isMarking) == false && BlackoutWindow.NowJumpTable == true;
                StatusManager.StatusNotifySet(mgs == false ? "" : "アイテムが見つかりませんでした < 番組表へジャンプ");
            }
            BlackoutWindow.Clear();

            RefreshMoveButtonStatus();
            RefreshStatus();
        }
        protected EpgViewPeriod SearchJumpPeriod(DateTime time)
        {
            if (DataPeriod.Contains(time) == true) return DataPeriod;
            if (EpgViewPeriod.DefPeriod.Contains(time) == true) return EpgViewPeriod.DefPeriod;

            //見つからない場合はそのまま
            if (CommonManager.Instance.DB.IsEventTimePossible(time) == false)
            { return DataPeriod; }

            return new EpgViewPeriod(time - TimeSpan.FromDays(Settings.Instance.EpgArcStartSunday ? (int)time.DayOfWeek : 0));
        }
        public bool IsEnabledJumpTab(SearchItem target)
        {
            return MoveToItem(target, JumpItemStyle.None, true);
        }
        public bool MoveToItem(SearchItem target, JumpItemStyle style = JumpItemStyle.MoveTo, bool dryrun = false)
        {
            if (target == null) return false;
            if (target.ReserveInfo != null)
            {
                ReloadReserveInfo();
                if(target.ReserveInfo is ReserveDataEnd)
                {
                    return MoveToRecInfoItem(MenuUtil.GetRecFileInfo(target.ReserveInfo), style, dryrun) >= 0;
                }
                else
                {
                    return MoveToReserveItem(target.ReserveInfo, style, dryrun) >= 0;
                }
            }
            return MoveToProgramItem(target.EventInfo, style, dryrun) >= 0;
        }
    }
}
