/* アナログ時計
 * 参照：http://var.blog.jp/archives/76031956.html
 *       http://ch.nicovideo.jp/yww/blomaga/ar706628
 *       http://www.ne.jp/asahi/aaa/tach1394/delphi/index.htm
 *       https://blog.okazuki.jp/entry/20130102/1357124042
 *       http://kuxumarin.hatenablog.com/entry/2017/05/20/WPF_%E3%81%AE_UserControl_%E3%81%A7_%E3%82%B3%E3%83%B3%E3%83%88%E3%83%AD%E3%83%BC%E3%83%AB%E3%81%AE%E9%AB%98%E3%81%95%E3%81%A8%E5%B9%85%E3%82%92%E5%8F%96%E5%BE%97%E3%81%99%E3%82%8B
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace analog_clock
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 文字盤の中心（x座標もy座標も同じ）
        /// </summary>
        private double center;
        /// <summary>
        /// 文字盤の枠の半径
        /// </summary>
        private double radius;
        /// <summary>
        /// 同期用のタイマー
        /// </summary>
        private DispatcherTimer timer;
        /// <summary>
        /// 時刻の同期
        /// </summary>
        private Action ajustTime;

        public MainWindow()
        {
            InitializeComponent();

            // 時計自体の設定
            center = frame.Width / 2;
            radius = center;
            ajustTime = initHands();
            initClockFace();
            ajustTime();

            // 同期処理の設定
            timer = initTimer();
            timer.Start();
        }

        /// <summary>
        /// 文字盤の初期化
        /// </summary>
        private void initClockFace()
        {
            // 文字盤の目盛りを描く
            for (int minute = 0; minute < 60; minute++)
            {
                if (minute % 5 == 0)
                    drawADial(minute * 6, 4, 2);
                else
                    drawADial(minute * 6, 2, 1);
            }

            // 文字盤の数字を書く
            for (int hour = 1; hour <= 12; hour++)
                writeANumber(hour);
        }

        /// <summary>
        /// 目盛りを描く
        /// </summary>
        /// <param name="angle">0時を0度としたときの角度</param>
        /// <param name="length">目盛りの長さ</param>
        /// <param name="thickness">目盛りの太さ</param>
        private void drawADial(int angle, double length, double thickness)
        {
            var line = new Line();
            line.Stroke = Brushes.Black;
            line.StrokeThickness = thickness;

            // RotateTransform を使う方法
            /*double margin = radius * 5 / 100;   // 縁から中心に向かっての余白
            line.X1 = 0;
            line.Y1 = margin;
            line.X2 = 0;
            line.Y2 = margin + length;
            line.HorizontalAlignment = HorizontalAlignment.Center;
            var rotation = new RotateTransform();
            rotation.Angle = angle;
            rotation.CenterY = center;
            line.RenderTransform = rotation;*/

            // 三角関数を使う方法
            double position = radius * 95 / 100;   // 中心から目盛りを描く点までの距離
            line.X1 = center + position * Math.Sin(angle * Math.PI / 180.0);
            line.Y1 = center - position * Math.Cos(angle * Math.PI / 180.0);
            line.X2 = center + (position - length) * Math.Sin(angle * Math.PI / 180.0);
            line.Y2 = center - (position - length) * Math.Cos(angle * Math.PI / 180.0);

            dials.Children.Add(line);
        }

        /// <summary>
        /// 目盛りの数字を書く
        /// </summary>
        /// <param name="hour">数字</param>
        private void writeANumber(int hour)
        {
            var textBlock = new TextBlock();
            textBlock.FontSize = 11;
            textBlock.Text = hour.ToString();
            textBlock.Opacity = 0.7;
            double position = radius * 75 / 100;      // 中心から目盛りを描く点までの距離
            Dispatcher.BeginInvoke(new Action(() =>   // 生成された textBlock の大きさを取得するための処理
            {
                double textWidth = textBlock.ActualWidth / 2;
                double textHeight = textBlock.ActualHeight / 2;
                Canvas.SetLeft(textBlock, center + position * Math.Sin(hour * Math.PI / 6.0) - textWidth);
                Canvas.SetTop(textBlock, center - position * Math.Cos(hour * Math.PI / 6.0) - textHeight);
            }),
            DispatcherPriority.Loaded);
            dials.Children.Add(textBlock);
        }

        /// <summary>
        /// 短針・長針・秒針の動作に関する設定
        /// </summary>
        /// <returns>針の位置を現在時刻に同期する関数</returns>
        private Action initHands()
        {
            bool isCalled = true;
            return () =>
            {
                var dt = DateTime.Now;
                var hourHandStoryboard = this.hourHand.Resources["hourHandStoryboard"] as Storyboard;
                var minuteHandStoryboard = this.minuteHand.Resources["minuteHandStoryboard"] as Storyboard;
                var secondHandStoryboard = this.secondHand.Resources["secondHandStoryboard"] as Storyboard;

                // 秒針の animation を初期化（最初に1回だけ実行）
                //new Action(initSecondHand(secondHandStoryboard))();
                if (isCalled)
                {
                    var secondHandAnimation = new DoubleAnimationUsingKeyFrames();
                    Storyboard.SetTargetName(secondHandAnimation, "secondHand");
                    Storyboard.SetTargetProperty(secondHandAnimation, new PropertyPath("(Rectangle.RenderTransform).(RotateTransform.Angle)"));
                    secondHandAnimation.Duration = TimeSpan.FromMinutes(1);
                    secondHandAnimation.RepeatBehavior = RepeatBehavior.Forever;
                    for (int second = 0; second < 60; second++)
                        secondHandAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(second * 6, TimeSpan.FromSeconds(second)));
                    secondHandStoryboard.Children.Add(secondHandAnimation);
                    isCalled = false;
                }

                // 短針・長針・秒針の位置を現在時刻に合わせる
                hourHandStoryboard.Begin();
                hourHandStoryboard.SeekAlignedToLastTick(dt.TimeOfDay, TimeSeekOrigin.BeginTime);
                minuteHandStoryboard.Begin();
                minuteHandStoryboard.SeekAlignedToLastTick(dt.TimeOfDay, TimeSeekOrigin.BeginTime);
                secondHandStoryboard.Begin();
                secondHandStoryboard.SeekAlignedToLastTick(dt.TimeOfDay, TimeSeekOrigin.BeginTime);
            };
        }

        /*private Action initSecondHand(Storyboard storyboard)
        {
            bool isCalled = true;
            return () =>
            {
                if (isCalled)
                {
                    var secondHandAnimation = new DoubleAnimationUsingKeyFrames();
                    Storyboard.SetTargetName(secondHandAnimation, "secondHand");
                    Storyboard.SetTargetProperty(secondHandAnimation, new PropertyPath("(Rectangle.RenderTransform).(RotateTransform.Angle)"));
                    secondHandAnimation.Duration = TimeSpan.FromMinutes(1);
                    secondHandAnimation.RepeatBehavior = RepeatBehavior.Forever;
                    for (int second = 0; second < 60; second++)
                        secondHandAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(second * 6, TimeSpan.FromSeconds(second)));
                    storyboard.Children.Add(secondHandAnimation);
                    isCalled = false;
                }
            };
        }*/

        /// <summary>
        /// タイマーの生成処理
        /// </summary>
        /// <returns>生成したタイマー</returns>
        private DispatcherTimer initTimer()
        {
            var t = new DispatcherTimer(DispatcherPriority.SystemIdle);
            t.Interval = TimeSpan.FromMinutes(10);
            t.Tick += (sender, e) =>
            {
                ajustTime();
            };
            return t;
        }

        private void Quit_Clicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }


}
