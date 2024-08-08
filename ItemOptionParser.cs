using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static PoeTradeSearch.Native;
using static System.Net.WebRequestMethods;

namespace PoeTradeSearch
{
    public partial class WinMain : Window
    // public class ItemOptionParser
    {
        private void updateWindow(FilterDictItem filter, double min, double max, string[] cate_ids, bool local_exists, 
            int lang, int optionIdx, string dataLabel, 
            List<Itemfilter> itemfilters, ParserDictItem special_option, int is_deep, bool hasResistance, string ft_type, ParserData PS)
        {
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
                string tmp_type = !local_exists && mConfig.Options.AutoSelectPseudo ? "pseudo" : ft_type;
                (FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue = RS.lFilterType.ContainsKey(tmp_type) ? RS.lFilterType[tmp_type] : "_none_";

                if ((FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue == null)
                {
                    foreach (string type in new string[] { ft_type, "explicit", "fractured" })
                    {
                        (FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue = RS.lFilterType.ContainsKey(type) ? RS.lFilterType[type] : "_none_";
                        if ((FindName("cbOpt" + optionIdx) as ComboBox).SelectedValue != null) break;
                    }
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
                double tmp2 = min;
                min = max;
                max = tmp2;
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
        private (string, ParserDictItem, string, string) optionToFilter(string parsedOption, ParserData PS, byte lang, Dictionary<string, string> itemBaseInfo, string[] cate_ids, ParserDictItem special_option, string[] asSplit)
        {
            string map_influenced = "";
            string input = parsedOption.RepEx(@"\s(\([a-zA-Z]+\)|—\s.+)$", "");
            string ft_type = parsedOption.Split(new string[] { "\n" }, 0)[0].RepEx(@"(.+)\s\(([a-zA-Z]+)\)$", "$2");
            if (!RS.lFilterType.ContainsKey(ft_type)) ft_type = "_none_"; // 영향력 검사???

            if (ft_type == "implicit" && cate_ids.Length == 1 && cate_ids[0] == "map")
            {
                string pats = "";
                foreach (ParserDictItem item in PS.MapTier.Entries)
                {
                    pats += item.Text[lang] + "|";
                }
                Match match = Regex.Match(input.Trim(), "(.+) (" + pats + "_none_)(.*)");
                if (match.Success)
                {
                    map_influenced = match.Groups[2] + "";
                    input = match.Groups[1] + " #" + match.Groups[3];
                }
                return (input, special_option, map_influenced, ft_type);
            }
            else if (special_option == null)
            {
                if (ft_type == "implicit" && cate_ids.Length == 1 && cate_ids[0] == "logbook")
                {
                    special_option = Array.Find(PS.Logbook.Entries, x => x.Text[lang] == input);
                    if (special_option != null)
                    {
                        input = PS.Logbook.Text[lang];
                        special_option.Key = "LOGBOOK";
                    }
                }
                else if (ft_type == "enchant" && asSplit.Length > 1 && cate_ids.Length == 2 && cate_ids[0] == "jewel")
                {
                    string tmp2 = input.Split(':')?[1].Trim().RepEx(@"[0-9]+\%", "#%");
                    special_option = Array.Find(PS.Cluster.Entries, x => x.Text[lang] == tmp2);
                    if (special_option != null)
                    {
                        input = asSplit[0] + ": #";
                        special_option.Key = "CLUSTER";
                    }
                }
                else if (ft_type == "_none_" && itemBaseInfo[PS.Radius.Text[lang]] != "")
                {
                    special_option = Array.Find(PS.Radius.Entries, x => x.Text[lang] == asSplit[0]);
                    if (special_option != null)
                    {
                        itemBaseInfo[PS.Radius.Text[lang]] = RS.lRadius.Entries[special_option.Id.ToInt() - 1].Text[lang];
                        special_option.Key = "RADIUS";
                    }
                }
            }

            return (input, special_option, map_influenced, ft_type);
        }

        private void ItemTextParser(string itemText, bool isWinShow = true)
        {
            int[] SocketParser(string socket)
            {
                int sckcnt = socket.Replace(" ", "-").Split('-').Length;
                string[] scklinks = socket.Split(' ');

                int lnkcnt = 0;
                for (int s = 0; s < scklinks.Length; s++)
                {
                    if (lnkcnt < scklinks[s].Length) lnkcnt = scklinks[s].Length;
                }

                return new int[] { sckcnt, lnkcnt < 3 ? 0 : lnkcnt - (int)Math.Ceiling((double)lnkcnt / 2) + 1 };
            }

            string[] ItemBaseParser(string[] opts)
            {
                string category = opts[0].Split(':')[1].Trim(); // 종류. 갑옷, 장갑 등
                string rarity = opts[1].Split(':')[1].Trim(); // 희귀도. 레어, 마법 등
                string name = Regex.Replace(opts[2] ?? "", @"<<set:[A-Z]+>>", "");
                bool b = opts.Length > 3 && opts[3] != ""; // 일반, 마법 등급은 3번줄의 아이템 이름(예. 소름 끼치는 조임쇠)이 없음. 4번줄이 있을 경우 true
                return new string[] { category, rarity, b ? name : "",
                    b ? Regex.Replace(opts[3] ?? "", @"<<set:[A-Z]+>>", "") : name
                };
            }

            List<string> ItemOptionParser(string opts, string tier, ref int is_deep, ref bool is_multi_line)
            {
                List<string> options = new List<string>();
                string[] tmp = opts.Split(new string[] { "\n" }, 0).Select(x => x.Trim()).ToArray();
                is_deep = tmp[0][0] == '{' && tmp.Length > 1 ? 0 : -1;

                if (tmp.Length == (is_deep == 0 ? 3 : 2))
                {
                    is_multi_line = true;
                    options.Add(tmp[0 + (is_deep == 0 ? 1 : 0)] + "\n" + tmp[1 + (is_deep == 0 ? 1 : 0)]);
                }
                if (tmp.Length == (is_deep == 0 ? 4 : 3))
                {
                    is_multi_line = true;
                    options.Add(tmp[0 + (is_deep == 0 ? 1 : 0)] + "\n" + tmp[1 + (is_deep == 0 ? 1 : 0)] + "\n" + tmp[2 + (is_deep == 0 ? 1 : 0)]);
                }

                for (int ssi = 0; ssi < tmp.Length - (is_deep == 0 ? 1 : 0); ssi++)
                {
                    options.Add(tmp[ssi + (is_deep == 0 ? 1 : 0)].RepEx(@"([0-9]+)\([0-9\.\+\-]*[0-9]+\)", "$1"));
                }

                is_deep = is_deep == 0 ? tmp[0].RepEx(@"^.+\s\(" + tier + @": ([0-9])\)\s—.+$", "$1").ToInt(0) : is_deep;
                return options;
            }

            //TODO 위키 보기시 이름만 빼자
            string map_influenced = "";
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
                    string[] ibase_info = ItemBaseParser(asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None));

                    ParserDictItem category = Array.Find(PS.Category.Entries, x => x.Text[lang] == ibase_info[0]); // category
                    string[] cate_ids = category != null ? category.Id.Split('.') : new string[] { "" }; // ex) {"id":"armour.chest","key":"armour","text":[ "갑옷", "Body Armours" ]}
                    ParserDictItem rarity = Array.Find(PS.Rarity.Entries, x => x.Text[lang] == ibase_info[1]); // rarity
                    rarity = rarity == null ? new ParserDictItem() { Id = "", Text = new string[] { ibase_info[1], ibase_info[1] } } : rarity; // ex) {"id":"rare","text":[ "희귀", "Rare" ]}

                    int optionIdx = 0;
                    double attackSpeedIncr = 0, PhysicalDamageIncr = 0;

                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> itemBaseInfo = new Dictionary<string, string>()
                    {
                        { PS.Quality.Text[lang], "" }, { PS.Level.Text[lang], "" }, { PS.ItemLevel.Text[lang], "" }, { PS.TalismanTier.Text[lang], "" }, { PS.MapTier.Text[lang], "" },
                        { PS.Sockets.Text[lang], "" }, { PS.Heist.Text[lang], "" }, { PS.MapUltimatum.Text[lang], "" }, { PS.RewardUltimatum.Text[lang], "" },
                        { PS.Radius.Text[lang], "" },  { PS.DeliriumReward.Text[lang], "" }, { PS.MonsterGenus.Text[lang], "" }, { PS.MonsterGroup.Text[lang], "" },
                        { PS.PhysicalDamage.Text[lang], "" }, { PS.ElementalDamage.Text[lang], "" }, { PS.ChaosDamage.Text[lang], "" }, { PS.AttacksPerSecond.Text[lang], "" },
                        { PS.ShaperItem.Text[lang], "" }, { PS.ElderItem.Text[lang], "" }, { PS.CrusaderItem.Text[lang], "" }, { PS.RedeemerItem.Text[lang], "" },
                        { PS.HunterItem.Text[lang], "" }, { PS.WarlordItem.Text[lang], "" }, { PS.SynthesisedItem.Text[lang], "" },
                        { PS.Corrupted.Text[lang], "" }, { PS.Unidentified.Text[lang], "" }, { PS.ProphecyItem.Text[lang], "" }, { PS.Vaal.Text[lang] + " " + ibase_info[3], "" }
                    };

                    // 시즌이 지날수록 땜질을 많이해 점점 복잡지는 소스 언제 정리하지?...
                    for (int i = 1; i < asData.Length; i++)
                    {
                        string[] asOpts = asData[i].Split(new string[] { "\r\n" }, 0).Select(x => x.Trim()).ToArray();

                        for (int j = 0; j < asOpts.Length; j++)
                        {
                            if (asOpts[j].Trim().IsEmpty()) continue;

                            int is_deep = -1;
                            bool is_multi_line = false;
                            List<string> parsedOptions = ItemOptionParser(asOpts[j], PS.OptionTier.Text[lang], ref is_deep, ref is_multi_line);

                            bool firstOption = true;
                            foreach (string parsedOption in parsedOptions)
                            {
                                firstOption = false;
                                string[] asSplit = parsedOption.Replace(@" \([\w\s]+\)", "").Split(':').Select(x => x.Trim()).ToArray();

                                if (itemBaseInfo.ContainsKey(asSplit[0]))
                                {
                                    if (itemBaseInfo[asSplit[0]] == "")
                                        itemBaseInfo[asSplit[0]] = asSplit.Length > 1 ? asSplit[1] : "_TRUE_";

                                    continue;
                                }

                                if (optionIdx < 10 && (!itemBaseInfo[PS.ItemLevel.Text[lang]].IsEmpty() || !itemBaseInfo[PS.MapUltimatum.Text[lang]].IsEmpty()))
                                {
                                    ParserDictItem special_option = null;
                                    string input = "";
                                    string ft_type = "";

                                    (input, special_option, map_influenced, ft_type) = optionToFilter(parsedOption, PS, lang, itemBaseInfo, cate_ids, special_option, asSplit);

                                    input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                    input = Regex.Replace(input, @"\\#", @"[+-]?([0-9]+\.[0-9]+|[0-9]+|\#)");

                                    bool local_exists = false;
                                    FilterDictItem filter = null;
                                    string dataLabel = null;
                                    bool hasResistance = false; // 저항
                                    double min = 99999, max = 99999;

                                    (filter, min, max, local_exists, hasResistance, dataLabel) = ParserHelper.findFilterDictItem(input, mFilter, lang, cate_ids, parsedOption, PS);
                                    if (filter != null)
                                    {
                                        updateWindow(filter, min, max, cate_ids, local_exists, lang, optionIdx, dataLabel, itemfilters, special_option, is_deep, hasResistance, ft_type, PS);

                                        attackSpeedIncr += filter.Text == PS.AttackSpeedIncr.Text[lang] && min.WithIn(1, 999) ? min : 0;
                                        PhysicalDamageIncr += filter.Text == PS.PhysicalDamageIncr.Text[lang] && min.WithIn(1, 9999) ? min : 0;

                                        optionIdx++;
                                        if (firstOption && is_multi_line) break; // break if multi lines

                                    }

                                }
                            }
                        }
                    }

                    string item_rarity = rarity.Text[0];
                    string item_name = "";
                    string item_type = "";
                    if (ibase_info[3].Contains("의 기억")) {
                        item_name = ibase_info[3];
                        item_type = ibase_info[3].Substring(0, (ibase_info[3].IndexOf("의 기억") + 4));
                    }
                    else
                    {
                        item_name = ibase_info[2];
                        item_type = ibase_info[3];
                    }


                    int alt_quality = 0;
                    bool is_blight = false;

                    bool is_map = cate_ids[0] == "map"; // || itemBaseInfo[PS.MapTier.Text[lang]] != "";
                    bool is_map_fragment = cate_ids.Length > 1 && cate_ids.Join('.') == "map.fragment";
                    bool is_map_ultimatum = itemBaseInfo[PS.MapUltimatum.Text[lang]] != "";
                    bool is_prophecy = itemBaseInfo[PS.ProphecyItem.Text[lang]] == "_TRUE_";
                    bool is_currency = rarity.Id == "currency";
                    bool is_divination_card = rarity.Id == "card";
                    bool is_gem = rarity.Id == "gem";
                    bool is_Jewel = cate_ids[0] == "jewel";
                    bool is_vaal_gem = is_gem && itemBaseInfo[PS.Vaal.Text[lang] + " " + item_type] == "_TRUE_";
                    bool is_heist = itemBaseInfo[PS.Heist.Text[lang]] != "";
                    bool is_unIdentify = itemBaseInfo[PS.Unidentified.Text[lang]] == "_TRUE_";
                    bool is_detail = is_gem || is_map_fragment || (!is_map_ultimatum && is_currency) || is_divination_card || is_prophecy;

                    int item_idx = -1;

                    ////////////////////////////////
                    /// Find item from ItemsKO.txt and ItemsEN.txt
                    ////////////////////////////////
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

                        if (is_gem)
                        {
                            for (int i = 0; i < PS.Gems.Entries.Length; i++)
                            {
                                int pos = item_type.IndexOf(PS.Gems.Entries[i].Text[lang] + " ");
                                if (pos == 0)
                                {
                                    alt_quality = i + 1;
                                    item_type = item_type.Substring(PS.Gems.Entries[i].Text[lang].Length + 1);
                                }
                            }

                            if (is_vaal_gem && itemBaseInfo[PS.Corrupted.Text[lang]] == "_TRUE_")
                            {
                                FilterDictItem entries = Array.Find(data.Entries, x => x.Text.Equals(PS.Vaal.Text[lang] + " " + item_type));
                                if (entries != null) item_type = entries.Type;
                            }
                        }
                        else if (is_map && item_type.Length > 5)
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
                            setDPS(
                                    itemBaseInfo[PS.PhysicalDamage.Text[lang]], itemBaseInfo[PS.ElementalDamage.Text[lang]], itemBaseInfo[PS.ChaosDamage.Text[lang]],
                                    item_quality, itemBaseInfo[PS.AttacksPerSecond.Text[lang]], PhysicalDamageIncr, attackSpeedIncr
                                );
                        }
                    }

                    cbName.Items.Clear();
                    bool btmp = cate_idx == -1 || item_idx == -1;
                    for (int i = 0; i < 2; i++)
                    {
                        string name = btmp || rarity.Id != "unique" ? item_name : mItems[i].Result[cate_idx].Entries[item_idx].Name;
                        string type = btmp ? item_type : mItems[i].Result[cate_idx].Entries[item_idx].Type;
                        cbName.Items.Add(new ItemNames(name, type));
                    }
                    cbName.SelectedIndex = mConfig.Options.ServerType < 1 ? lang : mConfig.Options.ServerType;
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
                    bdExchange.Visibility = !is_gem && (is_detail || bdExchange.IsEnabled) ? Visibility.Visible : Visibility.Hidden;

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

                        if (is_gem || is_Jewel || is_heist || is_map)
                        {
                            cbAltQuality.Items.Add(
                                is_heist ? "모든 강탈 가치" : (
                                    is_gem ? "모든 젬" : (
                                        is_map_ultimatum ? "모든 보상" : (is_Jewel ? "모든 반경" : "영향 없음")
                                )));

                            foreach (ParserDictItem item in (
                                is_heist ? PS.Heist : (is_gem ? PS.Gems : (
                                    is_map_ultimatum ? PS.RewardUltimatum : (is_Jewel ? RS.lRadius : PS.MapTier)
                                ))).Entries)
                            {
                                cbAltQuality.Items.Add(item.Text[lang]);
                            }

                            if (is_gem)
                            {
                                ckLv.IsChecked = itemBaseInfo[PS.Level.Text[lang]].IndexOf(" (" + PS.Max.Text[lang]) > 0;
                                ckQuality.IsChecked = item_quality.ToInt(0) > 19;
                                cbAltQuality.SelectedIndex = alt_quality;
                            }
                            else if (is_Jewel)
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
                            else if (is_map || is_map_ultimatum)
                            {
                                Synthesis.Content = "역병";

                                if (is_map_ultimatum)
                                {
                                    cbAltQuality.SelectedValue = itemBaseInfo[PS.RewardUltimatum.Text[lang]];
                                    if (cbAltQuality.SelectedIndex == -1)
                                    {
                                        cbAltQuality.Items[cbAltQuality.Items.Count - 1] = itemBaseInfo[PS.RewardUltimatum.Text[lang]];
                                        cbAltQuality.SelectedIndex = cbAltQuality.Items.Count - 1;
                                    }
                                }
                                else
                                {
                                    ckLv.IsChecked = true;
                                    ckLv.Content = "등급";
                                    tbLvMin.Text = tbLvMax.Text = itemBaseInfo[PS.MapTier.Text[lang]];
                                    cbAltQuality.SelectedValue = map_influenced != "" ? map_influenced : "영향 없음";
                                }
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
                        int[] socket = SocketParser(itemBaseInfo[PS.Sockets.Text[lang]]);
                        tbSocketMin.Text = socket[0].ToString();
                        tbLinksMin.Text = socket[1] > 0 ? socket[1].ToString() : "";
                        ckSocket.IsChecked = socket[1] > 4;
                    }

                    ////////////////////////////////////////////
                    /// BREAK POINT
                    ////////////////////////////////////////////

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
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                ForegroundMessage(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ItemOption GetItemOptions()
        {
            ItemOption itemOption = new ItemOption();

            itemOption.LangIndex = cbName.SelectedIndex;
            itemOption.Inherits = (string[])cbName.Tag; //카테고리

            itemOption.Name = (cbName.SelectedItem as ItemNames).Name;
            itemOption.Type = (cbName.SelectedItem as ItemNames).Type;

            itemOption.Influence1 = cbInfluence1.SelectedIndex;
            itemOption.Influence2 = cbInfluence2.SelectedIndex;

            // 영향은 첫번째 값이 우선 순위여야 함
            if (itemOption.Influence1 == 0 && itemOption.Influence2 != 0)
            {
                itemOption.Influence1 = itemOption.Influence2;
                itemOption.Influence2 = 0;
            }

            itemOption.Corrupt = cbCorrupt.SelectedIndex;
            itemOption.Synthesis = Synthesis.IsChecked == true;
            itemOption.ChkSocket = ckSocket.IsChecked == true;
            itemOption.ChkQuality = ckQuality.IsChecked == true;
            itemOption.ChkLv = ckLv.IsChecked == true;
            itemOption.ByCategory = ckByCategory.IsChecked == true;

            itemOption.SocketMin = tbSocketMin.Text.ToDouble(99999);
            itemOption.SocketMax = tbSocketMax.Text.ToDouble(99999);
            itemOption.LinkMin = tbLinksMin.Text.ToDouble(99999);
            itemOption.LinkMax = tbLinksMax.Text.ToDouble(99999);
            itemOption.QualityMin = tbQualityMin.Text.ToDouble(99999);
            itemOption.QualityMax = tbQualityMax.Text.ToDouble(99999);
            itemOption.LvMin = tbLvMin.Text.ToDouble(99999);
            itemOption.LvMax = tbLvMax.Text.ToDouble(99999);

            itemOption.AltQuality = cbAltQuality.SelectedIndex;
            itemOption.RarityAt = cbRarity.Items.Count > 1 ? cbRarity.SelectedIndex : 0;
            itemOption.PriceMin = tbPriceFilterMin.Text == "" ? 0 : tbPriceFilterMin.Text.ToDouble(99999);

            bool is_ultimatum = (cbRarity.SelectedValue ?? "").Equals("결전");
            itemOption.Flags = is_ultimatum ? "ULTIMATUM|" + cbAltQuality.SelectedValue : "";

            itemOption.itemfilters.Clear();

            if (!is_ultimatum && itemOption.AltQuality > 0 && itemOption.Inherits[0].WithIn("jewel", "map"))
            {
                Itemfilter itemfilter = new Itemfilter();
                itemfilter.min = itemfilter.max = 99999;
                itemfilter.disabled = false;

                if (itemOption.Inherits[0] == "jewel")
                {
                    itemfilter.type = "explicit";
                    itemfilter.stat = "stat_3642528642";
                    itemfilter.option = itemOption.AltQuality.ToString();
                }
                else
                {
                    itemfilter.type = "implicit";
                    itemfilter.stat = "stat_1792283443";
                    itemfilter.option = itemOption.AltQuality.ToString();
                }

                FilterDict filterDict = Array.Find(mFilter[itemOption.LangIndex].Result, x => x.Label == RS.lFilterType[itemfilter.type]);
                if (filterDict != null)
                {
                    FilterDictItem filter = Array.Find(filterDict.Entries, x => x.Id == itemfilter.type + "." + itemfilter.stat);
                    itemfilter.text = filter?.Text ?? "";
                    itemOption.itemfilters.Add(itemfilter);
                }
            }

            int total_res_idx = -1;

            for (int i = 0; i < 10; i++)
            {
                Itemfilter itemfilter = new Itemfilter();
                ComboBox comboBox = (ComboBox)FindName("cbOpt" + i);

                if (comboBox.SelectedIndex > -1)
                {
                    itemfilter.text = ((TextBox)FindName("tbOpt" + i)).Text.Trim();
                    itemfilter.flag = (string)((TextBox)FindName("tbOpt" + i)).Tag;
                    itemfilter.disabled = ((CheckBox)FindName("tbOpt" + i + "_2")).IsChecked != true;
                    itemfilter.min = ((TextBox)FindName("tbOpt" + i + "_0")).Text.ToDouble(99999);
                    itemfilter.max = ((TextBox)FindName("tbOpt" + i + "_1")).Text.ToDouble(99999);
                    itemfilter.option = null;

                    if (itemfilter.disabled == false && ((CheckBox)FindName("tbOpt" + i + "_3")).IsChecked == true)
                    {
                        if (total_res_idx == -1)
                        {
                            total_res_idx = itemOption.itemfilters.Count;
                            itemfilter.type = "pseudo";
                            itemfilter.stat = "pseudo_total_resistance";
                        }
                        else
                        {
                            double min = itemOption.itemfilters[total_res_idx].min;
                            itemOption.itemfilters[total_res_idx].min = (min == 99999 ? 0 : min) + (itemfilter.min == 99999 ? 0 : itemfilter.min);
                            double max = itemOption.itemfilters[total_res_idx].max;
                            itemOption.itemfilters[total_res_idx].max = (max == 99999 ? 0 : max) + (itemfilter.max == 99999 ? 0 : itemfilter.max);
                            continue;
                        }
                    }
                    else
                    {
                        itemfilter.stat = ((FilterEntrie)comboBox.SelectedItem).Stat;
                        itemfilter.type = ((FilterEntrie)comboBox.SelectedItem).Type;

                        if (itemfilter.type == "pseudo" && RS.lPseudo.ContainsKey(itemfilter.stat))
                        {
                            itemfilter.stat = RS.lPseudo[itemfilter.stat];
                        }

                        if (itemfilter.flag == "CLUSTER" || itemfilter.flag == "LOGBOOK")
                        {
                            itemfilter.option = itemfilter.min;
                            if (itemfilter.flag == "CLUSTER") itemfilter.min = 99999;
                        }
                    }

                    itemOption.itemfilters.Add(itemfilter);
                }
            }

            // 총 저항은 min 값만 필요
            if (total_res_idx > -1)
            {
                double min = itemOption.itemfilters[total_res_idx].min;
                double max = itemOption.itemfilters[total_res_idx].max;
                itemOption.itemfilters[total_res_idx].min = (min == 99999 ? 0 : min) + (max == 99999 ? 0 : max);
                itemOption.itemfilters[total_res_idx].max = 99999;
            }

            return itemOption;
        }
    }
}
