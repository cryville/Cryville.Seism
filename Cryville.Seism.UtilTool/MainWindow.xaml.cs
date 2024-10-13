using Cryville.Seism.NIED;
using Microsoft.Win32;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Path = System.Windows.Shapes.Path;

namespace Cryville.Seism.UtilTool {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	[SuppressMessage("Performance", "CA1812")]
	sealed partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
		}

		static readonly double[] _svaKeys = [5, 15, 50, 100];

		void OnOpenClicked(object sender, RoutedEventArgs e) {
			var dialog = new OpenFileDialog() {
				Filter = "Kyoshin WIN32 Data (*.kwin)|*.kwin",
			};
			if (!(dialog.ShowDialog() ?? false)) return;

			double width = MainCanvas.ActualWidth;
			double height = MainCanvas.ActualHeight;

			using var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read);
			var data = KyoshinWin32Data.FromStream(stream);
			if (data.StationInfo is not KyoshinStationInfo stationInfo) return;

			MainCanvas.Children.Clear();
			double min = 0;
			double max = 0;
			for (int index = 0; index < stationInfo.ComponentCount; index++) {
				var component = stationInfo.Components[index];
				if (component.Unit.Type != KyoshinComponentUnitType.MetresPerSecondSquared) {
					MessageBox.Show($"Unsupported sensor unit type: {component.Unit.Type}");
					return;
				}
				foreach (var second in data.Seconds) {
					var channel = second.Channels[index];
					foreach (int rawValue in channel.Data) {
						double value = component.ToPhysicalValue(rawValue) * component.Unit.PhysicalScale;
						if (value < min) min = value;
						if (value > max) max = value;
					}
				}
			}
			double range = max - min;

			double ComputeCanvasY(double v) => height * (1 - (v - min) / range);

			var states = new ComponentState[stationInfo.ComponentCount];
			var lpgmCalc = new RealtimeLPGMCalculator(stationInfo.SampleRate);
			var shindoDelayLine = new BleedingDelayLine<double>(stationInfo.SampleRate * 60, (int)(stationInfo.SampleRate * 0.3), 1e-5);
			var svaDelayLine = new BleedingDelayLine<double>(stationInfo.SampleRate * 60, 1, 0);
			var pathShindo = new PathFigure();
			var pathSva = new PathFigure();
			for (int index = 0; index < stationInfo.ComponentCount; index++) states[index] = new(1.0 / stationInfo.SampleRate);
			bool flag = false;
			int j = 0;
			foreach (var second in data.Seconds) {
				int len = second.Channels[0].Data.Count;
				for (int i = 0; i < len; i++, j++) {
					double ax = 0, ay = 0, az = 0;
					double mag = 0;
					double x = j / 10.0;
					for (int ci = 0; ci < second.Channels.Count; ci++) {
						var component = stationInfo.Components[ci];
						var channel = second.Channels[ci];
						var state = states[ci];
						double v = component.ToPhysicalValue(channel.Data[i]) * component.Unit.PhysicalScale;
						switch (ci) {
							case 0: ax = v; break;
							case 1: ay = v; break;
							case 2: az = v; break;
							default: break;
						}
						double vf = state.Filter.Update(v);
						mag += vf * vf;
						Point p = new(x, ComputeCanvasY(v));
						Point pf = new(x, ComputeCanvasY(vf));
						if (flag) {
							state.Path.Segments.Add(new LineSegment(pf, true));
						}
						else {
							state.Path.StartPoint = pf;
						}
					}

					if (j >= 70) shindoDelayLine.Add(Math.Sqrt(mag) * 100);
					double shindo = 2 * Math.Log10(shindoDelayLine.ComputedValue) + 0.94;
					Point ps = new(x, height * (1 - (shindo + 3) / 10));
					if (flag) {
						pathShindo.Segments.Add(new LineSegment(ps, true));
					}
					else {
						pathShindo.StartPoint = ps;
					}

					lpgmCalc.Update(ax * 100, ay * 100, az * 100);
					svaDelayLine.Add(lpgmCalc.MaxSVA);
					double maxSva = svaDelayLine.ComputedValue;
					Point pv = new(x, height * (1 - maxSva / 200));
					if (flag) {
						pathSva.Segments.Add(new LineSegment(pv, true));
					}
					else {
						pathSva.StartPoint = pv;
					}

					flag = true;
				}
			}
			for (int index = 0; index < stationInfo.ComponentCount; index++) {
				var state = states[index];

				var geof = new PathGeometry();
				geof.Figures.Add(state.Path);
				MainCanvas.Children.Add(new Path {
					Data = geof,
					Stroke = (index % 3) switch {
						0 => Brushes.Red,
						1 => Brushes.Green,
						_ => Brushes.Blue,
					},
					StrokeThickness = 1,
				});
			}

			double tick = 0;
			for (double baseTick = 100; tick == 0; baseTick /= 10) {
				tick = Math.Round(range / 16 / baseTick) * baseTick;
			}
			for (double v = Math.Ceiling(min / tick) * tick; v <= Math.Floor(max / tick) * tick; v += tick) {
				var geovl = new PathGeometry();
				double yvl = ComputeCanvasY(v);
				geovl.Figures.Add(new(new(0, yvl), [new LineSegment(new(width, yvl), true)], false));
				MainCanvas.Children.Add(new Path {
					Data = geovl,
					Stroke = Brushes.Black,
					StrokeThickness = 1,
				});
				TextBlock text = new() {
					Text = (v * 100).ToString("F1", CultureInfo.InvariantCulture) + "gal",
					Foreground = Brushes.Black,
				};
				Canvas.SetTop(text, yvl);
				MainCanvas.Children.Add(text);
			}

			for (double s = -2.5; s <= 7; s += 1) {
				var geosl = new PathGeometry();
				double ysl = height * (1 - (s + 3) / 10);
				geosl.Figures.Add(new(new(0, ysl), [new LineSegment(new(width, ysl), true)], false));
				MainCanvas.Children.Add(new Path {
					Data = geosl,
					Stroke = Brushes.Magenta,
					StrokeThickness = 1,
				});
				TextBlock text = new() {
					Text = s.ToString("F1", CultureInfo.InvariantCulture),
					HorizontalAlignment = HorizontalAlignment.Right,
					Foreground = Brushes.Magenta,
				};
				Canvas.SetTop(text, ysl);
				Canvas.SetRight(text, 0);
				MainCanvas.Children.Add(text);
			}

			foreach (double s in _svaKeys) {
				var geovl = new PathGeometry();
				double yvl = height * (1 - s / 200);
				geovl.Figures.Add(new(new(0, yvl), [new LineSegment(new(width, yvl), true)], false));
				MainCanvas.Children.Add(new Path {
					Data = geovl,
					Stroke = Brushes.DarkCyan,
					StrokeThickness = 1,
				});
				TextBlock text = new() {
					Text = s.ToString(CultureInfo.InvariantCulture) + "cm/s",
					HorizontalAlignment = HorizontalAlignment.Right,
					Foreground = Brushes.DarkCyan,
				};
				Canvas.SetTop(text, yvl);
				Canvas.SetRight(text, 32);
				MainCanvas.Children.Add(text);
			}

			var geos = new PathGeometry();
			geos.Figures.Add(pathShindo);
			MainCanvas.Children.Add(new Path {
				Data = geos,
				Stroke = Brushes.Magenta,
				StrokeThickness = 2,
			});

			var geov = new PathGeometry();
			geov.Figures.Add(pathSva);
			MainCanvas.Children.Add(new Path {
				Data = geov,
				Stroke = Brushes.DarkCyan,
				StrokeThickness = 2,
			});
		}
		sealed record ComponentState(RealtimeShindoFilter<double> Filter, PathFigure Path) {
			public ComponentState(double deltaT) : this(new(deltaT, DoubleOperators.Instance), new()) { }
		}
	}
}