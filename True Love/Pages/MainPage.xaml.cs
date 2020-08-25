﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace True_Love.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // 滚动条位置变量
        public double scrlocation = 0;
        // 导航栏当前显示状态（这个是为了减少不必要的开销，因为我做的是动画隐藏显示效果如果不用一个变量来记录当前导航栏状态的会重复执行隐藏或显示）
        bool isshowbmbar = true;
        double x = 0;

        public MainPage()
        {
            this.InitializeComponent();
            if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                AppTitleBar.Visibility = Visibility.Visible;
                Window.Current.SetTitleBar(AppTitleBar);
            }

            this.ManipulationMode = ManipulationModes.TranslateX; // 设置这个页面的手势模式为横向滑动
            this.ManipulationCompleted += The_ManipulationCompleted; // 订阅手势滑动结束后的事件
            this.ManipulationDelta += The_ManipulationDelta; // 订阅手势滑动事件

            #region 兼容低版本号系统
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar") // = WP
)
            {
                var myBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Colors.Black);
                var myBrush2 = new Windows.UI.Xaml.Media.SolidColorBrush(Colors.Black);
                myBrush2.Opacity = 0.7;

                CommandBar.Background = myBrush2;
                BackgroundOfBar.Background = myBrush;

                BackgroundOfBar.Height = 45;              
            }
            else if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4)) // >= OS15063
            {
                // 背景
                var myBrush = new AcrylicBrush
                {
                    BackgroundSource = AcrylicBackgroundSource.Backdrop,
                    TintColor = Color.FromArgb(255, 0, 0, 1),
                    FallbackColor = Colors.Black,
                    TintOpacity = 0.7,
                };
                CommandBar.Background = myBrush;
                BackgroundOfBar.Background = myBrush;

                BackgroundOfBar.Height = 40;

                BackTopButton.Style = (Style)this.Resources["AppBarButtonRevealStyle"];
                RefreshButton.Style = (Style)this.Resources["AppBarButtonRevealStyle"];

                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))// > OS15063
                {
                    // 键盘快捷键             
                    var FrametoLove = new KeyboardAccelerator { Key = VirtualKey.F1 };
                    LovePage.KeyboardAccelerators.Add(FrametoLove);

                    var FrametoMemory = new KeyboardAccelerator { Key = VirtualKey.F2 };
                    HomePage.KeyboardAccelerators.Add(FrametoMemory);

                    var BacktoTop = new KeyboardAccelerator { Key = VirtualKey.F6 };
                    BackTopButton.KeyboardAccelerators.Add(BacktoTop);

                    var Refresh = new KeyboardAccelerator { Key = VirtualKey.F5 };
                    RefreshButton.KeyboardAccelerators.Add(Refresh);
                }             
            }
            #endregion
        }

        #region NavigationView
        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e) => throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

        // List of ValueTuple holding the Navigation Tag and the relative Navigation Page
        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("home", typeof(HomePage)),
            ("comment", typeof(CommentsPage)),
            ("image", typeof(ImagesPage)),
        };

        private readonly List<(string Tag, Type Page)> _pagesforWP = new List<(string Tag, Type Page)>
        {
            ("home", typeof(HomePageforWP)),
            ("comment", typeof(CommentsPage)),
            ("image", typeof(ImagesPage)),
        };

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;
            // Add handler for ContentFrame navigation.
            ContentFrame.Navigated += On_Navigated;

            // NavView doesn't load any page by default, so load home page.
            NavView.SelectedItem = NavView.MenuItems[0];
            // If navigation occurs on SelectionChanged, this isn't needed.
            // Because we use ItemInvoked to navigate, we need to call Navigate
            // here to load the home page.
            NavView_Navigate("home", new EntranceNavigationTransitionInfo());

            // Add keyboard accelerators for backwards navigation.
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                var goBack = new KeyboardAccelerator { Key = VirtualKey.Escape };
                goBack.Invoked += BackInvoked;
                this.KeyboardAccelerators.Add(goBack);
            }
        }

        /// <summary>
        /// 手势滑动中 https://blog.csdn.net/github_36704374/article/details/59580697
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void The_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            x += e.Delta.Translation.X; // 将滑动的值赋给 x
        }

        /// <summary>
        /// 手势滑动结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void The_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (x > 100) // 判断滑动的距离
                NavView.IsPaneOpen = true; // 打开汉堡菜单            
            if (x < -100)
                NavView.IsPaneOpen = false; // 关闭汉堡菜单
            x = 0;  // 清零 x，不然x会累加
        }

        private void NavView_ItemInvoked(muxc.NavigationView sender,
                                         muxc.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        // NavView_SelectionChanged is not used in this example, but is shown for completeness.
        // You will typically handle either ItemInvoked or SelectionChanged to perform navigation,
        // but not both.
        private void NavView_SelectionChanged(muxc.NavigationView sender,
                                              muxc.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
            {
                NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
            }
            else if (args.SelectedItemContainer != null)
            {
                var navItemTag = args.SelectedItemContainer.Tag.ToString();
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo)
        {
            Type _page = null;
            if (navItemTag == "settings")
            {
                _page = typeof(SettingsPage);
            }
            else if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var item = _pagesforWP.FirstOrDefault(p => p.Tag.Equals(navItemTag));
                _page = item.Page;
            }
            else
            {
                var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
                _page = item.Page;
            }

            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(_page is null) && !Equals(preNavPageType, _page))
            {
                ContentFrame.Navigate(_page, null, transitionInfo);
            }
        }

        private void NavView_BackRequested(muxc.NavigationView sender, muxc.NavigationViewBackRequestedEventArgs args) => On_BackRequested();

        private void BackInvoked(KeyboardAccelerator sender,
                                 KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;     
        }
        private void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!ContentFrame.CanGoBack) return;
            ContentFrame.GoBack();
            e.Handled = true;
        }

        private bool On_BackRequested()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (NavView.IsPaneOpen &&
                (NavView.DisplayMode == muxc.NavigationViewDisplayMode.Compact ||
                 NavView.DisplayMode == muxc.NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavView.SelectedItem = (muxc.NavigationViewItem)NavView.SettingsItem;
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) NavView.Header = "SETTINGS";

                CommandBarChanged("settings");
            }
            else if (ContentFrame.SourcePageType != null && ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var item = _pagesforWP.FirstOrDefault(p => p.Page == e.SourcePageType);

                NavView.SelectedItem = NavView.MenuItems.OfType<muxc.NavigationViewItem>().First(n => n.Tag.Equals(item.Tag));

                NavView.Header = ((muxc.NavigationViewItem)NavView.SelectedItem)?.Content?.ToString().ToUpper();

                CommandBarChanged(item.Tag.ToString());
            }
            else if (ContentFrame.SourcePageType != null)
            {
                var item = _pages.FirstOrDefault(p => p.Page == e.SourcePageType);

                NavView.SelectedItem = NavView.MenuItems.OfType<muxc.NavigationViewItem>().First(n => n.Tag.Equals(item.Tag));

                CommandBarChanged(item.Tag.ToString());
            }
        }
        #endregion

        #region 底部工具栏
        /// <summary>
        /// 加载相关代码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBar_Loaded(object sender, RoutedEventArgs e)
        {
            CommandBarTransition();
        }

        /// <summary>
        /// 鼠标右击工具栏活动。
        /// </summary>
        private void CommandBar_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            CommandBar.IsOpen = !CommandBar.IsOpen == true;
        }

        /// <summary>
        /// 工具栏跟随窗口移动动画。
        /// </summary>
        private void CommandBarTransition()
        {
            EdgeUIThemeTransition inStoryoard = new EdgeUIThemeTransition { Edge = EdgeTransitionLocation.Bottom };
            var tc = new TransitionCollection { inStoryoard };
            bar.Transitions = tc;
        }

        /// <summary>
        /// 检查工具栏相关的按钮可用状态。
        /// 下滑隐藏命令栏 https://www.cnblogs.com/lonelyxmas/p/9919869.html
        /// </summary>
        private void Sv_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (sv.VerticalOffset != scrlocation)
            {
                //滚动条当前位置大于存储的变量值时代表往下滑，隐藏底部栏
                if (sv.VerticalOffset > scrlocation)
                {                 
                    //隐藏
                    if (isshowbmbar)
                    {
                        //通过动画来隐藏
                        //bar.Translation = new Vector3(0, 40, 0);
                        Close.Begin();

                        isshowbmbar = false;
                    }
                }
                //反之展开
                else
                {
                    //显示
                    if (isshowbmbar == false)
                    {
                        //通过动画来隐藏
                        //bar.Translation = new Vector3(0, 0, 0);
                        Open.Begin();

                        isshowbmbar = true;
                    }
                }

                //当滚动条高度大于1时，返回顶部按钮维持使用状态
                if (sv.VerticalOffset > 1)
                {
                    BackTopButton.IsEnabled = true;
                }
                //反之停用此按钮
                else if (sv.VerticalOffset < sv.ViewportHeight)
                {
                    BackTopButton.IsEnabled = false;
                }
            }  
            scrlocation = sv.VerticalOffset;  
        }

        /// <summary>
        /// 返回滑动条参数。
        /// </summary>
        /// <param name="horizontalOffset">X：横度。</param>
        /// <param name="verticalOffset">Y：高度。</param>
        /// <param name="zoomFactor">Z：斜度。</param>
        public void ChangeView(double? horizontalOffset,
                               double? verticalOffset,
                               float? zoomFactor)
        {

        }

        /// <summary>
        /// 返回顶部按钮。
        /// </summary>
        private void BackToTop_Click(object sender, RoutedEventArgs e)
        {
            sv.ChangeView(null, 0, null);
        }

        /// <summary>
        /// 刷新按钮。
        /// </summary>
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            CommentsPage.Current.webview.Refresh();
            
        }

        /// <summary>
        /// 检查刷新按钮可用状态。
        /// </summary>
        /// <param name="tag">NavViewItem.Tag</param>
        private void CommandBarChanged(string tag)
        {
            switch (tag)
            {
                case "home":
                    CommandBar.Visibility = Visibility.Collapsed;
                    break;

                case "comment":
                    CommandBar.Visibility = Visibility.Visible;
                    RefreshButton.IsEnabled = true;
                    break;           

                default:
                    CommandBar.Visibility = Visibility.Visible;
                    RefreshButton.IsEnabled = false;
                    break;
            }
        }
        #endregion
    }
}