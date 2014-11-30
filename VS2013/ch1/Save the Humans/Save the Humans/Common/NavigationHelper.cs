using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Save_the_Humans.Common
{
    /// <summary>
    /// NavigationHelper 페이지 간의 탐색을 지원합니다. NavigationManager는
    /// 앞뒤로 이동하는 데 사용되는 명령을 제공하고 앞뒤로 이동하는 데 사용되는 
    /// 하드웨어 탐색 요청 바로 가기와 Windows Phone의 하드웨어
    /// 뒤로 단추를 처리합니다. 또한 페이지 사이를 탐색할 때 프로세스 수명 관리
    /// 및 상태 관리를 처리하는 SuspensionManager가 통합되어 있습니다.
    /// </summary>
    /// <example>
    /// NavigationHelper를 사용하려면 다음 두 단계를 따르거나
    /// BasicPage 또는 기타 페이지 항목 템플릿(BlankPage가 아니어야 함)으로 시작하십시오.
    /// 
    /// 1) 다음과 같은 위치에서 NavigationHelper의 인스턴스를 만듭니다.
    ///   페이지 생성자 같은 위치에 만들고 LoadState 및 
    ///   SaveState 이벤트에 대한 콜백을 등록합니다.
    /// <code>
    ///   public MyPage()
    ///   {
    ///     this.InitializeComponent();
    ///     var navigationHelper = new NavigationHelper(this);
    ///     this.navigationHelper.LoadState += navigationHelper_LoadState;
    ///     this.navigationHelper.SaveState += navigationHelper_SaveState;
    ///   }
    ///   
    ///   private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
    ///   { }
    ///   private async void navigationHelper_SaveState(object sender, LoadStateEventArgs e)
    ///   { }
    /// </code>
    /// 
    /// 2) 페이지가 참여할 때마다 페이지를 NavigationHelper 호출에 등록합니다.
    ///   <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedTo"/> 
    ///   및 <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedFrom"/> 이벤트를 재정의하여 탐색에 참여할 때마다 페이지가 NavigationManager를 호출하도록 등록합니다.
    /// <code>
    ///   protected override void OnNavigatedTo(NavigationEventArgs e)
    ///   {
    ///     navigationHelper.OnNavigatedTo(e);
    ///   }
    ///   
    ///   protected override void OnNavigatedFrom(NavigationEventArgs e)
    ///   {
    ///     navigationHelper.OnNavigatedFrom(e);
    ///   }
    /// </code>
    /// </example>
    [Windows.Foundation.Metadata.WebHostHidden]
    public class NavigationHelper : DependencyObject
    {
        private Page Page { get; set; }
        private Frame Frame { get { return this.Page.Frame; } }

        /// <summary>
        /// <see cref="NavigationHelper"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="page">탐색에 사용되는 현재 페이지에 대한 참조입니다. 
        /// 이 참조는 프레임 조작을 가능하게 하고 키보드 
        /// 탐색 요청이 페이지가 전체 창을 차지했을 경우에만 발생하도록 합니다.</param>
        public NavigationHelper(Page page)
        {
            this.Page = page;

            // 이 페이지가 시각적 트리의 일부인 경우 두 가지를 변경합니다.:
            // 1) 응용 프로그램 뷰 상태를 페이지의 시각적 상태에 매핑
            // 2) Windows에서 앞이나 뒤로 이동하는 데 사용되는
            this.Page.Loaded += (sender, e) =>
            {
#if WINDOWS_PHONE_APP
                Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#else
                // 키보드 및 마우스 탐색은 전체 창 크기인 경우에만 적용됩니다.
                if (this.Page.ActualHeight == Window.Current.Bounds.Height &&
                    this.Page.ActualWidth == Window.Current.Bounds.Width)
                {
                    // 포커스가 필요하지 않도록 창을 직접 수신합니다.
                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
                        CoreDispatcher_AcceleratorKeyActivated;
                    Window.Current.CoreWindow.PointerPressed +=
                        this.CoreWindow_PointerPressed;
                }
#endif
            };

            // 페이지가 더 이상 표시되지 않는 경우 동일한 변경을 취소합니다.
            this.Page.Unloaded += (sender, e) =>
            {
#if WINDOWS_PHONE_APP
                Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
#else
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -=
                    CoreDispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.PointerPressed -=
                    this.CoreWindow_PointerPressed;
#endif
            };
        }

        #region 탐색 지원

        RelayCommand _goBackCommand;
        RelayCommand _goForwardCommand;

        /// <summary>
        /// 뒤로 탐색 기록의 가장 최근 항목으로 이동하기 위한
        /// 뒤로 단추의 명령 속성을 바인딩하는 데 사용되는 <see cref="RelayCommand"/>입니다. 프레임이
        /// 자체적으로 탐색 기록을 관리할 경우에 해당합니다.
        /// 
        /// <see cref="RelayCommand"/>는 가상 메서드 <see cref="GoBack"/>을
        /// 실행 작업으로 사용하고 CanExecute에 대해서는 <see cref="CanGoBack"/>을 사용하도록 설정됩니다.
        /// </summary>
        public RelayCommand GoBackCommand
        {
            get
            {
                if (_goBackCommand == null)
                {
                    _goBackCommand = new RelayCommand(
                        () => this.GoBack(),
                        () => this.CanGoBack());
                }
                return _goBackCommand;
            }
            set
            {
                _goBackCommand = value;
            }
        }
        /// <summary>
        /// 앞으로 탐색 기록의 가장 최근 항목으로 이동하는 데 사용되는 <see cref="RelayCommand"/>입니다. 
        /// 프레임이 자체적으로 탐색 기록을 관리할 경우에 해당합니다.
        /// 
        /// <see cref="RelayCommand"/>는 가상 메서드 <see cref="GoForward"/>를
        /// 실행 작업으로 사용하고 CanExecute에 대해서는 <see cref="CanGoForward"/>를 사용하도록 설정됩니다.
        /// </summary>
        public RelayCommand GoForwardCommand
        {
            get
            {
                if (_goForwardCommand == null)
                {
                    _goForwardCommand = new RelayCommand(
                        () => this.GoForward(),
                        () => this.CanGoForward());
                }
                return _goForwardCommand;
            }
        }

        /// <summary>
        /// <see cref="GoBackCommand"/> 속성이 사용하는 가상 메서드로,
        /// <see cref="Frame"/>의 뒤로 이동 가능 여부를 확인하기 위해 사용합니다.
        /// </summary>
        /// <returns>
        /// <see cref="Frame"/>에 적어도 하나의 항목이 있을 경우 
        /// True입니다(뒤로 탐색 기록 내).
        /// </returns>
        public virtual bool CanGoBack()
        {
            return this.Frame != null && this.Frame.CanGoBack;
        }
        /// <summary>
        /// <see cref="GoForwardCommand"/> 속성이 사용하는 가상 메서드로,
        /// <see cref="Frame"/>의 앞으로 이동 가능 여부를 확인하기 위해 사용합니다.
        /// </summary>
        /// <returns>
        /// <see cref="Frame"/>에 적어도 하나의 항목이 있을 경우 
        /// True입니다(앞으로 탐색 기록 내).
        /// </returns>
        public virtual bool CanGoForward()
        {
            return this.Frame != null && this.Frame.CanGoForward;
        }

        /// <summary>
        /// <see cref="GoBackCommand"/> 속성이 사용하는 가상 메서드로,
        /// <see cref="Windows.UI.Xaml.Controls.Frame.GoBack"/> 메서드를 호출하기 위해 사용합니다.
        /// </summary>
        public virtual void GoBack()
        {
            if (this.Frame != null && this.Frame.CanGoBack) this.Frame.GoBack();
        }
        /// <summary>
        /// <see cref="GoForwardCommand"/> 속성이 사용하는 가상 메서드로,
        /// <see cref="Windows.UI.Xaml.Controls.Frame.GoForward"/> 메서드를 호출하기 위해 사용합니다.
        /// </summary>
        public virtual void GoForward()
        {
            if (this.Frame != null && this.Frame.CanGoForward) this.Frame.GoForward();
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// 하드웨어 뒤로 단추를 누르면 호출됩니다. Windows Phone 전용입니다.
        /// </summary>
        /// <param name="sender">이벤트를 트리거한 인스턴스입니다.</param>
        /// <param name="e">이벤트의 발생 조건을 설명하는 이벤트 데이터입니다.</param>
        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            if (this.GoBackCommand.CanExecute(null))
            {
                e.Handled = true;
                this.GoBackCommand.Execute(null);
            }
        }
#else
        /// <summary>
        /// 이 페이지가 활성화되고 전체 창 크기로 표시된 경우 Alt 키 조합 등
        /// 시스템 키를 포함한 모든 키 입력에서 호출됩니다. 페이지에 포커스가 없으면
        /// 페이지 간 키보드 탐색을 검색하는 데 사용됩니다.
        /// </summary>
        /// <param name="sender">이벤트를 트리거한 인스턴스입니다.</param>
        /// <param name="e">이벤트의 발생 조건을 설명하는 이벤트 데이터입니다.</param>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender,
            AcceleratorKeyEventArgs e)
        {
            var virtualKey = e.VirtualKey;

            // 왼쪽 화살표, 오른쪽 화살표 또는 전용 이전 또는 다음 키를 눌렀을 때만 더
            // 조사합니다.
            if ((e.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                e.EventType == CoreAcceleratorKeyEventType.KeyDown) &&
                (virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right ||
                (int)virtualKey == 166 || (int)virtualKey == 167))
            {
                var coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey;

                if (((int)virtualKey == 166 && noModifiers) ||
                    (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    // 이전 키 또는 Alt+왼쪽 화살표를 누르면 뒤로 탐색
                    e.Handled = true;
                    this.GoBackCommand.Execute(null);
                }
                else if (((int)virtualKey == 167 && noModifiers) ||
                    (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    // 다음 키 또는 Alt+오른쪽 화살표를 누르면 앞으로 탐색
                    e.Handled = true;
                    this.GoForwardCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// 이 페이지가 활성화되고 전체 창 크기로 표시된 경우 모든 마우스 클릭, 터치 스크린 탭
        /// 또는 이와 같은 상호 작용에 대해 호출됩니다. 브라우저 스타일의 다음 및 이전 마우스 단추 클릭을
        /// 검색하여 페이지 간에 탐색하는 데 사용됩니다.
        /// </summary>
        /// <param name="sender">이벤트를 트리거한 인스턴스입니다.</param>
        /// <param name="e">이벤트의 발생 조건을 설명하는 이벤트 데이터입니다.</param>
        private void CoreWindow_PointerPressed(CoreWindow sender,
            PointerEventArgs e)
        {
            var properties = e.CurrentPoint.Properties;

            // 왼쪽 화살표, 오른쪽 화살표 및 가운데 화살표 단추와 함께 누르는 단추를 무시합니다.
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed ||
                properties.IsMiddleButtonPressed) return;

            // 뒤로 또는 앞으로를 누르면(동시 아님) 해당 방향으로 탐색합니다.
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                e.Handled = true;
                if (backPressed) this.GoBackCommand.Execute(null);
                if (forwardPressed) this.GoForwardCommand.Execute(null);
            }
        }
#endif

        #endregion

        #region 프로세스 수명 관리

        private String _pageKey;

        /// <summary>
        /// 이 이벤트를 현재 페이지에 등록하여
        /// 탐색하는 동안 전달된 콘텐츠와
        /// 이전 세션에서 페이지를 다시 만들 때 제공된 저장된 상태로 페이지를 채웁니다.
        /// </summary>
        public event LoadStateEventHandler LoadState;
        /// <summary>
        /// 이 이벤트를 현재 페이지에 등록하여
        /// 응용 프로그램이 일시 중단되거나
        /// 페이지가 탐색 캐시에서 삭제될 경우
        /// 현재 페이지와 연결된 상태를 유지합니다.
        /// </summary>
        public event SaveStateEventHandler SaveState;

        /// <summary>
        /// 이 페이지가 프레임에 표시되려고 할 때 호출됩니다. 
        /// 이 메서드는 <see cref="LoadState"/>를 호출합니다. 여기에 모든 페이지별
        /// 탐색 및 프로세스 수명 관리 로직을 배치해야 합니다.
        /// </summary>
        /// <param name="e">페이지에 도달한 방법을 설명하는 이벤트 데이터입니다. Parameter
        /// 속성은 표시할 그룹을 지정합니다.</param>
        public void OnNavigatedTo(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            this._pageKey = "Page-" + this.Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                // 탐색 스택에 새 페이지를 추가할 때 앞으로 탐색에 대한 기존 상태를
                // 지웁니다.
                var nextPageKey = this._pageKey;
                int nextPageIndex = this.Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                // 탐색 매개 변수를 새 페이지에 전달합니다.
                if (this.LoadState != null)
                {
                    this.LoadState(this, new LoadStateEventArgs(e.Parameter, null));
                }
            }
            else
            {
                // 일시 중단된 상태를 로드하고 캐시에서 삭제된 페이지를 다시 만드는 것과
                // 같은 전략을 사용하여 탐색 매개 변수와 유지된 페이지 상태를 페이지로
                // 전달합니다.
                if (this.LoadState != null)
                {
                    this.LoadState(this, new LoadStateEventArgs(e.Parameter, (Dictionary<String, Object>)frameState[this._pageKey]));
                }
            }
        }

        /// <summary>
        /// 이 페이지가 프레임에 더 이상 표시되지 않을 때 호출됩니다.
        /// 이 메서드는 <see cref="SaveState"/>를 호출합니다. 여기에 모든 페이지별
        /// 탐색 및 프로세스 수명 관리 로직을 배치해야 합니다.
        /// </summary>
        /// <param name="e">페이지에 도달한 방법을 설명하는 이벤트 데이터입니다. Parameter
        /// 속성은 표시할 그룹을 지정합니다.</param>
        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            var pageState = new Dictionary<String, Object>();
            if (this.SaveState != null)
            {
                this.SaveState(this, new SaveStateEventArgs(pageState));
            }
            frameState[_pageKey] = pageState;
        }

        #endregion
    }

    /// <summary>
    /// <see cref="NavigationHelper.LoadState"/> 이벤트를 처리할 메서드를 나타냅니다.
    /// </summary>
    public delegate void LoadStateEventHandler(object sender, LoadStateEventArgs e);
    /// <summary>
    /// <see cref="NavigationHelper.SaveState"/> 이벤트를 처리할 메서드를 나타냅니다.
    /// </summary>
    public delegate void SaveStateEventHandler(object sender, SaveStateEventArgs e);

    /// <summary>
    ///페이지가 상태 로드를 시도할 때 필요한 이벤트 데이터를 유지하는 데 사용되는 클래스입니다.
    /// </summary>
    public class LoadStateEventArgs : EventArgs
    {
        /// <summary>
        /// 이 페이지가 처음 요청되었을 때 <see cref="Frame.Navigate(Type, Object)"/>에 
        /// 전달된 매개 변수 값입니다.
        /// </summary>
        public Object NavigationParameter { get; private set; }
        /// <summary>
        /// 이전 세션 동안 이 페이지에 유지된
        /// 사전 상태입니다. 페이지를 처음 방문할 때는 이 값이 null입니다.
        /// </summary>
        public Dictionary<string, Object> PageState { get; private set; }

        /// <summary>
        /// <see cref="LoadStateEventArgs"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="navigationParameter">
        /// 이 페이지가 처음 요청되었을 때 <see cref="Frame.Navigate(Type, Object)"/>에 
        /// 전달된 매개 변수 값입니다.
        /// </param>
        /// <param name="pageState">
        /// 이전 세션 동안 이 페이지에 유지된
        /// 사전 상태입니다. 페이지를 처음 방문할 때는 이 값이 null입니다.
        /// </param>
        public LoadStateEventArgs(Object navigationParameter, Dictionary<string, Object> pageState)
            : base()
        {
            this.NavigationParameter = navigationParameter;
            this.PageState = pageState;
        }
    }
    /// <summary>
    ///페이지가 상태 저장을 시도할 때 필요한 이벤트 데이터를 유지하는 데 사용되는 클래스입니다.
    /// </summary>
    public class SaveStateEventArgs : EventArgs
    {
        /// <summary>
        /// serializable 상태로 채워질 빈 사전입니다.
        /// </summary>
        public Dictionary<string, Object> PageState { get; private set; }

        /// <summary>
        /// <see cref="SaveStateEventArgs"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="pageState">serializable 상태로 채워질 빈 사전입니다.</param>
        public SaveStateEventArgs(Dictionary<string, Object> pageState)
            : base()
        {
            this.PageState = pageState;
        }
    }
}
