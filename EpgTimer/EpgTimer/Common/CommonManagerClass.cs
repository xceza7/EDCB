﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace EpgTimer
{
    class CommonManager
    {
        public CtrlCmdUtil CtrlCmd { get; private set; }
        public DBManager DB { get; private set; }
        public TVTestCtrlClass TVTestCtrl { get; private set; }
        public bool NWMode { get; set; }
        public List<NotifySrvInfo> NotifyLogList { get; private set; }
        public NWConnect NW { get; private set; }

        MenuManager _mm;
        public MenuManager MM
        {
            get
            {
                //初期化に他のオブジェクトを使うので遅延させる
                if (_mm == null) _mm = new MenuManager();
                //
                return _mm;
            }
            set { _mm = value; }
        }

        public List<Brush> CustContentColorList { get; private set; }
        public Brush CustTitle1Color { get; private set; }
        public Brush CustTitle2Color { get; private set; }
        public Brush CustTunerServiceColor { get; private set; }
        public Brush CustTunerTextColor { get; private set; }
        public List<Brush> CustTunerServiceColorPri { get; private set; }
        public Brush TunerBackColor { get; private set; }
        public Brush TunerReserveBorderColor { get; private set; }
        public Brush TunerTimeFontColor { get; private set; }
        public Brush TunerTimeBackColor { get; private set; }
        public Brush TunerTimeBorderColor { get; private set; }
        public Brush TunerNameFontColor { get; private set; }
        public Brush TunerNameBackColor { get; private set; }
        public Brush TunerNameBorderColor { get; private set; }
        public List<Brush> CustTimeColorList { get; private set; }
        public Brush EpgServiceBackColor { get; private set; }
        public Brush EpgBackColor { get; private set; }
        public Brush EpgBorderColor { get; private set; }
        public Brush EpgServiceFontColor { get; private set; }
        public Brush EpgServiceBorderColor { get; private set; }
        public Brush EpgTimeFontColor { get; private set; }
        public Brush EpgTimeBorderColor { get; private set; }
        public Brush EpgWeekdayBorderColor { get; private set; }
        public Brush ResDefBackColor { get; private set; }
        public Brush ResErrBackColor { get; private set; }
        public Brush ResWarBackColor { get; private set; }
        public Brush ResNoBackColor { get; private set; }
        public Brush ResAutoAddMissingBackColor { get; private set; }
        public Brush ResMultipleBackColor { get; private set; }
        public Brush ListDefForeColor { get; private set; }
        public List<Brush> RecModeForeColor { get; private set; }
        public Brush RecEndDefBackColor { get; private set; }
        public Brush RecEndErrBackColor { get; private set; }
        public Brush RecEndWarBackColor { get; private set; }
        public Brush StatResForeColor { get; private set; }
        public Brush StatRecForeColor { get; private set; }
        public Brush StatOnAirForeColor { get; private set; }

        private static CommonManager _instance;
        public static CommonManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CommonManager();
                return _instance;
            }
            set { _instance = value; }
        }

        public CommonManager()
        {
            CtrlCmd = new CtrlCmdUtil();
            DB = new DBManager(CtrlCmd);
            TVTestCtrl = new TVTestCtrlClass(CtrlCmd);
            NW = new NWConnect(CtrlCmd);
            NWMode = false;
            NotifyLogList = new List<NotifySrvInfo>();
            CustContentColorList = new List<Brush>();
            CustTunerServiceColorPri = new List<Brush>();
            CustTimeColorList = new List<Brush>();
            RecModeForeColor = new List<Brush>();
        }

        public static readonly Dictionary<UInt32, ContentKindInfo> ContentKindDictionary;
        public static readonly Dictionary<UInt16, string> ComponentKindDictionary;
        public static readonly string[] DayOfWeekArray;
        public static readonly string[] RecModeList;
        public static readonly string[] YesNoList;
        public static readonly string[] PriorityList;

        static CommonManager()
        {
            ContentKindDictionary = new[]
            {
                new ContentKindInfo(0x00FF0000, "ニュース／報道", ""),
                new ContentKindInfo(0x00000000, "ニュース／報道", "定時・総合"),
                new ContentKindInfo(0x00010000, "ニュース／報道", "天気"),
                new ContentKindInfo(0x00020000, "ニュース／報道", "特集・ドキュメント"),
                new ContentKindInfo(0x00030000, "ニュース／報道", "政治・国会"),
                new ContentKindInfo(0x00040000, "ニュース／報道", "経済・市況"),
                new ContentKindInfo(0x00050000, "ニュース／報道", "海外・国際"),
                new ContentKindInfo(0x00060000, "ニュース／報道", "解説"),
                new ContentKindInfo(0x00070000, "ニュース／報道", "討論・会談"),
                new ContentKindInfo(0x00080000, "ニュース／報道", "報道特番"),
                new ContentKindInfo(0x00090000, "ニュース／報道", "ローカル・地域"),
                new ContentKindInfo(0x000A0000, "ニュース／報道", "交通"),
                new ContentKindInfo(0x000F0000, "ニュース／報道", "その他"),

                new ContentKindInfo(0x01FF0000, "スポーツ", ""),
                new ContentKindInfo(0x01000000, "スポーツ", "スポーツニュース"),
                new ContentKindInfo(0x01010000, "スポーツ", "野球"),
                new ContentKindInfo(0x01020000, "スポーツ", "サッカー"),
                new ContentKindInfo(0x01030000, "スポーツ", "ゴルフ"),
                new ContentKindInfo(0x01040000, "スポーツ", "その他の球技"),
                new ContentKindInfo(0x01050000, "スポーツ", "相撲・格闘技"),
                new ContentKindInfo(0x01060000, "スポーツ", "オリンピック・国際大会"),
                new ContentKindInfo(0x01070000, "スポーツ", "マラソン・陸上・水泳"),
                new ContentKindInfo(0x01080000, "スポーツ", "モータースポーツ"),
                new ContentKindInfo(0x01090000, "スポーツ", "マリン・ウィンタースポーツ"),
                new ContentKindInfo(0x010A0000, "スポーツ", "競馬・公営競技"),
                new ContentKindInfo(0x010F0000, "スポーツ", "その他"),

                new ContentKindInfo(0x02FF0000, "情報／ワイドショー", ""),
                new ContentKindInfo(0x02000000, "情報／ワイドショー", "芸能・ワイドショー"),
                new ContentKindInfo(0x02010000, "情報／ワイドショー", "ファッション"),
                new ContentKindInfo(0x02020000, "情報／ワイドショー", "暮らし・住まい"),
                new ContentKindInfo(0x02030000, "情報／ワイドショー", "健康・医療"),
                new ContentKindInfo(0x02040000, "情報／ワイドショー", "ショッピング・通販"),
                new ContentKindInfo(0x02050000, "情報／ワイドショー", "グルメ・料理"),
                new ContentKindInfo(0x02060000, "情報／ワイドショー", "イベント"),
                new ContentKindInfo(0x02070000, "情報／ワイドショー", "番組紹介・お知らせ"),
                new ContentKindInfo(0x020F0000, "情報／ワイドショー", "その他"),

                new ContentKindInfo(0x03FF0000, "ドラマ", ""),
                new ContentKindInfo(0x03000000, "ドラマ", "国内ドラマ"),
                new ContentKindInfo(0x03010000, "ドラマ", "海外ドラマ"),
                new ContentKindInfo(0x03020000, "ドラマ", "時代劇"),
                new ContentKindInfo(0x030F0000, "ドラマ", "その他"),

                new ContentKindInfo(0x04FF0000, "音楽", ""),
                new ContentKindInfo(0x04000000, "音楽", "国内ロック・ポップス"),
                new ContentKindInfo(0x04010000, "音楽", "海外ロック・ポップス"),
                new ContentKindInfo(0x04020000, "音楽", "クラシック・オペラ"),
                new ContentKindInfo(0x04030000, "音楽", "ジャズ・フュージョン"),
                new ContentKindInfo(0x04040000, "音楽", "歌謡曲・演歌"),
                new ContentKindInfo(0x04050000, "音楽", "ライブ・コンサート"),
                new ContentKindInfo(0x04060000, "音楽", "ランキング・リクエスト"),
                new ContentKindInfo(0x04070000, "音楽", "カラオケ・のど自慢"),
                new ContentKindInfo(0x04080000, "音楽", "民謡・邦楽"),
                new ContentKindInfo(0x04090000, "音楽", "童謡・キッズ"),
                new ContentKindInfo(0x040A0000, "音楽", "民族音楽・ワールドミュージック"),
                new ContentKindInfo(0x040F0000, "音楽", "その他"),

                new ContentKindInfo(0x05FF0000, "バラエティ", ""),
                new ContentKindInfo(0x05000000, "バラエティ", "クイズ"),
                new ContentKindInfo(0x05010000, "バラエティ", "ゲーム"),
                new ContentKindInfo(0x05020000, "バラエティ", "トークバラエティ"),
                new ContentKindInfo(0x05030000, "バラエティ", "お笑い・コメディ"),
                new ContentKindInfo(0x05040000, "バラエティ", "音楽バラエティ"),
                new ContentKindInfo(0x05050000, "バラエティ", "旅バラエティ"),
                new ContentKindInfo(0x05060000, "バラエティ", "料理バラエティ"),
                new ContentKindInfo(0x050F0000, "バラエティ", "その他"),

                new ContentKindInfo(0x06FF0000, "映画", ""),
                new ContentKindInfo(0x06000000, "映画", "洋画"),
                new ContentKindInfo(0x06010000, "映画", "邦画"),
                new ContentKindInfo(0x06020000, "映画", "アニメ"),
                new ContentKindInfo(0x060F0000, "映画", "その他"),

                new ContentKindInfo(0x07FF0000, "アニメ／特撮", ""),
                new ContentKindInfo(0x07000000, "アニメ／特撮", "国内アニメ"),
                new ContentKindInfo(0x07010000, "アニメ／特撮", "海外アニメ"),
                new ContentKindInfo(0x07020000, "アニメ／特撮", "特撮"),
                new ContentKindInfo(0x070F0000, "アニメ／特撮", "その他"),

                new ContentKindInfo(0x08FF0000, "ドキュメンタリー／教養", ""),
                new ContentKindInfo(0x08000000, "ドキュメンタリー／教養", "社会・時事"),
                new ContentKindInfo(0x08010000, "ドキュメンタリー／教養", "歴史・紀行"),
                new ContentKindInfo(0x08020000, "ドキュメンタリー／教養", "自然・動物・環境"),
                new ContentKindInfo(0x08030000, "ドキュメンタリー／教養", "宇宙・科学・医学"),
                new ContentKindInfo(0x08040000, "ドキュメンタリー／教養", "カルチャー・伝統文化"),
                new ContentKindInfo(0x08050000, "ドキュメンタリー／教養", "文学・文芸"),
                new ContentKindInfo(0x08060000, "ドキュメンタリー／教養", "スポーツ"),
                new ContentKindInfo(0x08070000, "ドキュメンタリー／教養", "ドキュメンタリー全般"),
                new ContentKindInfo(0x08080000, "ドキュメンタリー／教養", "インタビュー・討論"),
                new ContentKindInfo(0x080F0000, "ドキュメンタリー／教養", "その他"),

                new ContentKindInfo(0x09FF0000, "劇場／公演", ""),
                new ContentKindInfo(0x09000000, "劇場／公演", "現代劇・新劇"),
                new ContentKindInfo(0x09010000, "劇場／公演", "ミュージカル"),
                new ContentKindInfo(0x09020000, "劇場／公演", "ダンス・バレエ"),
                new ContentKindInfo(0x09030000, "劇場／公演", "落語・演芸"),
                new ContentKindInfo(0x09040000, "劇場／公演", "歌舞伎・古典"),
                new ContentKindInfo(0x090F0000, "劇場／公演", "その他"),

                new ContentKindInfo(0x0AFF0000, "趣味／教育", ""),
                new ContentKindInfo(0x0A000000, "趣味／教育", "旅・釣り・アウトドア"),
                new ContentKindInfo(0x0A010000, "趣味／教育", "園芸・ペット・手芸"),
                new ContentKindInfo(0x0A020000, "趣味／教育", "音楽・美術・工芸"),
                new ContentKindInfo(0x0A030000, "趣味／教育", "囲碁・将棋"),
                new ContentKindInfo(0x0A040000, "趣味／教育", "麻雀・パチンコ"),
                new ContentKindInfo(0x0A050000, "趣味／教育", "車・オートバイ"),
                new ContentKindInfo(0x0A060000, "趣味／教育", "コンピュータ・ＴＶゲーム"),
                new ContentKindInfo(0x0A070000, "趣味／教育", "会話・語学"),
                new ContentKindInfo(0x0A080000, "趣味／教育", "幼児・小学生"),
                new ContentKindInfo(0x0A090000, "趣味／教育", "中学生・高校生"),
                new ContentKindInfo(0x0A0A0000, "趣味／教育", "大学生・受験"),
                new ContentKindInfo(0x0A0B0000, "趣味／教育", "生涯教育・資格"),
                new ContentKindInfo(0x0A0C0000, "趣味／教育", "教育問題"),
                new ContentKindInfo(0x0A0F0000, "趣味／教育", "その他"),

                new ContentKindInfo(0x0BFF0000, "福祉", ""),
                new ContentKindInfo(0x0B000000, "福祉", "高齢者"),
                new ContentKindInfo(0x0B010000, "福祉", "障害者"),
                new ContentKindInfo(0x0B020000, "福祉", "社会福祉"),
                new ContentKindInfo(0x0B030000, "福祉", "ボランティア"),
                new ContentKindInfo(0x0B040000, "福祉", "手話"),
                new ContentKindInfo(0x0B050000, "福祉", "文字（字幕）"),
                new ContentKindInfo(0x0B060000, "福祉", "音声解説"),
                new ContentKindInfo(0x0B0F0000, "福祉", "その他"),

                new ContentKindInfo(0x0E0100FF, "スポーツ(CS)", ""),
                new ContentKindInfo(0x0E010000, "スポーツ(CS)", "テニス"),
                new ContentKindInfo(0x0E010001, "スポーツ(CS)", "バスケットボール"),
                new ContentKindInfo(0x0E010002, "スポーツ(CS)", "ラグビー"),
                new ContentKindInfo(0x0E010003, "スポーツ(CS)", "アメリカンフットボール"),
                new ContentKindInfo(0x0E010004, "スポーツ(CS)", "ボクシング"),
                new ContentKindInfo(0x0E010005, "スポーツ(CS)", "プロレス"),
                new ContentKindInfo(0x0E01000F, "スポーツ(CS)", "その他"),

                new ContentKindInfo(0x0E0101FF, "洋画(CS)", ""),
                new ContentKindInfo(0x0E010100, "洋画(CS)", "アクション"),
                new ContentKindInfo(0x0E010101, "洋画(CS)", "SF／ファンタジー"),
                new ContentKindInfo(0x0E010102, "洋画(CS)", "コメディー"),
                new ContentKindInfo(0x0E010103, "洋画(CS)", "サスペンス／ミステリー"),
                new ContentKindInfo(0x0E010104, "洋画(CS)", "恋愛／ロマンス"),
                new ContentKindInfo(0x0E010105, "洋画(CS)", "ホラー／スリラー"),
                new ContentKindInfo(0x0E010106, "洋画(CS)", "ウエスタン"),
                new ContentKindInfo(0x0E010107, "洋画(CS)", "ドラマ／社会派ドラマ"),
                new ContentKindInfo(0x0E010108, "洋画(CS)", "アニメーション"),
                new ContentKindInfo(0x0E010109, "洋画(CS)", "ドキュメンタリー"),
                new ContentKindInfo(0x0E01010A, "洋画(CS)", "アドベンチャー／冒険"),
                new ContentKindInfo(0x0E01010B, "洋画(CS)", "ミュージカル／音楽映画"),
                new ContentKindInfo(0x0E01010C, "洋画(CS)", "ホームドラマ"),
                new ContentKindInfo(0x0E01010F, "洋画(CS)", "その他"),

                new ContentKindInfo(0x0E0102FF, "邦画(CS)", ""),
                new ContentKindInfo(0x0E010200, "邦画(CS)", "アクション"),
                new ContentKindInfo(0x0E010201, "邦画(CS)", "SF／ファンタジー"),
                new ContentKindInfo(0x0E010202, "邦画(CS)", "お笑い／コメディー"),
                new ContentKindInfo(0x0E010203, "邦画(CS)", "サスペンス／ミステリー"),
                new ContentKindInfo(0x0E010204, "邦画(CS)", "恋愛／ロマンス"),
                new ContentKindInfo(0x0E010205, "邦画(CS)", "ホラー／スリラー"),
                new ContentKindInfo(0x0E010206, "邦画(CS)", "青春／学園／アイドル"),
                new ContentKindInfo(0x0E010207, "邦画(CS)", "任侠／時代劇"),
                new ContentKindInfo(0x0E010208, "邦画(CS)", "アニメーション"),
                new ContentKindInfo(0x0E010209, "邦画(CS)", "ドキュメンタリー"),
                new ContentKindInfo(0x0E01020A, "邦画(CS)", "アドベンチャー／冒険"),
                new ContentKindInfo(0x0E01020B, "邦画(CS)", "ミュージカル／音楽映画"),
                new ContentKindInfo(0x0E01020C, "邦画(CS)", "ホームドラマ"),
                new ContentKindInfo(0x0E01020F, "邦画(CS)", "その他"),

                new ContentKindInfo(0x0E0103FF, "アダルト(CS)", ""),
                new ContentKindInfo(0x0E010300, "アダルト(CS)", "アダルト"),
                new ContentKindInfo(0x0E01030F, "アダルト(CS)", "その他"),

                new ContentKindInfo(0x0FFF0000, "その他", ""),
                new ContentKindInfo(0x0F0F0000, "その他", "その他"),

                new ContentKindInfo(0xFFFF0000, "ジャンル情報なし", ""),

                new ContentKindInfo(0x0E0000FF, "編成情報", "編成に関する情報あり"),
                new ContentKindInfo(0x0E000000, "編成情報", "中止の可能性あり"),
                new ContentKindInfo(0x0E000001, "編成情報", "延長の可能性あり"),
                new ContentKindInfo(0x0E000002, "編成情報", "中断の可能性あり"),
                new ContentKindInfo(0x0E000003, "編成情報", "同一シリーズの別話数放送の可能性あり"),
                new ContentKindInfo(0x0E000004, "編成情報", "編成未定"),
                new ContentKindInfo(0x0E00000F, "編成情報", "その他の編成情報"),

                new ContentKindInfo(0x0E0001FF, "中断情報", "中断に関する情報あり"),
                new ContentKindInfo(0x0E000100, "中断情報", "中断ニュースあり"),
                new ContentKindInfo(0x0E000101, "中断情報", "臨時サービスあり"),
                new ContentKindInfo(0x0E00010F, "中断情報", "その他の中断情報"),

                new ContentKindInfo(0x0E0002FF, "3D映像", "3D映像に関する情報あり"),
                new ContentKindInfo(0x0E000200, "3D映像", "3D映像あり"),
                new ContentKindInfo(0x0E00020F, "3D映像", "その他の3D映像情報"),

                new ContentKindInfo(0x0E000FFF, "その他付属情報", "その他付属情報あり"),
                new ContentKindInfo(0x0E000F0F, "その他付属情報", "その他"),

                new ContentKindInfo(0xFEFF0000, "不明な情報(番組表でのみ有効)", ""),
            }.ToDictionary(info => info.Data.Key, info => info);

            ComponentKindDictionary = new Dictionary<UInt16, string>()
            {
                { 0x0101, "480i(525i)、アスペクト比4:3" },
                { 0x0102, "480i(525i)、アスペクト比16:9 パンベクトルあり" },
                { 0x0103, "480i(525i)、アスペクト比16:9 パンベクトルなし" },
                { 0x0104, "480i(525i)、アスペクト比 > 16:9" },
                { 0x0191, "2160p、アスペクト比4:3" },
                { 0x0192, "2160p、アスペクト比16:9 パンベクトルあり" },
                { 0x0193, "2160p、アスペクト比16:9 パンベクトルなし" },
                { 0x0194, "2160p、アスペクト比 > 16:9" },
                { 0x01A1, "480p(525p)、アスペクト比4:3" },
                { 0x01A2, "480p(525p)、アスペクト比16:9 パンベクトルあり" },
                { 0x01A3, "480p(525p)、アスペクト比16:9 パンベクトルなし" },
                { 0x01A4, "480p(525p)、アスペクト比 > 16:9" },
                { 0x01B1, "1080i(1125i)、アスペクト比4:3" },
                { 0x01B2, "1080i(1125i)、アスペクト比16:9 パンベクトルあり" },
                { 0x01B3, "1080i(1125i)、アスペクト比16:9 パンベクトルなし" },
                { 0x01B4, "1080i(1125i)、アスペクト比 > 16:9" },
                { 0x01C1, "720p(750p)、アスペクト比4:3" },
                { 0x01C2, "720p(750p)、アスペクト比16:9 パンベクトルあり" },
                { 0x01C3, "720p(750p)、アスペクト比16:9 パンベクトルなし" },
                { 0x01C4, "720p(750p)、アスペクト比 > 16:9" },
                { 0x01D1, "240p アスペクト比4:3" },
                { 0x01D2, "240p アスペクト比16:9 パンベクトルあり" },
                { 0x01D3, "240p アスペクト比16:9 パンベクトルなし" },
                { 0x01D4, "240p アスペクト比 > 16:9" },
                { 0x01E1, "1080p(1125p)、アスペクト比4:3" },
                { 0x01E2, "1080p(1125p)、アスペクト比16:9 パンベクトルあり" },
                { 0x01E3, "1080p(1125p)、アスペクト比16:9 パンベクトルなし" },
                { 0x01E4, "1080p(1125p)、アスペクト比 > 16:9" },
                { 0x0201, "1/0モード（シングルモノ）" },
                { 0x0202, "1/0＋1/0モード（デュアルモノ）" },
                { 0x0203, "2/0モード（ステレオ）" },
                { 0x0204, "2/1モード" },
                { 0x0205, "3/0モード" },
                { 0x0206, "2/2モード" },
                { 0x0207, "3/1モード" },
                { 0x0208, "3/2モード" },
                { 0x0209, "3/2＋LFEモード（3/2.1モード）" },
                { 0x020A, "3/3.1モード" },
                { 0x020B, "2/0/0-2/0/2-0.1モード" },
                { 0x020C, "5/2.1モード" },
                { 0x020D, "3/2/2.1モード" },
                { 0x020E, "2/0/0-3/0/2-0.1モード" },
                { 0x020F, "0/2/0-3/0/2-0.1モード" },
                { 0x0210, "2/0/0-3/2/3-0.2モード" },
                { 0x0211, "3/3/3-5/2/3-3/0/0.2モード" },
                { 0x0240, "視覚障害者用音声解説" },
                { 0x0241, "聴覚障害者用音声" },
                { 0x0501, "H.264|MPEG-4 AVC、480i(525i)、アスペクト比4:3" },
                { 0x0502, "H.264|MPEG-4 AVC、480i(525i)、アスペクト比16:9 パンベクトルあり" },
                { 0x0503, "H.264|MPEG-4 AVC、480i(525i)、アスペクト比16:9 パンベクトルなし" },
                { 0x0504, "H.264|MPEG-4 AVC、480i(525i)、アスペクト比 > 16:9" },
                { 0x0591, "H.264|MPEG-4 AVC、2160p、アスペクト比4:3" },
                { 0x0592, "H.264|MPEG-4 AVC、2160p、アスペクト比16:9 パンベクトルあり" },
                { 0x0593, "H.264|MPEG-4 AVC、2160p、アスペクト比16:9 パンベクトルなし" },
                { 0x0594, "H.264|MPEG-4 AVC、2160p、アスペクト比 > 16:9" },
                { 0x05A1, "H.264|MPEG-4 AVC、480p(525p)、アスペクト比4:3" },
                { 0x05A2, "H.264|MPEG-4 AVC、480p(525p)、アスペクト比16:9 パンベクトルあり" },
                { 0x05A3, "H.264|MPEG-4 AVC、480p(525p)、アスペクト比16:9 パンベクトルなし" },
                { 0x05A4, "H.264|MPEG-4 AVC、480p(525p)、アスペクト比 > 16:9" },
                { 0x05B1, "H.264|MPEG-4 AVC、1080i(1125i)、アスペクト比4:3" },
                { 0x05B2, "H.264|MPEG-4 AVC、1080i(1125i)、アスペクト比16:9 パンベクトルあり" },
                { 0x05B3, "H.264|MPEG-4 AVC、1080i(1125i)、アスペクト比16:9 パンベクトルなし" },
                { 0x05B4, "H.264|MPEG-4 AVC、1080i(1125i)、アスペクト比 > 16:9" },
                { 0x05C1, "H.264|MPEG-4 AVC、720p(750p)、アスペクト比4:3" },
                { 0x05C2, "H.264|MPEG-4 AVC、720p(750p)、アスペクト比16:9 パンベクトルあり" },
                { 0x05C3, "H.264|MPEG-4 AVC、720p(750p)、アスペクト比16:9 パンベクトルなし" },
                { 0x05C4, "H.264|MPEG-4 AVC、720p(750p)、アスペクト比 > 16:9" },
                { 0x05D1, "H.264|MPEG-4 AVC、240p アスペクト比4:3" },
                { 0x05D2, "H.264|MPEG-4 AVC、240p アスペクト比16:9 パンベクトルあり" },
                { 0x05D3, "H.264|MPEG-4 AVC、240p アスペクト比16:9 パンベクトルなし" },
                { 0x05D4, "H.264|MPEG-4 AVC、240p アスペクト比 > 16:9" },
                { 0x05E1, "H.264|MPEG-4 AVC、1080p(1125p)、アスペクト比4:3" },
                { 0x05E2, "H.264|MPEG-4 AVC、1080p(1125p)、アスペクト比16:9 パンベクトルあり" },
                { 0x05E3, "H.264|MPEG-4 AVC、1080p(1125p)、アスペクト比16:9 パンベクトルなし" },
                { 0x05E4, "H.264|MPEG-4 AVC、1080p(1125p)、アスペクト比 > 16:9" }
            };
            DayOfWeekArray = new string[] { "日", "月", "火", "水", "木", "金", "土" };
            RecModeList = new string[] { "全サービス", "指定サービス", "全サービス(デコード処理なし)", "指定サービス(デコード処理なし)", "視聴", "無効" };
            YesNoList = new string[] { "しない", "する" };
            PriorityList = new string[] { "1 (低)", "2", "3", "4", "5 (高)" };
        }

        public static IEnumerable<ContentKindInfo> ContentKindList
        {
            get
            {
                //「その他」をラストへ。各々大分類を前へ
                // (Valueの順序は未定義だが、現在の実装的には削除・追加しなければ大丈夫らしい。でも並べ替えておく)
                return ContentKindDictionary.Values.OrderBy(info => (info.Data.Nibble2 + 1) & 0xFF | (info.Data.Nibble1 << 8) |
                    ((info.Data.content_nibble_level_1 == 0xFE ? 0x7000 : info.Data.IsAttributeInfo ? 0x6000 : info.Data.content_nibble_level_1 == 0xFF ? 0x5000 : info.Data.content_nibble_level_1 == 0x0F ? 0x1000 : info.Data.IsUserNibble ? info.Data.content_nibble_level_2 : 0) << 16));
            }
        }

        public static IEnumerable<int> CustomHourList
        {
            get { return Enumerable.Range(0, Settings.Instance.LaterTimeUse == false ? 24 : 37); }
        }

        public bool IsConnected { get { return NWMode == false || NW.IsConnected == true; } }

        //ChKeyを16ビットに圧縮し、EpgTimerの起動中保持し続ける。
        private static Dictionary<UInt64, UInt16> chKey64to16Dic = new Dictionary<UInt64, UInt16>();
        public static UInt16 Create16Key(UInt64 key64)
        {
            UInt16 Key16;
            if (chKey64to16Dic.TryGetValue(key64, out Key16) == false)
            {
                Key16 = (UInt16)chKey64to16Dic.Count;
                chKey64to16Dic.Add(key64, Key16);
            }
            return Key16;
        }
        public static UInt64 Create64Key(UInt16 ONID, UInt16 TSID, UInt16 SID)
        {
            return ((UInt64)ONID) << 32 | ((UInt64)TSID) << 16 | (UInt64)SID;
        }
        public static UInt64 Create64PgKey(UInt16 ONID, UInt16 TSID, UInt16 SID, UInt16 EventID)
        {
            return ((UInt64)ONID) << 48 | ((UInt64)TSID) << 32 | ((UInt64)SID) << 16 | (UInt64)EventID;
        }

        public static String Convert64PGKeyString(UInt64 Key)
        {
            return Convert64KeyString(Key >> 16) + "\r\n"
                + ConvertEpgIDString("EventID", Key);
        }
        public static String Convert64KeyString(UInt64 Key)
        {
            return ConvertEpgIDString("OriginalNetworkID", Key >> 32) + "\r\n" +
            ConvertEpgIDString("TransportStreamID", Key >> 16) + "\r\n" +
            ConvertEpgIDString("ServiceID", Key);
        }
        private static String ConvertEpgIDString(String Title, UInt64 id)
        {
            return string.Format("{0} : {1} (0x{1:X4})", Title, 0x000000000000FFFF & id);
        }

        public static string AdjustSearchText(string s)
        {
            return ReplaceUrl(s.ToLower()).Replace("　", " ").Replace("\r\n", "");
        }
        public static String ReplaceUrl(String url)
        {
            string retText = url;

            retText = retText.Replace("ａ", "a");
            retText = retText.Replace("ｂ", "b");
            retText = retText.Replace("ｃ", "c");
            retText = retText.Replace("ｄ", "d");
            retText = retText.Replace("ｅ", "e");
            retText = retText.Replace("ｆ", "f");
            retText = retText.Replace("ｇ", "g");
            retText = retText.Replace("ｈ", "h");
            retText = retText.Replace("ｉ", "i");
            retText = retText.Replace("ｊ", "j");
            retText = retText.Replace("ｋ", "k");
            retText = retText.Replace("ｌ", "l");
            retText = retText.Replace("ｍ", "m");
            retText = retText.Replace("ｎ", "n");
            retText = retText.Replace("ｏ", "o");
            retText = retText.Replace("ｐ", "p");
            retText = retText.Replace("ｑ", "q");
            retText = retText.Replace("ｒ", "r");
            retText = retText.Replace("ｓ", "s");
            retText = retText.Replace("ｔ", "t");
            retText = retText.Replace("ｕ", "u");
            retText = retText.Replace("ｖ", "v");
            retText = retText.Replace("ｗ", "w");
            retText = retText.Replace("ｘ", "x");
            retText = retText.Replace("ｙ", "y");
            retText = retText.Replace("ｚ", "z");
            retText = retText.Replace("Ａ", "A");
            retText = retText.Replace("Ｂ", "B");
            retText = retText.Replace("Ｃ", "C");
            retText = retText.Replace("Ｄ", "D");
            retText = retText.Replace("Ｅ", "E");
            retText = retText.Replace("Ｆ", "F");
            retText = retText.Replace("Ｇ", "G");
            retText = retText.Replace("Ｈ", "H");
            retText = retText.Replace("Ｉ", "I");
            retText = retText.Replace("Ｊ", "J");
            retText = retText.Replace("Ｋ", "K");
            retText = retText.Replace("Ｌ", "L");
            retText = retText.Replace("Ｍ", "M");
            retText = retText.Replace("Ｎ", "N");
            retText = retText.Replace("Ｏ", "O");
            retText = retText.Replace("Ｐ", "P");
            retText = retText.Replace("Ｑ", "Q");
            retText = retText.Replace("Ｒ", "R");
            retText = retText.Replace("Ｓ", "S");
            retText = retText.Replace("Ｔ", "T");
            retText = retText.Replace("Ｕ", "U");
            retText = retText.Replace("Ｖ", "V");
            retText = retText.Replace("Ｗ", "W");
            retText = retText.Replace("Ｘ", "X");
            retText = retText.Replace("Ｙ", "Y");
            retText = retText.Replace("Ｚ", "Z");
            retText = retText.Replace("＃", "#");
            retText = retText.Replace("＄", "$");
            retText = retText.Replace("％", "%");
            retText = retText.Replace("＆", "&");
            retText = retText.Replace("’", "'");
            retText = retText.Replace("（", "(");
            retText = retText.Replace("）", ")");
            retText = retText.Replace("～", "~");
            retText = retText.Replace("＝", "=");
            retText = retText.Replace("｜", "|");
            retText = retText.Replace("＾", "^");
            retText = retText.Replace("￥", "\\");
            retText = retText.Replace("＠", "@");
            retText = retText.Replace("；", ";");
            retText = retText.Replace("：", ":");
            retText = retText.Replace("｀", "`");
            retText = retText.Replace("｛", "{");
            retText = retText.Replace("｝", "}");
            retText = retText.Replace("＜", "<");
            retText = retText.Replace("＞", ">");
            retText = retText.Replace("？", "?");
            retText = retText.Replace("！", "!");
            retText = retText.Replace("＿", "_");
            retText = retText.Replace("＋", "+");
            retText = retText.Replace("－", "-");
            retText = retText.Replace("＊", "*");
            retText = retText.Replace("／", "/");
            retText = retText.Replace("．", ".");
            retText = retText.Replace("０", "0");
            retText = retText.Replace("１", "1");
            retText = retText.Replace("２", "2");
            retText = retText.Replace("３", "3");
            retText = retText.Replace("４", "4");
            retText = retText.Replace("５", "5");
            retText = retText.Replace("６", "6");
            retText = retText.Replace("７", "7");
            retText = retText.Replace("８", "8");
            retText = retText.Replace("９", "9");

            return retText;
        }

        /// <summary>良くある通信エラー(CMD_ERR_CONNECT,CMD_ERR_TIMEOUT)をMessageBoxで表示する。</summary>
        public static bool CmdErrMsgTypical(ErrCode err, string caption = "通信エラー")
        {
            if (err == ErrCode.CMD_SUCCESS) return true;

            string msg;
            switch (err)
            {
                case ErrCode.CMD_ERR_CONNECT:
                    msg="サーバー または EpgTimerSrv に接続できませんでした。";
                    break;
                //case ErrCode.CMD_ERR_BUSY:  //もう表示しないことにしているようだ。
                //    msg = "データの読み込みを行える状態ではありません。\r\n（EPGデータ読み込み中。など）";
                //    break;
                case ErrCode.CMD_ERR_TIMEOUT:
                    msg = "EpgTimerSrvとの接続にタイムアウトしました。";
                    break;
                default:
                    msg = "通信エラーが発生しました。";
                    break;
            }
            CommonUtil.DispatcherMsgBoxShow(msg, caption);
            return false;
        }

        public static String ConvertTimeText(EpgEventInfo info)
        {
            if (info.StartTimeFlag == 0) return "未定 ～ 未定";
            //
            string reftxt = ConvertTimeText(info.start_time, info.PgDurationSecond, false, false, false, false);
            return info.DurationFlag != 0 ? reftxt : reftxt.Split(new char[] { '～' })[0] + "～ 未定";
        }
        public static String ConvertTimeText(EpgSearchDateInfo info)
        {
            //超手抜き。書式が変ったら、巻き込まれて死ぬ。
            var start = new DateTime(2000, 1, 2 + info.startDayOfWeek, info.startHour, info.startMin, 0);
            var end = new DateTime(2000, 1, 2 + info.endDayOfWeek, info.endHour, info.endMin, 0);
            if (end < start) end = end.AddDays(7);
            string reftxt = ConvertTimeText(start, (uint)(end - start).TotalSeconds, true, true, false, false);
            string[] src = reftxt.Split(new char[] { ' ', '～' });
            return src[0].Substring(6, 1) + " " + src[1] + " ～ " + src[2].Substring(6, 1) + " " + src[3];
        }
        public static String ConvertTimeText(DateTime start, uint duration, bool isNoYear, bool isNoSecond, bool isNoEndDay = true, bool isNoStartDay = false)
        {
            DateTime end = start + TimeSpan.FromSeconds(duration);

            if (Settings.Instance.LaterTimeUse == true)
            {
                bool over1Day = duration >= 24 * 60 * 60;
                bool? isStartLate = (isNoEndDay == false && over1Day == true) ? (bool?)false : null;
                bool isEndLate = (isNoEndDay == false || isNoStartDay == true && over1Day == false
                    ? over1Day == false && DateTime28.JudgeLateHour(end, start)
                    : DateTime28.JudgeLateHour(end, start, 99));
                DateTime28 ref_start = (isEndLate == true && isNoEndDay == true && isNoStartDay == false) ? new DateTime28(start) : null;

                return ConvertTimeText(start, isNoYear, isNoSecond, isNoStartDay, isStartLate)
                    + (isNoSecond == true ? "～" : " ～ ")
                    + ConvertTimeText(end, isNoYear, isNoSecond, isNoEndDay, isEndLate, ref_start);
            }
            else
            {
                return ConvertTimeText(start, isNoYear, isNoSecond, isNoStartDay)
                + (isNoSecond == true ? "～" : " ～ ")
                + ConvertTimeText(end, isNoYear, isNoSecond, isNoEndDay);
            }
        }
        public static String ConvertTimeText(DateTime time, bool isNoYear, bool isNoSecond, bool isNoDay = false, bool? isUse28 = null, DateTime28 ref_start = null)
        {
            if (Settings.Instance.LaterTimeUse == true)
            {
                var time28 = new DateTime28(time, isUse28, ref_start);
                return (isNoDay == true ? "" : time28.DateTimeMod.ToString((isNoYear == true ? "MM/dd(ddd) " : "yyyy/MM/dd(ddd) ")))
                + time28.HourMod.ToString("00:") + time.ToString(isNoSecond == true ? "mm" : "mm:ss");
            }
            else
            {
                return time.ToString((isNoDay == true ? "" :
                (isNoYear == true ? "MM/dd(ddd) " : "yyyy/MM/dd(ddd) ")) + (isNoSecond == true ? "HH:mm" : "HH:mm:ss"));
            }
        }
        public static String ConvertDurationText(uint duration, bool isNoSecond)
        {
            return (duration / 3600).ToString() 
                + ((duration % 3600) / 60).ToString(":00") 
                + (isNoSecond == true ? "" : (duration % 60).ToString(":00"));
        }

        public static String ConvertResModeText(ReserveMode? mode)
        {
            switch (mode)
            {
                case ReserveMode.KeywordAuto: return "キーワード予約";
                case ReserveMode.ManualAuto : return "プログラム自動予約";
                case ReserveMode.EPG        : return "個別予約(EPG)";
                case ReserveMode.Program    : return "個別予約(プログラム)";
                default                     : return "";
            }
        }

        public static String ConvertProgramText(EpgEventInfo eventInfo, EventInfoTextMode textMode)
        {
            if (eventInfo == null) return "";

            string retText = "";

            UInt64 key = eventInfo.Create64Key();
            if (ChSet5.ChList.ContainsKey(key) == true)
            {
                retText += ChSet5.ChList[key].ServiceName + "(" + ChSet5.ChList[key].NetworkName + ")" + "\r\n";
            }

            retText += ConvertTimeText(eventInfo) + "\r\n";

            string extText = "";
            if (eventInfo.ShortInfo != null)
            {
                retText += eventInfo.ShortInfo.event_name + "\r\n\r\n";
                extText = eventInfo.ShortInfo.text_char + "\r\n\r\n";
            }

            //基本情報
            if (textMode == EventInfoTextMode.BasicOnly)
            {
                return retText.TrimEnd('\r', '\n');
            }

            retText += extText;

            //基本情報+説明(番組表のツールチップなど)
            if (textMode == EventInfoTextMode.BasicText)
            {
                return retText.TrimEnd('\r', '\n');
            }

            if (eventInfo.ExtInfo != null)
            {
                retText += eventInfo.ExtInfo.text_char + "\r\n\r\n";
            }

            //テキスト情報全て(番組表のツールチップなど)
            if (textMode == EventInfoTextMode.TextAll)
            {
                return retText.TrimEnd('\r', '\n');
            }

            //ジャンル
            retText += "ジャンル :\r\n";
            var contentList = new List<ContentKindInfo>();
            if (eventInfo.ContentInfo != null)
            {
                contentList = eventInfo.ContentInfo.nibbleList.Select(data => ContentKindInfoForDisplay(data)).ToList();
            }
            foreach (ContentKindInfo info in contentList.Where(info => info.Data.IsAttributeInfo == false))
            {
                retText += info.ListBoxView + "\r\n";
            }
            retText += "\r\n";

            //映像
            retText += "映像 :";
            if (eventInfo.ComponentInfo != null)
            {
                int streamContent = eventInfo.ComponentInfo.stream_content;
                int componentType = eventInfo.ComponentInfo.component_type;
                UInt16 componentKey = (UInt16)(streamContent << 8 | componentType);
                if (ComponentKindDictionary.ContainsKey(componentKey) == true)
                {
                    retText += ComponentKindDictionary[componentKey];
                }
                if (eventInfo.ComponentInfo.text_char.Length > 0)
                {
                    retText += "\r\n";
                    retText += eventInfo.ComponentInfo.text_char;
                }
            }
            retText += "\r\n";

            //音声
            retText += "音声 :\r\n";
            if (eventInfo.AudioInfo != null)
            {
                foreach (EpgAudioComponentInfoData info in eventInfo.AudioInfo.componentList)
                {
                    int streamContent = info.stream_content;
                    int componentType = info.component_type;
                    UInt16 componentKey = (UInt16)(streamContent << 8 | componentType);
                    if (ComponentKindDictionary.ContainsKey(componentKey) == true)
                    {
                        retText += ComponentKindDictionary[componentKey];
                    }
                    if (info.text_char.Length > 0)
                    {
                        retText += "\r\n";
                        retText += info.text_char;
                    }
                    retText += "\r\n";
                    retText += "サンプリングレート :";
                    switch (info.sampling_rate)
                    {
                        case 1:
                            retText += "16kHz";
                            break;
                        case 2:
                            retText += "22.05kHz";
                            break;
                        case 3:
                            retText += "24kHz";
                            break;
                        case 5:
                            retText += "32kHz";
                            break;
                        case 6:
                            retText += "44.1kHz";
                            break;
                        case 7:
                            retText += "48kHz";
                            break;
                        default:
                            break;
                    }
                    retText += "\r\n";
                }
            }
            retText += "\r\n";

            //スクランブル
            if (!ChSet5.IsDttv(eventInfo.original_network_id))
            {
                retText += (eventInfo.FreeCAFlag == 0 ? "無料放送" : "有料放送") + "\r\n\r\n";
            }

            //イベントリレー
            if (eventInfo.EventRelayInfo != null)
            {
                if (eventInfo.EventRelayInfo.eventDataList.Count > 0)
                {
                    retText += "イベントリレーあり：\r\n";
                    foreach (EpgEventData info in eventInfo.EventRelayInfo.eventDataList)
                    {
                        retText += "→ ";
                        ChSet5Item chInfo;
                        if (ChSet5.ChList.TryGetValue(info.Create64Key(), out chInfo) == true)
                        {
                            retText += chInfo.ServiceName + "(" + chInfo.NetworkName + ")" + " ";
                        }
                        else
                        {
                            retText += ConvertEpgIDString("OriginalNetworkID", info.original_network_id) + " ";
                            retText += ConvertEpgIDString("TransportStreamID", info.transport_stream_id) + " ";
                            retText += ConvertEpgIDString("ServiceID", info.service_id) + " ";
                        }
                        var relayInfo = MenuUtil.SearchEventInfo(info.Create64PgKey());//過去番組は見つからないこともあるが構わない
                        retText += ConvertEpgIDString(" EventID", info.event_id) + " " + (relayInfo == null ? "" : relayInfo.DataTitle) + "\r\n";
                    }
                    retText += "\r\n";
                }
            }

            //その他情報(番組特性コード関係)
            var attStr = string.Join("\r\n", contentList.Where(info => info.Data.IsAttributeInfo == true).Select(info => info.SubName));
            if (attStr != "")
            {
                retText += attStr + "\r\n\r\n";
            }

            retText += Convert64PGKeyString(eventInfo.Create64PgKey()) + "\r\n";

            return retText;
        }

        //主にジャンル項目の設定リスト表示用
        public static ContentKindInfo ContentKindInfoForDisplay(EpgContentData data)
        {
            ContentKindInfo info;
            if (ContentKindDictionary.TryGetValue(data.Key, out info) == true)
            { return info; }

            //不明なカテゴリの表示用ContentKindInfo作成
            string idString = data.IsUserNibble ? string.Format("(0x{0:X8})", data.Key) : string.Format("(0x{0:X4})", data.Key >> 16);
            info = new ContentKindInfo(data.Key, "不明" + (data.IsCategory ? idString : ""), "不明" + (data.IsCategory ? "" : idString));

            if (ContentKindDictionary.ContainsKey(data.CategoryKey) == true)
            {
                info.ContentName = ContentKindDictionary[data.CategoryKey].ContentName;
            }
            return info;
        }

        //主にリストビューの表示用
        public static String ConvertJyanruText(EpgEventInfo eventInfo)
        {
            if (eventInfo == null || eventInfo.ContentInfo == null) return "";
            //
            return ConvertJyanruText(eventInfo.ContentInfo.nibbleList, true);
        }
        public static String ConvertJyanruText(CustomEpgTabInfo info)
        {
            if (info == null) return "";
            //
            string retText = ConvertJyanruText(info.ViewContentList);
            return ((retText != "" && info.ViewNotContentFlag == true) ? "NOT " : "") + retText;
        }
        public static String ConvertJyanruText(EpgSearchKeyInfo searchKeyInfo)
        {
            if (searchKeyInfo == null) return "";
            //
            string retText = ConvertJyanruText(searchKeyInfo.contentList);
            return ((retText != "" && searchKeyInfo.notContetFlag == 1) ? "NOT " : "") + retText;
        }
        public static String ConvertJyanruText(IEnumerable<EpgContentData> nibbleList, bool noAttribute = false)
        {
            if (nibbleList == null) return "";
            //
            var retText = new List<string>();

            var infoList = nibbleList.Where(info => noAttribute == false || info.IsAttributeInfo == false).ToList();
            var knownList = infoList.Where(info => ContentKindDictionary.ContainsKey(info.Key) == true).ToList();
            foreach (var gr in knownList.Select(info => ContentKindDictionary[info.Key]).GroupBy(info => info.Data.CategoryKey))
            {
                var smallCategory1 = string.Join(",", gr.Where(info => info.Data.IsCategory == false)
                                                            .Select(info => info.SubName).Distinct());
                retText.Add("[" + gr.First().ContentName + (smallCategory1 == "" ? "" : (" - " + smallCategory1)) + "]");
            }
            if (infoList.Count != knownList.Count) retText.Add("[不明]");

            return string.Join(",", retText);
        }

        //主にリストビューの表示用
        public static String ConvertAttribText(EpgEventInfo eventInfo)
        {
            if (eventInfo == null || eventInfo.ContentInfo == null) return "";
            //
            var retText = new List<string>();

            if (eventInfo.EventRelayInfo != null && eventInfo.EventRelayInfo.eventDataList.Count != 0)
            {
                retText.Add("[イベントリレー]");
            }

            var infoList = eventInfo.ContentInfo.nibbleList.Where(info => info.IsAttributeInfo == true).ToList();
            var knownList = infoList.Where(info => ContentKindDictionary.ContainsKey(info.Key) == true).ToList();
            foreach (var gr in knownList.GroupBy(info => info.CategoryKey))
            {
                retText.Add("[" + ContentKindDictionary[gr.Key].ContentName + "]");
            }
            if (infoList.Count != knownList.Count) retText.Add("[不明な情報]");

            return string.Join(",", retText);
        }

        public static String ConvertNetworkNameText(ushort originalNetworkID, bool IsSimple = false)
        {
            String retText = "";
            if (ChSet5.IsDttv(originalNetworkID) == true)
            {
                retText = "地デジ";
            }
            else if (ChSet5.IsBS(originalNetworkID) == true)
            {
                retText = "BS";
            }
            else if (ChSet5.IsCS1(originalNetworkID) == true)
            {
                retText = IsSimple == true ? "CS" : "CS1";
            }
            else if (ChSet5.IsCS2(originalNetworkID) == true)
            {
                retText = IsSimple == true ? "CS" : "CS2";
            }
            else if (ChSet5.IsCS3(originalNetworkID) == true)
            {
                retText = IsSimple == true ? "CS" : "CS3";
            }
            else
            {
                retText = "その他";
            }
            return retText;
        }

        static String ConvertValueText(int val, string[] textList, string errText = "不明")
        {
            return 0 <= val && val < textList.Length ? textList[val] : errText;
        }

        public static String ConvertRecModeText(int val)
        {
            return ConvertValueText(val, RecModeList);
        }

        public static String ConvertYesNoText(int val)
        {
            return ConvertValueText(val, YesNoList);
        }

        public static String ConvertPriorityText(int val)
        {
            return ConvertValueText(val - 1, PriorityList);
        }

        public static String ConvertTunerText(uint tunerID)
        {
            string tunerName = "";
            TunerReserveInfo info;
            if (Instance.DB.TunerReserveList.TryGetValue(tunerID, out info))
            {
                tunerName = info.tunerName;
            }
            else if (tunerID != 0)
            {
                tunerName = "不明なチューナー";
            }
            return new TunerSelectInfo(tunerName, tunerID).ToString();
        }

        public static String ConvertViewModeText(int viewMode)
        {
            return ConvertValueText(viewMode, new string[] { "標準モード", "1週間モード", "リスト表示モード" }, "");
        }

        public static FlowDocument ConvertDisplayText(EpgEventInfo eventInfo)
        {
            String epgText = ConvertProgramText(eventInfo, EventInfoTextMode.All);
            if (epgText == "") epgText = "番組情報がありません。\r\n" + "またはEPGデータが読み込まれていません。";
            String text = epgText;
            FlowDocument flowDoc = new FlowDocument();
            Regex regex = new Regex("((http://|https://|ｈｔｔｐ：／／|ｈｔｔｐｓ：／／).*\r\n)");

            if (regex.IsMatch(text) == true)
            {
                try
                {
                    //Regexのsplitでやるとhttp://だけのも取れたりするので、１つずつ行う
                    Paragraph para = new Paragraph();

                    do
                    {
                        Match matchVal = regex.Match(text);
                        int index = text.IndexOf(matchVal.Value);

                        para.Inlines.Add(text.Substring(0, index));
                        text = text.Remove(0, index);

                        Hyperlink h = new Hyperlink(new Run(matchVal.Value.Replace("\r\n", "")));
                        h.MouseLeftButtonDown += new MouseButtonEventHandler(h_MouseLeftButtonDown);
                        h.Foreground = Brushes.Blue;
                        h.Cursor = Cursors.Hand;
                        String url = CommonManager.ReplaceUrl(matchVal.Value.Replace("\r\n", ""));
                        h.NavigateUri = new Uri(url);
                        para.Inlines.Add(h);
                        para.Inlines.Add("\r\n");

                        text = text.Remove(0, matchVal.Value.Length);
                    } while (regex.IsMatch(text) == true);
                    para.Inlines.Add(text);

                    flowDoc.Blocks.Add(para);
                }
                catch
                {
                    flowDoc = new FlowDocument(new Paragraph(new Run(epgText)));
                }
            }
            else
            {
                flowDoc.Blocks.Add(new Paragraph(new Run(epgText)));
            }

            return flowDoc;
        }

        //デフォルト番組表の情報作成
        public List<CustomEpgTabInfo> CreateDefaultTabInfo()
        {
            //再表示の際の認識用に、負の仮番号を与えておく。
            var setInfo = new List<CustomEpgTabInfo>
            {
                new CustomEpgTabInfo(){ID = -1, TabName = "地デジ"},
                new CustomEpgTabInfo(){ID = -2, TabName = "BS"},
                new CustomEpgTabInfo(){ID = -3, TabName = "CS"},
                new CustomEpgTabInfo(){ID = -4, TabName = "その他"},
            };

            foreach (ChSet5Item info in ChSet5.ChListSelected)
            {
                setInfo[info.IsDttv ? 0 : info.IsBS ? 1 : info.IsCS ? 2 : 3].ViewServiceList.Add(info.Key);
            }

            return setInfo.Where(info => info.ViewServiceList.Count != 0).ToList();
        }

        static void h_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Hyperlink)
                {
                    var h = sender as Hyperlink;
                    Process.Start(h.NavigateUri.ToString());
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public static void GetFolderNameByDialog(TextBox txtBox, string Description = "", bool checkNWPath = false)
        {
            GetPathByDialog(txtBox, checkNWPath, path => GetFolderNameByDialog(path, Description));
        }
        public static string GetFolderNameByDialog(string InitialPath = "", string Description = "")
        {
            try
            {
                if (Settings.Instance.OpenFolderWithFileDialog == true)
                {
                    var dlg = new Microsoft.Win32.OpenFileDialog();
                    dlg.Title = Description;
                    dlg.CheckFileExists = false;
                    dlg.DereferenceLinks = false;
                    dlg.FileName = "(任意ファイル名)";
                    dlg.InitialDirectory = GetDirectoryName2(InitialPath);
                    if (dlg.ShowDialog() == true)
                    {
                        return GetDirectoryName2(dlg.FileName);
                    }
                }
                else
                {
                    var dlg = new System.Windows.Forms.FolderBrowserDialog();
                    dlg.Description = Description;
                    dlg.SelectedPath = GetDirectoryName2(InitialPath);
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        return dlg.SelectedPath;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return null;
        }

        public static void GetFileNameByDialog(TextBox txtBox, bool isNameOnly, string Title = "", string DefaultExt = "", bool checkNWPath = false)
        {
            GetPathByDialog(txtBox, checkNWPath, path => GetFileNameByDialog(path, isNameOnly, Title, DefaultExt));
        }
        public static string GetFileNameByDialog(string InitialPath = "", bool isNameOnly = false, string Title = "", string DefaultExt = "")
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Title = Title;
                dlg.FileName = Path.GetFileName(InitialPath);
                dlg.InitialDirectory = GetDirectoryName2(InitialPath);
                switch (DefaultExt)
                {
                    case ".exe":
                        dlg.DefaultExt = ".exe";
                        dlg.Filter = "exe Files|*.exe|all Files|*.*";
                        break;
                    case ".bat":
                        dlg.DefaultExt = ".bat";
                        dlg.Filter = "bat Files|*.bat|all Files|*.*";
                        break;
                    default:
                        dlg.Filter = "all Files|*.*";
                        break;
                }
                if (dlg.ShowDialog() == true)
                {
                    return isNameOnly == true ? dlg.SafeFileName : dlg.FileName;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return null;
        }

        private static string GetDirectoryName2(string folder_path)
        {
            string path = folder_path.Trim();
            while (path != "")
            {
                if (Directory.Exists(path)) break;
                path = Path.GetDirectoryName(path) ?? "";
            }
            return path;
        }

        //ネットワークパス対応のパス設定
        private static void GetPathByDialog(TextBox tbox, bool checkNWPath, Func<string, string> funcGetPathDialog)
        {
            string path = tbox.Text.Trim();

            string base_src = "";
            string base_nw = "";
            if (checkNWPath == true && CommonManager.Instance.NWMode == true && path != "" && path.StartsWith("\\\\") == false)
            {
                //可能ならUNCパスをサーバ側のパスに戻す。
                //複数の共有フォルダ使ってる場合はとりあえず諦める。(サーバ側で要逆変換)
                string path_src = path.TrimEnd('\\');
                string path_nw = GetRecPath(path_src).TrimEnd('\\');

                if (path_nw != "" && path_nw != path_src)
                {
                    IEnumerable<string> r_src = path_src.Split('\\').Reverse();
                    IEnumerable<string> r = path_nw.Split('\\').Reverse();
                    int length_match = -1;
                    foreach (var item in r.Zip(r_src, (p, ps) => new { nw = p, src = ps }))
                    {
                        if (item.nw != item.src) break;
                        length_match += item.nw.Length + 1;
                    }
                    length_match = Math.Max(0, length_match);
                    base_src = path_src.Substring(0, path_src.Length - length_match).TrimEnd('\\');
                    base_nw = path_nw.Substring(0, path_nw.Length - length_match).TrimEnd('\\');
                }
                if (base_nw != "")
                {
                    path = path_nw;
                }
            }

            path = funcGetPathDialog(path);
            if (path != null && tbox.IsEnabled == true && tbox.IsReadOnly == false)
            {
                //他のドライブに変ったりしたときは何もしない
                if (base_nw != "" && path.StartsWith(base_nw) == true)
                {
                    path = path.Replace(base_nw, base_src);
                    if (path.EndsWith(":") == true) path += "\\";//EpgTimerSrvに削除されてしまうが‥
                }
                tbox.Text = path;
            }
        }

        public static String GetRecPath(String path)
        {
            var nwPath = "";
            try
            {
                if (String.IsNullOrWhiteSpace(path) == true) return "";
                if (Instance.NWMode != true) return path;
                Instance.CtrlCmd.SendGetRecFileNetworkPath(path, ref nwPath);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return nwPath;
        }

        public static void OpenFolder(String folderPath, String title = "フォルダを開く")
        {
            try
            {
                String path1 = GetRecPath(folderPath);
                bool isFile = File.Exists(path1) == true;//録画結果から開く場合
                String path = isFile == true ? path1 : GetDirectoryName2(path1);//録画フォルダ未作成への対応
                bool noParent = path.TrimEnd('\\').CompareTo(path1.TrimEnd('\\')) != 0;//フォルダを遡った場合の特例

                if (String.IsNullOrWhiteSpace(path) == true)
                {
                    MessageBox.Show("パスが見つかりません。\r\n\r\n" + folderPath, title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    //オプションに応じて一つ上のフォルダから対象フォルダを選択した状態で開く。
                    String cmd = isFile == true || noParent == false && Settings.Instance.MenuSet.OpenParentFolder == true ? "/select," : "";
                    Process.Start("EXPLORER.EXE", cmd + "\"" + path + "\"");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public void FilePlay(ReserveData data)
        {
            if (data == null || data.RecSetting == null || data.IsEnabled == false) return;
            if (data.IsOnRec() == false)
            {
                MessageBox.Show("まだ録画が開始されていません。", "追っかけ再生", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (data.RecSetting.RecMode == 4)//視聴モード
            {
                TVTestCtrl.SetLiveCh(data.OriginalNetworkID, data.TransportStreamID, data.ServiceID);
                return;
            }

            if (Settings.Instance.FilePlayOnAirWithExe && (NWMode == false || Settings.Instance.FilePlayOnNwWithExe == true))
            {
                //ファイルパスを取得するため開いてすぐ閉じる
                var info = new NWPlayTimeShiftInfo();
                if (CtrlCmd.SendNwTimeShiftOpen(data.ReserveID, ref info) == ErrCode.CMD_SUCCESS)
                {
                    CtrlCmd.SendNwPlayClose(info.ctrlID);
                    if (info.filePath != "")
                    {
                        FilePlay(info.filePath);
                        return;
                    }
                }
                MessageBox.Show("録画ファイルの場所がわかりませんでした。", "追っかけ再生", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                TVTestCtrl.StartTimeShift(data.ReserveID);
            }
        }
        public void FilePlay(String filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) == true) return;

                if (NWMode == true && Settings.Instance.FilePlayOnNwWithExe == false)
                {
                    TVTestCtrl.StartStreamingPlay(filePath, NW.ConnectedIP, NW.ConnectedPort);
                }
                else
                {
                    //録画フォルダと保存・共有フォルダが異なる場合($FileNameExt$運用など)で、
                    //コマンドラインの一部になるときは、ファイルの確認を未チェックとする。
                    String path = GetRecPath(filePath);
                    String cmdLine = Settings.Instance.FilePlayCmd == "" ? "$FilePath$" : Settings.Instance.FilePlayCmd;
                    bool chkExist = cmdLine.Contains("$FilePath$") == true && cmdLine.Contains("$FileNameExt$") == false;

                    String title = "録画ファイルの再生";
                    String msg1 = "録画ファイルが見つかりません。\r\n\r\n" + filePath;
                    String msg2 = "再生アプリが見つかりません。\r\n設定を確認してください。\r\n\r\n" + Settings.Instance.FilePlayExe;

                    if (File.Exists(path) == false)
                    {
                        if (chkExist == true)
                        {
                            MessageBox.Show(msg1, title, MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        path = filePath;
                    }

                    //'$'->'\t'は再帰的な展開を防ぐため
                    cmdLine = cmdLine.Replace("$FileNameExt$", Path.GetFileName(path).Replace('$', '\t'));
                    cmdLine = cmdLine.Replace("$FilePath$", path).Replace('\t', '$');

                    if (Settings.Instance.FilePlayExe.Length == 0)
                    {
                        if (File.Exists(cmdLine) == false)
                        {
                            MessageBox.Show(msg1, title, MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        Process.Start(cmdLine);
                    }
                    else
                    {
                        if (File.Exists(Settings.Instance.FilePlayExe) == false)
                        {
                            MessageBox.Show(msg2, title, MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        Process.Start(Settings.Instance.FilePlayExe, cmdLine);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
        
        //ReloadCustContentColorList()用のコンバートメソッド
        public static Brush _GetColorBrush(string colorName, uint colorValue = 0
            , bool gradation = false, double luminance = -1, double saturation = -1)
        {
            Color c = (colorName == "カスタム" ? ColorDef.FromUInt(colorValue) : ColorDef.ColorFromName(colorName));
            Brush brsh;
            if (gradation == false)
            {
                brsh = new SolidColorBrush(c);
            }
            else
            {
                brsh = luminance == -1 ? ColorDef.GradientBrush(c) : ColorDef.GradientBrush(c, luminance, saturation);
            }
            brsh.Freeze();
            return brsh;
        }
        public void ReloadCustContentColorList()
        {
            try
            {
                CustContentColorList.Clear();
                for (int i = 0; i < Settings.Instance.ContentColorList.Count; i++)
                {
                    CustContentColorList.Add(_GetColorBrush(Settings.Instance.ContentColorList[i], Settings.Instance.ContentCustColorList[i], Settings.Instance.EpgGradation));
                }
                CustContentColorList.Add(_GetColorBrush(Settings.Instance.ReserveRectColorNormal, Settings.Instance.ContentCustColorList[0x11]));
                CustContentColorList.Add(_GetColorBrush(Settings.Instance.ReserveRectColorNo, Settings.Instance.ContentCustColorList[0x12]));
                CustContentColorList.Add(_GetColorBrush(Settings.Instance.ReserveRectColorNoTuner, Settings.Instance.ContentCustColorList[0x13]));
                CustContentColorList.Add(_GetColorBrush(Settings.Instance.ReserveRectColorWarning, Settings.Instance.ContentCustColorList[0x14]));
                CustContentColorList.Add(_GetColorBrush(Settings.Instance.ReserveRectColorAutoAddMissing, Settings.Instance.ContentCustColorList[0x15]));
                CustContentColorList.Add(_GetColorBrush(Settings.Instance.ReserveRectColorMultiple, Settings.Instance.ContentCustColorList[0x16]));

                CustTitle1Color = _GetColorBrush(Settings.Instance.TitleColor1, Settings.Instance.TitleCustColor1);
                CustTitle2Color = _GetColorBrush(Settings.Instance.TitleColor2, Settings.Instance.TitleCustColor2);
                CustTunerServiceColor = _GetColorBrush(Settings.Instance.TunerServiceColors[0], Settings.Instance.TunerServiceCustColors[0]);
                CustTunerTextColor = _GetColorBrush(Settings.Instance.TunerServiceColors[1], Settings.Instance.TunerServiceCustColors[1]);

                CustTunerServiceColorPri.Clear();
                for (int i = 2; i < 2 + 5; i++)
                {
                    CustTunerServiceColorPri.Add(_GetColorBrush(Settings.Instance.TunerServiceColors[i], Settings.Instance.TunerServiceCustColors[i]));
                }
                TunerBackColor = _GetColorBrush(Settings.Instance.TunerServiceColors[7 + 0], Settings.Instance.TunerServiceCustColors[7 + 0]);
                TunerReserveBorderColor = _GetColorBrush(Settings.Instance.TunerServiceColors[7 + 1], Settings.Instance.TunerServiceCustColors[7 + 1]);
                TunerTimeFontColor = _GetColorBrush(Settings.Instance.TunerServiceColors[7 + 2], Settings.Instance.TunerServiceCustColors[7 + 2]);
                TunerTimeBackColor = _GetColorBrush(Settings.Instance.TunerServiceColors[7 + 3], Settings.Instance.TunerServiceCustColors[7 + 3]);
                TunerTimeBorderColor = _GetColorBrush(Settings.Instance.TunerServiceColors[7 + 4], Settings.Instance.TunerServiceCustColors[7 + 4]);
                TunerNameFontColor = _GetColorBrush(Settings.Instance.TunerServiceColors[7 + 5], Settings.Instance.TunerServiceCustColors[7 + 5]);
                TunerNameBackColor = _GetColorBrush(Settings.Instance.TunerServiceColors[7 + 6], Settings.Instance.TunerServiceCustColors[7 + 6]);
                TunerNameBorderColor = _GetColorBrush(Settings.Instance.TunerServiceColors[7 + 7], Settings.Instance.TunerServiceCustColors[7 + 7]);

                CustTimeColorList.Clear();
                for (int i = 0; i < Settings.Instance.EpgEtcColors.Count; i++)
                {
                    CustTimeColorList.Add(_GetColorBrush(Settings.Instance.EpgEtcColors[i], Settings.Instance.EpgEtcCustColors[i], Settings.Instance.EpgGradationHeader));
                }

                EpgServiceBackColor = _GetColorBrush(Settings.Instance.EpgEtcColors[4], Settings.Instance.EpgEtcCustColors[4], Settings.Instance.EpgGradationHeader, 1.0, 2.0);
                EpgBackColor = _GetColorBrush(Settings.Instance.EpgEtcColors[5], Settings.Instance.EpgEtcCustColors[5]);
                EpgBorderColor = _GetColorBrush(Settings.Instance.EpgEtcColors[6], Settings.Instance.EpgEtcCustColors[6]);
                EpgServiceFontColor = _GetColorBrush(Settings.Instance.EpgEtcColors[7], Settings.Instance.EpgEtcCustColors[7]);
                EpgServiceBorderColor = _GetColorBrush(Settings.Instance.EpgEtcColors[8], Settings.Instance.EpgEtcCustColors[8]);
                EpgTimeFontColor = _GetColorBrush(Settings.Instance.EpgEtcColors[9], Settings.Instance.EpgEtcCustColors[9]);
                EpgTimeBorderColor = _GetColorBrush(Settings.Instance.EpgEtcColors[10], Settings.Instance.EpgEtcCustColors[10]);
                EpgWeekdayBorderColor = _GetColorBrush(Settings.Instance.EpgEtcColors[11], Settings.Instance.EpgEtcCustColors[11]);

                RecEndDefBackColor = _GetColorBrush(Settings.Instance.RecEndColors[0], Settings.Instance.RecEndCustColors[0]);
                RecEndErrBackColor = _GetColorBrush(Settings.Instance.RecEndColors[1], Settings.Instance.RecEndCustColors[1]);
                RecEndWarBackColor = _GetColorBrush(Settings.Instance.RecEndColors[2], Settings.Instance.RecEndCustColors[2]);


                ListDefForeColor = _GetColorBrush(Settings.Instance.ListDefColor, Settings.Instance.ListDefCustColor);

                RecModeForeColor.Clear();
                for (int i = 0; i < Settings.Instance.RecModeFontColors.Count; i++)
                {
                    RecModeForeColor.Add(_GetColorBrush(Settings.Instance.RecModeFontColors[i], Settings.Instance.RecModeFontCustColors[i]));
                }

                ResDefBackColor = _GetColorBrush(Settings.Instance.ResBackColors[0], Settings.Instance.ResBackCustColors[0]);
                ResNoBackColor = _GetColorBrush(Settings.Instance.ResBackColors[1], Settings.Instance.ResBackCustColors[1]);
                ResErrBackColor = _GetColorBrush(Settings.Instance.ResBackColors[2], Settings.Instance.ResBackCustColors[2]);
                ResWarBackColor = _GetColorBrush(Settings.Instance.ResBackColors[3], Settings.Instance.ResBackCustColors[3]);
                ResAutoAddMissingBackColor = _GetColorBrush(Settings.Instance.ResBackColors[4], Settings.Instance.ResBackCustColors[4]);
                ResMultipleBackColor = _GetColorBrush(Settings.Instance.ResBackColors[5], Settings.Instance.ResBackCustColors[5]);

                StatResForeColor = _GetColorBrush(Settings.Instance.StatColors[0], Settings.Instance.StatCustColors[0]);
                StatRecForeColor = _GetColorBrush(Settings.Instance.StatColors[1], Settings.Instance.StatCustColors[1]);
                StatOnAirForeColor = _GetColorBrush(Settings.Instance.StatColors[2], Settings.Instance.StatCustColors[2]);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        const int NotifyLogMaxLocal = 8192 * 2;
        public static void AddNotifyLog(NotifySrvInfo notifyInfo)
        {
            if (Instance.NotifyLogList.Count >= NotifyLogMaxLocal)
            {
                Instance.NotifyLogList.RemoveRange(0, NotifyLogMaxLocal / 2);
            }
            Instance.NotifyLogList.Add(notifyInfo);
            if (Settings.Instance.AutoSaveNotifyLog == 1)
            {
                String filePath = SettingPath.ModulePath + "\\Log";
                Directory.CreateDirectory(filePath);
                filePath += "\\EpgTimerNotify_" + DateTime.UtcNow.AddHours(9).ToString("yyyyMMdd") + ".log";
                using (var file = new StreamWriter(filePath, true, Encoding.Unicode))
                {
                    file.WriteLine(new NotifySrvInfoItem(notifyInfo));
                }
            }
        }

        public List<string> GetBonFileList()
        {
            var list = new List<string>();

            try
            {
                if (NWMode == false)
                {
                    foreach (string info in Directory.GetFiles(SettingPath.SettingFolderPath, "*.ChSet4.txt"))
                    {
                        list.Add(GetBonFileName(System.IO.Path.GetFileName(info)) + ".dll");
                    }
                }
                else
                {
                    //EpgTimerが作成したEpgTimerSrv.iniからBonDriverセクションを拾い出す
                    //将来にわたって確実なリストアップではないし、本来ならSendEnumPlugIn()あたりを変更して取得すべきだが、
                    //参考表示なので構わない
                    using (var reader = (new System.IO.StreamReader(SettingPath.TimerSrvIniPath, Encoding.GetEncoding(932))))
                    {
                        while (reader.Peek() >= 0)
                        {
                            string buff = reader.ReadLine();
                            int start = buff.IndexOf('[');
                            int end = buff.LastIndexOf(".dll]");
                            if (start >= 0 && end >= start + 2)
                            {
                                list.Add(buff.Substring(start + 1, end + 3 - start));
                            }
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        private static String GetBonFileName(String src)
        {
            int pos = src.LastIndexOf(")");
            if (pos < 1)
            {
                return src;
            }

            int count = 1;
            for (int i = pos - 1; i >= 0; i--)
            {
                if (src[i] == '(')
                {
                    count--;
                }
                else if (src[i] == ')')
                {
                    count++;
                }
                if (count == 0)
                {
                    return src.Substring(0, i);
                }
            }
            return src;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        private static Process SrvSettingProcess = null;

        public static void OpenSrvSetting()
        {
            if (Instance.NWMode == true) return;

            try
            {
                if (SrvSettingProcess == null || SrvSettingProcess.HasExited)
                {
                    SrvSettingProcess = Process.Start(Path.Combine(SettingPath.ModulePath, "EpgTimerSrv.exe"), "/setting");
                }
                else
                {
                    SetForegroundWindow(SrvSettingProcess.MainWindowHandle);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
    }
}
