﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

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
                // ((ComboBox)FindName("cbOpt" + i)).ItemsSource = new List<FilterEntrie>();
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

        private void setDPS(string physical, string elemental, string chaos, string quality, string perSecond, double phyDmgIncr, double speedIncr)
        {
            lbDPS.Content = ParserHelper.calcDPS(physical, elemental, chaos, quality, perSecond, phyDmgIncr, speedIncr);
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
                string[] asData = (itemText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && (asData[0].IndexOf(PS.Category.Text[0] + ": ") == 0 || asData[0].IndexOf(PS.Category.Text[1] + ": ") == 0))
                {
                    // reset window
                    ResetControls();

                    // language. 0: korean, 1: english
                    byte lang = (byte)(asData[0].IndexOf(PS.Category.Text[0] + ": ") == 0 ? 0 : 1);
                    // 베이스 찾기. 종류(갑옷, 장갑 등), 희귀도(마법, 희귀 등), 이름, 베이스로 배열이 생성되어 들어감
                    string[] ibase_info = ItemBaseParser(asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None));

                    ParserDictItem category = Array.Find(PS.Category.Entries, x => x.Text[lang] == ibase_info[0]); // category
                    string[] cate_ids = category != null ? category.Id.Split('.') : new string[] { "" }; // ex) {"id":"armour.chest","key":"armour","text":[ "갑옷", "Body Armours" ]}
                    ParserDictItem rarity = Array.Find(PS.Rarity.Entries, x => x.Text[lang] == ibase_info[1]); // rarity
                    rarity = rarity == null ? new ParserDictItem() { Id = "", Text = new string[] { ibase_info[1], ibase_info[1] } } : rarity; // ex) {"id":"rare","text":[ "희귀", "Rare" ]}

                    int k = 0;
                    double attackSpeedIncr = 0, PhysicalDamageIncr = 0;

                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> lItemOption = new Dictionary<string, string>()
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
                            List<string> options = ItemOptionParser(asOpts[j], PS.OptionTier.Text[lang], ref is_deep, ref is_multi_line);

                            for (int o = 0; o < options.Count; o++)
                            {
                                string[] asSplit = options[o].Replace(@" \([\w\s]+\)", "").Split(':').Select(x => x.Trim()).ToArray();

                                if (lItemOption.ContainsKey(asSplit[0]))
                                {
                                    if (lItemOption[asSplit[0]] == "") lItemOption[asSplit[0]] = asSplit.Length > 1 ? asSplit[1] : "_TRUE_";
                                }
                                else if (k < 10 && (!lItemOption[PS.ItemLevel.Text[lang]].IsEmpty() || !lItemOption[PS.MapUltimatum.Text[lang]].IsEmpty()))
                                {
                                    string input = options[o].RepEx(@"\s(\([a-zA-Z]+\)|—\s.+)$", "");
                                    string ft_type = options[o].Split(new string[] { "\n" }, 0)[0].RepEx(@"(.+)\s\(([a-zA-Z]+)\)$", "$2");
                                    if (!RS.lFilterType.ContainsKey(ft_type)) ft_type = "_none_"; // 영향력 검사???

                                    bool hasResistance = false; // 저항
                                    double min = 99999, max = 99999;
                                    ParserDictItem special_option = null;

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
                                        continue;
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
                                        else if (ft_type == "_none_" && lItemOption[PS.Radius.Text[lang]] != "")
                                        {
                                            special_option = Array.Find(PS.Radius.Entries, x => x.Text[lang] == asSplit[0]);
                                            if (special_option != null)
                                            {
                                                lItemOption[PS.Radius.Text[lang]] = RS.lRadius.Entries[special_option.Id.ToInt() - 1].Text[lang];
                                                special_option.Key = "RADIUS";
                                            }
                                        }
                                    }

                                    input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                    input = Regex.Replace(input, @"\\#", @"[+-]?([0-9]+\.[0-9]+|[0-9]+|\#)");

                                    bool local_exists = false;
                                    FilterDictItem filter = null;
                                    string dataLabel = null;

                                    /////////////////////
                                    // Split
                                    /////////////////////
                                    (filter, min, max, local_exists, hasResistance, dataLabel) = ParserHelper.findFilterDictItem(input, mFilter, lang, cate_ids, options[o], PS);
                                    if (filter != null)
                                    {
                                        string[] split_id = filter.Id.Split('.');

                                        (FindName("cbOpt" + k) as ComboBox).Items.Add(new FilterEntrie(cate_ids[0], split_id[0], split_id[1], dataLabel));


                                        Dictionary<string, SolidColorBrush> color = new Dictionary<string, SolidColorBrush>()
                                        {
                                            { "implicit", Brushes.DarkRed }, { "crafted", Brushes.Blue }, { "enchant", Brushes.Blue }, { "scourge", Brushes.DarkOrange }
                                        };

                                        SetFilterObjectColor(k, color.ContainsKey(ft_type) ? color[ft_type] : SystemColors.ActiveBorderBrush);

                                        (FindName("cbOpt" + k) as ComboBox).SelectedValue = RS.lFilterType["pseudo"];
                                        if ((FindName("cbOpt" + k) as ComboBox).SelectedValue == null)
                                        {
                                            if (split_id.Length == 2 && RS.lPseudo.ContainsKey(split_id[1]))
                                                (FindName("cbOpt" + k) as ComboBox).Items.Add(new FilterEntrie(cate_ids[0], "pseudo", split_id[1], RS.lFilterType["pseudo"]));
                                        }

                                        if ((FindName("cbOpt" + k) as ComboBox).Items.Count == 1)
                                        {
                                            (FindName("cbOpt" + k) as ComboBox).SelectedIndex = 0;
                                        }
                                        else
                                        {
                                            string tmp_type = !local_exists && mConfig.Options.AutoSelectPseudo ? "pseudo" : ft_type;
                                            (FindName("cbOpt" + k) as ComboBox).SelectedValue = RS.lFilterType.ContainsKey(tmp_type) ? RS.lFilterType[tmp_type] : "_none_";

                                            if ((FindName("cbOpt" + k) as ComboBox).SelectedValue == null)
                                            {
                                                foreach (string type in new string[] { ft_type, "explicit", "fractured" })
                                                {
                                                    (FindName("cbOpt" + k) as ComboBox).SelectedValue = RS.lFilterType.ContainsKey(type) ? RS.lFilterType[type] : "_none_";
                                                    if ((FindName("cbOpt" + k) as ComboBox).SelectedValue != null) break;
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

                                        itemfilters.Add(new Itemfilter
                                        {
                                            stat = split_id[1],
                                            type = filter.Type,
                                            text = filter.Text,
                                            max = max,
                                            min = min,
                                            disabled = true
                                        });

                                        (FindName("tbOpt" + k) as TextBox).Text = (is_deep > 0 ? is_deep.ToString() + ") " : "") + filter.Text;

                                        (FindName("tbOpt" + k + "_3") as CheckBox).Visibility = hasResistance ? Visibility.Visible : Visibility.Hidden;
                                        if ((FindName("tbOpt" + k + "_3") as CheckBox).Visibility == Visibility.Visible && mConfig.Options.AutoCheckTotalres)
                                            (FindName("tbOpt" + k + "_3") as CheckBox).IsChecked = true;

                                        if (special_option != null && (special_option.Key == "CLUSTER" || special_option.Key == "LOGBOOK"))
                                        {
                                            (FindName("tbOpt" + k) as TextBox).Text = special_option.Text[lang];
                                            (FindName("tbOpt" + k) as TextBox).Tag = special_option.Key;
                                            (FindName("tbOpt" + k + "_0") as TextBox).IsEnabled = false;
                                            (FindName("tbOpt" + k + "_1") as TextBox).IsEnabled = false;
                                            (FindName("tbOpt" + k + "_2") as CheckBox).IsChecked = true;
                                            itemfilters[itemfilters.Count - 1].min = min = special_option.Id.ToInt();
                                            itemfilters[itemfilters.Count - 1].max = max = 99999;
                                            if (itemfilters.Count > 0) // 군 주얼 패시브 갯수 자동 체크
                                            {
                                                (FindName("tbOpt0_2") as CheckBox).IsChecked = true;
                                                itemfilters[0].disabled = false;
                                            }
                                        }

                                        if (Array.Find(mParser.Disable.Entries, x => x.Id.Equals(split_id[1])) != null)
                                        {
                                            (FindName("tbOpt" + k + "_2") as CheckBox).IsChecked = false;
                                            (FindName("tbOpt" + k + "_2") as CheckBox).IsEnabled = false;
                                        }
                                        else
                                        {
                                            if (ft_type != "implicit" && (is_deep < 1 || is_deep < 3) &&
                                                (mChecked.Entries?.Find(x => x.Id.Equals(split_id[1]) && x.Key.IndexOf(cate_ids[0] + "/") > -1) != null))
                                            {
                                                (FindName("tbOpt" + k + "_2") as CheckBox).BorderThickness = new Thickness(2);
                                                (FindName("tbOpt" + k + "_2") as CheckBox).IsChecked = true;
                                                itemfilters[itemfilters.Count - 1].disabled = false;
                                            }
                                        }

                                        (FindName("tbOpt" + k + "_0") as TextBox).Text = min == 99999 ? "" : min.ToString();
                                        (FindName("tbOpt" + k + "_1") as TextBox).Text = max == 99999 ? "" : max.ToString();

                                        attackSpeedIncr += filter.Text == PS.AttackSpeedIncr.Text[lang] && min.WithIn(1, 999) ? min : 0;
                                        PhysicalDamageIncr += filter.Text == PS.PhysicalDamageIncr.Text[lang] && min.WithIn(1, 9999) ? min : 0;

                                        string[] strs_tmp = (FindName("tbOpt" + k) as TextBox).Text.Split('\n');
                                        if (strs_tmp.Length > 1)
                                        {
                                            (FindName("tbOpt" + k) as TextBox).Text = strs_tmp[0];
                                            for (int ssi = 1; ssi < strs_tmp.Length; ssi++)
                                            {
                                                k++;
                                                SetFilterObjectVisibility(k, Visibility.Hidden);
                                                (FindName("tbOpt" + k) as TextBox).Text = strs_tmp[ssi];
                                                (FindName("tbOpt" + k + "_2") as CheckBox).IsChecked = false;
                                                ((ComboBox)FindName("cbOpt" + k)).Items.Clear();
                                                SetFilterObjectColor(k, color.ContainsKey(ft_type) ? color[ft_type] : SystemColors.ActiveBorderBrush);
                                            }
                                        }

                                        if (ft_type == "_none_" && (FindName("cbOpt" + k) as ComboBox).SelectedIndex > -1 &&
                                            (string)(FindName("cbOpt" + k) as ComboBox).SelectedValue != RS.lFilterType["explicit"] &&
                                            (string)(FindName("cbOpt" + k) as ComboBox).SelectedValue != RS.lFilterType["pseudo"])
                                        {
                                            SetFilterObjectColor(k, Brushes.Pink);
                                        }

                                        k++;
                                        if (o == 0 && is_multi_line) break; // break if multi lines
                                    }
                                }
                            }
                        }
                    }

                    string item_rarity = rarity.Text[0];
                    string item_name = "";
                    string item_type = "";
                    bool is_memory = false;
                    if (ibase_info[3].Contains("의 기억")) {
                        item_name = ibase_info[3];
                        item_type = ibase_info[3].Substring(0, (ibase_info[3].IndexOf("의 기억") + 4));
                        is_memory = true;
                    }
                    else
                    {
                        item_name = ibase_info[2];
                        item_type = ibase_info[3];
                    }


                    int alt_quality = 0;
                    bool is_blight = false;

                    bool is_map = cate_ids[0] == "map"; // || lItemOption[PS.MapTier.Text[lang]] != "";
                    bool is_map_fragment = cate_ids.Length > 1 && cate_ids.Join('.') == "map.fragment";
                    bool is_map_ultimatum = lItemOption[PS.MapUltimatum.Text[lang]] != "";
                    bool is_prophecy = lItemOption[PS.ProphecyItem.Text[lang]] == "_TRUE_";
                    bool is_currency = rarity.Id == "currency";
                    bool is_divination_card = rarity.Id == "card";
                    bool is_gem = rarity.Id == "gem";
                    bool is_Jewel = cate_ids[0] == "jewel";
                    bool is_vaal_gem = is_gem && lItemOption[PS.Vaal.Text[lang] + " " + item_type] == "_TRUE_";
                    bool is_heist = lItemOption[PS.Heist.Text[lang]] != "";
                    bool is_unIdentify = lItemOption[PS.Unidentified.Text[lang]] == "_TRUE_";
                    bool is_detail = is_gem || is_map_fragment || (!is_map_ultimatum && is_currency) || is_divination_card || is_prophecy;

                    int item_idx = -1;
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
                    else if (lItemOption[PS.MonsterGenus.Text[lang]] != "" && lItemOption[PS.MonsterGroup.Text[lang]] != "")
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

                            if (is_vaal_gem && lItemOption[PS.Corrupted.Text[lang]] == "_TRUE_")
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
                            if (!lItemOption[PS.DeliriumReward.Text[lang]].IsEmpty() && itemfilters.Count > 0)
                            {
                                (FindName("tbOpt0_2") as CheckBox).IsChecked = true;
                                (FindName("tbOpt0_0") as TextBox).Text = "";
                                itemfilters[0].disabled = false;
                                itemfilters[0].min = 99999;
                            }
                        }
                        else if (lItemOption[PS.SynthesisedItem.Text[lang]] == "_TRUE_")
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

                    string item_quality = Regex.Replace(lItemOption[PS.Quality.Text[lang]], "[^0-9]", "");
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
                                    lItemOption[PS.PhysicalDamage.Text[lang]], lItemOption[PS.ElementalDamage.Text[lang]], lItemOption[PS.ChaosDamage.Text[lang]],
                                    item_quality, lItemOption[PS.AttacksPerSecond.Text[lang]], PhysicalDamageIncr, attackSpeedIncr
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
                        tbLvMin.Text = Regex.Replace(lItemOption[is_gem ? PS.Level.Text[lang] : PS.ItemLevel.Text[lang]], "[^0-9]", "");
                        tbQualityMin.Text = item_quality;

                        string[] Influences = { PS.ShaperItem.Text[lang], PS.ElderItem.Text[lang], PS.CrusaderItem.Text[lang], PS.RedeemerItem.Text[lang], PS.HunterItem.Text[lang], PS.WarlordItem.Text[lang] };
                        for (int i = 0; i < Influences.Length; i++)
                        {
                            if (lItemOption[Influences[i]] == "_TRUE_")
                                cbInfluence1.SelectedIndex = i + 1;
                        }

                        for (int i = 0; i < Influences.Length; i++)
                        {
                            if (cbInfluence1.SelectedIndex != (i + 1) && lItemOption[Influences[i]] == "_TRUE_")
                                cbInfluence2.SelectedIndex = i + 1;
                        }

                        if (lItemOption[PS.Corrupted.Text[lang]] == "_TRUE_")
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
                                ckLv.IsChecked = lItemOption[PS.Level.Text[lang]].IndexOf(" (" + PS.Max.Text[lang]) > 0;
                                ckQuality.IsChecked = item_quality.ToInt(0) > 19;
                                cbAltQuality.SelectedIndex = alt_quality;
                            }
                            else if (is_Jewel)
                            {
                                cbAltQuality.SelectedItem = lItemOption[PS.Radius.Text[lang]];
                                if (cbAltQuality.SelectedIndex == -1)
                                {
                                    cbAltQuality.Items.Clear();
                                    cbAltQuality.Items.Add(lItemOption[PS.Radius.Text[lang]] ?? "");
                                    cbAltQuality.SelectedIndex = 0;
                                }
                            }
                            else if (is_heist)
                            {
                                string tmp = Regex.Replace(lItemOption[PS.Heist.Text[lang]], @".+ \(([^\)]+)\)$", "$1");
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
                                    cbAltQuality.SelectedValue = lItemOption[PS.RewardUltimatum.Text[lang]];
                                    if (cbAltQuality.SelectedIndex == -1)
                                    {
                                        cbAltQuality.Items[cbAltQuality.Items.Count - 1] = lItemOption[PS.RewardUltimatum.Text[lang]];
                                        cbAltQuality.SelectedIndex = cbAltQuality.Items.Count - 1;
                                    }
                                }
                                else
                                {
                                    ckLv.IsChecked = true;
                                    ckLv.Content = "등급";
                                    tbLvMin.Text = tbLvMax.Text = lItemOption[PS.MapTier.Text[lang]];
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

                    if (lItemOption[PS.Sockets.Text[lang]] != "")
                    {
                        int[] socket = SocketParser(lItemOption[PS.Sockets.Text[lang]]);
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
                        Synthesis.IsChecked = (is_map && is_blight) || lItemOption[PS.SynthesisedItem.Text[lang]] == "_TRUE_";
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

        private string CreateJson(ItemOption itemOptions, bool useSaleType)
        {
            string BeforeDayToString(int day)
            {
                if (day < 1)
                    return "any";
                else if (day < 3)
                    return "1day";
                else if (day < 7)
                    return "3days";
                else if (day < 14)
                    return "1week";
                return "2weeks";
            }

            try
            {
                JsonData jsonData = new JsonData();
                jsonData.Query = new q_Query();
                q_Query JQ = jsonData.Query;

                jsonData.Sort.Price = "asc";

                byte lang_index = (byte)itemOptions.LangIndex;
                string Inherit = itemOptions.Inherits.Length > 0 ? itemOptions.Inherits[0] : "any";

                JQ.Name = itemOptions.Name;
                JQ.Type = itemOptions.Type;

                JQ.Stats = new q_Stats[0];
                JQ.Status.Option = "online";

                JQ.Filters.Type.Filters.Category.Option = Inherit == "jewel" ? Inherit : itemOptions.Inherits.Join('.');
                JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? mParser.Rarity.Entries[itemOptions.RarityAt - 1].Id : "any";
                //JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? RS.lRarity.ElementAt(itemOptions.RarityAt - 1).Key.ToLower() : "any";

                JQ.Filters.Trade.Disabled = mConfig.Options.SearchBeforeDay == 0;
                JQ.Filters.Trade.Filters.Indexed.Option = BeforeDayToString(mConfig.Options.SearchBeforeDay);
                JQ.Filters.Trade.Filters.SaleType.Option = useSaleType ? "priced" : "any";
                JQ.Filters.Trade.Filters.Price.Max = 99999;
                JQ.Filters.Trade.Filters.Price.Min = itemOptions.PriceMin > 0 ? itemOptions.PriceMin : 99999;

                JQ.Filters.Socket.Disabled = itemOptions.ChkSocket != true;

                JQ.Filters.Socket.Filters.Links.Min = itemOptions.LinkMin;
                JQ.Filters.Socket.Filters.Links.Max = itemOptions.LinkMax;
                JQ.Filters.Socket.Filters.Sockets.Min = itemOptions.SocketMin;
                JQ.Filters.Socket.Filters.Sockets.Max = itemOptions.SocketMax;

                JQ.Filters.Misc.Filters.Quality.Min = itemOptions.ChkQuality == true ? itemOptions.QualityMin : 99999;
                JQ.Filters.Misc.Filters.Quality.Max = itemOptions.ChkQuality == true ? itemOptions.QualityMax : 99999;

                JQ.Filters.Misc.Filters.Ilvl.Min = itemOptions.ChkLv != true || Inherit == "gem" || Inherit == "map" ? 99999 : itemOptions.LvMin;
                JQ.Filters.Misc.Filters.Ilvl.Max = itemOptions.ChkLv != true || Inherit == "gem" || Inherit == "map" ? 99999 : itemOptions.LvMax;
                JQ.Filters.Misc.Filters.Gem_level.Min = itemOptions.ChkLv == true && Inherit == "gem" ? itemOptions.LvMin : 99999;
                JQ.Filters.Misc.Filters.Gem_level.Max = itemOptions.ChkLv == true && Inherit == "gem" ? itemOptions.LvMax : 99999;
                JQ.Filters.Misc.Filters.AlternateQuality.Option = Inherit == "gem" && itemOptions.AltQuality > 0 ? itemOptions.AltQuality.ToString() : "any";


                /////////////////////////////
                /// BREAK POINT
                /////////////////////////////

                JQ.Filters.Misc.Filters.Shaper.Option = Inherit != "map" && (itemOptions.Influence1 == 1 || itemOptions.Influence2 == 1) ? "true" : "any";
                JQ.Filters.Misc.Filters.Elder.Option = Inherit != "map" && (itemOptions.Influence1 == 2 || itemOptions.Influence2 == 2) ? "true" : "any";
                JQ.Filters.Misc.Filters.Crusader.Option = Inherit != "map" && (itemOptions.Influence1 == 3 || itemOptions.Influence2 == 3) ? "true" : "any";
                JQ.Filters.Misc.Filters.Redeemer.Option = Inherit != "map" && (itemOptions.Influence1 == 4 || itemOptions.Influence2 == 4) ? "true" : "any";
                JQ.Filters.Misc.Filters.Hunter.Option = Inherit != "map" && (itemOptions.Influence1 == 5 || itemOptions.Influence2 == 5) ? "true" : "any";
                JQ.Filters.Misc.Filters.Warlord.Option = Inherit != "map" && (itemOptions.Influence1 == 6 || itemOptions.Influence2 == 6) ? "true" : "any";

                JQ.Filters.Misc.Filters.Synthesis.Option = Inherit != "map" && itemOptions.Synthesis == true ? "true" : "any";
                JQ.Filters.Misc.Filters.Corrupted.Option = itemOptions.Corrupt == 1 ? "true" : (itemOptions.Corrupt == 2 ? "false" : "any");

                JQ.Filters.Heist.Filters.HeistObjective.Option = "any";
                if (Inherit == "heistmission" && itemOptions.AltQuality > 0)
                {
                    string[] tmp = new string[] { "moderate", "high", "precious", "priceless" };
                    JQ.Filters.Heist.Filters.HeistObjective.Option = tmp[itemOptions.AltQuality - 1];
                }

                JQ.Filters.Heist.Disabled = JQ.Filters.Heist.Filters.HeistObjective.Option == "any";

                JQ.Filters.Misc.Disabled = !(
                    itemOptions.ChkQuality == true || itemOptions.Corrupt > 0 || (Inherit == "gem" && itemOptions.AltQuality > 0)
                    || (Inherit != "map" && (itemOptions.Influence1 != 0 || itemOptions.ChkLv == true || itemOptions.Synthesis == true))
                );

                JQ.Filters.Map.Disabled = !(
                    Inherit == "map" && (itemOptions.AltQuality > 0 || itemOptions.ChkLv == true || itemOptions.Synthesis == true || itemOptions.Influence1 != 0)
                );

                JQ.Filters.Map.Filters.Tier.Min = itemOptions.ChkLv == true && Inherit == "map" ? itemOptions.LvMin : 99999;
                JQ.Filters.Map.Filters.Tier.Max = itemOptions.ChkLv == true && Inherit == "map" ? itemOptions.LvMax : 99999;
                JQ.Filters.Map.Filters.Shaper.Option = Inherit == "map" && itemOptions.Influence1 == 1 ? "true" : "any";
                JQ.Filters.Map.Filters.Elder.Option = Inherit == "map" && itemOptions.Influence1 == 2 ? "true" : "any";
                JQ.Filters.Map.Filters.Blight.Option = Inherit == "map" && itemOptions.Synthesis == true ? "true" : "any";

                JQ.Filters.Ultimatum.Disabled = !(itemOptions.AltQuality > 0 && itemOptions.Flags.IndexOf("ULTIMATUM|") == 0);
                if (!JQ.Filters.Ultimatum.Disabled && itemOptions.AltQuality > 0)
                {
                    JQ.Filters.Ultimatum.Filters.Reward.Option = mParser.RewardUltimatum.Entries[itemOptions.AltQuality - 1].Id;
                    JQ.Filters.Ultimatum.Filters.Output.Option = JQ.Filters.Ultimatum.Filters.Reward.Option == "ExchangeUnique" ? itemOptions.Flags.Split('|')[1] : "any";
                }

                bool error_filter = false;

                if (itemOptions.itemfilters.Count > 0)
                {
                    JQ.Stats = new q_Stats[1];
                    JQ.Stats[0] = new q_Stats();
                    JQ.Stats[0].Type = "and";
                    JQ.Stats[0].Filters = new q_Stats_filters[itemOptions.itemfilters.Count];

                    int idx = 0;

                    for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                    {
                        string input = itemOptions.itemfilters[i].text;
                        string stat = itemOptions.itemfilters[i].stat;
                        string type = itemOptions.itemfilters[i].type;

                        if (input.Trim() != "" && RS.lFilterType.ContainsKey(type))
                        {
                            string type_name = RS.lFilterType[type];

                            FilterDict filterDict = Array.Find(mFilter[lang_index].Result, x => x.Label == type_name);

                            if (filterDict != null)
                            {
                                // 무기에 경우 pseudo_adds_[a-lang]+_damage 옵션은 공격 시 가 붙음
                                if (Inherit == "weapon" && type == "pseudo" && Regex.IsMatch(stat, @"^pseudo_adds_[a-z]+_damage$"))
                                {
                                    stat += "_to_attacks";
                                }

                                FilterDictItem filter = Array.Find(filterDict.Entries, x => x.Id == type + "." + stat);

                                JQ.Stats[0].Filters[idx] = new q_Stats_filters();
                                JQ.Stats[0].Filters[idx].Value = new q_Min_And_Max();

                                if (filter != null && (filter.Id ?? "").Trim() != "")
                                {
                                    JQ.Stats[0].Filters[idx].Disabled = itemOptions.itemfilters[i].disabled == true;
                                    JQ.Stats[0].Filters[idx].Value.Min = itemOptions.itemfilters[i].min;
                                    JQ.Stats[0].Filters[idx].Value.Max = itemOptions.itemfilters[i].max;
                                    JQ.Stats[0].Filters[idx].Value.Option = itemOptions.itemfilters[i].option;
                                    JQ.Stats[0].Filters[idx++].Id = filter.Id;
                                }
                                else
                                {
                                    error_filter = true;
                                    itemOptions.itemfilters[i].isNull = true;

                                    // 오류 방지를 위해 널값시 아무거나 추가 
                                    JQ.Stats[0].Filters[idx].Id = "temp_ids";
                                    JQ.Stats[0].Filters[idx].Value.Min = JQ.Stats[0].Filters[idx].Value.Max = 99999;
                                    JQ.Stats[0].Filters[idx++].Disabled = true;
                                }
                            }
                        }
                    }
                }

                //if (!ckSocket.Dispatcher.CheckAccess())
                //else if (ckSocket.Dispatcher.CheckAccess())

                string sEntity = Json.Serialize<JsonData>(jsonData);

                if (itemOptions.ByCategory || JQ.Name == "" || JQ.Filters.Type.Filters.Rarity.Option != "unique")
                {
                    sEntity = sEntity.Replace("\"name\":\"" + JQ.Name + "\",", "");

                    /////////////////
                    /// BREAK POINT 1
                    /////////////////
                    if (Inherit == "jewel" || itemOptions.ByCategory)
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "");
                    else if (Inherit == "prophecy" || JQ.Filters.Type.Filters.Category.Option == "monster.sample")
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"" + (Inherit == "prophecy" ? "name" : "term") + "\":\"" + JQ.Type + "\",");
                }

                sEntity = sEntity.RepEx("\"(min|max)\":99999|\"option\":(0|\"any\"|null)", "").RepEx("\"[a-z_]+\":{[,]*}", "");
                sEntity = sEntity.RepEx(",{2,}", ",").RepEx("({),{1,}", "$1").RepEx(",{1,}(}|])", "$1");

                if (error_filter)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        (ThreadStart)delegate ()
                        {
                            for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                            {
                                if (itemOptions.itemfilters[i].isNull)
                                {
                                    ((TextBox)FindName("tbOpt" + i)).Background = System.Windows.Media.Brushes.Red;
                                    ((TextBox)FindName("tbOpt" + i + "_0")).Text = "error";
                                    ((TextBox)FindName("tbOpt" + i + "_1")).Text = "error";
                                    ((CheckBox)FindName("tbOpt" + i + "_2")).IsChecked = false;
                                    ((CheckBox)FindName("tbOpt" + i + "_2")).IsEnabled = false;
                                    ((CheckBox)FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                                }
                            }
                        }
                    );
                }

                return sEntity;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                ForegroundMessage(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }
        }

    }
}
