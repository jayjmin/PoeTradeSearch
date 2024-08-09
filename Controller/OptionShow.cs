using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoeTradeSearch
{
    public partial class WinMain : Window
    {
        private void ResetControls()
        {
            tbLinksMin.Text = "";
            tbSocketMin.Text = "";
            tbLinksMax.Text = "";
            tbSocketMax.Text = "";
            tbLvMin.Text = "";
            tbLvMax.Text = "";
            tbQualityMin.Text = "";
            tbQualityMax.Text = "";
            tkDetail.Text = "";

            lbDPS.Content = "옵션";
            Synthesis.Content = "결합";

            cbRarity.Items.Clear();
            cbRarity.Items.Add("모두");
            cbRarity.Items.Add(mParser.Rarity.Entries[0].Text[0]);
            cbRarity.Items.Add(mParser.Rarity.Entries[1].Text[0]);
            cbRarity.Items.Add(mParser.Rarity.Entries[2].Text[0]);
            cbRarity.Items.Add(mParser.Rarity.Entries[3].Text[0]);

            cbAiiCheck.IsChecked = false;
            ckLv.IsChecked = false;
            ckQuality.IsChecked = false;
            ckSocket.IsChecked = false;
            Synthesis.IsChecked = false;

            cbAltQuality.Items.Clear();
            cbInfluence1.SelectedIndex = 0;
            cbInfluence2.SelectedIndex = 0;
            cbInfluence1.BorderThickness = new Thickness(1);
            cbInfluence2.BorderThickness = new Thickness(1);

            cbCorrupt.SelectedIndex = 0;
            cbCorrupt.BorderThickness = new Thickness(1);
            cbCorrupt.FontWeight = FontWeights.Normal;
            cbCorrupt.Foreground = cbInfluence1.Foreground;

            cbOrbs.SelectionChanged -= CbOrbs_SelectionChanged;
            cbSplinters.SelectionChanged -= CbOrbs_SelectionChanged;
            cbOrbs.SelectedIndex = 0;
            cbSplinters.SelectedIndex = 0;
            cbOrbs.SelectionChanged += CbOrbs_SelectionChanged;
            cbSplinters.SelectionChanged += CbOrbs_SelectionChanged;

            cbOrbs.FontWeight = FontWeights.Normal;
            cbSplinters.FontWeight = FontWeights.Normal;

            ckLv.Content = mParser.Level.Text[0];
            ckLv.FontWeight = FontWeights.Normal;
            ckLv.Foreground = Synthesis.Foreground;
            ckLv.BorderBrush = Synthesis.BorderBrush;
            ckQuality.FontWeight = FontWeights.Normal;
            ckQuality.Foreground = Synthesis.Foreground;
            ckQuality.BorderBrush = Synthesis.BorderBrush;
            lbSocketBackground.Visibility = Visibility.Hidden;

            tabControl1.SelectedIndex = 0;
            cbPriceListCount.SelectedIndex = (mConfig.Options.SearchListCount / 20) - 1;
            tbPriceFilterMin.Text = mConfig.Options.SearchPriceMinimum > 0 ? mConfig.Options.SearchPriceMinimum.ToString() : "";

            tkPriceCount.Text = "";
            tkPriceInfo.Text = (string)tkPriceInfo.Tag;
            cbPriceListTotal.Text = "0/0 검색";

            for (int i = 0; i < 10; i++)
            {
                ((ComboBox)FindName("cbOpt" + i)).Items.Clear();
                // ((ComboBox)FindName("cbOpt" + langIdx)).ItemsSource = new List<FilterEntrie>();
                ((ComboBox)FindName("cbOpt" + i)).DisplayMemberPath = "Name";
                ((ComboBox)FindName("cbOpt" + i)).SelectedValuePath = "Name";

                ((TextBox)FindName("tbOpt" + i)).Text = "";
                ((TextBox)FindName("tbOpt" + i)).Tag = null; // 특수 옵션에 사용
                ((TextBox)FindName("tbOpt" + i)).Background = SystemColors.WindowBrush;
                ((TextBox)FindName("tbOpt" + i + "_0")).Text = "";
                ((TextBox)FindName("tbOpt" + i + "_1")).Text = "";
                ((TextBox)FindName("tbOpt" + i + "_0")).IsEnabled = true;
                ((TextBox)FindName("tbOpt" + i + "_1")).IsEnabled = true;
                ((TextBox)FindName("tbOpt" + i + "_0")).Background = SystemColors.WindowBrush;
                ((TextBox)FindName("tbOpt" + i + "_0")).Foreground = ((TextBox)FindName("tbOpt" + i)).Foreground;
                ((CheckBox)FindName("tbOpt" + i + "_2")).BorderThickness = new Thickness(1);
                ((CheckBox)FindName("tbOpt" + i + "_2")).IsEnabled = true;
                ((CheckBox)FindName("tbOpt" + i + "_2")).IsChecked = false;
                ((CheckBox)FindName("tbOpt" + i + "_3")).IsChecked = false;

                SetFilterObjectColor(i, SystemColors.ActiveBorderBrush);
                SetFilterObjectVisibility(i, Visibility.Visible);

                ((CheckBox)FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
            }
        }

        private void SetFilterObjectColor(int index, System.Windows.Media.SolidColorBrush colorBrush)
        {
            ((Control)FindName("tbOpt" + index)).BorderBrush = colorBrush;
            ((Control)FindName("tbOpt" + index + "_0")).BorderBrush = colorBrush;
            ((Control)FindName("tbOpt" + index + "_1")).BorderBrush = colorBrush;
            ((Control)FindName("tbOpt" + index + "_2")).BorderBrush = colorBrush;
            ((Control)FindName("tbOpt" + index + "_3")).BorderBrush = colorBrush;
        }

        private void SetFilterObjectVisibility(int index, Visibility visibility)
        {
            ((ComboBox)FindName("cbOpt" + index)).Visibility = visibility;
            ((Control)FindName("tbOpt" + index + "_0")).Visibility = visibility;
            ((Control)FindName("tbOpt" + index + "_1")).Visibility = visibility;
            ((Control)FindName("tbOpt" + index + "_2")).Visibility = visibility;
            ((Control)FindName("tbOpt" + index + "_3")).Visibility = visibility;
        }
        private void SetDPS(string physical, string elemental, string chaos, string quality, string perSecond, double phyDmgIncr, double speedIncr)
        {
            lbDPS.Content = Helper.CalcDPS(physical, elemental, chaos, quality, perSecond, phyDmgIncr, speedIncr);
        }

        private void Deduplicationfilter(List<Itemfilter> itemfilters)
        {
            for (int i = 0; i < itemfilters.Count; i++)
            {
                string txt = ((TextBox)FindName("tbOpt" + i)).Text;
                if (((CheckBox)FindName("tbOpt" + i + "_2")).IsEnabled == false) continue;

                for (int j = 0; j < itemfilters.Count; j++)
                {
                    if (i == j) continue;

                    CheckBox tmpCcheckBox2 = (CheckBox)FindName("tbOpt" + j + "_2");
                    if (((TextBox)FindName("tbOpt" + j)).Text == txt)
                    {
                        tmpCcheckBox2.IsChecked = false;
                        tmpCcheckBox2.IsEnabled = false;
                        itemfilters[j].disabled = true;
                    }
                }
            }
        }
        
        internal void AddItem(int optionIdx, FilterEntrie entrie)
        {
            (FindName("cbOpt" + optionIdx) as ComboBox).Items.Add(entrie);
        }

        internal void AddOptionItem(FilterDictItem filter, double min, double max, string[] cate_ids, bool local_exists,
            int lang, int optionIdx, string dataLabel, List<Itemfilter> itemfilters, ParserDictItem special_option, int is_deep, bool hasResistance, string ft_type, ParserData PS)
        {
            // This fuction adds one search option line in the main box to select min/max value to search.

            string[] split_id = filter.Id.Split('.');

            (FindName("cbOpt" + optionIdx) as ComboBox).Items.Add(new FilterEntrie(cate_ids[0], split_id[0], split_id[1], dataLabel));


            Dictionary<string, SolidColorBrush> color = new Dictionary<string, SolidColorBrush>()
                                        {
                                            { "implicit", Brushes.DarkRed }, { "crafted", Brushes.Blue }, { "enchant", Brushes.Blue }, { "scourge", Brushes.DarkOrange }
            };
            SetFilterObjectColor(optionIdx, color.ContainsKey(ft_type) ? color[ft_type] : SystemColors.ActiveBorderBrush);

            (FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue = RS.lFilterType["pseudo"];
            if ((FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue == null)
            {
                if (split_id.Length == 2 && RS.lPseudo.ContainsKey(split_id[1]))
                    (FindName("cbOpt" + optionIdx) as ComboBox).Items.Add(new FilterEntrie(cate_ids[0], "pseudo", split_id[1], RS.lFilterType["pseudo"]));
            }

            if ((FindName("cbOpt" + optionIdx) as ComboBox).Items.Count == 1)
            {
                (FindName("cbOpt" + optionIdx) as ComboBox).SelectedIndex = 0;
            }
            else
            {
                // Initial choice - pseudo or explicit?
                List<string> orderedType = new List<string>();

                if (ft_type == "fractured")
                    orderedType.Add(ft_type);
                if (!local_exists && mConfig.Options.AutoSelectPseudo)
                    orderedType.Add("pseudo");
                orderedType.Add(ft_type);
                orderedType.Add("explicit");
                orderedType.Add("fractured");
                foreach (string type in orderedType)
                {
                    (FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue = RS.lFilterType.ContainsKey(type) ? RS.lFilterType[type] : "_none_";
                    if ((FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue != null) break;
                }
            }

            // 평균
            if (min != 99999 && max != 99999 && filter.Text.IndexOf("#" + (lang == 0 ? "~" : " to ") + "#") > -1)
            {
                min += max;
                min = Math.Truncate(min / 2 * 10) / 10;
                max = 99999;
            }

            // 역방향 이면 위치 바꿈
            ParserDictItem force_pos = Array.Find(PS.Position.Entries, x => x.Id.Equals(split_id[1]));
            if (force_pos?.Key == "reverse" || force_pos?.Key == "right")
            {
                (max, min) = (min, max);
            }

            Itemfilter itemFilter = new Itemfilter
            {
                stat = split_id[1],
                type = filter.Type,
                text = filter.Text,
                max = max,
                min = min,
                disabled = true
            };

            itemfilters.Add(itemFilter);

            (FindName("tbOpt" + optionIdx) as TextBox).Text = (is_deep > 0 ? is_deep.ToString() + ") " : "") + filter.Text;

            (FindName("tbOpt" + optionIdx + "_3") as CheckBox).Visibility = hasResistance ? Visibility.Visible : Visibility.Hidden;
            if ((FindName("tbOpt" + optionIdx + "_3") as CheckBox).Visibility == Visibility.Visible && mConfig.Options.AutoCheckTotalres)
                (FindName("tbOpt" + optionIdx + "_3") as CheckBox).IsChecked = true;

            if (special_option != null && (special_option.Key == "CLUSTER" || special_option.Key == "LOGBOOK"))
            {
                (FindName("tbOpt" + optionIdx) as TextBox).Text = special_option.Text[lang];
                (FindName("tbOpt" + optionIdx) as TextBox).Tag = special_option.Key;
                (FindName("tbOpt" + optionIdx + "_0") as TextBox).IsEnabled = false;
                (FindName("tbOpt" + optionIdx + "_1") as TextBox).IsEnabled = false;
                (FindName("tbOpt" + optionIdx + "_2") as CheckBox).IsChecked = true;
                itemFilter.min = min = special_option.Id.ToInt();
                itemFilter.max = max = 99999;
                if (itemfilters.Count > 0) // 군 주얼 패시브 갯수 자동 체크
                {
                    (FindName("tbOpt0_2") as CheckBox).IsChecked = true;
                    itemfilters[0].disabled = false;
                }
            }
            else if (cate_ids.Length == 1 && cate_ids[0] == "memoryline")
            {
                (FindName("tbOpt0_2") as CheckBox).IsChecked = true;
                itemfilters[0].disabled = false;
            }

            if (Array.Find(mParser.Disable.Entries, x => x.Id.Equals(split_id[1])) != null)
            {
                (FindName("tbOpt" + optionIdx + "_2") as CheckBox).IsChecked = false;
                (FindName("tbOpt" + optionIdx + "_2") as CheckBox).IsEnabled = false;
            }
            else
            {
                if (ft_type != "implicit" && (is_deep < 1 || is_deep < 3) &&
                    (mChecked.Entries?.Find(x => x.Id.Equals(split_id[1]) && x.Key.IndexOf(cate_ids[0] + "/") > -1) != null))
                {
                    (FindName("tbOpt" + optionIdx + "_2") as CheckBox).BorderThickness = new Thickness(2);
                    (FindName("tbOpt" + optionIdx + "_2") as CheckBox).IsChecked = true;
                    itemFilter.disabled = false;
                }
            }

            (FindName("tbOpt" + optionIdx + "_0") as TextBox).Text = min == 99999 ? "" : min.ToString();
            (FindName("tbOpt" + optionIdx + "_1") as TextBox).Text = max == 99999 ? "" : max.ToString();

            string[] strs_tmp = (FindName("tbOpt" + optionIdx) as TextBox).Text.Split('\n');
            if (strs_tmp.Length > 1)
            {
                (FindName("tbOpt" + optionIdx) as TextBox).Text = strs_tmp[0];
                for (int ssi = 1; ssi < strs_tmp.Length; ssi++)
                {
                    optionIdx++;
                    SetFilterObjectVisibility(optionIdx, Visibility.Hidden);
                    (FindName("tbOpt" + optionIdx) as TextBox).Text = strs_tmp[ssi];
                    (FindName("tbOpt" + optionIdx + "_2") as CheckBox).IsChecked = false;
                    ((ComboBox)FindName("cbOpt" + optionIdx)).Items.Clear();
                    SetFilterObjectColor(optionIdx, color.ContainsKey(ft_type) ? color[ft_type] : SystemColors.ActiveBorderBrush);
                }
            }

            if (ft_type == "_none_" && (FindName("cbOpt" + optionIdx) as ComboBox).SelectedIndex > -1 &&
                (string)(FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue != RS.lFilterType["explicit"] &&
                (string)(FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue != RS.lFilterType["pseudo"])
            {
                SetFilterObjectColor(optionIdx, Brushes.Pink);
            }
        }

        private void ItemParser(string itemText, bool isWinShow = true)
        {

            // PS stores data loaded from Parser.txt file.
            ParserData PS = mParser;

            try
            {
                // asData example:
                // [0]	"아이템 종류: 반지\r\n아이템 희귀도: 희귀\r\n솔 관절\r\n자수정 반지\r\n"	string
                // [1]	"\r\n요구사항:\r\n레벨: 64\r\n"	string
                // [2]	"\r\n아이템 레벨: 85\r\n"	string
                // [3]	"\r\n카오스 저항 +21% (implicit)\r\n"	string
                // [4]	"\r\n공격 시 번개 피해 5~61 추가\r\n시전 속도 15% 증가\r\n마나 최대치 +17\r\n화염 저항 +36%\r\n냉기 저항 +38%\r\n비-집중 유지 스킬의 총 마나 소모 -7 (crafted)"  string

                string[] asData = (itemText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && (asData[0].IndexOf(PS.Category.Text[0] + ": ") == 0 || asData[0].IndexOf(PS.Category.Text[1] + ": ") == 0))
                {
                    // reset window
                    ResetControls();

                    // language. 0: korean, 1: english
                    byte lang = (byte)(asData[0].IndexOf(PS.Category.Text[0] + ": ") == 0 ? 0 : 1);

                    // 베이스 찾기: 종류(갑옷, 장갑 등), 희귀도(마법, 희귀 등), 이름, 베이스(자수정 반지) 로 배열이 생성되어 들어감
                    string[] ibase_info = OptionParser.ItemBaseParser(asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None));

                    ParserDictItem category = Array.Find(PS.Category.Entries, x => x.Text[lang] == ibase_info[0]); // category
                    string[] cate_ids = category != null ? category.Id.Split('.') : new string[] { "" }; // ex) {"id":"armour.chest","key":"armour","text":[ "갑옷", "Body Armours" ]}
                    ParserDictItem rarity = Array.Find(PS.Rarity.Entries, x => x.Text[lang] == ibase_info[1]); // rarity
                    rarity = rarity ?? new ParserDictItem() { Id = "", Text = new string[] { ibase_info[1], ibase_info[1] } }; // ex) {"id":"rare","text":[ "희귀", "Rare" ]}
                    string itemType = ibase_info[3];

                    double attackSpeedIncr = 0, PhysicalDamageIncr = 0;
                    List<Itemfilter> itemfilters;
                    Dictionary<string, string> itemBaseInfo;
                    string map_influenced;

                    (itemBaseInfo, itemfilters, attackSpeedIncr, PhysicalDamageIncr, map_influenced) = OptionParser.ParseOption(asData, PS, lang, itemType, cate_ids, mFilter, this);

                    string item_rarity = rarity.Text[0];
                    string item_name = "";
                    string item_type = "";
                    item_name = ibase_info[2];
                    item_type = ibase_info[3];

                    string gem_disc = "";
                    bool is_blight = false;

                    bool is_map = cate_ids[0] == "map"; // || itemBaseInfo[PS.MapTier.Text[langIdx]] != "";
                    bool is_map_fragment = cate_ids.Length > 1 && cate_ids.Join('.') == "map.fragment";
                    bool is_map_ultimatum = itemBaseInfo[PS.MapUltimatum.Text[lang]] != "";
                    bool is_prophecy = itemBaseInfo[PS.ProphecyItem.Text[lang]] == "_TRUE_";
                    bool is_memory = cate_ids[0] == "memoryline";
                    bool is_currency = rarity.Id == "currency";
                    bool is_divination_card = rarity.Id == "card";
                    bool is_gem = rarity.Id == "gem";
                    bool is_Jewel = cate_ids[0] == "jewel";
                    bool is_sanctum = cate_ids[0] == "sanctum";
                    bool is_vaal_gem = is_gem && itemBaseInfo[PS.Vaal.Text[lang] + " " + item_type] == "_TRUE_";
                    bool is_heist = itemBaseInfo[PS.Heist.Text[lang]] != "";
                    bool is_unIdentify = itemBaseInfo[PS.Unidentified.Text[lang]] == "_TRUE_";
                    bool is_detail = is_gem || is_map_fragment || is_currency || is_divination_card || is_prophecy;

                    if (is_memory)
                    {
                        item_name = ibase_info[3];
                        item_type = ibase_info[3];
                    }

                    if (is_map_ultimatum || is_sanctum)
                        is_detail = false;

                    int item_idx = -1;

                    ////////////////////////////////
                    /// DEBUGGING POINT
                    /// Find item from ItemsKO.txt and ItemsEN.txt
                    ////////////////////////////////
                    // How to debug Korean - English mismatch issue:
                    // 1. Find Id and Key from Parser.txt file. This is the value of "category" here.
                    // 2. Find the same Id from ItemsEN.txt file.
                    // 'Key' from Parser.txt must be equal to 'id' of ItemEN.txt file. If not, update 'Key' in the Parser.txt because ItemEN.txt is updated dynamically from trade site.

                    // 정리:
                    // 영문판 Ctrl + C 했을 때 게임에서 복사된 Item Class는 복수 (Jewels) 로 표시됨.
                    // ItemsEN.txt 에는 Id: 단수(jewel) / Key: 복수(jewels)로 표시됨
                    // Parser.txt 에는 Id: 아이템 이름 (jewel.base) / Key: 아이템 종류 (jewel)를 저장함.
                    // 1. Ctrl+C에서 복사된 Item Class를 Parser.txt에서 검색해서 category를 찾음.
                    // 2. category.Key (아이템 종류)를 다시 ItemEN.txt에서 Id로 검색함.
                    // ItemsEN.Key와 Category.Key를 비교하는게 아님.

                    int cate_idx = category != null ? Array.FindIndex(mItems[lang].Result, x => x.Id.Equals(category.Key)) : -1;

                    if (is_prophecy)
                    {
                        cate_ids = new string[] { "prophecy" };
                        item_rarity = Array.Find(PS.Category.Entries, x => x.Id == "prophecy").Text[lang];
                        item_idx = Array.FindIndex(mItems[lang].Result[cate_idx].Entries, x => x.Type == item_type);
                    }
                    if (is_map_fragment || is_map_ultimatum)
                    {
                        item_rarity = is_map_ultimatum ? "결전" : Array.Find(PS.Category.Entries, x => x.Id == "map.fragment").Text[lang];
                        item_idx = Array.FindIndex(mItems[lang].Result[cate_idx].Entries, x => x.Type == item_type);
                    }
                    else if (itemBaseInfo[PS.MonsterGenus.Text[lang]] != "" && itemBaseInfo[PS.MonsterGroup.Text[lang]] != "")
                    {
                        cate_ids = new string[] { "monster", "beast" };
                        cate_idx = Array.FindIndex(mItems[lang].Result, x => x.Id.Equals("monsters"));
                        item_idx = Array.FindIndex(mItems[lang].Result[cate_idx].Entries, x => x.Text == item_type);
                        item_rarity = Array.Find(PS.Category.Entries, x => x.Id == "monster.beast").Text[lang];
                        item_type = lang == 1 || item_idx == -1 ? item_type : mItems[1].Result[cate_idx].Entries[item_idx].Type;
                        item_idx = -1; // 야수는 영어로만 검색됨...
                    }
                    else if (is_gem)
                    {
                        if (is_vaal_gem && itemBaseInfo[PS.Corrupted.Text[lang]] == "_TRUE_")
                        {
                            FilterDict data = mItems[lang].Result[cate_idx];
                            FilterDictItem entries = Array.Find(data.Entries, x => x.Text.Equals(PS.Vaal.Text[lang] + " " + item_type));
                            if (entries != null) item_type = entries.Type;
                        }

                        // Find transfigured gem first: "name" is transfigured gem full name, "type" is the base gem name.
                        item_idx = Array.FindIndex(mItems[lang].Result[cate_idx].Entries, x => (x.Text == item_type));

                        if (item_idx == -1)
                        {
                            item_idx = Array.FindIndex(mItems[lang].Result[cate_idx].Entries, x => (x.Type == item_type));
                        }
                        else
                        {
                            // Transfigured Gem
                            item_name = item_type;
                            item_type = mItems[lang].Result[cate_idx].Entries[item_idx].Type;
                            gem_disc = mItems[lang].Result[cate_idx].Entries[item_idx].Disc;
                        }
                    }
                    else if (cate_idx > -1)
                    {
                        FilterDict data = mItems[lang].Result[cate_idx];

                        if ((is_unIdentify || rarity.Id == "normal") && item_type.Length > 4 && item_type.IndexOf(PS.Superior.Text[lang] + " ") == 0)
                        {
                            item_type = item_type.Substring(lang == 1 ? 9 : 3);
                        }
                        else if (rarity.Id == "magic")
                        {
                            item_type = item_type.Split(new string[] { lang == 1 ? " of " : " - " }, StringSplitOptions.None)[0].Trim();
                        }

                        if (is_map && item_type.Length > 5)
                        {
                            if (item_type.Length > 5)
                            {
                                if (item_type.IndexOf(PS.Blighted.Text[lang] + " ") == 0)
                                {
                                    is_blight = true;
                                    item_type = item_type.Substring(PS.Blighted.Text[lang].Length + 1);
                                }

                                if (item_type.IndexOf(PS.Shaped.Text[lang] + " ") == 0)
                                    item_type = item_type.Substring(PS.Shaped.Text[lang].Length + 1);
                            }
                            // 환영 지도면 구분을 위해서 1번 옵션 자동 체크
                            if (!itemBaseInfo[PS.DeliriumReward.Text[lang]].IsEmpty() && itemfilters.Count > 0)
                            {
                                (FindName("tbOpt0_2") as CheckBox).IsChecked = true;
                                (FindName("tbOpt0_0") as TextBox).Text = "";
                                itemfilters[0].disabled = false;
                                itemfilters[0].min = 99999;
                            }
                        }
                        else if (itemBaseInfo[PS.SynthesisedItem.Text[lang]] == "_TRUE_")
                        {
                            string[] tmp = PS.SynthesisedItem.Text[lang].Split(' ');
                            if (item_type.IndexOf(tmp[0] + " ") == 0)
                                item_type = item_type.Substring(tmp[0].Length + 1);
                        }

                        if (!is_unIdentify && rarity.Id == "magic")
                        {
                            string[] tmp = item_type.Split(' ');

                            if (data != null && tmp.Length > 1)
                            {
                                for (int i = 0; i < tmp.Length - 1; i++)
                                {
                                    tmp[i] = "";
                                    string tmp2 = tmp.Join(' ').Trim();

                                    FilterDictItem entries = Array.Find(data.Entries, x => x.Type.Equals(tmp2));
                                    if (entries != null)
                                    {
                                        item_type = entries.Type;
                                        break;
                                    }
                                }
                            }
                        }

                        item_idx = Array.FindIndex(mItems[lang].Result[cate_idx].Entries, x => (x.Type == item_type && (rarity.Id != "unique" || x.Name == item_name)));
                    }

                    string item_quality = Regex.Replace(itemBaseInfo[PS.Quality.Text[lang]], "[^0-9]", "");
                    bool is_gear = cate_ids.Length > 1 && cate_ids[0].WithIn("weapon", "armour", "accessory");

                    if (is_detail || is_map_fragment)
                    {
                        try
                        {
                            int i = is_map_fragment ? 1 : (is_gem ? 3 : 2);
                            tkDetail.Text = asData.Length > (i + 1) ? asData[i] + asData[i + 1] : asData[asData.Length - 1];

                            tkDetail.Text = Regex.Replace(
                                tkDetail.Text.Replace(PS.UnstackItems.Text[lang], ""),
                                "<(uniqueitem|prophecy|divination|gemitem|magicitem|rareitem|whiteitem|corrupted|default|normal|augmented|size:[0-9]+)>",
                                ""
                            );
                        }
                        catch { }
                    }
                    else
                    {
                        // 장기는 중복 옵션 제거
                        if (cate_ids.Join('.') == "monster.sample")
                        {
                            Deduplicationfilter(itemfilters);
                        }
                        else if (!is_unIdentify && cate_ids[0] == "weapon")
                        {
                            SetDPS(
                                    itemBaseInfo[PS.PhysicalDamage.Text[lang]], itemBaseInfo[PS.ElementalDamage.Text[lang]], itemBaseInfo[PS.ChaosDamage.Text[lang]],
                                    item_quality, itemBaseInfo[PS.AttacksPerSecond.Text[lang]], PhysicalDamageIncr, attackSpeedIncr
                                );
                        }
                    }

                    DisplayOption(isWinShow, PS, asData, lang, cate_ids, rarity, attackSpeedIncr, PhysicalDamageIncr, itemfilters, itemBaseInfo, map_influenced, item_rarity, item_name, item_type, gem_disc, is_blight, is_map, is_map_fragment, is_map_ultimatum, is_gem, is_Jewel, is_sanctum, is_heist, is_unIdentify, is_detail, item_idx, cate_idx, item_quality, is_gear);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                ForegroundMessage(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayOption(bool isWinShow, ParserData PS, string[] asData, byte lang, string[] cate_ids, ParserDictItem rarity, double attackSpeedIncr, double PhysicalDamageIncr, List<Itemfilter> itemfilters, Dictionary<string, string> itemBaseInfo, string map_influenced, string item_rarity, string item_name, string item_type, string gem_disc, bool is_blight, bool is_map, bool is_map_fragment, bool is_map_ultimatum, bool is_gem, bool is_Jewel, bool is_sanctum, bool is_heist, bool is_unIdentify, bool is_detail, int item_idx, int cate_idx, string item_quality, bool is_gear)
        {
            cbName.Items.Clear();

            bool btmp = cate_idx == -1 || item_idx == -1;
            for (int langIdx = 0; langIdx < 2; langIdx++)
            {
                string name = btmp || rarity.Id != "unique" ? item_name : mItems[langIdx].Result[cate_idx].Entries[item_idx].Name;
                string type = btmp ? item_type : mItems[langIdx].Result[cate_idx].Entries[item_idx].Type;
                cbName.Items.Add(new ItemNames(name, type));
            }
            cbName.SelectedIndex = Helper.SelectServerLang(mConfig.Options.ServerType, lang);
            cbName.Tag = cate_ids; //카테고리

            string[] bys = mConfig.Options.AutoSelectByType.ToLower().Split(',');
            if (bys.Length > 0)
            {
                ckByCategory.IsChecked = Array.IndexOf(bys, cate_ids.Join('.')) > -1;
            }

            cbRarity.SelectedValue = item_rarity;

            if (cbRarity.SelectedIndex == -1)
            {
                cbRarity.Items.Clear();
                cbRarity.Items.Add(item_rarity);
                cbRarity.SelectedIndex = 0;
            }
            else if ((string)cbRarity.SelectedValue == "normal")
            {
                cbRarity.SelectedIndex = 0;
            }

            bdExchange.IsEnabled = cate_ids[0] == "currency" && GetExchangeItem(lang, item_type) != null;
            bdExchange.Visibility = !(is_gem || is_sanctum) && (is_detail || bdExchange.IsEnabled) ? Visibility.Visible : Visibility.Hidden;

            if (bdExchange.Visibility == Visibility.Hidden)
            {
                tbLvMin.Text = Regex.Replace(itemBaseInfo[is_gem ? PS.Level.Text[lang] : PS.ItemLevel.Text[lang]], "[^0-9]", "");
                tbQualityMin.Text = item_quality;

                string[] Influences = { PS.ShaperItem.Text[lang], PS.ElderItem.Text[lang], PS.CrusaderItem.Text[lang], PS.RedeemerItem.Text[lang], PS.HunterItem.Text[lang], PS.WarlordItem.Text[lang] };
                for (int i = 0; i < Influences.Length; i++)
                {
                    if (itemBaseInfo[Influences[i]] == "_TRUE_")
                        cbInfluence1.SelectedIndex = i + 1;
                }

                for (int i = 0; i < Influences.Length; i++)
                {
                    if (cbInfluence1.SelectedIndex != (i + 1) && itemBaseInfo[Influences[i]] == "_TRUE_")
                        cbInfluence2.SelectedIndex = i + 1;
                }

                if (itemBaseInfo[PS.Corrupted.Text[lang]] == "_TRUE_")
                {
                    cbCorrupt.BorderThickness = new Thickness(2);
                    cbCorrupt.FontWeight = FontWeights.Bold;
                    cbCorrupt.Foreground = System.Windows.Media.Brushes.DarkRed;
                }

                if (is_gem)
                {
                    ckLv.IsChecked = itemBaseInfo[PS.Level.Text[lang]].IndexOf(" (" + PS.Max.Text[lang]) > 0;
                    ckQuality.IsChecked = item_quality.ToInt(0) > 19;
                    cbAltQuality.Items.Add(gem_disc);
                    cbAltQuality.SelectedValue = gem_disc;
                }
                else if (is_sanctum)
                {
                    ckLv.IsChecked = true;
                    tbLvMin.Text = tbLvMax.Text = itemBaseInfo[PS.AreaLevel.Text[lang]];
                }
                else if (is_Jewel || is_heist || is_map)
                {
                    cbAltQuality.Items.Add(
                        is_heist ? "모든 강탈 가치" : (
                            is_map_ultimatum ? "모든 보상" : (
                                is_Jewel ? "모든 반경"
                                    : "영향 없음")
                        ));

                    foreach (ParserDictItem item in (
                        is_heist ? PS.Heist : (
                            is_map_ultimatum ? PS.RewardUltimatum : (is_Jewel ? RS.lRadius : PS.MapTier)
                        )).Entries)
                    {
                        cbAltQuality.Items.Add(item.Text[lang]);
                    }

                    if (is_Jewel)
                    {
                        cbAltQuality.SelectedItem = itemBaseInfo[PS.Radius.Text[lang]];
                        if (cbAltQuality.SelectedIndex == -1)
                        {
                            cbAltQuality.Items.Clear();
                            cbAltQuality.Items.Add(itemBaseInfo[PS.Radius.Text[lang]] ?? "");
                            cbAltQuality.SelectedIndex = 0;
                        }
                    }
                    else if (is_heist)
                    {
                        string tmp = Regex.Replace(itemBaseInfo[PS.Heist.Text[lang]], @".+ \(([^\)]+)\)$", "$1");
                        cbAltQuality.SelectedValue = tmp;
                        if (cbAltQuality.SelectedIndex == -1)
                        {
                            cbAltQuality.SelectedIndex = 0;
                        }
                        ckLv.IsChecked = true;
                    }
                    else if (is_map_ultimatum)
                    {
                        Synthesis.Content = "역병";
                        cbAltQuality.SelectedValue = itemBaseInfo[PS.RewardUltimatum.Text[lang]];
                        if (cbAltQuality.SelectedIndex == -1)
                        {
                            cbAltQuality.Items[cbAltQuality.Items.Count - 1] = itemBaseInfo[PS.RewardUltimatum.Text[lang]];
                            cbAltQuality.SelectedIndex = cbAltQuality.Items.Count - 1;
                        }
                    }
                    else if (is_map)
                    {
                        Synthesis.Content = "역병";
                        ckLv.IsChecked = true;
                        ckLv.Content = "등급";
                        tbLvMin.Text = tbLvMax.Text = itemBaseInfo[PS.MapTier.Text[lang]];
                        cbAltQuality.SelectedValue = map_influenced != "" ? map_influenced : "영향 없음";
                    }
                }
                else if (is_gear || cate_ids[0] == "flask")
                {
                    if (tbQualityMin.Text.ToInt(0) > (cate_ids[0] == "accessory" ? 4 : 20))
                    {
                        ckQuality.FontWeight = FontWeights.Bold;
                        ckQuality.Foreground = System.Windows.Media.Brushes.DarkRed;
                        ckQuality.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                    }

                    if (is_gear)
                    {
                        cbCorrupt.SelectedIndex = mConfig.Options.AutoSelectCorrupt == "no" ? 2 : (mConfig.Options.AutoSelectCorrupt == "yes" ? 1 : 0);
                    }
                }
            }

            if (itemBaseInfo[PS.Sockets.Text[lang]] != "")
            {
                int[] socket = OptionParser.SocketParser(itemBaseInfo[PS.Sockets.Text[lang]]);
                tbSocketMin.Text = socket[0].ToString();
                tbLinksMin.Text = socket[1] > 0 ? socket[1].ToString() : "";
                ckSocket.IsChecked = socket[1] > 4;
            }

            if (is_gear && ckLv.IsChecked == false && cbName.Items.Count == 2)
            {
                ItemNames names = (ItemNames)cbName.Items[0];
                string tmp = names.Type.Escape() + @"\(([0-9]+)\)\/";
                string tmp2 = (cbInfluence1.Text ?? "__NULL__") + "|" + (cbInfluence2.Text ?? "__NULL__");
                CheckedDictItem baseitem = mChecked.bases?.Find(x => Regex.IsMatch(x.Id, "모두|" + tmp2) && Regex.IsMatch(x.Key, tmp));
                if (baseitem != null)
                {
                    MatchCollection mmm = Regex.Matches(baseitem.Key, tmp);
                    if (mmm.Count == 1 && mmm[0].Groups.Count == 2 && mmm[0].Groups[1].Value.ToInt(101) <= tbLvMin.Text.ToInt(0))
                    {
                        ckLv.FontWeight = FontWeights.Bold;
                        ckLv.Foreground = System.Windows.Media.Brushes.DarkRed;
                        ckLv.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                        ckLv.IsChecked = true;
                    }
                }
            }

            if (isWinShow || this.Visibility == Visibility.Visible)
            {
                Synthesis.IsChecked = (is_map && is_blight) || itemBaseInfo[PS.SynthesisedItem.Text[lang]] == "_TRUE_";
                lbSocketBackground.Visibility = is_gear ? Visibility.Hidden : Visibility.Visible;
                cbAltQuality.Visibility = is_gear ? Visibility.Hidden : Visibility.Visible;
                bdDetail.Visibility = is_detail ? Visibility.Visible : Visibility.Hidden;

                cbInfluence1.Visibility = cbAltQuality.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                cbInfluence2.Visibility = cbAltQuality.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                if (cbInfluence1.SelectedIndex > 0) cbInfluence1.BorderThickness = new Thickness(2);
                if (cbInfluence2.SelectedIndex > 0) cbInfluence2.BorderThickness = new Thickness(2);

                tkPriceInfo.Foreground = tkPriceCount.Foreground = SystemColors.WindowTextBrush;

                mLockUpdatePrice = false;

                if (mConfig.Options.SearchAutoDelay > 0 && mAutoSearchTimerCount < 1)
                {
                    UpdatePriceThreadWorker(GetItemOptions(), null);
                }
                else
                {
                    liPrice.Items.Clear();
                }

                if (mConfig.Options.AutoCheckUnique && rarity.Id == "unique")
                    cbAiiCheck.IsChecked = true;

                this.Show();
            }
        }
    }
}
