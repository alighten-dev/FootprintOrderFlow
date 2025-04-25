#region Using declarations
using System;
using System.Collections;
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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class FootprintOrderFlow : Indicator
    {
        #region Variables and Properties

		// ***** Aggregation Settings *****
		[NinjaScriptProperty]
		[Display(Name = "Aggregation Interval (ticks)", Order = 1, GroupName = "Aggregation Settings")]
		public int AggregationInterval { get; set; } = 15;  // Use a higher value (15) to match iotecOFPlus4
		
		[NinjaScriptProperty]
		[Display(Name = "Value Area %", Order = 2, GroupName = "Aggregation Settings")]
		public double ValueAreaPer { get; set; } = 70;
		
		// ***** Signal Lookback Settings *****
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Volume Seq Lookback", Order = 3, GroupName = "Signal Lookback Settings")]
		public int VolumeSeqLookback { get; set; } = 4;
		
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Stacked Imbalance Lookback", Order = 4, GroupName = "Signal Lookback Settings")]
		public int StackedImbalanceLookback { get; set; } = 2;
		
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Delta Sequence Lookback", Order = 5, GroupName = "Signal Lookback Settings")]
		public int DeltaSequenceLookback { get; set; } = 2;
		
		[NinjaScriptProperty]
		[Display(Name = "Sweep Lookback", Order = 6, GroupName = "Signal Lookback Settings")]
		public int SweepLookback { get; set; } = 4;
		
		[NinjaScriptProperty]
		[Display(Name = "Divergence Lookback", Order = 7, GroupName = "Signal Lookback Settings")]
		public int DivergenceLookback { get; set; } = 3;
		
		// ***** Signal Thresholds *****
		[NinjaScriptProperty]
		[Display(Name = "Imbalance Ratio", Order = 7, GroupName = "Signal Thresholds")]
		public double ImbFact { get; set; } = 4.0;
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Large Ratio Threshold", Order = 8, GroupName = "Signal Thresholds")]
		public double LargeRatioThreshold { get; set; } = 30;  
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Small Ratio Threshold", Order = 9, GroupName = "Signal Thresholds")]
		public double SmallRatioThreshold { get; set; } = 0.69;  
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Delta Threshold 1", Order = 10, GroupName = "Signal Thresholds")]
		public int DeltaThreshold1 { get; set; } = 100;  
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Delta Threshold 2", Order = 11, GroupName = "Signal Thresholds")]
		public int DeltaThreshold2 { get; set; } = 300; 
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Volume Threshold", Order = 12, GroupName = "Signal Thresholds")]
		public int VolumeThreshold { get; set; } = 2000; 
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Near Zero Threshold (Flip/Sweep/Table Display)", Order = 12, GroupName = "Signal Thresholds")]
		public int NearZeroaThreshold { get; set; } = 8; 
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Exhaustion Threshold", Order = 13, GroupName = "Signal Thresholds")]
		public int ExhaustionThreshold { get; set; } = 8; 	
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Set the Font Size", Order = 1, GroupName = "Display")]
		public int StandardFontSize { get; set; } = 16; 
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Footprint on Bars", Order = 2, GroupName = "Display")]
		public bool EnableFootprint { get; set; } = true; 
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Summary Grid on Chart", Order = 3, GroupName = "Display")]
		public bool EnableSummaryGrid { get; set; } = true; // Adds the summary grid at the bottom of the chart to display Delta/MinDelta/MaxDelta/Volume
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Signal Grid on Bar", Order = 4, GroupName = "Display")]
		public bool EnableSignalGrid { get; set; } = true; // Adds the summary grid at the bottom of the chart to display Delta/MinDelta/MaxDelta/Volume
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Signal Grid Offset", Order = 5, GroupName = "Display")]
		public int SignalGridOffset { get; set; } = 40; // Signal display offset from bar
		
		// ***** Color Settings *****
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Delta Low Positive Color", Order = 12, GroupName = "Color Settings")]
		public System.Windows.Media.Brush DeltaLowPositiveColor { get; set; } = System.Windows.Media.Brushes.DarkGreen;
		[Browsable(false)]
		public string DeltaLowPositiveColorSerialize
		{
		    get { return Serialize.BrushToString(DeltaLowPositiveColor); }
		    set { DeltaLowPositiveColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Delta Medium Positive Color", Order = 13, GroupName = "Color Settings")]
		public System.Windows.Media.Brush DeltaMediumPositiveColor { get; set; } = System.Windows.Media.Brushes.Green;
		[Browsable(false)]
		public string DeltaMediumPositiveColorSerialize
		{
		    get { return Serialize.BrushToString(DeltaMediumPositiveColor); }
		    set { DeltaMediumPositiveColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Delta High Positive Color", Order = 14, GroupName = "Color Settings")]
		public System.Windows.Media.Brush DeltaHighPositiveColor { get; set; } = System.Windows.Media.Brushes.Lime;
		[Browsable(false)]
		public string DeltaHighPositiveColorSerialize
		{
		    get { return Serialize.BrushToString(DeltaHighPositiveColor); }
		    set { DeltaHighPositiveColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Delta Low Negative Color", Order = 15, GroupName = "Color Settings")]
		public System.Windows.Media.Brush DeltaLowNegativeColor { get; set; } = System.Windows.Media.Brushes.DarkRed;
		[Browsable(false)]
		public string DeltaLowNegativeColorSerialize
		{
		    get { return Serialize.BrushToString(DeltaLowNegativeColor); }
		    set { DeltaLowNegativeColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Delta Medium Negative Color", Order = 16, GroupName = "Color Settings")]
		public System.Windows.Media.Brush DeltaMediumNegativeColor { get; set; } = System.Windows.Media.Brushes.Red;
		[Browsable(false)]
		public string DeltaMediumNegativeColorSerialize
		{
		    get { return Serialize.BrushToString(DeltaMediumNegativeColor); }
		    set { DeltaMediumNegativeColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Delta High Negative Color", Order = 17, GroupName = "Color Settings")]
		public System.Windows.Media.Brush DeltaHighNegativeColor { get; set; } = System.Windows.Media.Brushes.Crimson;
		[Browsable(false)]
		public string DeltaHighNegativeColorSerialize
		{
		    get { return Serialize.BrushToString(DeltaHighNegativeColor); }
		    set { DeltaHighNegativeColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "High Volume Color", Order = 18, GroupName = "Color Settings")]
		public System.Windows.Media.Brush HighVolumeColor { get; set; } = System.Windows.Media.Brushes.Cyan;
		[Browsable(false)]
		public string HighVolumeColorSerialize
		{
		    get { return Serialize.BrushToString(HighVolumeColor); }
		    set { HighVolumeColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Zero Delta Color", Order = 19, GroupName = "Color Settings")]
		public System.Windows.Media.Brush ZeroDeltaColor { get; set; } = System.Windows.Media.Brushes.Gray;
		[Browsable(false)]
		public string ZeroDeltaColorSerialize
		{
		    get { return Serialize.BrushToString(ZeroDeltaColor); }
		    set { ZeroDeltaColor = Serialize.StringToBrush(value); }
		}
		
		// ***** Signal Colors *****
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "VolSeq Color", Order = 20, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush VolSeqColor { get; set; } = System.Windows.Media.Brushes.Blue;
		[Browsable(false)]
		public string VolSeqColorSerialize
		{
		    get { return Serialize.BrushToString(VolSeqColor); }
		    set { VolSeqColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "StackedImb Color", Order = 21, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush StackedImbColor { get; set; } = System.Windows.Media.Brushes.Orange;
		[Browsable(false)]
		public string StackedImbColorSerialize
		{
		    get { return Serialize.BrushToString(StackedImbColor); }
		    set { StackedImbColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "RevPOC Color", Order = 22, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush RevPOCColor { get; set; } = System.Windows.Media.Brushes.Purple;
		[Browsable(false)]
		public string RevPOCColorSerialize
		{
		    get { return Serialize.BrushToString(RevPOCColor); }
		    set { RevPOCColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Sweep Color", Order = 23, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush SweepColor { get; set; } = System.Windows.Media.Brushes.Yellow;
		[Browsable(false)]
		public string SweepColorSerialize
		{
		    get { return Serialize.BrushToString(SweepColor); }
		    set { SweepColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "DeltaSeq Color", Order = 24, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush DeltaSeqColor { get; set; } = System.Windows.Media.Brushes.Magenta;
		[Browsable(false)]
		public string DeltaSeqColorSerialize
		{
		    get { return Serialize.BrushToString(DeltaSeqColor); }
		    set { DeltaSeqColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Divergence Color", Order = 25, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush DivergenceColor { get; set; } = System.Windows.Media.Brushes.Cyan;
		[Browsable(false)]
		public string DivergenceColorSerialize
		{
		    get { return Serialize.BrushToString(DivergenceColor); }
		    set { DivergenceColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Absorption Color", Order = 26, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush AbsorptionColor { get; set; } = System.Windows.Media.Brushes.LightGreen;
		[Browsable(false)]
		public string AbsorptionColorSerialize
		{
		    get { return Serialize.BrushToString(AbsorptionColor); }
		    set { AbsorptionColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "Exhaustion Color", Order = 27, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush ExhaustionColor { get; set; } = System.Windows.Media.Brushes.Red;
		[Browsable(false)]
		public string ExhaustionColorSerialize
		{
		    get { return Serialize.BrushToString(ExhaustionColor); }
		    set { ExhaustionColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "VAGap Color", Order = 28, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush VAGapColor { get; set; } = System.Windows.Media.Brushes.Teal;
		[Browsable(false)]
		public string VAGapColorSerialize
		{
		    get { return Serialize.BrushToString(VAGapColor); }
		    set { VAGapColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[NinjaScriptProperty]
		[Display(Name = "LargeRatio Color", Order = 29, GroupName = "Signal Colors")]
		public System.Windows.Media.Brush LargeRatioColor { get; set; } = System.Windows.Media.Brushes.Pink;
		[Browsable(false)]
		public string LargeRatioColorSerialize
		{
		    get { return Serialize.BrushToString(LargeRatioColor); }
		    set { LargeRatioColor = Serialize.StringToBrush(value); }
		}	
		
		// ***** Show/Hide Individual Signals for Predator *****
		[NinjaScriptProperty]
		[Display(Name = "Enable VolSeq Diamond Signal", Order = 100, GroupName = "Individual Signal Settings")]
		public bool EnableVolSeqSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "VolSeq Diamond Offset", Order = 101, GroupName = "Individual Signal Settings")]
		public int VolSeqDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Stacked Imb Diamond Signal", Order = 102, GroupName = "Individual Signal Settings")]
		public bool EnableStackedImbSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "Stacked Imb Diamond Offset", Order = 103, GroupName = "Individual Signal Settings")]
		public int StackedImbDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Reversal POC Diamond Signal", Order = 104, GroupName = "Individual Signal Settings")]
		public bool EnableReversalPOCSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "Reversal POC Diamond Offset", Order = 105, GroupName = "Individual Signal Settings")]
		public int ReversalPOCDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Sweep Diamond Signal", Order = 106, GroupName = "Individual Signal Settings")]
		public bool EnableSweepSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "Sweep Diamond Offset", Order = 107, GroupName = "Individual Signal Settings")]
		public int SweepDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable DeltaSeq Diamond Signal", Order = 108, GroupName = "Individual Signal Settings")]
		public bool EnableDeltaSeqSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "DeltaSeq Diamond Offset", Order = 109, GroupName = "Individual Signal Settings")]
		public int DeltaSeqDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Divergence Diamond Signal", Order = 110, GroupName = "Individual Signal Settings")]
		public bool EnableDivergenceSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "Divergence Diamond Offset", Order = 111, GroupName = "Individual Signal Settings")]
		public int DivergenceDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable DeltaFlip Diamond Signal", Order = 112, GroupName = "Individual Signal Settings")]
		public bool EnableDeltaFlipSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "DeltaFlip Diamond Offset", Order = 113, GroupName = "Individual Signal Settings")]
		public int DeltaFlipDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable DeltaTrap Diamond Signal", Order = 114, GroupName = "Individual Signal Settings")]
		public bool EnableDeltaTrapSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "DeltaTrap Diamond Offset", Order = 115, GroupName = "Individual Signal Settings")]
		public int DeltaTrapDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Absorption Diamond Signal", Order = 116, GroupName = "Individual Signal Settings")]
		public bool EnableAbsorptionSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "Absorption Diamond Offset", Order = 117, GroupName = "Individual Signal Settings")]
		public int AbsorptionDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Exhaustion Diamond Signal", Order = 118, GroupName = "Individual Signal Settings")]
		public bool EnableExhaustionSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "Exhaustion Diamond Offset", Order = 119, GroupName = "Individual Signal Settings")]
		public int ExhaustionDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable VAGap Diamond Signal", Order = 120, GroupName = "Individual Signal Settings")]
		public bool EnableVAGapSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "VAGap Diamond Offset", Order = 121, GroupName = "Individual Signal Settings")]
		public int VAGapDiamondOffset { get; set; } = 3;
		
		[NinjaScriptProperty]
		[Display(Name = "Enable Large Ratio Diamond Signal", Order = 122, GroupName = "Individual Signal Settings")]
		public bool EnableLargeRatioSignal { get; set; } = false;
		
		[NinjaScriptProperty]
		[Display(Name = "Large Ratio Diamond Offset", Order = 123, GroupName = "Individual Signal Settings")]
		public int LargeRatioDiamondOffset { get; set; } = 3;

		
		

        // ***** Internal variables *****
        private NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType;
        private double tickSize;
		private int lastPrintedBar = -1;

        // Aggregated dictionaries persist across bars (keyed by bar index)
        private Dictionary<int, Dictionary<double, double>> GetAskVolumeForPrice = new Dictionary<int, Dictionary<double, double>>();
        private Dictionary<int, Dictionary<double, double>> GetBidVolumeForPrice = new Dictionary<int, Dictionary<double, double>>();
        private Dictionary<int, Dictionary<double, double>> GetDeltaForPrice = new Dictionary<int, Dictionary<double, double>>();
        private Dictionary<int, Dictionary<double, double>> GetTotalVolumeForPrice = new Dictionary<int, Dictionary<double, double>>();
		
		// Dictionaries for calculated values
		private Dictionary<int, double> GetDeltaForBar = new Dictionary<int, double>();		
		private Dictionary<int, double> GetMinDeltaForBar = new Dictionary<int, double>();
		private Dictionary<int, double> GetMaxDeltaForBar = new Dictionary<int, double>();
		private Dictionary<int, double> GetCumDeltaForBar = new Dictionary<int, double>();
		private Dictionary<int, double> GetVolumeForBar = new Dictionary<int, double>();
		private Dictionary<int, double> GetPOCForBar = new Dictionary<int, double>();
		private Dictionary<int, double> GetVAHForBar = new Dictionary<int, double>();
		private Dictionary<int, double> GetVALForBar = new Dictionary<int, double>();
		private Dictionary<int, double> GetRatioForBar = new Dictionary<int, double>();
		private Dictionary<int, int> GetPOCPosForBar = new Dictionary<int, int>(); 
		private Dictionary<int, double> GetDeltaPerVolumeForBar = new Dictionary<int, double>(); 
		private Dictionary<int, string> GetDeltaSignalsForBar = new Dictionary<int, string>();		
		
		// Dictionary for calculated signals
		private Dictionary<int, int> GetVolSeqForBar = new Dictionary<int, int>(); 
		private Dictionary<int, int> GetStackedImbForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetReversalPOCForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetSweepForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetDeltaSeqForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetDivergenceForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetDeltaFlipForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetDeltaTrapForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetAbsorptionForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetExhaustionForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetVAGapForBar = new Dictionary<int, int>();
		private Dictionary<int, int> GetLargeRatioForBar = new Dictionary<int, int>();
		
        // Calculated values for the current bar 
        private double barDelta = 0;
		private double minDelta = 0;
		private double maxDelta = 0;        
        private double cumulativeDelta = 0;
        private double totalVolume = 0;
        private double pocPrice = 0;
        private double vah = 0;  // Value Area High
        private double val = 0;  // Value Area Low
        private double ratio = 0;
        private double pocPos = 0; // POC Position: -1, 0, 1
		private string deltaSignal = "";
		private double deltaPerVolume = 0;
		
        // Calculated signals for the current bar 
        private int volSeqSignal = 0;
        private int stackedImbSignal = 0;
        private int reversalPOCSignal = 0;
        private int sweepSignal = 0;
        private int deltaSeqSignal = 0;
		private int divergenceSignal = 0;
		private int deltaFlipSignal = 0;
		private int deltaTrapSignal = 0;
		private int absorptionSignal = 0;
		private int exhaustionSignal = 0;
		private int vaGapSignal = 0;
		private int largeRatioSignal = 0;

        #endregion

        #region OnStateChange
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Custom Footprint Indicator that aggregates Bid, Ask, Delta, Volume, POC and Value Area, plus signals. Plots designed for integration with strategies. - By Alighten";
                Name = "FootprintOrderFlow";
                Calculate = Calculate.OnEachTick;
                IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				IsSuspendedWhileInactive					= false;

                // --- Add Plots for Values ---
				AddPlot(Brushes.Transparent, "AggDelta");
                AddPlot(Brushes.Transparent, "MinDelta");
                AddPlot(Brushes.Transparent, "MaxDelta");
				AddPlot(Brushes.Transparent, "CumDelta");
                AddPlot(Brushes.Transparent, "TotalVol");
                AddPlot(Brushes.Transparent, "POC");
                AddPlot(Brushes.Transparent, "VAHigh");
                AddPlot(Brushes.Transparent, "VALow");
				AddPlot(Brushes.Transparent, "Ratio");
				AddPlot(Brushes.Transparent, "POCPos");	
				AddPlot(Brushes.Transparent, "DeltaPerVolume");
				
				// --- Add Plots for Signals ---
                AddPlot(Brushes.Transparent, "VolSeq");
                AddPlot(Brushes.Transparent, "StackImb");                
                AddPlot(Brushes.Transparent, "RevPOC");
                AddPlot(Brushes.Transparent, "Sweep");
                AddPlot(Brushes.Transparent, "DeltaSeq");            
				AddPlot(Brushes.Transparent, "Divergence");
				AddPlot(Brushes.Transparent, "DeltaFlip");
				AddPlot(Brushes.Transparent, "DeltaTrap");
				AddPlot(Brushes.Transparent, "Absorption");
				AddPlot(Brushes.Transparent, "Exhaustion");
				AddPlot(Brushes.Transparent, "VAGap");
				AddPlot(Brushes.Transparent, "LargeRatio");				
            }
            else if (State == State.Configure)
            {	
                // Add volumetric data series (using VolumetricDeltaType.BidAsk)
                AddVolumetric(Instrument.FullName, BarsPeriod.BarsPeriodType, BarsPeriod.Value, VolumetricDeltaType.BidAsk, 1);
            }
            else if (State == State.DataLoaded)
            {
                barsType = BarsArray[1].BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
                tickSize = TickSize;
            }
        }
        #endregion

        #region OnBarUpdate
        protected override void OnBarUpdate()
        {
			try
			{
            // Process only the primary series (BarsInProgress==0)
            if (Bars == null || barsType == null)
                return;
			
//			if (BarsInProgress != 0)
//				return;
			
			if (CurrentBar < 0 || CurrentBar >= Bars.Count)
    			return;
			
			if (barsType.Volumes.Count() <= CurrentBar)
    			return;

            // --- Initialize persistent dictionaries for this bar if not already done ---
            if (!GetAskVolumeForPrice.ContainsKey(CurrentBar))
                GetAskVolumeForPrice[CurrentBar] = new Dictionary<double, double>();
            if (!GetBidVolumeForPrice.ContainsKey(CurrentBar))
                GetBidVolumeForPrice[CurrentBar] = new Dictionary<double, double>();
            if (!GetDeltaForPrice.ContainsKey(CurrentBar))
                GetDeltaForPrice[CurrentBar] = new Dictionary<double, double>();
            if (!GetTotalVolumeForPrice.ContainsKey(CurrentBar))
                GetTotalVolumeForPrice[CurrentBar] = new Dictionary<double, double>();

            // --- Clear previous aggregated data for the current bar ---
            GetAskVolumeForPrice[CurrentBar].Clear();
            GetBidVolumeForPrice[CurrentBar].Clear();
            GetDeltaForPrice[CurrentBar].Clear();
            GetTotalVolumeForPrice[CurrentBar].Clear();
			
			if (!GetAskVolumeForPrice.ContainsKey(CurrentBar))
                GetAskVolumeForPrice[CurrentBar] = new Dictionary<double, double>();

            if (!GetBidVolumeForPrice.ContainsKey(CurrentBar))
                GetBidVolumeForPrice[CurrentBar] = new Dictionary<double, double>();
			
            if (!GetDeltaForPrice.ContainsKey(CurrentBar))
                GetDeltaForPrice[CurrentBar] = new Dictionary<double, double>();

            if (!GetTotalVolumeForPrice.ContainsKey(CurrentBar))
                GetTotalVolumeForPrice[CurrentBar] = new Dictionary<double, double>();

            double lowPrice = Low[0];
            double highPrice = High[0];
            double binInterval = AggregationInterval * tickSize;

            // --- Aggregate data over the price range ---
            for (double p = lowPrice; p <= highPrice; p += binInterval)
            {
                double aggAsk = 0;
                double aggBid = 0;

                // For each tick within this bin:
                for (double currentPrice = p; currentPrice < p + binInterval && currentPrice <= highPrice; currentPrice += tickSize)
                {
                    aggAsk += barsType.Volumes[CurrentBar].GetAskVolumeForPrice(currentPrice);
            		aggBid += barsType.Volumes[CurrentBar].GetBidVolumeForPrice(currentPrice);
			   	}

                // Store aggregated values (using the starting price of the bin)
                GetAskVolumeForPrice[CurrentBar][p] = aggAsk;
                GetBidVolumeForPrice[CurrentBar][p] = aggBid;
                GetDeltaForPrice[CurrentBar][p] = aggAsk - aggBid;
                GetTotalVolumeForPrice[CurrentBar][p] = aggAsk + aggBid;
            }
            // (Optionally, call a Cleanup method to remove keys that are not at exact multiples – see below.)
//            CleanupExtraneousValues(GetAskVolumeForPrice[CurrentBar], lowPrice, binInterval);
//            CleanupExtraneousValues(GetBidVolumeForPrice[CurrentBar], lowPrice, binInterval);
//            CleanupExtraneousValues(GetDeltaForPrice[CurrentBar], lowPrice, binInterval);
//            CleanupExtraneousValues(GetTotalVolumeForPrice[CurrentBar], lowPrice, binInterval);			

            // --- Calculate POC from aggregated total volume ---
            if (GetTotalVolumeForPrice[CurrentBar].Count > 0)
            {
                var pocPair = GetTotalVolumeForPrice[CurrentBar].Aggregate((a, b) => a.Value > b.Value ? a : b);
                pocPrice = pocPair.Key;
            }
            else
            {
                pocPrice = 0;
            }

            // --- Retrieve built-in bar-level values from the volumetric series ---
            barDelta = barsType.Volumes[CurrentBar].BarDelta;
            cumulativeDelta = barsType.Volumes[CurrentBar].CumulativeDelta;
            totalVolume = barsType.Volumes[CurrentBar].TotalVolume;

            // --- Calculate minDelta and maxDelta from the aggregated delta values ---
            if (GetDeltaForPrice[CurrentBar].Count > 0)
            {
                maxDelta = GetDeltaForPrice[CurrentBar].Max(kvp => kvp.Value);
                minDelta = GetDeltaForPrice[CurrentBar].Min(kvp => kvp.Value);
            }
            else
            {
                maxDelta = 0;
                minDelta = 0;
            }
			
			// --- Delta/Volume: Simple ratio
			if (totalVolume > 0)
				deltaPerVolume = barDelta / totalVolume;


            // --- Calculate Value Area (VA) using aggregated total volume bins ---
            double targetVA = totalVolume * (ValueAreaPer / 100.0);
            var ascendingBins = GetTotalVolumeForPrice[CurrentBar].OrderBy(x => x.Key).ToList();
            int pocIndex = ascendingBins.FindIndex(x => x.Key == pocPrice);
            if (pocIndex < 0)
                pocIndex = 0;
            // Initialize VA at POC
            vah = pocPrice;
            val = pocPrice;
            double cumVolume = GetTotalVolumeForPrice[CurrentBar][pocPrice];
            int up = pocIndex + 1;
            int down = pocIndex - 1;
            while (cumVolume < targetVA && (up < ascendingBins.Count || down >= 0))
            {
                double volUp = (up < ascendingBins.Count) ? ascendingBins[up].Value : 0;
                double volDown = (down >= 0) ? ascendingBins[down].Value : 0;
                if (up < ascendingBins.Count && (down < 0 || volUp >= volDown))
                {
                    cumVolume += volUp;
                    vah = ascendingBins[up].Key;
                    up++;
                }
                else if (down >= 0)
                {
                    cumVolume += volDown;
                    val = ascendingBins[down].Key;
                    down--;
                }
                else
                {
                    break;
                }
            }            

            // --- Calculate POC Position ---
            double range = highPrice - lowPrice;
            if (range > 0)
            {
                if (pocPrice > lowPrice + 2 * range / 3)
                    pocPos = 1;
                else if (pocPrice < lowPrice + range / 3)
                    pocPos = -1;
                else
                    pocPos = 0;
            }
            else
            {
                pocPos = 0;
            }

            // --- Calculate additional signals (VolSeq, Stacked Imbalance, RevPOC, Sweep, Delta Sequence)

            // Volume Sequencing: check if ask volumes increase over the first few bins.
            if (ascendingBins.Count >= VolumeSeqLookback)
            {
                bool askSeq = true;
                for (int i = 1; i < VolumeSeqLookback; i++)
                {
                    double pricePrev = ascendingBins[i - 1].Key;
                    double priceCurr = ascendingBins[i].Key;
                    double askPrev = GetAskVolumeForPrice[CurrentBar].ContainsKey(pricePrev) ? GetAskVolumeForPrice[CurrentBar][pricePrev] : 0;
                    double askCurr = GetAskVolumeForPrice[CurrentBar].ContainsKey(priceCurr) ? GetAskVolumeForPrice[CurrentBar][priceCurr] : 0;
                    if (!(askCurr > askPrev))
                    {
                        askSeq = false;
                        break;
                    }
                }
                bool bidSeq = true;
                for (int i = ascendingBins.Count - VolumeSeqLookback + 1; i < ascendingBins.Count; i++)
                {
                    int idxBelow = i - 1;
                    double priceBelow = ascendingBins[idxBelow].Key;
                    double priceNow = ascendingBins[i].Key;
                    double bidBelow = GetBidVolumeForPrice[CurrentBar].ContainsKey(priceBelow) ? GetBidVolumeForPrice[CurrentBar][priceBelow] : 0;
                    double bidNow = GetBidVolumeForPrice[CurrentBar].ContainsKey(priceNow) ? GetBidVolumeForPrice[CurrentBar][priceNow] : 0;
                    if (!(bidBelow > bidNow))
                    {
                        bidSeq = false;
                        break;
                    }
                }
                if (askSeq)
                    volSeqSignal = 1;
                else if (bidSeq)
                    volSeqSignal = -1;
                else
                    volSeqSignal = 0;
            }
            else
            {
                volSeqSignal = 0;
            }

            // --- Stacked Imbalance: count consecutive bins where ask is much larger than bid or vice versa.
			// For Ask Imbalance:
			int maxConsecutiveAsk = 0;
			int currentConsecutiveAsk = 0;
			for (int i = 1; i < ascendingBins.Count; i++)
			{
			    double priceCurr = ascendingBins[i].Key;
			    double pricePrev = ascendingBins[i - 1].Key;
			    double askCurr = GetAskVolumeForPrice[CurrentBar].ContainsKey(priceCurr) ? GetAskVolumeForPrice[CurrentBar][priceCurr] : 0;
			    double bidPrev = GetBidVolumeForPrice[CurrentBar].ContainsKey(pricePrev) ? GetBidVolumeForPrice[CurrentBar][pricePrev] : 0;
			    
			    // Check diagonal ask imbalance condition:
			    if (askCurr >= ImbFact * bidPrev)
			        currentConsecutiveAsk++;
			    else
			        currentConsecutiveAsk = 0;
			    
			    if (currentConsecutiveAsk > maxConsecutiveAsk)
			        maxConsecutiveAsk = currentConsecutiveAsk;
			}
			
			// For Bid Imbalance:
			int maxConsecutiveBid = 0;
			int currentConsecutiveBid = 0;
			for (int i = 0; i < ascendingBins.Count - 1; i++)
			{
			    double priceCurr = ascendingBins[i].Key;
			    double priceNext = ascendingBins[i + 1].Key;
			    double bidCurr = GetBidVolumeForPrice[CurrentBar].ContainsKey(priceCurr) ? GetBidVolumeForPrice[CurrentBar][priceCurr] : 0;
			    double askNext = GetAskVolumeForPrice[CurrentBar].ContainsKey(priceNext) ? GetAskVolumeForPrice[CurrentBar][priceNext] : 0;
			    
			    // Check diagonal bid imbalance condition:
			    if (bidCurr >= ImbFact * askNext)
			        currentConsecutiveBid++;
			    else
			        currentConsecutiveBid = 0;
			    
			    if (currentConsecutiveBid > maxConsecutiveBid)
			        maxConsecutiveBid = currentConsecutiveBid;
			}
			
			// Set the stacked imbalance signal based on the maximum consecutive counts.
			if (maxConsecutiveAsk >= StackedImbalanceLookback)
			    stackedImbSignal = 1;
			else if (maxConsecutiveBid >= StackedImbalanceLookback)
			    stackedImbSignal = -1;
			else
			    stackedImbSignal = 0;


            // Reversal POC: simple logic based on previous bar color and current POC position
            if (CurrentBar >= 1)
            {
                bool prevBarGreen = Closes[0][1] >= Opens[0][1];
                bool prevBarRed = Closes[0][1] < Opens[0][1];
                bool currBarGreen = Closes[0][0] >= Opens[0][0];
                bool currBarRed = Closes[0][0] < Opens[0][0];
                if (prevBarRed && currBarGreen && Low[0] < Low[1] && pocPos == -1)
                    reversalPOCSignal = 1;
                else if (prevBarGreen && currBarRed && High[0] > High[1] && pocPos == 1)
                    reversalPOCSignal = -1;
                else
                    reversalPOCSignal = 0;
            }
            else
            {
                reversalPOCSignal = 0;
            }

            // Sweep Signal: count consecutive bins with very low volume
            int consAskLow = 0, consBidLow = 0;
            foreach (var bin in ascendingBins)
            {
                double askVol = GetAskVolumeForPrice[CurrentBar].ContainsKey(bin.Key) ? GetAskVolumeForPrice[CurrentBar][bin.Key] : 0;
                double bidVol = GetBidVolumeForPrice[CurrentBar].ContainsKey(bin.Key) ? GetBidVolumeForPrice[CurrentBar][bin.Key] : 0;
                if (askVol <= NearZeroaThreshold)
                    consAskLow++;
                else
                    consAskLow = 0;
                if (bidVol <= NearZeroaThreshold)
                    consBidLow++;
                else
                    consBidLow = 0;
            }
            if (consAskLow >= SweepLookback)
                sweepSignal = -1;
            else if (consBidLow >= SweepLookback)
                sweepSignal = 1;
            else
                sweepSignal = 0;

            // Delta Sequence Signal: compare built-in tick deltas over the last few bars
            if (CurrentBar >= DeltaSequenceLookback)
            {
                bool isIncreasing = true;
                bool isDecreasing = true;
                for (int i = CurrentBar - DeltaSequenceLookback + 1; i <= CurrentBar; i++)
                {
                    double prevDelta = barsType.Volumes[i - 1].BarDelta;
                    double currDelta = barsType.Volumes[i].BarDelta;
                    if (currDelta <= prevDelta) isIncreasing = false;
                    if (currDelta >= prevDelta) isDecreasing = false;
                }
                if (isIncreasing)
				{
                    deltaSeqSignal = 1;
					deltaSignal = "I";
				}
                else if (isDecreasing)
				{
                    deltaSeqSignal = -1;
					deltaSignal = "D";
				}
                else
				{
                    deltaSeqSignal = 0;
					deltaSignal = "";
				}
            }
            else
            {
                deltaSeqSignal = 0;
				deltaSignal = "";
            }
			
			// Bar Ratio: Calculate a simple bar ratio between second and base price aggregates ---
            if (Close[0] > Open[0])
            {
                double lowBidVol = (GetBidVolumeForPrice[CurrentBar].ContainsKey(lowPrice + binInterval)) ? GetBidVolumeForPrice[CurrentBar][lowPrice + binInterval] : 0;
                double lowBidBase = (GetBidVolumeForPrice[CurrentBar].ContainsKey(lowPrice)) ? GetBidVolumeForPrice[CurrentBar][lowPrice] : 1;
                ratio = lowBidBase != 0 ? lowBidVol / lowBidBase : 0;
				
				// Set signal: +1 if ratio exceeds the LargeRatioThreshold, else 0.
    			largeRatioSignal = (ratio > LargeRatioThreshold) ? 1 : 0;
            }
            else if (Close[0] < Open[0])
            {
                double maxAsk = (GetAskVolumeForPrice[CurrentBar].Keys.Count > 0) ? GetAskVolumeForPrice[CurrentBar].Keys.Max() : 0;
                double highAskVol = (GetAskVolumeForPrice[CurrentBar].ContainsKey(maxAsk - binInterval)) ? GetAskVolumeForPrice[CurrentBar][maxAsk - binInterval] : 0;
                double highAskBase = (GetAskVolumeForPrice[CurrentBar].ContainsKey(maxAsk)) ? GetAskVolumeForPrice[CurrentBar][maxAsk] : 1;
                ratio = highAskBase != 0 ? highAskVol / highAskBase : 0;
				
				// Set signal: -1 if ratio exceeds the LargeRatioThreshold, else 0.
    			largeRatioSignal = (ratio > LargeRatioThreshold) ? -1 : 0;
            }
            else
            {
                ratio = 0;
				largeRatioSignal = 0;
            }
			
			// --- Delta Divergence: Trend reversal signal where lowest/highest bar has opposite delta ---
			if (CurrentBar >= DivergenceLookback)
			{			    
			    int lowestIndex = LowestBar(Low, DivergenceLookback);
			    int highestIndex = HighestBar(High, DivergenceLookback);
			
			    // For a Long divergence:
			    // - The current bar is the lowest (i.e. lowestIndex == 0)
			    // - The current bar is green (Close >= Open)
			    // - The bar delta is positive.
			    if (lowestIndex == 0 && Close[0] >= Open[0] && barDelta < 0)
			    {
			        divergenceSignal = 1;
			    }
			    // For a Short divergence:
			    // - The current bar is the highest (i.e. highestIndex == 0)
			    // - The current bar is red (Close < Open)
			    // - The bar delta is negative.
			    else if (highestIndex == 0 && Close[0] < Open[0] && barDelta > 0)
			    {
			        divergenceSignal = -1;
			    }
			    else
			    {
			        divergenceSignal = 0;
			    }
			}
			else
			{
			    divergenceSignal = 0;
			}
			
			// --- Delta Flip: Sudden reverse of delta ---
			if (CurrentBar >= 1 && GetDeltaForBar.ContainsKey(CurrentBar))
			{
			    double bar1Delta    = GetDeltaForBar[CurrentBar - 1];
			    double bar1MinDelta = GetMinDeltaForBar[CurrentBar - 1];
			    double bar1MaxDelta = GetMaxDeltaForBar[CurrentBar - 1];
			    double bar2Delta    = GetDeltaForBar[CurrentBar];
			    double bar2MinDelta = GetMinDeltaForBar[CurrentBar];
			    double bar2MaxDelta = GetMaxDeltaForBar[CurrentBar];
				
				// Define thresholds
			    double closeToMinMaxThreshold  = 10.0;
			
			    // Case A: Bullish Flip
			    // Previous bar closes on its min-delta and has max-delta ~0,
			    // Current bar closes on its max-delta and has min-delta ~0.
			    bool bullishFlip = ( Math.Abs(bar1Delta - bar1MinDelta) < closeToMinMaxThreshold &&
			                         Math.Abs(bar1MaxDelta) < NearZeroaThreshold &&
			                         Math.Abs(bar2Delta - bar2MaxDelta) < closeToMinMaxThreshold &&
			                         Math.Abs(bar2MinDelta) < NearZeroaThreshold );
			
			    // Case B: Bearish Flip
			    // Previous bar closes on its max-delta and has min-delta ~0,
			    // Current bar closes on its min-delta and has max-delta ~0.
			    bool bearishFlip = ( Math.Abs(bar1Delta - bar1MaxDelta) < closeToMinMaxThreshold &&
			                         Math.Abs(bar1MinDelta) < NearZeroaThreshold &&
			                         Math.Abs(bar2Delta - bar2MinDelta) < closeToMinMaxThreshold &&
			                         Math.Abs(bar2MaxDelta) < NearZeroaThreshold );
			
			    // Set the flip signal based on which case is true
			    if (bullishFlip)
			        deltaFlipSignal = 1;
			    else if (bearishFlip)
			        deltaFlipSignal = -1;
			    else
			        deltaFlipSignal = 0;
			}
			else
			{
			    deltaFlipSignal = 0;
			}
			
			// --- Delta Flip: Sudden reverse of delta ---
			if (CurrentBar >= 2 && GetDeltaForBar.ContainsKey(CurrentBar))
			{
				double bar1Delta = GetDeltaForBar[CurrentBar - 2];
			    double bar2Delta = GetDeltaForBar[CurrentBar - 1];
			    double bar3Delta = GetDeltaForBar[CurrentBar];
			
			    
			    bool bar1BigPos = (bar1Delta >= DeltaThreshold1);
			    bool bar1BigNeg = (bar1Delta <= -DeltaThreshold1);
			
			    bool bar2BigPos = (bar2Delta >= DeltaThreshold1);
			    bool bar2BigNeg = (bar2Delta <= -DeltaThreshold1);
			
			    bool bar3BigPos = (bar3Delta >= DeltaThreshold1);
			    bool bar3BigNeg = (bar3Delta <= -DeltaThreshold1);
						    
			    bool emaSlopeUp   = (EMA(5)[0] > EMA(5)[1]);
			    bool emaSlopeDown = (EMA(5)[0] < EMA(5)[1]);
			
			    bool bar3GapUp   = (GetVALForBar[CurrentBar] > GetVAHForBar[CurrentBar - 1] + binInterval);
			    bool bar3GapDown = (GetVAHForBar[CurrentBar] < GetVALForBar[CurrentBar - 1] - binInterval);
			
			    // -------------
			    // 5) Build the "Long" condition
			    //    bar1: big negative delta
			    //    bar2: big positive delta
			    //    bar3: EMA(5) slope up, AND (big positive delta OR gap up)
			    // -------------
			    bool longTrap = bar1BigNeg 
			                    && bar2BigPos
			                    && emaSlopeUp
			                    && (bar3BigPos || bar3GapUp);
			
			    // -------------
			    // 6) Build the "Short" condition
			    //    bar1: big positive delta
			    //    bar2: big negative delta
			    //    bar3: EMA(5) slope down, AND (big negative delta OR gap down)
			    // -------------
			    bool shortTrap = bar1BigPos
			                     && bar2BigNeg
			                     && emaSlopeDown
			                     && (bar3BigNeg || bar3GapDown);
			
			    // -------------
			    // 7) Combine into an integer signal
			    //    +1 for LONG trap, -1 for SHORT trap, 0 otherwise
			    // -------------
			    int deltaTrapSignal = 0;
			    if (longTrap)
			        deltaTrapSignal = 1;
			    else if (shortTrap)
			        deltaTrapSignal = -1;
			    else
			        deltaTrapSignal = 0;
			} 
			else
			{
			    deltaTrapSignal = 0;
			}
			
			// --- Absorption: Large delta recovering to within the aborption difference threshold
		    // Bullish Absorption:
		    // If the bar shows an extreme negative delta (currentMinDelta < -100) and recovers so that it ends above zero...
		    if (minDelta < -DeltaThreshold1 && barDelta > -NearZeroaThreshold)
		    {
		        absorptionSignal = 1;
		    }
		    // Bearish Absorption:
		    // If the bar shows an extreme positive delta (currentMaxDelta > 100) and falls so that its net delta is negative...
		    else if (maxDelta > DeltaThreshold1 && barDelta < NearZeroaThreshold)
		    {
		        absorptionSignal = -1;
		    }
		    else
		    {
		        absorptionSignal = 0;
		    }
			
			
			// --- Exhaustion: Based on prints (threshold set to 10 by default)			
			// Recompute the ordered bins from the aggregated total volume dictionary.
			var bins = GetTotalVolumeForPrice[CurrentBar].OrderBy(x => x.Key).ToList();
			if (bins.Count > 0)
			{
			    double lowestBin = bins.First().Key;
			    double highestBin = bins.Last().Key;
			
			    // Bearish exhaustion print: red candle with almost no ask volume at the high of the bar.
			    if (Close[0] < Open[0])  // red candle
			    {
			        double askVolAtHigh = GetAskVolumeForPrice[CurrentBar].ContainsKey(highestBin) ? GetAskVolumeForPrice[CurrentBar][highestBin] : 0;
			        if (askVolAtHigh < ExhaustionThreshold)
			            exhaustionSignal = -1;
			        else
			            exhaustionSignal = 0;
			    }
			    // Bullish exhaustion print: green candle with almost no bid volume at the low of the bar.
			    else if (Close[0] >= Open[0])  // green candle
			    {
			        double bidVolAtLow = GetBidVolumeForPrice[CurrentBar].ContainsKey(lowestBin) ? GetBidVolumeForPrice[CurrentBar][lowestBin] : 0;
			        if (bidVolAtLow < ExhaustionThreshold)
			            exhaustionSignal = 1;
			        else
			            exhaustionSignal = 0;
			    }
			    else
			    {
			        exhaustionSignal = 0;
			    }
			}
			else
			{
			    exhaustionSignal = 0;
			}
			
			// --- vaGapSignal: Check for a gap between the current bar's value area and the previous bar's value area.
			// For longs: current bar's VAL is above previous bar's VAH by at least one binInterval.
			// For shorts: current bar's VAH is below previous bar's VAL by at least one binInterval.
			if (CurrentBar >= 1)
			{
			    // Long gap condition: current bar's value area is shifted upward
			    if (val > GetVAHForBar[CurrentBar - 1] + binInterval)
			        vaGapSignal = 1;
			    // Short gap condition: current bar's value area is shifted downward
			    else if (vah < GetVALForBar[CurrentBar - 1] - binInterval)
			        vaGapSignal = -1;
			    else
			        vaGapSignal = 0;
			}
			else
			{
			    vaGapSignal = 0;
			}
						
			
			// --- Set dicitionary values for use in OnRender ---
			GetDeltaForBar[CurrentBar] = barDelta;
			GetMaxDeltaForBar[CurrentBar] = maxDelta;
			GetMinDeltaForBar[CurrentBar] = minDelta;
			GetCumDeltaForBar[CurrentBar] = cumulativeDelta;
			GetVolumeForBar[CurrentBar] = totalVolume;			
			GetPOCForBar[CurrentBar] = pocPrice;
			GetVAHForBar[CurrentBar] = vah;
			GetVALForBar[CurrentBar] = val;
			GetRatioForBar[CurrentBar] = ratio;
			GetDeltaPerVolumeForBar[CurrentBar] = deltaPerVolume;
			
			// --- Set dictionary string for table ---
			GetDeltaSignalsForBar[CurrentBar] = deltaSignal;
			
			// --- Set dictionary signals for use in OnRender ---
			GetVolSeqForBar[CurrentBar] = volSeqSignal;
			GetStackedImbForBar[CurrentBar] = stackedImbSignal;
			GetReversalPOCForBar[CurrentBar] = reversalPOCSignal;
			GetSweepForBar[CurrentBar] = sweepSignal;
			GetDeltaSeqForBar[CurrentBar] = deltaSeqSignal;
			GetDivergenceForBar[CurrentBar] = divergenceSignal;
			GetDeltaFlipForBar[CurrentBar] = deltaFlipSignal;
			GetDeltaTrapForBar[CurrentBar] = deltaTrapSignal;
			GetAbsorptionForBar[CurrentBar] = absorptionSignal;
			GetExhaustionForBar[CurrentBar] = exhaustionSignal;
			GetVAGapForBar[CurrentBar] = vaGapSignal;
			GetLargeRatioForBar[CurrentBar] = largeRatioSignal;
			
            // --- Set plot values ---			
			Values[0][0] = barDelta;
            Values[1][0] = minDelta;
            Values[2][0] = maxDelta;
			Values[3][0] = cumulativeDelta;
            Values[4][0] = totalVolume;
            Values[5][0] = pocPrice;
            Values[6][0] = vah;
            Values[7][0] = val;
			Values[8][0] = ratio;
			Values[9][0] = pocPos;
			Values[10][0] = deltaPerVolume; 
			
			// --- Set plot signals ---
            Values[11][0] = volSeqSignal;
            Values[12][0] = stackedImbSignal;			
            Values[13][0] = reversalPOCSignal;
            Values[14][0] = sweepSignal;
            Values[15][0] = deltaSeqSignal;            
			Values[16][0] = divergenceSignal;
			Values[17][0] = deltaFlipSignal;
			Values[18][0] = deltaTrapSignal;
			Values[19][0] = absorptionSignal;
			Values[20][0] = exhaustionSignal;
			Values[21][0] = vaGapSignal;
			Values[22][0] = largeRatioSignal;
			
			// --- Show individual signals for predator ---
			if (EnableVolSeqSignal)
			    DrawSignalDiamond(CurrentBar, "VolSeq", volSeqSignal, VolSeqColor, VolSeqDiamondOffset);
			
			if (EnableStackedImbSignal)
			    DrawSignalDiamond(CurrentBar, "StackedImb", stackedImbSignal, StackedImbColor, StackedImbDiamondOffset);
			
			if (EnableReversalPOCSignal)
			    DrawSignalDiamond(CurrentBar, "ReversalPOC", reversalPOCSignal, RevPOCColor, ReversalPOCDiamondOffset);
			
			if (EnableSweepSignal)
			    DrawSignalDiamond(CurrentBar, "Sweep", sweepSignal, SweepColor, SweepDiamondOffset);
			
			if (EnableDeltaSeqSignal)
			    DrawSignalDiamond(CurrentBar, "DeltaSeq", deltaSeqSignal, DeltaSeqColor, DeltaSeqDiamondOffset);
			
			if (EnableDivergenceSignal)
			    DrawSignalDiamond(CurrentBar, "Divergence", divergenceSignal, DivergenceColor, DivergenceDiamondOffset);
			
			if (EnableDeltaFlipSignal)
			    DrawSignalDiamond(CurrentBar, "DeltaFlip", deltaFlipSignal, DeltaSeqColor, DeltaFlipDiamondOffset);  // Replace color if desired.
			
			if (EnableDeltaTrapSignal)
			    DrawSignalDiamond(CurrentBar, "DeltaTrap", deltaTrapSignal, DeltaSeqColor, DeltaTrapDiamondOffset);  // Replace color if desired.
			
			if (EnableAbsorptionSignal)
			    DrawSignalDiamond(CurrentBar, "Absorption", absorptionSignal, AbsorptionColor, AbsorptionDiamondOffset);
			
			if (EnableExhaustionSignal)
			    DrawSignalDiamond(CurrentBar, "Exhaustion", exhaustionSignal, ExhaustionColor, ExhaustionDiamondOffset);
			
			if (EnableVAGapSignal)
			    DrawSignalDiamond(CurrentBar, "VAGap", vaGapSignal, VAGapColor, VAGapDiamondOffset);
			
			if (EnableLargeRatioSignal)
			    DrawSignalDiamond(CurrentBar, "LargeRatio", largeRatioSignal, LargeRatioColor, LargeRatioDiamondOffset);

            // --- Print a summary for the current bar ---
			if (CurrentBar > lastPrintedBar)
			{
			    lastPrintedBar = CurrentBar;
//			    Print("Time " + Time[0] +
//			          " | Bar " + CurrentBar +
//			          " | Delta=" + barDelta +
//			          " | MaxDelta=" + maxDelta +
//			          " | MinDelta=" + minDelta +
//			          " | CumDelta=" + cumulativeDelta +
//			          " | TotalVol=" + totalVolume +
//			          " | POC=" + pocPrice +
//			          " | VAH=" + vah +
//			          " | VAL=" + val +
//			          " | Ratio=" + ratio +
//			          " | DeltaPerVol=" + deltaPerVolume +
//			          " | DeltaSig=" + deltaSignal +
//			          " | VolSeq=" + volSeqSignal +
//			          " | StackImb=" + stackedImbSignal +
//			          " | RevPOC=" + reversalPOCSignal +
//			          " | Sweep=" + sweepSignal +
//			          " | DeltaSeq=" + deltaSeqSignal +
//			          " | Divergence=" + divergenceSignal +
//			          " | DeltaFlip=" + deltaFlipSignal +
//			          " | DeltaTrap=" + deltaTrapSignal +
//			          " | Absorption=" + absorptionSignal +
//			          " | Exhaustion=" + exhaustionSignal +
//			          " | VAGap=" + vaGapSignal +
//			          " | LargeRatio=" + largeRatioSignal);
			}

			}
			catch(Exception ex)
			{
				Print(ex.ToString());
			}
        }
        #endregion

		#region OnRender and DrawScrollingGrid and DrawLegend
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
		    base.OnRender(chartControl, chartScale);
		    if (RenderTarget == null)
		        return;
		
		    // Get the visible bar range (absolute indices)
		    int firstBar = ChartBars.FromIndex;
		    int lastBar  = ChartBars.ToIndex;
		    float barWidth = (float)chartControl.Properties.BarDistance;
		
		    using (TextFormat textFormat = new TextFormat(Core.Globals.DirectWriteFactory, "Arial", StandardFontSize))
		    {
		        textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
		        textFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
		        textFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
		
				if (EnableFootprint)
				{
			        // Loop through each visible bar
			        for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
			        {
			            // Ensure we have aggregated data for this bar
			            if (!GetTotalVolumeForPrice.ContainsKey(barIndex)
			             || !GetBidVolumeForPrice.  ContainsKey(barIndex)
			             || !GetAskVolumeForPrice.  ContainsKey(barIndex)
			             || !GetPOCForBar.          ContainsKey(barIndex)
			             || !GetVAHForBar.          ContainsKey(barIndex)
			             || !GetVALForBar.          ContainsKey(barIndex))
			                continue;
			
			            // Retrieve the aggregated dictionaries for the current bar
			            Dictionary<double, double> aggTotal = GetTotalVolumeForPrice[barIndex];
			            Dictionary<double, double> aggBid = GetBidVolumeForPrice[barIndex];
			            Dictionary<double, double> aggAsk = GetAskVolumeForPrice[barIndex];
			
			            int totalBarVolume = (int)aggTotal.Values.Sum();
			            if (totalBarVolume <= 0)
			                continue;
			
			            // Order the bins by price in ascending order.
			            var ascendingBins = aggTotal.OrderBy(x => x.Key).ToList();
			            if (ascendingBins.Count == 0)
			                continue;
			
			            // >> Retrieve persistent values calculated in OnBarUpdate:
			            double poc = GetPOCForBar[barIndex];      // The POC (price with maximum volume)
			            double VAHigh = GetVAHForBar[barIndex];     // The Value Area High boundary
			            double VALow = GetVALForBar[barIndex];      // The Value Area Low boundary
			
			            // Use the built-in series to determine bar direction.
			            bool barIsUp = (Bars.GetClose(barIndex) >= Bars.GetOpen(barIndex));
			
			            // Get the X coordinate of the bar.
			            float xBar = chartControl.GetXByBarIndex(ChartBars, barIndex);
			
			            // Loop through each aggregated price bin for this bar.
			            for (int i = 0; i < ascendingBins.Count; i++)
			            {
			                double binPrice = ascendingBins[i].Key;
			                float y = chartScale.GetYByValue(binPrice);
			
			                float xBid = xBar - barWidth * 0.3f - 20;
			                float xAsk = xBar + barWidth * 0.3f - 20;
			                float yText = y - 12;
			
			                // >> Check if this bin's price is within the Value Area boundaries.
			                bool isInValueArea = (binPrice >= VALow && binPrice <= VAHigh);
			                if (isInValueArea)
			                {
			                    // >> Choose color based on bar direction: green if up, red if down.
			                    var vaColor = barIsUp
			                        ? new SharpDX.Color4(0f, 1f, 0f, 0.2f)  // Green for up bars
			                        : new SharpDX.Color4(1f, 0f, 0f, 0.2f); // Red for down bars
			                    float width = (xAsk - xBid) + 40;
			                    RectangleF vaRect = new RectangleF(xBid, yText, width, 24);
			                    using (var vaBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, vaColor))
			                    {
			                        RenderTarget.FillRectangle(vaRect, vaBrush);
			                    }
			                }
			
			                // Always paint the POC bin (a gray rectangle)
			                if (Math.Abs(binPrice - poc) < Instrument.MasterInstrument.TickSize * 0.5)
			                {
			                    float width = (xAsk - xBid) + 40;
			                    RectangleF pocRect = new RectangleF(xBid, yText, width, 24);
			                    using (var pocBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.5f, 0.5f, 0.5f, 0.7f)))
			                    {
			                        RenderTarget.FillRectangle(pocRect, pocBrush);
			                    }
			                }
			
				            // Retrieve bid and ask volumes from aggregated dictionaries
				            int bidVol = aggBid.ContainsKey(binPrice) ? (int)aggBid[binPrice] : 0;
				            int askVol = aggAsk.ContainsKey(binPrice) ? (int)aggAsk[binPrice] : 0;
				
				            using (var bidBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.White))
				            using (var askBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.White))
				            {
				                // Simple diagonal imbalance check:
				                bool isAskImbalance = false;
				                bool isBidImbalance = false;
				                if (i > 0)
				                {
				                    double binPriceBelow = ascendingBins[i - 1].Key;
				                    int bidBelow = aggBid.ContainsKey(binPriceBelow) ? (int)aggBid[binPriceBelow] : 0;
				                    if (askVol >= ImbFact * bidBelow)
				                        isAskImbalance = true;
				                }
				                if (i < ascendingBins.Count - 1)
				                {
				                    double binPriceAbove = ascendingBins[i + 1].Key;
				                    int askAbove = aggAsk.ContainsKey(binPriceAbove) ? (int)aggAsk[binPriceAbove] : 0;
				                    if (bidVol >= ImbFact * askAbove)
				                        isBidImbalance = true;
				                }
				                if (isAskImbalance)
				                    askBrush.Color = new SharpDX.Color4(0f, 0.5f, 1f, 1f);
				                if (isBidImbalance)
				                    bidBrush.Color = new SharpDX.Color4(1f, 0f, 0f, 1f);
				
				                string bidText = bidVol.ToString();
				                using (TextLayout bidLayout = new TextLayout(Core.Globals.DirectWriteFactory, bidText, textFormat, 40, 24))
				                {
				                    RenderTarget.DrawTextLayout(new Vector2(xBid, yText), bidLayout, bidBrush);
				                }
				                string askText = askVol.ToString();
				                using (TextLayout askLayout = new TextLayout(Core.Globals.DirectWriteFactory, askText, textFormat, 40, 24))
				                {
				                    RenderTarget.DrawTextLayout(new Vector2(xAsk, yText), askLayout, askBrush);
				                }
				            }
						}
					}
				}
				if (EnableSignalGrid)
				{
					// Loop through each visible bar
			        for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
			        {
						// Get the X coordinate of the bar.
			            float xBar = chartControl.GetXByBarIndex(ChartBars, barIndex);
						
						if (!GetVolSeqForBar.     ContainsKey(barIndex)
		                 || !GetStackedImbForBar. ContainsKey(barIndex)
		                 || !GetReversalPOCForBar.ContainsKey(barIndex)
		                 || !GetSweepForBar.      ContainsKey(barIndex)
		                 || !GetDeltaSeqForBar.   ContainsKey(barIndex)
		                 || !GetAbsorptionForBar. ContainsKey(barIndex)
		                 || !GetExhaustionForBar. ContainsKey(barIndex)
		                 || !GetVAGapForBar.      ContainsKey(barIndex)
		                 || !GetLargeRatioForBar. ContainsKey(barIndex))
		                    continue;
						
						// --- Draw signal grid for each visible bar (if signal data is available)
						// Build an array of the 10 signal values for this bar:
						int[] signals = new int[10];
						signals[0] = GetVolSeqForBar[barIndex];
						signals[1] = GetStackedImbForBar[barIndex];
						signals[2] = GetReversalPOCForBar[barIndex];
						signals[3] = GetSweepForBar[barIndex];
						signals[4] = GetDeltaSeqForBar[barIndex];
						signals[5] = GetDivergenceForBar[barIndex];
						//signals[6] = GetDeltaFlipForBar[barIndex];
						//signals[7] = GetDeltaTrapForBar[barIndex];
						signals[6] = GetAbsorptionForBar[barIndex];
						signals[7] = GetExhaustionForBar[barIndex];
						signals[8] = GetVAGapForBar[barIndex];
						signals[9] = GetLargeRatioForBar[barIndex];
						
						// Define the corresponding color for each signal cell:
						SharpDX.Color4[] signalColors = new SharpDX.Color4[10];
						signalColors[0] = ConvertMediaBrushToColor4(VolSeqColor);
						signalColors[1] = ConvertMediaBrushToColor4(StackedImbColor);
						signalColors[2] = ConvertMediaBrushToColor4(RevPOCColor);
						signalColors[3] = ConvertMediaBrushToColor4(SweepColor);
						signalColors[4] = ConvertMediaBrushToColor4(DeltaSeqColor);
						signalColors[5] = ConvertMediaBrushToColor4(DivergenceColor);
						signalColors[6] = ConvertMediaBrushToColor4(AbsorptionColor);
						signalColors[7] = ConvertMediaBrushToColor4(ExhaustionColor);
						signalColors[8] = ConvertMediaBrushToColor4(VAGapColor);
						signalColors[9] = ConvertMediaBrushToColor4(LargeRatioColor);
						
						
						
				
						
						// --- Determine if there are positive or negative signals for this bar
						bool hasLong = false;
						bool hasShort = false;
						for (int s = 0; s < signals.Length; s++)
						{
						    if (signals[s] > 0)
						        hasLong = true;
						    else if (signals[s] < 0)
						        hasShort = true;
						}
						
						// Instead of using barIsLong to decide grid placement, use the signal sign:
						// For positive signals (long grid), always base Y on the bar's low:
						if (hasLong)
						{
						    int GridCellSize = 10;    // Each cell is 20x20 pixels
						    int GridOffset = SignalGridOffset;      // Distance from bar low
						    int gridCols = 2;
						    int gridRows = 5;
						    int gridWidth = gridCols * GridCellSize;
						    int gridHeight = gridRows * GridCellSize;
						    //float xBar = chartControl.GetXByBarIndex(ChartBars, barIndex);
						    float gridX = xBar - gridWidth / 2;
						
						    // Always draw the long grid below the bar's low.
						    float barLowY = chartScale.GetYByValue(Bars.GetLow(barIndex));
						    float longGridY = barLowY + GridOffset;
						
						    // Draw cells for long signals: fill only if the corresponding signal > 0.
						    for (int row = 0; row < gridRows; row++)
						    {
						        for (int col = 0; col < gridCols; col++)
						        {
						            int cellIndex = row * gridCols + col;
						            float cellX = gridX + col * GridCellSize;
						            float cellY = longGridY + row * GridCellSize;
						            RectangleF cellRect = new RectangleF(cellX, cellY, GridCellSize, GridCellSize);
						
						            if (signals[cellIndex] > 0)
						            {
						                using (var cellBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, signalColors[cellIndex]))
						                {
						                    RenderTarget.FillRectangle(cellRect, cellBrush);
						                }
						            }
						            // Draw border for every cell.
						            using (var borderBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(1f, 1f, 1f, 1f)))
						            {
						                RenderTarget.DrawRectangle(cellRect, borderBrush);
						            }
						        }
						    }
						}
						
						// For negative signals (short grid), always base Y on the bar's high:
						if (hasShort)
						{
						    int GridCellSize = 10;    // Each cell is 20x20 pixels
						    int GridOffset = SignalGridOffset;    // Distance from bar high
						    int gridCols = 2;
						    int gridRows = 5;
						    int gridWidth = gridCols * GridCellSize;
						    int gridHeight = gridRows * GridCellSize;
						    //float xBar = chartControl.GetXByBarIndex(ChartBars, barIndex);
						    float gridX = xBar - gridWidth / 2;
						
						    // Always draw the short grid above the bar's high.
						    float barHighY = chartScale.GetYByValue(Bars.GetHigh(barIndex));
						    float shortGridY = barHighY - GridOffset - gridHeight;
						
						    // Draw cells for short signals: fill only if the corresponding signal < 0.
						    for (int row = 0; row < gridRows; row++)
						    {
						        for (int col = 0; col < gridCols; col++)
						        {
						            int cellIndex = row * gridCols + col;
						            float cellX = gridX + col * GridCellSize;
						            float cellY = shortGridY + row * GridCellSize;
						            RectangleF cellRect = new RectangleF(cellX, cellY, GridCellSize, GridCellSize);
						
						            if (signals[cellIndex] < 0)
						            {
						                using (var cellBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, signalColors[cellIndex]))
						                {
						                    RenderTarget.FillRectangle(cellRect, cellBrush);
						                }
						            }
						            // Draw border for every cell.
						            using (var borderBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(1f, 1f, 1f, 1f)))
						            {
						                RenderTarget.DrawRectangle(cellRect, borderBrush);
						            }
						        }
						    }
						}
					}

		        }
		    }
			// Draw signal grid legend
			if (EnableSignalGrid)
				DrawLegend(chartControl, chartScale);

		    // Draw persistent computed values below each bar
			if (EnableSummaryGrid)
		    	DrawScrollingGrid(chartControl, chartScale);
		}
		
		private void DrawLegend(ChartControl chartControl, ChartScale chartScale)
		{
		    // Get the panel for coordinate reference
		    ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
		    
		    // Legend parameters
		    int cellSize     = 25;   // size of the color square
		    int rowHeight    = 35;   // total vertical space per row (square + label)
		    int columnWidth  = 150;  // total horizontal space per column (square + label)
		    int margin       = 60;   // distance from the chart edge
		    
		    // We have 5 rows x 2 columns = 10 signals
		    int legendRows   = 5;
		    int legendCols   = 2;
		    
		    // Position the legend near the top-right
		    //float legendX = panel.X + panel.W - margin - (legendCols * columnWidth);
		    float legendX = panel.X + margin;
			float legendY = panel.Y + margin;
		
		    // The labels for your 10 signals, row-major order:
		    string[] signalLabels = new string[]
		    {
		        "Volume Sequence", "Stacked Imbalance", "Reverse POC", "Sweep", "Delta Sequence",
		        "Divergence", "Absorption", "Exhaustion", "Value Area Gap", "Large Ratio"
		    };
		
		    // Corresponding colors for each label
		    SharpDX.Color4[] legendColors = new SharpDX.Color4[10];
		    legendColors[0] = ConvertMediaBrushToColor4(VolSeqColor);
		    legendColors[1] = ConvertMediaBrushToColor4(StackedImbColor);
		    legendColors[2] = ConvertMediaBrushToColor4(RevPOCColor);
		    legendColors[3] = ConvertMediaBrushToColor4(SweepColor);
		    legendColors[4] = ConvertMediaBrushToColor4(DeltaSeqColor);
		    legendColors[5] = ConvertMediaBrushToColor4(DivergenceColor);
		    legendColors[6] = ConvertMediaBrushToColor4(AbsorptionColor);
		    legendColors[7] = ConvertMediaBrushToColor4(ExhaustionColor);
		    legendColors[8] = ConvertMediaBrushToColor4(VAGapColor);
		    legendColors[9] = ConvertMediaBrushToColor4(LargeRatioColor);
		
		    // Create a text format for the legend labels
		    using (TextFormat legendTextFormat = new TextFormat(Core.Globals.DirectWriteFactory, "Arial", 14))
		    {
		        legendTextFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
		        legendTextFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
		        
		        // Loop over each cell in the 5x2 layout
		        for (int row = 0; row < legendRows; row++)
		        {
		            for (int col = 0; col < legendCols; col++)
		            {
		                int index = row * legendCols + col;
		                if (index >= legendColors.Length)
		                    break;
		                
		                // Compute the top-left of this cell
		                float cellX = legendX + col * columnWidth;
		                float cellY = legendY + row * rowHeight;
		
		                // Draw the color square
		                RectangleF colorRect = new RectangleF(cellX, cellY, cellSize, cellSize);
		                using (var fillBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, legendColors[index]))
		                {
		                    RenderTarget.FillRectangle(colorRect, fillBrush);
		                }
		                // Draw a white border around the square
		                using (var borderBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.White))
		                {
		                    RenderTarget.DrawRectangle(colorRect, borderBrush);
		                }
		
		                // Draw the label to the right of the color square
		                float labelX = cellX + cellSize + 5;   // small gap after the square
		                float labelY = cellY;                  // align with the square top
		                string labelText = signalLabels[index];
		                
		                using (TextLayout layout = new TextLayout(Core.Globals.DirectWriteFactory, labelText, legendTextFormat, columnWidth - cellSize, rowHeight))
		                {
		                    using (var textBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.White))
		                    {
		                        RenderTarget.DrawTextLayout(new Vector2(labelX, labelY), layout, textBrush);
		                    }
		                }
		            }
		        }
		    }
		}


		
		private void DrawScrollingGrid(ChartControl chartControl, ChartScale chartScale)
		{
		    // Get the current panel (assumes the indicator is on the primary panel)
		    ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
		
		    // Grid parameters
		    int rowCount = 5;       // Rows: "Delta Signals", "Delta", "MaxDelta", "MinDelta", "Volume"
		    float rowHeight = 25f;
		    float tableHeight = rowHeight * rowCount;
		    float labelColWidth = 150f;   // Fixed width for the label column
		    float colWidth = (float)chartControl.Properties.BarDistance;
		
		    // Label column position: fixed at bottom-left with a margin.
		    float labelX = panel.X;
		    float baseY = panel.Y + panel.H - tableHeight - 10;
		
		    // The left edge for data columns starts just after the label column.
		    float dataStartX = labelX + labelColWidth;
		    float dataEndX = dataStartX;  // We'll update as we find further columns
		
		    // Get the visible bar range (absolute bar indices)
		    int firstBar = ChartBars.FromIndex;
		    int lastBar = ChartBars.ToIndex;
		
		    // Scan visible bars to determine the rightmost data column X
		    for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
		    {
		        // Check if we have data for this bar (using one of your dictionaries)
		        if (!GetVolumeForBar.ContainsKey(barIndex))
		            continue;
		
		        float barX = chartControl.GetXByBarIndex(ChartBars, barIndex);
		        float colRight = barX + (colWidth / 2);
		        if (colRight > dataEndX)
		            dataEndX = colRight;
		    }
		
		    // 1) Draw the background row strips behind all data columns
		    using (var rowStripBrushEven = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.1f, 0.1f, 0.1f, 1f)))
		    using (var rowStripBrushOdd = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.2f, 0.2f, 0.2f, 1f)))
		    {
		        for (int r = 0; r < rowCount; r++)
		        {
		            float rowY = baseY + r * rowHeight;
		            RectangleF rowRect = new RectangleF(dataStartX, rowY, dataEndX - dataStartX, rowHeight);
		
		            if (r % 2 == 0)
		                RenderTarget.FillRectangle(rowRect, rowStripBrushEven);
		            else
		                RenderTarget.FillRectangle(rowRect, rowStripBrushOdd);
		        }
		    }
		
		    // 2) Draw each bar’s column on top of the row strips.
		    using (TextFormat tfCols = new TextFormat(Core.Globals.DirectWriteFactory, "Arial", StandardFontSize))
		    {
		        tfCols.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
		        tfCols.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
		        tfCols.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
		
		        for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
		        {
		            // Only draw if data exists for this bar
		            if (!GetVolumeForBar.ContainsKey(barIndex))
		                continue;
		
		            float barX = chartControl.GetXByBarIndex(ChartBars, barIndex);
		            float colX = barX - colWidth / 2;
		
		            // Retrieve values from your dictionaries
		            // (Assuming GetDeltaSignalsForBar is declared as Dictionary<int, string>)
		            string deltaSignals = GetDeltaSignalsForBar != null && GetDeltaSignalsForBar.ContainsKey(barIndex)
		                                  ? GetDeltaSignalsForBar[barIndex]
		                                  : "";
		            double deltaVal = GetDeltaForBar[barIndex];
		            double maxDeltaVal = GetMaxDeltaForBar[barIndex];
		            double minDeltaVal = GetMinDeltaForBar[barIndex];
		            double volumeVal = GetVolumeForBar[barIndex];
		
		            // Prepare cell values for the 5 rows
		            string[] rowValues = new string[]
		            {
		                deltaSignals,
		                deltaVal.ToString("N0"),
		                maxDeltaVal.ToString("N0"),
		                minDeltaVal.ToString("N0"),
		                volumeVal.ToString("N0")
		            };
		
		            // Draw each cell in the column
		            for (int r = 0; r < rowCount; r++)
		            {
		                float rowY = baseY + r * rowHeight;
		                RectangleF cellRect = new RectangleF(colX, rowY, colWidth, rowHeight);
		
		                // Default cell background color
		                SharpDX.Color4 cellBgColor = new SharpDX.Color4(0.2f, 0.2f, 0.2f, 1f);
		                double value;
		                bool parsed = double.TryParse(rowValues[r], out value);
		
		                // For Delta, MaxDelta, MinDelta rows (rows 1, 2, 3), choose a color based on thresholds.
		                if (parsed && (r == 1 || r == 2 || r == 3))
		                {
		                    if (Math.Abs(value) < NearZeroaThreshold)
		                    {
		                        cellBgColor = ConvertMediaBrushToColor4(ZeroDeltaColor);
		                    }
		                    else if (value > 0)
		                    {
		                        if (value <= DeltaThreshold1)
		                            cellBgColor = ConvertMediaBrushToColor4(DeltaLowPositiveColor);
		                        else if (value <= DeltaThreshold2)
		                            cellBgColor = ConvertMediaBrushToColor4(DeltaMediumPositiveColor);
		                        else
		                            cellBgColor = ConvertMediaBrushToColor4(DeltaHighPositiveColor);
		                    }
		                    else
		                    {
		                        double absVal = Math.Abs(value);
		                        if (absVal <= DeltaThreshold1)
		                            cellBgColor = ConvertMediaBrushToColor4(DeltaLowNegativeColor);
		                        else if (absVal <= DeltaThreshold2)
		                            cellBgColor = ConvertMediaBrushToColor4(DeltaMediumNegativeColor);
		                        else
		                            cellBgColor = ConvertMediaBrushToColor4(DeltaHighNegativeColor);
		                    }
		                }
		                // For the Volume row (row 4)
		                else if (parsed && r == 4)
		                {
		                    if (value > VolumeThreshold)
		                        cellBgColor = ConvertMediaBrushToColor4(HighVolumeColor);
		                }
		                // Row 0 ("Delta Signals") keeps its default background or you can customize it further.
		
		                // Fill the cell background
		                using (var cellBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, cellBgColor))
		                {
		                    RenderTarget.FillRectangle(cellRect, cellBrush);
		                }
		
		                // Draw the cell text
		                using (TextLayout valueLayout = new TextLayout(Core.Globals.DirectWriteFactory, rowValues[r], tfCols, colWidth, rowHeight))
		                {
		                    using (var textBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.White))
		                    {
		                        RenderTarget.DrawTextLayout(new Vector2(colX + 5, rowY), valueLayout, textBrush);
		                    }
		                }
		            }
		        }
		    }
		
		    // 3) Draw the pinned label column so it remains visible
		    RectangleF labelBg = new RectangleF(labelX, baseY, labelColWidth, tableHeight);
		    using (var bgBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.2f, 0.2f, 0.2f, 1f)))
		    {
		        RenderTarget.FillRectangle(labelBg, bgBrush);
		    }
		
		    string[] rowLabels = new string[] { "Delta Signals", "Delta", "MaxDelta", "MinDelta", "Volume" };
		
		    using (TextFormat tfLabels = new TextFormat(Core.Globals.DirectWriteFactory, "Arial", StandardFontSize))
		    {
		        tfLabels.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
		        tfLabels.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
		
		        for (int r = 0; r < rowCount; r++)
		        {
		            float rowY = baseY + r * rowHeight;
		            RectangleF rowRect = new RectangleF(labelX, rowY, labelColWidth, rowHeight);
		
		            // Optionally add shading to the label area
		            if (r % 2 == 0)
		            {
		                using (var rowBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.1f, 0.1f, 0.1f, 1f)))
		                {
		                    RenderTarget.FillRectangle(rowRect, rowBrush);
		                }
		            }
		
		            using (TextLayout labelLayout = new TextLayout(Core.Globals.DirectWriteFactory, rowLabels[r], tfLabels, labelColWidth, rowHeight))
		            {
		                using (var textBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.White))
		                {
		                    RenderTarget.DrawTextLayout(new Vector2(labelX + 5, rowY), labelLayout, textBrush);
		                }
		            }
		        }
		    }
		}
		
		#endregion

        #region Helper Methods
		private void DrawSignalDiamond(int bar, string signalName, int signalValue, System.Windows.Media.Brush signalBrush, int offsetValue)
		{
		    if (signalValue == 0)
		        return;
		
		    double offset = TickSize * offsetValue;
		    double yCoordinate = (signalValue > 0) ? Low[0] - offset : High[0] + offset;			
			string tagSuffix = (signalValue > 0) ? "_long_" : "_short_";
		    string tag = signalName + tagSuffix + bar.ToString();
		
		    Draw.Diamond(this, tag, true, 0, yCoordinate, signalBrush);
		}
		private SharpDX.Color4 ConvertMediaBrushToColor4(System.Windows.Media.Brush mediaBrush)
		{
		    var scb = mediaBrush as System.Windows.Media.SolidColorBrush;
		    if (scb == null)
		        return new SharpDX.Color4(0.2f, 0.2f, 0.2f, 1f);
		
		    // Convert from sRGB to float values
		    float a = (float)scb.Color.A / 255f;
		    float r = (float)scb.Color.R / 255f;
		    float g = (float)scb.Color.G / 255f;
		    float b = (float)scb.Color.B / 255f;
		    return new SharpDX.Color4(r, g, b, a);
		}
        private void CleanupExtraneousValues(Dictionary<double, double> dict, double lowPrice, double interval)
        {
            var keysToRemove = dict.Keys.Where(price => (price - lowPrice) % interval != 0).ToList();
            foreach (var key in keysToRemove)
            {
                dict.Remove(key);
            }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FootprintOrderFlow[] cacheFootprintOrderFlow;
		public FootprintOrderFlow FootprintOrderFlow(int aggregationInterval, double valueAreaPer, int volumeSeqLookback, int stackedImbalanceLookback, int deltaSequenceLookback, int sweepLookback, int divergenceLookback, double imbFact, double largeRatioThreshold, double smallRatioThreshold, int deltaThreshold1, int deltaThreshold2, int volumeThreshold, int nearZeroaThreshold, int exhaustionThreshold, int standardFontSize, bool enableFootprint, bool enableSummaryGrid, bool enableSignalGrid, int signalGridOffset, System.Windows.Media.Brush deltaLowPositiveColor, System.Windows.Media.Brush deltaMediumPositiveColor, System.Windows.Media.Brush deltaHighPositiveColor, System.Windows.Media.Brush deltaLowNegativeColor, System.Windows.Media.Brush deltaMediumNegativeColor, System.Windows.Media.Brush deltaHighNegativeColor, System.Windows.Media.Brush highVolumeColor, System.Windows.Media.Brush zeroDeltaColor, System.Windows.Media.Brush volSeqColor, System.Windows.Media.Brush stackedImbColor, System.Windows.Media.Brush revPOCColor, System.Windows.Media.Brush sweepColor, System.Windows.Media.Brush deltaSeqColor, System.Windows.Media.Brush divergenceColor, System.Windows.Media.Brush absorptionColor, System.Windows.Media.Brush exhaustionColor, System.Windows.Media.Brush vAGapColor, System.Windows.Media.Brush largeRatioColor, bool enableVolSeqSignal, int volSeqDiamondOffset, bool enableStackedImbSignal, int stackedImbDiamondOffset, bool enableReversalPOCSignal, int reversalPOCDiamondOffset, bool enableSweepSignal, int sweepDiamondOffset, bool enableDeltaSeqSignal, int deltaSeqDiamondOffset, bool enableDivergenceSignal, int divergenceDiamondOffset, bool enableDeltaFlipSignal, int deltaFlipDiamondOffset, bool enableDeltaTrapSignal, int deltaTrapDiamondOffset, bool enableAbsorptionSignal, int absorptionDiamondOffset, bool enableExhaustionSignal, int exhaustionDiamondOffset, bool enableVAGapSignal, int vAGapDiamondOffset, bool enableLargeRatioSignal, int largeRatioDiamondOffset)
		{
			return FootprintOrderFlow(Input, aggregationInterval, valueAreaPer, volumeSeqLookback, stackedImbalanceLookback, deltaSequenceLookback, sweepLookback, divergenceLookback, imbFact, largeRatioThreshold, smallRatioThreshold, deltaThreshold1, deltaThreshold2, volumeThreshold, nearZeroaThreshold, exhaustionThreshold, standardFontSize, enableFootprint, enableSummaryGrid, enableSignalGrid, signalGridOffset, deltaLowPositiveColor, deltaMediumPositiveColor, deltaHighPositiveColor, deltaLowNegativeColor, deltaMediumNegativeColor, deltaHighNegativeColor, highVolumeColor, zeroDeltaColor, volSeqColor, stackedImbColor, revPOCColor, sweepColor, deltaSeqColor, divergenceColor, absorptionColor, exhaustionColor, vAGapColor, largeRatioColor, enableVolSeqSignal, volSeqDiamondOffset, enableStackedImbSignal, stackedImbDiamondOffset, enableReversalPOCSignal, reversalPOCDiamondOffset, enableSweepSignal, sweepDiamondOffset, enableDeltaSeqSignal, deltaSeqDiamondOffset, enableDivergenceSignal, divergenceDiamondOffset, enableDeltaFlipSignal, deltaFlipDiamondOffset, enableDeltaTrapSignal, deltaTrapDiamondOffset, enableAbsorptionSignal, absorptionDiamondOffset, enableExhaustionSignal, exhaustionDiamondOffset, enableVAGapSignal, vAGapDiamondOffset, enableLargeRatioSignal, largeRatioDiamondOffset);
		}

		public FootprintOrderFlow FootprintOrderFlow(ISeries<double> input, int aggregationInterval, double valueAreaPer, int volumeSeqLookback, int stackedImbalanceLookback, int deltaSequenceLookback, int sweepLookback, int divergenceLookback, double imbFact, double largeRatioThreshold, double smallRatioThreshold, int deltaThreshold1, int deltaThreshold2, int volumeThreshold, int nearZeroaThreshold, int exhaustionThreshold, int standardFontSize, bool enableFootprint, bool enableSummaryGrid, bool enableSignalGrid, int signalGridOffset, System.Windows.Media.Brush deltaLowPositiveColor, System.Windows.Media.Brush deltaMediumPositiveColor, System.Windows.Media.Brush deltaHighPositiveColor, System.Windows.Media.Brush deltaLowNegativeColor, System.Windows.Media.Brush deltaMediumNegativeColor, System.Windows.Media.Brush deltaHighNegativeColor, System.Windows.Media.Brush highVolumeColor, System.Windows.Media.Brush zeroDeltaColor, System.Windows.Media.Brush volSeqColor, System.Windows.Media.Brush stackedImbColor, System.Windows.Media.Brush revPOCColor, System.Windows.Media.Brush sweepColor, System.Windows.Media.Brush deltaSeqColor, System.Windows.Media.Brush divergenceColor, System.Windows.Media.Brush absorptionColor, System.Windows.Media.Brush exhaustionColor, System.Windows.Media.Brush vAGapColor, System.Windows.Media.Brush largeRatioColor, bool enableVolSeqSignal, int volSeqDiamondOffset, bool enableStackedImbSignal, int stackedImbDiamondOffset, bool enableReversalPOCSignal, int reversalPOCDiamondOffset, bool enableSweepSignal, int sweepDiamondOffset, bool enableDeltaSeqSignal, int deltaSeqDiamondOffset, bool enableDivergenceSignal, int divergenceDiamondOffset, bool enableDeltaFlipSignal, int deltaFlipDiamondOffset, bool enableDeltaTrapSignal, int deltaTrapDiamondOffset, bool enableAbsorptionSignal, int absorptionDiamondOffset, bool enableExhaustionSignal, int exhaustionDiamondOffset, bool enableVAGapSignal, int vAGapDiamondOffset, bool enableLargeRatioSignal, int largeRatioDiamondOffset)
		{
			if (cacheFootprintOrderFlow != null)
				for (int idx = 0; idx < cacheFootprintOrderFlow.Length; idx++)
					if (cacheFootprintOrderFlow[idx] != null && cacheFootprintOrderFlow[idx].AggregationInterval == aggregationInterval && cacheFootprintOrderFlow[idx].ValueAreaPer == valueAreaPer && cacheFootprintOrderFlow[idx].VolumeSeqLookback == volumeSeqLookback && cacheFootprintOrderFlow[idx].StackedImbalanceLookback == stackedImbalanceLookback && cacheFootprintOrderFlow[idx].DeltaSequenceLookback == deltaSequenceLookback && cacheFootprintOrderFlow[idx].SweepLookback == sweepLookback && cacheFootprintOrderFlow[idx].DivergenceLookback == divergenceLookback && cacheFootprintOrderFlow[idx].ImbFact == imbFact && cacheFootprintOrderFlow[idx].LargeRatioThreshold == largeRatioThreshold && cacheFootprintOrderFlow[idx].SmallRatioThreshold == smallRatioThreshold && cacheFootprintOrderFlow[idx].DeltaThreshold1 == deltaThreshold1 && cacheFootprintOrderFlow[idx].DeltaThreshold2 == deltaThreshold2 && cacheFootprintOrderFlow[idx].VolumeThreshold == volumeThreshold && cacheFootprintOrderFlow[idx].NearZeroaThreshold == nearZeroaThreshold && cacheFootprintOrderFlow[idx].ExhaustionThreshold == exhaustionThreshold && cacheFootprintOrderFlow[idx].StandardFontSize == standardFontSize && cacheFootprintOrderFlow[idx].EnableFootprint == enableFootprint && cacheFootprintOrderFlow[idx].EnableSummaryGrid == enableSummaryGrid && cacheFootprintOrderFlow[idx].EnableSignalGrid == enableSignalGrid && cacheFootprintOrderFlow[idx].SignalGridOffset == signalGridOffset && cacheFootprintOrderFlow[idx].DeltaLowPositiveColor == deltaLowPositiveColor && cacheFootprintOrderFlow[idx].DeltaMediumPositiveColor == deltaMediumPositiveColor && cacheFootprintOrderFlow[idx].DeltaHighPositiveColor == deltaHighPositiveColor && cacheFootprintOrderFlow[idx].DeltaLowNegativeColor == deltaLowNegativeColor && cacheFootprintOrderFlow[idx].DeltaMediumNegativeColor == deltaMediumNegativeColor && cacheFootprintOrderFlow[idx].DeltaHighNegativeColor == deltaHighNegativeColor && cacheFootprintOrderFlow[idx].HighVolumeColor == highVolumeColor && cacheFootprintOrderFlow[idx].ZeroDeltaColor == zeroDeltaColor && cacheFootprintOrderFlow[idx].VolSeqColor == volSeqColor && cacheFootprintOrderFlow[idx].StackedImbColor == stackedImbColor && cacheFootprintOrderFlow[idx].RevPOCColor == revPOCColor && cacheFootprintOrderFlow[idx].SweepColor == sweepColor && cacheFootprintOrderFlow[idx].DeltaSeqColor == deltaSeqColor && cacheFootprintOrderFlow[idx].DivergenceColor == divergenceColor && cacheFootprintOrderFlow[idx].AbsorptionColor == absorptionColor && cacheFootprintOrderFlow[idx].ExhaustionColor == exhaustionColor && cacheFootprintOrderFlow[idx].VAGapColor == vAGapColor && cacheFootprintOrderFlow[idx].LargeRatioColor == largeRatioColor && cacheFootprintOrderFlow[idx].EnableVolSeqSignal == enableVolSeqSignal && cacheFootprintOrderFlow[idx].VolSeqDiamondOffset == volSeqDiamondOffset && cacheFootprintOrderFlow[idx].EnableStackedImbSignal == enableStackedImbSignal && cacheFootprintOrderFlow[idx].StackedImbDiamondOffset == stackedImbDiamondOffset && cacheFootprintOrderFlow[idx].EnableReversalPOCSignal == enableReversalPOCSignal && cacheFootprintOrderFlow[idx].ReversalPOCDiamondOffset == reversalPOCDiamondOffset && cacheFootprintOrderFlow[idx].EnableSweepSignal == enableSweepSignal && cacheFootprintOrderFlow[idx].SweepDiamondOffset == sweepDiamondOffset && cacheFootprintOrderFlow[idx].EnableDeltaSeqSignal == enableDeltaSeqSignal && cacheFootprintOrderFlow[idx].DeltaSeqDiamondOffset == deltaSeqDiamondOffset && cacheFootprintOrderFlow[idx].EnableDivergenceSignal == enableDivergenceSignal && cacheFootprintOrderFlow[idx].DivergenceDiamondOffset == divergenceDiamondOffset && cacheFootprintOrderFlow[idx].EnableDeltaFlipSignal == enableDeltaFlipSignal && cacheFootprintOrderFlow[idx].DeltaFlipDiamondOffset == deltaFlipDiamondOffset && cacheFootprintOrderFlow[idx].EnableDeltaTrapSignal == enableDeltaTrapSignal && cacheFootprintOrderFlow[idx].DeltaTrapDiamondOffset == deltaTrapDiamondOffset && cacheFootprintOrderFlow[idx].EnableAbsorptionSignal == enableAbsorptionSignal && cacheFootprintOrderFlow[idx].AbsorptionDiamondOffset == absorptionDiamondOffset && cacheFootprintOrderFlow[idx].EnableExhaustionSignal == enableExhaustionSignal && cacheFootprintOrderFlow[idx].ExhaustionDiamondOffset == exhaustionDiamondOffset && cacheFootprintOrderFlow[idx].EnableVAGapSignal == enableVAGapSignal && cacheFootprintOrderFlow[idx].VAGapDiamondOffset == vAGapDiamondOffset && cacheFootprintOrderFlow[idx].EnableLargeRatioSignal == enableLargeRatioSignal && cacheFootprintOrderFlow[idx].LargeRatioDiamondOffset == largeRatioDiamondOffset && cacheFootprintOrderFlow[idx].EqualsInput(input))
						return cacheFootprintOrderFlow[idx];
			return CacheIndicator<FootprintOrderFlow>(new FootprintOrderFlow(){ AggregationInterval = aggregationInterval, ValueAreaPer = valueAreaPer, VolumeSeqLookback = volumeSeqLookback, StackedImbalanceLookback = stackedImbalanceLookback, DeltaSequenceLookback = deltaSequenceLookback, SweepLookback = sweepLookback, DivergenceLookback = divergenceLookback, ImbFact = imbFact, LargeRatioThreshold = largeRatioThreshold, SmallRatioThreshold = smallRatioThreshold, DeltaThreshold1 = deltaThreshold1, DeltaThreshold2 = deltaThreshold2, VolumeThreshold = volumeThreshold, NearZeroaThreshold = nearZeroaThreshold, ExhaustionThreshold = exhaustionThreshold, StandardFontSize = standardFontSize, EnableFootprint = enableFootprint, EnableSummaryGrid = enableSummaryGrid, EnableSignalGrid = enableSignalGrid, SignalGridOffset = signalGridOffset, DeltaLowPositiveColor = deltaLowPositiveColor, DeltaMediumPositiveColor = deltaMediumPositiveColor, DeltaHighPositiveColor = deltaHighPositiveColor, DeltaLowNegativeColor = deltaLowNegativeColor, DeltaMediumNegativeColor = deltaMediumNegativeColor, DeltaHighNegativeColor = deltaHighNegativeColor, HighVolumeColor = highVolumeColor, ZeroDeltaColor = zeroDeltaColor, VolSeqColor = volSeqColor, StackedImbColor = stackedImbColor, RevPOCColor = revPOCColor, SweepColor = sweepColor, DeltaSeqColor = deltaSeqColor, DivergenceColor = divergenceColor, AbsorptionColor = absorptionColor, ExhaustionColor = exhaustionColor, VAGapColor = vAGapColor, LargeRatioColor = largeRatioColor, EnableVolSeqSignal = enableVolSeqSignal, VolSeqDiamondOffset = volSeqDiamondOffset, EnableStackedImbSignal = enableStackedImbSignal, StackedImbDiamondOffset = stackedImbDiamondOffset, EnableReversalPOCSignal = enableReversalPOCSignal, ReversalPOCDiamondOffset = reversalPOCDiamondOffset, EnableSweepSignal = enableSweepSignal, SweepDiamondOffset = sweepDiamondOffset, EnableDeltaSeqSignal = enableDeltaSeqSignal, DeltaSeqDiamondOffset = deltaSeqDiamondOffset, EnableDivergenceSignal = enableDivergenceSignal, DivergenceDiamondOffset = divergenceDiamondOffset, EnableDeltaFlipSignal = enableDeltaFlipSignal, DeltaFlipDiamondOffset = deltaFlipDiamondOffset, EnableDeltaTrapSignal = enableDeltaTrapSignal, DeltaTrapDiamondOffset = deltaTrapDiamondOffset, EnableAbsorptionSignal = enableAbsorptionSignal, AbsorptionDiamondOffset = absorptionDiamondOffset, EnableExhaustionSignal = enableExhaustionSignal, ExhaustionDiamondOffset = exhaustionDiamondOffset, EnableVAGapSignal = enableVAGapSignal, VAGapDiamondOffset = vAGapDiamondOffset, EnableLargeRatioSignal = enableLargeRatioSignal, LargeRatioDiamondOffset = largeRatioDiamondOffset }, input, ref cacheFootprintOrderFlow);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FootprintOrderFlow FootprintOrderFlow(int aggregationInterval, double valueAreaPer, int volumeSeqLookback, int stackedImbalanceLookback, int deltaSequenceLookback, int sweepLookback, int divergenceLookback, double imbFact, double largeRatioThreshold, double smallRatioThreshold, int deltaThreshold1, int deltaThreshold2, int volumeThreshold, int nearZeroaThreshold, int exhaustionThreshold, int standardFontSize, bool enableFootprint, bool enableSummaryGrid, bool enableSignalGrid, int signalGridOffset, System.Windows.Media.Brush deltaLowPositiveColor, System.Windows.Media.Brush deltaMediumPositiveColor, System.Windows.Media.Brush deltaHighPositiveColor, System.Windows.Media.Brush deltaLowNegativeColor, System.Windows.Media.Brush deltaMediumNegativeColor, System.Windows.Media.Brush deltaHighNegativeColor, System.Windows.Media.Brush highVolumeColor, System.Windows.Media.Brush zeroDeltaColor, System.Windows.Media.Brush volSeqColor, System.Windows.Media.Brush stackedImbColor, System.Windows.Media.Brush revPOCColor, System.Windows.Media.Brush sweepColor, System.Windows.Media.Brush deltaSeqColor, System.Windows.Media.Brush divergenceColor, System.Windows.Media.Brush absorptionColor, System.Windows.Media.Brush exhaustionColor, System.Windows.Media.Brush vAGapColor, System.Windows.Media.Brush largeRatioColor, bool enableVolSeqSignal, int volSeqDiamondOffset, bool enableStackedImbSignal, int stackedImbDiamondOffset, bool enableReversalPOCSignal, int reversalPOCDiamondOffset, bool enableSweepSignal, int sweepDiamondOffset, bool enableDeltaSeqSignal, int deltaSeqDiamondOffset, bool enableDivergenceSignal, int divergenceDiamondOffset, bool enableDeltaFlipSignal, int deltaFlipDiamondOffset, bool enableDeltaTrapSignal, int deltaTrapDiamondOffset, bool enableAbsorptionSignal, int absorptionDiamondOffset, bool enableExhaustionSignal, int exhaustionDiamondOffset, bool enableVAGapSignal, int vAGapDiamondOffset, bool enableLargeRatioSignal, int largeRatioDiamondOffset)
		{
			return indicator.FootprintOrderFlow(Input, aggregationInterval, valueAreaPer, volumeSeqLookback, stackedImbalanceLookback, deltaSequenceLookback, sweepLookback, divergenceLookback, imbFact, largeRatioThreshold, smallRatioThreshold, deltaThreshold1, deltaThreshold2, volumeThreshold, nearZeroaThreshold, exhaustionThreshold, standardFontSize, enableFootprint, enableSummaryGrid, enableSignalGrid, signalGridOffset, deltaLowPositiveColor, deltaMediumPositiveColor, deltaHighPositiveColor, deltaLowNegativeColor, deltaMediumNegativeColor, deltaHighNegativeColor, highVolumeColor, zeroDeltaColor, volSeqColor, stackedImbColor, revPOCColor, sweepColor, deltaSeqColor, divergenceColor, absorptionColor, exhaustionColor, vAGapColor, largeRatioColor, enableVolSeqSignal, volSeqDiamondOffset, enableStackedImbSignal, stackedImbDiamondOffset, enableReversalPOCSignal, reversalPOCDiamondOffset, enableSweepSignal, sweepDiamondOffset, enableDeltaSeqSignal, deltaSeqDiamondOffset, enableDivergenceSignal, divergenceDiamondOffset, enableDeltaFlipSignal, deltaFlipDiamondOffset, enableDeltaTrapSignal, deltaTrapDiamondOffset, enableAbsorptionSignal, absorptionDiamondOffset, enableExhaustionSignal, exhaustionDiamondOffset, enableVAGapSignal, vAGapDiamondOffset, enableLargeRatioSignal, largeRatioDiamondOffset);
		}

		public Indicators.FootprintOrderFlow FootprintOrderFlow(ISeries<double> input , int aggregationInterval, double valueAreaPer, int volumeSeqLookback, int stackedImbalanceLookback, int deltaSequenceLookback, int sweepLookback, int divergenceLookback, double imbFact, double largeRatioThreshold, double smallRatioThreshold, int deltaThreshold1, int deltaThreshold2, int volumeThreshold, int nearZeroaThreshold, int exhaustionThreshold, int standardFontSize, bool enableFootprint, bool enableSummaryGrid, bool enableSignalGrid, int signalGridOffset, System.Windows.Media.Brush deltaLowPositiveColor, System.Windows.Media.Brush deltaMediumPositiveColor, System.Windows.Media.Brush deltaHighPositiveColor, System.Windows.Media.Brush deltaLowNegativeColor, System.Windows.Media.Brush deltaMediumNegativeColor, System.Windows.Media.Brush deltaHighNegativeColor, System.Windows.Media.Brush highVolumeColor, System.Windows.Media.Brush zeroDeltaColor, System.Windows.Media.Brush volSeqColor, System.Windows.Media.Brush stackedImbColor, System.Windows.Media.Brush revPOCColor, System.Windows.Media.Brush sweepColor, System.Windows.Media.Brush deltaSeqColor, System.Windows.Media.Brush divergenceColor, System.Windows.Media.Brush absorptionColor, System.Windows.Media.Brush exhaustionColor, System.Windows.Media.Brush vAGapColor, System.Windows.Media.Brush largeRatioColor, bool enableVolSeqSignal, int volSeqDiamondOffset, bool enableStackedImbSignal, int stackedImbDiamondOffset, bool enableReversalPOCSignal, int reversalPOCDiamondOffset, bool enableSweepSignal, int sweepDiamondOffset, bool enableDeltaSeqSignal, int deltaSeqDiamondOffset, bool enableDivergenceSignal, int divergenceDiamondOffset, bool enableDeltaFlipSignal, int deltaFlipDiamondOffset, bool enableDeltaTrapSignal, int deltaTrapDiamondOffset, bool enableAbsorptionSignal, int absorptionDiamondOffset, bool enableExhaustionSignal, int exhaustionDiamondOffset, bool enableVAGapSignal, int vAGapDiamondOffset, bool enableLargeRatioSignal, int largeRatioDiamondOffset)
		{
			return indicator.FootprintOrderFlow(input, aggregationInterval, valueAreaPer, volumeSeqLookback, stackedImbalanceLookback, deltaSequenceLookback, sweepLookback, divergenceLookback, imbFact, largeRatioThreshold, smallRatioThreshold, deltaThreshold1, deltaThreshold2, volumeThreshold, nearZeroaThreshold, exhaustionThreshold, standardFontSize, enableFootprint, enableSummaryGrid, enableSignalGrid, signalGridOffset, deltaLowPositiveColor, deltaMediumPositiveColor, deltaHighPositiveColor, deltaLowNegativeColor, deltaMediumNegativeColor, deltaHighNegativeColor, highVolumeColor, zeroDeltaColor, volSeqColor, stackedImbColor, revPOCColor, sweepColor, deltaSeqColor, divergenceColor, absorptionColor, exhaustionColor, vAGapColor, largeRatioColor, enableVolSeqSignal, volSeqDiamondOffset, enableStackedImbSignal, stackedImbDiamondOffset, enableReversalPOCSignal, reversalPOCDiamondOffset, enableSweepSignal, sweepDiamondOffset, enableDeltaSeqSignal, deltaSeqDiamondOffset, enableDivergenceSignal, divergenceDiamondOffset, enableDeltaFlipSignal, deltaFlipDiamondOffset, enableDeltaTrapSignal, deltaTrapDiamondOffset, enableAbsorptionSignal, absorptionDiamondOffset, enableExhaustionSignal, exhaustionDiamondOffset, enableVAGapSignal, vAGapDiamondOffset, enableLargeRatioSignal, largeRatioDiamondOffset);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FootprintOrderFlow FootprintOrderFlow(int aggregationInterval, double valueAreaPer, int volumeSeqLookback, int stackedImbalanceLookback, int deltaSequenceLookback, int sweepLookback, int divergenceLookback, double imbFact, double largeRatioThreshold, double smallRatioThreshold, int deltaThreshold1, int deltaThreshold2, int volumeThreshold, int nearZeroaThreshold, int exhaustionThreshold, int standardFontSize, bool enableFootprint, bool enableSummaryGrid, bool enableSignalGrid, int signalGridOffset, System.Windows.Media.Brush deltaLowPositiveColor, System.Windows.Media.Brush deltaMediumPositiveColor, System.Windows.Media.Brush deltaHighPositiveColor, System.Windows.Media.Brush deltaLowNegativeColor, System.Windows.Media.Brush deltaMediumNegativeColor, System.Windows.Media.Brush deltaHighNegativeColor, System.Windows.Media.Brush highVolumeColor, System.Windows.Media.Brush zeroDeltaColor, System.Windows.Media.Brush volSeqColor, System.Windows.Media.Brush stackedImbColor, System.Windows.Media.Brush revPOCColor, System.Windows.Media.Brush sweepColor, System.Windows.Media.Brush deltaSeqColor, System.Windows.Media.Brush divergenceColor, System.Windows.Media.Brush absorptionColor, System.Windows.Media.Brush exhaustionColor, System.Windows.Media.Brush vAGapColor, System.Windows.Media.Brush largeRatioColor, bool enableVolSeqSignal, int volSeqDiamondOffset, bool enableStackedImbSignal, int stackedImbDiamondOffset, bool enableReversalPOCSignal, int reversalPOCDiamondOffset, bool enableSweepSignal, int sweepDiamondOffset, bool enableDeltaSeqSignal, int deltaSeqDiamondOffset, bool enableDivergenceSignal, int divergenceDiamondOffset, bool enableDeltaFlipSignal, int deltaFlipDiamondOffset, bool enableDeltaTrapSignal, int deltaTrapDiamondOffset, bool enableAbsorptionSignal, int absorptionDiamondOffset, bool enableExhaustionSignal, int exhaustionDiamondOffset, bool enableVAGapSignal, int vAGapDiamondOffset, bool enableLargeRatioSignal, int largeRatioDiamondOffset)
		{
			return indicator.FootprintOrderFlow(Input, aggregationInterval, valueAreaPer, volumeSeqLookback, stackedImbalanceLookback, deltaSequenceLookback, sweepLookback, divergenceLookback, imbFact, largeRatioThreshold, smallRatioThreshold, deltaThreshold1, deltaThreshold2, volumeThreshold, nearZeroaThreshold, exhaustionThreshold, standardFontSize, enableFootprint, enableSummaryGrid, enableSignalGrid, signalGridOffset, deltaLowPositiveColor, deltaMediumPositiveColor, deltaHighPositiveColor, deltaLowNegativeColor, deltaMediumNegativeColor, deltaHighNegativeColor, highVolumeColor, zeroDeltaColor, volSeqColor, stackedImbColor, revPOCColor, sweepColor, deltaSeqColor, divergenceColor, absorptionColor, exhaustionColor, vAGapColor, largeRatioColor, enableVolSeqSignal, volSeqDiamondOffset, enableStackedImbSignal, stackedImbDiamondOffset, enableReversalPOCSignal, reversalPOCDiamondOffset, enableSweepSignal, sweepDiamondOffset, enableDeltaSeqSignal, deltaSeqDiamondOffset, enableDivergenceSignal, divergenceDiamondOffset, enableDeltaFlipSignal, deltaFlipDiamondOffset, enableDeltaTrapSignal, deltaTrapDiamondOffset, enableAbsorptionSignal, absorptionDiamondOffset, enableExhaustionSignal, exhaustionDiamondOffset, enableVAGapSignal, vAGapDiamondOffset, enableLargeRatioSignal, largeRatioDiamondOffset);
		}

		public Indicators.FootprintOrderFlow FootprintOrderFlow(ISeries<double> input , int aggregationInterval, double valueAreaPer, int volumeSeqLookback, int stackedImbalanceLookback, int deltaSequenceLookback, int sweepLookback, int divergenceLookback, double imbFact, double largeRatioThreshold, double smallRatioThreshold, int deltaThreshold1, int deltaThreshold2, int volumeThreshold, int nearZeroaThreshold, int exhaustionThreshold, int standardFontSize, bool enableFootprint, bool enableSummaryGrid, bool enableSignalGrid, int signalGridOffset, System.Windows.Media.Brush deltaLowPositiveColor, System.Windows.Media.Brush deltaMediumPositiveColor, System.Windows.Media.Brush deltaHighPositiveColor, System.Windows.Media.Brush deltaLowNegativeColor, System.Windows.Media.Brush deltaMediumNegativeColor, System.Windows.Media.Brush deltaHighNegativeColor, System.Windows.Media.Brush highVolumeColor, System.Windows.Media.Brush zeroDeltaColor, System.Windows.Media.Brush volSeqColor, System.Windows.Media.Brush stackedImbColor, System.Windows.Media.Brush revPOCColor, System.Windows.Media.Brush sweepColor, System.Windows.Media.Brush deltaSeqColor, System.Windows.Media.Brush divergenceColor, System.Windows.Media.Brush absorptionColor, System.Windows.Media.Brush exhaustionColor, System.Windows.Media.Brush vAGapColor, System.Windows.Media.Brush largeRatioColor, bool enableVolSeqSignal, int volSeqDiamondOffset, bool enableStackedImbSignal, int stackedImbDiamondOffset, bool enableReversalPOCSignal, int reversalPOCDiamondOffset, bool enableSweepSignal, int sweepDiamondOffset, bool enableDeltaSeqSignal, int deltaSeqDiamondOffset, bool enableDivergenceSignal, int divergenceDiamondOffset, bool enableDeltaFlipSignal, int deltaFlipDiamondOffset, bool enableDeltaTrapSignal, int deltaTrapDiamondOffset, bool enableAbsorptionSignal, int absorptionDiamondOffset, bool enableExhaustionSignal, int exhaustionDiamondOffset, bool enableVAGapSignal, int vAGapDiamondOffset, bool enableLargeRatioSignal, int largeRatioDiamondOffset)
		{
			return indicator.FootprintOrderFlow(input, aggregationInterval, valueAreaPer, volumeSeqLookback, stackedImbalanceLookback, deltaSequenceLookback, sweepLookback, divergenceLookback, imbFact, largeRatioThreshold, smallRatioThreshold, deltaThreshold1, deltaThreshold2, volumeThreshold, nearZeroaThreshold, exhaustionThreshold, standardFontSize, enableFootprint, enableSummaryGrid, enableSignalGrid, signalGridOffset, deltaLowPositiveColor, deltaMediumPositiveColor, deltaHighPositiveColor, deltaLowNegativeColor, deltaMediumNegativeColor, deltaHighNegativeColor, highVolumeColor, zeroDeltaColor, volSeqColor, stackedImbColor, revPOCColor, sweepColor, deltaSeqColor, divergenceColor, absorptionColor, exhaustionColor, vAGapColor, largeRatioColor, enableVolSeqSignal, volSeqDiamondOffset, enableStackedImbSignal, stackedImbDiamondOffset, enableReversalPOCSignal, reversalPOCDiamondOffset, enableSweepSignal, sweepDiamondOffset, enableDeltaSeqSignal, deltaSeqDiamondOffset, enableDivergenceSignal, divergenceDiamondOffset, enableDeltaFlipSignal, deltaFlipDiamondOffset, enableDeltaTrapSignal, deltaTrapDiamondOffset, enableAbsorptionSignal, absorptionDiamondOffset, enableExhaustionSignal, exhaustionDiamondOffset, enableVAGapSignal, vAGapDiamondOffset, enableLargeRatioSignal, largeRatioDiamondOffset);
		}
	}
}

#endregion
