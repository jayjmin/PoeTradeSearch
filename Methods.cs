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

                if (itemOptions.ByCategory || JQ.Name == "" || !(JQ.Filters.Type.Filters.Rarity.Option == "unique"))
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

                if (Inherit == "gem" && itemOptions.Name != "")
                {
                    TransfiguredGemType gemType = new TransfiguredGemType();
                    gemType.Option = itemOptions.Type;
                    gemType.Discriminator = itemOptions.Flags; // alt_x or alt_y
                    string transType = Json.Serialize<TransfiguredGemType>(gemType);
                    sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"type\":" + transType + ",");
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
