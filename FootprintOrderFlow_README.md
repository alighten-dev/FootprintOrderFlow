# FootprintOrderFlow

A NinjaTrader 8 custom footprint indicator that aggregates Bid, Ask, Delta, Volume, Point of Control (POC), Value Area, and a rich suite of signals—designed to plug directly into your strategies.

![image](https://github.com/user-attachments/assets/dd6e2d4d-2452-4bc1-92c2-026c195124e5)

## Features

- **Bid/Ask/Delta/Volume aggregation** across configurable price bins  
- **Aligned aggregation levels**, for cleaner, more consistent visuals  
- **27 Plots**: 27 plots for use in strategies, including 12 OrderFlow signals, two new signals for Delta Flip and Stopping Volume
- **Predator-compatible signals** for seamless use with drawing-object capture tools  
- **Fully customizable** Bars, Value Areas, and POC styling (colors, opacity, font size)  
- **Cleaner chart resizing** with redesigned Value Areas and POCs  
- **Five POC-focused plots** to drive strategy entries:  
  - `Values[5]` → `pocPrice`  
  - `Values[23]` → `pocVA_FromLow`  
  - `Values[24]` → `pocVA_FromHigh`  
  - `Values[25]` → `pocBar_FromLow`  
  - `Values[26]` → `pocBar_FromHigh`

## Requirements

1. **OrderFlow+** subscription  
2. Volumetric secondary data series (Time, Range, or Tick) added to your chart  
3. No Tick Replay required  

## Installation & Setup

Download the .CS file and place it in your indicators folder.

C:\Users<your user name>\Documents\NinjaTrader 8\bin\Custom\Indicators

## Configuration

- **Enable/disable** the footprint, summary table, signal grid, and legend independently.  
- **Font size** can be set for both the footprint and summary table.  
- **Opacity** and color brushes for Bars, Value Areas, and POCs are fully exposed as properties.  
- **Stability update**: internal key checks prevent missing-key errors during rendering.

## Plots & Signals

| Plot / Signal      | Description                                                                                |
|--------------------|--------------------------------------------------------------------------------------------|
| **AggDelta**       | Net delta across all bins (ask – bid)                                                       |
| **MinDelta**       | Lowest delta among bins                                                                     |
| **MaxDelta**       | Highest delta among bins                                                                    |
| **CumDelta**       | Running total of net delta over bars                                                        |
| **TotalVol**       | Sum of bid + ask volume                                                                      |
| **POC**            | Price level with highest volume                                                             |
| **VAHigh**         | Upper boundary of the Value Area (e.g. 70% volume)                                          |
| **VALow**          | Lower boundary of the Value Area                                                            |
| **Ratio**          | Custom volume ratio for imbalance detection                                                 |
| **DeltaPerVolume** | Normalized net delta (delta ÷ total volume)                                                 |
| **VolSeq**         | Sequential volume increase/decrease                                                         |
| **StackImb**       | Consecutive bin-level imbalances                                                            |
| **RevPOC**         | POC reversal versus prior bars                                                              |
| **Sweep**          | Low-volume “sweep” bins indicating potential breakout                                      |
| **DeltaSeq**       | Trend detection in delta over recent bars                                                   |
| **Divergence**     | Divergence between delta and price action                                                   |
| **DeltaFlip**      | Sudden reversal in delta values                                                             |
| **DeltaTrap**      | Trap patterns in delta sequences                                                            |
| **Absorption**     | Counter-volume absorbing large delta moves                                                  |
| **Exhaustion**     | Low extreme-end volume indicating exhaustion                                                |
| **VAGap**          | Gap between current and previous Value Areas                                                |
| **LargeRatio**     | High-threshold volume ratio imbalances                                                       |
| **Stopping Volume**| Identifies bars where bid/ask volume at the extremes exceeds recent lookback averages       |

## POC Trade Plots

Five dedicated plots to help your strategy trade directly on the POC:

```csharp
double pocPrice      = footprint1.Values[5][0];
double pocVA_FromLow = footprint1.Values[23][0];
double pocVA_FromHigh= footprint1.Values[24][0];
double pocBar_FromLow= footprint1.Values[25][0];
double pocBar_FromHigh=footprint1.Values[26][0];
```

## Strategy Integration

1. **Configure volumetric data** in `State.Configure`:
   ```csharp
   AddVolumetric(Instrument.FullName,
                 BarsPeriod.BarsPeriodType,
                 BarsPeriod.Value,
                 VolumetricDeltaType.BidAsk,
                 1);
   ```
2. **Instantiate** in `State.DataLoaded`:
   ```csharp
   Footprint1 = FootprintOrderFlow(
				    Close,
				    3, 70, false, 4, 2, 2, 4, 3, 3, 4.0, 30, .69, 100, 300, 2000, 8, 8,
				    16, false, false, true, true, 40,
					System.Windows.Media.Brushes.LimeGreen,
					System.Windows.Media.Brushes.Red,
					System.Windows.Media.Brushes.White,
					System.Windows.Media.Brushes.White,
					System.Windows.Media.Brushes.DarkGreen,
					System.Windows.Media.Brushes.DarkRed,
					20,
					System.Windows.Media.Brushes.Gray,
					System.Windows.Media.Brushes.Yellow,
					System.Windows.Media.Brushes.White,
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
					System.Windows.Media.Brushes.DarkGreen,
				    System.Windows.Media.Brushes.Brown,
				    false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3, false, 3
				);
   ```
3. **Reference plots** by index:
   ```csharp
   Print($"Delta={footprint1.Values[0][1]} | MaxDelta={footprint1.Values[2][1]} | …");
   ```

## Troubleshooting

- If the **Summary Table** hides price bars, adjust the **Lower Margin**:
  - Right-click Y-axis → Properties → Margin Lower  
- Predator-style tools? Individual plot signals ensure every marker is accessible to capture tools.

## Credit

Bid/Ask aggregation logic originally from **iotecdotdev**’s [iotecOFPlus](https://github.com/iotecdotdev/iotecOFPlus). Their Footprint indicator served as inspiration—check it out!
