#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion
//////
// pjsStrategyBase is a clean code template to get your started building NT strategies from scracth, in source.
// this strategy uses unmanaged orders -- make sure you understand the implications by referring to the ninjatrader documentation
// follow me on Twitter @pjsmith
// Buy me a coffee if you like https://ko-fi.com/pjsmith
//////

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{

    public class pjsStrategyBaseTemplate : Strategy
    {
        public enum Cat
        {
            FixeCtBreakEven,
            VariableCt
        }
        public enum ButtonsPosition
        {
            BottomRight,
            TopRight,
            TopLeft,
            BottomLeft
        }
        public enum ButtonsAlign
        {
            Horizontal,
            Vertical
        }
		private System.Windows.Controls.Button closeButton;
		private System.Windows.Controls.Button BEButton;
		private System.Windows.Controls.Button flatButton;
		private System.Windows.Controls.Button EnableButton;
        private System.Windows.Controls.Button LongButton;
        private System.Windows.Controls.Button ShortButton;
		private System.Windows.Controls.Grid myGrid;

        double URpl = 0;                 // unrealised P&L
        double Rpl  = 0;                 // realised P&L
        int TradeEntryBar = 0;
        private NinjaTrader.Gui.Tools.SimpleFont myFont;
        #region HA series
        private Series<double> HALow;
        private Series<double> HAHigh;
        private Series<double> HAOpen;
        private Series<double> HAClose;
        #endregion
        #region external indies
        private RSI RSI1;
        private jtEconNews2a econNews;
        #endregion
		#region Current order state
        private Order myOrder = null;
		private OrderState myOrderState;
		private Order myOrderTP = null;
		private Order myOrderSL = null;
        private double myAvgPrice = 0;
        private int myQty = 0;
        #endregion
        #region Last order state
        private int LastorderLS = 0;
        private int LastorderWasStop = 0;
        private int LastOrderTrailed = 0;
        private int LastOrderTPHit = 0;

        #endregion
        private double OrderTP;
		private double OrderSL;
        private bool Enabled = true;
        private int CurrentBar0;
        private double lastprice = 0;
         private double totalvalue = 0;
         private int positionQty = 0;
          private int size = 0;
        private double lastEntry = 0;
        private int lastEntryBar = 0;
        private int sessionBar = 0;
        private bool BESet = false;
        private string AdditionalStatusText = "";
        #region strategy specific declerations go in here (keep the code base tidy)

        #endregion
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"PJS Custom strategy template.";
                Name = "pjsStrategyBaseTemplate";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.ByStrategyPosition;
                BarsRequiredToTrade = 20;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = false;
                IsUnmanaged = true;

				GoTime					= DateTime.Parse("2:00 PM");                        // times in your local time zone
				EndTime					= DateTime.Parse("8:45 PM");
                LongsEnabled = true;
                ShortsEnabled = true;
                SL = 80;
                TP = 160;
                Qty = 1;

				UseCloseAtFloatingProfit                    = true;
				FloatingProfit                              = 250;
				UseCloseAtFloatingLoss                      = true;
				FloatingLoss                                = -400;
				//Stop trading if the Account Cash Value Target is reached ($)
                AccountCashValueTarget                      = 55000;
				AccountCashValueDrawDown                    = 500;
                UseTrailingStop                             = false;

                ShowStatusText                                = true;
                #region optimisation defaults
                RSIPeriod = 28;
                #endregion
                AddPlot(Brushes.Black,"AvgPrice");

				Period					= 100;
				TSL						= 400;
                StopToBE                = 26;
            }
            else if (State == State.Configure)
            {

				try // try to get the font from the chart (may not exist if using strategy analyzer)
				{
					myFont = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", 12) { Size = 12, Bold = true };
				}
				catch
				{
					myFont = new NinjaTrader.Gui.Tools.SimpleFont("Arial", 12) { Size = 12, Bold = true };
				}
                AddDataSeries(BarsPeriodType.Tick,1);
                AddDataSeries(BarsPeriodType.Day,1);
            }
            else if (State == State.DataLoaded)
            {
                // setup HA bar series
                if (UseHABars)
                {
                    HALow = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
                    HAHigh = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
                    HAOpen = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
                    HAClose = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
                }
                // initialise indicators
                RSI1 =  RSI(RSIPeriod,3);//pjsRSI(false,RSIPeriod,3,50,0,0,false,false,5,50,5,false,false,0,"","",false,false,false,false,false,72,22,0,false,false,false,true,0,0,1,0,GoTime,EndTime,HistogramTypes.RSI,CalculationTypes.RMI,0,0,10,0,"",@"4D47D-M49KW-CW3H4-DNE9D-XMPQA");
                #region initialise non base indicators for stategy here

                #endregion
            }
            else if (State == State.Historical)
			{
				if (UserControlCollection.Contains(myGrid))
					return;

				Dispatcher.InvokeAsync((() =>
				{
					switch(buttonpos)
					{
						case(ButtonsPosition.TopRight):
						myGrid = new System.Windows.Controls.Grid
						{
							Name = "MyCustomGrid", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top
						};
						break;

						case(ButtonsPosition.TopLeft):
						myGrid = new System.Windows.Controls.Grid
						{
							Name = "MyCustomGrid", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
						};
						break;

						case(ButtonsPosition.BottomRight):
						myGrid = new System.Windows.Controls.Grid
						{
							Name = "MyCustomGrid", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom
						};
						break;

						case(ButtonsPosition.BottomLeft):
						myGrid = new System.Windows.Controls.Grid
						{
							Name = "MyCustomGrid", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom
						};
						break;
					}

					System.Windows.Controls.ColumnDefinition column1 = new System.Windows.Controls.ColumnDefinition();
					System.Windows.Controls.ColumnDefinition column2 = new System.Windows.Controls.ColumnDefinition();
					System.Windows.Controls.ColumnDefinition column3 = new System.Windows.Controls.ColumnDefinition();
					System.Windows.Controls.ColumnDefinition column4 = new System.Windows.Controls.ColumnDefinition();
					System.Windows.Controls.ColumnDefinition column5 = new System.Windows.Controls.ColumnDefinition();
					System.Windows.Controls.ColumnDefinition column6 = new System.Windows.Controls.ColumnDefinition();
					System.Windows.Controls.RowDefinition row1 = new System.Windows.Controls.RowDefinition();
					System.Windows.Controls.RowDefinition row2 = new System.Windows.Controls.RowDefinition();
					System.Windows.Controls.RowDefinition row3 = new System.Windows.Controls.RowDefinition();
					System.Windows.Controls.RowDefinition row4 = new System.Windows.Controls.RowDefinition();
					System.Windows.Controls.RowDefinition row5 = new System.Windows.Controls.RowDefinition();
					System.Windows.Controls.RowDefinition row6 = new System.Windows.Controls.RowDefinition();

					myGrid.ColumnDefinitions.Add(column1);
					myGrid.ColumnDefinitions.Add(column2);
					myGrid.ColumnDefinitions.Add(column3);
					myGrid.ColumnDefinitions.Add(column4);
					myGrid.ColumnDefinitions.Add(column5);
					myGrid.ColumnDefinitions.Add(column6);
					myGrid.RowDefinitions.Add(row1);
					myGrid.RowDefinitions.Add(row2);
					myGrid.RowDefinitions.Add(row3);
					myGrid.RowDefinitions.Add(row4);
					myGrid.RowDefinitions.Add(row5);
					myGrid.RowDefinitions.Add(row6);


					flatButton = new System.Windows.Controls.Button();
					flatButton.Name = "FlatButton";
					flatButton.Content = "FLAT";
					flatButton.Background = Brushes.Transparent;
					flatButton.Foreground = Brushes.BlueViolet;
					flatButton.Opacity = 1;
					flatButton.AllowDrop = true;
					flatButton.BorderBrush = Brushes.BlueViolet;
					flatButton.FontFamily = new FontFamily("Arial");
					flatButton.FontSize = 12;
					flatButton.FontWeight = FontWeights.ExtraBold;
					flatButton.BorderThickness = new Thickness(3);

					closeButton = new System.Windows.Controls.Button();
					closeButton.Name = "CloseButton";
					closeButton.Content = "CLOSE";
					closeButton.Background = Brushes.Transparent;
					closeButton.Foreground = Brushes.Green;
					closeButton.Opacity = 1;
					closeButton.AllowDrop = true;
					closeButton.BorderBrush = Brushes.Green;
					closeButton.FontFamily = new FontFamily("Arial");
					closeButton.FontSize = 12;
					closeButton.FontWeight = FontWeights.ExtraBold;
					closeButton.BorderThickness = new Thickness(3);

					BEButton = new System.Windows.Controls.Button();
					BEButton.Name = "BEButton";
					BEButton.Content = "BE";
					BEButton.Background = Brushes.Transparent;
					BEButton.Foreground = Brushes.Tomato;
					BEButton.Opacity = 1;
					BEButton.AllowDrop = true;
					BEButton.BorderBrush = Brushes.Tomato;
					BEButton.FontFamily = new FontFamily("Arial");
					BEButton.FontSize = 12;
					BEButton.FontWeight = FontWeights.ExtraBold;
					BEButton.BorderThickness = new Thickness(3);

					EnableButton = new System.Windows.Controls.Button();
					EnableButton.Name = "EnableButton";
					EnableButton.Content = "On";
					EnableButton.Background = Brushes.Transparent;
					EnableButton.Foreground = Brushes.Lime;
					EnableButton.Opacity = 1;
					EnableButton.AllowDrop = true;
					EnableButton.BorderBrush = Brushes.Lime;
					EnableButton.FontFamily = new FontFamily("Arial");
					EnableButton.FontSize = 12;
					EnableButton.FontWeight = FontWeights.ExtraBold;
					EnableButton.BorderThickness = new Thickness(3);

					LongButton = new System.Windows.Controls.Button();
					LongButton.Name = "LongButton";
					LongButton.Content = "LONG";
					LongButton.Background = Brushes.Transparent;
					LongButton.Foreground = Brushes.Lime;
					LongButton.Opacity = 1;
					LongButton.AllowDrop = true;
					LongButton.BorderBrush = Brushes.Lime;
					LongButton.FontFamily = new FontFamily("Arial");
					LongButton.FontSize = 12;
					LongButton.FontWeight = FontWeights.ExtraBold;
					LongButton.BorderThickness = new Thickness(3);

					ShortButton = new System.Windows.Controls.Button();
					ShortButton.Name = "ShortButton";
					ShortButton.Content = "SHORT";
					ShortButton.Background = Brushes.Transparent;
					ShortButton.Foreground = Brushes.Lime;
					ShortButton.Opacity = 1;
					ShortButton.AllowDrop = true;
					ShortButton.BorderBrush = Brushes.Lime;
					ShortButton.FontFamily = new FontFamily("Arial");
					ShortButton.FontSize = 12;
					ShortButton.FontWeight = FontWeights.ExtraBold;
					ShortButton.BorderThickness = new Thickness(3);


					closeButton.Click += OnButtonClick;
					BEButton.Click += OnButtonClick;
					flatButton.Click += OnButtonClick;
					EnableButton.Click += OnButtonClick;
                    LongButton.Click += OnButtonClick;
                    ShortButton.Click += OnButtonClick;

					switch(buttonal)
					{
					case (ButtonsAlign.Horizontal):
						System.Windows.Controls.Grid.SetColumn(flatButton, 0);
						System.Windows.Controls.Grid.SetColumn(closeButton, 1);
						System.Windows.Controls.Grid.SetColumn(BEButton, 2);
						System.Windows.Controls.Grid.SetColumn(EnableButton, 3);
                        System.Windows.Controls.Grid.SetColumn(LongButton, 4);
                        System.Windows.Controls.Grid.SetColumn(ShortButton, 5);

					break;
					case (ButtonsAlign.Vertical):
						System.Windows.Controls.Grid.SetRow(flatButton, 0);
						System.Windows.Controls.Grid.SetRow(closeButton, 1);
						System.Windows.Controls.Grid.SetRow(BEButton, 2);
						System.Windows.Controls.Grid.SetRow(EnableButton, 3);
                        System.Windows.Controls.Grid.SetRow(LongButton, 4);
                        System.Windows.Controls.Grid.SetRow(ShortButton, 5);
					break;
					}

					myGrid.Children.Add(flatButton);
					myGrid.Children.Add(closeButton);
					myGrid.Children.Add(BEButton);
					myGrid.Children.Add(EnableButton);
                    myGrid.Children.Add(LongButton);
                    myGrid.Children.Add(ShortButton);

					UserControlCollection.Add(myGrid);
				}));
			}
			else if (State == State.Terminated)
			{
				Dispatcher.InvokeAsync((() =>
				{
					if (myGrid != null)
					{
						if (flatButton != null)
						{
							myGrid.Children.Remove(flatButton);
							flatButton.Click -= OnButtonClick;
							flatButton = null;
						}
						if (closeButton != null)
						{
							myGrid.Children.Remove(closeButton);
							closeButton.Click -= OnButtonClick;
							closeButton = null;
						}
						if (BEButton != null)
						{
							myGrid.Children.Remove(BEButton);
							BEButton.Click -= OnButtonClick;
							BEButton = null;
						}
						if (EnableButton != null)
						{
							myGrid.Children.Remove(EnableButton);
							EnableButton.Click -= OnButtonClick;
							EnableButton = null;
						}
						if (LongButton != null)
						{
							myGrid.Children.Remove(LongButton);
							LongButton.Click -= OnButtonClick;
							LongButton = null;
						}
						if (ShortButton != null)
						{
							myGrid.Children.Remove(ShortButton);
							ShortButton.Click -= OnButtonClick;
							ShortButton = null;
						}
					}
				}));
			}
        }

        private void OnButtonClick(object sender, RoutedEventArgs rea)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			if (button == closeButton && button.Name == "CloseButton" && button.Content == "CLOSE")
			{
					if (Position.MarketPosition == MarketPosition.Long)
					{
						ChangeOrder(myOrderTP, myOrderTP.Quantity, 0, myOrderSL.StopPrice);
					}
					else if (Position.MarketPosition == MarketPosition.Short)
					{
						ChangeOrder(myOrderTP, myOrderTP.Quantity, 0, myOrderSL.StopPrice);
					}
				return;
			}

			if (button == BEButton && button.Name == "BEButton" && button.Content == "BE")
			{
					if(Position.MarketPosition == MarketPosition.Long && lastprice > (myAvgPrice + (1*TickSize)))
					{
						ChangeOrder(myOrderSL, myOrderSL.Quantity, 0, myAvgPrice + (1*TickSize));
					}
					else if(Position.MarketPosition == MarketPosition.Short && lastprice < (myAvgPrice - (1*TickSize)))
					{
						ChangeOrder(myOrderSL, myOrderSL.Quantity, 0, myAvgPrice - (1*TickSize));
					}
                    BESet = true;
				return;
			}

			if (button == flatButton && button.Name == "FlatButton" && button.Content == "FLAT")
			{
					if(Position.MarketPosition == MarketPosition.Long && lastprice < (myAvgPrice))
					{
					ChangeOrder(myOrderTP, myOrderSL.Quantity, 0, myAvgPrice + (1*TickSize));
					}
					else if (Position.MarketPosition == MarketPosition.Short && lastprice > (myAvgPrice))
					{
					ChangeOrder(myOrderTP, myOrderSL.Quantity, 0,myAvgPrice - (1*TickSize));
					}
				return;
			}

			if (button == EnableButton && button.Name == "EnableButton" && button.Content == "On")
			{
				Enabled = false;
				EnableButton.Content = "Off";
				EnableButton.Foreground = Brushes.Red;
				EnableButton.BorderBrush = Brushes.Red;
				return;
			}
			if (button == EnableButton && button.Name == "EnableButton" && button.Content == "Off")
			{
				Enabled = true;
				EnableButton.Content = "On";
				EnableButton.Foreground = Brushes.Lime;
				EnableButton.BorderBrush = Brushes.Lime;
				return;
			}

			if (button == ShortButton && button.Name == "ShortButton" && button.Content == "SHORT")
			{
				Enabled = false;
				ShortButton.Content = "SHORT(d)";
				ShortButton.Foreground = Brushes.Red;
				ShortButton.BorderBrush = Brushes.Red;
                ShortsEnabled = false;
				return;
			}
			if (button == ShortButton && button.Name == "ShortButton" && button.Content == "SHORT(d)")
			{
				Enabled = true;
				ShortButton.Content = "SHORT";
				ShortButton.Foreground = Brushes.Lime;
				ShortButton.BorderBrush = Brushes.Lime;
                ShortsEnabled = true;
				return;
			}
			if (button == LongButton && button.Name == "LongButton" && button.Content == "LONG")
			{
				Enabled = false;
				LongButton.Content = "LONG(d)";
				LongButton.Foreground = Brushes.Red;
				LongButton.BorderBrush = Brushes.Red;
                LongsEnabled = false;
				return;
			}
			if (button == LongButton && button.Name == "LongButton" && button.Content == "LONG(d)")
			{
				Enabled = true;
				LongButton.Content = "LONG";
				LongButton.Foreground = Brushes.Lime;
				LongButton.BorderBrush = Brushes.Lime;
                LongsEnabled = true;
				return;
			}
		}

		private bool TradingEnabled(DateTime t)
		{
			if (t.TimeOfDay > GoTime.TimeOfDay && t.TimeOfDay < EndTime.TimeOfDay)
				return true;

			return false;
		}

        private void ProcessTick()
        {

            return;
        }
        private void ProcessDay()
        {

            return;
        }
        private bool CheckForShort()
        {
            bool result = false;
            if (!ShortsEnabled)
                return false;
            // trade logic for SHORT trade
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (RSI1[0] < 40  && Close[0] < Open[0] )
                {
                    result = true;
                }
            }
            //
            return result;
        }
        private bool CheckForLong()
        {
            bool result = false;
            if (!LongsEnabled)
                return false;
            // trade logic for LONG trade
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (RSI1[0] > 60 && Close[0] > Open[0] )
                {
                    result = true;
                }
            }
            //
            return result;
        }
        private bool CheckExitShort()
        {
            bool result = false;
            // exit logic for SHORT trade

            //
            return result;
        }
        private bool CheckExitLong()
        {
            bool result = false;
            // exit logic for LONG trade

            //
            return result;
        }
        private bool CheckConditions()
        {
            bool result = false;
            //

            //
            return result;
        }
        private void ShowStatus()
        {
            if (ChartControl != null)
                Draw.TextFixed(this,"pjsStrategybase", "\nUnrealised PnL : " + URpl.ToString("#0.00").PadLeft(5)
                        +"\nRealised PnL   : " + Rpl.ToString("#0.00").PadLeft(5)
                        + "\n\nCurrent Position\nUnrealised PnL : " + URpl.ToString("#0.00").PadLeft(5)
                        +"\n"+Position.MarketPosition.ToString().PadRight(15)+": "+Position.Quantity.ToString().PadLeft(5)
                        +"\nBars           : "+(Position.MarketPosition != MarketPosition.Flat ? (CurrentBar-TradeEntryBar).ToString().PadLeft(5) : "-".PadLeft(5))
                        +AdditionalStatusText,TextPosition.TopLeft,Brushes.LightGray,myFont,Position.MarketPosition == MarketPosition.Short ? Brushes.Red : Position.MarketPosition == MarketPosition.Long ? Brushes.LimeGreen : Brushes.DimGray,Brushes.Black,70);
        }
        protected override void OnBarUpdate()
        {
            if (BarsInProgress == 1)    ProcessTick();
            if (BarsInProgress == 1)    ProcessDay();
            if (BarsInProgress != 0)
                return;
            #region start HABars
            if (UseHABars)
            {
                if (CurrentBar == 0)
                {
                    HAOpen[0] = Open[0];
                    HAHigh[0] = High[0];
                    HALow[0] = Low[0];
                    HAClose[0] = Close[0];
                }
                else
                {
                    HAOpen[0] = ((HAOpen[1] + HAClose[1]) * 0.5); // Calculate the open
                    HAClose[0] = ((Open[0] + High[0] + Low[0] + Close[0]) * 0.25); // Calculate the close
                    HAHigh[0] = (Math.Max(High[0], HAOpen[0])); // Calculate the high
                    HALow[0] = (Math.Min(Low[0], HAOpen[0])); // Calculate the low
                }
            }
            #endregion
            #region set some common vars
            CurrentBar0 = CurrentBar;
            lastprice = Close[0];
            #endregion

            bool GetLong = false;
            bool GetShort = false;
            bool DoExitLong = false;
            bool DoExitShort = false;
            bool IsGoTime = TradingEnabled(Time[0]);
            if (Bars.IsFirstBarOfSession && IsFirstTickOfBar)   sessionBar = 0;
            if (IsFirstTickOfBar) sessionBar ++;

		    if (UseCloseAtFloatingLoss||UseCloseAtFloatingProfit)    PnL();

            if (BarsInProgress == 0)                                                                                                        // our primary data series
            {
                //Print(CurrentBar+":Enabled="+Enabled+":"+":IsGoTime="+IsGoTime+":RSI1="+RSI1.Value[0].ToString("N1"));
                if (Enabled && (IsGoTime || myQty != 0))
                {
                    #region trade entry conditions / logic (edit in functions to keep codebase clean)
                    CheckConditions();              // sets up any vars needed for trade descisions/overall logic
                    GetLong = CheckForLong();
                    GetShort = CheckForShort();
                    #endregion

                    #region take trades
                    if (GetLong)
                    {
                        LastorderLS = 1;
                        LastOrderTrailed = 0; LastOrderTPHit = 0;
                        lastEntryBar = CurrentBar;
                        SubmitOrderUnmanaged(1, OrderAction.Buy, OrderType.Market, Qty, 0, 0, "", "B");
                        if (UseScaleIn > 0)
                        {
                            for (int o=1; o<UseScaleIn; o++)
                            {
                                SubmitOrderUnmanaged(1, OrderAction.Buy, OrderType.Limit, Qty, MasterInstrument.RoundPrice(Close[0]-((20/UseScaleIn)*o), TickSize), 0, "", "B");
                            }
                        }
                        BESet = false;
                    }
                    else if (GetShort)
                    {
                        LastorderLS = -1;
                        LastOrderTrailed = 0; LastOrderTPHit = 0;
                        lastEntryBar = CurrentBar;
                        SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.Market, Qty, 0, 0, "", "S");
                        if (UseScaleIn > 0)
                        {
                            for (int o=1; o<UseScaleIn; o++)
                            {
                                SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.Limit, Qty, MasterInstrument.RoundPrice(Close[0]+((20/UseScaleIn)*o), TickSize), 0, "", "S");
                            }
                        }
                        BESet = false;
                    }
                    #endregion
                }
                else
                {
                    // optionally close positions when time runs out
                    if (Position.MarketPosition == MarketPosition.Long)         DoExitLong  = true;
                    else if (Position.MarketPosition == MarketPosition.Short)   DoExitShort = true;
                }
                if (Position.MarketPosition != MarketPosition.Flat)        // check existing open positions
                {
                    if (Position.MarketPosition == MarketPosition.Long)         DoExitLong  = CheckExitLong();
                    else if (Position.MarketPosition == MarketPosition.Short)   DoExitShort = CheckExitShort();
                }
            }
            if ((DoExitLong || DoExitShort))
            {
                LastorderWasStop = 0;
                if (DoExitLong && Position.MarketPosition == MarketPosition.Long)
                    ChangeOrder(myOrderTP, myOrderTP.Quantity, 0, myOrderSL.StopPrice);
                if (DoExitShort && Position.MarketPosition == MarketPosition.Short)
                    ChangeOrder(myOrderTP, myOrderTP.Quantity, 0, myOrderSL.StopPrice);
            }
            else
            {
                if (UseTrailingStop &&  Position.MarketPosition != MarketPosition.Flat)
                {
                    lock(Account.Orders)
                    {
                        foreach (Order Worder in Account.Orders) //this will cancel all orders on the account.
                        {
                            if (Worder.Name.Contains("TS") && (Worder.OrderState == OrderState.Working || Worder.OrderState== OrderState.TriggerPending || (Worder.OrderState == OrderState.Accepted && Worder.OrderType == OrderType.StopMarket)))
                            {
                                // Print("FOUND STOP ORDER " +Worder.ToString());
                                if (Worder.Name == "TSLB" && (Worder.OrderAction == OrderAction.Sell || Worder.OrderAction == OrderAction.SellShort) && Worder.StopPrice < lastprice-(TrailStopStartsTicks*TickSize))
                                {
                                    // Print("TRAILING STOPL");
                                    ChangeOrder(Worder, Worder.Quantity, 0,  lastprice-(TSL*TickSize));
                                    Draw.Square(this,"TS-"+CurrentBar.ToString(),false,0,lastprice-(TSL*TickSize),Brushes.Red);
                                    LastOrderTrailed = 1;
                                }
                                if (Worder.Name == "TSLS" &&  (Worder.OrderAction == OrderAction.BuyToCover || Worder.OrderAction == OrderAction.Buy) && Worder.StopPrice > lastprice+(TrailStopStartsTicks*TickSize))
                                {
                                    // Print("TRAILING STOPS");
                                    ChangeOrder(Worder, Worder.Quantity, 0, lastprice+(TSL*TickSize));
                                    Draw.Square(this,"TS-"+CurrentBar.ToString(),false,0,lastprice-(TSL*TickSize),Brushes.Red);
                                    LastOrderTrailed = 1;
                                }
                            }
                        }
                    }
                }
                if (StopToBE > 0 && !BESet)
                {
                    if(Position.MarketPosition == MarketPosition.Long && lastprice > (myAvgPrice + StopToBE + (1*TickSize)))
					{
						ChangeOrder(myOrderSL, myOrderSL.Quantity, 0, myAvgPrice + (1*TickSize));
                        BESet = true;
					}
					else if(Position.MarketPosition == MarketPosition.Short && lastprice < (myAvgPrice - StopToBE - (1*TickSize)))
					{
						ChangeOrder(myOrderSL, myOrderSL.Quantity, 0, myAvgPrice - (1*TickSize));
                        BESet = true;
					}

                }

            }
            if (ShowStatusText) ShowStatus();
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
            //Print(order.ToString());
            if ( order.Name == "B" || order.Name == "S" )
			{
                myOrder = order;
                myOrderState = orderState;
            }
			else if( order.Name.Contains("TP") )
			{
                myOrderTP = order;
                if (orderState == OrderState.Filled) LastOrderTPHit = 1;
            }
			else if( order.Name.Contains("SL") )
			{
                myOrderSL = order;
                if (LastOrderTrailed == 0 && orderState == OrderState.Filled)  LastorderWasStop = 1;
            }

            if ( order.Name == "B" && orderState == OrderState.Filled )
			{
                OrderTP = (averageFillPrice + ( TP * TickSize ));
                OrderSL = (averageFillPrice - ( SL * TickSize ));
                TradeEntryBar = CurrentBar0;
                lastEntry = averageFillPrice;
                // Print("Placing TP and STOP orders");
                SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.MIT, quantity, 0, OrderTP, "OCO"+order.OrderId, "TP");
                SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.StopMarket, quantity, 0, OrderSL, "OCO"+order.OrderId, UseTrailingStop ? "TSLB" : "SLB");
            }
			else if( order.Name == "S" && orderState == OrderState.Filled )
			{
                OrderTP = (averageFillPrice - ( TP * TickSize ));
                OrderSL = (averageFillPrice + ( SL * TickSize ));
                TradeEntryBar = CurrentBar0;
                lastEntry = averageFillPrice;
                // Print("Placing TP and STOP orders");
                SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.MIT, quantity, 0, OrderTP, "OCO"+order.OrderId, "TP");
                SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.StopMarket, quantity, 0, OrderSL, "OCO"+order.OrderId, UseTrailingStop ? "TSLS" : "SLS");
			}
            else if ( order.Name.Contains("TSB") && orderState == OrderState.Filled )
			{
                OrderTP = (averageFillPrice + ( TP * TickSize ));
                OrderSL = (averageFillPrice - ( SL * TickSize ));
                //TradeEntryBar = CurrentBar0;
				//lastEntry = averageFillPrice;
                //SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.MIT, quantity, 0, OrderTP, "OCO"+order.OrderId, "TP");
                //SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.StopMarket, quantity, 0, OrderSL, "OCO"+order.OrderId, "SL");
            }
			else if( order.Name.Contains("TSS") && orderState == OrderState.Filled )
			{
                OrderTP = (averageFillPrice - ( TP * TickSize ));
                OrderSL = (averageFillPrice + ( SL * TickSize ));
                //TradeEntryBar = CurrentBar0;
				//lastEntry = averageFillPrice;
                //SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.MIT, quantity, 0, OrderTP, "OCO"+order.OrderId, "TP");
                //SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.StopMarket, quantity, 0, OrderSL, "OCO"+order.OrderId, "SL");
			}
            else if (orderState == OrderState.Filled && Position.MarketPosition == MarketPosition.Flat)
            {
                // cancel any working orders
                lock(Account.Orders)
				{
                    foreach (Order Worder in Account.Orders) //this will cancel all orders on the account.
                    {
                        if(Worder.OrderState == OrderState.Working || Worder.OrderState == OrderState.Accepted)
                        {
                            CancelOrder(Worder);
                        }
                    }
                }
            }
		}

        protected override void OnPositionUpdate(Cbi.Position position, double averagePrice,
            int quantity, Cbi.MarketPosition marketPosition)
        {
            if (position.MarketPosition == MarketPosition.Flat)
            {
                lock(Account.Orders)
				{
                    // cancel any working orders
                    foreach (Order Worder in Account.Orders) //this will cancel all orders on the account.
                    {
                        if(Worder.OrderState == OrderState.Working || Worder.OrderState == OrderState.Accepted && ( Worder.Name == "B" || Worder.Name == "S" ))
                        {
                            CancelOrder(Worder);
                        }
                    }
                }
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            // Output the execution
            int QtyLast = myQty;

            // make sure it's our instrument
            if (Instrument.FullName == execution.Instrument.FullName)
            {
                // increment or decrement our position counters
                if (execution.MarketPosition.ToString() == "Short")
                {
                    size -= execution.Quantity;
                    positionQty -= execution.Quantity;
                    myQty -= execution.Quantity;
                    totalvalue -= execution.Quantity * execution.Price;

                }
                else if (execution.MarketPosition.ToString() == "Long")
                {
                    size += execution.Quantity;
                    positionQty += execution.Quantity;
                    myQty += execution.Quantity;
                    totalvalue += execution.Quantity * execution.Price;
                }
                // we have to reset to zero for last set of trades unless we want cumalative p&l
                if (size == 0)
                {
                    totalvalue = 0;
                }

                // is this a starting order?
                if (myQty != 0 && QtyLast == 0)
                {
                    // new opening trade to setup stats
                    myAvgPrice = execution.Price; // set avg price as new position.
                    // update trades

                }
                else if (QtyLast != 0)
                {
                }

                // are we flat?
                if (myQty == 0)
                {
                }

                // we got a filled order, so update the averages etc. if qty changed
                if (myQty != QtyLast)
                {
                }

                if (myQty != 0)
                {
                    myAvgPrice   = Math.Abs(totalvalue / Math.Abs(myQty));
                }

                if (size == 0)
                {
                }
            }
        }

        private void PnL()
        {
            URpl = Account.Get(AccountItem.UnrealizedProfitLoss, Currency.UsDollar);
            Rpl  = Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar);

            if ((URpl <= FloatingLoss && UseCloseAtFloatingLoss)
            || ((URpl >= FloatingProfit) && UseCloseAtFloatingProfit)
            || ((Rpl <= FloatingLoss) && (UseCloseAtFloatingLoss))
            || ((Rpl >= FloatingProfit) && (UseCloseAtFloatingProfit))
            || ((Rpl + URpl <= FloatingLoss) && (UseCloseAtFloatingLoss))
            || ((Rpl) + URpl >= FloatingProfit) && (UseCloseAtFloatingProfit))
            {
                Account.FlattenEverything();
                Enabled = false;
            }
        }

        #region Properties
        [NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Order=501, GroupName="Parameters")]
		public int Period
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Lot2TP", Order=5081, GroupName="Parameters")]
		public double Lot2TP
		{ get; set; }


		[NinjaScriptProperty]
		[Display(Name="StopToBE (points)", Order=122, GroupName="Parameters")]
		public double StopToBE
		{ get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Require slope filter", Order = 100, GroupName = "Parameters - Optional")]
        public double SlopeFilter
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "BarsBetweenEntries", Order = 101, GroupName = "Parameters - Optional")]
        public int BarsBetweenEntries
        { get; set; }


        [NinjaScriptProperty]
        [Display(Name = "RSI period", Order = 100, GroupName = "Parameters - Optimisation")]
        public int RSIPeriod
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stop loss (ticks)", Order = 100, GroupName = "Parameters")]
        public int SL
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Profit target (ticks)", Order = 105, GroupName = "Parameters")]
        public int TP
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use trail stop", Order = 115, GroupName = "Parameters")]
        public bool UseTrailingStop
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "..trail stop starts (ticks)", Order = 120, GroupName = "Parameters")]
        public int TrailStopStartsTicks
        { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="..trailStop distance (ticks)", Order=121, GroupName="Parameters")]
		public int TSL
		{ get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use scale in", Order = 130, GroupName = "Parameters")]
        public int UseScaleIn
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Longs enabled", Order = 10, GroupName = "Parameters")]
        public bool LongsEnabled
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Short enabled", Order = 20, GroupName = "Parameters")]
        public bool ShortsEnabled
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use HA bars", Order = 30, GroupName = "Parameters")]
        public bool UseHABars
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show status on chart", Order = 40, GroupName = "Parameters")]
        public bool ShowStatusText
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max size", Order = 100, GroupName = "Parameters")]
        public int MaxSize
        { get; set; }


		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Qty", Description="Contract quantity per position", Order=1, GroupName="Parameters")]
		public int Qty
		{ get; set; }

		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time", Order=50, GroupName="Parameters")]
		public DateTime GoTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time", Order=55, GroupName="Parameters")]
		public DateTime EndTime
		{ get; set; }

        [NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="UseCloseAtFloatingLoss", Description="Use Close At Floating Loss", Order=0, GroupName="Money Management")]
		public bool UseCloseAtFloatingLoss
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "FloatingLoss", Description = "Floating Loss", Order = 1, GroupName = "Money Management")]
		public double FloatingLoss
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="UseCloseAtFloatingProfit", Description="Use Close At Floating Profit", Order=2, GroupName="Money Management")]
		public bool UseCloseAtFloatingProfit
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "FloatingProfit", Description = "Floating Profit", Order = 3, GroupName = "Money Management")]
		public double FloatingProfit
		{ get; set; }

        [NinjaScriptProperty]
        [Display(Name="AccountCashValueTarget", Description="Stop if the Account Cash Value Target reached", Order=4, GroupName="Money Management")]
        public double AccountCashValueTarget
        { get; set; }

		[NinjaScriptProperty]
        [Display(Name="AccountCashValueDrawDown", Description="Stop if Max DrawDown reached", Order=5, GroupName="Money Management")]
        public double AccountCashValueDrawDown
        { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Buttons Position", Description="Chose the position of the buttons on the chart", Order=1, GroupName="D/ Environement Parameters")]
		public ButtonsPosition buttonpos
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Buttons Alignement", Description="Chose the alignement of the buttons", Order=1, GroupName="D/ Environement Parameters")]
		public ButtonsAlign buttonal
		{ get; set; }

		[NinjaScriptProperty]
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Order dot gap", Description="Entry dot distance from the bar", Order=3, GroupName="D/ Environement Parameters")]
		public double DotGap
		{ get; set; }


        #endregion

    }
}
