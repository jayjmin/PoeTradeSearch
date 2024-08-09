using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace PoeTradeSearch
{
    // public partial class WinMain : Window
    internal static class ItemOptionParser
    {
        internal static (Dictionary<string, string>, List<Itemfilter>, double, double, string) ParseOption(string[] asData, ParserData PS, byte lang, string itemType, string[] cate_ids, FilterData[] mFilter, WinMain winMain)
        {
            double attackSpeedIncr = 0, physicalDamageIncr = 0;
            string map_influenced = "";

            List<Itemfilter> itemfilters = new List<Itemfilter>();

            Dictionary<string, string> itemBaseInfo = new Dictionary<string, string>()
                    {
                        { PS.Quality.Text[lang], "" }, { PS.Level.Text[lang], "" }, { PS.ItemLevel.Text[lang], "" }, { PS.AreaLevel.Text[lang], "" }, { PS.TalismanTier.Text[lang], "" }, { PS.MapTier.Text[lang], "" },
                        { PS.Sockets.Text[lang], "" }, { PS.Heist.Text[lang], "" }, { PS.MapUltimatum.Text[lang], "" }, { PS.RewardUltimatum.Text[lang], "" },
                        { PS.Radius.Text[lang], "" },  { PS.DeliriumReward.Text[lang], "" }, { PS.MonsterGenus.Text[lang], "" }, { PS.MonsterGroup.Text[lang], "" },
                        { PS.PhysicalDamage.Text[lang], "" }, { PS.ElementalDamage.Text[lang], "" }, { PS.ChaosDamage.Text[lang], "" }, { PS.AttacksPerSecond.Text[lang], "" },
                        { PS.ShaperItem.Text[lang], "" }, { PS.ElderItem.Text[lang], "" }, { PS.CrusaderItem.Text[lang], "" }, { PS.RedeemerItem.Text[lang], "" },
                        { PS.HunterItem.Text[lang], "" }, { PS.WarlordItem.Text[lang], "" }, { PS.SynthesisedItem.Text[lang], "" },
                        { PS.Corrupted.Text[lang], "" }, { PS.Unidentified.Text[lang], "" }, { PS.ProphecyItem.Text[lang], "" }, { PS.Vaal.Text[lang] + " " + itemType, "" }
                    };

            // Number of actual options shown in the Window.
            int optionIdx = 0;

            // For loop to iterate copied text from the item, parse, and store into data structure
            // 시즌이 지날수록 땜질을 많이해 점점 복잡지는 소스 언제 정리하지?...
            for (int i = 1; i < asData.Length; i++)
            {
                string[] asOpts = asData[i].Split(new string[] { "\r\n" }, 0).Select(x => x.Trim()).ToArray();

                for (int j = 0; j < asOpts.Length; j++)
                {
                    if (asOpts[j].Trim().IsEmpty()) continue;

                    int is_deep = -1;
                    bool is_multi_line = false;
                    List<string> parsedOptions = ParseItemOption(asOpts[j], PS.OptionTier.Text[lang], ref is_deep, ref is_multi_line);

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

                        if (optionIdx >= 10 || itemBaseInfo[PS.ItemLevel.Text[lang]].IsEmpty() && itemBaseInfo[PS.MapUltimatum.Text[lang]].IsEmpty())
                        {
                            continue;
                        }

                        ParserDictItem special_option = null;
                        string input = "";
                        string ft_type = "";

                        string mapInfluenced = ItemParserHelper.FindMapInfluenced(parsedOption, PS, lang, cate_ids);

                        if (mapInfluenced != null)
                        {
                            map_influenced = mapInfluenced;
                            continue;
                        }

                        (input, special_option, ft_type) = ItemParserHelper.OptionToFilter(parsedOption, PS, lang, itemBaseInfo, cate_ids, special_option, asSplit);

                        input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                        input = Regex.Replace(input, @"\\#", @"[+-]?([0-9]+\.[0-9]+|[0-9]+|\#)");

                        bool local_exists = false;
                        FilterDictItem filter = null;
                        string dataLabel = null;
                        bool hasResistance = false; // 저항
                        double min = 99999, max = 99999;

                        foreach (FilterDict data_result in mFilter[lang].Result)
                        {
                            Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);
                            FilterDictItem[] entries = Array.FindAll(data_result.Entries, x => rgx.IsMatch(x.Text));

                            // "동작 속도 #% 증가" 처럼, 내부적으로는 음수로 계산할 경우를 위해
                            bool reverseFlag = FindReverseOption(PS, lang, input, data_result, ref entries);

                            // 2개 이상 같은 옵션이 있을때 장비 옵션 (특정) 만 추출
                            if (entries.Length > 1)
                            {
                                FilterDictItem[] entries_tmp = Array.FindAll(entries, x => x.Part == cate_ids[0]);
                                // 화살통 제외
                                if (entries_tmp.Length > 0 && (cate_ids.Length == 1 || cate_ids[1] != "quiver"))
                                {
                                    local_exists = true;
                                    entries = entries_tmp;
                                }
                                else
                                {
                                    entries = Array.FindAll(entries, x => x.Part == null);
                                }
                            }

                            if (entries.Length == 0)
                                continue;

                            Array.Sort(entries, delegate (FilterDictItem entrie1, FilterDictItem entrie2)
                            {
                                return (entrie2.Part ?? "").CompareTo(entrie1.Part ?? "");
                            });

                            MatchCollection matches1 = Regex.Matches(parsedOption, @"[-]?([0-9]+\.[0-9]+|[0-9]+)");
                            foreach (FilterDictItem entrie in entries)
                            {
                                string[] split_id = entrie.Id.Split('.');
                                int excludeId = Array.FindIndex(PS.ExcludeStat.Entries, x => x.Id == split_id[1] && x.Key == cate_ids[0]);
                                if (excludeId != -1)
                                    continue; // do not use the stat ID from exclude_stat defined in Parser.txt

                                if (!FindMinMax(reverseFlag, matches1, entrie, out int idxMin, out int idxMax, out bool isMin, out bool isMax))
                                    continue;

                                winMain.AddItem(optionIdx, new FilterEntrie(cate_ids[0], split_id[0], split_id[1], data_result.Label));

                                if (filter == null)
                                {
                                    filter = entrie;
                                    hasResistance = split_id.Length == 2 && RS.lResistance.ContainsKey(split_id[1]);
                                    UpdateMinMaxValue(out min, out max, reverseFlag, matches1, idxMin, idxMax, isMin, isMax);
                                }

                                break;
                            }

                        }

                        if (filter != null)
                        {
                            winMain.AddOptionItem(filter, min, max, cate_ids, local_exists, lang, optionIdx, dataLabel, itemfilters, special_option, is_deep, hasResistance, ft_type, PS);

                            attackSpeedIncr += filter.Text == PS.AttackSpeedIncr.Text[lang] && min.WithIn(1, 999) ? min : 0;
                            physicalDamageIncr += filter.Text == PS.PhysicalDamageIncr.Text[lang] && min.WithIn(1, 9999) ? min : 0;

                            optionIdx++;
                            if (firstOption && is_multi_line) break; // break if multi lines

                        }
                    }
                }
            }

            return (itemBaseInfo, itemfilters, attackSpeedIncr, physicalDamageIncr, map_influenced);

        }

        private static void UpdateMinMaxValue(out double min, out double max, bool reverseFlag, MatchCollection matches1, int idxMin, int idxMax, bool isMin, bool isMax)
        {
            if (reverseFlag)
            {
                max = isMax && matches1.Count > idxMax ? ((Match)matches1[idxMax]).Value.ToDouble(99999) * -1 : -99999;
                min = isMin && idxMax > idxMin && matches1.Count > idxMin ? ((Match)matches1[idxMin]).Value.ToDouble(99999) * -1 : 99999;
            }
            else
            {
                min = isMin && matches1.Count > idxMin ? ((Match)matches1[idxMin]).Value.ToDouble(99999) : 99999;
                max = isMax && idxMin < idxMax && matches1.Count > idxMax ? ((Match)matches1[idxMax]).Value.ToDouble(99999) : 99999;
            }
        }

        private static bool FindReverseOption(ParserData PS, byte lang, string input, FilterDict data_result, ref FilterDictItem[] entries)
        {
            bool reverseFlag = false;

            for (int l = 0; l < PS.ReverseIncreaseDecrease.Entries.Length; l++)
            {
                if (input.Contains(PS.ReverseIncreaseDecrease.Entries[l].Text[lang]))
                {
                    string input_tmp = Regex.Replace(input, PS.ReverseIncreaseDecrease.Entries[l].Text[lang], PS.ReverseIncreaseDecrease.Entries[l % 2 == 0 ? l + 1 : l - 1].Text[lang]);
                    Regex rgx_tmp = new Regex("^" + input_tmp + "$", RegexOptions.IgnoreCase);
                    FilterDictItem[] entries_tmp = Array.FindAll(data_result.Entries, x => rgx_tmp.IsMatch(x.Text));
                    if (entries_tmp.Length > 0)
                    {
                        if (entries.Length == 0)
                            reverseFlag = true;
                        entries = entries.Concat(entries_tmp).ToArray();
                    }
                    break;
                }
            }

            return reverseFlag;
        }

        private static bool FindMinMax(bool reverseFlag, MatchCollection matches1, FilterDictItem entrie, out int idxMin, out int idxMax, out bool isMin, out bool isMax)
        {
            idxMin = 0;
            idxMax = 0;
            isMin = false;
            isMax = false;
            MatchCollection matches2 = Regex.Matches(entrie.Text.Split('\n')[0], @"[-]?([0-9]+\.[0-9]+|[0-9]+|#)");

            for (int t = 0; t < matches2.Count; t++)
            {
                if (matches2[t].Value == "#")
                {
                    if (reverseFlag)
                    {
                        if (!isMax)
                        {
                            isMax = true;
                            idxMax = t;
                        }
                        else if (!isMin)
                        {
                            isMin = true;
                            idxMin = t;
                        }
                    }
                    else
                    {
                        if (!isMin)
                        {
                            isMin = true;
                            idxMin = t;
                        }
                        else if (!isMax)
                        {
                            isMax = true;
                            idxMax = t;
                        }
                    }
                }
                else if (matches1[t].Value != matches2[t].Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<string> ParseItemOption(string opts, string tier, ref int is_deep, ref bool is_multi_line)
        {
            List<string> options = new List<string>();
            string[] tmp = opts.Split(new string[] { "\n" }, 0).Select(x => x.Trim()).ToArray();
            is_deep = tmp[0][0] == '{' && tmp.Length > 1 ? 0 : -1;

            if (tmp.Length == (is_deep == 0 ? 3 : 2))
            {
                // 하나의 옵션이 2줄 이상으로 이루어진 경우 (회복량 증가+회복속도 감소)
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

        internal static int[] SocketParser(string socket)
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

        internal static string[] ItemBaseParser(string[] opts)
        {
            string category = opts[0].Split(':')[1].Trim(); // 종류. 갑옷, 장갑 등
            string rarity = opts[1].Split(':')[1].Trim(); // 희귀도. 레어, 마법 등
            string name = Regex.Replace(opts[2] ?? "", @"<<set:[A-Z]+>>", "");
            bool b = opts.Length > 3 && opts[3] != ""; // 일반, 마법 등급은 3번줄의 아이템 이름(예. 소름 끼치는 조임쇠)이 없음. 4번줄이 있을 경우 true
            return new string[] { category, rarity, b ? name : "",
                    b ? Regex.Replace(opts[3] ?? "", @"<<set:[A-Z]+>>", "") : name
                };
        }
    }
}
