using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static System.Windows.Forms.AxHost;

namespace PoeTradeSearch
{
    public partial class WinMain : Window
    {
        private const int DEFAULT = 99999;
        private const string PSEUDO_STAT_CHAOS_RES = "pseudo_total_chaos_resistance";
        private const string PSEUDO_STAT_ELEM_RES = "pseudo_total_elemental_resistance";

        private double ValueOrZero(double valueWithDefault)
        {
            return valueWithDefault == DEFAULT ? 0 : valueWithDefault;
        }

        private int PseudoResistanceMultiplierElem(string stat)
        {
            if (!RS.lResistance.ContainsKey(stat)) {
                return 1;
            }
            return RS.lResistance[stat].Item1;
        }

        private int PseudoResistanceMultiplierChaos(string stat)
        {
            if (!RS.lResistance.ContainsKey(stat))
            {
                return 0;
            }
            if (RS.lResistance[stat].Item2)
                return 1;
            return 0;
        }

        private ItemOption GetItemOptions()
        {
            ItemOption itemOption = new ItemOption
            {
                LangIndex = cbName.SelectedIndex,
                Inherits = (string[])cbName.Tag, //카테고리

                Name = (cbName.SelectedItem as ItemNames).Name,
                Type = (cbName.SelectedItem as ItemNames).Type,

                Influence1 = cbInfluence1.SelectedIndex,
                Influence2 = cbInfluence2.SelectedIndex
            };

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

            itemOption.SocketMin = tbSocketMin.Text.ToDouble(DEFAULT);
            itemOption.SocketMax = tbSocketMax.Text.ToDouble(DEFAULT);
            itemOption.LinkMin = tbLinksMin.Text.ToDouble(DEFAULT);
            itemOption.LinkMax = tbLinksMax.Text.ToDouble(DEFAULT);
            itemOption.QualityMin = tbQualityMin.Text.ToDouble(DEFAULT);
            itemOption.QualityMax = tbQualityMax.Text.ToDouble(DEFAULT);
            itemOption.LvMin = tbLvMin.Text.ToDouble(DEFAULT);
            itemOption.LvMax = tbLvMax.Text.ToDouble(DEFAULT);

            itemOption.AltQuality = cbAltQuality.SelectedIndex;
            itemOption.RarityAt = cbRarity.Items.Count > 1 ? cbRarity.SelectedIndex : 0;
            itemOption.PriceMin = tbPriceFilterMin.Text == "" ? 0 : tbPriceFilterMin.Text.ToDouble(DEFAULT);

            bool is_ultimatum = (cbRarity.SelectedValue ?? "").Equals("결전");
            itemOption.Flags = is_ultimatum ? "ULTIMATUM|" + cbAltQuality.SelectedValue : "";

            if (itemOption.Inherits[0] == "gem")
            {
                itemOption.Flags = cbAltQuality.SelectedValue.ToString();
            }

            itemOption.itemfilters.Clear();

            if (!is_ultimatum && itemOption.AltQuality > 0 && itemOption.Inherits[0].WithIn("jewel", "map"))
            {
                Itemfilter itemfilter = new Itemfilter();
                itemfilter.min = itemfilter.max = DEFAULT;
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

            Dictionary<string, int> pseudoStatIndex = new Dictionary<string, int>();

            for (int i = 0; i < 10; i++)
            {
                ComboBox comboBox = (ComboBox)FindName("cbOpt" + i);
                if (comboBox.SelectedIndex <= -1)
                    continue;

                Itemfilter itemfilter = NewItemFilter(i);

                if (itemfilter.disabled == false && ((CheckBox)FindName("tbOpt" + i + "_3")).IsChecked == true)
                {
                    // For pseudo resistances, sum up all res into pseudo filter.
                    itemfilter.min = ValueOrZero(itemfilter.min);
                    itemfilter.max = ValueOrZero(itemfilter.max);
                    string stat = ((FilterEntrie)comboBox.SelectedItem).Stat;

                    int elemMulti = PseudoResistanceMultiplierElem(stat);
                    if (elemMulti > 0)
                    {
                        pseudoStatIndex = UpsertPseudoItemFilter(itemOption, pseudoStatIndex, PSEUDO_STAT_ELEM_RES, i, itemfilter, elemMulti);
                    }

                    int chaosMulti = PseudoResistanceMultiplierChaos(stat);
                    if (chaosMulti > 0)
                    {
                        pseudoStatIndex = UpsertPseudoItemFilter(itemOption, pseudoStatIndex, PSEUDO_STAT_CHAOS_RES, i, itemfilter, chaosMulti);
                    }
                }
                else
                {
                    itemfilter.stat = ((FilterEntrie)comboBox.SelectedItem).Stat;
                    itemfilter.type = ((FilterEntrie)comboBox.SelectedItem).Type;

                    if (itemfilter.type == "pseudo" && RS.lPseudo.ContainsKey(itemfilter.stat))
                    {
                        // Replace with pseudo stat.
                        itemfilter.min = ValueOrZero(itemfilter.min);
                        itemfilter.max = ValueOrZero(itemfilter.max);

                        string pseudoStatName = RS.lPseudo[itemfilter.stat];
                        pseudoStatIndex = UpsertPseudoItemFilter(itemOption, pseudoStatIndex, pseudoStatName, i, itemfilter, 1);
                    }
                    else
                    {
                        if (itemfilter.flag == "CLUSTER" || itemfilter.flag == "LOGBOOK")
                        {
                            itemfilter.option = itemfilter.min;
                            if (itemfilter.flag == "CLUSTER") itemfilter.min = DEFAULT;
                        }
                        itemOption.itemfilters.Add(itemfilter);
                    }
                }
            }

            foreach (int pseudoIdx in pseudoStatIndex.Values)
            {
                if (itemOption.itemfilters[pseudoIdx].max == 0)
                    itemOption.itemfilters[pseudoIdx].max = 99999;
            }

            return itemOption;
        }

        private Dictionary<string, int> UpsertPseudoItemFilter(ItemOption itemOption, Dictionary<string, int> pseudoStatIndex, string pseudoStatName, int optionIdx, Itemfilter filter, double multiplier)
        {
            if (!pseudoStatIndex.ContainsKey(pseudoStatName))
            {
                Itemfilter pseudoFilter = NewItemFilter(optionIdx);
                pseudoFilter.type = "pseudo";
                pseudoFilter.stat = pseudoStatName;
                pseudoFilter.min = 0;
                pseudoFilter.max = 0;

                itemOption.itemfilters.Add(pseudoFilter);
                pseudoStatIndex[pseudoStatName] = itemOption.itemfilters.Count - 1;
            }
            int pseudoFilterIdx = pseudoStatIndex[pseudoStatName];
            itemOption.itemfilters[pseudoFilterIdx].min += filter.min * multiplier;
            itemOption.itemfilters[pseudoFilterIdx].max += filter.max * multiplier;

            return pseudoStatIndex;
        }

        private Itemfilter NewItemFilter(int optionIdx)
        {
            Itemfilter itemfilter = new Itemfilter
            {
                text = ((TextBox)FindName("tbOpt" + optionIdx)).Text.Trim(),
                flag = (string)((TextBox)FindName("tbOpt" + optionIdx)).Tag,
                disabled = ((CheckBox)FindName("tbOpt" + optionIdx + "_2")).IsChecked != true,
                min = ((TextBox)FindName("tbOpt" + optionIdx + "_0")).Text.ToDouble(DEFAULT),
                max = ((TextBox)FindName("tbOpt" + optionIdx + "_1")).Text.ToDouble(DEFAULT),
                option = null
            };
            return itemfilter;
        }
    }
}
