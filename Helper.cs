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
    internal static class Helper
    {
        public static int selectServerLang(int serverTypeConfig, int clientLang)
        {
            // serverTypeConfig: Auto(0), Korean(1), English(2) - check 'cbServerType'
            // clientLang: Korean(0), English(1)
            if (serverTypeConfig == 1 || serverTypeConfig == 2)
            {
                return serverTypeConfig - 1;
            }
            return clientLang;
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
