using EpgTimer.Common;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;

namespace EpgTimer.DefineClass
{
    /// <summary>
    /// 検索結果
    /// </summary>
    public class SearchLogResultItem : NotifyPropertyChangedItem, IDBRecord
    {

        static Brush foreground_Recorded = new SolidColorBrush(Color.FromRgb(66, 0, 0));
        static Brush background_Reserved = new SolidColorBrush(Color.FromRgb(215, 228, 242));
        static Brush background_Recorded = new SolidColorBrush(Color.FromRgb(230, 215, 215));
        static Brush background_Unknown = new SolidColorBrush(Color.FromRgb(220, 220, 220));

        #region - Constructor -
        #endregion

        static SearchLogResultItem()
        {
            foreground_Recorded.Freeze();
            background_Reserved.Freeze();
            background_Recorded.Freeze();
            background_Unknown.Freeze();
        }

        public SearchLogResultItem() { }

        public SearchLogResultItem(SearchLogItem logItem0, EpgEventInfo epgEventInfo0, DateTime lastUpdate0)
        {
            this.searchLogItemID = logItem0.ID;
            this.epgSearchKeyInfo = logItem0.epgSearchKeyInfoS;
            this.epgEventInfoR = new EpgEventInfoR(epgEventInfo0, lastUpdate0);
        }

        #region - Method -
        #endregion

        public void updateReserveInfo()
        {
            foreach (ReserveData rd1 in CommonManager.Instance.DB.ReserveList.Values)
            {
                if (rd1.Create64PgKey() == this.epgEventInfoR.Create64PgKey())
                {
                    if (rd1.RecSetting.RecMode == 0x05)
                    {
                        recodeStatus = RecLogItem.RecodeStatuses.無効登録;
                    }
                    else
                    {
                        recodeStatus = RecLogItem.RecodeStatuses.予約済み;
                    }
                }
            }
        }

        public FlowDocument getFlowDocumentOfEpg()
        {
            FlowDocument flowDoc = ConvertProgramText(epgEventInfoR, EventInfoTextMode.All);
            if (flowDoc == null)
            {
                flowDoc.Blocks.Add(
                    new Paragraph(
                        new Run("番組情報がありません。\r\n" + "またはEPGデータが読み込まれていません。")));
            }
            else if (this.epgSearchKeyInfo != null)
            {
                /* 検索ワードをハイライト */
                bool isRegex1 = (this.epgSearchKeyInfo.regExpFlag == 1);
                bool isCasesensitive1 = (this.epgSearchKeyInfo.caseFlag == 1);
                highlightText(flowDoc, this.epgSearchKeyInfo.andKey, isRegex1, isCasesensitive1);
            }

            return flowDoc;
        }

        public static FlowDocument ConvertProgramText(EpgEventInfo eventInfo, EventInfoTextMode textMode)
        {
            if (eventInfo == null) { return null; }

            FlowDocument flowDoc1 = new FlowDocument()
            {
                FontFamily = new FontFamily("メイリオ"),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(10, 10, 100)),
            };
            Paragraph p_basicInfo1 = new Paragraph();
            Section sctn_ExtInfo1 = new Section();

            string serviceName1 = null;
            UInt64 key = eventInfo.Create64Key();
            if (ChSet5.ChList.ContainsKey(key) == true)
            {
                serviceName1 = ChSet5.ChList[key].service_name + "(" + ChSet5.ChList[key].network_name + ")";
            }

            string date1 = null;
            if (eventInfo.StartTimeFlag == 1)
            {
                date1 = CommonManager.ConvertTimeText(eventInfo.start_time, false, true) + " ～ ";
            }
            else
            {
                date1 = "未定 ～ ";
            }
            if (eventInfo.DurationFlag == 1)
            {
                DateTime endTime = eventInfo.start_time + TimeSpan.FromSeconds(eventInfo.durationSec);
                date1 += CommonManager.ConvertTimeText(endTime, true, true);
            }
            else
            {
                date1 += "未定";
            }

            if (eventInfo.ShortInfo != null)
            {
                Inline event_name1 = new Bold(
                    new Run(eventInfo.ShortInfo.event_name + Environment.NewLine))
                {
                    FontSize = 12
                };
                p_basicInfo1.Inlines.Add(event_name1);
                //
                if (!string.IsNullOrWhiteSpace(date1))
                {
                    p_basicInfo1.Inlines.Add(new Run(date1 + Environment.NewLine));
                }
                if (!string.IsNullOrWhiteSpace(serviceName1))
                {
                    p_basicInfo1.Inlines.Add(new Run(serviceName1));
                }
                addParagraph(eventInfo.ShortInfo.text_char, sctn_ExtInfo1, 0);
            }

            if (eventInfo.ExtInfo != null)
            {
                addParagraph(eventInfo.ExtInfo.text_char, sctn_ExtInfo1, 0);
            }

            //ジャンル
            Section sctn_Genre1 = addSection(sctn_ExtInfo1);
            addParagraph("ジャンル:", sctn_Genre1, 0);
            if (eventInfo.ContentInfo != null)
            {
                var contentList = new List<ContentKindInfo>();
                if (eventInfo.ContentInfo != null)
                {
                    contentList = eventInfo.ContentInfo.nibbleList.Select(data => CommonManager.ContentKindInfoForDisplay(data)).ToList();
                }
                foreach (ContentKindInfo info in contentList.Where(info => info.Data.IsAttributeInfo == false))
                {
                    addParagraph(info.ListBoxView, sctn_Genre1);
                }
            }

            //映像
            Section sctn_ComponentInfo1 = addSection(sctn_ExtInfo1);
            addParagraph("映像:", sctn_ComponentInfo1, 0);
            if (eventInfo.ComponentInfo != null)
            {
                StringBuilder sb1 = new StringBuilder();
                int streamContent = eventInfo.ComponentInfo.stream_content;
                int componentType = eventInfo.ComponentInfo.component_type;
                UInt16 componentKey = (UInt16)(streamContent << 8 | componentType);
                if (CommonManager.ComponentKindDictionary.ContainsKey(componentKey) == true)
                {
                    addParagraph(CommonManager.ComponentKindDictionary[componentKey], sctn_ComponentInfo1);
                }
                if (eventInfo.ComponentInfo.text_char.Length > 0)
                {
                    addParagraph(eventInfo.ComponentInfo.text_char, sctn_ComponentInfo1);
                }
            }

            //音声
            Section sctn_AudioInfo1 = addSection(sctn_ExtInfo1);
            addParagraph("音声:", sctn_AudioInfo1, 0);
            if (eventInfo.AudioInfo != null)
            {
                foreach (EpgAudioComponentInfoData info in eventInfo.AudioInfo.componentList)
                {
                    StringBuilder sb1 = new StringBuilder();
                    int streamContent = info.stream_content;
                    int componentType = info.component_type;
                    UInt16 componentKey = (UInt16)(streamContent << 8 | componentType);
                    if (CommonManager.ComponentKindDictionary.ContainsKey(componentKey) == true)
                    {
                        sb1.AppendLine(CommonManager.ComponentKindDictionary[componentKey]);
                    }
                    if (info.text_char.Length > 0)
                    {
                        sb1.AppendLine(info.text_char);
                    }
                    string samplingRate1 = "サンプリングレート: ";
                    switch (info.sampling_rate)
                    {
                        case 1:
                            samplingRate1 += "16kHz";
                            break;
                        case 2:
                            samplingRate1 += "22.05kHz";
                            break;
                        case 3:
                            samplingRate1 += "24kHz";
                            break;
                        case 5:
                            samplingRate1 += "32kHz";
                            break;
                        case 6:
                            samplingRate1 += "44.1kHz";
                            break;
                        case 7:
                            samplingRate1 += "48kHz";
                            break;
                        default:
                            break;
                    }
                    sb1.Append(samplingRate1);
                    double marginTop1 = 0;
                    if (1 < sctn_AudioInfo1.Blocks.Count)
                    {
                        marginTop1 = 5;
                    }
                    addParagraph(sb1.ToString(), sctn_AudioInfo1, marginTop0: marginTop1);
                }
            }
            {
                Section sctn_ID1 = addSection(sctn_ExtInfo1);
                addParagraph("OriginalNetworkID : " + eventInfo.original_network_id.ToString() + " (0x" + eventInfo.original_network_id.ToString("X4") + ")", sctn_ID1, 0);
                addParagraph("TransportStreamID : " + eventInfo.transport_stream_id.ToString() + " (0x" + eventInfo.transport_stream_id.ToString("X4") + ")", sctn_ID1, 0);
                addParagraph("ServiceID : " + eventInfo.service_id.ToString() + " (0x" + eventInfo.service_id.ToString("X4") + ")", sctn_ID1, 0);
                addParagraph("EventID : " + eventInfo.event_id.ToString() + " (0x" + eventInfo.event_id.ToString("X4") + ")", sctn_ID1, 0);
            }
            if (textMode == EventInfoTextMode.BasicText || textMode == EventInfoTextMode.All) 
            {
                flowDoc1.Blocks.Add(p_basicInfo1);
            }
            if (textMode == EventInfoTextMode.All)
            {
                flowDoc1.Blocks.Add(sctn_ExtInfo1);
            }

            return flowDoc1;
        }

        static Section addSection(Section parent0, double marginTop = 10)
        {
            Section section1 = new Section()
            {
                Margin = new System.Windows.Thickness(0, marginTop, 0, 0)
            };
            parent0.Blocks.Add(section1);

            return section1;
        }

        static Paragraph addParagraph(string text0, Section parent0, double marginLeft0 = 10, double marginTop0 = 0)
        {
            Paragraph paragraph1 = new Paragraph(new Run(text0))
            {
                Margin = new System.Windows.Thickness(marginLeft0, marginTop0, 0, 0)
            };
            parent0.Blocks.Add(paragraph1);

            return paragraph1;
        }

        public static void highlightText(FlowDocument document, string keyword0, bool isRegex0, bool isCasesensitive0)
        {
            string keyword1;
            if (isRegex0)
            {
                keyword1 = keyword0;
            }
            else
            {
                StringBuilder sb1 = new StringBuilder();
                foreach (var item in Regex.Split(keyword0, "\\s+"))
                {
                    if (0 < sb1.Length)
                    {
                        sb1.Append("|");
                    }
                    sb1.Append("(" + Regex.Escape(item) + ")");
                }
                keyword1 = sb1.ToString();
            }
            // 半角・全角を区別しない
            {
                Regex rgx_Wide1 = new Regex("[０-９Ａ-Ｚａ-ｚ：－　]+");
                Regex rgx_Narrow1 = new Regex("[0-9A-Za-z\\:\\- ]+");
                StringBuilder sb1 = new StringBuilder();
                bool isEscaped1 = false;
                for (int i1 = 0; i1 < keyword1.Length; i1++)
                {
                    string s2 = keyword1[i1].ToString();
                    if (isRegex0 && keyword1[i1] == '\\')
                    {
                        sb1.Append(s2);
                        isEscaped1 = true;
                    }
                    else if (isEscaped1)
                    {
                        sb1.Append(s2);
                        isEscaped1 = false;
                    }
                    else
                    {
                        Match m_W2 = rgx_Wide1.Match(s2);
                        Match m_N2 = rgx_Narrow1.Match(s2);
                        if (m_W2.Success)
                        {
                            string s3 = Strings.StrConv(m_W2.Value, VbStrConv.Narrow, 0);
                            sb1.Append("(" + s2 + "|" + s3 + ")");
                        }
                        else if (m_N2.Success)
                        {
                            string s3 = Strings.StrConv(m_N2.Value, VbStrConv.Wide, 0);
                            sb1.Append("(" + s2 + "|" + s3 + ")");
                        }
                        else
                        {
                            sb1.Append(s2);
                        }
                    }
                }
                keyword1 = sb1.ToString();
            }
            //
            RegexOptions regexOptions1 = RegexOptions.Compiled;
            if (!isCasesensitive0)
            {
                regexOptions1 |= RegexOptions.IgnoreCase;
            }
            Regex rgx1 = new Regex(keyword1, regexOptions1);
            TextPointer pointer = document.ContentStart;
            while (pointer != null)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                    List<TextRange> textRanges1 = new List<TextRange>();
                    foreach (Match match1 in rgx1.Matches(textRun))
                    {
                        TextPointer start = pointer.GetPositionAtOffset(match1.Index);
                        TextPointer end = start.GetPositionAtOffset(match1.Length);
                        TextRange textRange1 = new TextRange(start, end);
                        textRanges1.Add(textRange1);
                    }
                    foreach (var textRange1 in textRanges1)
                    {
                        textRange1.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
                    }
                }

                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            SearchLogResultItem item0 = obj as SearchLogResultItem;
            if (item0 == null)
            {
                return false;
            }
            return (searchLogItemID == item0.searchLogItemID
                && epgEventInfoR.Equals(item0.epgEventInfoR));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        void setStatusProperty(RecLogItem.RecodeStatuses recodeStatus0)
        {
            /* status */
            switch (recodeStatus0)
            {
                case RecLogItem.RecodeStatuses.NONE:
                    if (epgEventInfoR != null && epgEventInfoR.IsOnAir() == true)
                    {
                        status = "放";
                    }
                    else
                    {
                        status = null;
                    }
                    break;
                case RecLogItem.RecodeStatuses.予約済み:
                    status = "予";
                    break;
                case RecLogItem.RecodeStatuses.録画完了:
                    status = "録";
                    break;
                case RecLogItem.RecodeStatuses.視聴済み:
                    status = "視";
                    break;
                case RecLogItem.RecodeStatuses.録画異常:
                    status = "異";
                    break;
                case RecLogItem.RecodeStatuses.無効登録:
                    status = "無";
                    break;
                case RecLogItem.RecodeStatuses.不明:
                    status = "?";
                    break;
                default:
                    throw new NotSupportedException();
            }
            /* statusColor */
            switch (recodeStatus)
            {
                case RecLogItem.RecodeStatuses.録画完了:
                case RecLogItem.RecodeStatuses.視聴済み:
                case RecLogItem.RecodeStatuses.録画異常:
                    statusColor = Brushes.Firebrick;
                    break;
                case RecLogItem.RecodeStatuses.無効登録:
                case RecLogItem.RecodeStatuses.不明:
                    statusColor = Brushes.Black;
                    break;
                default:
                    statusColor = Brushes.Navy;
                    break;
            }
            /* background */
            switch (recodeStatus)
            {
                case RecLogItem.RecodeStatuses.予約済み:
                    background = background_Reserved;
                    break;
                case RecLogItem.RecodeStatuses.録画完了:
                case RecLogItem.RecodeStatuses.視聴済み:
                case RecLogItem.RecodeStatuses.録画異常:
                    background = background_Recorded;
                    break;
                case RecLogItem.RecodeStatuses.無効登録:
                case RecLogItem.RecodeStatuses.不明:
                    background = background_Unknown;
                    break;
                default:
                    background = Brushes.White;
                    break;
            }
        }

        #region - Property -
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public long ID
        {
            get { return _ID; }
            set { _ID = value; }
        }
        long _ID = -1;

        public long searchLogItemID
        {
            get { return this._searchLogItemID; }
            set { this._searchLogItemID = value; }
        }
        long _searchLogItemID = -1;

        public long epgEventInfoID
        {
            get { return this._epgEventInfoID; }
            set { this._epgEventInfoID = value; }
        }
        long _epgEventInfoID = -1;

        /// <summary>
        /// 
        /// </summary>
        public EpgEventInfoR epgEventInfoR
        {
            get { return this._epgEventInfoR; }
            set { this._epgEventInfoR = value; }
        }
        EpgEventInfoR _epgEventInfoR = null;

        /// <summary>
        /// 
        /// </summary>
        public RecLogItem.RecodeStatuses recodeStatus
        {
            get { return this._recodeStatus; }
            set
            {
                this._recodeStatus = value;
                setStatusProperty(value);
            }
        }
        RecLogItem.RecodeStatuses _recodeStatus = RecLogItem.RecodeStatuses.NONE;

        /// <summary>
        /// 検索ワードをハイライト
        /// </summary>
        public EpgSearchKeyInfo epgSearchKeyInfo { get; set; } = null;

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public string status
        {
            get { return this._status; }
            set
            {
                if (this._status != value)
                {
                    this._status = value;
                    NotifyPropertyChanged();
                }
            }
        }
        string _status = null;

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public Brush statusColor
        {
            get
            {
                if (epgEventInfoR.IsOnAir() == true)
                {
                    if (recodeStatus != RecLogItem.RecodeStatuses.NONE)
                    {
                        return Brushes.Firebrick;
                    }
                    else
                    {
                        return Brushes.Navy;
                    }
                }
                else
                {
                    return _statusColor;
                }
            }
            set
            {
                if (_statusColor != value)
                {
                    _statusColor = value;
                    NotifyPropertyChanged();
                }
            }
        }
        Brush _statusColor = Brushes.Black;

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public bool confirmed
        {
            get { return this._confirmed; }
            set
            {
                if (this._confirmed != value)
                {
                    this._confirmed = value;
                    NotifyPropertyChanged();
                }
            }
        }
        bool _confirmed = false;

        /// <summary>
        /// ListViewへの表示または非表示を指示
        /// </summary>
        public bool isVisible
        {
            get { return this._isVisible; }
            set
            {
                if (this._isVisible != value)
                {
                    this._isVisible = value;
                    NotifyPropertyChanged();
                }
            }
        }
        bool _isVisible = false;

        /// <summary>
        /// ListViewItem Sort Property
        /// </summary>
        public DateTime date
        {
            get { return epgEventInfoR.start_time; }
        }

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public string dateStr
        {
            get { return epgEventInfoR.start_time.ToString("MM/dd(ddd) HH:mm"); }
        }

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public string timeLength
        {
            get { return TimeSpan.FromSeconds(epgEventInfoR.durationSec).ToString("hh\\:mm"); }
        }

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public string tvProgramTitle
        {
            get
            {
                if (epgEventInfoR != null && epgEventInfoR.ShortInfo != null)
                {
                    return epgEventInfoR.ShortInfo.event_name;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public string networkName
        {
            get
            {
                if (string.IsNullOrEmpty(_networkName))
                {
                    _networkName = CommonManager.ConvertNetworkNameText(epgEventInfoR.original_network_id);
                }
                return this._networkName;
            }
        }
        string _networkName = null;

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public string serviceName
        {
            get
            {
                if (string.IsNullOrEmpty(_serviceName))
                {
                    ulong key1 = CommonManager.Create64Key(epgEventInfoR.original_network_id, epgEventInfoR.transport_stream_id, epgEventInfoR.service_id);
                    _serviceName = ChSet5.ChList[key1].service_name;
                }
                return this._serviceName;
            }
        }
        string _serviceName = null;

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public string programContent
        {
            get
            {
                if (string.IsNullOrEmpty(_programContent)
                    && epgEventInfoR.ShortInfo != null)
                {
                    _programContent = epgEventInfoR.ShortInfo.text_char.Replace("\r\n", " ");
                }
                return this._programContent;
            }
        }
        string _programContent = null;

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public string jyanruKey
        {
            get
            {
                if (string.IsNullOrEmpty(_jyanruKey)
                    && epgEventInfoR != null)
                {
                    _jyanruKey = CommonManager.ConvertJyanruText(epgEventInfoR);
                }
                //
                return _jyanruKey;
            }
        }
        string _jyanruKey = null;

        /// <summary>
        ///  ListViewItem Property
        /// </summary>
        public Brush borderBrush
        {
            get
            {
                if (epgEventInfoR == null || epgEventInfoR.ContentInfo == null)
                {
                    return Brushes.White;
                }
                else if (epgEventInfoR.ContentInfo.nibbleList.Count == 0)
                {
                    return Brushes.Gainsboro;
                }
                return ViewUtil.EpgDataContentBrush(epgEventInfoR.ContentInfo.nibbleList);
            }
        }

        /// <summary>
        ///  ListViewItem Property
        /// </summary> 
        public Brush background
        {
            get { return this._background; }
            set
            {
                if (this._background != value)
                {
                    this._background = value;
                    NotifyPropertyChanged();
                }
            }
        }
        Brush _background = null;

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public string imageQuality
        {
            get { return this.epgEventInfoR.imageQuality; }
        }

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public Brush foreground_ImageQuality
        {
            get { return this.epgEventInfoR.foreground_ImageQuality; }
        }

        #region - Event Handler -
        #endregion

    }

}
