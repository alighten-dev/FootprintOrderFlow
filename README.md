# FootprintOrderFlow
NinjaTrader 8 Custom Footprint Indicator that aggregates Bid, Ask, Delta, Volume, POC, and Value Area, plus signals. Additional plots are designed for integration with strategies.

## Updates
1. The Delta Summary Table can hide the price bars. Scroll all the way down to see the instructions on how to fix this.
2. Added Predator Signals. Scroll down to the Predator section below. Happy hunting, predators!

## Important
1. This indicator requires an OrderFlow+ subscription.
2. You do NOT need to use "Tick Replay" to use this indicator.
3. This indicator only works with base bars that allow for Volumetric secondary dataseries. (e.g. Time, Range, Tick)

## Credit
The Bid/Ask aggregation logic was copied from the great work by iotecdotdev at https://github.com/iotecdotdev/iotecOFPlus. Check out his Footprint indicator. It's great!

## About
I created this indicator to work as a standalone indicator, but more importantly, to be integrated into your strategies. The indicator has three main components: the footprint, the summary table, and the signal grid. I have also included a legend so that you can quickly translate the signal grid. Each of these can be turned off individually, and all of them can be turned off to improve your strategy's performance.

**Complete Footprint Indicator**
![image](https://github.com/user-attachments/assets/0e2eb157-7f17-49b3-8705-2eb840077c7f)

**Just the Signals**
![image](https://github.com/user-attachments/assets/97ff74de-52d7-435f-8de5-fa522fa65f13)


## Plots
I have added plots for the following Order Flow values and signals. 

**AggDelta:**
Aggregated Delta. The net delta (ask minus bid volume) is calculated by summing the deltas across aggregated price bins in the current bar.

**MinDelta:**
Minimum Delta value from the aggregated price bins. It shows the lowest (most negative) delta in the bar.

**MaxDelta:**
Maximum Delta value from the aggregated price bins. It indicates the highest (most positive) delta in the bar.

**CumDelta:**
Cumulative Delta. This value sums the delta values over time, showing how net delta has evolved across bars.

**TotalVol:**
Total Volume. This is the combined volume (bid + ask) across all the aggregated bins for the current bar.

**POC:**
Point of Control. The price level where the highest volume occurred in the bar.

**VAHigh:**
Value Area High. The upper boundary of the value area (typically set to include 70% of total volume), representing the highest price within that area.

**VALow:**
Value Area Low. The lower boundary of the value area, representing the lowest price where the majority of volume has occurred.

**Ratio:**
A ratio calculated from volume data (for example, comparing volumes in adjacent bins) to help gauge potential market imbalances.

**POCPos:**
POC Position. Indicates the relative position of the Point of Control within the bar’s price range (e.g., lower third, middle, or upper third).

**DeltaPerVolume:**
Delta per Volume. This is the ratio of the bar’s delta to its total volume, providing a normalized measure of net buying or selling pressure.

**VolSeq:**
Volume Sequence Signal. This signal detects a sequential increase or decrease in volume across the aggregated price bins.

**StackImb:**
Stacked Imbalance Signal. Measures consecutive imbalances in bid versus ask volume, indicating potential strength in one direction.

**RevPOC:**
Reversal POC Signal. Generated when the Point of Control reverses its position relative to previous bars, which may indicate a market turnaround.

**Sweep:**
Sweep Signal. Triggered by a sequence of bins with very low volume, which may indicate a sweep of price levels.

**DeltaSeq:**
Delta Sequence Signal. Analyzes the trend in delta values over recent bars to detect consistent directional movement.

**Divergence:**
Divergence Signal. Identifies situations where the delta diverges from the price action (for example, when a lower price bar shows a positive delta), suggesting a possible reversal.

**DeltaFlip:**
Delta Flip Signal. Detects a sudden reversal in delta values, which can be a precursor to a change in market direction.

**DeltaTrap:**
Delta Trap Signal. Looks for trap patterns in the delta sequence that may indicate false breakouts or traps.

**Absorption:**
Absorption Signal. Indicates when a large delta move is being absorbed by counteracting volume, suggesting that the move might not be sustainable.

**Exhaustion:**
Exhaustion Signal. Highlights when one side (bid or ask) shows very low volume at the extremes of the bar, potentially indicating market exhaustion.

**VAGap:**
Value Area Gap Signal. Detects gaps between the current bar’s value area and that of previous bars, which can signal a shift in market sentiment.

**LargeRatio:**
Large Ratio Signal. Activated when a calculated volume ratio exceeds a set threshold, flagging potential market imbalances.


## From your strategy
Reference the object as usual.
```
private FootprintOrderFlowIndicator footprint1;
```

You will need the 1-tick Volumetric data series added to your configuration.
```
else if (State == State.Configure)
{
  AddVolumetric(Instrument.FullName, BarsPeriod.BarsPeriodType, BarsPeriod.Value, VolumetricDeltaType.BidAsk, 1);
}
```

Instantiate the object in your DataLoaded.
```
else if (State == State.DataLoaded)
			{	
				footprint1 = FootprintOrderFlow(
				    Close,
				    15, 70, 4, 2, 2, 4, 3, 4.0, 30, 100, 300, 2000, 8, 8,
				    false, false, true, 40,
				    System.Windows.Media.Brushes.DarkGreen,
				    System.Windows.Media.Brushes.Green,
				    System.Windows.Media.Brushes.Lime,
				    System.Windows.Media.Brushes.DarkRed,
				    System.Windows.Media.Brushes.Red,
				    System.Windows.Media.Brushes.Crimson,
				    System.Windows.Media.Brushes.Cyan,
				    System.Windows.Media.Brushes.Gray,
				    System.Windows.Media.Brushes.Blue,
				    System.Windows.Media.Brushes.Orange,
				    System.Windows.Media.Brushes.Purple,
				    System.Windows.Media.Brushes.Yellow,
				    System.Windows.Media.Brushes.Magenta,
				    System.Windows.Media.Brushes.Cyan,
				    System.Windows.Media.Brushes.LightGreen,
				    System.Windows.Media.Brushes.Red,
				    System.Windows.Media.Brushes.Teal,
				    System.Windows.Media.Brushes.Pink,
				    false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3
				);
			}
```

To access the plots, reference the appropriate plot by number.
```
Print("Time " + Time[1] +
          " | Bar " + (CurrentBar - 1) +
          " | Delta=" + footprint1.Values[0][1] +
          " | MaxDelta=" + footprint1.Values[2][1] +
          " | MinDelta=" + footprint1.Values[1][1] +
          " | CumDelta=" + footprint1.Values[3][1] +
          " | TotalVol=" + footprint1.Values[4][1] +
          " | POC=" + footprint1.Values[5][1] +
          " | VAH=" + footprint1.Values[6][1] +
          " | VAL=" + footprint1.Values[7][1] +
          " | Ratio=" + footprint1.Values[8][1] +
          " | DeltaPerVol=" + footprint1.Values[10][1] +
          " | VolSeq=" + footprint1.Values[11][1] +
          " | StackImb=" + footprint1.Values[12][1] +
          " | RevPOC=" + footprint1.Values[13][1] +
          " | Sweep=" + footprint1.Values[14][1] +
          " | DeltaSeq=" + footprint1.Values[15][1] +
          " | Divergence=" + footprint1.Values[16][1] +
          " | DeltaFlip=" + footprint1.Values[17][1] +
          " | DeltaTrap=" + footprint1.Values[18][1] +
          " | Absorption=" + footprint1.Values[19][1] +
          " | Exhaustion=" + footprint1.Values[20][1] +
          " | VAGap=" + footprint1.Values[21][1] +
          " | LargeRatio=" + footprint1.Values[22][1]);
```
## Summary Table Hiding Bars
If the summary table is hiding the price bars. Adjust the lower margin of your price scale. Right-click the Y-axis (price) -> Properties -> Margin Lower.

![image](https://github.com/user-attachments/assets/176e7012-60dc-4e0b-a1c1-9c9aa389337b)

Adjusting the "Margin Lower" value will prevent this:

![image](https://github.com/user-attachments/assets/b9bde5d2-25a5-47bb-9b5b-001153a4dde0)

## Predator Signals
To accommodate drawing object capturing tools like Predator, I have added individual signals for each of the primary "trade signal" plots. Happy hunting, Predators!

![image](https://github.com/user-attachments/assets/2b1bd8b2-2bb8-4912-8524-608c855507aa)
![image](https://github.com/user-attachments/assets/855d2e5e-e795-47b6-b3a0-11c79ccfbfef)
![image](https://github.com/user-attachments/assets/3811c7c5-398f-4d9f-a751-5a19f4c94e4d)


