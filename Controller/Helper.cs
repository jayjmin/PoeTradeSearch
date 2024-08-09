using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PoeTradeSearch
{
    internal static class Helper
    {

        public static (string, ParserDictItem, string) OptionToFilter(string parsedOption, ParserData PS, byte lang, Dictionary<string, string> itemBaseInfo, string[] cate_ids, ParserDictItem special_option, string[] asSplit)
        {
            string input = parsedOption.RepEx(@"\s(\([a-zA-Z]+\)|—\s.+)$", "");
            string ft_type = parsedOption.Split(new string[] { "\n" }, 0)[0].RepEx(@"(.+)\s\(([a-zA-Z]+)\)$", "$2");
            if (!RS.lFilterType.ContainsKey(ft_type)) ft_type = "_none_"; // 영향력 검사???


            if (special_option == null)
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

            return (input, special_option, ft_type);
        }

        public static string FindMapInfluenced(string parsedOption, ParserData PS, byte lang, string[] cate_ids)
        {
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
                    return match.Groups[2] + "";
                }
            }
            return null;
        }

        public static int SelectServerLang(int serverTypeConfig, int clientLang)
        {
            // serverTypeConfig: Auto(0), Korean(1), English(2) - check 'cbServerType'
            // clientLang: Korean(0), English(1)
            if (serverTypeConfig == 1 || serverTypeConfig == 2)
            {
                return serverTypeConfig - 1;
            }
            return clientLang;
        }

        public static string CalcDPS(string physical, string elemental, string chaos, string quality, string perSecond, double phyDmgIncr, double speedIncr)
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
