using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using static PoeTradeSearch.Native;
using static System.Net.WebRequestMethods;

namespace PoeTradeSearch
{
    internal static class ParserHelper
    {
        public static (FilterDictItem, double, double, bool, bool, string) findFilterDictItem(string input, FilterData[] mFilter, int lang, string[] cate_ids, string option, ParserData PS)
        {
            FilterDictItem filter = null;
            bool local_exists = false;
            bool hasResistance = false;
            double min = 99999, max = 99999;
            string dataLabel = null;

            foreach (FilterDict data_result in mFilter[lang].Result)
            {
                Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);
                FilterDictItem[] entries = Array.FindAll(data_result.Entries, x => rgx.IsMatch(x.Text));
                // "동작 속도 #% 증가" 처럼, 내부적으로는 음수로 계산할 경우를 위해
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
                            if (entries.Length == 0) reverseFlag = true;
                            entries = entries.Concat(entries_tmp).ToArray();
                        }
                        break; 
                    }
                }

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

                if (entries.Length > 0)
                {
                    Array.Sort(entries, delegate (FilterDictItem entrie1, FilterDictItem entrie2)
                    {
                        return (entrie2.Part ?? "").CompareTo(entrie1.Part ?? "");
                    });

                    MatchCollection matches1 = Regex.Matches(option, @"[-]?([0-9]+\.[0-9]+|[0-9]+)");
                    foreach (FilterDictItem entrie in entries)
                    {
                        int idxMin = 0, idxMax = 0;
                        bool isMin = false, isMax = false;
                        bool isMatch = true;

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
                                isMatch = false;
                                break;
                            }
                        }

                        if (isMatch)
                        {
                            dataLabel = data_result.Label;
                            string[] id_split = entrie.Id.Split('.');

                            if (filter == null)
                            {
                                filter = entrie;
                                hasResistance = id_split.Length == 2 && RS.lResistance.ContainsKey(id_split[1]);
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

                            break;
                        }
                    }
                }

            }
            return (filter, min, max, local_exists, hasResistance, dataLabel);

        }
        public static string calcDPS(string physical, string elemental, string chaos, string quality, string perSecond, double phyDmgIncr, double speedIncr)
        {
            // DPS 계산 POE-TradeMacro 참고
            double physicalDPS = DamageToDPS(physical);
            double elementalDPS = DamageToDPS(elemental);
            double chaosDPS = DamageToDPS(chaos);

            double quality20Dps = quality == "" ? 0 : quality.ToDouble(0);
            double attacksPerSecond = Regex.Replace(perSecond, "[^0-9.]", "").ToDouble(0);

            if (speedIncr > 0)
            {
                double baseAttackSpeed = attacksPerSecond / (speedIncr / 100 + 1);
                double modVal = baseAttackSpeed % 0.05;
                baseAttackSpeed += modVal > 0.025 ? (0.05 - modVal) : -modVal;
                attacksPerSecond = baseAttackSpeed * (speedIncr / 100 + 1);
            }

            physicalDPS = (physicalDPS / 2) * attacksPerSecond;
            elementalDPS = (elementalDPS / 2) * attacksPerSecond;
            chaosDPS = (chaosDPS / 2) * attacksPerSecond;

            //20 퀄리티 보다 낮을땐 20 퀄리티 기준으로 계산
            quality20Dps = quality20Dps < 20 ? physicalDPS * (phyDmgIncr + 120) / (phyDmgIncr + quality20Dps + 100) : 0;
            physicalDPS = quality20Dps > 0 ? quality20Dps : physicalDPS;

            return "DPS: P." + Math.Round(physicalDPS, 2).ToString() +
                            " + E." + Math.Round(elementalDPS, 2).ToString() +
                            " = T." + Math.Round(physicalDPS + elementalDPS + chaosDPS, 2).ToString();
        }
        private static double DamageToDPS(string damage)
        {
            double dps = 0;
            try
            {
                string[] stmps = Regex.Replace(damage, @"\([a-zA-Z]+\)", "").Split(',');
                for (int t = 0; t < stmps.Length; t++)
                {
                    string[] maidps = (stmps[t] ?? "").Trim().Split('-');
                    if (maidps.Length == 2)
                        dps += double.Parse(maidps[0].Trim()) + double.Parse(maidps[1].Trim());
                }
            }
            catch { }
            return dps;
        }
    }
}
