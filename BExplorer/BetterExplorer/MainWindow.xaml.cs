﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fluent;
using System.Reflection;
using System.IO;
using System.Drawing;
using System.Threading;
using Clipboards = System.Windows.Forms.Clipboard;
using MenuItem = Fluent.MenuItem;
using System.Windows.Shell;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Controls;
using Microsoft.WindowsAPICodePack.Controls.WindowsForms;
using System.Windows.Media.Animation;
using System.Collections.Specialized;
using WindowsHelper;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Threading;
using SevenZip;
using ContextMenu = Fluent.ContextMenu;
using System.ComponentModel.Composition;
using System.Globalization;
using NAppUpdate.Framework;
using NAppUpdate.Framework.Tasks;
using NAppUpdate.Framework.Conditions;
using AndreasJohansson.Win32.Shell;
using BetterExplorerControls;


namespace BetterExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        #region Variables and Constants
        ExplorerBrowser Explorer = new ExplorerBrowser();
        ClipboardMonitor cbm = new ClipboardMonitor();
        ContextMenu cmHistory = new ContextMenu();
        private int CurrentTabIndex;
        private int LastTabIndex = -1;
        private int BeforeLastTabIndex = -1;
        string StartUpLocation = KnownFolders.Libraries.ParsingName;
        bool IsHFlyoutEnabled;
        bool IsPreviewPaneEnabled;
        bool IsInfoPaneEnabled;
        bool IsNavigationPaneEnabled;
        bool isCheckModeEnabled;
        bool IsExtendedFileOpEnabled;
        public bool IsrestoreTabs;
        public bool isOnLoad;
        JumpList AppJL = new JumpList();
        public bool IsCalledFromLoading;
        public bool IsCalledFromViewEnum;
        bool ReadyToChangeLanguage;
        System.Windows.Forms.Timer FocusTimer = new System.Windows.Forms.Timer();
        IntPtr Handle;
        string EditComm = "";
        List<string> Archives = new List<string>(new string[] { ".rar", ".zip", ".7z", ".tar", ".gz", ".xz", ".bz2" });
        List<string> Images = new List<string>(new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".wmf" });
        MenuItem misa;
        MenuItem misd;
        MenuItem misag;
        MenuItem misdg;
        MenuItem misng;
        public static UpdateManager Updater;
        bool inLibrary = false;
        bool inDrive = false;
        //string LastItemSelected = "";
        string SelectedArchive = "";
        //bool KeepFocusOnExplorer = false;
        bool KeepBackstageOpen = false;
        IProgressDialog pd;
        private string ICON_DLLPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
        bool IsAfterFolderCreate = false;
        bool isGoingBackOrForward;
        bool asFolder = false;
        bool asImage = false;
        bool asArchive = false;
        bool asDrive = false;
        bool asApplication = false;
        bool asLibrary = false;
        bool canlogactions = false;
        string sessionid = DateTime.UtcNow.ToFileTimeUtc().ToString();
        string logdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BExplorer_ActionLog\\";

        #endregion

        #region Properties
        /// <summary>
        /// Gets Previouse Window State
        /// </summary>
        public WindowState PreviouseWindowState { get; private set; }

        /// <summary>
        /// Gets the New context menu
        /// </summary>
        /// <returns>The list of the nwe context menu items</returns>
        private List<string> GetNewContextMenu()
        {
            List<string> TheList = new List<string>();
            RegistryKey reg = Registry.CurrentUser;
            RegistryKey classesrk = reg.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew");
            string[] classes = (string[])classesrk.GetValue("Classes");

            foreach (string item in classes)
            {
                RegistryKey regext = Registry.ClassesRoot;
                RegistryKey extrk = regext.OpenSubKey(item + @"\ShellNew");
                if (extrk != null)
                {
                    string FileName = (string)extrk.GetValue("FileName");
                    TheList.Add(FileName);
                }


            }
            classesrk.Close();
            reg.Close();
            return TheList;
        }

        #endregion

        #region Events

        private void backstage_IsOpenChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ExplorerBrowser.IsBool = backstage.IsOpen;
            if (KeepBackstageOpen == true)
            {
                KeepBackstageOpen = false;
                //backstage.IsOpen = true;
            }
        }

        private void RibbonWindow_MouseUp_1(object sender, MouseButtonEventArgs e)
        {

            //IsAfterRename = true;
        }

        private void RibbonWindow_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsAfterRename = true;
        }

        private void TheRibbon_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (TheRibbon.IsMinimized)
            {
                System.Windows.Point p =
                ShellVView.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                this.GlassBorderThickness = new Thickness(8, p.Y - 2, 8, 8);
                try
                {
                    //this.SetBlur(false);
                }
                catch (Exception)
                {


                }
            }
            else
            {
                System.Windows.Point p = backstage.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                this.GlassBorderThickness = new Thickness(8, p.Y + backstage.ActualHeight, 8, 8);
                try
                {
                    //this.SetBlur(true);
                }
                catch (Exception)
                {


                }
            }

        }

        private void TheRibbon_CustomizeQuickAccessToolbar(object sender, EventArgs e)
        {

        }

        private int GetIntegerFromBoolean(bool value)
        {
            if (value == true)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private void RibbonWindow_Initialized(object sender, EventArgs e)
        {

            AppCommands.RoutedNewTab.InputGestures.Add(new KeyGesture(Key.T, ModifierKeys.Control));
            AppCommands.RoutedEnterInBreadCrumbCombo.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Alt));
            AppCommands.RoutedChangeTab.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control));
            AppCommands.RoutedCloseTab.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control));
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", false);

            if (rks == null)
            {
                return;
            }

            this.Width = Convert.ToDouble(rks.GetValue(@"LastWindowWidth","640"));
            this.Height = Convert.ToDouble(rks.GetValue(@"LastWindowHeight","480"));

            System.Drawing.Point Location = new System.Drawing.Point();
            try
            {
                Location = new System.Drawing.Point(
                    Convert.ToInt32(rks.GetValue(@"LastWindowPosLeft","0")),
                    Convert.ToInt32(rks.GetValue(@"LastWindowPosTop","0")));
            }
            catch
            {

            }
            if (Location != null)
            {
                this.Left = Location.X;
                this.Top = Location.Y;
            }

            switch (Convert.ToInt32(rks.GetValue(@"LastWindowState")))
            {
                case 2:
                    this.WindowState = System.Windows.WindowState.Maximized;
                    break;
                case 1:
                    this.WindowState = System.Windows.WindowState.Minimized;
                    break;
                case 0:
                    this.WindowState = System.Windows.WindowState.Normal;
                    break;
                default:
                    this.WindowState = System.Windows.WindowState.Maximized;
                    break;
            }

            TheRibbon.IsMinimized = Convert.ToBoolean(rks.GetValue(@"IsRibonMinimized", false));

            rks.Close();
        }

        /// <summary>
        /// Occures on layout change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            PreviouseWindowState = WindowState;
        }

        DispatcherTimer timerv = new DispatcherTimer();

        //'update view tab after changing view settings
        void ExplorerBrowserControl_ViewEnumerationComplete(object sender, EventArgs e)
        {

            //timerv.Interval = new TimeSpan(0, 0, 7);
            //timerv.IsEnabled = true;
            //timerv.Tick += new EventHandler(timerv_Tick);

            searchcicles++;
            //BeforeSearcCicles = searchcicles;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
                                {
                                    IsCalledFromViewEnum = true;
                                    zoomSlider.Value = Explorer.ContentOptions.ThumbnailSize;
                                    IsCalledFromViewEnum = false;
                                    IShellView isv = Explorer.GetShellView();

                                    Collumns[] AllAvailColls =
                                        Explorer.AvailableColumns(isv, true);
                                    btnMoreColls.Items.Clear();

                                    for (int j = 1; j < 10; j++)
                                    {
                                        try
                                        {
                                            MenuItem mic = new MenuItem();
                                            mic.Header = AllAvailColls[j].Name;
                                            mic.Tag = AllAvailColls[j].pkey;
                                            mic.Click += new RoutedEventHandler(mic_Click);
                                            mic.IsCheckable = true;
                                            foreach (Collumns col in Explorer.AvailableVisibleColumns)
                                            {
                                                if (col.Name == AllAvailColls[j].Name)
                                                {
                                                    mic.IsChecked = true;
                                                }
                                            }
                                            btnMoreColls.Items.Add(mic);
                                        }
                                        catch (Exception)
                                        {

                                        }


                                    }

                                    int ItemsCount = Explorer.GetItemsCount();


                                    if (ItemsCount == 0)
                                    {
                                        sbiItemsCount.Visibility = System.Windows.Visibility.Collapsed;
                                    }
                                    else
                                    {
                                        sbiItemsCount.Visibility = System.Windows.Visibility.Visible;
                                    }
                                    if (ItemsCount == 1)
                                    {
                                        sbiItemsCount.Content = "1 item";
                                    }
                                    else
                                    {
                                        sbiItemsCount.Content = ItemsCount.ToString() + " items";
                                    }


                                    Separator sep = new Separator();
                                    btnMoreColls.Items.Add(sep);
                                    MenuItem micm = new MenuItem();
                                    micm.Header = "More Columns";
                                    micm.Tag = AllAvailColls;
                                    micm.Click += new RoutedEventHandler(micm_Click);
                                    btnMoreColls.Items.Add(micm);

                                    btnSort.Items.Clear();
                                    btnGroup.Items.Clear();

                                    Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser.SORTCOLUMN sc;
                                    Explorer.GetSortColInfo(out sc);
                                    Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser.PROPERTYKEY pkg;
                                    bool GroupDir;
                                    Explorer.GetGroupColInfo(out pkg, out GroupDir);

                                    foreach (Microsoft.WindowsAPICodePack.Controls.WindowsForms.Collumns item in Explorer.AvailableVisibleColumns)
                                    {

                                        if (item != null)
                                        {
                                            MenuItem mi = new MenuItem();
                                            mi.Header = item.Name;
                                            mi.Tag = item;
                                            mi.GroupName = "GR2";
                                            mi.IsCheckable = true;
                                            if ((item.pkey.fmtid == sc.propkey.fmtid) && (item.pkey.pid == sc.propkey.pid))
                                            {
                                                mi.IsChecked = true;
                                            }
                                            else
                                            {
                                                mi.IsChecked = false;
                                            }
                                            mi.Click += new RoutedEventHandler(mi_Click);
                                            btnSort.Items.Add(mi);

                                            MenuItem mig = new MenuItem();
                                            mig.Header = item.Name;
                                            mig.Tag = item;
                                            mig.GroupName = "GR3";
                                            mig.IsCheckable = true;
                                            if ((item.pkey.fmtid == pkg.fmtid) && (item.pkey.pid == pkg.pid))
                                            {
                                                mig.IsChecked = true;
                                            }
                                            else
                                            {
                                                mig.IsChecked = false;
                                            }
                                            mig.Click += new RoutedEventHandler(mig_Click);
                                            btnGroup.Items.Add(mig);
                                        }
                                    }
                                    Separator sp = new Separator();
                                    btnSort.Items.Add(sp);
                                    misa = new MenuItem();
                                    misa.Click += new RoutedEventHandler(misa_Click);
                                    misa.Header = "Ascending";
                                    misa.IsCheckable = true;


                                    misa.GroupName = "GR1";

                                    misd = new MenuItem();
                                    misd.Header = "Descending";
                                    misd.IsCheckable = true;
                                    misd.Click += new RoutedEventHandler(misd_Click);
                                    misd.GroupName = "GR1";
                                    if (sc.direction == Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser.SORT.ASCENDING)
                                    {
                                        misa.IsChecked = true;
                                    }
                                    else
                                    {
                                        misd.IsChecked = true;
                                    }
                                    btnSort.Items.Add(misa);
                                    btnSort.Items.Add(misd);
                                    misng = new MenuItem();
                                    misng.Header = "(none)";
                                    misng.GroupName = "GR4";
                                    misng.Click += new RoutedEventHandler(misng_Click);
                                    btnGroup.Items.Add(misng);
                                    Separator spg = new Separator();
                                    btnGroup.Items.Add(spg);
                                    misag = new MenuItem();
                                    misag.Header = "Ascending";
                                    misag.IsCheckable = true;


                                    misag.GroupName = "GR4";

                                    misdg = new MenuItem();
                                    misdg.Header = "Descending";
                                    misdg.IsCheckable = true;
                                    misdg.GroupName = "GR4";
                                    if (GroupDir)
                                    {
                                        misag.IsChecked = true;
                                    }
                                    else
                                    {
                                        misdg.IsChecked = true;
                                    }

                                    btnGroup.Items.Add(misag);
                                    btnGroup.Items.Add(misdg);

                                    int i = Explorer.ContentOptions.ThumbnailSize;
                                    if (Explorer.ContentOptions.ThumbnailSize == 256)
                                    {
                                        inRibbonGallery1.SelectedIndex = 0;

                                    }
                                    if (Explorer.ContentOptions.ThumbnailSize == 96)
                                    {
                                        inRibbonGallery1.SelectedIndex = 1;

                                    }
                                    if (Explorer.ContentOptions.ThumbnailSize == 64)
                                    {
                                        inRibbonGallery1.SelectedIndex = 2;

                                    }
                                    if (Explorer.ContentOptions.ThumbnailSize == 48 & Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Icon)
                                    {
                                        inRibbonGallery1.SelectedIndex = 3;
                                        btnSbIcons.IsChecked = true;
                                    }
                                    else
                                    {
                                        btnSbIcons.IsChecked = false;
                                    }
                                    if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.List)
                                    {
                                        inRibbonGallery1.SelectedIndex = 4;
                                    }
                                    if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Details)
                                    {
                                        inRibbonGallery1.SelectedIndex = 5;
                                        btnSbDetails.IsChecked = true;
                                    }
                                    else
                                    {
                                        btnSbDetails.IsChecked = false;
                                    }
                                    if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Tile)
                                    {
                                        inRibbonGallery1.SelectedIndex = 6;
                                        btnSbTiles.IsChecked = true;
                                    }
                                    else
                                    {
                                        btnSbTiles.IsChecked = false;
                                    }
                                    if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Content)
                                    {
                                        inRibbonGallery1.SelectedIndex = 7;
                                    }
                                }
            ));
            Explorer.ExplorerSetFocus();
            GC.WaitForFullGCComplete();
            GC.Collect();
        }

        void misd_Click(object sender, RoutedEventArgs e)
        {
            foreach (object item in btnSort.Items)
            {
                if (item is MenuItem)
                    if (((MenuItem)item).IsChecked && ((MenuItem)item != (sender as MenuItem)))
                    {
                        Explorer.SetSortCollumn(((Collumns)((MenuItem)item).Tag).pkey, ExplorerBrowser.SORT.DESCENDING);
                    }
            }
        }

        void misa_Click(object sender, RoutedEventArgs e)
        {
            foreach (object item in btnSort.Items)
            {
                if (item is MenuItem)
                    if (((MenuItem)item).IsChecked && ((MenuItem)item != (sender as MenuItem)))
                    {
                        Explorer.SetSortCollumn(((Collumns)((MenuItem)item).Tag).pkey, ExplorerBrowser.SORT.ASCENDING);
                    }
            }
        }

        void timerv_Tick(object sender, EventArgs e)
        {
            DoubleAnimation da = new DoubleAnimation(CurrentProgressValue, CurrentProgressValue + 1, new Duration(new TimeSpan(0, 0, 2)));
            da.FillBehavior = FillBehavior.Stop;
            CurrentProgressValue = CurrentProgressValue + 1;
            (sender as DispatcherTimer).Stop();
        }

        void micm_Click(object sender, RoutedEventArgs e)
        {

            MoreColumns fMoreCollumns = new MoreColumns();
            fMoreCollumns.PopulateAvailableColumns((Collumns[])(sender as MenuItem).Tag,
                Explorer, this.PointToScreen(Mouse.GetPosition(this)));
        }

        void mic_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
            ExplorerBrowser.PROPERTYKEY pkey = (ExplorerBrowser.PROPERTYKEY)mi.Tag;
            Explorer.SetColInView(pkey, !mi.IsChecked);
        }

        [DllImport("shell32.dll")]

        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out()] out IntPtr pidl, uint sfgaoIn, [Out()] out uint psfgaoOut);


        FileSystemWatcher fsw_AC;
        void ExplorerBrowserControl_NavigationComplete(object sender, NavigationCompleteEventArgs e)
        {
            try
            {
            //    this.Title = "Better Explorer - " + e.NewLocation.GetDisplayName(DisplayNameType.Default);



                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
                                    {
            //                            ConstructMoveToCopyToMenu();
            //                            if (e.NewLocation.IsFileSystemObject || e.NewLocation.IsNetDrive || e.NewLocation.ParsingName.StartsWith(@"\\"))
            //                            {

            //                                try
            //                                {
            //                                    fsw_AC = new FileSystemWatcher(e.NewLocation.ParsingName);
            //                                    fsw_AC.EnableRaisingEvents = true;
            //                                    fsw_AC.Created += new FileSystemEventHandler(fsw_AC_Created);
            //                                    fsw_AC.Deleted += new FileSystemEventHandler(fsw_AC_Deleted);
            //                                }
            //                                catch
            //                                {

            //                                }
            //                            }

            //                            if (e.NewLocation.IsFileSystemObject)
	           //                         {
		          //                          btnSizeChart.IsEnabled = true;
	           //                         }
            //                            else
	           //                         {
            //                                btnSizeChart.IsEnabled = false;
	           //                         }
            //                            

            //                            btnAutosizeColls.IsEnabled =
            //                                Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Details ?
            //                                  true : false;

            //                            this.breadcrumbBarControl1.LoadDirectory(e.NewLocation);
            //                            this.breadcrumbBarControl1.LastPath = e.NewLocation.ParsingName;
            //                            //}));
                                        bool IsChanged = (Explorer.GetSelectedItemsCount() > 0);
                                        bool isFuncAvail;
                                        if (Explorer.SelectedItems.Count == 1)
                                        {
                                            isFuncAvail = (Explorer.SelectedItems[0].IsFileSystemObject) ||
                                                Explorer.NavigationLog.CurrentLocation.ParsingName ==
                                                  KnownFolders.Libraries.ParsingName;
                                        }
                                        else
                                        {
                                            if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                                ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                            isFuncAvail = true;
                                        }
                                        btnCopy.IsEnabled = IsChanged;
                                        //btnPathCopy.IsEnabled = IsChanged;
                                        btnCut.IsEnabled = IsChanged;
                                        btnRename.IsEnabled = IsChanged;
                                        btnDelete.IsEnabled = IsChanged && isFuncAvail;
                                        btnCopyto.IsEnabled = IsChanged;
                                        btnMoveto.IsEnabled = IsChanged;
                                        btnSelNone.IsEnabled = IsChanged;
                                        btnUpLevel.IsEnabled = !(Explorer.NavigationLog.CurrentLocation.Parent == null);

            //                            IntPtr pIDL = IntPtr.Zero;

            //                            try
            //                            {
            //                                uint iAttribute;
            //                                SHParseDisplayName(Explorer.NavigationLog.CurrentLocation.ParsingName,
            //                                    IntPtr.Zero, out pIDL, (uint)0, out iAttribute);

            //                                WindowsAPI.SHFILEINFO sfi = new WindowsAPI.SHFILEINFO();
            //                                IntPtr Res = IntPtr.Zero;
            //                                if (pIDL != IntPtr.Zero)
            //                                {
            //                                    if (!Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
            //                                    {
            //                                        Res = WindowsAPI.SHGetFileInfo(pIDL, 0, ref sfi, (uint)Marshal.SizeOf(sfi), WindowsAPI.SHGFI.IconLocation | WindowsAPI.SHGFI.SmallIcon | WindowsAPI.SHGFI.PIDL);
            //                                    }

            //                                }

            //                                if (Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
            //                                {
            //                                    WindowsAPI.SHGetFileInfo(Explorer.NavigationLog.CurrentLocation.ParsingName, 0, ref sfi, (uint)Marshal.SizeOf(sfi), (uint)WindowsAPI.SHGFI.IconLocation | (uint)WindowsAPI.SHGFI.SmallIcon);

            //                                }
            //                                JumpTask JTask = new JumpTask();
            //                                JTask.ApplicationPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            //                                JTask.Arguments = "\"" + Explorer.NavigationLog.CurrentLocation.ParsingName + "\"";
            //                                JTask.Title = Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default);
            //                                JTask.IconResourcePath = sfi.szDisplayName;
            //                                JTask.IconResourceIndex = sfi.iIcon;
            //                                JumpList.AddToRecentCategory(JTask);
            //                                AppJL.Apply();

            //                            }
            //                            finally
            //                            {

            //                                if (pIDL != IntPtr.Zero)
            //                                    Marshal.FreeCoTaskMem(pIDL);
            //                            }

            //                            try
            //                            {

            //                                //CloseableTabItem it = new CloseableTabItem();
            //                                //CreateTabbarRKMenu(it);
            //                                Explorer.NavigationLog.CurrentLocation.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            //                                Explorer.NavigationLog.CurrentLocation.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            //                                (tabControl1.Items[CurrentTabIndex] as CloseableTabItem).Header = Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default);
            //                                (tabControl1.Items[CurrentTabIndex] as CloseableTabItem).TabIcon = Explorer.NavigationLog.CurrentLocation.Thumbnail.BitmapSource;
            //                                (tabControl1.Items[CurrentTabIndex] as CloseableTabItem).Path = Explorer.NavigationLog.CurrentLocation;
            //                                if (!isGoingBackOrForward)
            //                                {
            //                                    (tabControl1.Items[CurrentTabIndex] as CloseableTabItem).log.CurrentLocation = Explorer.NavigationLog.CurrentLocation;
            //                                }

            //                                isGoingBackOrForward = false;

            //                                leftNavBut.IsEnabled = (tabControl1.Items[CurrentTabIndex] as CloseableTabItem).log.CanNavigateBackwards;
            //                                rightNavBut.IsEnabled = (tabControl1.Items[CurrentTabIndex] as CloseableTabItem).log.CanNavigateForwards;

            //                                //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).DragEnter += new DragEventHandler(newt_DragEnter);
            //                                //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).DragLeave += new DragEventHandler(newt_DragLeave);
            //                                //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).DragOver += new DragEventHandler(newt_DragOver);
            //                                //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).Drop += new DragEventHandler(newt_Drop);
            //                                //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).AllowDrop = true;
            //                                //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).Index = CurrentTabIndex;
            //                                //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).CloseTab += new RoutedEventHandler(cti_CloseTab);
            //                                //tabControl1.Items[CurrentTabIndex] = it;
            //                                tabControl1.SelectedIndex = CurrentTabIndex;


            //                            }
            //                            catch (Exception)
            //                            {

            //                            }
            //                            bool isinLibraries = false;
            //                            if (Explorer.NavigationLog.CurrentLocation.Parent != null)
            //                            {
            //                                if (Explorer.NavigationLog.CurrentLocation.Parent.ParsingName ==
            //                                      KnownFolders.Libraries.ParsingName)
            //                                {
            //                                    isinLibraries = true;
            //                                }
            //                                else
            //                                {
            //                                    isinLibraries = false;
            //                                }
            //                            }

            //                            btnCreateFolder.IsEnabled = Explorer.NavigationLog.CurrentLocation.IsFileSystemObject ||
            //                                (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.Libraries.ParsingName) ||
            //                                (isinLibraries);
            //                            if (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.Libraries.ParsingName)
            //                            {
            //                                btnCreateFolder.Header = FindResource("btnNewLibraryCP");  //"New Library";
            //                                stNewFolder.Title = FindResource("btnNewLibraryCP").ToString();//"New Library";
            //                                stNewFolder.Text = "Creates a new library in the current folder.";
            //                                stNewFolder.Image = new BitmapImage(new Uri(@"/BetterExplorer;component/Images/newlib32.png", UriKind.Relative));
            //                                btnCreateFolder.LargeIcon = @"..\Images\newlib32.png";
            //                                btnCreateFolder.Icon = @"..\Images\newlib16.png";
            //                            }
            //                            else
            //                            {
            //                                btnCreateFolder.Header = FindResource("btnNewFolderCP");//"New Folder";
            //                                stNewFolder.Title = FindResource("btnNewFolderCP").ToString(); //"New Folder";
            //                                stNewFolder.Text = "Creates a new folder in the current folder";
            //                                stNewFolder.Image = new BitmapImage(new Uri(@"/BetterExplorer;component/Images/folder_new32.png", UriKind.Relative));
            //                                btnCreateFolder.LargeIcon = @"..\Images\folder_new32.png";
            //                                btnCreateFolder.Icon = @"..\Images\folder_new16.png";
            //                            }
            //                            if (Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
            //                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder)
            //                            {
            //                                ctgFolderTools.Visibility = Visibility.Visible;
            //                            }

            //                            if (Explorer.NavigationLog.CurrentLocation.ParsingName.Contains(KnownFolders.Libraries.ParsingName) &&
            //                                Explorer.NavigationLog.CurrentLocation.ParsingName != KnownFolders.Libraries.ParsingName)
            //                            {
            //                                ctgLibraries.Visibility = System.Windows.Visibility.Visible;
            //                                ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
            //                                ctgImage.Visibility = System.Windows.Visibility.Collapsed;
            //                                ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
            //                                ctgExe.Visibility = System.Windows.Visibility.Collapsed;
            //                                inLibrary = true;

            //                                //MessageBox.Show("In Library");
            //                                try
            //                                {
            //                                    ShellLibrary lib =
            //                                        ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
            //                                    IsFromSelectionOrNavigation = true;
            //                                    chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
            //                                    IsFromSelectionOrNavigation = false;
            //                                    foreach (ShellObject item in lib)
            //                                    {
            //                                        MenuItem miItem = new MenuItem();
            //                                        miItem.Header = item.GetDisplayName(DisplayNameType.Default);
            //                                        miItem.Tag = item;
            //                                        item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            //                                        item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            //                                        miItem.Icon = item.Thumbnail.BitmapSource;
            //                                        miItem.GroupName = "GRDS1";
            //                                        miItem.IsCheckable = true;
            //                                        miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
            //                                        miItem.Click += new RoutedEventHandler(miItem_Click);
            //                                        btnDefSave.Items.Add(miItem);
            //                                    }

            //                                    btnDefSave.IsEnabled = !(lib.Count == 0);
            //                                    lib.Close();
            //                                }
            //                                catch
            //                                {

            //                                }
            //                            }
            //                            else
            //                            {
            //                                if (!Explorer.NavigationLog.CurrentLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
            //                                {
            //                                    btnDefSave.Items.Clear();
            //                                    ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
            //                                    inLibrary = false;
            //                                }
            //                                //MessageBox.Show("Not in Library");
            //                            }
            //                            if (Explorer.NavigationLog.CurrentLocation.IsDrive)
            //                            {
            //                                ctgDrive.Visibility = System.Windows.Visibility.Visible;
            //                                inDrive = true;
            //                                //MessageBox.Show("In Drive");
            //                            }
            //                            else
            //                            {
            //                                ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
            //                                inDrive = false;
            //                                //MessageBox.Show("Not In Drive");
            //                            }
            //                            if (isinLibraries)
            //                            {
            //                                ctgFolderTools.Visibility = Visibility.Collapsed;
            //                            }

                                    }
                                ));

                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception exe)
            {
            //    ShellObject ne = e.NewLocation;
            //    bool isinLibraries = false;
            //    bool itisLibraries = false;

            //    if (ne.Parent.ParsingName == KnownFolders.Libraries.ParsingName)
            //    {
            //        isinLibraries = true;
            //    }
            //    else
            //    {
            //        isinLibraries = false;
            //    }

            //    if (ne.ParsingName == KnownFolders.Libraries.ParsingName)
            //    {
            //        itisLibraries = true;
            //    }
            //    else
            //    {
            //        itisLibraries = false;
            //    }

            //    //if (MessageBox.Show("An error occurred while loading a folder. Please report this issue at http://bexplorer.codeplex.com/. \r\n\r\nHere is some information about the folder being loaded:\r\n\r\nName: " + ne.GetDisplayName(DisplayNameType.Default) + "\r\nLocation: " + ne.ParsingName +
            //    //    "\r\n\r\nFolder, Drive, or Library: " + GetYesNoFromBoolean(ne.IsFolder) + "\r\nDrive: " + GetYesNoFromBoolean(ne.IsDrive) + "\r\nNetwork Drive: " + GetYesNoFromBoolean(ne.IsNetDrive) + "\r\nRemovable: " + GetYesNoFromBoolean(ne.IsRemovable) +
            //    //    "\r\nSearch Folder: " + GetYesNoFromBoolean(ne.IsSearchFolder) + "\r\nShared: " + GetYesNoFromBoolean(ne.IsShared) + "\r\nShortcut: " + GetYesNoFromBoolean(ne.IsLink) + "\r\nLibrary: " + GetYesNoFromBoolean(isinLibraries) + "\r\nLibraries Folder: " + GetYesNoFromBoolean(itisLibraries) +
            //    //    "\r\n\r\n Would you like to see additional information? Click No to try continuing the program.", "Error Occurred on Completing Navigation", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            //    //{
            //    //    MessageBox.Show("An error occurred while loading a folder. Please report this issue at http://bexplorer.codeplex.com/. \r\n\r\nHere is additional information about the error: \r\n\r\n" + exe.Message + "\r\n\r\n" + exe.ToString(), "Additional Error Data", MessageBoxButton.OK, MessageBoxImage.Error);
            //    //}
            }

            Explorer.ExplorerSetFocus();
        }

        public string GetYesNoFromBoolean(bool value)
        {
            if (value == true)
            {
                return "Yes";
            }
            else
            {
                return "No";
            }
        }

        void miItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            ShellObject SaveLoc = mi.Tag as ShellObject;

            if (Explorer.NavigationLog.CurrentLocation.ParsingName.Contains(KnownFolders.Libraries.ParsingName) &&
                Explorer.NavigationLog.CurrentLocation.ParsingName.EndsWith("library-ms"))
            {
                ShellLibrary lib =
                    ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
                lib.DefaultSaveFolder = SaveLoc.ParsingName;
                lib.Close();

            }
            else if (Explorer.SelectedItems[0].ParsingName.Contains(KnownFolders.Libraries.ParsingName))
            {
                ShellLibrary lib =
                    ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
                lib.DefaultSaveFolder = SaveLoc.ParsingName;
                lib.Close();

            }
        }

        void fsw_AC_Deleted(object sender, FileSystemEventArgs e)
        {
            if (true)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
                                {
                                    if (Explorer.NavigationLog.CurrentLocation.HasSubFolders)
                                    {
                                        try
                                        {
                                            //if (!(breadcrumbBarControl1.elPanel.Children[breadcrumbBarControl1.elPanel.Children.Count - 1] as BreadcrumbBarItem).HasDropDownMenu)
                                            //{
                                            //    breadcrumbBarControl1.UpdateLastItem(1);
                                            //}
                                        }
                                        catch
                                        {

                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            //if ((breadcrumbBarControl1.elPanel.Children[breadcrumbBarControl1.elPanel.Children.Count - 1] as BreadcrumbBarItem).HasDropDownMenu)
                                            //{
                                            //    breadcrumbBarControl1.UpdateLastItem(0);
                                            //}
                                        }
                                        catch
                                        {

                                        }
                                    }
                                }));
            }

        }

        void fsw_AC_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (ShellObject.FromParsingName(e.FullPath).IsFolder)
                {
                    try
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
                                    {
                                        //if (!(breadcrumbBarControl1.elPanel.Children[breadcrumbBarControl1.elPanel.Children.Count - 1] as BreadcrumbBarItem).HasDropDownMenu)
                                        //{
                                        //    breadcrumbBarControl1.UpdateLastItem(1);
                                        //}
                                    }));
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {

            }
        }


        void Explorer_ExplorerGotFocus(object sender, EventArgs e)
        {
            if (breadcrumbBarControl1.IsInEditMode)
            {
                breadcrumbBarControl1.ExitEditMode();
            }
            
        }

        void Explorer_LostFocus(object sender, EventArgs e)
        {
            IsAfterRename = false;
        }

        void Explorer_KeyUP(object sender, ExplorerKeyUPEventArgs e)
        {

            if (e.Key == 27 || e.Key == 13 || e.Key == 777 || e.Key == 17 || e.Key == 778)
            {
                IsAfterRename = true;
            }         
        }

        void Explorer_RenameFinished(object sender, EventArgs e)
        {
            IsAfterRename = true;
        }

        void Explorer_GotFocus(object sender, EventArgs e)
        {
            
        }

        void FocusTimer_Tick(object sender, EventArgs e)
        {
            if (Keyboard.FocusedElement != null)
            {
                ExplorerBrowser.IsBool = true;
            }
            else
            {
                ExplorerBrowser.IsBool = false;
            }
            // MessageBox.Show(Keyboard.FocusedElement.GetType().ToString());
            //if (Keyboard.FocusedElement.GetType().ToString().Contains(windowsfor))
            //{

            //}
            //{
            //    if (!this.IsActive)
            //    {
            //        this.Activate();
            //    }

            //}
        }


        private bool IsLibW = false;
        void Explorer_ItemsChanged(object sender, EventArgs e)
        {
            int ItemsCount = Explorer.GetItemsCount();
            sbiItemsCount.Content = ItemsCount == 1 ? ItemsCount.ToString() + " item" : ItemsCount.ToString() +
                                          " items";

            if (IsAfterRename)
                Explorer.ExplorerSetFocus();

            if (IsAfterFolderCreate)
            {

                if (!IsCompartibleRename)
                {
                    Explorer.DoRename(LastPath.Replace(@"\\", @"\"), IsLibW);
                }
                IsAfterFolderCreate = false;

            }


        }

        void Explorer_ExplorerBrowserMouseLeave(object sender, EventArgs e)
        {
            //if (Itmpop != null)
            //{
            //    Itmpop.Visibility = System.Windows.Visibility.Hidden;
            //}
        }

        void Explorer_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MessageBox.Show("over");
        }

        System.Windows.Forms.Timer thumbtimer = new System.Windows.Forms.Timer();
        Boolean isPreviewShown = false;
        int LastSelectedItemIndex = -2;
        //PreviewMedia PicturePreview;
        Rectangle ElRectangle;
        string IIPath;

        //ItemPopup Itmpop;
        //void Explorer_ItemHot(object sender, ExplorerAUItemEventArgs e)
        //{
        //    //MessageBox.Show(Explorer.AvailableVisibleColumns[0].Width.ToString());
        //    if (IsHFlyoutEnabled)
        //    {
        //        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
        //                            (ThreadStart)(() =>
        //                            {

        //                                if (!e.IsElementBackground & e.Elementindex != -1)
        //                                {
        //                                    ElRectangle = e.ElementRectangle;
        //                                    //MessageBox.Show(e.Elementindex.ToString());
        //                                    string IPath = Explorer.GetItemPath(e.Elementindex);
        //                                    IIPath = IPath;
        //                                    string ext = Path.GetExtension(IPath);
        //                                    System.Windows.Media.Brush ItemPopBGBrush;
        //                                    if (Explorer.ItemIsFolder(e.Elementindex))
        //                                    {
        //                                        ItemPopBGBrush = System.Windows.Media.Brushes.Blue;
        //                                    }
        //                                    else
        //                                    {
        //                                        ItemPopBGBrush = System.Windows.Media.Brushes.Red;
        //                                    }
        //                                    if ((LastSelectedItemIndex != e.Elementindex && LastSelectedItemIndex != -2))
        //                                    {

        //                                        if (Itmpop != null)
        //                                        {
        //                                            //Itmpop.Close();
        //                                            //Itmpop = null;
        //                                            Itmpop.Visibility = System.Windows.Visibility.Hidden;
        //                                            //Itmpop = new ItemPopup();
        //                                            ExplorerBrowser.PopFX = e.ElementRectangle.Right;
        //                                            ExplorerBrowser.PopFY = e.ElementRectangle.Top;
        //                                            Itmpop.Owner = this;
        //                                            if (Explorer.ContentOptions.ViewMode != ExplorerBrowserViewMode.Details)
        //                                            {

        //                                                Itmpop.ShowAtPosition(new System.Windows.Point(e.ImageRec.Right, e.ImageRec.Bottom),
        //                                                    ItemPopBGBrush);
        //                                            }
        //                                            else
        //                                            {

        //                                                Itmpop.ShowAtPosition(new System.Windows.Point(e.TextRec.Left +
        //                                                    Explorer.AvailableVisibleColumns[0].Width - 22, e.TextRec.Bottom),
        //                                                      ItemPopBGBrush);
        //                                            }
        //                                        }

        //                                    }

        //                                    //if (ext.ToLowerInvariant() == ".jpg")
        //                                    //{

        //                                    //    if ((LastSelectedItemIndex == e.Elementindex || LastSelectedItemIndex == -2))
        //                                    //    {
        //                                    //        //if (PicturePreview != null)
        //                                    //        //{
        //                                    //        //    PicturePreview.Disposeimg();
        //                                    //        //    PicturePreview.Dispose();
        //                                    //        //    isPreviewShown = false;
        //                                    //        //}
        //                                    //        if (PicturePreview == null)
        //                                    //        {
        //                                    //            PicturePreview = new PreviewMedia();

        //                                    //            PicturePreview.SetImage(IPath);
        //                                    //            //Thread.Sleep(400);
        //                                    //            //PicturePreview.LoadPreview(new System.Drawing.Point(e.ElementRectangle.Left, e.ElementRectangle.Bottom));

        //                                    //            isPreviewShown = false;

        //                                    //            thumbtimer.Start();
        //                                    //        }


        //                                    //        GC.WaitForPendingFinalizers();
        //                                    //        GC.Collect();
        //                                    //    }
        //                                    //    else
        //                                    //    {
        //                                    //        thumbtimer.Stop();
        //                                    //        if (PicturePreview != null)
        //                                    //        {
        //                                    //            PicturePreview.Disposeimg();
        //                                    //            PicturePreview.Dispose();
        //                                    //        }
        //                                    //        PicturePreview = new PreviewMedia();
        //                                    //        PicturePreview.SetImage(IPath);

        //                                    //        //PicturePreview.LoadPreview(new System.Drawing.Point(e.ElementRectangle.Left, e.ElementRectangle.Bottom));

        //                                    //        //isPreviewShown = true;

        //                                    //        isPreviewShown = false;
        //                                    //        thumbtimer.Start();
        //                                    //        GC.WaitForPendingFinalizers();
        //                                    //        GC.Collect();

        //                                    //    }


        //                                    //}
        //                                    //else
        //                                    //{
        //                                    //    thumbtimer.Stop();
        //                                    //    if (PicturePreview != null)
        //                                    //    {
        //                                    //        PicturePreview.Disposeimg();
        //                                    //        PicturePreview.Dispose();
        //                                    //        isPreviewShown = false;

        //                                    //    }
        //                                    //    GC.WaitForPendingFinalizers();
        //                                    //    GC.Collect();

        //                                    //}

        //                                }
        //                                else
        //                                {
        //                                    //thumbtimer.Stop();
        //                                    //if (PicturePreview != null)
        //                                    //{
        //                                    //    PicturePreview.Disposeimg();
        //                                    //    PicturePreview.Dispose(); 

        //                                    //    isPreviewShown = false;
        //                                    //}
        //                                    //GC.WaitForPendingFinalizers();
        //                                    //GC.Collect();
        //                                    if (Itmpop != null)
        //                                    {
        //                                        //Itmpop.Close();
        //                                        //Itmpop = null;
        //                                        Itmpop.Visibility = System.Windows.Visibility.Hidden;
        //                                        GC.WaitForPendingFinalizers();
        //                                        GC.Collect();
        //                                    }

        //                                }
        //                                LastSelectedItemIndex = e.Elementindex;
        //                            }));
        //    }

        //}

        public static bool IsFocusedItemHost()
        {
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            return elementWithFocus.IsKeyboardFocused;
        }

        void thumbtimer_Tick(object sender, EventArgs e)
        {
            //Win32Point p = new Win32Point();
            //GetCursorPos(ref p);
            ////WindowsAPI.SetParent(PicturePreview.Handle, this.Handle);
            //WindowsAPI.ShowWindow((int)PicturePreview.Handle, 4);// PicturePreview.Show();
            //PicturePreview.Left = p.X + 5;

            //PicturePreview.Top = p.Y + 5;
            //PicturePreview.Opacity = 100;
            (sender as System.Windows.Forms.Timer).Stop();
            if (!isPreviewShown)
            {
                //PicturePreview.LoadPreview(new System.Drawing.Point(ElRectangle.Left, ElRectangle.Bottom));

                isPreviewShown = true;
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        bool IsCancel = false;
        void Explorer_NavigationPending(object sender, NavigationPendingEventArgs e)
        {

            if (fsw_AC != null)
                fsw_AC.Dispose();

            e.Cancel = IsCancel;
            if (IsAfterRename)
                breadcrumbBarControl1.ExitEditMode();

            try
            {
            
                this.Title = "Better Explorer - " + e.PendingLocation.GetDisplayName(DisplayNameType.Default);



                Dispatcher.BeginInvoke(DispatcherPriority.Render, (ThreadStart)(() =>
                                    {
                                        ConstructMoveToCopyToMenu();
                                        if (e.PendingLocation.IsFileSystemObject || e.PendingLocation.IsNetDrive || e.PendingLocation.ParsingName.StartsWith(@"\\"))
                                        {

                                            try
                                            {
                                                fsw_AC = new FileSystemWatcher(e.PendingLocation.ParsingName);
                                                fsw_AC.EnableRaisingEvents = true;
                                                fsw_AC.Created += new FileSystemEventHandler(fsw_AC_Created);
                                                fsw_AC.Deleted += new FileSystemEventHandler(fsw_AC_Deleted);
                                            }
                                            catch
                                            {

                                            }
                                        }

                                        if (e.PendingLocation.IsFileSystemObject)
	                                    {
		                                    btnSizeChart.IsEnabled = true;
	                                    }
                                        else
	                                    {
                                            btnSizeChart.IsEnabled = false;
	                                    }

                                        this.breadcrumbBarControl1.LoadDirectory(e.PendingLocation);
                                        this.breadcrumbBarControl1.LastPath = e.PendingLocation.ParsingName;
                                        IntPtr pIDL = IntPtr.Zero;

                                        try
                                        {
                                            uint iAttribute;
                                            SHParseDisplayName(e.PendingLocation.ParsingName,
                                                IntPtr.Zero, out pIDL, (uint)0, out iAttribute);

                                            WindowsAPI.SHFILEINFO sfi = new WindowsAPI.SHFILEINFO();
                                            IntPtr Res = IntPtr.Zero;
                                            if (pIDL != IntPtr.Zero)
                                            {
                                                if (!e.PendingLocation.IsFileSystemObject)
                                                {
                                                    Res = WindowsAPI.SHGetFileInfo(pIDL, 0, ref sfi, (uint)Marshal.SizeOf(sfi), WindowsAPI.SHGFI.IconLocation | WindowsAPI.SHGFI.SmallIcon | WindowsAPI.SHGFI.PIDL);
                                                }

                                            }

                                            if (e.PendingLocation.IsFileSystemObject)
                                            {
                                                WindowsAPI.SHGetFileInfo(e.PendingLocation.ParsingName, 0, ref sfi, (uint)Marshal.SizeOf(sfi), (uint)WindowsAPI.SHGFI.IconLocation | (uint)WindowsAPI.SHGFI.SmallIcon);

                                            }
                                            JumpTask JTask = new JumpTask();
                                            JTask.ApplicationPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                                            JTask.Arguments = "\"" + e.PendingLocation.ParsingName + "\"";
                                            JTask.Title = e.PendingLocation.GetDisplayName(DisplayNameType.Default);
                                            JTask.IconResourcePath = sfi.szDisplayName;
                                            JTask.IconResourceIndex = sfi.iIcon;
                                            JumpList.AddToRecentCategory(JTask);
                                            AppJL.Apply();

                                        }
                                        finally
                                        {

                                            if (pIDL != IntPtr.Zero)
                                                Marshal.FreeCoTaskMem(pIDL);
                                        }

                                        try
                                        {

                                            //CloseableTabItem it = new CloseableTabItem();
                                            //CreateTabbarRKMenu(it);
                                            e.PendingLocation.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                            e.PendingLocation.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                            (tabControl1.SelectedItem as CloseableTabItem).Header = e.PendingLocation.GetDisplayName(DisplayNameType.Default);
                                            (tabControl1.SelectedItem as CloseableTabItem).TabIcon = e.PendingLocation.Thumbnail.BitmapSource;
                                            (tabControl1.SelectedItem as CloseableTabItem).Path = e.PendingLocation;
                                            if (!isGoingBackOrForward)
                                            {
                                                (tabControl1.SelectedItem as CloseableTabItem).log.CurrentLocation = e.PendingLocation;
                                            }

                                            isGoingBackOrForward = false;

                                            leftNavBut.IsEnabled = (tabControl1.SelectedItem as CloseableTabItem).log.CanNavigateBackwards;
                                            rightNavBut.IsEnabled = (tabControl1.SelectedItem as CloseableTabItem).log.CanNavigateForwards;

                                            //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).DragEnter += new DragEventHandler(newt_DragEnter);
                                            //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).DragLeave += new DragEventHandler(newt_DragLeave);
                                            //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).DragOver += new DragEventHandler(newt_DragOver);
                                            //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).Drop += new DragEventHandler(newt_Drop);
                                            //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).AllowDrop = true;
                                            //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).Index = CurrentTabIndex;
                                            //(tabControl1.Items[CurrentTabIndex] as CloseableTabItem).CloseTab += new RoutedEventHandler(cti_CloseTab);
                                            //tabControl1.Items[CurrentTabIndex] = it;
                                            tabControl1.SelectedIndex = CurrentTabIndex;


                                        }
                                        catch (Exception)
                                        {

                                        }
                                        bool isinLibraries = false;
                                        if (e.PendingLocation.Parent != null)
                                        {
                                            if (e.PendingLocation.Parent.ParsingName ==
                                                  KnownFolders.Libraries.ParsingName)
                                            {
                                                isinLibraries = true;
                                            }
                                            else
                                            {
                                                isinLibraries = false;
                                            }
                                        }

                                        btnCreateFolder.IsEnabled = e.PendingLocation.IsFileSystemObject ||
                                            (e.PendingLocation.ParsingName == KnownFolders.Libraries.ParsingName) ||
                                            (isinLibraries);

                                        if (e.PendingLocation.ParsingName == KnownFolders.Libraries.ParsingName)
                                        {
                                            btnCreateFolder.Header = FindResource("btnNewLibraryCP");  //"New Library";
                                            stNewFolder.Title = FindResource("btnNewLibraryCP").ToString();//"New Library";
                                            stNewFolder.Text = "Creates a new library in the current folder.";
                                            stNewFolder.Image = new BitmapImage(new Uri(@"/BetterExplorer;component/Images/newlib32.png", UriKind.Relative));
                                            btnCreateFolder.LargeIcon = @"..\Images\newlib32.png";
                                            btnCreateFolder.Icon = @"..\Images\newlib16.png";
                                        }
                                        else
                                        {
                                            btnCreateFolder.Header = FindResource("btnNewFolderCP");//"New Folder";
                                            stNewFolder.Title = FindResource("btnNewFolderCP").ToString(); //"New Folder";
                                            stNewFolder.Text = "Creates a new folder in the current folder";
                                            stNewFolder.Image = new BitmapImage(new Uri(@"/BetterExplorer;component/Images/folder_new32.png", UriKind.Relative));
                                            btnCreateFolder.LargeIcon = @"..\Images\folder_new32.png";
                                            btnCreateFolder.Icon = @"..\Images\folder_new16.png";
                                        }
                                        if (e.PendingLocation.IsFolder && !e.PendingLocation.IsDrive &&
                                            !e.PendingLocation.IsSearchFolder)
                                        {
                                            ctgFolderTools.Visibility = Visibility.Visible;
                                        }

                                        if (e.PendingLocation.ParsingName.Contains(KnownFolders.Libraries.ParsingName) &&
                                            e.PendingLocation.ParsingName != KnownFolders.Libraries.ParsingName)
                                        {
                                            ctgLibraries.Visibility = System.Windows.Visibility.Visible;
                                            ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                            ctgImage.Visibility = System.Windows.Visibility.Collapsed;
                                            ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
                                            ctgExe.Visibility = System.Windows.Visibility.Collapsed;
                                            inLibrary = true;

                                            try
                                            {
                                                ShellLibrary lib =
                                                    ShellLibrary.Load(e.PendingLocation.GetDisplayName(DisplayNameType.Default), false);
                                                IsFromSelectionOrNavigation = true;
                                                chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
                                                IsFromSelectionOrNavigation = false;
                                                foreach (ShellObject item in lib)
                                                {
                                                    MenuItem miItem = new MenuItem();
                                                    miItem.Header = item.GetDisplayName(DisplayNameType.Default);
                                                    miItem.Tag = item;
                                                    item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                                    item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                                    miItem.Icon = item.Thumbnail.BitmapSource;
                                                    miItem.GroupName = "GRDS1";
                                                    miItem.IsCheckable = true;
                                                    miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
                                                    miItem.Click += new RoutedEventHandler(miItem_Click);
                                                    btnDefSave.Items.Add(miItem);
                                                }

                                                btnDefSave.IsEnabled = !(lib.Count == 0);
                                                lib.Close();
                                            }
                                            catch
                                            {

                                            }
                                        }
                                        else
                                        {
                                            if (!e.PendingLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
                                            {
                                                btnDefSave.Items.Clear();
                                                ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
                                                inLibrary = false;
                                            }
                                            //MessageBox.Show("Not in Library");
                                        }
                                        if (e.PendingLocation.IsDrive)
                                        {
                                            ctgDrive.Visibility = System.Windows.Visibility.Visible;
                                            inDrive = true;
                                            //MessageBox.Show("In Drive");
                                        }
                                        else
                                        {
                                            ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                            inDrive = false;
                                            //MessageBox.Show("Not In Drive");
                                        }
                                        if (isinLibraries)
                                        {
                                            ctgFolderTools.Visibility = Visibility.Collapsed;
                                        }

                                    }
                                ));

                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception exe)
            {
                ShellObject ne = e.PendingLocation;
                bool isinLibraries = false;
                bool itisLibraries = false;

                if (ne.Parent.ParsingName == KnownFolders.Libraries.ParsingName)
                {
                    isinLibraries = true;
                }
                else
                {
                    isinLibraries = false;
                }

                if (ne.ParsingName == KnownFolders.Libraries.ParsingName)
                {
                    itisLibraries = true;
                }
                else
                {
                    itisLibraries = false;
                }

                //if (MessageBox.Show("An error occurred while loading a folder. Please report this issue at http://bexplorer.codeplex.com/. \r\n\r\nHere is some information about the folder being loaded:\r\n\r\nName: " + ne.GetDisplayName(DisplayNameType.Default) + "\r\nLocation: " + ne.ParsingName +
                //    "\r\n\r\nFolder, Drive, or Library: " + GetYesNoFromBoolean(ne.IsFolder) + "\r\nDrive: " + GetYesNoFromBoolean(ne.IsDrive) + "\r\nNetwork Drive: " + GetYesNoFromBoolean(ne.IsNetDrive) + "\r\nRemovable: " + GetYesNoFromBoolean(ne.IsRemovable) +
                //    "\r\nSearch Folder: " + GetYesNoFromBoolean(ne.IsSearchFolder) + "\r\nShared: " + GetYesNoFromBoolean(ne.IsShared) + "\r\nShortcut: " + GetYesNoFromBoolean(ne.IsLink) + "\r\nLibrary: " + GetYesNoFromBoolean(isinLibraries) + "\r\nLibraries Folder: " + GetYesNoFromBoolean(itisLibraries) +
                //    "\r\n\r\n Would you like to see additional information? Click No to try continuing the program.", "Error Occurred on Completing Navigation", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                //{
                //    MessageBox.Show("An error occurred while loading a folder. Please report this issue at http://bexplorer.codeplex.com/. \r\n\r\nHere is additional information about the error: \r\n\r\n" + exe.Message + "\r\n\r\n" + exe.ToString(), "Additional Error Data", MessageBoxButton.OK, MessageBoxImage.Error);
                //}
            }

            Explorer.ExplorerSetFocus();

            //if (Itmpop.Visibility == System.Windows.Visibility.Visible)
            //{
            //    Itmpop.Visibility = System.Windows.Visibility.Hidden;
            //}
        }



        void Explorer_ViewChanged(object sender, ViewChangedEventArgs e)
        {

            if (e.ThumbnailSize == 256)
            {
                inRibbonGallery1.SelectedIndex = 0;

            }
            if (e.ThumbnailSize == 96)
            {
                inRibbonGallery1.SelectedIndex = 1;

            }
            if (e.ThumbnailSize == 64)
            {
                inRibbonGallery1.SelectedIndex = 2;

            }
            if (e.ThumbnailSize == 48 & e.View == ExplorerBrowserViewMode.Icon)
            {
                inRibbonGallery1.SelectedIndex = 3;
                btnSbIcons.IsChecked = true;
            }
            else
            {
                btnSbIcons.IsChecked = false;
            }
            if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.List)
            {
                inRibbonGallery1.SelectedIndex = 4;
            }
            if (e.View == ExplorerBrowserViewMode.Details)
            {
                inRibbonGallery1.SelectedIndex = 5;
                btnSbDetails.IsChecked = true;
            }
            else
            {
                btnSbDetails.IsChecked = false;
            }
            if (e.View == ExplorerBrowserViewMode.Tile)
            {
                inRibbonGallery1.SelectedIndex = 6;
                btnSbTiles.IsChecked = true;
            }
            else
            {
                btnSbTiles.IsChecked = false;
            }
            if (e.View == ExplorerBrowserViewMode.Content)
            {
                inRibbonGallery1.SelectedIndex = 7;
            }
            IsCalledFromViewEnum = true;
            zoomSlider.Value = e.ThumbnailSize;
            IsCalledFromViewEnum = false;

            btnAutosizeColls.IsEnabled = e.View == ExplorerBrowserViewMode.Details ? true : false;

        }

        void fsw_Renamed(object sender, RenamedEventArgs e)
        {


            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                         (Action)(() =>
                         {
                             foreach (MenuItem item in btnFavorites.Items)
                             {
                                 ShellLink lnk = new ShellLink(e.FullPath);
                                 if (item.Tag.ToString() == lnk.Target)
                                 {
                                     item.Header = Path.GetFileNameWithoutExtension(e.Name);
                                 }
                                 lnk.Dispose();
                             }
                         }));

        }

        void fsw_Deleted(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                             (Action)(() =>
                             {
                                 MenuItem ItemForRemove = null;
                                 foreach (MenuItem item in btnFavorites.Items)
                                 {

                                     if (item.Header.ToString() == Path.GetFileNameWithoutExtension(e.Name))
                                     {
                                         ItemForRemove = item;

                                     }

                                 }
                                 btnFavorites.Items.Remove(ItemForRemove);
                             }));
        }

        void fsw_Created(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                             (Action)(() =>
                             {
                                 if (Path.GetExtension(e.FullPath).ToLowerInvariant() == ".lnk")
                                 {
                                     ShellObject so = ShellObject.FromParsingName(e.FullPath);
                                     MenuItem mi = new MenuItem();
                                     mi.Header = so.GetDisplayName(DisplayNameType.Default);
                                     ShellLink lnk = new ShellLink(so.ParsingName);
                                     mi.Tag = lnk.Target;
                                     lnk.Dispose();
                                     so.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                     so.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                     ImageSource icon = so.Thumbnail.BitmapSource;
                                     mi.Icon = icon;
                                     mi.Click += new RoutedEventHandler(mif_Click);
                                     so.Dispose();
                                     btnFavorites.Items.Add(mi);
                                 }
                             }));
            WindowsAPI.SetFocus(ExplorerBrowser.SysListViewHandle);
        }


        void ExplorerBrowserControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            //TheStatusBar.InvalidateVisual();
            //TheStatusBar.UpdateLayout();
        }

        void ExplorerBrowserControl_ClientSizeChanged(object sender, EventArgs e)
        {
            //TheStatusBar.InvalidateVisual();
            //TheStatusBar.UpdateLayout();
        }

        bool IsSelectionRized = false;
        //BackgroundWorker bwSelectionChanged = new BackgroundWorker();
        //'Selection change (when an item is selected in a folder)
        void ExplorerBrowserControl_SelectionChanged(object sender, EventArgs e)
        {


            if (IsAfterRename)
            {
                breadcrumbBarControl1.ExitEditMode();
                Explorer.Focus();
            }
            if (ctgSearch.Visibility == System.Windows.Visibility.Visible)
            {
                ctgSearch.Visibility = System.Windows.Visibility.Collapsed;
                TheRibbon.SelectedTabItem = HomeTab;
            }



            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {

                if (!IsSelectionRized)
                {
                    try
                    {
                        // declare variables and constants
                        int SelItemsCount = Explorer.GetSelectedItemsCount();

                        string defapp = "";
                        bool IsChanged = (SelItemsCount > 0);
                        bool isFuncAvail;
                        bool isEditAvailable;
                        string ext;
                        IsSelectionRized = true;

                        // set up buttons
                        btnOpenWith.Items.Clear();
                        btnDefSave.Items.Clear();



                        if (SelItemsCount == 0)
                        {
                            // WHAT TO DO IF NO ITEMS ARE SELECTED
                            //MessageBox.Show("No Items Selected");

                            // hide status bar items
                            sbiSelItemsCount.Visibility = System.Windows.Visibility.Collapsed;
                            spSelItems.Visibility = System.Windows.Visibility.Collapsed;

                            // disable buttons
                            btnShare.IsEnabled = false;
                            btnCopy.IsEnabled = false;
                            btnCut.IsEnabled = false;
                            btnRename.IsEnabled = false;
                            btnDelete.IsEnabled = false;
                            btnCopyto.IsEnabled = false;
                            btnMoveto.IsEnabled = false;
                            btnSelNone.IsEnabled = false;
                            btnOpenWith.IsEnabled = false;
                            btnEdit.IsEnabled = false;
                            btnHistory.IsEnabled = false;
                            btnAdvancedSecurity.IsEnabled = false;


                            // hide contextual tabs
                            ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
                            ctgExe.Visibility = System.Windows.Visibility.Collapsed;
                            if (Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
                            {
                                ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
                            }
                            else
                                ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;

                            ctgImage.Visibility = System.Windows.Visibility.Collapsed;

                            // if the current viewing location is a Drive, show Drive Tools.
                            if (inDrive == true)
                            {
                                ctgDrive.Visibility = System.Windows.Visibility.Visible;
                            }
                            else
                            {
                                ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                            }

                            // if the current viewing location is a Library, show Library Tools.
                            if (Explorer.NavigationLog.CurrentLocation.Parent != null)
                            {
                                if (Explorer.NavigationLog.CurrentLocation.Parent.ParsingName == KnownFolders.Libraries.ParsingName)
                                {
                                    ctgFolderTools.Visibility = Visibility.Collapsed;
                                }
                            }

                            if (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.Libraries.ParsingName)
                            {
                                ctgFolderTools.Visibility = Visibility.Collapsed;
                            }

                            if (inLibrary == true)
                            {
                                ctgFolderTools.Visibility = Visibility.Collapsed;
                                ctgLibraries.Visibility = System.Windows.Visibility.Visible;
                                ShellLibrary lib =
                                    ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
                                IsFromSelectionOrNavigation = true;
                                chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
                                IsFromSelectionOrNavigation = false;
                                foreach (ShellObject item in lib)
                                {
                                    MenuItem miItem = new MenuItem();
                                    miItem.Header = item.GetDisplayName(DisplayNameType.Default);
                                    miItem.Tag = item;
                                    item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                    item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                    miItem.Icon = item.Thumbnail.BitmapSource;
                                    miItem.GroupName = "GRDS1";
                                    miItem.Click += new RoutedEventHandler(miItem_Click);
                                    miItem.IsCheckable = true;
                                    miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
                                    btnDefSave.Items.Add(miItem);
                                }

                                btnDefSave.IsEnabled = !(lib.Count == 0);
                                lib.Close();
                            }
                            else
                            {
                                if (!Explorer.NavigationLog.CurrentLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
                                {
                                    ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
                                }

                            }
                        }
                        else
                        {
                            // WHAT TO DO IF ITEMS ARE SELECTED

                            // show status bar items
                            sbiSelItemsCount.Visibility = System.Windows.Visibility.Visible;
                            spSelItems.Visibility = System.Windows.Visibility.Visible;

                            // enable (most) buttons
                            btnCopy.IsEnabled = true;
                            btnCut.IsEnabled = true;
                            btnRename.IsEnabled = true;
                            btnCopyto.IsEnabled = true;
                            btnMoveto.IsEnabled = true;
                            btnSelNone.IsEnabled = true;
                            btnAdvancedSecurity.IsEnabled = false;
                            btnDefSave.Items.Clear();

                            if (SelItemsCount == 1)
                            {
                                // IF ONE ITEM IS SELECTED
                                ShellObject SelectedItem = Explorer.SelectedItems[0];
                                //MessageBox.Show("One Item Selected \r\n" + SelectedItem.ParsingName);

                                // set up status bar
                                sbiSelItemsCount.Content = "1 item selected";

                                // set variables

                                btnShare.IsEnabled = SelectedItem.IsFolder && SelectedItem.IsFileSystemObject;
                                btnAdvancedSecurity.IsEnabled = SelectedItem.IsFileSystemObject;
                                isFuncAvail = (SelectedItem.IsFileSystemObject &&
                                    (Explorer.NavigationLog.CurrentLocation.ParsingName != KnownFolders.Computer.ParsingName));
                                ext = System.IO.Path.GetExtension(SelectedItem.ParsingName);
                                RegistryKey rg = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + ext + @"\OpenWithProgids");
                                if (rg != null)
                                {

                                    string filetype = rg.GetValueNames()[0];
                                    rg.Close();
                                    RegistryKey rgtype = Registry.ClassesRoot.OpenSubKey(filetype + @"\shell\edit\command");
                                    if (rgtype != null)
                                    {
                                        string editcommand = (string)rgtype.GetValue("");

                                        isEditAvailable = true;
                                        EditComm = editcommand.Replace("\"", "");
                                        rgtype.Close();
                                    }
                                    else
                                    {
                                        isEditAvailable = false;
                                    }
                                }
                                else
                                {
                                    isEditAvailable = false;
                                }

                                // set up Open With button
                                List<string> iiii = new List<string>();
                                if (isFuncAvail)
                                {

                                    string extension =
                                        System.IO.Path.GetExtension(SelectedItem.ParsingName);
                                    iiii = Explorer.RecommendedPrograms(extension);
                                    MenuItem mid = new MenuItem();
                                    defapp = WindowsAPI.GetAssoc(extension, WindowsAPI.AssocF.Verify,
                                                        WindowsAPI.AssocStr.Executable);
                                    if (defapp != "" && defapp != "\"%1\"" && extension != "")
                                    {

                                        string DefAppName = WindowsAPI.GetAssoc(extension, WindowsAPI.AssocF.Verify,
                                                                                                WindowsAPI.AssocStr.FriendlyAppName);
                                        try
                                        {
                                            ShellObject objd = ShellObject.FromParsingName(defapp);
                                            mid.Header = DefAppName;
                                            mid.Tag = defapp;
                                            mid.Click += new RoutedEventHandler(miow_Click);
                                            objd.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                            objd.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                            mid.Icon = objd.Thumbnail.BitmapSource;
                                            objd.Dispose();
                                            btnOpenWith.Items.Add(mid);
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }

                                    foreach (string item in iiii)
                                    {
                                        // we'll see if it works without checking for "firefox.exe"
                                        //if (item != "firefox.exe" && item != "CompressedFolder")
                                        if (item != "CompressedFolder")
                                        {
                                            MenuItem mi = new MenuItem();
                                            string deffappname;
                                            String ExePath = "";
                                            ExePath = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify |
                                                WindowsAPI.AssocF.Open_ByExeName, WindowsAPI.AssocStr.Executable);
                                            deffappname = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify |
                                                WindowsAPI.AssocF.Open_ByExeName, WindowsAPI.AssocStr.FriendlyAppName);
                                            if (!File.Exists(ExePath))
                                            {
                                                ExePath = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify,
                                                     WindowsAPI.AssocStr.Executable);
                                            }

                                            string andeffappname = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify,
                                                WindowsAPI.AssocStr.FriendlyAppName);

                                            if (deffappname == "")
                                            {
                                                deffappname = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify,
                                                    WindowsAPI.AssocStr.FriendlyAppName);
                                            }

                                            if (andeffappname != "" && Char.IsLetterOrDigit(andeffappname, 1) && !Char.IsLetterOrDigit(deffappname, 1)
                                                                                            && (System.IO.Path.GetExtension(ExePath).ToLowerInvariant() == ".exe" ||
                                                                                                System.IO.Path.GetExtension(ExePath).ToLowerInvariant() == ".dll"))
                                            {
                                                deffappname = andeffappname;
                                            }


                                            try
                                            {
                                                ShellObject obj = ShellObject.FromParsingName(ExePath);
                                                mi.Header = deffappname;
                                                mi.Tag = ExePath;
                                                mi.Click += new RoutedEventHandler(miow_Click);
                                                obj.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                                obj.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                                mi.Icon = obj.Thumbnail.BitmapSource;
                                                obj.Dispose();
                                            }
                                            catch (Exception)
                                            {

                                            }

                                            btnOpenWith.Items.Add(mi);
                                        }
                                    }
                                }

                                // enable buttons
                                btnDelete.IsEnabled = (isFuncAvail || Explorer.NavigationLog.CurrentLocation.ParsingName ==
                                    KnownFolders.Libraries.ParsingName);
                                btnOpenWith.IsEnabled = (isFuncAvail &&
                                    System.IO.Path.GetExtension(SelectedItem.ParsingName) != ""
                                      && (iiii.Count > 0 || (defapp != "" && defapp != "\"%1\"")));

                                btnEdit.IsEnabled = isFuncAvail && isEditAvailable;
                                btnHistory.IsEnabled = true;

                                //set up contextual tabs

                                bool selisfolder = false;
                                bool selislib = false;

                                // Folder/Disk Tools
                                if (SelectedItem.IsFolder && SelectedItem.IsFileSystemObject)
                                {
                                    // Check for if Disk
                                    if (SelectedItem.IsDrive)
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Visible;
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    }
                                    else if (!SelectedItem.IsNetDrive)
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
                                        selisfolder = true;
                                    }
                                    else
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                        if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                            ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    }
                                }
                                else
                                {
                                    ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                    if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                }

                                // Library Tools
                                if (SelectedItem.ParsingName.Contains(KnownFolders.Libraries.ParsingName))
                                {
                                    ctgLibraries.Visibility = System.Windows.Visibility.Visible;
                                    selislib = true;
                                    ShellLibrary lib =
                                        ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
                                    IsFromSelectionOrNavigation = true;
                                    chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
                                    IsFromSelectionOrNavigation = false;
                                    foreach (ShellObject item in lib)
                                    {
                                        MenuItem miItem = new MenuItem();
                                        miItem.Header = item.GetDisplayName(DisplayNameType.Default);
                                        miItem.Tag = item;
                                        item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                        item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                        miItem.Icon = item.Thumbnail.BitmapSource;
                                        miItem.IsCheckable = true;
                                        miItem.Click += new RoutedEventHandler(miItem_Click);
                                        miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
                                        btnDefSave.Items.Add(miItem);
                                    }

                                    btnDefSave.IsEnabled = !(lib.Count == 0);
                                    lib.Close();

                                }
                                else
                                {
                                    if (!Explorer.NavigationLog.CurrentLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
                                    {
                                        btnDefSave.Items.Clear();
                                        ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
                                    }

                                }

                                //Application Tools
                                if ((System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant() == ".exe" ||
                                    System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant() == ".msi") &&
                                    !(SelectedItem.IsFolder))
                                {
                                    ctgExe.Visibility = System.Windows.Visibility.Visible;
                                    btnPin.IsChecked = WindowsAPI.IsPinnedToTaskbar(Explorer.SelectedItems[0].ParsingName);
                                    if (asApplication == true)
                                    {
                                        TheRibbon.SelectedTabItem = ctgExe.Items[0];
                                    }
                                }
                                else
                                {
                                    ctgExe.Visibility = System.Windows.Visibility.Collapsed;
                                }

                                // Archive Tools
                                if (Archives.Contains(System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant()))
                                {
                                    ctgArchive.Visibility = System.Windows.Visibility.Visible;
                                    if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    selisfolder = false;
                                    txtExtractLocation.Text = new FileInfo(SelectedItem.ParsingName).DirectoryName;
                                    SelectedArchive = SelectedItem.ParsingName;
                                    if (System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant().EndsWith(".zip"))
                                    {
                                        btnViewArchive.IsEnabled = true;
                                    }
                                    else
                                    {
                                        btnViewArchive.IsEnabled = false;
                                    }
                                    if (asArchive == true)
                                    {
                                        TheRibbon.SelectedTabItem = ctgArchive.Items[0];
                                    }
                                }
                                else
                                {
                                    ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
                                }

                                // Image Tools
                                System.Drawing.Bitmap cvt;
                                if (//SelItemsCount > 0 &&
                                    Images.Contains(System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant()))
                                {
                                    cvt = new Bitmap(SelectedItem.ParsingName);
                                    //imgdHeight.Text = cvt.Height.ToString();
                                    //imgdWidth.Text = cvt.Width.ToString();
                                    imgSizeDisplay.WidthData = cvt.Width.ToString();
                                    imgSizeDisplay.HeightData = cvt.Height.ToString();
                                    ctgImage.Visibility = System.Windows.Visibility.Visible;
                                    if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    if (asImage == true)
                                    {
                                        TheRibbon.SelectedTabItem = ctgImage.Items[0];
                                    }
                                    cvt.Dispose();
                                }
                                else
                                {
                                    ctgImage.Visibility = System.Windows.Visibility.Collapsed;
                                }
                                //LastItemSelected = SelectedItem.ParsingName;

                                // Folder/Disk Tools
                                if (Explorer.NavigationLog.CurrentLocation.IsFolder && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
                                {
                                    // Check for if Disk
                                    if (Explorer.NavigationLog.CurrentLocation.IsDrive)
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Visible;
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                        if (asDrive == true)
                                        {
                                            //TheRibbon.SelectedTabItem = ctgDrive.Items[0];
                                        }
                                    }
                                    else if (!Explorer.NavigationLog.CurrentLocation.IsNetDrive)
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                        //ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
                                    }
                                    else
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                        if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                            ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    }
                                }
                                else
                                {
                                    if (!(Explorer.SelectedItems[0].IsDrive && !Explorer.SelectedItems[0].IsNetDrive))
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    }

                                }

                                if (selisfolder == true && Explorer.SelectedItems[0].IsFolder)
                                {
                                    ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
                                    if (asFolder == true)
                                    {
                                        TheRibbon.SelectedTabItem = ctgFolderTools.Items[0];
                                    }
                                }
                                else
                                {
                                    if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                }



                                if (selislib == true)
                                {
                                    ctgLibraries.Visibility = System.Windows.Visibility.Visible;
                                    if (asLibrary == true)
                                    {
                                        TheRibbon.SelectedTabItem = ctgLibraries.Items[0];
                                    }
                                }

                            }
                            else
                            {
                                // IF MULTIPLE ITEMS ARE SELECTED
                                //MessageBox.Show(SelItemsCount.ToString() + " items selected");

                                // set variables
                                isFuncAvail = true;
                                isEditAvailable = false;

                                // set up status bar
                                sbiSelItemsCount.Content = SelItemsCount.ToString() + " items selected";

                                // enable (or disable) buttons
                                btnDelete.IsEnabled = true;
                                btnShare.IsEnabled = false;
                                btnOpenWith.IsEnabled = false;
                                btnEdit.IsEnabled = false;
                                btnHistory.IsEnabled = false;

                                // hide contextual tabs
                                ctgImage.Visibility = System.Windows.Visibility.Collapsed;
                                ctgExe.Visibility = System.Windows.Visibility.Collapsed;
                                ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
                                ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                    ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;

                                // Folder/Disk Tools
                                if (Explorer.NavigationLog.CurrentLocation.IsFolder && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
                                {
                                    // Check for if Disk
                                    if (Explorer.NavigationLog.CurrentLocation.IsDrive)
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Visible;
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    }
                                    else if (!Explorer.NavigationLog.CurrentLocation.IsNetDrive)
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                        //ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
                                    }
                                    else
                                    {
                                        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                        if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                            ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    }
                                }
                                else
                                {
                                    ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
                                    if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
                                                !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
                                        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                }

                                // if the current viewing location is a Library, show Library Tools.
                                if (inLibrary == true)
                                {
                                    ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
                                    ctgLibraries.Visibility = System.Windows.Visibility.Visible;
                                    ShellLibrary lib =
                    ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
                                    IsFromSelectionOrNavigation = true;
                                    chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
                                    IsFromSelectionOrNavigation = false;
                                    foreach (ShellObject item in lib)
                                    {
                                        MenuItem miItem = new MenuItem();
                                        miItem.Header = item.GetDisplayName(DisplayNameType.Default);
                                        miItem.Tag = item.ParsingName;
                                        item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                        item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                        miItem.Icon = item.Thumbnail.BitmapSource;
                                        miItem.IsCheckable = true;
                                        miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
                                        btnDefSave.Items.Add(miItem);
                                    }

                                    btnDefSave.IsEnabled = !(lib.Count == 0);
                                    lib.Close();
                                }
                                else
                                {
                                    if (!Explorer.NavigationLog.CurrentLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
                                    {
                                        btnDefSave.Items.Clear();
                                        ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
                                    }

                                }
                            }

                        }

                        // old code removed (if you want it, get an earlier version of this file)

                    }
                    catch (Exception)
                    {


                    }
                }
            }));
            IsSelectionRized = false;

            if (IsAfterRename)
                Explorer.ExplorerSetFocus();

        }
        bool IsFromSelectionOrNavigation = false;

        // background worker code removed. hopefully we don't need it still... Lol.

        void cbm_ClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {

            btnPaste.IsEnabled = e.DataObject.GetDataPresent(DataFormats.FileDrop);
            btnPasetShC.IsEnabled = e.DataObject.GetDataPresent(DataFormats.FileDrop);
        }

        private void TheRibbon_IsCollapsedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //MessageBox.Show("It has collapsed. " + e.NewValue, "BetterExplorer Test", MessageBoxButton.OK, MessageBoxImage.Information);
            if ((bool)e.NewValue == true)
            {
                this.edtSearchBox.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.edtSearchBox.Visibility = System.Windows.Visibility.Visible;
            }
        }

        #endregion

        #region Conditional Select

        private void miSelAllByType_Click(object sender, RoutedEventArgs e)
        {
            ShellObject FirstSelItem = Explorer.SelectedItems.Count > 0 ? Explorer.SelectedItems[0] : null;

            ShellContainer CurrentLoc = (ShellContainer)Explorer.NavigationLog.CurrentLocation;
            if (FirstSelItem != null)
            {
                string pth = Path.GetExtension(FirstSelItem.ParsingName).ToLowerInvariant();
                foreach (ShellObject item in CurrentLoc)
                {
                    if (Path.GetExtension(item.ParsingName).ToLowerInvariant() ==
                        pth)
                    {
                        Explorer.SelectItem(item);
                    }

                }
            }

            btnCondSel.IsDropDownOpen = false;
            Explorer.Focus();

        }

        private void miSelAllByDate_Click(object sender, RoutedEventArgs e)
        {
            ShellObject FirstSelItem = Explorer.SelectedItems.Count > 0 ? Explorer.SelectedItems[0] : null;

            if (FirstSelItem != null)
            {
                DateTime cd = new FileInfo(FirstSelItem.ParsingName).CreationTimeUtc;

                ShellObjectCollection CurrentLoc = ((ShellContainer)Explorer.NavigationLog.CurrentLocation).ToShellObjectCollection();

                using (ShellObjectCollection selectable = FilterByCreateDate(CurrentLoc, cd, ConditionalSelectParameters.DateFilterTypes.Equals))
                {
                    foreach (ShellObject item in selectable)
                    {
                        Explorer.SelectItem(item);
                    }
                }
            }

            btnCondSel.IsDropDownOpen = false;
            Explorer.Focus();
        }

        private void btnCondSel_Click(object sender, RoutedEventArgs e)
        {
            btnCondSel.IsDropDownOpen = false;
            Explorer.ExplorerSetFocus();
            ConditionalSelectForm csf = new ConditionalSelectForm();
            csf.ShowDialog();
            if (csf.CancelAction == false)
            {
                ConditionallySelectFiles(csf.csd);

            }
            //this.Activate();
            Explorer.ExplorerSetFocus();
        }

        private void ConditionallySelectFiles(ConditionalSelectData csd)
        {

            if (csd == null)
            {
                return;
            }

            ShellObjectCollection shells = Explorer.Items;
            ShellObjectCollection l1shells = new ShellObjectCollection();
            ShellObjectCollection l2shells = new ShellObjectCollection();
            ShellObjectCollection l3shells = new ShellObjectCollection();
            ShellObjectCollection l4shells = new ShellObjectCollection();
            ShellObjectCollection l5shells = new ShellObjectCollection();

            Explorer.DeSelectAllItems();

            if (csd.FilterByFileName == true)
            {
                foreach (ShellObject item in shells)
                {
                    if (Directory.Exists(item.ParsingName) == false)
                    {
                        FileInfo data = new FileInfo(item.ParsingName);
                        if (csd.FileNameData.matchCase == true)
                        {
                            switch (csd.FileNameData.filter)
                            {
                                case ConditionalSelectParameters.FileNameFilterTypes.Contains:
                                    if (data.Name.Contains(csd.FileNameData.query) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.StartsWith:
                                    if (data.Name.StartsWith(csd.FileNameData.query) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.EndsWith:
                                    if (data.Name.EndsWith(csd.FileNameData.query) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.Equals:
                                    if (data.Name == csd.FileNameData.query)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.DoesNotContain:
                                    if (data.Name.Contains(csd.FileNameData.query) == false)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.NotEqualTo:
                                    if (data.Name != csd.FileNameData.query)
                                        l1shells.Add(item);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            switch (csd.FileNameData.filter)
                            {
                                case ConditionalSelectParameters.FileNameFilterTypes.Contains:
                                    if (data.Name.ToLower().Contains(csd.FileNameData.query.ToLower()) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.StartsWith:
                                    if (data.Name.ToLower().StartsWith(csd.FileNameData.query.ToLower()) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.EndsWith:
                                    if (data.Name.ToLower().EndsWith(csd.FileNameData.query.ToLower()) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.Equals:
                                    if (data.Name.ToLower() == csd.FileNameData.query.ToLower())
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.DoesNotContain:
                                    if (data.Name.ToLower().Contains(csd.FileNameData.query.ToLower()) == false)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.NotEqualTo:
                                    if (data.Name.ToLower() != csd.FileNameData.query.ToLower())
                                        l1shells.Add(item);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (ShellObject item in shells)
                {
                    if (Directory.Exists(item.ParsingName) == false)
                    {
                        l1shells.Add(item);
                    }
                }
            }

            if (csd.FilterByFileSize == true)
            {
                foreach (ShellObject item in l1shells)
                {
                    FileInfo data = new FileInfo(item.ParsingName);
                    switch (csd.FileSizeData.filter)
                    {
                        case ConditionalSelectParameters.FileSizeFilterTypes.LargerThan:
                            if (data.Length > csd.FileSizeData.query1)
                                l2shells.Add(item);
                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.SmallerThan:
                            if (data.Length < csd.FileSizeData.query1)
                                l2shells.Add(item);
                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.Equals:
                            if (data.Length == csd.FileSizeData.query1)
                                l2shells.Add(item);
                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.Between:
                            long smallbound;
                            long largebound;
                            if (csd.FileSizeData.query2 > csd.FileSizeData.query1)
                            {
                                smallbound = csd.FileSizeData.query1;
                                largebound = csd.FileSizeData.query2;
                            }
                            else
                            {
                                if (csd.FileSizeData.query2 < csd.FileSizeData.query1)
                                {
                                    smallbound = csd.FileSizeData.query2;
                                    largebound = csd.FileSizeData.query1;
                                }
                                else
                                {
                                    // they are the same, use Equal code

                                    //MessageBox.Show("Could not continue Conditional Select operation. When searching for files that is between a certain size, make sure the upper and lower bounds of the search parameters are not the same.", "BetterExplorer Conditional Select - Filter by File Size", MessageBoxButton.OK, MessageBoxImage.Error);
                                    //return;

                                    if (data.Length == csd.FileSizeData.query1)
                                        l2shells.Add(item);
                                    break;
                                }
                            }

                            if (data.Length > smallbound)
                                if (data.Length < largebound)
                                    l2shells.Add(item);

                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.NotEqualTo:
                            if (data.Length != csd.FileSizeData.query1)
                                l2shells.Add(item);
                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.NotBetween:
                            long smallbound2;
                            long largebound2;
                            if (csd.FileSizeData.query2 > csd.FileSizeData.query1)
                            {
                                smallbound2 = csd.FileSizeData.query1;
                                largebound2 = csd.FileSizeData.query2;
                            }
                            else
                            {
                                if (csd.FileSizeData.query2 < csd.FileSizeData.query1)
                                {
                                    smallbound2 = csd.FileSizeData.query2;
                                    largebound2 = csd.FileSizeData.query1;
                                }
                                else
                                {
                                    // they are the same, use Unequal code
                                    if (data.Length != csd.FileSizeData.query1)
                                        l2shells.Add(item);
                                    break;
                                    //MessageBox.Show("Could not continue Conditional Select operation. When searching for files that is not between a certain size, make sure the upper and lower bounds of the search parameters are not the same.", "BetterExplorer Conditional Select - Filter by File Size", MessageBoxButton.OK, MessageBoxImage.Error);
                                    //return;
                                }
                            }

                            if (data.Length < smallbound2 || data.Length > largebound2)
                                l2shells.Add(item);

                            break;
                        default:
                            break;
                    }

                }
            }
            else
            {
                foreach (ShellObject item in l1shells)
                {
                    l2shells.Add(item);
                }
            }

            if (csd.FilterByDateCreated == true)
            {
                foreach (ShellObject item in FilterByCreateDate(l2shells, csd.DateCreatedData.queryDate, csd.DateCreatedData.filter))
                {
                    l3shells.Add(item);
                }
            }
            else
            {
                foreach (ShellObject item in l2shells)
                {
                    l3shells.Add(item);
                }
            }

            if (csd.FilterByDateModified == true)
            {
                foreach (ShellObject item in FilterByWriteDate(l3shells, csd.DateModifiedData.queryDate, csd.DateModifiedData.filter))
                {
                    l4shells.Add(item);
                }
            }
            else
            {
                foreach (ShellObject item in l2shells)
                {
                    l4shells.Add(item);
                }
            }

            if (csd.FilterByDateAccessed == true)
            {
                foreach (ShellObject item in FilterByAccessDate(l4shells, csd.DateAccessedData.queryDate, csd.DateAccessedData.filter))
                {
                    l5shells.Add(item);
                }
            }
            else
            {
                foreach (ShellObject item in l4shells)
                {
                    l5shells.Add(item);
                }
            }

            List<ShellObject> sel = new List<ShellObject>();

            foreach (ShellObject item in l5shells)
            {
                // this is where the code should be to select multiple files.
                //
                // However, the Explorer control does not support selecting multiple files.

                Explorer.SelectItem(item);
            }

            Explorer.Focus();

        }

        public ShellObjectCollection FilterByCreateDate(ShellObjectCollection shells, DateTime datetocompare, ConditionalSelectParameters.DateFilterTypes filter)
        {
            ShellObjectCollection outshells = new ShellObjectCollection();

            foreach (ShellObject item in shells)
            {
                FileInfo data = new FileInfo(item.ParsingName);
                switch (filter)
                {
                    case ConditionalSelectParameters.DateFilterTypes.EarlierThan:
                        if (DateTime.Compare(data.CreationTimeUtc.Date, datetocompare.Date) < 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.LaterThan:
                        if (DateTime.Compare(data.CreationTimeUtc.Date, datetocompare.Date) > 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.Equals:
                        if (DateTime.Compare(data.CreationTimeUtc.Date, datetocompare.Date) == 0)
                            outshells.Add(item);
                        break;
                    default:
                        break;
                }
            }

            return outshells;
        }

        public ShellObjectCollection FilterByWriteDate(ShellObjectCollection shells, DateTime datetocompare, ConditionalSelectParameters.DateFilterTypes filter)
        {
            ShellObjectCollection outshells = new ShellObjectCollection();

            foreach (ShellObject item in shells)
            {
                FileInfo data = new FileInfo(item.ParsingName);
                switch (filter)
                {
                    case ConditionalSelectParameters.DateFilterTypes.EarlierThan:
                        if (DateTime.Compare(data.LastWriteTimeUtc.Date, datetocompare) < 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.LaterThan:
                        if (DateTime.Compare(data.LastWriteTimeUtc.Date, datetocompare) > 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.Equals:
                        if (DateTime.Compare(data.LastWriteTimeUtc.Date, datetocompare) == 0)
                            outshells.Add(item);
                        break;
                    default:
                        break;
                }
            }

            return outshells;
        }

        public ShellObjectCollection FilterByAccessDate(ShellObjectCollection shells, DateTime datetocompare, ConditionalSelectParameters.DateFilterTypes filter)
        {
            ShellObjectCollection outshells = new ShellObjectCollection();

            foreach (ShellObject item in shells)
            {
                FileInfo data = new FileInfo(item.ParsingName);
                switch (filter)
                {
                    case ConditionalSelectParameters.DateFilterTypes.EarlierThan:
                        if (DateTime.Compare(data.LastAccessTimeUtc.Date, datetocompare) < 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.LaterThan:
                        if (DateTime.Compare(data.LastAccessTimeUtc.Date, datetocompare) > 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.Equals:
                        if (DateTime.Compare(data.LastAccessTimeUtc.Date, datetocompare) == 0)
                            outshells.Add(item);
                        break;
                    default:
                        break;
                }
            }

            return outshells;
        }

        #endregion

        #region ClickEvents

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            CheckForUpdate();
            backstage.IsOpen = true;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/DimitarCC/BetterExplorer");

        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            Explorer.ShowPropPage(this.Handle, Explorer.SelectedItems[0].ParsingName, "Previous Versions");
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // create new window
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            //BetterExplorer.MainWindow g = new MainWindow();
            //g.Show();
          var k = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Process.Start(k, "/nw");
        }


        void miow_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            Process.Start(item.Tag.ToString(), "\"" + Explorer.SelectedItems[0].ParsingName + "\"");
        }

        void mif_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            ShellObject Path = ShellObject.FromParsingName(item.Tag.ToString());
            Explorer.Navigate(Path);
            Path.Dispose();

        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.Specialized.StringCollection sc = new System.Collections.Specialized.StringCollection();
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                sc.Add(item.ParsingName);
            }
            System.Windows.Forms.Clipboard.SetFileDropList(sc);
            Explorer.IsMoveClipboardOperation = false;
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
            DropData PasteData = new DropData();
            PasteData.DropList = System.Windows.Forms.Clipboard.GetFileDropList();
            if (Explorer.SelectedItems.Count > 0 & Explorer.SelectedItems.Count < 2)
            {
                PasteData.PathForDrop = Explorer.SelectedItems[0].ParsingName;

            }
            else
            {
                PasteData.PathForDrop = Explorer.NavigationLog.CurrentLocation.ParsingName;
            }
            AddToLog("Files Pasted at " + PasteData.PathForDrop + " Files pasted: " + PasteData.DropList.ToString());
            Thread t = null;
            if (Explorer.IsMoveClipboardOperation)
            {
                t = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
            }
            else
            {
                t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
            }
            t.SetApartmentState(ApartmentState.STA);
            t.Start(PasteData);
        }

        private void SplitButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender, e);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            IsAfterRename = false;
            Explorer.DoRename();

        }

        private void btnPathCopy_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 1)
            {
                string path = null;
                foreach (ShellObject item in Explorer.SelectedItems)
                {
                    //This way i think is better of making multiple line in .Net ;)
                    if (string.IsNullOrEmpty(path))
                    {
                        path = item.ParsingName;
                    }
                    else
                    {
                        path = path + "\r\n" + item.ParsingName;
                    }

                }
                Clipboards.SetText(path);
            }
            else if (Explorer.SelectedItems.Count == 1)
            {
                Clipboards.SetText(Explorer.SelectedItems[0].ParsingName);
            }
            else
            {
                Clipboards.SetText(Explorer.NavigationLog.CurrentLocation.ParsingName);
            }

        }

        private void btnSelAll_Click(object sender, RoutedEventArgs e)
        {
            Explorer.SelectAllItems();
        }

        private void btnSelNone_Click(object sender, RoutedEventArgs e)
        {
            Explorer.DeSelectAllItems();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //KeepFocusOnExplorer = true;
            Explorer.DeleteToRB();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            //KeepFocusOnExplorer = true;
            Thread MoveThread = new Thread(new ParameterizedThreadStart(Explorer.DoDelete));
            MoveThread.SetApartmentState(ApartmentState.STA);
            MoveThread.Start(Explorer.SelectedItems);
        }

        string LastPath = "";
        public bool IsCompartibleRename = false;
        public bool IsAfterRename = false;


        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string path = "";

            bool IsLib = false;
            if (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.Libraries.ParsingName)
            {
                path = Explorer.CreateNewLibrary(FindResource("btnNewLibraryCP").ToString());
                AddToLog("Library Created");
                IsLib = true;
            }
            else
            {
                path = Explorer.CreateNewFolder(FindResource("btnNewFolderCP").ToString());
                AddToLog("Folder Created In " + Explorer.NavigationLog.CurrentLocation.ParsingName);
            }

            LastPath = path;

            if (IsCompartibleRename)
            {
                Explorer.RefreshExplorer();

            }
            else
            {
                WindowsAPI.SHChangeNotify(WindowsAPI.HChangeNotifyEventID.SHCNE_MKDIR,
                WindowsAPI.HChangeNotifyFlags.SHCNF_PATHW | WindowsAPI.HChangeNotifyFlags.SHCNF_FLUSHNOWAIT,
                  Marshal.StringToHGlobalAuto(path.Replace(@"\\", @"\")), IntPtr.Zero);
            }


            IsAfterRename = false;
            Explorer.DoRename(LastPath.Replace(@"\\", @"\"), IsLib);



            IsLibW = IsLib;
            IsAfterFolderCreate = true;


            //Explorer.Focus();

        }


        private void btnProperties_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 0)
            {
                Explorer.ShowFileProperties();
            }
            else
            {
                Explorer.ShowFileProperties(Explorer.NavigationLog.CurrentLocation.ParsingName);
            }
            Explorer.Focus();
        }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern int RegisterHotKey(IntPtr hwnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int UnregisterHotKey(IntPtr hwnd, int id);

        //short mHotKeyId = 0;

        private void btnInvSel_Click(object sender, RoutedEventArgs e)
        {
            Explorer.InvertSelection();
            //WindowsAPI.SendMessage((int)Explorer.Handle, 0x0111, 0x7013, 0);
        }

        private void btnPasetShC_Click(object sender, RoutedEventArgs e)
        {

            StringCollection DropList = System.Windows.Forms.Clipboard.GetFileDropList();
            string PathForDrop = Explorer.NavigationLog.CurrentLocation.ParsingName;
            foreach (string item in DropList)
            {
                using (ShellLink shortcut = new ShellLink())
                {
                    ShellObject o = ShellObject.FromParsingName(item);
                    shortcut.Target = item;
                    shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(item);
                    shortcut.Description = o.GetDisplayName(DisplayNameType.Default);
                    shortcut.DisplayMode = ShellLink.LinkDisplayMode.edmNormal;
                    shortcut.Save(PathForDrop + "\\" + o.GetDisplayName(DisplayNameType.Default) + ".lnk");
                    AddToLog("Shortcut Created at " + PathForDrop + "\\" + o.GetDisplayName(DisplayNameType.Default) + " from source " + item);
                    o.Dispose();
                }
            }

        }

        private void btnctDocuments_Click(object sender, RoutedEventArgs e)
        {
            DropData dd = new DropData();
            dd.Shellobjects = Explorer.SelectedItems;
            dd.PathForDrop = KnownFolders.Documents.ParsingName;
            Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
            CopyThread.SetApartmentState(ApartmentState.STA);
            CopyThread.Start(dd);
            AddToLog("Files Copied to " + dd.PathForDrop + " Files copied: " + PathStringCombiner.CombinePaths(dd.Shellobjects.ToList()));
        }

        private void btnctDesktop_Click(object sender, RoutedEventArgs e)
        {
            DropData dd = new DropData();
            dd.Shellobjects = Explorer.SelectedItems;
            dd.PathForDrop = KnownFolders.Desktop.ParsingName;
            Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
            CopyThread.SetApartmentState(ApartmentState.STA);
            CopyThread.Start(dd);
            AddToLog("Files Copied to " + dd.PathForDrop + " Files copied: " + PathStringCombiner.CombinePaths(dd.Shellobjects.ToList()));
        }

        private void btnctDounloads_Click(object sender, RoutedEventArgs e)
        {
            DropData dd = new DropData();
            dd.Shellobjects = Explorer.SelectedItems;
            dd.PathForDrop = KnownFolders.Downloads.ParsingName;
            Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
            CopyThread.SetApartmentState(ApartmentState.STA);
            CopyThread.Start(dd);
            AddToLog("Files Copied to " + dd.PathForDrop + " Files copied: " + PathStringCombiner.CombinePaths(dd.Shellobjects.ToList()));
        }

        private void btnmtDocuments_Click(object sender, RoutedEventArgs e)
        {
            DropData dd = new DropData();
            dd.Shellobjects = Explorer.SelectedItems;
            dd.PathForDrop = KnownFolders.Documents.ParsingName;
            Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
            CopyThread.SetApartmentState(ApartmentState.STA);
            CopyThread.Start(dd);
            AddToLog("Files Moved to " + dd.PathForDrop + " Files moved: " + PathStringCombiner.CombinePaths(dd.Shellobjects.ToList()));
        }

        private void btnmtDesktop_Click(object sender, RoutedEventArgs e)
        {
            DropData dd = new DropData();
            dd.Shellobjects = Explorer.SelectedItems;
            dd.PathForDrop = KnownFolders.Desktop.ParsingName;
            Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
            CopyThread.SetApartmentState(ApartmentState.STA);
            CopyThread.Start(dd);
            AddToLog("Files Moved to " + dd.PathForDrop + " Files moved: " + PathStringCombiner.CombinePaths(dd.Shellobjects.ToList()));
        }

        private void btnmtDounloads_Click(object sender, RoutedEventArgs e)
        {
            DropData dd = new DropData();
            dd.Shellobjects = Explorer.SelectedItems;
            dd.PathForDrop = KnownFolders.Downloads.ParsingName;
            Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
            CopyThread.SetApartmentState(ApartmentState.STA);
            CopyThread.Start(dd);
            AddToLog("Files Moved to " + dd.PathForDrop + " Files moved: " + PathStringCombiner.CombinePaths(dd.Shellobjects.ToList()));
        }

        private void btnmtOther_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DropData dd = new DropData();
                dd.Shellobjects = Explorer.SelectedItems;
                dd.PathForDrop = dlg.FileName;
                Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
                CopyThread.SetApartmentState(ApartmentState.STA);
                CopyThread.Start(dd);
                AddToLog("Files Moved to " + dd.PathForDrop + " Files moved: " + PathStringCombiner.CombinePaths(dd.Shellobjects.ToList()));
            }
        }

        private void btnctOther_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DropData dd = new DropData();
                dd.Shellobjects = Explorer.SelectedItems;
                dd.PathForDrop = dlg.FileName;
                Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                CopyThread.SetApartmentState(ApartmentState.STA);
                CopyThread.Start(dd);
                AddToLog("Files Copied to " + dd.PathForDrop + " Files copied: " + PathStringCombiner.CombinePaths(dd.Shellobjects.ToList()));
            }
            Explorer.Focus();
        }

        private void btnCopyto_Click(object sender, RoutedEventArgs e)
        {
            btnctOther_Click(sender, e);
        }

        private void btnMoveto_Click(object sender, RoutedEventArgs e)
        {
            btnmtOther_Click(sender, e);
        }

        private void btnCut_Click(object sender, RoutedEventArgs e)
        {
            AddToLog("Files cut: " + PathStringCombiner.CombinePaths(Explorer.SelectedItems.ToList(), false));
            Explorer.DoCut();
        }

        private void btnNewItem_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE() { fShowAllObjects = 0 };
            WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWALLOBJECTS, true);
        }

        private void btnOpenWith_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("\"" + Explorer.SelectedItems[0].ParsingName + "\"");
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            int indexp = EditComm.IndexOf("/");
            string substr = "";
            string main = "";
            if (indexp != -1)
            {
                substr = EditComm.Substring(indexp);
                main = EditComm.Substring(0, indexp);
            }
            else
            {
                main = EditComm.Substring(0, EditComm.IndexOf("%1"));
                substr = "%1";
            }
            Process.Start(main.TrimEnd(), substr.Replace("%1", String.Format("\"{0}\"", Explorer.SelectedItems[0].ParsingName)));
        }

        private void btnFormatDrive_Click(object sender, RoutedEventArgs e)
        {
            Explorer.FormatDrive();
        }

        private void btnCleanDrive_Click(object sender, RoutedEventArgs e)
        {
            Explorer.CleanupDrive();
        }

        private void btnDefragDrive_Click(object sender, RoutedEventArgs e)
        {
            Explorer.DefragDrive();
        }

        private void btnRunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            Explorer.RunExeAsAdmin(Explorer.SelectedItems[0].ParsingName);
        }

        private void btnPin_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.PinUnpinToTaskbar(Explorer.SelectedItems[0].ParsingName);
        }

        private void btnFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.GetSelectedItemsCount() == 1)
            {
                ShellLink link = new ShellLink();
                link.DisplayMode = ShellLink.LinkDisplayMode.edmNormal;
                link.Target = Explorer.SelectedItems[0].ParsingName;
                link.Save(KnownFolders.Links.ParsingName + @"\" +
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default) + ".lnk");
                link.Dispose();
            }

            if (Explorer.GetSelectedItemsCount() == 0)
            {
                ShellLink link = new ShellLink();
                link.DisplayMode = ShellLink.LinkDisplayMode.edmNormal;
                link.Target = Explorer.NavigationLog.CurrentLocation.ParsingName;
                link.Save(KnownFolders.Links.ParsingName + @"\" +
                    Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default) + ".lnk");
                link.Dispose();
            }

        }

        #endregion

        #region Tabs

        private void ConstructMoveToCopyToMenu()
        {
            btnMoveto.Items.Clear();
            btnCopyto.Items.Clear();
            MenuItem OtherLocationMove = new MenuItem();
            OtherLocationMove.Header = FindResource("miOtherDestCP");
            OtherLocationMove.Click += new RoutedEventHandler(btnmtOther_Click);
            MenuItem OtherLocationCopy = new MenuItem();
            OtherLocationCopy.Header = FindResource("miOtherDestCP");
            OtherLocationCopy.Click += new RoutedEventHandler(btnctOther_Click);

            MenuItem mimDesktop = new MenuItem();
            ShellObject sod = (ShellObject)KnownFolders.Desktop;
            sod.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            sod.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            mimDesktop.Icon = sod.Thumbnail.BitmapSource;
            mimDesktop.Header = FindResource("btnctDesktopCP");
            mimDesktop.Click += new RoutedEventHandler(btnmtDesktop_Click);
            MenuItem micDesktop = new MenuItem();
            micDesktop.Icon = sod.Thumbnail.BitmapSource;
            micDesktop.Header = FindResource("btnctDesktopCP");
            micDesktop.Click += new RoutedEventHandler(btnctDesktop_Click);
            sod.Dispose();

            MenuItem mimDocuments = new MenuItem();
            ShellObject sodc = (ShellObject)KnownFolders.Documents;
            sodc.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            sodc.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            mimDocuments.Icon = sodc.Thumbnail.BitmapSource;
            mimDocuments.Header = FindResource("btnctDocumentsCP");
            mimDocuments.Click += new RoutedEventHandler(btnmtDocuments_Click);
            MenuItem micDocuments = new MenuItem();
            micDocuments.Icon = sodc.Thumbnail.BitmapSource;
            micDocuments.Header = FindResource("btnctDocumentsCP");
            micDocuments.Click += new RoutedEventHandler(btnctDocuments_Click);
            sodc.Dispose();

            MenuItem mimDownloads = new MenuItem();
            ShellObject sodd = (ShellObject)KnownFolders.Downloads;
            sodd.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            sodd.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            mimDownloads.Icon = sodd.Thumbnail.BitmapSource;
            mimDownloads.Header = FindResource("btnctDownloadsCP");
            mimDownloads.Click += new RoutedEventHandler(btnmtDounloads_Click);
            MenuItem micDownloads = new MenuItem();
            micDownloads.Icon = sodd.Thumbnail.BitmapSource;
            micDownloads.Header = FindResource("btnctDownloadsCP");
            micDownloads.Click += new RoutedEventHandler(btnctDounloads_Click);
            sodd.Dispose();

            btnMoveto.Items.Add(mimDocuments);
            btnMoveto.Items.Add(mimDownloads);
            btnMoveto.Items.Add(mimDesktop);
            btnMoveto.Items.Add(new Separator());

            btnCopyto.Items.Add(micDocuments);
            btnCopyto.Items.Add(micDownloads);
            btnCopyto.Items.Add(micDesktop);
            btnCopyto.Items.Add(new Separator());

            foreach (CloseableTabItem item in tabControl1.Items)
            {
                bool IsAdditem = true;
                foreach (object mii in btnCopyto.Items)
                {
                    if (mii is MenuItem)
                    {
                        if ((mii as MenuItem).Tag != null)
                        {
                            if (((mii as MenuItem).Tag as ShellObject) == item.Path)
                            {
                                IsAdditem = false;
                                break;
                            }
                        }
                    }
                }
                if (IsAdditem && item.Path.IsFileSystemObject)
                {
                    try
                    {
                        MenuItem mim = new MenuItem();
                        mim.Header = item.Path.GetDisplayName(DisplayNameType.Default);
                        mim.Tag = item.Path;
                        ShellObject so = ShellObject.FromParsingName(item.Path.ParsingName);
                        so.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                        so.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                        mim.Icon = so.Thumbnail.BitmapSource;
                        mim.Click += new RoutedEventHandler(mim_Click);
                        btnMoveto.Items.Add(mim);
                        MenuItem mic = new MenuItem();
                        mic.Header = item.Path.GetDisplayName(DisplayNameType.Default);
                        mic.Tag = item.Path;
                        mic.Icon = so.Thumbnail.BitmapSource;
                        mic.Click += new RoutedEventHandler(mimc_Click);
                        btnCopyto.Items.Add(mic);
                        so.Dispose();
                    }
                    catch
                    {
                        //Do nothing if ShellObject is not available anymore and close the problematic item
                        CloseTab(item);
                    }
                }
            }
            btnMoveto.Items.Add(new Separator());
            btnMoveto.Items.Add(OtherLocationMove);
            btnCopyto.Items.Add(new Separator());
            btnCopyto.Items.Add(OtherLocationCopy);

        }

        void mimc_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            DropData dd = new DropData();
            dd.Shellobjects = Explorer.SelectedItems;
            dd.PathForDrop = (mi.Tag as ShellObject).ParsingName;
            Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
            CopyThread.SetApartmentState(ApartmentState.STA);
            CopyThread.Start(dd);
        }

        void mim_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            DropData dd = new DropData();
            dd.Shellobjects = Explorer.SelectedItems;
            dd.PathForDrop = (mi.Tag as ShellObject).ParsingName;
            Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
            CopyThread.SetApartmentState(ApartmentState.STA);
            CopyThread.Start(dd);
        }

        private void btnNewTab_Click(object sender, RoutedEventArgs e)
        {
            NewTab();
        }

        private void btnTabClone_Click(object sender, RoutedEventArgs e)
        {
            CloneTab(tabControl1.Items[CurrentTabIndex] as CloseableTabItem);
        }

        private void btnTabCloseC_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl1.SelectedIndex == 0 && tabControl1.Items.Count == 1)
            {
                Close();
                return;
            }
            int CurSelIndex = tabControl1.SelectedIndex;
            if (tabControl1.SelectedIndex == 0)
            {
                tabControl1.SelectedItem = tabControl1.Items[1];
                CurrentTabIndex = 0;
            }
            else
            {
                tabControl1.SelectedItem = tabControl1.Items[CurSelIndex - 1];
                CurrentTabIndex = CurSelIndex - 1;
            }


            tabControl1.Items.RemoveAt(CurSelIndex);

        }

        //'close tab
        void cti_CloseTab(object sender, RoutedEventArgs e)
        {
            CloseableTabItem curItem = e.Source as CloseableTabItem;

            CloseTab(curItem);
        }

        public void CloneTab(CloseableTabItem CurTab)
        {
            CloseableTabItem newt = new CloseableTabItem();
            CreateTabbarRKMenu(newt);
            newt.Header = CurTab.Header;
            newt.TabIcon = CurTab.TabIcon;
            newt.Path = CurTab.Path;
            newt.Index = tabControl1.Items.Count;
            newt.CloseTab += new RoutedEventHandler(newt_CloseTab);
            newt.DragEnter += new DragEventHandler(newt_DragEnter);
            newt.DragLeave += new DragEventHandler(newt_DragLeave);
            newt.DragOver += new DragEventHandler(newt_DragOver);
            newt.Drop += new DragEventHandler(newt_Drop);
            newt.AllowDrop = true;
            newt.log.CurrentLocation = CurTab.Path;
            tabControl1.Items.Add(newt);
            tabControl1.SelectedIndex = tabControl1.Items.Count - 1;
            tabControl1.SelectedItem = tabControl1.Items[tabControl1.Items.Count - 1];
            LastTabIndex = CurrentTabIndex;
            CurrentTabIndex = tabControl1.Items.Count - 1;
            ConstructMoveToCopyToMenu();
        }

        private void ERNewTab(object sender, ExecutedRoutedEventArgs e)
        {
            NewTab();
        }

        void SelectTab(int Index)
        {
            int selIndex = 0;
            if (CurrentTabIndex == tabControl1.Items.Count - 1)
            {
                selIndex = 0;
            }
            else
            {
                selIndex = Index;
            }
            tabControl1.SelectedItem = tabControl1.Items[selIndex];
            CurrentTabIndex = selIndex;
        }

        void CloseTab(CloseableTabItem thetab)
        {
            if (thetab.Index == 0 && tabControl1.Items.Count == 1)
            {
                Close();
                return;
            }

            int CurSelIndex = thetab.Index;
            if (CurSelIndex == 0)
            {
                if (LastTabIndex != -1)
                {
                    try
                    {
                        tabControl1.SelectedItem = tabControl1.Items[LastTabIndex];
                        CurrentTabIndex = LastTabIndex;
                    }
                    catch
                    {

                        tabControl1.SelectedItem = tabControl1.Items[tabControl1.Items.Count - 1];
                        CurrentTabIndex = tabControl1.Items.Count - 1;
                    }
                }
                else
                {
                    tabControl1.SelectedItem = tabControl1.Items[1];
                    CurrentTabIndex = 0;
                }

            }
            else
            {
                if (CurSelIndex == tabControl1.Items.Count - 1)
                {
                    if (CurSelIndex == LastTabIndex)
                    {
                        if (BeforeLastTabIndex != -1 && tabControl1.Items.Count >= BeforeLastTabIndex + 1)
                        {
                            tabControl1.SelectedItem = tabControl1.Items[BeforeLastTabIndex];
                            CurrentTabIndex = BeforeLastTabIndex;
                        }
                        else
                        {
                            tabControl1.SelectedItem = tabControl1.Items[CurSelIndex - 1];
                            CurrentTabIndex = CurSelIndex - 1;
                            LastTabIndex = CurrentTabIndex;
                        }

                    }
                    else
                    {
                        if (LastTabIndex != -1)
                        {
                            try
                            {
                                tabControl1.SelectedItem = tabControl1.Items[LastTabIndex];
                                CurrentTabIndex = LastTabIndex;
                            }
                            catch
                            {
                                 
                               //Handle the exeption on using Close all tabs command since we dont need to keep any index just to close the app
                            }
                        }
                    }

                }
                else
                {
                    if (CurSelIndex == LastTabIndex)
                    {
                        if (BeforeLastTabIndex != -1 && tabControl1.Items.Count >= BeforeLastTabIndex + 1)
                        {
                            tabControl1.SelectedItem = tabControl1.Items[BeforeLastTabIndex];
                            CurrentTabIndex = BeforeLastTabIndex;
                        }
                        else
                        {
                            tabControl1.SelectedItem = tabControl1.Items[CurSelIndex + 1];
                            CurrentTabIndex = CurSelIndex + 1;
                            LastTabIndex = CurrentTabIndex;
                        }
                    }
                    else
                    {
                        if (LastTabIndex != -1)
                        {
                            try
                            {
                                tabControl1.SelectedItem = tabControl1.Items[LastTabIndex];
                                CurrentTabIndex = LastTabIndex;
                            }
                            catch
                            {
                                if (LastTabIndex != 0)
                                {
                                    tabControl1.SelectedItem = tabControl1.Items[tabControl1.Items.Count - 1];
                                    CurrentTabIndex = tabControl1.Items.Count - 1;
                                }
                                else
                                {
                                    tabControl1.SelectedItem = tabControl1.Items[0];
                                    CurrentTabIndex = 0;
                                }

                            }
                        }
                    }
                }
            }

            for (int i = CurSelIndex + 1; i < tabControl1.Items.Count; i++)
            {
                CloseableTabItem tab = tabControl1.Items[i] as CloseableTabItem;
                tab.Index = tab.Index - 1;
            }
            try
            {
                tabControl1.Items.RemoveAt(CurSelIndex);
            }
            catch
            {
                tabControl1.Items.RemoveAt(CurSelIndex - 1);
            }
            ConstructMoveToCopyToMenu();
        }
        void newt_CloseTab(object sender, RoutedEventArgs e)
        {
            CloseableTabItem curItem = e.Source as CloseableTabItem;
            CloseTab(curItem);
        }

        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (e.AddedItems.Count > 0)
            {

                CloseableTabItem itb = e.AddedItems[0] as CloseableTabItem;

                isGoingBackOrForward = itb.log.HistoryItemsList.Count != 0;
                if (itb != null)
                {

                        BeforeLastTabIndex = LastTabIndex;

                        tabControl1.SelectedIndex = itb.Index;
                        LastTabIndex = tabControl1.SelectedIndex;
                        CurrentTabIndex = itb.Index;
                        if (itb.Path != Explorer.NavigationLog.CurrentLocation)
                        {


                            if (!Keyboard.IsKeyDown(Key.Tab))
                            {
                                Explorer.Navigate(itb.Path);
                            }
                            else
                            {
                                t.Interval = 500;
                                t.Tag = itb.Path;
                                t.Tick += new EventHandler(t_Tick);
                                t.Start();
                            }

                        }
                    //'btnTabCloseC.IsEnabled = tabControl1.Items.Count > 1;
                    //'there's a bug that has this enabled when there's only one tab open, but why disable it
                    //'if it never crashes the program? Closing the last tab simply closes the program, so I
                    //'thought, what the heck... let's just keep it enabled. :) -JaykeBird
                }
            }

            Explorer.ExplorerSetFocus();
            Explorer.Focus();
        }

        void t_Tick(object sender, EventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.Tab))
            {

                Explorer.Navigate((sender as System.Windows.Forms.Timer).Tag as ShellObject);
                WindowsAPI.SetFocus(ExplorerBrowser.SysListViewHandle);
                (sender as System.Windows.Forms.Timer).Stop();
            }
        }

        private void tabControl1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                CloneTab(tabControl1.SelectedItem as CloseableTabItem);
            }
        }

        private void RCloseTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (tabControl1.SelectedIndex == 0 && tabControl1.Items.Count == 1)
            {
                Close();
                return;
            }
            int CurSelIndex = tabControl1.SelectedIndex;
            if (tabControl1.SelectedIndex == 0)
            {
                tabControl1.SelectedItem = tabControl1.Items[1];
                CurrentTabIndex = 0;
            }
            else
            {
                tabControl1.SelectedItem = tabControl1.Items[CurSelIndex - 1];
                CurrentTabIndex = CurSelIndex - 1;
            }


            tabControl1.Items.RemoveAt(CurSelIndex);
        }

        private void ChangeTab(object sender, ExecutedRoutedEventArgs e)
        {
            t.Stop();
            SelectTab(CurrentTabIndex + 1);
        }

        private void ERGoToBCCombo(object sender, ExecutedRoutedEventArgs e)
        {
            breadcrumbBarControl1.EnterEditMode();
        }


        void CreateTabbarRKMenu(CloseableTabItem tabitem)
        {
            tabitem.mnu = new ContextMenu();
            MenuItem miclosecurrentr = new MenuItem();
            miclosecurrentr.Header = "Close current tab";
            miclosecurrentr.Tag = tabitem;
            miclosecurrentr.Click += new RoutedEventHandler(miclosecurrentr_Click);
            tabitem.mnu.Items.Add(miclosecurrentr);

            MenuItem miclosealltab = new MenuItem();
            miclosealltab.Header = "Close all tabs";
            miclosealltab.Click += new RoutedEventHandler(miclosealltab_Click);
            tabitem.mnu.Items.Add(miclosealltab);

            MenuItem miclosealltabbd = new MenuItem();
            miclosealltabbd.Header = "Close all other tabs";
            miclosealltabbd.Tag = tabitem;
            miclosealltabbd.Click += new RoutedEventHandler(miclosealltabbd_Click);
            tabitem.mnu.Items.Add(miclosealltabbd);

            //tabitem.mnu.Items.Add(new Separator());

            MenuItem miopeninnew = new MenuItem();
            miopeninnew.Header = "Open in new window";
            miopeninnew.Tag = tabitem;
            miopeninnew.Click += new RoutedEventHandler(miopeninnew_Click);
            //tabitem.mnu.Items.Add(miopeninnew);

        }

        void miopeninnew_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
            CloseableTabItem ti = mi.Tag as CloseableTabItem;
            Process.Start(Assembly.GetExecutingAssembly().GetName().Name, ti.Path.ParsingName + " /nw");
            CloseTab(ti);
            //throw new NotImplementedException();
        }

        void miclosealltabbd_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
            CloseableTabItem ti = mi.Tag as CloseableTabItem;
            CloseAllTabsButThis(ti);
        }

        void miclosealltab_Click(object sender, RoutedEventArgs e)
        {
            CloseAllTabs(true);
        }

        void miclosecurrentr_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
            CloseableTabItem ti = mi.Tag as CloseableTabItem;
            CloseTab(ti);
        }

        void CloseAllTabs(bool CloseFirstTab)
        {
            List<CloseableTabItem> tabs = new List<CloseableTabItem>();
            foreach (object item in tabControl1.Items)
            {
                CloseableTabItem it = item as CloseableTabItem;
                tabs.Add(it);
            }

            if (CloseFirstTab)
            {
                foreach (CloseableTabItem item in tabs)
                {

                    CloseTab(item);
                }
            }
            else
            {
                foreach (CloseableTabItem item in tabs)
                {

                    if (item.Index != 0)
                    {
                        CloseTab(item);
                    }

                }
            }

            tabs = null;

        }


        void CloseAllTabsButThis(CloseableTabItem tabitem)
        {
            List<CloseableTabItem> tabs = new List<CloseableTabItem>();
            foreach (object item in tabControl1.Items)
            {
                CloseableTabItem it = item as CloseableTabItem;
                tabs.Add(it);
            }


            foreach (CloseableTabItem item in tabs)
            {

                if (true)
                {
                    if (item != tabitem)
                    {
                        CloseTab(item);
                    }

                }

            }

            tabs = null;
            ConstructMoveToCopyToMenu();

        }

        public void NewTab(bool IsNavigate = true)
        {
            CloseableTabItem newt = new CloseableTabItem();
            CreateTabbarRKMenu(newt);

            ShellObject DefPath;
            if (StartUpLocation.IndexOf("::") == 0 && StartUpLocation.IndexOf(@"\") == -1)
                DefPath = ShellObject.FromParsingName("shell:" + StartUpLocation);
            else
                try
                {
                    DefPath = ShellObject.FromParsingName(StartUpLocation);
                }
                catch
                {
                    DefPath = (ShellObject)KnownFolders.Libraries;
                }
            DefPath.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            DefPath.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            newt.Header = DefPath.GetDisplayName(DisplayNameType.Default);
            newt.TabIcon = DefPath.Thumbnail.BitmapSource;
            newt.Path = DefPath;
            newt.IsNavigate = IsNavigate;
            newt.Index = tabControl1.Items.Count;
            newt.AllowDrop = true;
            newt.log.CurrentLocation = DefPath;

            newt.CloseTab += new RoutedEventHandler(newt_CloseTab);
            newt.DragEnter += new DragEventHandler(newt_DragEnter);
            newt.DragOver += new DragEventHandler(newt_DragOver);
            newt.Drop += new DragEventHandler(newt_Drop);
            tabControl1.Items.Add(newt);
            LastTabIndex = tabControl1.SelectedIndex;
            tabControl1.SelectedIndex = tabControl1.Items.Count - 1;
            tabControl1.SelectedItem = tabControl1.Items[tabControl1.Items.Count - 1];

            CurrentTabIndex = tabControl1.Items.Count - 1;
            ConstructMoveToCopyToMenu();
        }

        public void NewTab(string Location, bool IsNavigate = false)
        {
            CloseableTabItem newt = new CloseableTabItem();
            CreateTabbarRKMenu(newt);

            ShellObject DefPath;
            if (Location.IndexOf("::") == 0 && Location.IndexOf(@"\") == -1)
                DefPath = ShellObject.FromParsingName("shell:" + Location);
            else
                DefPath = ShellObject.FromParsingName(Location);
            DefPath.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            DefPath.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            newt.Header = DefPath.GetDisplayName(DisplayNameType.Default);
            newt.TabIcon = DefPath.Thumbnail.BitmapSource;
            newt.Path = DefPath;
            newt.IsNavigate = IsNavigate;
            newt.Index = tabControl1.Items.Count;
            newt.AllowDrop = true;
            newt.CloseTab += new RoutedEventHandler(newt_CloseTab);
            newt.DragEnter += new DragEventHandler(newt_DragEnter);
            newt.DragOver += new DragEventHandler(newt_DragOver);
            newt.Drop += new DragEventHandler(newt_Drop);

            tabControl1.Items.Add(newt);
            LastTabIndex = tabControl1.SelectedIndex;
            if (IsNavigate)
            {
                //IsCancel = true;

                tabControl1.SelectedIndex = tabControl1.Items.Count - 1;
                tabControl1.SelectedItem = tabControl1.Items[tabControl1.Items.Count - 1];
                //IsCancel = false;
            }
            else
            {
                newt.log.CurrentLocation = DefPath;
            }

            CurrentTabIndex = tabControl1.Items.Count - 1;
            ConstructMoveToCopyToMenu();
        }

        void newt_Drop(object sender, DragEventArgs e)
        {


            System.Windows.Point pt = e.GetPosition(sender as IInputElement);


            if ((sender as CloseableTabItem).Path.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        DropData PasteData = new DropData();
                        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                        StringCollection scol = new StringCollection();
                        scol.AddRange(collection);
                        PasteData.DropList = scol;
                        PasteData.PathForDrop = (sender as CloseableTabItem).Path.ParsingName;
                        Thread t = null;
                        t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start(PasteData);
                    }
                }
                else
                {
                    //if (Path.GetPathRoot((sender as CloseableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    e.Effects = DragDropEffects.Move;
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        DropData PasteData = new DropData();
                        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                        StringCollection scol = new StringCollection();
                        scol.AddRange(collection);
                        PasteData.DropList = scol;
                        PasteData.PathForDrop = (sender as CloseableTabItem).Path.ParsingName;
                        Thread t = null;
                        t = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start(PasteData);
                    }

                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;
                    //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    //    {
                    //        DropData PasteData = new DropData();
                    //        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                    //        StringCollection scol = new StringCollection();
                    //        scol.AddRange(collection);
                    //        PasteData.DropList = scol;
                    //        PasteData.PathForDrop = (sender as CloseableTabItem).Path.ParsingName;
                    //        Thread t = null;
                    //        t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                    //        t.SetApartmentState(ApartmentState.STA);
                    //        t.Start(PasteData);
                    //    }

                    //}
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }


            DropTargetHelper.Drop(e.Data, pt, e.Effects);
        }

        void newt_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if ((sender as CloseableTabItem).Path.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;

                }
                else
                {
                    //if (Path.GetPathRoot((sender as CloseableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    //    e.Effects = DragDropEffects.Move;


                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;

                    //}

                    // I decided just to have it do a move because it will avoid errors with special shell folders.
                    // Besides, if a person wants to copy something, they should know how to press Ctrl to copy.
                    e.Effects = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            Win32Point ptw = new Win32Point();
            GetCursorPos(ref ptw);
            //e.Handled = true;
            DropTargetHelper.DragOver(new System.Windows.Point(ptw.X, ptw.Y), e.Effects);
        }

        void newt_DragLeave(object sender, DragEventArgs e)
        {
            DropTargetHelper.DragLeave();
        }

        void newt_DragEnter(object sender, DragEventArgs e)
        {


            if ((sender as CloseableTabItem).Path.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;

                }
                else
                {
                    //if (Path.GetPathRoot((sender as CloseableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    //    e.Effects = DragDropEffects.Move;


                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;

                    //}

                    // I decided just to have it do a move because it will avoid errors with special shell folders.
                    // Besides, if a person wants to copy something, they should know how to press Ctrl to copy.
                    e.Effects = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }


            Win32Point ptw = new Win32Point();
            GetCursorPos(ref ptw);
            e.Effects = DragDropEffects.None;
            DropTargetHelper.DragEnter(this, e.Data, new System.Windows.Point(ptw.X, ptw.Y), e.Effects);
        }

        private void tabControl1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            string h = sender.GetType().Name.ToString();

            hitTestList = new List<DependencyObject>();

            System.Windows.Point pt = e.GetPosition(sender as IInputElement);

            VisualTreeHelper.HitTest(
                sender as Visual, null,
                CollectAllVisuals_Callback,
                new PointHitTestParameters(pt));

            hitTestList.Reverse();

            //DependencyObject elementToFind = null;

            if (hitTestList[0].GetType().Name.Equals("ScrollViewer") && hitTestList.Count == 2)
            {
                NewTab();
            }
            else if (hitTestList.Count == 3)
            {
                if (hitTestList[2].GetType().Name == "Grid")
                {
                    NewTab();
                }
            }

        }

        #endregion

        #region HelperFunctions
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static System.Drawing.Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new System.Drawing.Point(w32Mouse.X, w32Mouse.Y);
        }


        List<DependencyObject> hitTestList = null;
        HitTestResultBehavior CollectAllVisuals_Callback(HitTestResult result)
        {
            if (result == null || result.VisualHit == null)
                return HitTestResultBehavior.Stop;

            hitTestList.Add(result.VisualHit);
            return HitTestResultBehavior.Continue;
        }
        public bool Activate(bool restoreIfMinimized)
        {
            if (restoreIfMinimized && WindowState == WindowState.Minimized)
            {
                WindowState = PreviouseWindowState == WindowState.Normal
                                        ? WindowState.Normal : WindowState.Maximized;
            }
            return Activate();
        }

        /// <summary>
        /// Apends the args.
        /// </summary>
        /// <param name="args">The args.</param>
        public void ApendArgs(string[] args)
        {
            //if (args == null) return;
            //Application.Current.Properties["cmd2"] = args[0];
        }

        private ImageFormat GetImageFormat(string FileName)
        {
            using (Bitmap bitmap = new Bitmap(FileName))
            {
                return bitmap.RawFormat;
            }
        }


        private void RotateAndSaveImage(String input, String output, ImageFormat format, RotateFlipType type)
        {
            //create an object that we can use to examine an image file
            System.Drawing.Image img = System.Drawing.Image.FromFile(input);

            //rotate the picture by 90 degrees
            img.RotateFlip(type);

            //re-save the picture
            img.Save(output, format);

            //tidy up after we've finished
            img.Dispose();
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bool exitApp = false;
            try
            {
                if (WindowsAPI.getOSInfo() == WindowsAPI.OsVersionInfo.Windows8)
                {
                    TheStatusBar.Visibility = System.Windows.Visibility.Collapsed;
                    rStatusbar.Height = new GridLength(0);
                }



                Handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                //Itmpop = new ItemPopup();
                //Itmpop.Visibility = System.Windows.Visibility.Hidden;
                //Itmpop.Owner = this;
                //Itmpop.Show();

                //'sets up FileSystemWatcher for Favorites folder
                String FavPath = "";
                try
                {
                    FavPath = KnownFolders.Links.ParsingName;
                    FileSystemWatcher fsw = new FileSystemWatcher(FavPath);
                    fsw.Created += new FileSystemEventHandler(fsw_Created);
                    fsw.Deleted += new FileSystemEventHandler(fsw_Deleted);
                    fsw.Renamed += new RenamedEventHandler(fsw_Renamed);
                    fsw.EnableRaisingEvents = true;
                }
                catch
                {

                    FavPath = "";
                }

                //' set up breadcrumb bar
                breadcrumbBarControl1.SetDragHandlers(new DragEventHandler(bbi_DragEnter), new DragEventHandler(bbi_DragLeave), new DragEventHandler(bbi_DragOver), new DragEventHandler(bbi_Drop));

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                 (ThreadStart)(() =>
                                 {
                                     //PicturePreview = new PreviewMedia();

                                     //'set up Favorites menu (note that BetterExplorer does not support links to a Control Panel. lol -JaykeBird)
                                     //'I will probably use a modification to this code in the new breadcrumbbar
                                     if (FavPath != "")
                                     {
                                         DirectoryInfo FavInfo = new DirectoryInfo(FavPath);
                                         FileInfo[] FavFiles = FavInfo.GetFiles();
                                         foreach (FileInfo item in FavFiles)
                                         {
                                             if (Path.GetExtension(item.FullName).ToLowerInvariant() == ".lnk")
                                             {
                                                 try
                                                 {
                                                     ShellObject so = ShellObject.FromParsingName(item.FullName);
                                                     MenuItem mi = new MenuItem();
                                                     mi.Header = so.GetDisplayName(DisplayNameType.Default);
                                                     ShellLink lnk = new ShellLink(so.ParsingName);
                                                     string Target = lnk.Target;
                                                     if (Target.Contains("::"))
                                                     {
                                                         Target = "shell:" + Target;
                                                     }
                                                     if (item.Name.ToLowerInvariant() == "recentplaces.lnk")
                                                     {
                                                         Target = "shell:::{22877a6d-37a1-461a-91b0-dbda5aaebc99}";
                                                     }
                                                     mi.Tag = Target;
                                                     lnk.Dispose();
                                                     so.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                                     so.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                                     ImageSource icon = so.Thumbnail.BitmapSource;
                                                     mi.Icon = icon;
                                                     mi.Click += new RoutedEventHandler(mif_Click);
                                                     so.Dispose();
                                                     btnFavorites.Items.Add(mi);
                                                 }
                                                 catch
                                                 {


                                                 }
                                             }
                                         }
                                     }
                                     else
                                     {
                                         btnFavorites.Visibility = System.Windows.Visibility.Collapsed;
                                     }

                                     //'set up Explorer control
                                     Explorer.SelectionChanged += new EventHandler(ExplorerBrowserControl_SelectionChanged);
                                     Explorer.NavigationComplete += new EventHandler<NavigationCompleteEventArgs>(ExplorerBrowserControl_NavigationComplete);
                                     Explorer.ViewEnumerationComplete += new EventHandler(ExplorerBrowserControl_ViewEnumerationComplete);
                                     Explorer.NavigationPending += new EventHandler<NavigationPendingEventArgs>(Explorer_NavigationPending);
                                     Explorer.GotFocus += new EventHandler(Explorer_GotFocus);
                                     Explorer.ExplorerGotFocus += new EventHandler(Explorer_ExplorerGotFocus);
                                     //Explorer.ExplorerGotFocus += new EventHandler(Explorer_ExplorerGotFocus);
                                     Explorer.RenameFinished += new EventHandler(Explorer_RenameFinished);
                                     Explorer.KeyUP += new EventHandler<ExplorerKeyUPEventArgs>(Explorer_KeyUP);
                                     Explorer.KeyUp += new System.Windows.Forms.KeyEventHandler(explorerBrowser1_KeyUp);
                                     Explorer.LostFocus += new EventHandler(Explorer_LostFocus);

                                     Explorer.NavigationOptions.PaneVisibility.Commands = PaneVisibilityState.Hide;
                                     Explorer.NavigationOptions.PaneVisibility.CommandsOrganize = PaneVisibilityState.Hide;
                                     Explorer.NavigationOptions.PaneVisibility.CommandsView = PaneVisibilityState.Hide;
                                     Explorer.ItemsChanged += new EventHandler(Explorer_ItemsChanged);
                                     Explorer.ContentOptions.FullRowSelect = true;
                                     //Explorer.ContentOptions.CheckSelect = false;
                                     Explorer.ClientSizeChanged += new EventHandler(ExplorerBrowserControl_ClientSizeChanged);
                                     Explorer.Paint += new System.Windows.Forms.PaintEventHandler(ExplorerBrowserControl_Paint);
                                     Explorer.ViewChanged += new EventHandler<ViewChangedEventArgs>(Explorer_ViewChanged);
                                     //Explorer.ItemHot += new EventHandler<ExplorerAUItemEventArgs>(Explorer_ItemHot);
                                     Explorer.ExplorerBrowserMouseLeave += new EventHandler(Explorer_ExplorerBrowserMouseLeave);
                                     Explorer.MouseMove += new System.Windows.Forms.MouseEventHandler(Explorer_MouseMove);
                                     IsCalledFromLoading = true;
                                     WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                     WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWALLOBJECTS | WindowsAPI.SSF.SSF_SHOWEXTENSIONS, false);
                                     chkHiddenFiles.IsChecked = (state.fShowAllObjects == 1);
                                     chkExtensions.IsChecked = (state.fShowExtensions == 1);
                                     IsCalledFromLoading = false;
                                     isOnLoad = true;
                                     //'load from Registry
                                     RegistryKey rk = Registry.CurrentUser;
                                     RegistryKey rks = rk.CreateSubKey(@"Software\BExplorer");

                                     switch (Convert.ToString(rks.GetValue(@"CurrentTheme", "Blue")))
                                     {
                                         case "Blue":
                                             btnBlue.IsChecked = true;
                                             break;
                                         case "Silver":
                                             btnSilver.IsChecked = true;
                                             break;
                                         case "Black":
                                             btnBlack.IsChecked = true;
                                             break;
                                         case "Green":
                                             btnGreen.IsChecked = true;
                                             break;
                                         default:
                                             btnBlue.IsChecked = true;
                                             break;
                                     }
                                     int HFlyoutEnabled = (int)rks.GetValue(@"HFlyoutEnabled", 0);

                                     IsHFlyoutEnabled = (HFlyoutEnabled == 1);
                                     chkIsFlyout.IsChecked = IsHFlyoutEnabled;

                                     int InfoPaneEnabled = (int)rks.GetValue(@"InfoPaneEnabled", 0);

                                     IsInfoPaneEnabled = (InfoPaneEnabled == 1);
                                     btnInfoPane.IsChecked = IsInfoPaneEnabled;

                                     int PreviewPaneEnabled = (int)rks.GetValue(@"PreviewPaneEnabled", 0);

                                     IsPreviewPaneEnabled = (PreviewPaneEnabled == 1);
                                     btnPreviewPane.IsChecked = IsPreviewPaneEnabled;

                                     int NavigationPaneEnabled = (int)rks.GetValue(@"NavigationPaneEnabled", 1);

                                     IsNavigationPaneEnabled = (NavigationPaneEnabled == 1);
                                     btnNavigationPane.IsChecked = IsNavigationPaneEnabled;

                                     int CheckBoxesEnabled = (int)rks.GetValue(@"CheckModeEnabled", 0);

                                     isCheckModeEnabled = (CheckBoxesEnabled == 1);
                                     chkShowCheckBoxes.IsChecked = isCheckModeEnabled;

                                     int ExFileOpEnabled = (int)rks.GetValue(@"FileOpExEnabled", 0);

                                     IsExtendedFileOpEnabled = (ExFileOpEnabled == 1);
                                     Explorer.IsExFileOpEnabled = IsExtendedFileOpEnabled;
                                     chkIsTerraCopyEnabled.IsChecked = IsExtendedFileOpEnabled;

                                     int CompartibleRename = (int)rks.GetValue(@"CompartibleRename", 1);

                                     IsCompartibleRename = (CompartibleRename == 1);

                                     //chkIsCompartibleRename.IsChecked = IsCompartibleRename;

                                     int RestoreTabs = (int)rks.GetValue(@"IsRestoreTabs", 1);

                                     IsrestoreTabs = (RestoreTabs == 1);

                                     chkIsRestoreTabs.IsChecked = IsrestoreTabs;

                                     int LogActions = (int)rks.GetValue(@"EnableActionLog", 0);

                                     canlogactions = (LogActions == 1);
                                     chkLogHistory.IsChecked = canlogactions;

                                     // load settings for auto-switch to contextual tab
                                     asFolder = ((int)rks.GetValue(@"AutoSwitchFolderTools", 0) == 1);
                                     asArchive = ((int)rks.GetValue(@"AutoSwitchArchiveTools", 1) == 1);
                                     asImage = ((int)rks.GetValue(@"AutoSwitchImageTools", 1) == 1);
                                     asApplication = ((int)rks.GetValue(@"AutoSwitchApplicationTools", 0) == 1);
                                     asLibrary = ((int)rks.GetValue(@"AutoSwitchLibraryTools", 1) == 1);
                                     asDrive = ((int)rks.GetValue(@"AutoSwitchDriveTools", 0) == 1);

                                     chkFolder.IsChecked = asFolder;
                                     chkArchive.IsChecked = asArchive;
                                     chkImage.IsChecked = asImage;
                                     chkApp.IsChecked = asApplication;
                                     chkLibrary.IsChecked = asLibrary;
                                     chkDrive.IsChecked = asDrive;

                                     // set up history on breadcrumb bar (currently missing try-catch statement in order to catch error)
                                     try
                                     {
                                         breadcrumbBarControl1.ClearHistory();
                                         breadcrumbBarControl1.HistoryItems = ReadHistoryFromFile(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\history.txt");
                                     }
                                     catch (FileNotFoundException)
                                     {

                                         //throw;
                                     }

                                     AddToLog("Session Began");

                                     StartUpLocation =
                                          rks.GetValue(@"StartUpLoc", KnownFolders.Libraries.ParsingName).ToString();

                                     if (StartUpLocation == "")
                                     {
                                         rks.SetValue(@"StartUpLoc", KnownFolders.Libraries.ParsingName);
                                         StartUpLocation = KnownFolders.Libraries.ParsingName;
                                     }
                                     char[] delimiters = new char[] { ';' };
                                     string LastOpenedTabs = rks.GetValue(@"OpenedTabs", "").ToString();
                                     string[] Tabs = LastOpenedTabs.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                                     rks.Close();
                                     rk.Close();

                                     try
                                     {
                                         RegistryKey rkbe = Registry.ClassesRoot;
                                         RegistryKey rksbe = rkbe.OpenSubKey(@"Folder\shell", RegistryKeyPermissionCheck.ReadSubTree);
                                         bool IsThereDefault = rksbe.GetValue("", "-1").ToString() != "";
                                         chkIsDefault.IsChecked = IsThereDefault;
                                         chkIsDefault.IsEnabled = true;
                                         rksbe.Close();
                                         rkbe.Close();
                                     }
                                     catch (Exception)
                                     {
                                         chkIsDefault.IsChecked = false;
                                         chkIsDefault.IsEnabled = false;
                                     }

                                     RegistryKey rkfe = Registry.CurrentUser;
                                     RegistryKey rksfe = rk.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", RegistryKeyPermissionCheck.ReadSubTree);
                                     chkTreeExpand.IsChecked = (int)rksfe.GetValue("NavPaneExpandToCurrentFolder", 0) == 1;
                                     rksfe.Close();
                                     rkfe.Close();

                                     isOnLoad = false;


                                     //'Rest of the setup of Explorer control. We have to set up that here after 
                                     //the initialization of registry settings
                                     Explorer.NavigationOptions.PaneVisibility.Preview =
                                         IsPreviewPaneEnabled ? PaneVisibilityState.Show : PaneVisibilityState.Hide;
                                     Explorer.NavigationOptions.PaneVisibility.Details =
                                         IsInfoPaneEnabled ? PaneVisibilityState.Show : PaneVisibilityState.Hide;

                                     Explorer.NavigationOptions.PaneVisibility.Navigation =
                                         IsNavigationPaneEnabled ? PaneVisibilityState.Show : PaneVisibilityState.Hide;



                                     Explorer.ContentOptions.CheckSelect = isCheckModeEnabled;

                                     if (StartUpLocation.IndexOf("::") == 0 && StartUpLocation.IndexOf(@"\") == -1)
                                     {
                                         btnSetCurrentasStartup.Header =
                                            ShellObject.FromParsingName("Shell:" + StartUpLocation).GetDisplayName(DisplayNameType.Default);
                                         btnSetCurrentasStartup.Icon = ShellObject.FromParsingName("Shell:" + StartUpLocation).Thumbnail.BitmapSource;
                                     }
                                     else
                                     {
                                         try
                                         {
                                             btnSetCurrentasStartup.Header =
                                                  ShellObject.FromParsingName(StartUpLocation).GetDisplayName(DisplayNameType.Default);

                                             btnSetCurrentasStartup.Icon = ShellObject.FromParsingName(StartUpLocation).Thumbnail.BitmapSource;
                                         }
                                         catch 
                                         {
                                             
                                            
                                         }
                                     }

                                     //'set StartUp location
                                     if (Application.Current.Properties["cmd"] != null)
                                     {

                                         String cmd = Application.Current.Properties["cmd"].ToString();

                                         if (cmd.IndexOf("::") == 0)
                                         {

                                             Explorer.Navigate(ShellObject.FromParsingName("shell:" + cmd));
                                             //CloseAllTabsButThis((CloseableTabItem)tabControl1.SelectedItem);
                                         }
                                         else
                                             Explorer.Navigate(ShellObject.FromParsingName(cmd.Replace("\"", "")));

                                         
                                         CloseableTabItem cti = new CloseableTabItem();
                                         CreateTabbarRKMenu(cti);
                                         Explorer.NavigationLog.CurrentLocation.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                         cti.TabIcon = Explorer.NavigationLog.CurrentLocation.Thumbnail.BitmapSource;
                                         cti.Header = Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default);
                                         cti.Path = Explorer.NavigationLog.CurrentLocation;
                                         cti.Index = 0;
                                         cti.log.CurrentLocation = Explorer.NavigationLog.CurrentLocation;
                                         cti.CloseTab += new RoutedEventHandler(cti_CloseTab);
                                         tabControl1.Items.Add(cti);
                                         tabControl1.SelectedIndex = 0;
                                         CurrentTabIndex = tabControl1.SelectedIndex;
                                     }
                                     else
                                     {
                                         if (Tabs.Length == 0 || !IsrestoreTabs)
                                         {
                                             if (StartUpLocation.IndexOf("::") == 0 && StartUpLocation.IndexOf(@"\") == -1)
                                                 Explorer.Navigate(ShellObject.FromParsingName("shell:" + StartUpLocation));
                                             else
                                                 try
                                                 {
                                                     Explorer.Navigate(ShellObject.FromParsingName(StartUpLocation));
                                                 }
                                                 catch
                                                 {
                                                     Explorer.Navigate((ShellObject)KnownFolders.Libraries);
                                                 }
                                         }
                                         if (IsrestoreTabs)
                                         {
                                             isOnLoad = true;
                                             int i = 0;
                                             foreach (string str in Tabs)
                                             {
                                                 try
                                                 {
                                                     i++;
                                                     if (i == Tabs.Length)
                                                     {
                                                         NewTab(str, true);
                                                     }
                                                     else
                                                     {
                                                         NewTab(str, false);
                                                     }
                                                 }
                                                 catch
                                                 {
                                                     MessageBox.Show("BetterExplorer is unable to load one of the tabs from your last session. Your other tabs are perfectly okay though! \r\n\r\nThis location was unable to be loaded: " + str, "Unable to Create New Tab", MessageBoxButton.OK, MessageBoxImage.Error);
                                                     //NewTab();
                                                 }

                                             }
                                             isOnLoad = false;

                                         }
                                     }

                                     //sets up Jump List
                                     AppJL.ShowRecentCategory = true;
                                     AppJL.ShowFrequentCategory = true;
                                     JumpList.SetJumpList(Application.Current, AppJL);
                                     JumpTask newTab = new JumpTask();
                                     newTab.ApplicationPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                                     newTab.Arguments = "t";
                                     newTab.Title = "Open Tab";
                                     newTab.Description = "Opens new tab with default location";

                                     JumpTask newWindow = new JumpTask();
                                     newWindow.ApplicationPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                                     newWindow.Arguments = "/nw";
                                     newWindow.Title = "New Window";
                                     newWindow.Description = "Creates a new window with default location";

                                     AppJL.JumpItems.Add(newTab);
                                     AppJL.JumpItems.Add(newWindow); //'causes BetterExplorer to crash
                                     AppJL.Apply();


                                     //'set up tab bar (good thing to know for when I add my new tab bar -JaykeBird)
                                     cbm.ClipboardChanged += new EventHandler<ClipboardChangedEventArgs>(cbm_ClipboardChanged);

                                     if (Tabs.Length == 0 || !IsrestoreTabs)
                                     {
                                         CloseableTabItem cti = new CloseableTabItem();
                                         CreateTabbarRKMenu(cti);
                                         Explorer.NavigationLog.CurrentLocation.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                         cti.TabIcon = Explorer.NavigationLog.CurrentLocation.Thumbnail.BitmapSource;
                                         cti.Header = Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default);
                                         cti.Path = Explorer.NavigationLog.CurrentLocation;
                                         cti.Index = 0;
                                         cti.log.CurrentLocation = Explorer.NavigationLog.CurrentLocation;
                                         cti.CloseTab += new RoutedEventHandler(cti_CloseTab);
                                         tabControl1.Items.Add(cti);
                                         tabControl1.SelectedIndex = 0;
                                         CurrentTabIndex = tabControl1.SelectedIndex;
                                     }
                                     


                                 }
               ));

                if (exitApp)
                {

                    Application.Current.Shutdown();
                    return;
                }

                try
                {

                    Updater = UpdateManager.Instance;
                    Updater.UpdateFeedReader = new NAppUpdate.Framework.FeedReaders.NauXmlFeedReader();
                    Updater.UpdateExecutableName = "Web Update.exe";
                    Updater.UpdateSource = new NAppUpdate.Framework.Sources.SimpleWebSource("http://sharobg.com/betterex/Update/update.xml");
                    //CheckForUpdate(false);
                }
                catch (IOException)
                {
                    this.stiUpdate.Content = "Switch to another BetterExplorer window or restart to check for updates.";
                    this.btnUpdateCheck.IsEnabled = false;
                }

                verNumber.Content = "Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " Beta 1";
                if (!TheRibbon.IsMinimized)
                {
                    TheRibbon.SelectedTabItem = HomeTab;
                    this.TheRibbon.SelectedTabIndex = 0;
                }


                //MessageBox.Show(TheRibbon.SelectedTabIndex.ToString(), "SelectedTabIndex Should be 0", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exe)
            {
                MessageBox.Show("An error occurred while loading the window. Please report this issue at http://bexplorer.codeplex.com/. \r\n\r\n Here is some information about the error: \r\n\r\n" + exe.Message + "\r\n\r\n" + exe.ToString(), "Error While Loading", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public void CheckForUpdate(bool ShowNoUpdatesMessage = true)
        {
            if (Updater.CheckForUpdates())
            {

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                            (Action)(() =>
                                            {
                                                string ver = "";
                                                foreach (IUpdateTask item in Updater.UpdatesToApply)
                                                {

                                                    if ((item as FileUpdateTask).LocalPath.ToLowerInvariant() ==
                                                        Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName).ToLowerInvariant())
                                                    {

                                                        foreach (BooleanCondition.ConditionItem itemc in (item as FileUpdateTask).UpdateConditions.ChildConditions)
                                                        {
                                                            var fileVersion = itemc._Condition as FileVersionCondition;
                                                            if (fileVersion != null)
                                                            {
                                                                ver = fileVersion.Version;
                                                            }
                                                        }
                                                    }
                                                }
                                                stiUpdate.Content = FindResource("stUpdateAvailableCP").ToString().Replace("VER", ver);
                                                stiUpdate.Foreground = System.Windows.Media.Brushes.Red;

                                                if (MessageBox.Show("Updates are available. Do you want to update?", "Updates available",
                                                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                                {
                                                    Updater.PrepareUpdatesAsync(finished =>
                                                    {

                                                        try
                                                        {
                                                            if (finished)
                                                            {
                                                                Updater.ApplyUpdates();
                                                                // ApplyUpdates is a synchronous method by design. Make sure to save all user work before calling
                                                                // it as it might restart your application
                                                                // get out of the way so the console window isn't obstructed
                                                                Dispatcher d = Application.Current.Dispatcher;
                                                                d.BeginInvoke(new Action(() => this.Hide()));
                                                                if (!Updater.ApplyUpdates(true, true, true))
                                                                {
                                                                    d.BeginInvoke(new Action(() => this.Show())); // this.WindowState = WindowState.Normal;
                                                                    MessageBox.Show("An error occurred while trying to install software updates");
                                                                }
                                                                else
                                                                {
                                                                    d.BeginInvoke(new Action(() => this.Close()));
                                                                }
                                                                Updater.CleanUp();
                                                                App.Current.Dispatcher.BeginInvoke(new Action(() => this.Close()));
                                                            }
                                                            else
                                                                Updater.CleanUp();
                                                        }
                                                        catch (System.Exception ex)
                                                        {
                                                            MessageBox.Show("Error", "There was a problem with the update: \n" + ex.Message);
                                                        }

                                                    });




                                                }

                                            }));


            }
            else
            {
                stiUpdate.Content = FindResource("stUpdateNotAvailableCP").ToString();
                stiUpdate.Foreground = System.Windows.Media.Brushes.Black;
                if (ShowNoUpdatesMessage)
                    MessageBox.Show("No updates are available.", "Updates available",
                                                        MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public MainWindow()
        {
            CommandBinding cbnewtab = new CommandBinding(AppCommands.RoutedNewTab, ERNewTab);
            this.CommandBindings.Add(cbnewtab);
            CommandBinding cbGotoCombo = new CommandBinding(AppCommands.RoutedEnterInBreadCrumbCombo, ERGoToBCCombo);
            this.CommandBindings.Add(cbGotoCombo);
            CommandBinding cbCloseTab = new CommandBinding(AppCommands.RoutedCloseTab, RCloseTab);
            this.CommandBindings.Add(cbCloseTab);
            CommandBinding cbChangeTab= new CommandBinding(AppCommands.RoutedChangeTab, ChangeTab);
            this.CommandBindings.Add(cbChangeTab);
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

            // loads current Ribbon color theme
            try
            {
                switch (Convert.ToString(rks.GetValue(@"CurrentTheme", "Blue")))
                {
                    case "Blue":
                        ChangeRibbonThemeL("Blue");
                        break;
                    case "Silver":
                        ChangeRibbonThemeL("Silver");
                        break;
                    case "Black":
                        ChangeRibbonThemeL("Black");
                        break;
                    case "Green":
                        ChangeRibbonThemeL("Green");
                        break;
                    default:
                        ChangeRibbonThemeL("Blue");
                        break;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while trying to load the theme data from the Registry. \n\r \n\r" + ex.Message + "\n\r \n\rPlease let us know of this issue at http://bexplorer.codeplex.com/", "RibbonTheme Error - " + ex.ToString());
                //MessageBox.Show("There was exception during reg read! Please report on http:\\bexplorer.codeplex.com");
            }

            // loads current UI language (uses en-US if default)
            try
            {

                string loc;
                if (Convert.ToString(rks.GetValue(@"Locale", ":null:")) == ":null:")
                {
                    // creates value in Registry if value does not exist
                    rks.SetValue(@"Locale", "en-US");
                    loc = "en-US";
                }
                else
                {
                    loc = Convert.ToString(rks.GetValue(@"Locale", ":null:"));
                }

                SelectCulture(loc);

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while trying to load the locale data from the Registry. \n\r \n\r" + ex.Message + "\n\r \n\rPlease let us know of this issue at http://bexplorer.codeplex.com/", "RibbonTheme Error - " + ex.ToString());
                //MessageBox.Show("There was exception during reg read! Please report on http:\\bexplorer.codeplex.com");
            }

            // gets values from registry to be applied after initialization
            string lohc = Convert.ToString(rks.GetValue(@"Locale", ":null:"));
            double sbw = Convert.ToDouble(rks.GetValue(@"SearchBarWidth", "220"));

            string rtlused = Convert.ToString(rks.GetValue(@"RTLMode", "false"));

            string tabba = Convert.ToString(rks.GetValue(@"TabBarAlignment", "top"));

            rks.Close();
            rk.Close();

            //Main Initialization routine
            InitializeComponent();

            // sets up ComboBox to select the current UI language
            foreach (TranslationComboBoxItem item in this.TranslationComboBox.Items)
            {
                if (item.LocaleCode == lohc)
                {
                    this.TranslationComboBox.SelectedItem = item;
                }
            }

            // gets size of search bar
            this.SearchBarColumn.Width = new GridLength(sbw);

            // store 1st value
            PreviouseWindowState = WindowState;

            // attach to event (used to store prev. win. state)
            LayoutUpdated += Window_LayoutUpdated;

            ShellVView.Child = Explorer;

            // prepares RTL mode
            if (rtlused == "true")
            {
                FlowDirection = System.Windows.FlowDirection.RightToLeft;
            }
            else
            {
                FlowDirection = System.Windows.FlowDirection.LeftToRight;
            }

            if (tabba == "top")
            {
                TabbaTop.IsChecked = true;
            }
            else if (tabba == "bottom")
            {
                TabbaBottom.IsChecked = true;
            }
            else
            {
                TabbaTop.IsChecked = true;
            }

            // allows user to change language
            ReadyToChangeLanguage = true;
        }

        private void RibbonWindow_Closing(object sender, CancelEventArgs e)
        {
            Explorer.automan.Dispose();
            //if (PicturePreview != null)
            //{
            //    PicturePreview.Close(); 
            //}
            string OpenedTabs = "";
            foreach (CloseableTabItem item in tabControl1.Items)
            {
                OpenedTabs += ";" + item.Path.ParsingName;
            }
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"LastWindowWidth", this.Width);
            rks.SetValue(@"LastWindowHeight", this.Height);
            rks.SetValue(@"LastWindowPosLeft", this.Left);
            rks.SetValue(@"LastWindowPosTop", this.Top);
            if (btnBlue.IsChecked == true)
            {
                rks.SetValue(@"CurrentTheme", "Blue");
            }
            else if (btnSilver.IsChecked == true)
            {
                rks.SetValue(@"CurrentTheme", "Silver");
            }
            else if (btnBlack.IsChecked == true)
            {
                rks.SetValue(@"CurrentTheme", "Black");
            }
            else if (btnGreen.IsChecked == true)
            {
                rks.SetValue(@"CurrentTheme", "Green");
            }
            switch (this.WindowState)
            {
                case System.Windows.WindowState.Maximized:
                    rks.SetValue(@"LastWindowState", 2);
                    break;
                case System.Windows.WindowState.Minimized:
                    rks.SetValue(@"LastWindowState", 1);
                    break;
                case System.Windows.WindowState.Normal:
                    rks.SetValue(@"LastWindowState", 0);
                    break;
                default:
                    rks.SetValue(@"LastWindowState", -1);
                    break;
            }
            rks.SetValue(@"IsRibonMinimized", TheRibbon.IsMinimized);
            rks.SetValue(@"OpenedTabs", OpenedTabs);

            if (FlowDirection == System.Windows.FlowDirection.RightToLeft)
            {
                rks.SetValue(@"RTLMode", "true");
            }
            else
            {
                rks.SetValue(@"RTLMode", "false");
            }

                                     //asFolder = ((int)rks.GetValue(@"AutoSwitchFolderTools", 0) == 1);
                                     //asArchive = ((int)rks.GetValue(@"AutoSwitchArchiveTools", 1) == 1);
                                     //asImage = ((int)rks.GetValue(@"AutoSwitchImageTools", 1) == 1);
                                     //asApplication = ((int)rks.GetValue(@"AutoSwitchApplicationTools", 0) == 1);
                                     //asLibrary = ((int)rks.GetValue(@"AutoSwitchLibraryTools", 1) == 1);
                                     //asDrive = ((int)rks.GetValue(@"AutoSwitchDriveTools", 0) == 1);

            rks.SetValue(@"AutoSwitchFolderTools", GetIntegerFromBoolean(asFolder));
            rks.SetValue(@"AutoSwitchArchiveTools", GetIntegerFromBoolean(asArchive));
            rks.SetValue(@"AutoSwitchImageTools", GetIntegerFromBoolean(asImage));
            rks.SetValue(@"AutoSwitchApplicationTools", GetIntegerFromBoolean(asApplication));
            rks.SetValue(@"AutoSwitchLibraryTools", GetIntegerFromBoolean(asLibrary));
            rks.SetValue(@"AutoSwitchDriveTools", GetIntegerFromBoolean(asDrive));

            if (this.TabbaBottom.IsChecked == true)
            {
                rks.SetValue(@"TabBarAlignment", "bottom");
            }
            else
            {
                rks.SetValue(@"TabBarAlignment", "top");
            }

            rks.Close();

            SaveHistoryToFile(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\history.txt", breadcrumbBarControl1.HistoryItems);
            AddToLog("Session Ended");
        }

        #region Old Search Code
        private ShellObject BeforeSearchFolder;
        Thread backgroundSearchThread;
        // Helper method to do the search on a background thread
        internal void DoSimpleSearch(object arg)
        {
            SearchCondition searchCondition = SearchConditionFactory.ParseStructuredQuery(arg.ToString());
            ShellSearchFolder searchFolder =
                new ShellSearchFolder(searchCondition, (ShellContainer)BeforeSearchFolder);


            Dispatcher.Invoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(
                    delegate()
                    {
                        Explorer.Navigate(searchFolder);

                    }
                ));

            if (BeforeSearchFolder != null)
            {
                Thread.Sleep(750);
                Explorer.RefreshExplorer();
            }


        }


        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Explorer.Navigate((ShellSearchFolder)e.Argument);
        }

        public void DoSearch(string SearchCriteria)
        {
            if (backgroundSearchThread != null)
                backgroundSearchThread.Abort();


            if (Explorer.NavigationLog.CurrentLocation.IsSearchFolder)
            {
                if (BeforeSearchFolder == null)
                {
                    BeforeSearchFolder = Explorer.NavigationLog.CurrentLocation;
                }
            }
            else
            {
                BeforeSearchFolder = Explorer.NavigationLog.CurrentLocation;
            }

            if (SearchCriteria != "")
            {

                backgroundSearchThread = new Thread(new ParameterizedThreadStart(DoSimpleSearch));
                backgroundSearchThread.IsBackground = true;
                // ApartmentState.STA is required for COM
                backgroundSearchThread.SetApartmentState(ApartmentState.STA);
                backgroundSearchThread.Start(SearchCriteria);

            }
        }
        private void searchTextBox1_Search(object sender, RoutedEventArgs e)
        {

            //DoSearch(searchTextBox1.Text);
            //StatusBar.UpdateLayout();

        }
        int searchcicles = 0;
        //int BeforeSearcCicles = 0;
        int CurrentProgressValue = 0;
        #endregion

        #region Change Ribbon Color (Theme)

        public void ChangeRibbonThemeL(string ThemeName)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, (ThreadStart)(() =>
            {

                Resources.BeginInit();
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Fluent;component/Themes/Office2010/" + ThemeName + ".xaml") });
                Resources.MergedDictionaries.RemoveAt(0);
                Resources.EndInit();
            }));
        }

        private void btnSilver_Click(object sender, RoutedEventArgs e)
        {
            ChangeRibbonThemeL("Silver");
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CurrentTheme", "Silver");
            rks.Close();
            KeepBackstageOpen = true;
        }

        public void ChangeRibbonTheme(string ThemeName)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, (ThreadStart)(() =>
            {
                Application.Current.Resources.BeginInit();
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Fluent;component/Themes/Office2010/" + ThemeName + ".xaml") });
                Application.Current.Resources.MergedDictionaries.RemoveAt(0);
                Application.Current.Resources.EndInit();
            }));
        }

        private void btnBlue_Click(object sender, RoutedEventArgs e)
        {
            ChangeRibbonThemeL("Blue");
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CurrentTheme", "Blue");
            rks.Close();
            KeepBackstageOpen = true;
        }

        private void btnBlack_Click(object sender, RoutedEventArgs e)
        {
            ChangeRibbonThemeL("Black");
            btnBlack.IsChecked = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CurrentTheme", "Black");
            rks.Close();
            KeepBackstageOpen = true;
        }

        private void btnGreen_Click(object sender, RoutedEventArgs e)
        {
            ChangeRibbonThemeL("Green");
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CurrentTheme", "Green");
            rks.Close();
            KeepBackstageOpen = true;
        }

        #endregion

        #region Archive Commands

        //private void CreateArchive_Show(OutArchiveFormat format) 
        //{
        //    var selectedItems = new List<string>();
        //    foreach(ShellObject item in Explorer.SelectedItems)
        //    {
        //        if (Directory.Exists(item.ParsingName))
        //        {
        //            DirectoryInfo di = new DirectoryInfo(item.ParsingName);
        //            FileInfo[] Files = di.GetFiles("*", SearchOption.AllDirectories);
        //            foreach (FileInfo fi in Files)
        //            {
        //                selectedItems.Add(fi.FullName);
        //            }

        //        }
        //        else
        //        {
        //            selectedItems.Add(item.ParsingName);
        //        }

        //    }
        //    if (selectedItems.Count > 0)
        //    {
        //        try
        //        {
        //            var CAI = new CreateArchive(selectedItems,
        //                                        true,
        //                                        Path.GetDirectoryName(selectedItems[0]),
        //                                        format);

        //            CAI.Show(this.GetWin32Window());


        //        }
        //        catch (Exception exception)
        //        {
        //            var dialog = new TaskDialog();
        //            dialog.StandardButtons = TaskDialogStandardButtons.Ok;
        //            dialog.Text = exception.Message;
        //            dialog.Show();
        //        }
        //    }
        // }

        private void ExtractFiles()
        {
            var selectedItems = new List<string>();
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                selectedItems.Add(item.ParsingName);
            }
            try
            {
                var CAI = new CreateArchive(selectedItems,
                                            false,
                                            Explorer.SelectedItems[0].ParsingName);

                CAI.Show(this.GetWin32Window());


            }
            catch (Exception exception)
            {
                var dialog = new TaskDialog();
                dialog.StandardButtons = TaskDialogStandardButtons.Ok;
                dialog.Text = exception.Message;
                dialog.Show();
            }
        }

        private void btnExtractNow_Click(object sender, RoutedEventArgs e)
        {
            if (chkUseNewFolder.IsChecked == true)
            {
                string OutputLoc = txtExtractLocation.Text + "\\" + RemoveExtensionsFromFile(Explorer.SelectedItems[0].Name, new FileInfo(Explorer.SelectedItems[0].ParsingName).Extension);
                try
                {
                    Directory.CreateDirectory(OutputLoc);
                    ExtractToLocation(SelectedArchive, OutputLoc);
                }
                catch (Exception)
                {
                    MessageBoxResult wtd = MessageBox.Show("The directory " + OutputLoc + " already exists. Would you like for BetterExplorer to extract there instead?", "Folder Exists", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    switch (wtd)
                    {
                        case MessageBoxResult.Cancel:
                            break;
                        case MessageBoxResult.No:
                            break;
                        case MessageBoxResult.None:
                            break;
                        case MessageBoxResult.OK:
                            break;
                        case MessageBoxResult.Yes:
                            ExtractToLocation(SelectedArchive, OutputLoc);
                            break;
                        default:
                            break;
                    }
                }

            }
            else
            {
                ExtractToLocation(SelectedArchive, txtExtractLocation.Text);
            }

        }

        private void btnChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtExtractLocation.Text = dlg.FileName;
            }
        }

        private void ExtractToLocation(string archive, string output)
        {
            var selectedItems = new List<string>() { archive };

            var archiveProcressScreen = new ArchiveProcressScreen(selectedItems,
                               output,
                               ArchiveAction.Extract,
                               "");
            archiveProcressScreen.ExtractionCompleted += new ArchiveProcressScreen.ExtractionCompleteEventHandler(ExtractionHasCompleted);

            AddToLog("Archive Extracted to " + output + " from source " + archive);

            archiveProcressScreen.Show();
        }

        private void ExtractionHasCompleted(object sender, ArchiveEventArgs e)
        {
            if (chkOpenResults.IsChecked == true)
            {
                NewTab(e.OutputLocation);
            }
        }

        //private void miCreateZIP_Click(object sender, RoutedEventArgs e)
        //{
        //    CreateArchive_Show(OutArchiveFormat.Zip);
        //}

        //private void miCreate7z_Click(object sender, RoutedEventArgs e)
        //{
        //    CreateArchive_Show(OutArchiveFormat.SevenZip);
        //}

        //private void miCreateCAB_Click(object sender, RoutedEventArgs e)
        //{
        //    CreateArchive_Show(OutArchiveFormat.Tar);
        //}

        private void miExtractToLocation_Click(object sender, RoutedEventArgs e)
        {
            ExtractFiles();
        }

        private void miExtractHere_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                              System.Windows.Threading.DispatcherPriority.Background,
                              new ThreadStart(
                                delegate()
                                {

                                    string FileName = Explorer.SelectedItems[0].ParsingName;
                                    SevenZipExtractor extractor = new SevenZipExtractor(FileName);
                                    string DirectoryName = System.IO.Path.GetDirectoryName(FileName);
                                    string ArchName = System.IO.Path.GetFileNameWithoutExtension(FileName);
                                    extractor.Extracting += new EventHandler<ProgressEventArgs>(extractor_Extracting);
                                    extractor.ExtractionFinished += new EventHandler<EventArgs>(extractor_ExtractionFinished);
                                    extractor.FileExtractionStarted += new EventHandler<FileInfoEventArgs>(extractor_FileExtractionStarted);
                                    extractor.FileExtractionFinished += new EventHandler<FileInfoEventArgs>(extractor_FileExtractionFinished);
                                    extractor.PreserveDirectoryStructure = true;
                                    string Separator = "";
                                    if (DirectoryName[DirectoryName.Length - 1] != Char.Parse(@"\"))
                                    {
                                        Separator = @"\";
                                    }
                                    AddToLog("Extracted Archive to " + DirectoryName + Separator + ArchName + " from source " + FileName);
                                    extractor.BeginExtractArchive(DirectoryName + Separator + ArchName);
                                }));
        }

        void extractor_FileExtractionFinished(object sender, FileInfoEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void extractor_FileExtractionStarted(object sender, FileInfoEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void extractor_ExtractionFinished(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if ((sender as SevenZipExtractor) != null)
            {
                (sender as SevenZipExtractor).Dispose();
            }

        }

        void extractor_Extracting(object sender, ProgressEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void btnExtract_Click(object sender, RoutedEventArgs e)
        {
            miExtractHere_Click(sender, e);
        }

        private void btnCheckArchive_Click(object sender, RoutedEventArgs e)
        {
            Thread trIntegrityCheck = new Thread(new ThreadStart(DoCheck));
            trIntegrityCheck.Start();
        }


        private void DoCheck()
        {

            string FileName = Explorer.SelectedItems[0].ParsingName;
            SevenZipExtractor extractor = new SevenZipExtractor(FileName);
            if (!extractor.Check())
                MessageBox.Show("Not Pass");
            else
                MessageBox.Show("Pass");

            extractor.Dispose();
        }

        private void btnViewArchive_Click(object sender, RoutedEventArgs e)
        {
            //var name = Explorer.SelectedItems.First().ParsingName;
            //var archiveDetailView = new ArchiveDetailView(ICON_DLLPATH, name);
            //archiveDetailView.Show(this.GetWin32Window());

            ArchiveViewWindow g = new ArchiveViewWindow(Explorer.SelectedItems[0], IsPreviewPaneEnabled, IsInfoPaneEnabled);
            g.Show();
        }

        private void btnCreateArchive_Click(object sender, RoutedEventArgs e)
        {
            ArchiveCreateWizard acw = new ArchiveCreateWizard(Explorer.SelectedItems, Explorer.NavigationLog.CurrentLocation.ParsingName);
            acw.Show();
            AddToLog("Archive Creation Wizard begun. Current location: " + Explorer.NavigationLog.CurrentLocation.ParsingName);
        }

        #endregion

        #region Library Commands

        private void btnOLItem_Click(object sender, RoutedEventArgs e)
        {

            ShellLibrary lib = null;
            if (Explorer.GetSelectedItemsCount() == 1)
            {
                lib = ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
            }
            else
            {
                lib = ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
            }
            switch ((sender as MenuItem).Tag.ToString())
            {
                case "gu":
                    lib.LibraryType = LibraryFolderType.Generic;
                    lib.Close();
                    break;
                case "doc":
                    lib.LibraryType = LibraryFolderType.Documents;
                    lib.Close();
                    break;
                case "pic":
                    lib.LibraryType = LibraryFolderType.Pictures;
                    lib.Close();
                    break;
                case "vid":
                    lib.LibraryType = LibraryFolderType.Videos;
                    lib.Close();
                    break;
                case "mu":
                    lib.LibraryType = LibraryFolderType.Music;
                    lib.Close();
                    break;
                default:
                    break;
            }
        }

        private void chkPinNav_Checked(object sender, RoutedEventArgs e)
        {
            ShellLibrary lib = null;
            if (Explorer.GetSelectedItemsCount() == 1)
            {
                lib = ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
            }
            else
            {
                lib = ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
            }
            if (!IsFromSelectionOrNavigation)
            {
                lib.IsPinnedToNavigationPane = true;
            }

            lib.Close();
        }

        private void chkPinNav_Unchecked(object sender, RoutedEventArgs e)
        {
            ShellLibrary lib = null;
            if (Explorer.GetSelectedItemsCount() == 1)
            {
                lib = ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
            }
            else
            {
                lib = ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
            }
            if (!IsFromSelectionOrNavigation)
            {
                lib.IsPinnedToNavigationPane = false;
            }

            lib.Close();
        }

        private void btnChangeLibIcon_Click(object sender, RoutedEventArgs e)
        {
            IconView iv = new IconView();

            iv.LoadIcons(Explorer, true);
        }

        private void btnManageLib_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShellLibrary.ShowManageLibraryUI(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default),
                    this.Handle, "Choose which folders will be in this library", "A library gathers content from all of the folders listed below and puts it all in one window for you to see.", true);
            }
            catch
            {

                ShellLibrary.ShowManageLibraryUI(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default),
                    this.Handle, "Choose which folders will be in this library", "A library gathers content from all of the folders listed below and puts it all in one window for you to see.", true);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            TaskDialog td = new TaskDialog();
            td.Caption = "Reset Library";
            td.Icon = TaskDialogStandardIcon.Warning;
            td.Text = "Click \"OK\" to reset currently selected library properties";
            td.InstructionText = "Reset Library Properties?";
            td.FooterIcon = TaskDialogStandardIcon.Information;
            //td.FooterText = "This will reset all the properties to default properties for library type";
            td.DetailsCollapsedLabel = "More info";
            td.DetailsExpandedLabel = "More Info";
            td.DetailsExpandedText = "This will reset all the properties to default properties for library type";
            td.DetailsExpanded = false;
            td.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;
            td.StandardButtons = TaskDialogStandardButtons.Ok | TaskDialogStandardButtons.Cancel;
            td.OwnerWindowHandle = this.Handle;
            if (td.Show() == TaskDialogResult.Ok)
            {
                if (Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default).ToLowerInvariant() != "documents" &&
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default).ToLowerInvariant() != "music" &&
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default).ToLowerInvariant() != "videos" &&
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default).ToLowerInvariant() != "pictures")
                {
                    ShellLibrary lib = ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default),
                                false);
                    lib.IsPinnedToNavigationPane = true;
                    lib.LibraryType = LibraryFolderType.Generic;
                    lib.IconResourceId = new IconReference(@"C:\Windows\System32\imageres.dll", 187);
                    lib.Close();
                }
            }
        }

        #endregion

        #region Navigation (Back/Forward Arrows) and Up Button

        private void leftNavBut_Click(object sender, RoutedEventArgs e)
        {
            isGoingBackOrForward = true;
            Explorer.Navigate((tabControl1.Items[CurrentTabIndex] as CloseableTabItem).log.NavigateBack());
        }

        private void rightNavBut_Click(object sender, RoutedEventArgs e)
        {
            isGoingBackOrForward = true;
            Explorer.Navigate((tabControl1.Items[CurrentTabIndex] as CloseableTabItem).log.NavigateForward());
        }

        private void downArrow_Click(object sender, RoutedEventArgs e)
        {
            cmHistory.Items.Clear();
            cmHistory.Opened += new RoutedEventHandler(cmHistory_Opened);
            cmHistory.Closed += new RoutedEventHandler(cmHistory_Closed);

            NavigationLog nl = (tabControl1.Items[CurrentTabIndex] as CloseableTabItem).log;
            int i = 0;
            foreach (ShellObject item in nl.HistoryItemsList)
            {

                if (item != null)
                {
                    item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                    item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                    MenuItem mi = new MenuItem();
                    mi.Header = item.GetDisplayName(DisplayNameType.Default);
                    mi.Tag = item;
                    mi.Icon = item.Thumbnail.BitmapSource;
                    mi.IsCheckable = true;
                    mi.IsChecked = (i == nl.CurrentLocPos);
                    mi.GroupName = "G1";
                    mi.Click += new RoutedEventHandler(miItems_Click);
                    cmHistory.Items.Add(mi);
                }
                i++;
            }

            cmHistory.Placement = PlacementMode.Bottom;
            cmHistory.PlacementTarget = navBarGrid;
            cmHistory.IsOpen = true;
        }

        void miItems_Click(object sender, RoutedEventArgs e)
        {
            ShellObject item = (ShellObject)(sender as MenuItem).Tag;
            if (item != null)
            {
                isGoingBackOrForward = true;
                NavigationLog nl = (tabControl1.Items[CurrentTabIndex] as CloseableTabItem).log;
                (sender as MenuItem).IsChecked = true;
                nl.CurrentLocPos = cmHistory.Items.IndexOf((sender as MenuItem));
                Explorer.Navigate(item);
            }
        }

        private void btnUpLevel_Click(object sender, RoutedEventArgs e)
        {
            Explorer.Navigate(Explorer.NavigationLog.CurrentLocation.Parent);
            WindowsAPI.SetFocus(ExplorerBrowser.SysListViewHandle);
            //ShellContextMenu cm = new ShellContextMenu();
            //FileInfo fi = new FileInfo(Explorer.SelectedItems[0].ParsingName);
            //FileInfo[] fii = new FileInfo[1];
            //fii[0] = fi;
            //cm.ShowContextMenu(Handle, fii, GetMousePosition());
            


        }


        #endregion

        #region View Tab/Status Bar

        private void btnMoreColls_Click(object sender, RoutedEventArgs e)
        {
            using (MoreColumns fMoreCollumns = new MoreColumns())
            {
                fMoreCollumns.PopulateAvailableColumns(Explorer.AvailableColumns(Explorer.GetShellView(), true), Explorer,
                    this.PointToScreen(Mouse.GetPosition(this)));
            }
        }

        void mig_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            Explorer.SetGroupCollumn(((Collumns)item.Tag).pkey, true);
        }

        private void btnAutosizeColls_Click(object sender, RoutedEventArgs e)
        {
            Explorer.SetAutoSizeColumns();
        }

        private void btnSbDetails_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Details)
            {
                btnSbDetails.IsChecked = true;
            }
            else
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Details;
        }

        private void btnSbIcons_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Icon)
            {
                btnSbIcons.IsChecked = true;
            }
            else
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Icon;
        }

        private void btnSbTiles_Click(object sender, RoutedEventArgs e)
        {

            if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Tile)
            {
                btnSbTiles.IsChecked = true;
            }
            else
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Tile;

        }

        private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsCalledFromLoading && !IsCalledFromViewEnum)
            {
                Dispatcher.BeginInvoke(
                              new Action(
                                delegate()
                                {
                                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
                                    Explorer.ContentOptions.ThumbnailSize = (int)e.NewValue;
                                }));

            }
            IsCalledFromLoading = false;
            IsCalledFromViewEnum = false;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Explorer.RefreshExplorer();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            //DoubleAnimation da = new DoubleAnimation(100, new Duration(new TimeSpan(0, 0, 2)));
            //da.FillBehavior = FillBehavior.Stop;
            //breadcrumbBar1.BeginAnimation(Odyssey.Controls.BreadcrumbBar.ProgressValueProperty, da);
            //Explorer.HideFocusRectangle();


            //For some reason, this was giving off an error.

            //ChangePaneVisibility(0x1, true);
            //Explorer.ShowPropPage(this.Handle, Explorer.SelectedItems[0].ParsingName, "Security");



            //Explorer.RefreshExplorer();


            //resets Refresh image back to normal
            //except I can't get it to work (it's probably just a small error I made, but I don't have time to fix right now.
            //BitmapImage go = new BitmapImage(new Uri("\\Images\\Refresh16.png", UriKind.Relative));
            //RefreshButtonImage.Source = go;
        }

        void mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            MenuItem ascitem = (MenuItem)btnSort.Items[btnSort.Items.IndexOf(misa)];
            if (ascitem.IsChecked)
            {
                Explorer.SetSortCollumn(((Collumns)item.Tag).pkey, ExplorerBrowser.SORT.ASCENDING);
            }
            else
            {
                Explorer.SetSortCollumn(((Collumns)item.Tag).pkey, ExplorerBrowser.SORT.DESCENDING);
            }

        }

        void misng_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            item.IsChecked = true;
            ExplorerBrowser.PROPERTYKEY pk = new ExplorerBrowser.PROPERTYKEY();
            pk.fmtid = new Guid("00000000-0000-0000-0000-000000000000");
            pk.pid = 0;
            Explorer.SetGroupCollumn(pk, true);
        }


        private void inRibbonGallery1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            switch (inRibbonGallery1.SelectedIndex)
            {
                case 0:
                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
                    Explorer.ContentOptions.ThumbnailSize = 256;
                    break;
                case 1:
                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
                    Explorer.ContentOptions.ThumbnailSize = 96;
                    break;
                case 2:
                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
                    Explorer.ContentOptions.ThumbnailSize = 64;
                    break;
                case 3:
                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
                    Explorer.ContentOptions.ThumbnailSize = 48;
                    break;
                case 4:

                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.List;
                    break;
                case 5:

                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Details;
                    break;
                case 6:

                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Tile;
                    break;
                case 7:
                    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Content;
                    break;
                default:
                    break;
            }

        }

        private void chkHiddenFiles_Checked(object sender, RoutedEventArgs e)
        {

            if (!IsCalledFromLoading)
            {
                Dispatcher.BeginInvoke(
                              new Action(
                                delegate()
                                {
                                    WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                    state.fShowAllObjects = 1;
                                    WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWALLOBJECTS, true);
                                    Explorer.RefreshExplorer();
                                }
                ));
            };
        }

        private void chkHiddenFiles_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsCalledFromLoading)
            {
                Dispatcher.BeginInvoke(
                              new Action(
                                delegate()
                                {
                                    WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                    state.fShowAllObjects = 0;
                                    WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWALLOBJECTS, true);
                                    Explorer.RefreshExplorer();
                                }
                ));
            }
        }

        private void chkExtensions_Checked(object sender, RoutedEventArgs e)
        {

            if (!IsCalledFromLoading)
            {
                Dispatcher.BeginInvoke(
                              new Action(
                                delegate()
                                {
                                    WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                    state.fShowExtensions = 1;
                                    WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWEXTENSIONS, true);
                                    Explorer.RefreshExplorer();
                                }
                ));
            }
        }

        private void chkExtensions_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsCalledFromLoading)
            {
                Dispatcher.BeginInvoke(
                              new Action(
                                delegate()
                                {
                                    WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                    state.fShowExtensions = 0;
                                    WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWEXTENSIONS, true);
                                    Explorer.RefreshExplorer();
                                }
                ));
            }
        }

        #endregion

        #region Hide items

        private void btnHideSelItems_Click(object sender, RoutedEventArgs e)
        {

            ShellObjectCollection SelItems = Explorer.SelectedItems;
            pd = new IProgressDialog(this.Handle);
            pd.Title = "Applying attributes";
            pd.CancelMessage = "Please wait while the operation is cancelled";
            pd.Maximum = (uint)SelItems.Count;
            pd.Value = 0;
            pd.Line1 = "Applying attributes to:";
            pd.Line3 = "Calculating Time Remaining...";
            pd.ShowDialog(IProgressDialog.PROGDLG.Normal, IProgressDialog.PROGDLG.AutoTime, IProgressDialog.PROGDLG.NoMinimize);
            Thread hthread = new Thread(new ParameterizedThreadStart(DoHideShow));
            hthread.Start(SelItems);

        }
        bool IsHidingUserCancel = false;
        public void DoHideShowWithChilds(object o)
        {
            IsHidingUserCancel = false;
            ShellObjectCollection SelItems = o as ShellObjectCollection;
            List<string> ItemsToHideShow = new List<string>();
            foreach (ShellObject obj in SelItems)
            {
                ItemsToHideShow.Add(obj.ParsingName);
                if (Directory.Exists(obj.ParsingName))
                {
                    DirectoryInfo di = new DirectoryInfo(obj.ParsingName);
                    DirectoryInfo[] dirs = di.GetDirectories("*", SearchOption.AllDirectories);
                    FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
                    foreach (DirectoryInfo dir in dirs)
                    {
                        ItemsToHideShow.Add(dir.FullName);
                    }
                    foreach (FileInfo file in files)
                    {
                        ItemsToHideShow.Add(file.FullName);
                    }
                }

            }

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {

                pd.Maximum = (uint)ItemsToHideShow.Count;

            }));

            foreach (string item in ItemsToHideShow)
            {
                try
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        if (pd.HasUserCancelled)
                        {
                            IsHidingUserCancel = true;

                        }
                    }));

                    if (IsHidingUserCancel)
                    {
                        break;
                    }

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {

                        pd.Line2 = item;

                    }));

                    FileAttributes attr = File.GetAttributes(item);

                    if ((attr & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        File.SetAttributes(item, attr & ~FileAttributes.Hidden);
                    }
                    else
                    {
                        File.SetAttributes(item, attr | FileAttributes.Hidden);
                    }

                }
                finally
                {

                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {

                    pd.Value++;

                }));

            }
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {

                pd.CloseDialog();
            }));
        }
        public void DoHideShow(object o)
        {
            ShellObjectCollection SelItems = o as ShellObjectCollection;
            IsHidingUserCancel = false;
            foreach (ShellObject item in SelItems)
            {
                try
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        if (pd.HasUserCancelled)
                        {
                            IsHidingUserCancel = true;

                        }
                    }));

                    if (IsHidingUserCancel)
                    {
                        break;
                    }

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {

                        pd.Line2 = item.ParsingName;

                    }));

                    FileAttributes attr = File.GetAttributes(item.ParsingName);

                    if ((attr & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        File.SetAttributes(item.ParsingName, attr & ~FileAttributes.Hidden);
                    }
                    else
                    {
                        File.SetAttributes(item.ParsingName, attr | FileAttributes.Hidden);
                    }

                }
                finally
                {

                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {

                    pd.Value++;

                }));

            }
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {

                pd.CloseDialog();
            }));
        }

        private void miHideItems_Click(object sender, RoutedEventArgs e)
        {
            ShellObjectCollection SelItems = Explorer.SelectedItems;
            pd = new IProgressDialog(this.Handle);
            pd.Title = "Applying attributes";
            pd.CancelMessage = "Please wait while the operation is cancelled";
            pd.Maximum = 100;
            pd.Value = 0;
            pd.Line1 = "Applying attributes to:";
            pd.Line3 = "Calculating Time Remaining...";
            pd.ShowDialog(IProgressDialog.PROGDLG.Normal, IProgressDialog.PROGDLG.AutoTime, IProgressDialog.PROGDLG.NoMinimize);
            Thread hthread = new Thread(new ParameterizedThreadStart(DoHideShowWithChilds));
            hthread.Start(SelItems);
        }

        #endregion

        #region Share Tab Commands (excluding Archive)

        private void btnMapDrive_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.WNetConnectionDialog(this.Handle, 1);
        }

        private void btnDisconectDrive_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.WNetDisconnectDialog(this.Handle, 1);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            //ShareWizzard sw = new ShareWizzard();
            //sw.CurrentPath = Explorer.SelectedItems[0].ParsingName;
            //sw.SetIcon(Explorer.SelectedItems[0]);
            //sw.ShowDialog(this.GetWin32Window());
            //sw.Close();
            Explorer.OpenShareUI();
        }

        private void btnAdvancedSecurity_Click(object sender, RoutedEventArgs e)
        {
            Explorer.ShowPropPage(this.Handle, Explorer.SelectedItems[0].ParsingName, "Security");
        }

        #endregion

        #region Change Language

        //private void btnchangeLocale_Click(object sender, RoutedEventArgs e)
        //{
        //    RegistryKey rk = Registry.CurrentUser;
        //    RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

        //    rks.SetValue(@"Locale", txtLocale.Text);

        //    SelectCulture(txtLocale.Text);

        //    rks.Close();
        //    rk.Close();
        //}

        public void ChangeLocale(string locale)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

            rks.SetValue(@"Locale", locale);

            SelectCulture(locale);

            rks.Close();
            rk.Close();
        }

        /// <summary>
        /// Sets the UI language
        /// </summary>
        /// <param name="culture">Language code (ex. "en-EN")</param>
        public static void SelectCulture(string culture)
        {
            // List all our resources      
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }
            // We want our specific culture      
            string requestedCulture = string.Format("Locale.{0}.xaml", culture);
            ResourceDictionary resourceDictionary =
                dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
            if (resourceDictionary == null)
            {
                // If not found, we select our default language        
                //        
                requestedCulture = "DefaultLocale.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
            }

            // If we have the requested resource, remove it from the list and place at the end.\      
            // Then this language will be our string table to use.      
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }
            // Inform the threads of the new culture      

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }

        private void TranslationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReadyToChangeLanguage == true)
            {
                ChangeLocale(((TranslationComboBoxItem)e.AddedItems[0]).LocaleCode);
                lblLocale.Visibility = Visibility.Visible;
            }
        }

        private void btnRemoveLangSetting_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

            rks.DeleteValue(@"Locale");

            rks.Close();
            rk.Close();
        }

        private void TranslationHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(TranslationURL.Text);
        }

        #endregion

        #region Image Editing

        private string RemoveExtensionsFromFile(string file, string ext)
        {
            if (file.EndsWith(ext) == true)
            {
                return file.Remove(file.LastIndexOf(ext), ext.Length);
            }
            else
            {
                return file;
            }
        }

        private void ConvertImage(ImageFormat format, string name, string extension)
        {
            System.Drawing.Bitmap cvt = new Bitmap(name);
            string namen = RemoveExtensionsFromFile(name, new System.IO.FileInfo(name).Extension);
            try
            {
                AddToLog("Converted Image from " + name + " to new file " + namen + extension);
                cvt.Save(namen + extension, format);
            }
            catch (Exception)
            {
                MessageBox.Show("There appears to have been an issue with converting the file. Make sure the filename \"" + RemoveExtensionsFromFile(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), new System.IO.FileInfo(name).Extension) + extension + "\" does already not exist.", "Conversion Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            cvt.Dispose();
        }

        private void ConvertToJPG_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Jpeg, item.ParsingName, ".jpg");
            }
        }

        private void ConvertToPNG_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Png, item.ParsingName, ".png");
            }
        }

        private void ConvertToGIF_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Gif, item.ParsingName, ".gif");
            }
        }

        private void ConvertToBMP_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Bmp, item.ParsingName, ".bmp");
            }
        }

        private void ConvertToWMF_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Wmf, item.ParsingName, ".wmf");
            }
        }

        private void btnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            //Dispatcher.BeginInvoke(
            //                  System.Windows.Threading.DispatcherPriority.Background,
            //                  new Action(
            //                    delegate()
            //                    {
            //                        ImageFormat imagef = GetImageFormat(Explorer.SelectedItems[0].ParsingName);
            //                        RotateAndSaveImage(Explorer.SelectedItems[0].ParsingName,
            //                            Explorer.SelectedItems[0].ParsingName, imagef, RotateFlipType.Rotate270FlipNone);
            //                    }));
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                cvt.RotateFlip(RotateFlipType.Rotate270FlipNone);
                cvt.Save(item.ParsingName);
                cvt.Dispose();
                AddToLog("Rotated image " + item.ParsingName);
            }

        }

        private void btnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            //Dispatcher.BeginInvoke(
            //                  System.Windows.Threading.DispatcherPriority.Background,
            //                  new Action(
            //                    delegate()
            //                    {
            //                        ImageFormat imagef = GetImageFormat(Explorer.SelectedItems[0].ParsingName);
            //                        RotateAndSaveImage(Explorer.SelectedItems[0].ParsingName,
            //                            Explorer.SelectedItems[0].ParsingName, imagef, RotateFlipType.Rotate90FlipNone);
            //                    }));
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                cvt.RotateFlip(RotateFlipType.Rotate90FlipNone);
                cvt.Save(item.ParsingName);
                cvt.Dispose();
                AddToLog("Rotated image " + item.ParsingName);
            }
        }

        private void btnWallpaper_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Stretched);
            }));
        }

        private void miWallFill_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Fill);
            }));
        }

        private void miWallFit_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Fit);
            }));
        }

        private void miWallStretch_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Stretched);
            }));
        }

        private void miWallTile_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Tiled);
            }));
        }

        private void miWallCenter_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Centered);
            }));
        }

        private void btnFlipX_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                cvt.RotateFlip(RotateFlipType.RotateNoneFlipX);
                cvt.Save(item.ParsingName);
                cvt.Dispose();
                AddToLog("Flipped image " + item.ParsingName);
            }
        }

        private void btnFlipY_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                cvt.RotateFlip(RotateFlipType.RotateNoneFlipY);
                cvt.Save(item.ParsingName);
                cvt.Dispose();
                AddToLog("Flipped image " + item.ParsingName);
            }
        }

        private void btnResize_Click(object sender, RoutedEventArgs e)
        {
            ResizeImage ri = new ResizeImage(Explorer.SelectedItems[0]);
            ri.ShowDialog();
            if (ri.Confirm == true)
            {
                System.Drawing.Bitmap cvt = new Bitmap(Explorer.SelectedItems[0].ParsingName);
                System.Drawing.Bitmap cst = ChangeImageSize(cvt, ri.newwidth, ri.newheight);

                string ext = GetExtension(Explorer.SelectedItems[0].ParsingName);

                cst.Save(Explorer.SelectedItems[0].ParsingName + " (" + ri.newwidth + " X " + ri.newheight + ")" + ext);
                cvt.Dispose();
                cst.Dispose();
            }
        }

        private string GetExtension(string file)
        {
            return file.Substring(file.LastIndexOf("."));
        }

        private Bitmap ChangeImageSize(Bitmap img, int width, int height)
        {
            Bitmap bm_dest = new Bitmap(width, height);
            Graphics gr_dest = Graphics.FromImage(bm_dest);
            gr_dest.DrawImage(img, 0, 0, bm_dest.Width + 1, bm_dest.Height + 1);
            return bm_dest;
        }

        #endregion

        #region Folder Tools Commands
        private void btnChangeFoldericon_Click(object sender, RoutedEventArgs e)
        {
            IconView iv = new IconView();

            iv.LoadIcons(Explorer, false);
        }

        private void btnClearFoldericon_Click(object sender, RoutedEventArgs e)
        {
            Explorer.ClearFolderIcon(Explorer.SelectedItems[0].ParsingName);
        }
        #endregion

        #region Registry Setting Changes / BetterExplorerOperations Calls / Action Log

        private void btnSetCurrentasStartup_Click(object sender, RoutedEventArgs e)
        {
            string CurrentLocString = Explorer.NavigationLog.CurrentLocation.ParsingName;
            StartUpLocation = CurrentLocString;
            btnSetCurrentasStartup.Header = Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default);
            btnSetCurrentasStartup.Icon = Explorer.NavigationLog.CurrentLocation.Thumbnail.BitmapSource;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"StartUpLoc", CurrentLocString);
            rks.Close();
            rk.Close();
            KeepBackstageOpen = true;
        }

        private void RibbonWindow_Deactivated(object sender, EventArgs e)
        {
            //if (Itmpop.Visibility == System.Windows.Visibility.Visible)
            //{
            //    Itmpop.Visibility = System.Windows.Visibility.Hidden;
            //}
            //backstage.IsOpen = false;
            ExplorerBrowser.IsBool = true;
        }

        private void chkIsFlyout_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"HFlyoutEnabled", 1);
                IsHFlyoutEnabled = true;
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsFlyout_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"HFlyoutEnabled", 0);
                IsHFlyoutEnabled = false;
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsInfoPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"InfoPaneEnabled", 1);
                IsInfoPaneEnabled = true;
                ChangePaneVisibility(0x1, true);
                //Explorer.Navigate(Explorer.NavigationLog.CurrentLocation);
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsInfoPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"InfoPaneEnabled", 0);
                IsInfoPaneEnabled = false;
                Explorer.NavigationOptions.PaneVisibility.Details = PaneVisibilityState.Hide;
                //Explorer.Navigate(Explorer.NavigationLog.CurrentLocation);
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsPreviewPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"PreviewPaneEnabled", 0);
                IsPreviewPaneEnabled = false;
                Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide;
                //Explorer.Navigate(Explorer.NavigationLog.CurrentLocation);
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsPreviewPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"PreviewPaneEnabled", 1);
                IsPreviewPaneEnabled = true;
                Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Show;
                //Explorer.SetState();
                //Explorer.Navigate(Explorer.NavigationLog.CurrentLocation);
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsDefault_Checked(object sender, RoutedEventArgs e)
        {


            if (!isOnLoad)
            {
                String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                string dir = Path.GetDirectoryName(CurrentexePath);
                string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");
                Process proc = new Process();
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Verb = "runas",
                    UseShellExecute = true,
                    Arguments = "/env /user:" + "Administrator " + "\"" + ExePath + "\"",
                };
                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                int h = (int)WindowsAPI.getWindowId(null, "BetterExplorerOperations");
                int jj = WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"),
                    0, "0x77654");
                proc.WaitForExit();
            }

        }
        public const int WM_COPYDATA = 0x4A;
        private void chkIsDefault_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                string dir = Path.GetDirectoryName(CurrentexePath);
                string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");
                Process proc = new Process();
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Verb = "runas",
                    UseShellExecute = true,
                    Arguments = "/env /user:" + "Administrator " + "\"" + ExePath + "\"",
                };
                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"),
                     0, "0x77655");
                proc.WaitForExit();
            }
        }

        private void chkIsTerraCopyEnabled_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"FileOpExEnabled", 1);
                IsExtendedFileOpEnabled = true;
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsTerraCopyEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"FileOpExEnabled", 0);
                IsExtendedFileOpEnabled = false;
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsCompartibleRename_Checked(object sender, RoutedEventArgs e)
        {
            IsCompartibleRename = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CompartibleRename", 1);
            rks.Close();
            rk.Close();
        }

        private void chkIsCompartibleRename_Unchecked(object sender, RoutedEventArgs e)
        {
            IsCompartibleRename = false;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CompartibleRename", 0);
            rks.Close();
            rk.Close();
        }

        private void chkIsRestoreTabs_Checked(object sender, RoutedEventArgs e)
        {
            IsrestoreTabs = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"IsRestoreTabs", 1);
            rks.Close();
            rk.Close();
        }

        private void chkIsRestoreTabs_Unchecked(object sender, RoutedEventArgs e)
        {
            IsrestoreTabs = false;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"IsRestoreTabs", 0);
            rks.Close();
            rk.Close();
        }

        private void gridSplitter1_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"SearchBarWidth", SearchBarColumn.Width.Value);
            rks.Close();
            rk.Close();
        }

        private void SearchBarReset_Click(object sender, RoutedEventArgs e)
        {
            SearchBarColumn.Width = new GridLength(220);
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"SearchBarWidth", SearchBarColumn.Width.Value);
            rks.Close();
            rk.Close();
        }

        private void chkShowCheckBoxes_Checked(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CheckModeEnabled", 1);
            isCheckModeEnabled = true;
            Explorer.ContentOptions.CheckSelect = true;
            rk.Close();
            rks.Close();
        }

        private void chkShowCheckBoxes_Unchecked(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CheckModeEnabled", 0);
            isCheckModeEnabled = false;
            Explorer.ContentOptions.CheckSelect = false;
            rk.Close();
            rks.Close();
        }

        private void chkTreeExpand_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                string dir = Path.GetDirectoryName(CurrentexePath);
                string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");
                Process proc = new Process();
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Verb = "runas",
                    UseShellExecute = true,
                    Arguments = "/env /user:" + "Administrator " + "\"" + ExePath + "\"",
                };
                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                int h = (int)WindowsAPI.getWindowId(null, "BetterExplorerOperations");
                int jj = WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"),
                    0, "0x88775");
                proc.WaitForExit();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SymLinkInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string lpDestination;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string lpTarget;
            public int SymType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string lpMsg;

        };

        private void chkTreeExpand_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                string dir = Path.GetDirectoryName(CurrentexePath);
                string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");
                Process proc = new Process();
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Verb = "runas",
                    UseShellExecute = true,
                    Arguments = "/env /user:" + "Administrator " + "\"" + ExePath + "\"",
                };
                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                int h = (int)WindowsAPI.getWindowId(null, "BetterExplorerOperations");
                int jj = WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"),
                    0, "0x88776");
                proc.WaitForExit();
            }
        }


        private void chkLogHistory_Checked(object sender, RoutedEventArgs e)
        {
            canlogactions = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"EnableActionLog", 1);
            rks.Close();
            rk.Close();
        }

        private void chkLogHistory_Unchecked(object sender, RoutedEventArgs e)
        {
            canlogactions = false;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"EnableActionLog", 0);
            rks.Close();
            rk.Close();
        }

        private void AddToLog(string value)
        {
            try
            {
                if (canlogactions == true)
                {
                    if (Directory.Exists(logdir) == false)
                    {
                        Directory.CreateDirectory(logdir);
                    }

                    using (StreamWriter sw = new StreamWriter(logdir + sessionid + ".txt", true))
                    {
                        sw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " : " + value);
                    }
                }
            }
            catch (Exception exe)
            {
                MessageBox.Show("An error occurred while writing to the log file. This error can be avoided if you disable the action logging feature. Please report this issue at http://bexplorer.codeplex.com/. \r\n\r\n Here is some information about the error: \r\n\r\n" + exe.Message + "\r\n\r\n" + exe.ToString(), "Error While Writing to Log", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnShowLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(logdir) == false)
                {
                    Directory.CreateDirectory(logdir);
                }

                NewTab(logdir);
            }
            catch (Exception exe)
            {
                MessageBox.Show("An error occurred while trying to open the logs folder. Please report this issue at http://bexplorer.codeplex.com/. \r\n\r\n Here is some information about the error: \r\n\r\n" + exe.Message + "\r\n\r\n" + exe.ToString(), "Error While Opening Log Folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Change Pane Visibility

        /// <summary>
        /// Changes the PaneVisibility at Runtime
        /// </summary>
        /// <param name="PaneForChange">The pane for change (0x1 - Details, 0x2 - Preview, 0x3 - Navigation)</param>
        /// <param name="IsShow"></param>
        private void ChangePaneVisibility(uint PaneForChange, bool IsShow)
        {

            ShellObject lastLoc = Explorer.NavigationLog.CurrentLocation;
            Explorer.automan.Dispose();
            Explorer.Dispose();
            Explorer = new ExplorerBrowser();

            switch (PaneForChange)
            {
                case 0x1:
                    {
                        Explorer.NavigationOptions.PaneVisibility.Details = IsShow ? PaneVisibilityState.Show : PaneVisibilityState.Hide;
                        RegistryKey rk = Registry.CurrentUser;
                        RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                        rks.SetValue(@"InfoPaneEnabled", IsShow ? 1 : 0);
                        rks.Close();
                        rk.Close();
                        IsInfoPaneEnabled = IsShow;
                        if (!IsNavigationPaneEnabled)
                        {
                            Explorer.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Hide;
                        }
                        if (!IsPreviewPaneEnabled)
                        {
                            Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide;
                        }
                        else
                        {
                            Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Show;
                        }
                        break;
                    }
                case 0x2:
                    {
                        Explorer.NavigationOptions.PaneVisibility.Preview = IsShow ? PaneVisibilityState.Show : PaneVisibilityState.Hide;

                        RegistryKey rk = Registry.CurrentUser;
                        RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                        rks.SetValue(@"PreviewPaneEnabled", IsShow ? 1 : 0);
                        rks.Close();
                        rk.Close();
                        IsPreviewPaneEnabled = IsShow;
                        if (!IsNavigationPaneEnabled)
                        {
                            Explorer.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Hide;
                        }
                        if (!IsInfoPaneEnabled)
                        {
                            Explorer.NavigationOptions.PaneVisibility.Details = PaneVisibilityState.Hide;
                        }
                        break;
                    }
                case 0x3:
                    {
                        Explorer.NavigationOptions.PaneVisibility.Navigation = IsShow ? PaneVisibilityState.Show : PaneVisibilityState.Hide;
                        IsNavigationPaneEnabled = true;
                        RegistryKey rk = Registry.CurrentUser;
                        RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                        rks.SetValue(@"NavigationPaneEnabled", IsShow ? 1 : 0);
                        rks.Close();
                        rk.Close();
                        IsNavigationPaneEnabled = IsShow;
                        if (!IsPreviewPaneEnabled)
                        {
                            Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide;
                        }
                        else
                        {
                            Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Show;
                        }
                        if (!IsInfoPaneEnabled)
                        {
                            Explorer.NavigationOptions.PaneVisibility.Details = PaneVisibilityState.Hide;
                        }
                        break;
                    }
                default:
                    break;
            }



            Explorer.SelectionChanged += new EventHandler(ExplorerBrowserControl_SelectionChanged);
            Explorer.NavigationComplete += new EventHandler<NavigationCompleteEventArgs>(ExplorerBrowserControl_NavigationComplete);
            Explorer.ViewEnumerationComplete += new EventHandler(ExplorerBrowserControl_ViewEnumerationComplete);
            Explorer.NavigationPending += new EventHandler<NavigationPendingEventArgs>(Explorer_NavigationPending);
            Explorer.GotFocus += new EventHandler(Explorer_GotFocus);

            Explorer.NavigationOptions.PaneVisibility.Commands = PaneVisibilityState.Hide;
            Explorer.NavigationOptions.PaneVisibility.CommandsOrganize = PaneVisibilityState.Hide;
            Explorer.NavigationOptions.PaneVisibility.CommandsView = PaneVisibilityState.Hide;
            Explorer.ItemsChanged += new EventHandler(Explorer_ItemsChanged);
            Explorer.ContentOptions.FullRowSelect = true;
            Explorer.ContentOptions.CheckSelect = false;
            Explorer.ClientSizeChanged += new EventHandler(ExplorerBrowserControl_ClientSizeChanged);
            Explorer.Paint += new System.Windows.Forms.PaintEventHandler(ExplorerBrowserControl_Paint);
            Explorer.ViewChanged += new EventHandler<ViewChangedEventArgs>(Explorer_ViewChanged);
            //Explorer.ItemHot += new EventHandler<ExplorerAUItemEventArgs>(Explorer_ItemHot);
            Explorer.ExplorerBrowserMouseLeave += new EventHandler(Explorer_ExplorerBrowserMouseLeave);
            Explorer.MouseMove += new System.Windows.Forms.MouseEventHandler(Explorer_MouseMove);

            isGoingBackOrForward = true;

            Explorer.Width = (int)ShellVView.Width;
            Explorer.Height = (int)ShellVView.Height;
            Explorer.Navigate(lastLoc);
            ShellVView.Child = Explorer;
        }

        private void btnNavigationPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x3, true);
        }

        private void btnNavigationPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x3, false);
        }

        private void btnPreviewPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x2, true);
        }

        private void btnPreviewPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x2, false);
        }

        private void btnInfoPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x1, false);
        }

        private void btnInfoPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x1, true);
        }

        #endregion

        #region IsBool Code

        private void TheStatusBar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ExplorerBrowser.IsBool = true;
        }

        private void TheStatusBar_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            ExplorerBrowser.IsBool = false;
        }

        private void RibbonWindow_Activated(object sender, EventArgs e)
        {
            ExplorerBrowser.IsBool = false;
        }

        void fAbout_Closed(object sender, EventArgs e)
        {
            ExplorerBrowser.IsBool = false;
        }


        private void btnCopyto_GotFocus(object sender, RoutedEventArgs e)
        {
            ExplorerBrowser.IsBool = true;
        }

        private void btnCopyto_LostFocus(object sender, RoutedEventArgs e)
        {
            ExplorerBrowser.IsBool = false;
        }

        private void btnMoreColls_DropDownOpened(object sender, EventArgs e)
        {
            ExplorerBrowser.IsBool = true;
        }

        private void btnMoreColls_DropDownClosed(object sender, EventArgs e)
        {
            ExplorerBrowser.IsBool = false;
        }

        void cmHistory_Closed(object sender, RoutedEventArgs e)
        {
            ExplorerBrowser.IsBool = false;
        }

        void cmHistory_Opened(object sender, RoutedEventArgs e)
        {
            ExplorerBrowser.IsBool = true;
        }

        #endregion

        #region Breadcrumb Bar

        private void breadcrumbBarControl1_NavigateRequested(object sender, PathEventArgs e)
        {
            Explorer.Navigate(e.ShellObject != null ? e.ShellObject : ShellObject.FromParsingName(e.Path));
        }

        private void RibbonWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                breadcrumbBarControl1.ExitEditMode();
            }
        }

        private void TheStatusBar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (IsAfterRename)
            {
                breadcrumbBarControl1.ExitEditMode();
            }
        }

        private void RibbonWindow_GotFocus(object sender, RoutedEventArgs e)
        {
            if (backstage.IsOpen == false)
            {
                if (IsAfterRename)
                {
                    
                    Explorer.ExplorerSetFocus();
                }
            }

            if (breadcrumbBarControl1.IsInEditMode)
            {
                breadcrumbBarControl1.ExitEditMode();
            }
        }

        private void SaveHistoryToFile(string relativepath, List<String> history)
        {
            //string path = null;
            //foreach (ShellObject item in Explorer.SelectedItems)
            //{
            //    //This way i think is better of making multiple line in .Net ;)
            //    if (string.IsNullOrEmpty(path))
            //    {
            //        path = item.ParsingName;
            //    }
            //    else
            //    {
            //        path = path + "\r\n" + item.ParsingName;
            //    }

            //}

            // Write each entry to a file. (the "false" parameter makes sure the file is overwritten.)
            using (StreamWriter sw = new StreamWriter(relativepath, false))
            {
                foreach (string item in history)
                {
                    sw.WriteLine(item);
                }
            }

        }

        private List<string> ReadHistoryFromFile(string relativepath)
        {
            string line = "";
            List<string> hl = new List<string>();
            using (StreamReader sr = new StreamReader(relativepath))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    hl.Add(line);
                }
            }

            return hl;
        }

        void bbi_Drop(object sender, DragEventArgs e)
        {


            System.Windows.Point pt = e.GetPosition(sender as IInputElement);


            if ((sender as BreadcrumbBarItem).ShellObject.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        DropData PasteData = new DropData();
                        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                        StringCollection scol = new StringCollection();
                        scol.AddRange(collection);
                        PasteData.DropList = scol;
                        PasteData.PathForDrop = (sender as BreadcrumbBarItem).ShellObject.ParsingName;
                        AddToLog("Copied Files to " + PasteData.PathForDrop + " Files copied: " + scol.ToString());
                        Thread t = null;
                        t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start(PasteData);
                    }
                }
                else
                {
                    //if (Path.GetPathRoot((sender as CloseableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    e.Effects = DragDropEffects.Move;
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        DropData PasteData = new DropData();
                        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                        StringCollection scol = new StringCollection();
                        scol.AddRange(collection);
                        PasteData.DropList = scol;
                        PasteData.PathForDrop = (sender as BreadcrumbBarItem).ShellObject.ParsingName;
                        AddToLog("Moved Files to " + PasteData.PathForDrop + " Files moved: " + scol.ToString());
                        Thread t = null;
                        t = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start(PasteData);
                    }

                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;
                    //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    //    {
                    //        DropData PasteData = new DropData();
                    //        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                    //        StringCollection scol = new StringCollection();
                    //        scol.AddRange(collection);
                    //        PasteData.DropList = scol;
                    //        PasteData.PathForDrop = (sender as CloseableTabItem).Path.ParsingName;
                    //        Thread t = null;
                    //        t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                    //        t.SetApartmentState(ApartmentState.STA);
                    //        t.Start(PasteData);
                    //    }

                    //}
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }


            DropTargetHelper.Drop(e.Data, pt, e.Effects);
        }

        void bbi_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if ((sender as BreadcrumbBarItem).ShellObject.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;

                }
                else
                {
                    //if (Path.GetPathRoot((sender as CloseableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    //    e.Effects = DragDropEffects.Move;


                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;

                    //}

                    // I decided just to have it do a move because it will avoid errors with special shell folders.
                    // Besides, if a person wants to copy something, they should know how to press Ctrl to copy.
                    e.Effects = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            Win32Point ptw = new Win32Point();
            GetCursorPos(ref ptw);
            //e.Handled = true;
            DropTargetHelper.DragOver(new System.Windows.Point(ptw.X, ptw.Y), e.Effects);
        }

        void bbi_DragLeave(object sender, DragEventArgs e)
        {
            DropTargetHelper.DragLeave();
        }

        void bbi_DragEnter(object sender, DragEventArgs e)
        {


            if ((sender as BreadcrumbBarItem).ShellObject.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;

                }
                else
                {
                    //if (Path.GetPathRoot((sender as CloseableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    //    e.Effects = DragDropEffects.Move;


                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;

                    //}

                    // I decided just to have it do a move because it will avoid errors with special shell folders.
                    // Besides, if a person wants to copy something, they should know how to press Ctrl to copy.
                    e.Effects = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }


            Win32Point ptw = new Win32Point();
            GetCursorPos(ref ptw);
            e.Effects = DragDropEffects.None;
            DropTargetHelper.DragEnter(this, e.Data, new System.Windows.Point(ptw.X, ptw.Y), e.Effects);
        }

        #endregion

        #region Other Random Unused Code

        //private void rTabbar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //}

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scviewer = (sender as ScrollViewer);
            scviewer.ScrollToHorizontalOffset(scviewer.HorizontalOffset - e.Delta);
        }


        private void Backdoor(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("The (handled) Button.MouseUp event occurred.");
        }

        //private void button1_Click(object sender, RoutedEventArgs e)
        //{
        //    MessageBox.Show(Explorer.ColSize(0).ToString());
        //}

        //private void btnPathPick_Click(object sender, RoutedEventArgs e)
        //{

        //}

        private void RibbonWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (breadcrumbBarControl1.IsInEditMode)
            {
                breadcrumbBarControl1.ExitEditMode();
            }
        }

        private void TheRibbon_IsMinimizedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        //private void TheRibbon_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        //{
        //    if (KeepFocusOnExplorer == true)
        //    {
        //        Explorer.Focus();
        //        KeepFocusOnExplorer = false;
        //    }
        //}

        private void explorerBrowser1_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //if (e.KeyCode == System.Windows.Forms.Keys.Delete)
            //{
            //    KeepFocusOnExplorer = true;
            //}
        }

        private void RibbonWindow_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void ShellVView_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            fmAbout fAbout = new fmAbout(this);
            fAbout.Closed += new EventHandler(fAbout_Closed);
            ExplorerBrowser.IsBool = true;
            fAbout.ShowDialog();
        }

        #endregion

        #region Search

        private void searchTextBox1_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //e.Handled = true;
            //searchTextBox1.Focus();
        }

        private void searchTextBox1_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void searchTextBox1_GotFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

        }

        private void searchTextBox1_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {


        }

        private void searchTextBox1_LostFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

        }

        private void searchTextBox1_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;

        }

        private void searchTextBox1_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ctgSearch.Visibility = System.Windows.Visibility.Visible;
            if (!TheRibbon.IsMinimized)
            { 
                TheRibbon.SelectedTabItem = tbSearch;
            }
            
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            bool isThereChecked = false;
            foreach (object item in ((sender as MenuItem).Parent as SplitButton).Items)
            {
                if (item is MenuItem)
                {
                    if ((item as MenuItem).IsChecked)
                    {
                        isThereChecked = true;
                        break;
                    }
                }

            }

            ((sender as MenuItem).Parent as SplitButton).IsChecked = isThereChecked;
        }

        private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            bool isThereChecked = false;
            foreach (object item in ((sender as MenuItem).Parent as SplitButton).Items)
            {
                if (item is MenuItem)
                {
                    if ((item as MenuItem).IsChecked)
                    {
                        isThereChecked = true;
                        break;
                    }
                }

            }
            ((sender as MenuItem).Parent as SplitButton).IsChecked = isThereChecked;
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            //(sender as MenuItem).IsChecked = !(sender as MenuItem).IsChecked;
        }

        private void ctgSearch_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if (((System.Windows.Visibility)e.NewValue) == System.Windows.Visibility.Collapsed)
            //    TheRibbon.SelectedTabItem = HomeTab;
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            //string SearchStr = "";
            //string CondStr = "";
            //string KindStr = "";
            //string OtherStr = "";
            //SearchStr = edtSearchBox.SearchCriteriatext.Text;
            ////foreach (Fluent.ToggleButton item in tsgModifiers.Items)
            ////{
            ////    if (item.IsChecked == true)
            ////    {
            ////        CondStr = item.Tag.ToString();
            ////    }
            ////}
            //CondStr.Replace("AND", "");
            //SearchStr += " " + CondStr + " ";
            //foreach (Fluent.ToggleButton item in tsgKind.Items)
            //{
            //    if (item.IsChecked == true)
            //    {
            //        if (item.Tag.ToString() == "kind:everything")
            //        {
            //            KindStr = item.Tag.ToString();
            //            break;
            //        }
            //        else
            //        {
            //            KindStr += item.Tag.ToString() + " " + CondStr + " ";
            //        }

            //    }
            //}

            //foreach (object item in tsgOther.Items)
            //{
            //    if (item is SplitButton)
            //    {
            //        if ((item as SplitButton).IsChecked)
            //        {
            //            foreach (object obj in (item as SplitButton).Items)
            //            {
            //                if (obj is MenuItem)
            //                {
            //                    if ((obj as MenuItem).IsChecked)
            //                    {
            //                        OtherStr += (obj as MenuItem).Tag.ToString() + " " + CondStr + " ";
            //                    }
            //                }

            //            }
            //        }
            //    }
            //}
            //SearchStr += KindStr;
            //SearchStr += OtherStr;

            //DoSearch(SearchStr);

            DoSearch(edtSearchBox.FullSearchTerms);
        }

        private void searchTextBox1_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
        }

        private void searchTextBox1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void edtSearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key == Key.Enter)
            {
                DoSearch(edtSearchBox.FullSearchTerms);
            }
        }

        private void edtSearchBox_BeginSearch_1(object sender, SearchRoutedEventArgs e)
        {
            DoSearch(edtSearchBox.FullSearchTerms);
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            edtSearchBox.ModifiedCondition = (string)((FrameworkElement)sender).Tag;
            dmsplit.IsChecked = true;
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            edtSearchBox.DateCondition = (string)((FrameworkElement)sender).Tag;
            dcsplit.IsChecked = true;
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ((Fluent.ToggleButton)sender).IsChecked = true;
            edtSearchBox.KindCondition = (string)((FrameworkElement)sender).Tag;
            edtSearchBox.Focus();
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            edtSearchBox.SizeCondition = (string)((FrameworkElement)sender).Tag;
            scSize.IsChecked = true;
        }

        private string GetValueOnly(string property, string value)
        {
            return value.Substring(property.Length + 1);
        }

        private void ToggleButton_Click_1(object sender, RoutedEventArgs e)
        {
            StringSearchCriteriaDialog dat = new StringSearchCriteriaDialog("ext", edtSearchBox.ExtensionCondition);
            dat.ShowDialog();
            if (dat.Confirm == true)
            {
                edtSearchBox.ExtensionCondition = "ext:" + dat.textBox1.Text;
                if (dat.textBox1.Text.Length > 0)
                {
                    ExtToggle.IsChecked = true;
                }
                else
                {
                    ExtToggle.IsChecked = false;
                }
            }
            else
            {
                if (GetValueOnly("ext", edtSearchBox.ExtensionCondition).Length > 0)
                {
                    ExtToggle.IsChecked = true;
                }
                else
                {
                    ExtToggle.IsChecked = false;
                }
            }
        }

        private void edtSearchBox_FiltersCleared(object sender, EventArgs e)
        {
            scSize.IsChecked = false;
            foreach (object item in scSize.Items)
            {
                try
                {
                    (item as MenuItem).IsChecked = false;
                }
                catch
                {

                }
            }
            ExtToggle.IsChecked = false;
            AuthorToggle.IsChecked = false;
            SubjectToggle.IsChecked = false;
            dcsplit.IsChecked = false;
            foreach (object item in dcsplit.Items)
            {
                try
                {
                    (item as MenuItem).IsChecked = false;
                }
                catch
                {

                }
            }
            dmsplit.IsChecked = false;
            foreach (object item in dmsplit.Items)
            {
                try
                {
                    (item as MenuItem).IsChecked = false;
                }
                catch
                {

                }
            }
        }

        private void AuthorToggle_Click(object sender, RoutedEventArgs e)
        {
            StringSearchCriteriaDialog dat = new StringSearchCriteriaDialog("author", edtSearchBox.AuthorCondition);
            dat.ShowDialog();
            if (dat.Confirm == true)
            {
                edtSearchBox.AuthorCondition = "author:" + dat.textBox1.Text;
                if (dat.textBox1.Text.Length > 0)
                {
                    AuthorToggle.IsChecked = true;
                }
                else
                {
                    AuthorToggle.IsChecked = false;
                }
            }
            else
            {
                if (GetValueOnly("author", edtSearchBox.AuthorCondition).Length > 0)
                {
                    AuthorToggle.IsChecked = true;
                }
                else
                {
                    AuthorToggle.IsChecked = false;
                }
            }
        }

        private void SubjectToggle_Click(object sender, RoutedEventArgs e)
        {
            StringSearchCriteriaDialog dat = new StringSearchCriteriaDialog("subject", edtSearchBox.SubjectCondition);
            dat.ShowDialog();
            if (dat.Confirm == true)
            {
                edtSearchBox.SubjectCondition = "subject:" + dat.textBox1.Text;
                if (dat.textBox1.Text.Length > 0)
                {
                    SubjectToggle.IsChecked = true;
                }
                else
                {
                    SubjectToggle.IsChecked = false;
                }
            }
            else
            {
                if (GetValueOnly("subject", edtSearchBox.SubjectCondition).Length > 0)
                {
                    SubjectToggle.IsChecked = true;
                }
                else
                {
                    SubjectToggle.IsChecked = false;
                }
            }
        }

        private void miCustomSize_Click(object sender, RoutedEventArgs e)
        {
            SizeSearchCriteriaDialog dat = new SizeSearchCriteriaDialog();
            string sd = GetValueOnly("size", edtSearchBox.SizeCondition);
            dat.curval.Text = sd;

            dat.ShowDialog();

            if (dat.Confirm == true)
            {
                edtSearchBox.SizeCondition = "size:" + dat.GetSizeQuery();

                if (dat.GetSizeQuery().Length > 0)
                {
                    scSize.IsChecked = true;
                }
                else
                {
                    scSize.IsChecked = false;
                }
            }
            else
            {
                if (edtSearchBox.SizeCondition.Length > 5)
                {
                    scSize.IsChecked = true;
                }
                else
                {
                    scSize.IsChecked = false;
                }
            }
        }

        private void edtSearchBox_RequestCriteriaChange(object sender, SearchRoutedEventArgs e)
        {
            if (e.SearchTerms.StartsWith("author:"))
            {
                AuthorToggle_Click(sender, new RoutedEventArgs(e.RoutedEvent));
            }
            else
            {
                if (e.SearchTerms.StartsWith("ext:"))
                {
                    ToggleButton_Click_1(sender, new RoutedEventArgs(e.RoutedEvent));
                }
                else
                {
                    if (e.SearchTerms.StartsWith("subject:"))
                    {
                        SubjectToggle_Click(sender, new RoutedEventArgs(e.RoutedEvent));
                    }
                    else
                    {
                        if (e.SearchTerms.StartsWith("size:"))
                        {
                            miCustomSize_Click(sender, new RoutedEventArgs(e.RoutedEvent));
                        }
                        else
                        {
                            if (e.SearchTerms.StartsWith("date:"))
                            {
                                dcCustomTime_Click(sender, new RoutedEventArgs(e.RoutedEvent));
                            }
                            else
                            {
                                if (e.SearchTerms.StartsWith("modified:"))
                                {
                                    dmCustomTime_Click(sender, new RoutedEventArgs(e.RoutedEvent));
                                }
                                else
                                {
                                    MessageBox.Show("You have discovered an error in this program. Please tell us which filter you were trying to edit and any other details we should know. \r\n\r\nYour filter: " + e.SearchTerms, "Oops! Found a Bug!", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            //MessageBox.Show("Editor not available or invalid parameter \n\r \r\n" + e.SearchTerms, "Search Criteria Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void dcCustomTime_Click(object sender, RoutedEventArgs e)
        {
            SDateSearchCriteriaDialog star = new SDateSearchCriteriaDialog();

            star.DateCriteria = GetValueOnly("date", edtSearchBox.DateCondition);
            star.textBlock1.Text = "Set Date Created Filter";
            star.ShowDialog();

            if (star.Confirm == true)
            {
                edtSearchBox.DateCondition = "date:" + star.DateCriteria;
            }

            if (edtSearchBox.UseDateCondition == true)
            {
                dcsplit.IsChecked = true;
            }
            else
            {
                dcsplit.IsChecked = false;
            }
        }

        private void dmCustomTime_Click(object sender, RoutedEventArgs e)
        {
            SDateSearchCriteriaDialog star = new SDateSearchCriteriaDialog();

            star.DateCriteria = GetValueOnly("modified", edtSearchBox.ModifiedCondition);
            star.textBlock1.Text = "Set Date Modified Filter";
            star.ShowDialog();

            if (star.Confirm == true)
            {
                edtSearchBox.ModifiedCondition = "modified:" + star.DateCriteria;
            }

            if (edtSearchBox.UseModifiedCondition == true)
            {
                dmsplit.IsChecked = true;
            }
            else
            {
                dmsplit.IsChecked = false;
            }
        }

        #endregion

        private void miCreateSymlink_Click(object sender, RoutedEventArgs e)
        {

            StringCollection DropList = System.Windows.Forms.Clipboard.GetFileDropList();
            string PathForDrop = Explorer.NavigationLog.CurrentLocation.ParsingName.Replace(@"\\", @"\");

            String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
            string dir = Path.GetDirectoryName(CurrentexePath);
            string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");


            int winhandle = (int)WindowsAPI.getWindowId(null, "BetterExplorerOperations");

            List<ShellObject> items = new List<ShellObject>();

            foreach (string item in DropList)
            {
                ShellObject o = ShellObject.FromParsingName(item);
                items.Add(o);
                AddToLog("Created Symbolic Link at " + PathForDrop + @"\" + o.GetDisplayName(DisplayNameType.Default) + " linked to " + o.ParsingName);
            }

            string sources = PathStringCombiner.CombinePaths(items, true);
            string drops = PathStringCombiner.CombinePathsWithSinglePath(PathForDrop + @"\", items, false);


            for (int val = 0; val < items.Count; val++)
            {
                string source = items[val].ParsingName.Replace(@"\\", @"\");
                string drop = String.Format(@"{0}\{1}", PathForDrop, items[val].GetDisplayName(DisplayNameType.Default));
            }

            using (Process proc = new Process())
            {
                var psi = new ProcessStartInfo 
                { 
                    FileName = ExePath, 
                    Verb = "runas", 
                    UseShellExecute = true, 
                    Arguments = String.Format("/env /user:Administrator \"{0}\"", ExePath) 
                };

                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                int res = WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"), 0, 
                    "0x88779", sources, drops, "", "", "");
                proc.WaitForExit();
            }
        }

        private void miJunctionpoint_Click(object sender, RoutedEventArgs e)
        {
            StringCollection DropList = System.Windows.Forms.Clipboard.GetFileDropList();
            string PathForDrop = Explorer.NavigationLog.CurrentLocation.ParsingName.Replace(@"\\", @"\");
            foreach (string item in DropList)
            {
                ShellObject o = ShellObject.FromParsingName(item);
                JunctionPointUtils.JunctionPoint.Create(PathForDrop + @"\" + o.GetDisplayName(DisplayNameType.Default),
                                                        o.ParsingName, true);
                AddToLog("Created Junction Point at " + PathForDrop + @"\" + o.GetDisplayName(DisplayNameType.Default) + " linked to " + o.ParsingName);
                o.Dispose();
            }
        }

        private void btnSizeChart_Click(object sender, RoutedEventArgs e)
        {
            FolderSizeWindow fsw = new FolderSizeWindow(Explorer.NavigationLog.CurrentLocation.ParsingName);
            fsw.Owner = this;
            fsw.Show();


        }

        private void btnFSizeChart_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 0)
            {
                if ((Explorer.SelectedItems[0].IsFolder || Explorer.SelectedItems[0].IsDrive) && Explorer.SelectedItems[0].IsFileSystemObject)
                {
                    FolderSizeWindow fsw = new FolderSizeWindow(Explorer.SelectedItems[0].ParsingName);
                    fsw.Owner = this;
                    fsw.Show();
                }
                else
                {
                    if ((Explorer.NavigationLog.CurrentLocation.IsFolder || Explorer.NavigationLog.CurrentLocation.IsDrive)
                          && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
                    {
                        FolderSizeWindow fsw = new FolderSizeWindow(Explorer.NavigationLog.CurrentLocation.ParsingName);
                        fsw.Owner = this;
                        fsw.Show();
                    }
                }

            }
            else
            {
                if ((Explorer.NavigationLog.CurrentLocation.IsFolder || Explorer.NavigationLog.CurrentLocation.IsDrive)
                                          && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
                {
                    FolderSizeWindow fsw = new FolderSizeWindow(Explorer.NavigationLog.CurrentLocation.ParsingName);
                    fsw.Owner = this;
                    fsw.Show();
                }
            }

        }

        #region AutoSwitch

        private void chkFolder_Checked(object sender, RoutedEventArgs e)
        {
            asFolder = true;
        }

        private void chkFolder_Unchecked(object sender, RoutedEventArgs e)
        {
            asFolder = false;
        }

        private void chkDrive_Checked(object sender, RoutedEventArgs e)
        {
            asDrive = true;
        }

        private void chkDrive_Unchecked(object sender, RoutedEventArgs e)
        {
            asDrive = false;
        }

        private void chkArchive_Checked(object sender, RoutedEventArgs e)
        {
            asArchive = true;
        }

        private void chkArchive_Unchecked(object sender, RoutedEventArgs e)
        {
            asArchive = false;
        }

        private void chkApp_Checked(object sender, RoutedEventArgs e)
        {
            asApplication = true;
        }

        private void chkApp_Unchecked(object sender, RoutedEventArgs e)
        {
            asApplication = false;
        }

        private void chkImage_Checked(object sender, RoutedEventArgs e)
        {
            asImage = true;
        }

        private void chkImage_Unchecked(object sender, RoutedEventArgs e)
        {
            asImage = false;
        }

        private void chkLibrary_Checked(object sender, RoutedEventArgs e)
        {
            asLibrary = true;
        }

        private void chkLibrary_Unchecked(object sender, RoutedEventArgs e)
        {
            asLibrary = false;
        }

        #endregion

        private string GetDefaultFolderfromLibrary(string library)
        {
            try
            {
                ShellLibrary lib = ShellLibrary.Load(library, true);
                return lib.DefaultSaveFolder;
            }
            catch
            {
                return library;
            }
        }

        private string GetUseablePath(string path)
        {
            if (path.StartsWith("::"))
            {
                string lib = path.Substring(0, path.IndexOf("\\"));
                string gp = GetDefaultFolderfromLibrary(lib);
                if (gp == lib)
                {
                    return path;
                }
                else
                {
                    return gp + path.Substring(path.IndexOf("\\") + 1);
                }
            }
            else
            {
                return path;
            }
        }

        //Code source - Codeplex User Salysle
        //Hope it works...
        private bool IsConnectedToInternet()
        {
            int lngFlags = 0;

            return InternetGetConnectedState(lngFlags, 0);
        }

        [DllImport("wininet.dll")]
        public static extern bool InternetGetConnectedState(int lpSFlags, int dwReserved);

        private void RibbonWindow_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (breadcrumbBarControl1.IsInEditMode)
            {
                breadcrumbBarControl1.ExitEditMode();
            }
        }

        private void btnCondSel_DropDownOpened(object sender, EventArgs e)
        {

        }


        #region Tab Controls

        private void MoveTabBarToBottom()
        {
            Grid.SetRow(this.tabControl1, 4);
            this.rTabbarTop.Height = new GridLength(0);
            this.rTabbarBot.Height = new GridLength(25);
            this.tabControl1.TabStripPlacement = Dock.Bottom;
        }

        private void MoveTabBarToTop()
        {
            Grid.SetRow(this.tabControl1, 2);
            this.rTabbarTop.Height = new GridLength(25);
            this.rTabbarBot.Height = new GridLength(0);
            this.tabControl1.TabStripPlacement = Dock.Top;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            MoveTabBarToTop();
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            MoveTabBarToBottom();
        }

        #endregion

    }

    #region Helper External Classes

    class Wpf32Window : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; private set; }

        public Wpf32Window(Window wpfWindow)
        {
            Handle = new WindowInteropHelper(wpfWindow).Handle;
        }
    }

    public static class WindowExtensions
    {
        public static System.Windows.Forms.IWin32Window GetWin32Window(this Window parent)
        {
            return new Wpf32Window(parent);
        }
    }

    /// <summary>
    /// Converts Icon to ImageSource
    /// </summary>
    internal static class IconUtilities
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource ToImageSource(this Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
            {
                throw new Win32Exception();
            }

            return wpfBitmap;
        }
    }
    #endregion

}


