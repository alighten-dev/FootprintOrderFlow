# FootprintOrderFlow

A NinjaTrader 8 custom footprint indicator that aggregates Bid, Ask, Delta, Volume, Point of Control (POC), Value Area, and a rich suite of signals—designed to plug directly into your strategies.

## Features

- **Bid/Ask/Delta/Volume aggregation** across configurable price bins  
- **Aligned aggregation levels**, for cleaner, more consistent visuals  
- **Two new OrderFlow signals**: Delta Flip and Stopping Volume  
- **Predator-compatible signals** for seamless use with drawing-object capture tools  
- **Fully customizable** Bars, Value Areas, and POC styling (colors, opacity, font size)  
- **Cleaner chart resizing** with redesigned Value Areas and POCs  
- **Five POC-focused plots** to drive strategy entries:  
  - `Values[5]` → `pocPrice`  
  - `Values[23]` → `pocVA_FromLow`  
  - `Values[24]` → `pocVA_FromHigh`  
  - `Values[25]` → `pocBar_FromLow`  
  - `Values[26]` → `pocBar_FromHigh` fileciteturn0file0

## Requirements

1. **OrderFlow+** subscription  
2. Volumetric secondary data series (Time, Range, or Tick) added to your chart  
3. No Tick Replay required  

## Installation & Setup

1. Place the compiled `.dll` into your NinjaTrader 8 `bin\Custom\Indicators` folder.  
2. Restart NinjaTrader 8 or reload your Indicators.  
3. Add **FootprintOrderFlow** to any chart with a compatible base bars type.

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
   footprint1 = FootprintOrderFlow(
       Closes[0],
       // … your parameters here …
       System.Windows.Media.Brushes.LimeGreen,
       System.Windows.Media.Brushes.Red,
       /* etc. */
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
