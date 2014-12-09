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
    using ViewModel;
    
    public partial class BeeAttackGameControl : UserControl
    {
        private readonly ViewModel.BeeAttackViewModel _viewModel = new ViewModel.BeeAttackViewModel();

        public BeeAttackGameControl()
        {
            InitializeComponent();

            DataContext = _viewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StartGame(flower.RenderSize, hive.RenderSize, playArea.RenderSize);
            
        }

        private void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Test");
            _viewModel.ManipulationDelta(e.Delta.Translation.X);
        }
    }
}
