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

// 빈 페이지 항목 템플릿에 대한 설명은 http://go.microsoft.com/fwlink/?LinkId=391641에 나와 있습니다.

namespace BeeAttack
{
    /// <summary>
    /// 자체에서 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// 이 페이지가 프레임에 표시되려고 할 때 호출됩니다.
        /// </summary>
        /// <param name="e">페이지에 도달한 방법을 설명하는 이벤트 데이터입니다.
        /// 이 매개 변수는 일반적으로 페이지를 구성하는 데 사용됩니다.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: 여기에 표시할 페이지를 준비합니다.

            // TODO: 응용 프로그램에 여러 페이지가 포함된 경우
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed 이벤트에 등록하여
            // 하드웨어 뒤로 단추를 처리하는지 확인하십시오.
            // 일부 템플릿에서 제공하는 NavigationHelper를 사용할 경우
            // 이 이벤트가 자동으로 처리됩니다.
        }
    }
}
