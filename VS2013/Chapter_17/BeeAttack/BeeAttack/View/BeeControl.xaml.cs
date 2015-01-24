using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 사용자 정의 컨트롤 항목 템플릿에 대한 설명은 http://go.microsoft.com/fwlink/?LinkId=234236에 나와 있습니다.

namespace BeeAttack.View
{
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Media.Imaging;

    public sealed partial class BeeControl : UserControl
    {
        public readonly Storyboard FallingStoryboard;

        public BeeControl()
        {
            this.InitializeComponent();
            StartFlapping(TimeSpan.FromMilliseconds(30));
        }

        public BeeControl(double X, double fromY, double toY, EventHandler<object> completed)
            : this()
        {
            FallingStoryboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation();

            Storyboard.SetTarget(animation, this);
            Canvas.SetLeft(this, X);
            Storyboard.SetTargetProperty(animation, "(Canvas.Top)");
            
            animation.From = fromY;
            animation.To = toY;
            animation.Duration = TimeSpan.FromSeconds(1);

            if (completed != null) FallingStoryboard.Completed += completed;

            FallingStoryboard.Children.Add(animation);
            FallingStoryboard.Begin();
        }

        public void StartFlapping(TimeSpan interval)
        {
            List<string> imageNames = new List<string>() {
            "Bee animation 1.png", "Bee animation 2.png", "Bee animation 3.png", "Bee animation 4.png"
        };

            Storyboard storyboard = new Storyboard();
            ObjectAnimationUsingKeyFrames animation = new ObjectAnimationUsingKeyFrames();
            Storyboard.SetTarget(animation, image);
            Storyboard.SetTargetProperty(animation, "Source");
            
            TimeSpan currentInterval = TimeSpan.FromMilliseconds(0);
            foreach (string imageName in imageNames)
            {
                ObjectKeyFrame keyFrame = new DiscreteObjectKeyFrame();
                keyFrame.Value = CreateImageFromAssets(imageName);
                keyFrame.KeyTime = currentInterval;
                animation.KeyFrames.Add(keyFrame);
                currentInterval = currentInterval.Add(interval);
            }

            storyboard.RepeatBehavior = RepeatBehavior.Forever;
            storyboard.AutoReverse = true;
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private static BitmapImage CreateImageFromAssets(string imageFilename)
        {
            return new BitmapImage(new Uri("ms-appx:///Assets/" + imageFilename));
        }
    }
}
