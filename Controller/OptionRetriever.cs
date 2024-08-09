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

            if (itemOption.Inherits[0] == "gem")
            {
                itemOption.Flags = cbAltQuality.SelectedValue.ToString();
            }

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
                            itemfilter.stat = "pseudo_total_elemental_resistance";
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
