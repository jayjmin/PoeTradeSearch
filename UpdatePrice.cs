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

        private Thread priceThread = null;
        private void UpdatePriceThreadWorker(ItemOption itemOptions, string[] exchange)
        {
            if (!mLockUpdatePrice)
            {
                mLockUpdatePrice = true;

                int listCount = (cbPriceListCount.SelectedIndex + 1) * 2;
                int langIndex = cbName.SelectedIndex;
                string league = mConfig.Options.League;

                mAutoSearchTimer.Stop();
                liPrice.Items.Clear();

                tkPriceCount.Text = ".";
                tkPriceInfo.Text = "시세 확인중...";
                cbPriceListTotal.Text = "0/0 검색";

                priceThread?.Interrupt();
                priceThread?.Abort();
                priceThread = new Thread(() =>
                {
                    UpdatePrice(league, langIndex,
                            exchange != null ? exchange : new string[1] { CreateJson(itemOptions, true) }, listCount
                        );

                    if (mConfig.Options.SearchAutoDelay > 0)
                    {
                        mAutoSearchTimer.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                        {
                            mAutoSearchTimerCount = mConfig.Options.SearchAutoDelay;
                            mAutoSearchTimer.Start();
                        });
                    }
                });
                priceThread.Start();
            }
        }

        private int mAutoSearchTimerCount;
        private void AutoSearchTimer_Tick(object sender, EventArgs e)
        {
            tkPriceInfo.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
            {
                if (mAutoSearchTimerCount < 1)
                {
                    mAutoSearchTimer.Stop();
                    if (liPrice.Items.Count == 0 && tkPriceCount.Text != ".")
                        tkPriceInfo.Text = (string)tkPriceInfo.Tag;
                }
                else
                {
                    mAutoSearchTimerCount--;
                    if (liPrice.Items.Count == 0 && tkPriceCount.Text != ".")
                        tkPriceInfo.Text = (string)tkPriceInfo.Tag + " (" + mAutoSearchTimerCount + ")";
                }
            });
        }

        private ParserDictItem GetExchangeItem(string id)
        {
            ParserDictItem item = Array.Find(mParser.Currency.Entries, x => x.Id == id);
            if (item == null)
                item = Array.Find(mParser.Exchange.Entries, x => x.Id == id);

            return item;
        }

        private ParserDictItem GetExchangeItem(int index, string text)
        {
            ParserDictItem item = Array.Find(mParser.Currency.Entries, x => x.Text[index] == text);
            if (item == null)
                item = Array.Find(mParser.Exchange.Entries, x => x.Text[index] == text);

            return item;
        }


        private void UpdatePrice(string league, int langIndex, string[] entity, int listCount)
        {
            string url_string = "";
            string json_entity = "";
            string msg = "정보가 없습니다";
            string msg_2 = "";

            try
            {
                if (entity.Length > 0 && !string.IsNullOrEmpty(entity[0]))
                {
                    if (entity.Length == 1)
                    {
                        url_string = RS.TradeApi[langIndex] + league;
                        json_entity = entity[0];
                    }
                    else
                    {
                        url_string = RS.ExchangeApi[langIndex] + league;
                        json_entity = "{\"exchange\":{\"status\":{\"option\":\"online\"},\"have\":[\"" + entity[0] + "\"],\"want\":[\"" + entity[1] + "\"]}}";
                    }

                    /////////////////
                    /// BREAK POINT - Price Update Error
                    // If both Korean and English does not work, check this line. Find out what is url_string and json_entity below.
                    /////////////////
                    
                    string request_result = SendHTTP(json_entity, url_string, mConfig.Options.ServerTimeout);
                    msg = "현재 리그의 거래소 접속이 원활하지 않습니다";

                    if (request_result != null)
                    {
                        ResultData resultData = Json.Deserialize<ResultData>(request_result);
                        Dictionary<string, int> currencys = new Dictionary<string, int>();

                        int total = 0;
                        int resultCount = resultData.Result.Length;

                        if (resultData.Result.Length > 0)
                        {
                            string ents0 = "", ents1 = "";

                            if (entity.Length > 1)
                            {
                                //listCount = listCount + 2;
                                ents0 = Regex.Replace(entity[0], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                ents1 = Regex.Replace(entity[1], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                            }

                            for (int x = 0; x < listCount; x++)
                            {
                                string[] tmp = new string[10];
                                int cnt = x * 10;
                                int length = 0;

                                if (cnt >= resultData.Result.Length)
                                    break;

                                for (int i = 0; i < 10; i++)
                                {
                                    if (i + cnt >= resultData.Result.Length)
                                        break;

                                    tmp[i] = resultData.Result[i + cnt];
                                    length++;
                                }

                                string json_result = "";
                                string url = RS.FetchApi[langIndex] + tmp.Join(',') + "?query=" + resultData.ID;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                                request.CookieContainer = new CookieContainer();
                                request.UserAgent = RS.UserAgent;
                                request.Timeout = mConfig.Options.ServerTimeout * 1000;
                                //request.UseDefaultCredentials = true;

                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    json_result = streamReader.ReadToEnd();
                                }

                                if (json_result != "")
                                {
                                    FetchData fetchData = new FetchData();
                                    fetchData.Result = new FetchInfo[10];

                                    fetchData = Json.Deserialize<FetchData>(json_result);

                                    for (int i = 0; i < fetchData.Result.Length; i++)
                                    {
                                        if (fetchData.Result[i] == null)
                                            break;

                                        if (fetchData.Result[i].Listing.Price != null && fetchData.Result[i].Listing.Price.Amount > 0)
                                        {
                                            string key = "";
                                            string indexed = fetchData.Result[i].Listing.Indexed;
                                            string account = fetchData.Result[i].Listing.Account.Name;
                                            string currency = fetchData.Result[i].Listing.Price.Currency;
                                            double amount = fetchData.Result[i].Listing.Price.Amount;

                                            liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                                            {
                                                ParserDictItem item = GetExchangeItem(currency);
                                                string keyName = item != null ? item.Text[0] : currency;

                                                if (entity.Length > 1)
                                                {
                                                    item = GetExchangeItem(entity[1]);
                                                    string tName2 = item != null ? item.Text[0] : entity[1];
                                                    liPrice.Items.Add(Math.Round(1 / amount, 4) + " " + tName2 + " <-> " + Math.Round(amount, 4) + " " + keyName + " [" + account + "]");
                                                }
                                                else
                                                {
                                                    liPrice.Items.Add((
                                                        String.Format(
                                                            "{0} [{1}] {2}", (amount + " " + keyName).PadRight(14, '\u2000'), account, GetLapsedTime(indexed).PadRight(10, '\u2000'))
                                                        )
                                                    );
                                                }
                                            });

                                            if (entity.Length > 1)
                                                key = amount < 1 ? Math.Round(1 / amount, 1) + " " + ents1 : Math.Round(amount, 1) + " " + ents0;
                                            else
                                                key = Math.Round(amount - 0.1) + " " + currency;

                                            if (currencys.ContainsKey(key))
                                                currencys[key]++;
                                            else
                                                currencys.Add(key, 1);

                                            total++;
                                        }
                                    }
                                }

                                if (!mLockUpdatePrice)
                                {
                                    currencys.Clear();
                                    break;
                                }
                            }

                            if (currencys.Count > 0)
                            {
                                List<KeyValuePair<string, int>> myList = new List<KeyValuePair<string, int>>(currencys);
                                string first = ((KeyValuePair<string, int>)myList[0]).Key;
                                string last = ((KeyValuePair<string, int>)myList[myList.Count - 1]).Key;

                                myList.Sort(
                                    delegate (KeyValuePair<string, int> firstPair,
                                    KeyValuePair<string, int> nextPair)
                                    {
                                        return -1 * firstPair.Value.CompareTo(nextPair.Value);
                                    }
                                );

                                for (int i = 0; i < myList.Count; i++)
                                {
                                    if (i == 2) break;
                                    if (myList[i].Value < 2) continue;
                                    msg_2 += myList[i].Key + "[" + myList[i].Value + "], ";
                                }

                                msg = Regex.Replace(first + " ~ " + last, @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                msg_2 = Regex.Replace(msg_2.TrimEnd(',', ' '), @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");

                                if (msg_2 == "") msg_2 = "가장 많은 수 없음";
                            }
                        }

                        cbPriceListTotal.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                        {
                            cbPriceListTotal.Text = total + "/" + resultCount + " 검색";
                        });

                        if (resultData.Total == 0 || currencys.Count == 0)
                        {
                            msg = mLockUpdatePrice ? "해당 물품의 거래가 없습니다" : "검색 실패: 클릭하여 다시 시도해주세요";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                mLockUpdatePrice = false;

                tkPriceCount.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    if (tkPriceCount.Text == ".") tkPriceCount.Text = ""; // 값 . 이면 읽는중 표시 끝나면 처리
                });

                tkPriceInfo.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    tkPriceInfo.Text = msg + (msg_2 != "" ? " = " + msg_2 : "");
                });

                liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    if (liPrice.Items.Count == 0)
                    {
                        liPrice.Items.Add(msg + (msg_2 != "" ? " = " + msg_2 : ""));
                    }
                    else
                    {
                        liPrice.ScrollIntoView(liPrice.Items[0]);
                    }
                });
            }
        }
    }
}
