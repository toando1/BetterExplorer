﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.Resources;
using MS.WindowsAPICodePack.Internal;
using System.Text;
using System.Linq;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using WindowsHelper;
using System.ComponentModel;
using System.Collections.Generic;
using FileOperations;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using BIND_OPTS = System.Runtime.InteropServices.ComTypes.BIND_OPTS;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using Message = System.Windows.Forms.Message;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;
using System.Diagnostics;
using ZiadSpace.Util;


namespace Microsoft.WindowsAPICodePack.Controls.WindowsForms
{

    /// <summary>
    /// This class is a wrapper around the Windows Explorer Browser control.
    /// </summary>
    public sealed class ExplorerBrowser :
        UserControl,
        IServiceProvider,
        IExplorerPaneVisibility,
        IExplorerBrowserEvents,
        ICommDlgBrowser3,
        IMessageFilter  
    {
        #region Settings
        public static bool IsOldSysListView = false;
        #endregion

        
        public bool ISDisablesubclass = false;
        public bool IsExFileOpEnabled = false;
        //public QTTabBarLib.Automation.AutomationManager automan = new QTTabBarLib.Automation.AutomationManager();
        #region Imports
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern IShellItem SHCreateItemWithParent(
            [In] IntPtr pidlParent,
            [In] IShellFolder psfParent,
            [In] IntPtr pidl,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        [DllImport("user32.dll")]
        public static extern uint RegisterClipboardFormat(string lpszFormat);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void GetColumnbyIndex(IShellView view, bool isAll, int index, out PROPERTYKEY res);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetItemName(IFolderView2 view, int index);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void GetItemLocation(IShellView view, int index, out int pointx, out int pointy);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetColumnInShellView(IShellView view, int count, [MarshalAs(UnmanagedType.LPArray)] PROPERTYKEY[] pk);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetSortColumns(IFolderView2 view, int count, [MarshalAs(UnmanagedType.LPArray)] SORTCOLUMN[] pk);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetColumnInfobyIndex(IShellView view, bool isAll, int index, out CM_COLUMNINFO res);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetColumnInfobyPK(IShellView view, bool isAll, PROPERTYKEY pk, out CM_COLUMNINFO res);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void GetSortColumns(IShellView view, int index, out SORTCOLUMN sc);

        [Guid("00000101-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface IEnumString
        {
            void Clone(out IEnumString ppenum);
            int Next(int celt, String[] rgelt, out int pceltFetched);
            int Reset();
            int Skip(int celt);

        }; // class IEnumString
        [Guid("00000010-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface IRunningObjectTable
        {
            void EnumRunning(out IEnumMoniker ppenumMoniker);
            void GetObject(IMoniker pmkObjectName, out Object ppunkObject);
            void GetTimeOfLastChange(IMoniker pmkObjectName,
                                     out FILETIME pfiletime);
            void IsRunning(IMoniker pmkObjectName);
            void NoteChangeTime(int dwRegister, ref FILETIME pfiletime);
            void Register(int grfFlags, Object punkObject, IMoniker pmkObjectName,
                          out int pdwRegister);
            void Revoke(int dwRegister);

        }; // class IRunningObjectTable
        [Guid("0000000e-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface IBindCtx
        {
            void EnumObjectParam(out IEnumString ppenum);
            void GetBindOptions(ref BIND_OPTS pbindopts);
            void GetObjectParam(String pszKey, out Object ppunk);
            void GetRunningObjectTable(out IRunningObjectTable pprot);
            void RegisterObjectBound(Object punk);
            void RegisterObjectParam(String pszKey, Object punk);
            void ReleaseBoundObjects();
            void RevokeObjectBound(Object punk);
            void RevokeObjectParam(String pszKey);
            void SetBindOptions(ref BIND_OPTS pbindopts);

        }; // class IBindCtx
        [ComImport]

        [Guid("3D8B0590-F691-11d2-8EA9-006097DF5BD4")]

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

        public interface IAsyncOperation
        {

            [PreserveSig]

            Int32 SetAsyncMode([MarshalAs(UnmanagedType.VariantBool)] Boolean DoOpAsync);

            [PreserveSig]

            Int32 GetAsyncMode([MarshalAs(UnmanagedType.VariantBool)] out Boolean IsOpAsync);

            [PreserveSig]

            Int32 StartOperation(IBindCtx bcReserved);

            [PreserveSig]

            Int32 InOperation([MarshalAs(UnmanagedType.VariantBool)] out Boolean InAsyncOp);

            [PreserveSig]

            Int32 EndOperation(UInt32 hResult, IBindCtx bcReserved, DragDropEffects Effects);

        }
        [Guid("0000000f-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface IMoniker
        {
            void BindToObject(IBindCtx pbc, IMoniker pmkToLeft,
                              ref Guid riidResult, out Object ppvResult);
            void BindToStorage(IBindCtx pbc, IMoniker pmkToLeft,
                               ref Guid riidResult, out Object ppvResult);
            void CommonPrefixWith(IMoniker pmkOther, out IMoniker ppmkPrefix);
            void ComposeWith(IMoniker pmkRight, bool fOnlyIfNotGeneric,
                             out IMoniker ppmkComposite);
            void Enum(bool fForward, out IEnumMoniker ppenumMoniker);
            void GetClassID(out Guid pClassID);
            void GetDisplayName(IBindCtx pbc, IMoniker pmkToLeft,
                                out String ppszDisplayName);
            void GetSizeMax(out long pcbSize);
            void GetTimeOfLastChange(IBindCtx pbc, IMoniker pmkToLeft,
                                     out FILETIME pFileTime);
            void Hash(out int pdwHash);
            void Inverse(out IMoniker ppmk);
            int IsDirty();
            void IsEqual(IMoniker pmkOtherMoniker);
            void IsRunning(IBindCtx pbc, IMoniker pmkToLeft,
                           IMoniker pmkNewlyRunning);
            void IsSystemMoniker(out int pdwMksys);
            void Load(IStream pStm);
            void ParseDisplayName(IBindCtx pbc, IMoniker pmkToLeft,
                                  String pszDisplayName, out int pcbEaten,
                                  out IMoniker ppmkOut);
            void Reduce(IBindCtx pbc, int dwReduceHowFar,
                        ref IMoniker ppmkToLeft, out IMoniker ppmkReduced);
            void RelativePathTo(IMoniker pmkOther, out IMoniker ppmkRelPath);
            void Save(IStream pStm, bool fClearDirty);

        }; // class IMoniker

        [Guid("0000000c-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface IStream
        {
            void Clone(out IStream ppstm);
            void Commit(int grfCommitFlags);
            void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten);
            void LockRegion(long libOffset, long cb, int dwLockType);
            void Read(byte[] pv, int cb, IntPtr pcbRead);
            void Revert();
            void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition);
            void SetSize(long libNewSize);
            void Stat(out STATSTG pstatstg, int grfStatFlag);
            void UnlockRegion(long libOffset, long cb, int dwLockType);
            void Write(byte[] pv, int cb, IntPtr pcbWritten);

        }; // class IStream

        [Guid("00000102-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface IEnumMoniker
        {
            void Clone(out IEnumMoniker ppenum);
            int Next(int celt, IMoniker[] rgelt, out int pceltFetched);
            int Reset();
            int Skip(int celt);

        }; // class IEnumMoniker

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000010E-0000-0000-C000-000000000046")]
        public interface IDataObject
        {
            void GetData([In] ref FORMATETC format, out STGMEDIUM medium);
            void GetDataHere([In] ref FORMATETC format, ref STGMEDIUM medium);
            [PreserveSig]
            int QueryGetData([In] ref FORMATETC format);
            [PreserveSig]
            int GetCanonicalFormatEtc([In] ref FORMATETC formatIn, out FORMATETC formatOut);
            void SetData([In] ref FORMATETC formatIn, [In] ref STGMEDIUM medium, [MarshalAs(UnmanagedType.Bool)] bool release);
            IEnumFORMATETC EnumFormatEtc(DATADIR direction);
            [PreserveSig]
            int DAdvise([In] ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection);
            void DUnadvise(int connection);
            [PreserveSig]
            int EnumDAdvise(out IEnumSTATDATA enumAdvise);
        }
        #endregion

        #region properties
        /// <summary>
        /// Options that control how the ExplorerBrowser navigates
        /// </summary>
        public ExplorerBrowserNavigationOptions NavigationOptions { get; private set; }

        /// <summary>
        /// Options that control how the content of the ExplorerBorwser looks
        /// </summary>
        public ExplorerBrowserContentOptions ContentOptions { get; private set; }

        public static ShellObject CurrentLocation;

        private IShellItemArray shellItemsArray;

        private ShellObjectCollection itemsCollection;

        public string GetItemPath(int Index)
        {
            IFolderView2 ifv2 = GetFolderView2();
            return Marshal.PtrToStringAuto(GetItemName(ifv2, Index));

        }
        /// <summary>
        /// The set of ShellObjects in the Explorer Browser
        /// </summary>
        public ShellObjectCollection Items
        {
            get
            {
                if (shellItemsArray != null)
                {
                    Marshal.ReleaseComObject(shellItemsArray);
                }

                if (itemsCollection != null)
                {
                    itemsCollection.Dispose();
                    itemsCollection = null;
                }

                shellItemsArray = GetItemsArray();
                itemsCollection = new ShellObjectCollection(shellItemsArray, true);

                return itemsCollection;
            }
        }

        private IShellItemArray selectedShellItemsArray;

        public void SetActiveShell()
        {
            WindowsAPI.SetActiveWindow(SysListViewHandle);
        }

        private ShellObjectCollection selectedItemsCollection;
        /// <summary>
        /// The set of selected ShellObjects in the Explorer Browser
        /// </summary>
        public ShellObjectCollection SelectedItems
        {
            get
            {
                if (selectedShellItemsArray != null)
                {
                    Marshal.ReleaseComObject(selectedShellItemsArray);
                }

                if (selectedItemsCollection != null)
                {
                    selectedItemsCollection.Dispose();
                    selectedItemsCollection = null;
                }

                selectedShellItemsArray = GetSelectedItemsArray();
                selectedItemsCollection = new ShellObjectCollection(selectedShellItemsArray, true);

                return selectedItemsCollection;
            }
        }

        /// <summary>
        /// Contains the navigation history of the ExplorerBrowser
        /// </summary>
        public ExplorerBrowserNavigationLog NavigationLog { get; private set; }

        /// <summary>
        /// The name of the property bag used to persist changes to the ExplorerBrowser's view state.
        /// </summary>
        public string PropertyBagName
        {
            get { return propertyBagName; }
            set
            {
                propertyBagName = value;
                if (explorerBrowserControl != null)
                {
                    explorerBrowserControl.SetPropertyBag(propertyBagName);
                }
            }
        }

        Collumns[] _Collumns = new Collumns[0];
        [Browsable(false)]
        public Collumns[] AvailableVisibleColumns
        {
            get
            {
                return _Collumns;
            }
            set { _Collumns = value; }
        }

        public Bitmap BackgroundImage
        {
            get
            {
                return WindowsAPI.GetListViewBackgroundImage(SysListViewHandle);
            }
            set
            {
                SetBackgroundImage(value);
            }
        }

        #endregion

        #region operations
        [Serializable]
        public struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
        }
        public enum SORT
        {
            DESCENDING = -1,
            ASCENDING = 1
        }
        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct CM_COLUMNINFO
        {

            /// DWORD->unsigned int
            public int cbSize;

            /// DWORD->unsigned int
            public int dwMask;

            /// DWORD->unsigned int
            public int dwState;

            /// UINT->unsigned int
            public uint uWidth;

            /// UINT->unsigned int
            public uint uDefaultWidth;

            /// UINT->unsigned int
            public uint uIdealWidth;

            /// WCHAR[]
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string wszName;
        }
        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct SORTCOLUMN
        {
            public PROPERTYKEY propkey;
            public SORT direction;
        }
        public void GetSortColInfo(out SORTCOLUMN ci)
        {
            try
            {
                IFolderView2 ifv2 = GetFolderView2();
                SORTCOLUMN sc = new SORTCOLUMN();
                int SortColsCount = -1;
                ifv2.GetSortColumnCount(out SortColsCount);
                if (SortColsCount > 0)
                {
                    GetSortColumns(GetShellView(), 0, out sc); 
                }
                
                //SORTCOLUMN newsc = (SORTCOLUMN)Marshal.PtrToStructure(scptr, typeof(SORTCOLUMN));
                ci = sc;
                //Marshal.FreeHGlobal(scptr);

            }
            catch (Exception)
            {
                SORTCOLUMN sc = new SORTCOLUMN();
                ci = sc;
            }
        }

        public void DesubclassWin()
        {
            DeSubClass(SysListViewHandle);
        }
        public void SubClassWin()
        {
            SubclassHWnd(SysListViewHandle);
        }
        public void SetColInView(PROPERTYKEY pk, bool Remove)
        {

            if (!Remove)
            {
                PROPERTYKEY[] pkk = new PROPERTYKEY[AvailableVisibleColumns.Length + 1];
                for (int i = 0; i < AvailableVisibleColumns.Length; i++)
                {
                    pkk[i] = AvailableVisibleColumns[i].pkey;
                }

                pkk[AvailableVisibleColumns.Length] = pk;

                SetColumnInShellView(GetShellView(), AvailableVisibleColumns.Length + 1, pkk);

                AvailableVisibleColumns = AvailableColumns(GetShellView(), false);
            }
            else
            {
                PROPERTYKEY[] pkk = new PROPERTYKEY[AvailableVisibleColumns.Length - 1];
                int j = 0;
                for (int i = 0; i < AvailableVisibleColumns.Length; i++)
                {
                    if (!(AvailableVisibleColumns[i].pkey.fmtid == pk.fmtid && AvailableVisibleColumns[i].pkey.pid == pk.pid))
                    {
                      pkk[j] = AvailableVisibleColumns[i].pkey;
                      j++;
                    }
                    
                }

                SetColumnInShellView(GetShellView(), AvailableVisibleColumns.Length - 1, pkk);

                AvailableVisibleColumns = AvailableColumns(GetShellView(), false);
            }
        }
        public Collumns[] AvailableColumns(IShellView SHView, bool All)
        {

            Guid iid = new Guid(ExplorerBrowserIIDGuid.IColumnManager);
            IntPtr view = IntPtr.Zero;
            IntPtr Ishellv = IntPtr.Zero;
            Ishellv = Marshal.GetComInterfaceForObject(GetShellView(), typeof(IShellView));
            Marshal.QueryInterface(Ishellv, ref iid, out view);
            IColumnManager cm = (IColumnManager)Marshal.GetObjectForIUnknown(view);
            uint HeaderColsCount = 0;
            cm.GetColumnCount(All?CM_ENUM_FLAGS.CM_ENUM_ALL:CM_ENUM_FLAGS.CM_ENUM_VISIBLE, out HeaderColsCount);
            Collumns[] ci = new Collumns[HeaderColsCount];
            for (int i = 0; i < HeaderColsCount; i++)
            {
                Collumns col = new Collumns();
                PROPERTYKEY pk;
                CM_COLUMNINFO cmi;

                try
                {
                    GetColumnbyIndex(GetShellView(), All, i, out pk);
                    GetColumnInfobyIndex(GetShellView(), All, i, out cmi);
                    col.pkey = pk;
                    col.Name = cmi.wszName;
                    col.Width = (int)cmi.uWidth;
                    ci[i] = col;

                }
                catch 
                {


                }

            }


            return ci;
        }


        public void GetItemAtLocation(Point p)
        {
            //WindowsAPI.SetFocus(SysListViewHandle);
            //IFolderView2 ifv2 = GetFolderView2();
            //IShellView isv = GetShellView();

            //for (int i = 0; i < GetItemsCount() - 1; i++)
            //{
            //    int pitem = -1;
            //    ifv2.GetVisibleItem(i,false, out pitem);
            //    if (pitem != -1)
            //    {
            //        IntPtr pidl;
            //        ifv2.Item(i, out pidl);
                    
                    
            //        if (pidl != IntPtr.Zero)
            //        {
            //            int px, py;
            //            GetItemLocation(isv, i, out px,out py);
                        
            //           Guid IshellItemGuid = new Guid(ShellIIDGuid.IShellItem);

            //           string ItemName = Marshal.PtrToStringAuto(GetItemName(ifv2, i));
            //           ShellObject sho = ShellObject.FromParsingName(ItemName);

            //           //MessageBox.Show(sho.ParsingName);
            //        }
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            

        }
        public void SetAutoSizeColumns()
        {
            WindowsAPI.SetFocus(SysListViewHandle);
            SendKeys.SendWait("^{+}");
        }




        public void DoCopy(object Data)
        {
            DropData DataDrop = (DropData)Data;

            if (DataDrop.ItemsForDrop != null)
            {
                using (FileOperation fileOp = new FileOperation())
                {
                    foreach (ShellObject item in DataDrop.ItemsForDrop)
                    {

                        string New_Name = "";
                        if (Path.GetExtension(item.ParsingName) == "")
                        {

                            New_Name = item.GetDisplayName(DisplayNameType.Default);
                        }
                        else
                        {
                            New_Name = Path.GetFileName(item.ParsingName);
                        }
                        if (!File.Exists(DataDrop.PathForDrop))
                        {
                            fileOp.CopyItem(item.ParsingName, DataDrop.PathForDrop, New_Name);
                        }
                        else
                        {
                            fileOp.CopyItem(item.ParsingName, NavigationLog.CurrentLocation.ParsingName, New_Name);
                        }

                    }

                    fileOp.PerformOperations();
                }
            }
            else if (DataDrop.DropList != null)
            {
                using (FileOperation fileOp = new FileOperation())
                {
                    foreach (string item in DataDrop.DropList)
                    {

                        string New_Name = "";
                        if (Path.GetExtension(item) == "")
                        {
                            ShellObject shi = ShellObject.FromParsingName(item);
                            New_Name = shi.GetDisplayName(DisplayNameType.Default);
                        }
                        else
                        {
                            New_Name = Path.GetFileName(item);
                        }
                        if (!File.Exists(DataDrop.PathForDrop))
                        {
                            fileOp.CopyItem(item, DataDrop.PathForDrop, New_Name);
                        }
                        else
                        {
                            fileOp.CopyItem(item, NavigationLog.CurrentLocation.ParsingName, New_Name);
                        }

                    }

                    fileOp.PerformOperations();
                }
            }
            else if (DataDrop.Shellobjects != null)
            {

                    using (FileOperation fileOp = new FileOperation())
                    {
                        foreach (ShellObject item in DataDrop.Shellobjects)
                        {

                            string New_Name = "";
                            if (Path.GetExtension(item.ParsingName) == "")
                            {
                                ShellObject shi = item;
                                New_Name = shi.GetDisplayName(DisplayNameType.Default);
                            }
                            else
                            {
                                New_Name = Path.GetFileName(item.ParsingName);
                            }
                            if (!File.Exists(DataDrop.PathForDrop))
                            {
                                fileOp.CopyItem(item.ParsingName, DataDrop.PathForDrop, New_Name);
                            }
                            else
                            {
                                fileOp.CopyItem(item.ParsingName, NavigationLog.CurrentLocation.ParsingName, New_Name);
                            }

                        }

                        fileOp.PerformOperations();
                    } 

            }
        }

        public void DoMove(object Data)
        {
            DropData DataDrop = (DropData)Data;

            if (DataDrop.ItemsForDrop != null)
            {
                using (FileOperation fileOp = new FileOperation())
                {
                    foreach (ShellObject item in DataDrop.ItemsForDrop)
                    {

                        string New_Name = "";
                        if (Path.GetExtension(item.ParsingName) == "")
                        {

                            New_Name = item.GetDisplayName(DisplayNameType.Default);
                        }
                        else
                        {
                            New_Name = Path.GetFileName(item.ParsingName);
                        }
                        if (!File.Exists(DataDrop.PathForDrop))
                        {
                            fileOp.MoveItem(item.ParsingName, DataDrop.PathForDrop, New_Name);
                        }
                        else
                        {
                            fileOp.MoveItem(item.ParsingName, NavigationLog.CurrentLocation.ParsingName, New_Name);
                        }

                    }

                    fileOp.PerformOperations();
                } 
            }
            else if (DataDrop.DropList != null)
            {
                using (FileOperation fileOp = new FileOperation())
                {
                    foreach (string item in DataDrop.DropList)
                    {

                        string New_Name = "";
                        if (Path.GetExtension(item) == "")
                        {
                            ShellObject shi = ShellObject.FromParsingName(item);
                            New_Name = shi.GetDisplayName(DisplayNameType.Default);
                        }
                        else
                        {
                            New_Name = Path.GetFileName(item);
                        }
                        if (!File.Exists(DataDrop.PathForDrop))
                        {
                            fileOp.MoveItem(item, DataDrop.PathForDrop, New_Name);
                        }
                        else
                        {
                            fileOp.MoveItem(item, NavigationLog.CurrentLocation.ParsingName, New_Name);
                        }

                    }

                    fileOp.PerformOperations();
                }
            }
            else if (DataDrop.Shellobjects != null)
            {

                using (FileOperation fileOp = new FileOperation())
                {
                    foreach (ShellObject item in DataDrop.Shellobjects)
                    {

                        string New_Name = "";
                        if (Path.GetExtension(item.ParsingName) == "")
                        {
                            ShellObject shi = item;
                            New_Name = shi.GetDisplayName(DisplayNameType.Default);
                        }
                        else
                        {
                            New_Name = Path.GetFileName(item.ParsingName);
                        }
                        if (!File.Exists(DataDrop.PathForDrop))
                        {
                            fileOp.MoveItem(item.ParsingName, DataDrop.PathForDrop, New_Name);
                        }
                        else
                        {
                            fileOp.MoveItem(item.ParsingName, NavigationLog.CurrentLocation.ParsingName, New_Name);
                        }

                    }

                    fileOp.PerformOperations();
                }

            }
        }


        public void DeleteToRB()
        {
            string Files = "";
            foreach (ShellObject selectedItem in SelectedItems)
            {
                if (Files == "")
                {
                    Files = selectedItem.ParsingName;
                }
                else
                    Files = Files + '\0' + selectedItem.ParsingName;
            }
            RecybleBin.Send(Files);
        }

        public void DoDelete(object Data)
        {
            ShellObjectCollection DataDelete = (ShellObjectCollection)Data;

            using (FileOperation fileOp = new FileOperation())
            {
                foreach (ShellObject item in DataDelete)
                {
                    fileOp.DeleteItem(item.ParsingName);
                }

                fileOp.PerformOperations();
            }
            
        }

        public void SelectAllItems()
        {
            
            WindowsAPI.SetFocus(SysListViewHandle);
            SendKeys.SendWait("^a");

        }
        public void DeSelectAllItems()
        {
            WindowsAPI.SetFocus(SysListViewHandle);
            IFolderView2 ifv = GetFolderView2();
            ifv.SelectItem(-1, (uint)WindowsAPI.SVSIF.SVSI_DESELECTOTHERS);

        }

        public void SelectItem(int Index)
        {
            WindowsAPI.SetFocus(SysListViewHandle);
            IFolderView2 ifv = GetFolderView2();
            ifv.SelectItem(Index, (uint)WindowsAPI.SVSIF.SVSI_DESELECTOTHERS);
        }

        public void InvertSelection()
        {
            WindowsAPI.SetFocus(SysListViewHandle);
            IFolderView2 ifv2 = GetFolderView2();
            IShellView shv = GetShellView();
            
            
            for (int i = 0; i < GetItemsCount(); i++)
            {

                IntPtr pidl;
                ifv2.Item(i, out pidl);
                WindowsAPI.SVSIF state;
                ifv2.GetSelectionState(pidl, out state);
                if (state == WindowsAPI.SVSIF.SVSI_DESELECT || state == WindowsAPI.SVSIF.SVSI_FOCUSED &&
                    state != WindowsAPI.SVSIF.SVSI_SELECT)
                {
                    shv.SelectItem(pidl, WindowsAPI.SVSIF.SVSI_SELECT);
                }
                else
                {
                    shv.SelectItem(pidl, WindowsAPI.SVSIF.SVSI_DESELECT);
                }

            }
            Marshal.ReleaseComObject(shv);
            Marshal.ReleaseComObject(ifv2);
        }
        System.Runtime.InteropServices.ComTypes.IDataObject GetSelectionDataObject()
        {
            object result;

            if (GetShellView() == null)
            {
                return null;
            }

            Guid IData = typeof (System.Runtime.InteropServices.ComTypes.IDataObject).GUID;
            GetShellView().GetItemObject(ShellViewGetItemObject.Selection,
                ref IData, out result);
//            IntPtr result = (IntPtr) resulto;
//            if (result != IntPtr.Zero)
//            {
//                System.Runtime.InteropServices.ComTypes.IDataObject wrapped =
//                    (System.Runtime.InteropServices.ComTypes.IDataObject)
//                        Marshal.GetTypedObjectForIUnknown(result,
//                            typeof(System.Runtime.InteropServices.ComTypes.IDataObject));
//                return wrapped;
//            }
//            else
//            {
//                return null;
//            }
            return (System.Runtime.InteropServices.ComTypes.IDataObject)result;
        }

        public void ShowFileProperties()
        {
            if (WindowsAPI.SHMultiFileProperties(GetSelectionDataObject(), 0) != 0 /*S_OK*/)
            {
                throw new Win32Exception();
            }
        }
        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;
        public void ShowFileProperties(string Filename)
        {
            WindowsAPI.SHELLEXECUTEINFO info = new WindowsAPI.SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            WindowsAPI.ShellExecuteEx(ref info);
        }
        public void DoRename()
        {

            IFolderView2 fv2 = GetFolderView2();
            int iIndex = -1;
            fv2.GetSelectedItem(0, out iIndex);
            DoRename(iIndex);
        }
        public void DoRename(int Index)
        {

            //WindowsAPI.SetFocus(SysListViewHandle);
            IFolderView2 ifv2 = GetFolderView2();
            IShellView shv = GetShellView();
                IntPtr pidl;
                ifv2.Item(Index, out pidl);
                shv.SelectItem(pidl, WindowsAPI.SVSIF.SVSI_SELECT | WindowsAPI.SVSIF.SVSI_DESELECTOTHERS | 
                    WindowsAPI.SVSIF.SVSI_EDIT);

            Marshal.ReleaseComObject(shv);
            Marshal.ReleaseComObject(ifv2);

            //WindowsAPI.SendMessage(SysListViewHandle, WindowsAPI.MSG.LVM_EDITLABEL, Index, 0);
            //WindowsAPI.SetFocus(SysListViewHandle);

        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
	    private static extern IntPtr ILCreateFromPath([MarshalAs(UnmanagedType.LPTStr)] string pszPath);
	
	    [DllImport("shell32.dll")]
	    private static extern IntPtr ILFindLastID(IntPtr pidl);

        [DllImport("shell32.dll")]

        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out()] out IntPtr pidl, uint sfgaoIn, [Out()] out uint psfgaoOut);

        public void SelectItems(ShellObject[] ShellObjectArray)
        {
            IntPtr pIDL = IntPtr.Zero;
            IFolderView ifv = GetFolderView();

            Array PIDLArray = new Array[ShellObjectArray.Length];
            int i = 0;

            

            foreach (ShellObject item in ShellObjectArray)
	        {
                uint iAttribute;
                SHParseDisplayName(item.ParsingName.Replace(@"\\",@"\"), IntPtr.Zero, out pIDL, (uint)0, out iAttribute);

                if (pIDL != IntPtr.Zero)
                {
                    IntPtr pIDLRltv = ILFindLastID(pIDL);
                    if (pIDLRltv != IntPtr.Zero)
                    {
                        PIDLArray.SetValue((int)pIDLRltv,i);
                    }
                }
		        
                i++;
	        }


            NativePoint p = new NativePoint();
            try
            {
                //ifv.SelectAndPositionItems((uint)Marshal.SizeOf(PIDLArray), PIDLArray, ref p, (uint)WindowsAPI.SVSIF.SVSI_SELECT);
            }
            finally
            {
                if (ifv != null)
                    Marshal.ReleaseComObject(ifv);

                if (pIDL != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pIDL);
            }
        }

        public void DoRename(string pathNew, bool IsLiB)
        {

            if (!IsLiB)
            {
                IntPtr pIDL = IntPtr.Zero;
                IShellView shv = GetShellView();

                try
                {
                    uint iAttribute;
                    SHParseDisplayName(pathNew, IntPtr.Zero, out pIDL, (uint)0, out iAttribute);

                    if (pIDL != IntPtr.Zero)
                    {
                        IntPtr pIDLRltv = ILFindLastID(pIDL);
                        if (pIDLRltv != IntPtr.Zero)
                        {
                            shv.SelectItem(pIDLRltv, WindowsAPI.SVSIF.SVSI_SELECT | WindowsAPI.SVSIF.SVSI_DESELECTOTHERS |
                                 WindowsAPI.SVSIF.SVSI_ENSUREVISIBLE | WindowsAPI.SVSIF.SVSI_EDIT);
                        }
                    }
                }
                finally
                {
                    if (shv != null)
                        Marshal.ReleaseComObject(shv);

                    if(pIDL != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(pIDL);
                }
            }
            else
            {
                IntPtr pIDL = IntPtr.Zero;
                IShellView shv = GetShellView();

                try
                {
                    uint iAttribute;
                    ShellLibrary ll = ShellLibrary.Load(pathNew, false);

                    if (ll.PIDL != IntPtr.Zero)
                    {
                        IntPtr pIDLRltv = ILFindLastID(ll.PIDL);
                        if (pIDLRltv != IntPtr.Zero)
                        {
                            shv.SelectItem(pIDLRltv, WindowsAPI.SVSIF.SVSI_SELECT | WindowsAPI.SVSIF.SVSI_DESELECTOTHERS |
                                 WindowsAPI.SVSIF.SVSI_ENSUREVISIBLE | WindowsAPI.SVSIF.SVSI_EDIT);
                        }
                    }
                }
                finally
                {
                    if (shv != null)
                        Marshal.ReleaseComObject(shv);

                    if (pIDL != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(pIDL);
                }
            }
            
        }

        public void SelectItem(ShellObject Item)
        {


                IntPtr pIDL = IntPtr.Zero;
                IShellView shv = GetShellView();

                try
                {
                    uint iAttribute;
                    SHParseDisplayName(Item.ParsingName.Replace(@"\\", @"\"), IntPtr.Zero, out pIDL, (uint)0, out iAttribute);

                    if (pIDL != IntPtr.Zero)
                    {
                        IntPtr pIDLRltv = ILFindLastID(pIDL);
                        if (pIDLRltv != IntPtr.Zero)
                        {
                            shv.SelectItem(pIDLRltv, WindowsAPI.SVSIF.SVSI_SELECT);
                        }
                    }
                }
                finally
                {
                    if (shv != null)
                        Marshal.ReleaseComObject(shv);

                    if (pIDL != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(pIDL);
                }

        }

        public void DoRename(IntPtr apidl)
        {

            IntPtr pIDL = IntPtr.Zero;
            IShellView shv = GetShellView();

            try
            {
                uint iAttribute;

                if (apidl != IntPtr.Zero)
                {
                    IntPtr pIDLRltv = ILFindLastID(apidl);
                    if (pIDLRltv != IntPtr.Zero)
                    {
                        shv.SelectItem(pIDLRltv, WindowsAPI.SVSIF.SVSI_SELECT | WindowsAPI.SVSIF.SVSI_DESELECTOTHERS |
                             WindowsAPI.SVSIF.SVSI_ENSUREVISIBLE | WindowsAPI.SVSIF.SVSI_EDIT);
                    }
                }
            }
            finally
            {
                if (shv != null)
                    Marshal.ReleaseComObject(shv);

                if (pIDL != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pIDL);
            }

        }

        void OnEscKey()
        {
            if (IsOldSysListView)
            {
                int itemCount = WindowsAPI.SendMessage(SysListViewHandle,
                        WindowsAPI.MSG.LVM_GETITEMCOUNT, 0, 0);

                for (int n = 0; n < itemCount; ++n)
                {
                    WindowsAPI.LVITEMA item = new WindowsAPI.LVITEMA();
                    item.mask = WindowsAPI.LVIF.LVIF_STATE;
                    item.iItem = n;
                    item.stateMask = WindowsAPI.LVIS.LVIS_CUT;
                    WindowsAPI.SendMessage(SysListViewHandle, WindowsAPI.MSG.LVM_GETITEMA,
                        0, ref item);

                    if (item.state != 0)
                    {
                        WindowsAPI.LVITEMA lvItem = new WindowsAPI.LVITEMA();
                        lvItem.stateMask = WindowsAPI.LVIS.LVIS_CUT;
                        lvItem.state = 0;
                        WindowsAPI.SendMessage(SysListViewHandle, WindowsAPI.MSG.LVM_SETITEMSTATE, n, ref lvItem);
                    }
                } 
            }
        }
        public bool IsMoveClipboardOperation = false;

        public void DoCut()
        {

            if (IsOldSysListView)
            {
                StringCollection sc = new StringCollection();
                foreach (ShellObject item in SelectedItems)
                {
                    sc.Add(item.ParsingName);
                }
                Clipboard.SetFileDropList(sc);
                IsMoveClipboardOperation = true;
                WindowsAPI.SetFocus(SysListViewHandle);
                int itemCount = WindowsAPI.SendMessage(SysListViewHandle,
                    WindowsAPI.MSG.LVM_GETITEMCOUNT, 0, 0);

                for (int n = 0; n < itemCount; ++n)
                {
                    WindowsAPI.LVITEMA item = new WindowsAPI.LVITEMA();
                    item.mask = WindowsAPI.LVIF.LVIF_STATE;
                    item.iItem = n;
                    item.stateMask = WindowsAPI.LVIS.LVIS_SELECTED;
                    WindowsAPI.SendMessage(SysListViewHandle, WindowsAPI.MSG.LVM_GETITEMA,
                        0, ref item);

                    if (item.state != 0)
                    {
                        WindowsAPI.LVITEMA lvItem = new WindowsAPI.LVITEMA();
                        lvItem.stateMask = WindowsAPI.LVIS.LVIS_CUT;
                        lvItem.state = WindowsAPI.LVIS.LVIS_CUT;
                        WindowsAPI.SendMessage(SysListViewHandle, WindowsAPI.MSG.LVM_SETITEMSTATE, n, ref lvItem);


                    }
                } 
            }
            else
            {
                WindowsAPI.SetFocus(SysListViewHandle);
                SendKeys.SendWait("^x");
            }
            
        }
   
        public void GetGroupColInfo(out PROPERTYKEY pk, out bool Asc)
        {
            try
            {
                IFolderView2 ifv2 = GetFolderView2();
                ifv2.GetGroupBy(out pk, out Asc);

            }
            catch (Exception)
            {
                pk = new PROPERTYKEY();
                Asc = false;
            }
        }

        public IShellView GetShellView()
        {
            Guid iid = new Guid(ExplorerBrowserIIDGuid.IShellView);
            IntPtr view = IntPtr.Zero;
            HResult hr = this.explorerBrowserControl.GetCurrentView(ref iid, out view);
            IShellView isv = (IShellView)Marshal.GetObjectForIUnknown(view);
            return isv;
        }


        public enum ERROR
        {
            SUCCESS,
            FILE_EXISTS = 80,
            BAD_PATHNAME = 161,
            ALREADY_EXISTS = 183,
            FILENAME_EXCED_RANGE = 206,
            CANCELLED = 1223,
        }
        [DllImport("shell32.dll", EntryPoint = "#165", CharSet = CharSet.Unicode)]
        public static extern ERROR SHCreateDirectory(IShellView hwnd, string pszPath);

        public string CreateNewFolder()
        {

            string name = "New Folder";
            int suffix = 0;

            
            do
            {
                if (NavigationLog.CurrentLocation.Parent != null)
                {
                    if (NavigationLog.CurrentLocation.Parent.ParsingName == KnownFolders.Libraries.ParsingName)
                    {
                        ShellLibrary lib = 
                            ShellLibrary.Load(NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), true);
                        name = String.Format("{0}\\New Folder ({1})",
                            lib.DefaultSaveFolder, ++suffix);
                        lib.Close();
                    }
                    else
                        name = String.Format("{0}\\New Folder ({1})",
                            NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.FileSystemPath), ++suffix);
                }
                else
                    name = String.Format("{0}\\New Folder ({1})",
                        NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.FileSystemPath), ++suffix);
            } while (Directory.Exists(name) || File.Exists(name));

            ERROR result = SHCreateDirectory(GetShellView(), name);

            switch (result)
            {
                case ERROR.FILE_EXISTS:
                case ERROR.ALREADY_EXISTS:
                    throw new IOException("The directory already exists");
                case ERROR.BAD_PATHNAME:
                    throw new IOException("Bad pathname");
                case ERROR.FILENAME_EXCED_RANGE:
                    throw new IOException("The filename is too long");
            }
            return name;
        }

        public string CreateNewFolder(string name)
        {
            int suffix = 0;
            string endname = name;

            do
            {
                if (NavigationLog.CurrentLocation.Parent != null)
                {
                    if (NavigationLog.CurrentLocation.Parent.ParsingName == KnownFolders.Libraries.ParsingName)
                    {
                        ShellLibrary lib =
                            ShellLibrary.Load(NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), true);
                        endname = String.Format("{0}\\" + name + " ({1})",
                            lib.DefaultSaveFolder, ++suffix);
                        lib.Close();
                    }
                    else
                        endname = String.Format("{0}\\" + name + " ({1})",
                            NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.FileSystemPath), ++suffix);
                }
                else
                    endname = String.Format("{0}\\" + name + " ({1})",
                        NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.FileSystemPath), ++suffix);
            } while (Directory.Exists(endname) || File.Exists(endname));

            ERROR result = SHCreateDirectory(GetShellView(), endname);

            switch (result)
            {
                case ERROR.FILE_EXISTS:
                case ERROR.ALREADY_EXISTS:
                    throw new IOException("The directory already exists");
                case ERROR.BAD_PATHNAME:
                    throw new IOException("Bad pathname");
                case ERROR.FILENAME_EXCED_RANGE:
                    throw new IOException("The filename is too long");
            }
            return endname;
        }

        public string CreateNewLibrary()
        {

            string name = "New Library";
            int suffix = 0;
            ShellLibrary lib = null;
            try
            {
                lib = ShellLibrary.Load(name, true);
            }
            catch
            {

            }
            if (lib != null)
            {
                do
                {
                    name = String.Format("New Library ({0})",
                        ++suffix);
                    try
                    {
                        lib = ShellLibrary.Load(name, true);
                    }
                    catch
                    {
                        lib = null;
                    }
                } while (lib != null); 
            }


            ShellLibrary libcreate = new ShellLibrary(name, false);

            return libcreate.GetDisplayName(DisplayNameType.Default);
        }

        public string CreateNewLibrary(string name)
        {

            string endname = name;
            int suffix = 0;
            ShellLibrary lib = null;
            try
            {
                lib = ShellLibrary.Load(endname, true);
            }
            catch
            {

            }
            if (lib != null)
            {
                do
                {
                    endname = String.Format(name + "({0})",
                        ++suffix);
                    try
                    {
                        lib = ShellLibrary.Load(endname, true);
                    }
                    catch
                    {
                        lib = null;
                    }
                } while (lib != null);
            }


            ShellLibrary libcreate = new ShellLibrary(endname, false);

            return libcreate.GetDisplayName(DisplayNameType.Default);
        }

        public List<string> RecommendedPrograms(string ext)
        {
            List<string> progs = new List<string>();

            string baseKey = ext;

            using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(baseKey + @"\OpenWithList"))
            {

                if (rk != null)
                {
                    foreach (string item in rk.GetSubKeyNames())
                    {
                        progs.Add(item);
                    } 
                }
            }

            using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(baseKey + @"\OpenWithProgids"))
            {
                if (rk != null)
                {
                    foreach (string item in rk.GetValueNames())
                        progs.Add(item);
                }
            }

            return progs;
        }

        public void SetBackgroundImage(Bitmap Image)
        {
            
            WindowsAPI.SetListViewBackgroundImage(SysListViewHandle, Image);
        }


        [DllImport("Ntshrui.dll")]
        public static extern HResult ShowShareFolderUI(IntPtr hwndParent, IntPtr pszPath);


        private const uint SHOP_FILEPATH = 0x2;
        [DllImport("shell32.dll", SetLastError = true)]
        static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, [MarshalAs(UnmanagedType.LPWStr)] string pszObjectName, [MarshalAs(UnmanagedType.LPWStr)] string pszPropertyPage);

        public void ShowPropPage(IntPtr HWND, string filename, string proppage)
        {

            SHObjectProperties(HWND, SHOP_FILEPATH, filename, proppage);
        }

        public void OpenShareUI()
        {
            ExplorerBrowser.IsBool = true;
            HResult hr = ShowShareFolderUI(this.Handle, Marshal.StringToHGlobalAuto( SelectedItems[0].ParsingName.Replace(@"\\", @"\")));
        }

        public HResult SetFolderIcon(string wszPath, string wszExpandedIconPath, int iIcon)
        {
            HResult hr;

            WindowsAPI.LPSHFOLDERCUSTOMSETTINGS fcs = new WindowsAPI.LPSHFOLDERCUSTOMSETTINGS();
            fcs.dwSize = (uint)Marshal.SizeOf(fcs);
            fcs.dwMask = WindowsAPI.FCSM_ICONFILE;
            fcs.pszIconFile = wszExpandedIconPath.Replace(@"\\", @"\");
            fcs.cchIconFile = 0;
            fcs.iIconIndex = iIcon;

            // Set the folder icon
            hr = WindowsAPI.SHGetSetFolderCustomSettings(ref fcs, wszPath.Replace(@"\\",@"\"), WindowsAPI.FCS_FORCEWRITE);

            if (hr == HResult.Ok)
            {
                // Update the icon cache
                WindowsAPI.SHFILEINFO sfi = new WindowsAPI.SHFILEINFO();
                WindowsAPI.SHGetFileInfo(wszPath.Replace(@"\\", @"\"), 0, ref sfi, (uint)Marshal.SizeOf(sfi), WindowsAPI.SHGFI_ICONLOCATION);
                int iIconIndex = WindowsAPI.Shell_GetCachedImageIndex(sfi.szDisplayName.Replace(@"\\", @"\"), sfi.iIcon, 0);
                WindowsAPI.SHUpdateImage(sfi.szDisplayName.Replace(@"\\", @"\"), sfi.iIcon, 0, iIconIndex);
                //RefreshExplorer();
                WindowsAPI.SHChangeNotify(WindowsAPI.HChangeNotifyEventID.SHCNE_UPDATEIMAGE,
                WindowsAPI.HChangeNotifyFlags.SHCNF_DWORD | WindowsAPI.HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, IntPtr.Zero,
                  (IntPtr)sfi.iIcon);
            }

            return hr;
        }

        public HResult ClearFolderIcon(string wszPath)
        {
            HResult hr;

            WindowsAPI.LPSHFOLDERCUSTOMSETTINGS fcs = new WindowsAPI.LPSHFOLDERCUSTOMSETTINGS();
            fcs.dwSize = (uint)Marshal.SizeOf(fcs);
            fcs.dwMask = WindowsAPI.FCSM_ICONFILE;
            hr = WindowsAPI.SHGetSetFolderCustomSettings(ref fcs, wszPath, WindowsAPI.FCS_FORCEWRITE);
            if (hr == HResult.Ok)
            {
                // Update the icon cache
                WindowsAPI.SHFILEINFO sfi = new WindowsAPI.SHFILEINFO();
                WindowsAPI.SHGetFileInfo(wszPath.Replace(@"\\", @"\"), 0, ref sfi, (uint)Marshal.SizeOf(sfi), WindowsAPI.SHGFI_ICONLOCATION);
                int iIconIndex = WindowsAPI.Shell_GetCachedImageIndex(sfi.szDisplayName.Replace(@"\\", @"\"), sfi.iIcon, 0);
                WindowsAPI.SHUpdateImage(sfi.szDisplayName.Replace(@"\\", @"\"), sfi.iIcon, 0, iIconIndex);
                WindowsAPI.SHChangeNotify(WindowsAPI.HChangeNotifyEventID.SHCNE_UPDATEIMAGE,
                WindowsAPI.HChangeNotifyFlags.SHCNF_DWORD | WindowsAPI.HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, IntPtr.Zero,
                  (IntPtr)sfi.iIcon);
            }

            return hr;
        }


       

        public void RefreshExplorer()
        {
            try
            {
                Guid iid = new Guid(ExplorerBrowserIIDGuid.IShellView);
                IntPtr view = IntPtr.Zero;
                HResult hr = this.explorerBrowserControl.GetCurrentView(ref iid, out view);
                IShellView isv = (IShellView)Marshal.GetObjectForIUnknown(view);
                try
                {

                    isv.Refresh();
                }
                finally
                {
                    Marshal.ReleaseComObject(isv);
                }
            }
            catch (Exception)
            {
                

            }
        }

        
        /// <summary>
        /// Clears the Explorer Browser of existing content, fills it with
        /// content from the specified container, and adds a new point to the Travel Log.
        /// </summary>
        /// <param name="shellObject">The shell container to navigate to.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Will throw if navigation fails for any other reason.</exception>
        public void Navigate(ShellObject shellObject)
        {
            
            if (shellObject == null)
            {
                throw new ArgumentNullException("shellObject");
            }

            if (explorerBrowserControl == null)
            {
                antecreationNavigationTarget = shellObject;
            }
            else
            {
                HResult hr = explorerBrowserControl.BrowseToObject(shellObject.NativeShellItem, 0);
                
                if (hr != HResult.Ok)
                {
                    
                    if ((hr == HResult.ResourceInUse || hr == HResult.Canceled) && NavigationFailed != null)
                    {
                        NavigationFailedEventArgs args = new NavigationFailedEventArgs();
                        args.FailedLocation = shellObject;
                        NavigationFailed(this, args);
                    }
                    else
                    {
                        throw new CommonControlException(LocalizedMessages.ExplorerBrowserBrowseToObjectFailed, hr);
                    }
                }
            }
        }
        public static CM_COLUMNINFO cmi;
        public int ColSize(int index)
        {
            Guid iid = new Guid(ExplorerBrowserIIDGuid.IShellView);
            IntPtr view = IntPtr.Zero;
            HResult hr = this.explorerBrowserControl.GetCurrentView(ref iid, out view);
            IShellView isv = (IShellView)Marshal.GetObjectForIUnknown(view);
            GetColumnInfobyIndex(isv, false, index, out cmi);
            return (int)cmi.uWidth;
        }

        /// <summary>
        /// Navigates within the navigation log. This does not change the set of 
        /// locations in the navigation log.
        /// </summary>
        /// <param name="direction">Forward of Backward</param>
        /// <returns>True if the navigation succeeded, false if it failed for any reason.</returns>
        public bool NavigateLogLocation(NavigationLogDirection direction)
        {
            return NavigationLog.NavigateLog(direction);
        }

        /// <summary>
        /// Navigate within the navigation log. This does not change the set of 
        /// locations in the navigation log.
        /// </summary>
        /// <param name="navigationLogIndex">An index into the navigation logs Locations collection.</param>
        /// <returns>True if the navigation succeeded, false if it failed for any reason.</returns>
        public bool NavigateLogLocation(int navigationLogIndex)
        {
            return NavigationLog.NavigateLog(navigationLogIndex);
        }
        #endregion

        #region events

        /// <summary>
        /// Fires when the SelectedItems collection changes. 
        /// </summary>
        public event EventHandler SelectionChanged;

        public event EventHandler ExplorerGotFocus;

        public event EventHandler RenameFinished;
        public event EventHandler<ExplorerKeyUPEventArgs>  KeyUP;

        /// <summary>
        /// Fires when the Items colection changes. 
        /// </summary>
        public event EventHandler ItemsChanged;

        /// <summary>
        /// Fires when a navigation has been initiated, but is not yet complete.
        /// </summary>
        public event EventHandler<NavigationPendingEventArgs> NavigationPending;

        /// <summary>
        /// Fires when a navigation has been 'completed': no NavigationPending listener 
        /// has cancelled, and the ExplorerBorwser has created a new view. The view 
        /// will be populated with new items asynchronously, and ItemsChanged will be 
        /// fired to reflect this some time later.
        /// </summary>
        public event EventHandler<NavigationCompleteEventArgs> NavigationComplete;

        /// <summary>
        /// Fires when either a NavigationPending listener cancels the navigation, or
        /// if the operating system determines that navigation is not possible.
        /// </summary>
        public event EventHandler<NavigationFailedEventArgs> NavigationFailed;

        /// <summary>
        /// Fires when the ExplorerBorwser view has finished enumerating files.
        /// </summary>
        public event EventHandler ViewEnumerationComplete;

        /// <summary>
        /// Fires when the item selected in the view has changed (i.e., a rename ).
        /// This is not the same as SelectionChanged.
        /// </summary>
        public event EventHandler ViewSelectedItemChanged;

        public event EventHandler ExplorerBrowserMouseLeave;

        public event EventHandler<ViewChangedEventArgs> ViewChanged;

        public event EventHandler<ExplorerAUItemEventArgs> ItemHot;
        public static System.Windows.Forms.Timer Checktmr = new System.Windows.Forms.Timer();

        #endregion

        #region implementation
        public bool IsAfterRename = false;
        public static int PopFX = 0;
        public static int PopFY = 0;
        #region construction
        internal ExplorerBrowserClass explorerBrowserControl;

        // for the IExplorerBrowserEvents Advise call
        internal uint eventsCookie;

        // name of the property bag that contains the view state options of the browser
        string propertyBagName = typeof(ExplorerBrowser).FullName;

        /// <summary>
        /// Initializes the ExplorerBorwser WinForms wrapper.
        /// </summary>
        public ExplorerBrowser()
            : base()
        {
            NavigationOptions = new ExplorerBrowserNavigationOptions(this);
            ContentOptions = new ExplorerBrowserContentOptions(this);
            NavigationLog = new ExplorerBrowserNavigationLog(this);
            //Checktmr.Interval = 100;
            //Checktmr.Tick += new EventHandler(Checktmr_Tick);
            //Checktmr.Start();

            
        }

        void  Checktmr_Tick(object sender, EventArgs e)
        {
            //if (true)
            //    {


                    //IntPtr n = WindowsAPI.FindWindowEx(this.Handle, IntPtr.Zero, "ExplorerBrowserControl", null);
                    //IntPtr z = WindowsAPI.FindWindowEx(n, IntPtr.Zero, "DUIViewWndClassName", null);
                    //IntPtr o = WindowsAPI.FindWindowEx(z, IntPtr.Zero, "DirectUIHWND", null);
                    //IntPtr s1 = WindowsAPI.FindWindowEx(o, IntPtr.Zero, "CtrlNotifySink", null);
                    //IntPtr s2 = WindowsAPI.FindWindowEx(o, s1, "CtrlNotifySink", null);
                    //IntPtr s3 = WindowsAPI.FindWindowEx(o, s2, "CtrlNotifySink", null);
                    //IntPtr k = WindowsAPI.FindWindowEx(s3, IntPtr.Zero, "SHELLDLL_DefView", null);
                    //SysListViewHandle = WindowsAPI.FindWindowEx(k, IntPtr.Zero, "SysListView32", null);
                    //if (SysListViewHandle == IntPtr.Zero)
                    //{
                    //    SysListViewHandle = WindowsAPI.FindWindowEx(k, IntPtr.Zero, "DirectUIHWND", null);
                    //}
                    //IntPtr s4 = WindowsAPI.FindWindowEx(SysListViewHandle, IntPtr.Zero, "CtrlNotifySink", null);
                    //IntPtr s5 = WindowsAPI.FindWindowEx(SysListViewHandle, s4, "CtrlNotifySink", null);
                    //VScrollHandle = WindowsAPI.FindWindowEx(s5, IntPtr.Zero, "ScrollBar", null);

                    WindowsAPI.RECTW r = new WindowsAPI.RECTW();
                    WindowsAPI.GetWindowRect(SysListViewHandle, ref r);
                    Rectangle recb = r.ToRectangle();

                //}

                if (recb.Contains(Cursor.Position))
                {
                    QTTabBarLib.Automation.AutomationManager automan = null;
                    try
                    {
                        automan = new QTTabBarLib.Automation.AutomationManager();
                        automan.DoQuery(factory =>
                        {
                            QTTabBarLib.Automation.AutomationElement el = factory.FromPoint(Cursor.Position);
                            if (el != null)
                            {
                                string name = el.GetClassName();
                                MessageBox.Show(name);
                                if (el.GetClassName() == "UIItem")
                                {
                                   // MessageBox.Show(el.GetBoundingRect().Top.ToString() + "/" + el.GetBoundingRect().Left.ToString() + " - " + el.GetClassName());
                                  //  vItemHot(el.GetClassName(), el.GetItemName(), el.GetBoundingRect(), el.GetItemIndex(), false);
                                }
                                else
                                {
                                    if (el.GetClassName() == "UIItemsView" || el.GetClassName() == "UIGroupItem")
                                    {
                                        vItemHot(el.GetClassName(), el.GetItemName(), el.GetBoundingRect(), el.GetItemIndex(), true, new Rectangle(0,0,0,0), new Rectangle(0,0,0,0));
                                    }
                                    else
                                    {
                                        vItemHot(el.GetClassName(), el.GetItemName(), el.GetBoundingRect(), el.GetItemIndex(),true, new Rectangle(0, 0, 0, 0), new Rectangle(0, 0, 0, 0));
                                    }

                                }
                                el.Dispose();
                            }

                            return 0;
                        });
                        automan.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (automan != null)
                        {
                            automan.Dispose();
                        }
                        //return 1;
                    }
                    
                }
        }

        #endregion

        #region message handlers

        /// <summary>
        /// Displays a placeholder for the explorer browser in design mode
        /// </summary>
        /// <param name="e">Contains information about the paint event.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (DesignMode && e != null)
            {
                using (LinearGradientBrush linGrBrush = new LinearGradientBrush(
                    ClientRectangle,
                    Color.Aqua,
                    Color.CadetBlue,
                    LinearGradientMode.ForwardDiagonal))
                {
                    e.Graphics.FillRectangle(linGrBrush, ClientRectangle);
                }

                using (Font font = new Font("Garamond", 30))
                {
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        e.Graphics.DrawString(
                            "ExplorerBrowserControl",
                            font,
                            Brushes.White,
                            ClientRectangle,
                            sf);
                    }
                }
            }

            base.OnPaint(e);
        }

        ShellObject antecreationNavigationTarget;
        ExplorerBrowserViewEvents viewEvents;

        /// <summary>
        /// Creates and initializes the native ExplorerBrowser control
        /// </summary>
        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            if (this.DesignMode == false)
            {
                explorerBrowserControl = new ExplorerBrowserClass();

                this.BorderStyle = System.Windows.Forms.BorderStyle.None;
                
                // hooks up IExplorerPaneVisibility and ICommDlgBrowser event notifications
                ExplorerBrowserNativeMethods.IUnknown_SetSite(explorerBrowserControl, this);

                // hooks up IExplorerBrowserEvents event notification
                explorerBrowserControl.Advise(
                    Marshal.GetComInterfaceForObject(this, typeof(IExplorerBrowserEvents)),
                    out eventsCookie);

                // sets up ExplorerBrowser view connection point events
                viewEvents = new ExplorerBrowserViewEvents(this);
                if (IsOldSysListView)
                {
                    WindowsAPI.IFolderViewOptions fvo = (WindowsAPI.IFolderViewOptions)explorerBrowserControl;
                    fvo.SetFolderViewOptions(WindowsAPI.FOLDERVIEWOPTIONS.VISTALAYOUT, WindowsAPI.FOLDERVIEWOPTIONS.VISTALAYOUT);
                }
                
                
                NativeRect rect = new NativeRect();
                rect.Top = ClientRectangle.Top - 1;
                rect.Left = ClientRectangle.Left - 1;
                rect.Right = ClientRectangle.Right + 1;
                rect.Bottom = ClientRectangle.Bottom  + 1;

                

                explorerBrowserControl.Initialize(this.Handle, ref rect, null);

                // Force an initial show frames so that IExplorerPaneVisibility works the first time it is set.
                // This also enables the control panel to be browsed to. If it is not set, then navigating to 
                // the control panel succeeds, but no items are visible in the view.
                explorerBrowserControl.SetOptions(ExplorerBrowserOptions.ShowFrames);

                explorerBrowserControl.SetPropertyBag(propertyBagName);


                if (antecreationNavigationTarget != null)
                {
                    //BeginInvoke(new MethodInvoker(
                    //delegate
                    //{
                        Navigate(antecreationNavigationTarget);
                        antecreationNavigationTarget = null;
                    //}));
                }
            }

                Application.AddMessageFilter(this);
        }

        /// <summary>
        /// Sizes the native control to match the WinForms control wrapper.
        /// </summary>
        /// <param name="e">Contains information about the size changed event.</param>
        protected override void OnSizeChanged(EventArgs e)
        {
            if (explorerBrowserControl != null)
            {
                NativeRect rect = new NativeRect();
                rect.Top = ClientRectangle.Top - 1;
                rect.Left = ClientRectangle.Left - 1;
                rect.Right = ClientRectangle.Right + 1;
                rect.Bottom = ClientRectangle.Bottom + 1;

                IntPtr ptr = IntPtr.Zero;
                explorerBrowserControl.SetRect(ref ptr, rect);
            }

            base.OnSizeChanged(e);
        }

        /// <summary>
        /// Cleans up the explorer browser events+object when the window is being taken down.
        /// </summary>
        /// <param name="e">An EventArgs that contains event data.</param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (explorerBrowserControl != null)
            {
                // unhook events
                viewEvents.DisconnectFromView();
                explorerBrowserControl.Unadvise(eventsCookie);
                ExplorerBrowserNativeMethods.IUnknown_SetSite(explorerBrowserControl, null);

                // destroy the explorer browser control
                explorerBrowserControl.Destroy();

                // release com reference to it
                Marshal.ReleaseComObject(explorerBrowserControl);
                explorerBrowserControl = null;
            }

            base.OnHandleDestroyed(e);
        }
        #endregion

        #region object interfaces


        #region IServiceProvider
        /// <summary>
        /// 
        /// </summary>
        /// <param name="guidService">calling service</param>
        /// <param name="riid">requested interface guid</param>
        /// <param name="ppvObject">caller-allocated memory for interface pointer</param>
        /// <returns></returns>
        HResult IServiceProvider.QueryService(
            ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            HResult hr = HResult.Ok;

            if (guidService.CompareTo(new Guid(ExplorerBrowserIIDGuid.IExplorerPaneVisibility)) == 0)
            {
                // Responding to this SID allows us to control the visibility of the 
                // explorer browser panes
                ppvObject =
                    Marshal.GetComInterfaceForObject(this, typeof(IExplorerPaneVisibility));
                hr = HResult.Ok;
            }
            else if (guidService.CompareTo(new Guid(ExplorerBrowserIIDGuid.ICommDlgBrowser)) == 0)
            {
                if (riid.CompareTo(new Guid(ExplorerBrowserIIDGuid.ICommDlgBrowser)) == 0)
                {
                    ppvObject = Marshal.GetComInterfaceForObject(this, typeof(ICommDlgBrowser3));
                    hr = HResult.Ok;
                }
                // The below lines are commented out to decline requests for the ICommDlgBrowser2 interface.
                // This interface is incorrectly marshaled back to unmanaged, and causes an exception.
                // There is a bug for this, I have not figured the underlying cause.
                // Remove this comment and uncomment the following code to enable the ICommDlgBrowser2 interface
                //else if (riid.CompareTo(new Guid(ExplorerBrowserIIDGuid.ICommDlgBrowser2)) == 0)
                //{
                //    ppvObject = Marshal.GetComInterfaceForObject(this, typeof(ICommDlgBrowser3));
                //    hr = HResult.Ok;                    
                //}
                else if (riid.CompareTo(new Guid(ExplorerBrowserIIDGuid.ICommDlgBrowser3)) == 0)
                {
                    ppvObject = Marshal.GetComInterfaceForObject(this, typeof(ICommDlgBrowser3));
                    hr = HResult.Ok;
                }
                else
                {
                    ppvObject = IntPtr.Zero;
                    hr = HResult.NoInterface;
                }
            }
            else
            {
                IntPtr nullObj = IntPtr.Zero;
                ppvObject = nullObj;
                hr = HResult.NoInterface;
            }

            return hr;
        }
        #endregion

        #region IExplorerPaneVisibility
        /// <summary>
        /// Controls the visibility of the explorer borwser panes
        /// </summary>
        /// <param name="explorerPane">a guid identifying the pane</param>
        /// <param name="peps">the pane state desired</param>
        /// <returns></returns>
        HResult IExplorerPaneVisibility.GetPaneState(ref Guid explorerPane, out ExplorerPaneState peps)
        {
            switch (explorerPane.ToString())
            {
                case ExplorerBrowserViewPanes.AdvancedQuery:
                    peps = VisibilityToPaneState(NavigationOptions.PaneVisibility.AdvancedQuery);
                    break;
                case ExplorerBrowserViewPanes.Commands:
                    peps = VisibilityToPaneState(NavigationOptions.PaneVisibility.Commands);
                    break;
                case ExplorerBrowserViewPanes.CommandsOrganize:
                    peps = VisibilityToPaneState(NavigationOptions.PaneVisibility.CommandsOrganize);
                    break;
                case ExplorerBrowserViewPanes.CommandsView:
                    peps = VisibilityToPaneState(NavigationOptions.PaneVisibility.CommandsView);
                    break;
                case ExplorerBrowserViewPanes.Details:
                    peps = VisibilityToPaneState(NavigationOptions.PaneVisibility.Details);
                    break;
                case ExplorerBrowserViewPanes.Navigation:
                    peps = VisibilityToPaneState(NavigationOptions.PaneVisibility.Navigation);
                    break;
                case ExplorerBrowserViewPanes.Preview:
                    peps = VisibilityToPaneState(NavigationOptions.PaneVisibility.Preview);
                    break;
                case ExplorerBrowserViewPanes.Query:
                    peps = VisibilityToPaneState(NavigationOptions.PaneVisibility.Query);
                    break;
                default:
#if LOG_UNKNOWN_PANES
                    System.Diagnostics.Debugger.Log( 4, "ExplorerBrowser", "unknown pane view state. id=" + explorerPane.ToString( ) );
#endif
                    peps = VisibilityToPaneState(PaneVisibilityState.Show);
                    break;
            }

            return HResult.Ok;
        }

        private static ExplorerPaneState VisibilityToPaneState(PaneVisibilityState visibility)
        {
            switch (visibility)
            {
                case PaneVisibilityState.DoNotCare:
                    return ExplorerPaneState.DoNotCare;

                case PaneVisibilityState.Hide:
                    return ExplorerPaneState.DefaultOff | ExplorerPaneState.Force;

                case PaneVisibilityState.Show:
                    return ExplorerPaneState.DefaultOn | ExplorerPaneState.Force;

                default:
                    throw new ArgumentException("unexpected PaneVisibilityState");
            }
        }

        #endregion

        #region IExplorerBrowserEvents
        HResult IExplorerBrowserEvents.OnNavigationPending(IntPtr pidlFolder)
        {
            bool canceled = false;
            if (NavigationPending != null)
            {
                NavigationPendingEventArgs args = new NavigationPendingEventArgs();

                // For some special items (like network machines), ShellObject.FromIDList
                // might return null
                args.PendingLocation = ShellObjectFactory.Create(pidlFolder);

                if (args.PendingLocation != null)
                {
                    foreach (Delegate del in NavigationPending.GetInvocationList())
                    {
                        del.DynamicInvoke(new object[] { this, args });
                        if (args.Cancel)
                        {
                            canceled = true;
                        }
                    }
                }
            }

            return canceled ? HResult.Canceled : HResult.Ok;
        }

        HResult IExplorerBrowserEvents.OnViewCreated(object psv)
        {
            viewEvents.ConnectToView((IShellView)psv);
            
            
            return HResult.Ok;
        }

        HResult IExplorerBrowserEvents.OnNavigationComplete(IntPtr pidlFolder)
        {
            IsPressedLKButton = false;
            // view mode may change 
            ContentOptions.folderSettings.Options |= FolderOptions.SnapToGrid;
            ContentOptions.folderSettings.Options |= FolderOptions.AutoArrange;
            ContentOptions.folderSettings.ViewMode = GetCurrentViewMode();
            //ContentOptions.ThumbnailSize = GetCurrentthumbSize();
            
            if (NavigationComplete != null)
            {
                NavigationCompleteEventArgs args = new NavigationCompleteEventArgs();
                args.NewLocation = ShellObjectFactory.Create(pidlFolder);
                NavigationComplete(this, args);
                CurrentLocation = args.NewLocation;
            }
            FlushMemory();
            return HResult.Ok;
        }

        HResult IExplorerBrowserEvents.OnNavigationFailed(IntPtr pidlFolder)
        {
            if (NavigationFailed != null)
            {
                NavigationFailedEventArgs args = new NavigationFailedEventArgs();
                args.FailedLocation = ShellObjectFactory.Create(pidlFolder);
                NavigationFailed(this, args);
            }
            return HResult.Ok;
        }
        #endregion

        #region ICommDlgBrowser
        HResult ICommDlgBrowser3.OnDefaultCommand(IntPtr ppshv)
        {
            if (!SelectedItems[0].IsFolder)
            {
                return HResult.False;
            }
            else
            {

                if (Path.GetExtension(SelectedItems[0].ParsingName).ToLowerInvariant() == ".zip")
                {
                    return HResult.False;
                }
                else
                {
                    Navigate(SelectedItems[0]);
                    return HResult.Ok;
                }
            }
            //return HResult.False;
            
        }

        HResult ICommDlgBrowser3.OnStateChange(IntPtr ppshv, CommDlgBrowserStateChange uChange)
        {
            if (uChange == CommDlgBrowserStateChange.SelectionChange)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    FireSelectionChanged();
                }));
                
            }
            if (uChange == CommDlgBrowserStateChange.SetFocus)
            {
                ExplorerGotFocusRaized();
            }
            if (uChange == CommDlgBrowserStateChange.Rename)
            {
                if (RenameFinished != null)
                {
                    RenameFinished(this, EventArgs.Empty);
                }
            }


            if (uChange == CommDlgBrowserStateChange.KillFocus)
            {
                
            }
            return HResult.Ok;
        }

        HResult ICommDlgBrowser3.IncludeObject(IntPtr ppshv, IntPtr pidl)
        {
            // items in the view have changed, so the collections need updating
            FireContentChanged();

            return HResult.Ok;
        }

        #endregion

        #region ICommDlgBrowser2 Members

        // The below methods can be called into, but marshalling the response causes an exception to be
        // thrown from unmanaged code.  At this time, I decline calls requesting the ICommDlgBrowser2
        // interface.  This is logged as a bug, but moved to less of a priority, as it only affects being
        // able to change the default action text for remapping the default action.

        HResult ICommDlgBrowser3.GetDefaultMenuText(IShellView shellView, IntPtr text, int cchMax)
        {
            return HResult.False;
            //return HResult.Ok;
            //OK if new
            //False if default
            //other if error
        }

        HResult ICommDlgBrowser3.GetViewFlags(out uint pdwFlags)
        {
            //var flags = CommDlgBrowser2ViewFlags.NoSelectVerb;
            //Marshal.WriteInt32(pdwFlags, 0);
            pdwFlags = (uint)CommDlgBrowser2ViewFlags.ShowAllFiles;
            return HResult.Ok;
        }

        HResult ICommDlgBrowser3.Notify(IntPtr pshv, CommDlgBrowserNotifyType notifyType)
        {
            
            return HResult.Canceled;
        }

        #endregion

        #region ICommDlgBrowser3 Members

        HResult ICommDlgBrowser3.GetCurrentFilter(StringBuilder pszFileSpec, int cchFileSpec)
        {
            // If the method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
            return HResult.Ok;
        }

        HResult ICommDlgBrowser3.OnColumnClicked(IShellView ppshv, int iColumn)
        {
            // If the method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
            return HResult.Ok;
        }

        HResult ICommDlgBrowser3.OnPreViewCreated(IShellView ppshv)
        {

            // If the method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code
            return HResult.Ok;
        }

        #endregion

        #region IMessageFilter Members
        bool Ctrl = false;
        public QTTabBarLib.Automation.AutomationManager automan = new QTTabBarLib.Automation.AutomationManager();
        bool IsPressedLKButton = false;
        bool IsMouseClickOnHeader = false;
        bool IsMouseClickOutsideLV = true;
        bool IsGetHWnd = false;
        bool IMessageFilter.PreFilterMessage(ref Message m)
        {
            HResult hr = HResult.False;
            Message M = m;
            if (explorerBrowserControl != null)
            {
                // translate keyboard input


                if (m.Msg == (int)WindowsAPI.WndMsg.WM_ACTIVATEAPP)
                {
                    MessageBox.Show("Active");
                }

                if (m.Msg == (int)WindowsAPI.WndMsg.WM_SETFOCUS)
                {
                    MessageBox.Show("Active");
                }
                // Catch Left Mouse Click key
                if ((m.Msg == (int)WindowsAPI.WndMsg.WM_LBUTTONDOWN))
                {
                    if (KeyUP != null)
                    {
                        ExplorerKeyUPEventArgs args = new ExplorerKeyUPEventArgs();
                        args.Key = 778;
                        KeyUP(this, args);
                    }
                    //WindowsAPI.SetFocus(SysListViewHandle);
                    WindowsAPI.RECTW r = new WindowsAPI.RECTW();
                    WindowsAPI.GetWindowRect(SysListViewHandle, ref r);
                    Rectangle reclv = r.ToRectangle();
                    if (Cursor.Position.Y >= reclv.Top && Cursor.Position.Y <= reclv.Top + 29)
                    {
                        IsMouseClickOnHeader = true;
                    }
                    else
                    {
                        IsMouseClickOnHeader = false;
                    }
                    if (reclv.Contains(Cursor.Position))
                    {
                        IsPressedLKButton = true;
                    }
                    else
                    {
                        IsPressedLKButton = false;
                    }
                    //MessageBox.Show(Cursor.Position.Y.ToString() + @"\" + reclv.Top.ToString());
                    if (Cursor.Position.Y <= reclv.Top || Cursor.Position.Y >= reclv.Bottom)
                    {
                        IsMouseClickOutsideLV = true;
                    }
                    else
                    {
                        IsMouseClickOutsideLV = false;
                    }
                    
                }

                if ((m.Msg == (int)WindowsAPI.WndMsg.WM_LBUTTONUP))
                {
                    IsPressedLKButton = false;
                    IsMouseClickOnHeader = false;
                    //WindowsAPI.ReleaseCapture();
                    //WindowsAPI.SetFocus(SysListViewHandle);
                    if (KeyUP != null)
                    {
                        ExplorerKeyUPEventArgs args = new ExplorerKeyUPEventArgs();
                        args.Key = 777;
                        KeyUP(this, args);
                    }
                }

                if (m.Msg == (int)WindowsAPI.WndMsg.WM_XBUTTONUP)
                {
                    switch (HiWord((uint)m.WParam))
                    {
                        case 1:
                            NavigateLogLocation(NavigationLogDirection.Backward);
                            break;
                        case 2:
                            NavigateLogLocation(NavigationLogDirection.Forward);
                            break;
                    }

                }
                bool h = false;


                if ((m.Msg == (int)WindowsAPI.WndMsg.WM_SYSCOMMAND))
                {



                }

                if (m.Msg == (int)WindowsAPI.WndMsg.WM_MOUSEWHEEL)
                {
                    Int64 Wheel_delta = HiWord((Int64)m.WParam);
                    if (LoWord((Int64)m.WParam) == 0x0008) //Ctrl is down
                    {
                        if (Wheel_delta < 0)
                        {
                            if (ContentOptions.ThumbnailSize > 34)
                            {
                                if (ContentOptions.ViewMode != ExplorerBrowserViewMode.Thumbnail)
                                {
                                    ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
                                }
                                ContentOptions.ThumbnailSize = ContentOptions.ThumbnailSize - 10; 
                            }
                            
                            
                        }
                        else
                        {
                            if (ContentOptions.ThumbnailSize < 255)
                            {
                                if (ContentOptions.ViewMode != ExplorerBrowserViewMode.Thumbnail)
                                {
                                    ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
                                }
                                ContentOptions.ThumbnailSize = ContentOptions.ThumbnailSize + 10;
                            }
                            
                        }
                    }
                }

                if ((m.Msg == (int)WindowsAPI.WndMsg.WM_MOUSE_ENTER))
                {
                    //WindowsAPI.SetFocus(SysListViewHandle);
                }

                if ((m.Msg == (int)WindowsAPI.WndMsg.WM_MOUSE_LEAVE))
                {
                    ExplorerBrowserMouseLeave.Invoke(this, null);
                }


                if (m.Msg == (int)WindowsAPI.WndMsg.WM_KEYDOWN)
                {
                    // Catch ESC key
                    if (((int)m.WParam == 27))
                        OnEscKey();

                    if (KeyUP != null)
                    {
                        ExplorerKeyUPEventArgs args = new ExplorerKeyUPEventArgs();
                        args.Key = (int)m.WParam;
                        KeyUP(this, args);
                    }

                    if ((int)m.WParam == (int)Keys.ControlKey)
                    {
                        Ctrl = true;
                    }
                    if (!IsExFileOpEnabled)
                    {
                        if ((int)m.WParam == (int)Keys.X && Ctrl)
                        {
                            IsMoveClipboardOperation = true;
                        }
                        if ((int)m.WParam == (int)Keys.C && Ctrl)
                        {
                            IsMoveClipboardOperation = false;
                        }

                        if ((int)m.WParam == (int)Keys.V && Ctrl)
                        {

                            if (!Clipboard.ContainsText())
                            {
                                if (Clipboard.ContainsFileDropList())
                                {
                                    ShellObject sho;
                                    try
                                    {
                                        sho = ShellObject.FromParsingName(System.Windows.Forms.Clipboard.GetFileDropList()[0]);
                                    }
                                    catch (Exception)
                                    {

                                        sho = null;
                                    }

                                    if (sho != null)
                                    {
                                        sho.Dispose();
                                        DropData PasteData = new DropData();
                                        PasteData.DropList = System.Windows.Forms.Clipboard.GetFileDropList();
                                        if (SelectedItems.Count > 0 & SelectedItems.Count < 2)
                                        {
                                            PasteData.PathForDrop = SelectedItems[0].ParsingName;

                                        }
                                        else
                                        {
                                            PasteData.PathForDrop = NavigationLog.CurrentLocation.ParsingName;
                                        }
                                        Thread t = null;
                                        if (IsMoveClipboardOperation)
                                        {
                                            t = new Thread(new ParameterizedThreadStart(DoMove));
                                        }
                                        else
                                        {
                                            t = new Thread(new ParameterizedThreadStart(DoCopy));
                                        }

                                        t.SetApartmentState(ApartmentState.STA);
                                        t.Start(PasteData);
                                        return true;
                                    }
                                }

                            } 
                        }

                    }
                }
                if (m.Msg == (int)WindowsAPI.WndMsg.WM_KEYUP)
                {
                    if (KeyUP != null)
                    {
                        ExplorerKeyUPEventArgs args = new ExplorerKeyUPEventArgs();
                        args.Key = (int)m.WParam;
                        KeyUP(this, args);
                    }

                    if ((int)m.WParam == (int)Keys.ControlKey)
                    {
                        Ctrl = false;
                    }
                    if (!IsExFileOpEnabled)
                    {
                        if ((int)m.WParam == (int)Keys.V && Ctrl)
                        {
                            return true;
                        }
                    }
                    
                }



                if ((m.Msg == (int)WindowsAPI.WndMsg.WM_MOUSEMOVE))
                {
                    WindowsAPI.RECTW rscroll = new WindowsAPI.RECTW();
                    WindowsAPI.GetWindowRect(VScrollHandle, ref rscroll);
                    WindowsAPI.RECTW rr = new WindowsAPI.RECTW();
                    WindowsAPI.GetWindowRect(SysListViewHandle, ref rr);
                    Rectangle reclv = rr.ToRectangle();
                    Rectangle rec2 = new Rectangle(reclv.X + 1, rscroll.Top + 30, reclv.Width - 3,
                        rscroll.Bottom - rscroll.Top - 5 - 30);

                    //ExplorerBrowser.Checktmr.Start();
                    //A workarownd to ugly AutoScroll bug in IExplorerBrowsers
                    if (m.WParam.ToInt32() == 0x0001 && !IsMouseClickOnHeader && !IsMouseClickOutsideLV)
                    {

                        ShellObject s;
                        
                        if (rscroll.Height > 0)
                            if (!rec2.Contains(Cursor.Position))
                            {

                                if (Cursor.Position.Y <= rec2.Y)
                                {
                                    Cursor.Position = new Point(Cursor.Position.X, rec2.Top);
                                    return true;
                                }
                                if (Cursor.Position.Y >= rec2.Bottom)
                                {

                                    Cursor.Position = new Point(Cursor.Position.X, rec2.Bottom - 1);
                                    return true;
                                }

                            }
                    }
                    else
                    {
                        BeginInvoke(new MethodInvoker(
                        delegate
                        {
                            Guid iid = new Guid(ExplorerBrowserIIDGuid.IShellView);
                            IntPtr view = IntPtr.Zero;
                            HResult hrr = this.explorerBrowserControl.GetCurrentView(ref iid, out view);
                            if (view != IntPtr.Zero)
                            {
                                IShellView isv = (IShellView)Marshal.GetObjectForIUnknown(view);
                                AvailableVisibleColumns = AvailableColumns(isv, false);
                                Marshal.ReleaseComObject(isv);
                                
                            }
                            
                        }));
                    }

                    if (!reclv.Contains(Cursor.Position))
                    {
                        if (ExplorerBrowserMouseLeave != null)
                            ExplorerBrowserMouseLeave.Invoke(this, null);
                    }

                    BeginInvoke(new MethodInvoker(
                    delegate
                    {

                        WindowsAPI.RECTW r = new WindowsAPI.RECTW();
                        WindowsAPI.GetWindowRect(SysListViewHandle, ref r);
                        Rectangle recb = r.ToRectangle();
                        Rectangle PopFR = new Rectangle(PopFX, PopFY, 22, 22);
                        if (recb.Contains(Cursor.Position))
                        {
                            if (CurX != ((int)M.LParam & 0x0000FFFF) | CurY != HiWord((uint)M.LParam))
                            {

                                try
                                {

                                    if (!IsBool && !PopFR.Contains(Cursor.Position))
                                    {


//                                        automan.DoQuery(factory =>
//                                        {
//                                            QTTabBarLib.Automation.AutomationElement el;
//                                            try
//                                            {
//                                                el = factory.FromPoint(Cursor.Position);
//                                            }
//                                            catch
//                                            {
//
//                                                el = null;
//                                            }
//
//                                            if (el != null)
//                                            {
//                                                string name = el.GetClassName();
//                                                string name2 = el.GetParent().GetClassName();
//                                                if (name == "UIProperty" || name == "UIImage")
//                                                {
//                                                    el = el.GetParent();
//                                                }
//
//                                                if (el.GetClassName() == "UIItem")
//                                                {
//                                                    //MessageBox.Show(el.GetBoundingRect().Top.ToString() + "/" + el.GetBoundingRect().Left.ToString() + " - " + el.GetClassName());
//                                                    Rectangle ImageRecT = new Rectangle();
//                                                    Rectangle TextRecT = new Rectangle();
//
//                                                    foreach (QTTabBarLib.Automation.AutomationElement elm in el.GetChildren())
//                                                    {
//                                                        //MessageBox.Show(elm.GetClassName());
//                                                        if (elm.GetClassName() == "UIImage")
//                                                        {
//                                                            ImageRecT = elm.GetBoundingRect();
//                                                        }
//                                                        if (elm.GetClassName() == "UIProperty")
//                                                        {
//
//                                                            TextRecT = new Rectangle(ImageRecT.Left,
//                                                                ImageRecT.Top,
//                                                                AvailableVisibleColumns[0].Width, elm.GetBoundingRect().Height);
//                                                        }
//                                                    }
//
//                                                    vItemHot(el.GetClassName(), el.GetItemName(), el.GetBoundingRect(), el.GetItemIndex(), false, ImageRecT, TextRecT);
//                                                }
//                                                else
//                                                {
//                                                    //MessageBox.Show(name);
//                                                    if (el.GetClassName() == "UIItemsView" | el.GetClassName() == "UIGroupItem")
//                                                    {
//                                                        vItemHot(el.GetClassName(), el.GetItemName(), el.GetBoundingRect(), el.GetItemIndex(), true, new Rectangle(0, 0, 0, 0), new Rectangle(0, 0, 0, 0));
//                                                    }
//                                                    else
//                                                    {
//                                                        vItemHot(el.GetClassName(), el.GetItemName(), el.GetBoundingRect(), el.GetItemIndex(), true, new Rectangle(0, 0, 0, 0), new Rectangle(0, 0, 0, 0));
//                                                    }
//
//                                                }
//                                                el.Dispose();
//                                            }
//
//                                            return 1;
//                                        });
                                    }
                                    else
                                    {

                                    }

                                }
                                catch (Exception ex)
                                {
                                    if (automan != null)
                                    {
                                        automan.Dispose();
                                    }
                                    //return 1;
                                }
                                CurX = (int)LoWord((uint)M.LParam);
                                CurY = HiWord((uint)M.LParam);

                            }
                        }
                    }));

                }

                if (m.Msg == (int)WindowsAPI.WndMsg.WM_PAINT)
                {
                    if (IsOldSysListView)
                    {
                        WindowsAPI.SendMessage(SysListViewHandle, 296, MAKELONG(1, 1), 0);
                    }

                }
                Invoke(new MethodInvoker(
                    delegate
                    {
                        hr = ((IInputObject)explorerBrowserControl).TranslateAcceleratorIO(ref M);
                    }));
            }
            return (hr == HResult.Ok);
        }

        public static Int64 HiWord(Int64 number)
        {
            if ((number & 0x80000000) == 0x80000000)
                return (number >> 16);
            else
                return (number >> 16) & 0xffff;
        }

        public static Int64 LoWord(Int64 number)
        {
            return number & 0x0000FFFF;
        }

        int CurX;
        long CurY;

        #endregion

        #endregion

        #region utilities
        private static short LOWORD(int dw)
        {
            short loWord = 0;
            ushort andResult = (ushort)(dw & 0x00007FFF);
            ushort mask = 0x8000;
            if ((dw & 0x8000) != 0)
            {
                loWord = (short)(mask | andResult);
            }
            else
            {
                loWord = (short)andResult;
            }
            return loWord;
        }

        private static int MAKELONG(int wLow, int wHigh)
        {
            int low = (int)LOWORD(wLow);
            short high = LOWORD(wHigh);
            int product = 0x00010000 * (int)high;
            int makeLong = (int)(low | product);
            return makeLong;
        }

        protected const int WM_CHANGEUISTATE = 0x00000127;
        protected const int UIS_SET = 1;
        protected const int UIS_CLEAR = 2;
        protected const int UIS_INITIALIZE = 3;

        protected const short UISF_HIDEFOCUS = 0x0001;
        protected const short UISF_HIDEACCEL = 0x0002;
        protected const short UISF_ACTIVE = 0x0004;

        public void HideFocusRectangle()
        {
            WindowsAPI.SendMessage(SysListViewHandle, 296, MAKELONG(1,1), 0);
           
        }
        /// <summary>
        /// Returns the current view mode of the browser
        /// </summary>
        /// <returns></returns>
        internal FolderViewMode GetCurrentViewMode()
        {
            IFolderView2 ifv2 = GetFolderView2();
            uint viewMode = 0;
            if (ifv2 != null)
            {
                try
                {
                    HResult hr = ifv2.GetCurrentViewMode(out viewMode);
                    if (hr != HResult.Ok) { throw new ShellException(hr); }
                }
                finally
                {
                    Marshal.ReleaseComObject(ifv2);
                    ifv2 = null;
                }
            }
            return (FolderViewMode)viewMode;
        }

        /// <summary>
        /// Returns the curent View Thumb size
        /// </summary>
        /// <returns></returns>
        internal int GetCurrentthumbSize()
        {
            IFolderView2 ifv2 = GetFolderView2();
            int viewMode = 0;
            int IconSize = -1;
            if (ifv2 != null)
            {
                try
                {
                    HResult hr = ifv2.GetViewModeAndIconSize(out viewMode, out IconSize);
                    if (hr != HResult.Ok) { throw new ShellException(hr); }
                }
                finally
                {
                    Marshal.ReleaseComObject(ifv2);
                    ifv2 = null;
                }
            }
            return IconSize;
        }

        /// <summary>
        /// Sets the column in which will be sorted
        /// </summary>
        /// <param name="pk">The propertykey that identifies column</param>
        /// <param name="Order">The sort order</param>
        public void SetSortCollumn(PROPERTYKEY pk, SORT Order)
        {
            
            IFolderView2 ifv2 = GetFolderView2();
            SORTCOLUMN sc = new SORTCOLUMN();
            sc.propkey = pk;
            sc.direction = Order;
            IntPtr scptr = Marshal.AllocHGlobal(Marshal.SizeOf(sc));
            Marshal.StructureToPtr(sc, scptr, false);
            ifv2.SetSortColumns(scptr, 1);
            Marshal.FreeHGlobal(scptr);
        }

        /// <summary>
        /// Sets the column in which will be sorted
        /// </summary>
        /// <param name="pk">The propertykey list that identifies columns</param>
        /// <param name="Order">The sort order list</param>
        public void SetSortCollumn(List<PROPERTYKEY> pk, List<SORT> Order)
        {
            SORTCOLUMN[] scl = new SORTCOLUMN[pk.Count];
            IFolderView2 ifv2 = GetFolderView2();
            for (int i = 0; i < pk.Count; i++)
            {
                SORTCOLUMN sc = new SORTCOLUMN();
                sc.propkey = pk[i];
                sc.direction = Order[i];
                scl[i] = sc;
            }

            SetSortColumns(ifv2, pk.Count, scl);
        }

        public void FormatDrive()
        {
            string DriveLetter = "";
            if (SelectedItems.Count > 0)
            {
                if (Directory.GetLogicalDrives().Contains(SelectedItems[0].ParsingName))
                {
                    DriveLetter = SelectedItems[0].ParsingName;
                }
                else
                {
                    DriveLetter = NavigationLog.CurrentLocation.ParsingName;
                } 
            }
            else
            {
                DriveLetter = NavigationLog.CurrentLocation.ParsingName;
            }
            WindowsAPI.FormatDrive(SysListViewHandle, DriveLetter);
        }

        public void CleanupDrive()
        {
            string DriveLetter = "";
            if (SelectedItems.Count > 0)
            {
                if (Directory.GetLogicalDrives().Contains(SelectedItems[0].ParsingName))
                {
                    DriveLetter = SelectedItems[0].ParsingName;
                }
                else
                {
                    DriveLetter = NavigationLog.CurrentLocation.ParsingName;
                }
            }
            else
            {
                DriveLetter = NavigationLog.CurrentLocation.ParsingName;
            }
            Process.Start("Cleanmgr.exe", "/d" + DriveLetter.Replace(":\\",""));
        }
        public void DefragDrive()
        {
            string DriveLetter = "";
            if (SelectedItems.Count > 0)
            {
                if (Directory.GetLogicalDrives().Contains(SelectedItems[0].ParsingName))
                {
                    DriveLetter = SelectedItems[0].ParsingName;
                }
                else
                {
                    DriveLetter = NavigationLog.CurrentLocation.ParsingName;
                }
            }
            else
            {
                DriveLetter = NavigationLog.CurrentLocation.ParsingName;
            }
            Process.Start(Path.Combine(Environment.SystemDirectory,"dfrgui.exe"), "/u /v " + DriveLetter.Replace("\\", ""));
        }

        public void RunExeAsAdmin(string ExePath)
        {
            var psi = new ProcessStartInfo
            { 
                FileName = ExePath,
                Verb = "runas",
                UseShellExecute = true,
                Arguments = "/env /user:" + "Administrator " + "\"" + ExePath + "\"",
            };
            Process.Start(psi);
           
        }

        public void StartCompartabilityWizzard()
        {
            Process.Start("msdt.exe", "-id PCWDiagnostic");
        }

        /// <summary>
        /// Gets the IFolderView2 interface from the explorer browser.
        /// </summary>
        /// <returns></returns>
        public IFolderView2 GetFolderView2()
        {
            Guid iid = new Guid(ExplorerBrowserIIDGuid.IFolderView2);
            IntPtr view = IntPtr.Zero;
            if (this.explorerBrowserControl != null)
            {
                HResult hr = this.explorerBrowserControl.GetCurrentView(ref iid, out view);
                switch (hr)
                {
                    case HResult.Ok:
                        break;

                    case HResult.NoInterface:
                    case HResult.Fail:
#if LOG_KNOWN_COM_ERRORS
                        Debugger.Log( 2, "ExplorerBrowser", "Unable to obtain view. Error=" + e.ToString( ) );
#endif
                        return null;

                    default:
                        throw new CommonControlException(LocalizedMessages.ExplorerBrowserFailedToGetView, hr);
                }

                return (IFolderView2)Marshal.GetObjectForIUnknown(view);
            }
            return null;
        }

        public IFolderView2 GetFolderView()
        {
            Guid iid = new Guid(ExplorerBrowserIIDGuid.IFolderView);
            IntPtr view = IntPtr.Zero;
            if (this.explorerBrowserControl != null)
            {
                HResult hr = this.explorerBrowserControl.GetCurrentView(ref iid, out view);
                switch (hr)
                {
                    case HResult.Ok:
                        break;

                    case HResult.NoInterface:
                    case HResult.Fail:
#if LOG_KNOWN_COM_ERRORS
                        Debugger.Log( 2, "ExplorerBrowser", "Unable to obtain view. Error=" + e.ToString( ) );
#endif
                        return null;

                    default:
                        throw new CommonControlException(LocalizedMessages.ExplorerBrowserFailedToGetView, hr);
                }

                return (IFolderView2)Marshal.GetObjectForIUnknown(view);
            }
            return null;
        }

        /// <summary>
        /// Gets the selected items in the explorer browser as an IShellItemArray
        /// </summary>
        /// <returns></returns>
        internal IShellItemArray GetSelectedItemsArray()
        {
            IShellItemArray iArray = null;
            IFolderView2 iFV2 = GetFolderView2();
            if (iFV2 != null)
            {
                try
                {
                    Guid iidShellItemArray = new Guid(ShellIIDGuid.IShellItemArray);
                    object oArray = null;
                    HResult hr = iFV2.Items((uint)WindowsAPI.SVGIO.SVGIO_SELECTION, ref iidShellItemArray, out oArray);
                    iArray = oArray as IShellItemArray;
                    if (hr != HResult.Ok &&
                        hr != HResult.ElementNotFound &&
                        hr != HResult.Fail)
                    {
                        throw new COMException("unexpected error retrieving selection", (int)hr);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(iFV2);
                    iFV2 = null;
                }
            }

            return iArray;
        }

        /// <summary>
        /// Returns the number of the items in current view
        /// </summary>
        /// <returns></returns>
        public int GetItemsCount()
        {
            int itemsCount = 0;

            IFolderView2 iFV2 = GetFolderView2();
            if (iFV2 != null)
            {
                try
                {
                    HResult hr = iFV2.ItemCount((uint)ShellViewGetItemObject.AllView, out itemsCount);

                    if (hr != HResult.Ok &&
                        hr != HResult.ElementNotFound &&
                        hr != HResult.Fail)
                    {
                        throw new CommonControlException(LocalizedMessages.ExplorerBrowserItemCount, hr);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(iFV2);
                    iFV2 = null;
                }
            }

            return itemsCount;
        }

        /// <summary>
        /// Returns the number of selected items in current view
        /// </summary>
        /// <returns></returns>
        public int GetSelectedItemsCount()
        {
            int itemsCount = 0;

            IFolderView2 iFV2 = GetFolderView2();
            if (iFV2 != null)
            {
                try
                {
                    HResult hr = iFV2.ItemCount((uint)ShellViewGetItemObject.Selection, out itemsCount);

                    if (hr != HResult.Ok &&
                        hr != HResult.ElementNotFound &&
                        hr != HResult.Fail)
                    {
                        throw new CommonControlException(LocalizedMessages.ExplorerBrowserSelectedItemCount, hr);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(iFV2);
                    iFV2 = null;
                }
            }

            return itemsCount;
        }

        /// <summary>
        /// Sets the column in which current view will be grouped
        /// </summary>
        /// <param name="pk">The propertykey that identifies column</param>
        /// <param name="Asc">true-ascesing order; false- descesing order</param>
        public void SetGroupCollumn(PROPERTYKEY pk, bool Asc)
        {
            IFolderView2 ifv2 = GetFolderView2();
            IntPtr scptr = Marshal.AllocHGlobal(Marshal.SizeOf(pk));
            Marshal.StructureToPtr(pk, scptr, false);
            ifv2.SetGroupBy(scptr, Asc);
            Marshal.FreeHGlobal(scptr);
        }

        /// <summary>
        /// Gets the items in the ExplorerBrowser as an IShellItemArray
        /// </summary>
        /// <returns></returns>
        internal IShellItemArray GetItemsArray()
        {
            IShellItemArray iArray = null;
            IFolderView2 iFV2 = GetFolderView2();
            if (iFV2 != null)
            {
                try
                {
                    Guid iidShellItemArray = new Guid(ShellIIDGuid.IShellItemArray);
                    object oArray = null;
                    HResult hr = iFV2.Items((uint)ShellViewGetItemObject.AllView, ref iidShellItemArray, out oArray);
                    if (hr != HResult.Ok &&
                        hr != HResult.Fail &&
                        hr != HResult.ElementNotFound &&
                        hr != HResult.InvalidArguments)
                    {
                        throw new CommonControlException(LocalizedMessages.ExplorerBrowserViewItems, hr);
                    }

                    iArray = oArray as IShellItemArray;
                }
                finally
                {
                    Marshal.ReleaseComObject(iFV2);
                    iFV2 = null;
                }
            }
            return iArray;
        }

        /// <summary>
        /// Returns IShellItem from parent item and pidl
        /// </summary>
        /// <param name="parent">The parent folder</param>
        /// <param name="pidl">the PIDL of the item</param>
        /// <returns></returns>
        public static IShellItem CreateItemWithParent(IShellFolder parent, IntPtr pidl)
        {

            return SHCreateItemWithParent(IntPtr.Zero,
                                          parent, pidl, typeof(IShellItem).GUID);
        }

        /// <summary>
        /// Returns the IShellFolder interface from IShellItem
        /// </summary>
        /// <param name="sitem">The IShellItem</param>
        /// <returns></returns>
        public IShellFolder GetIShellFolder(IShellItem sitem)
        {
            IShellFolder result;
            ((IShellItem2)sitem).BindToHandler(IntPtr.Zero,
                BHID.SFObject, typeof(IShellFolder).GUID, out result);
            return result;
        }

        /// <summary>
        /// Returns the IShellFolder interface from IShellItem
        /// </summary>
        /// <param name="sitem">The IShellItem represented like ShellObject</param>
        /// <returns></returns>
        public IShellFolder GetIShellFolder(ShellObject sitem)
        {
            IShellFolder result;
            ((IShellItem2)sitem.nativeShellItem).BindToHandler(IntPtr.Zero,
                BHID.SFObject, typeof(IShellFolder).GUID, out result);
            return result;
        }

        /// <summary>
        /// Return the Shellobject collection from IDataObject interface
        /// </summary>
        /// <param name="pDataObj">IDataObject interface of data</param>
        /// <returns></returns>
        ShellObject[] ParseShellIDListArray(System.Runtime.InteropServices.ComTypes.IDataObject pDataObj)
        {
            List<ShellObject> result = new List<ShellObject>();
            FORMATETC format = new FORMATETC();
            STGMEDIUM medium = new STGMEDIUM();

            format.cfFormat = (short)RegisterClipboardFormat("Shell IDList Array");
            format.dwAspect = DVASPECT.DVASPECT_CONTENT;
            format.lindex = 0;
            format.ptd = IntPtr.Zero;
            format.tymed = TYMED.TYMED_HGLOBAL;

            pDataObj.GetData(ref format, out medium);
            GlobalLock(medium.unionmember);
            ShellObject parentFolder = null;
            try
            {
                
                int count = Marshal.ReadInt32(medium.unionmember);
                int offset = 4;

                for (int n = 0; n <= count; ++n)
                {
                    int pidlOffset = Marshal.ReadInt32(medium.unionmember, offset);
                    int pidlAddress = (int)medium.unionmember + pidlOffset;

                    if (n == 0)
                    {
                        parentFolder = ShellObjectFactory.Create(new IntPtr(pidlAddress));
                    }
                    else
                    {
                        result.Add(ShellObjectFactory.Create(CreateItemWithParent(GetIShellFolder(parentFolder.NativeShellItem), new IntPtr(pidlAddress))));
                    }

                    offset += 4;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(medium.unionmember);
            }

            return result.ToArray();
        }

        #endregion
        short mHotKeyId = 0;
        #region SubclassingWindow

        // another version of SetWindowLong that takes 2 pointers
        [DllImport("user32")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr newProc);
        [DllImport("user32")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, Win32WndProc newProc);
        [DllImport("user32")]
        private static extern int CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int Msg, int wParam, int lParam);

        // A delegate that matches Win32 WNDPROC:
        private delegate int Win32WndProc(IntPtr hWnd, int Msg, int wParam, int lParam);

        // from winuser.h:
        private const int GWL_WNDPROC = -4;


        // program variables
        private IntPtr oldWndProc = IntPtr.Zero;
        private Win32WndProc newWndProc = null;
        
        void SubclassHWnd(IntPtr hWnd)
        {
            // hWnd is the window you want to subclass..., create a new 
            // delegate for the new wndproc
            newWndProc = new Win32WndProc(MyWndProc);
            // subclass
            oldWndProc = SetWindowLong(hWnd, GWL_WNDPROC, newWndProc);
            IsGetHWnd = false;
            
        }
        void DeSubClass(IntPtr hWnd)
        {
            SetWindowLong(hWnd, GWL_WNDPROC, oldWndProc);
        }
        

        Rectangle rec = new Rectangle();
        // this is the new wndproc, just show a messagebox on left button down:
        private int MyWndProc(IntPtr hWnd, int Msg, int wParam, int lParam)
        {
            
            
            // Catch Mouse move event
            //if ((Msg == (int)WindowsAPI.WndMsg.WM_MOUSEMOVE))
            //{
            //    WindowsAPI.RECTW rscroll = new WindowsAPI.RECTW();
            //    WindowsAPI.GetWindowRect(VScrollHandle, ref rscroll);
            //    WindowsAPI.RECTW r = new WindowsAPI.RECTW();
            //    WindowsAPI.GetWindowRect(SysListViewHandle, ref r);
            //    Rectangle reclv = r.ToRectangle();
            //    Rectangle rec2 = new Rectangle(reclv.X + 1, rscroll.Top + 30, reclv.Width - 3,
            //        rscroll.Bottom - rscroll.Top - 5 - 30);

            //    //ExplorerBrowser.Checktmr.Start();
            //    //A workarownd to ugly AutoScroll bug in IExplorerBrowsers
            //    if (IsPressedLKButton && !IsMouseClickOnHeader)
            //    {

                    
            //        if (rscroll.Height > 0)
            //            if (!rec2.Contains(Cursor.Position))
            //            {
          
            //                if (Cursor.Position.Y <= rec2.Y)
            //                {
            //                    Cursor.Position = new Point(Cursor.Position.X, rec2.Top);
            //                    return 0;
            //                }
            //                if (Cursor.Position.Y >= rec2.Bottom)
            //                {

            //                    Cursor.Position = new Point(Cursor.Position.X, rec2.Bottom - 1);
            //                    return 0;
            //                }

            //            } 
            //    }
            //    else
            //    {
            //        Guid iid = new Guid(ExplorerBrowserIIDGuid.IShellView);
            //        IntPtr view = IntPtr.Zero;
            //        HResult hr = this.explorerBrowserControl.GetCurrentView(ref iid, out view);
            //        IShellView isv = (IShellView)Marshal.GetObjectForIUnknown(view);
            //        AvailableVisibleColumns = AvailableColumns(isv, false);
            //        Marshal.ReleaseComObject(isv);
            //    }

            //    if (!reclv.Contains(Cursor.Position))
            //    {
            //        ExplorerBrowserMouseLeave.Invoke(this, null);
            //    }
                

                
                
            //}

           

            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }


        #endregion

        #region view event forwarding
        internal void FireSelectionChanged()
        {
            if (SelectionChanged != null)
            {
               
                SelectionChanged(this, EventArgs.Empty);
            }
        }

        internal void ExplorerGotFocusRaized()
        {
            if (ExplorerGotFocus != null)
            {
                ExplorerGotFocus(this, EventArgs.Empty);
            }
        }

        internal void FireContentChanged()
        {
            if (ItemsChanged != null)
            {
                ItemsChanged.Invoke(this, EventArgs.Empty);
            }
        }

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsItemFolder(IFolderView2 view, int index);


        public bool ItemIsFolder(int Index)
        {
            IFolderView2 ifv2 = GetFolderView2();
            return IsItemFolder(ifv2, Index);
        }
        
        public bool ItemIsFolder()
        {
            IFolderView2 fv2 = GetFolderView2();
            int iIndex = -1;
            fv2.GetSelectedItem(0, out iIndex);
            return IsItemFolder(ifv2, iIndex);
        }

        public static IntPtr SysListViewHandle;
        public static bool IsBool;
        public static IntPtr VScrollHandle;
        public static WindowsAPI.IDropTarget SysListviewDT;
        public static IFolderView2 ifv2;
        private bool IsSecondSubclass = false;
        BackgroundWorker bgw = new BackgroundWorker();
        public static IShellView isvv;
        internal void FireContentEnumerationComplete()
        {
            if (ViewEnumerationComplete != null)
            {

                //BeginInvoke(new MethodInvoker(
                //    delegate
                //    {
                        IntPtr n = WindowsAPI.FindWindowEx(this.Handle, IntPtr.Zero, "ExplorerBrowserControl", null);
                        IntPtr z = WindowsAPI.FindWindowEx(n, IntPtr.Zero, "DUIViewWndClassName", null);
                        IntPtr o = WindowsAPI.FindWindowEx(z, IntPtr.Zero, "DirectUIHWND", null);
                        IntPtr s1 = WindowsAPI.FindWindowEx(o, IntPtr.Zero, "CtrlNotifySink", null);
                        IntPtr s2 = WindowsAPI.FindWindowEx(o, s1, "CtrlNotifySink", null);
                        IntPtr s3 = WindowsAPI.FindWindowEx(o, s2, "CtrlNotifySink", null);
                        IntPtr k = WindowsAPI.FindWindowEx(s3, IntPtr.Zero, "SHELLDLL_DefView", null);
                        SysListViewHandle = WindowsAPI.FindWindowEx(k, IntPtr.Zero, "SysListView32", null);
                        if (SysListViewHandle == IntPtr.Zero)
                        {
                            SysListViewHandle = WindowsAPI.FindWindowEx(k, IntPtr.Zero, "DirectUIHWND", null);
                        }
                        IntPtr s4 = WindowsAPI.FindWindowEx(SysListViewHandle, IntPtr.Zero, "CtrlNotifySink", null);
                        IntPtr s5 = WindowsAPI.FindWindowEx(SysListViewHandle, s4, "CtrlNotifySink", null);
                        VScrollHandle = WindowsAPI.FindWindowEx(s5, IntPtr.Zero, "ScrollBar", null);
                        WindowsAPI.RECTW rscroll = new WindowsAPI.RECTW();
                        WindowsAPI.GetWindowRect(VScrollHandle, ref rscroll);
                        Guid iid = new Guid(ExplorerBrowserIIDGuid.IShellView);
                        IntPtr view = IntPtr.Zero;
                        HResult hr = this.explorerBrowserControl.GetCurrentView(ref iid, out view);
                        IShellView isv = (IShellView)Marshal.GetObjectForIUnknown(view);

                        AvailableVisibleColumns = AvailableColumns(isv, false);
                        SysListviewDT = (WindowsAPI.IDropTarget)isv;

                        Marshal.ReleaseComObject(isv);
                        WindowsAPI.RevokeDragDrop(SysListViewHandle);
                        ShellViewDragDrop DropTarget = new ShellViewDragDrop();
                        WindowsAPI.RegisterDragDrop(SysListViewHandle, DropTarget);
                        
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    //}));
                
                ViewEnumerationComplete.Invoke(this, EventArgs.Empty);
            }
        }

        public void FlushMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                WindowsAPI.SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }

        public void ExplorerSetFocus()
        {
            WindowsAPI.SetFocus(SysListViewHandle);
        }

        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }

        internal void FireSelectedItemChanged()
        {
            if (ViewSelectedItemChanged != null)
            {
                ViewSelectedItemChanged.Invoke(this, EventArgs.Empty);
            }
        }

        internal void vViewChanged()
        {
            if (ViewChanged != null)
            {
                ViewChangedEventArgs e = new ViewChangedEventArgs();
                e.ThumbnailSize = ContentOptions.ThumbnailSize;
                e.View = ContentOptions.ViewMode;
                ViewChanged.Invoke(this, e);
            }
        }

        internal void vItemHot(string classname, string itemname, Rectangle rec, int index, bool Isback, Rectangle ImageRec, Rectangle TextRec)
        {
            
            if (ItemHot != null)
            {
                ExplorerAUItemEventArgs e = new ExplorerAUItemEventArgs();
                e.ElementClass = classname;
                e.ElementName = itemname;
                e.ElementRectangle = rec;
                e.Elementindex = index;
                e.ImageRec = ImageRec;
                e.TextRec = TextRec;
                e.IsElementBackground = Isback;
                ItemHot.Invoke(this, e);
            }
        }
        #endregion
        #endregion  
    
        public int DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
        {
            throw new NotImplementedException();
        }

        public void DUnadvise(int connection)
        {
            throw new NotImplementedException();
        }

        public int EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            throw new NotImplementedException();
        }

        public IEnumFORMATETC EnumFormatEtc(DATADIR direction)
        {
            throw new NotImplementedException();
        }

        public int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
        {
            throw new NotImplementedException();
        }

        public void GetData(ref FORMATETC format, out STGMEDIUM medium)
        {
            throw new NotImplementedException();
        }

        public void GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
        {
            throw new NotImplementedException();
        }

        public int QueryGetData(ref FORMATETC format)
        {
            throw new NotImplementedException();
        }

        public void SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Custom DragDrop handler class
    /// </summary>
    public class ShellViewDragDrop : WindowsAPI.IDropTarget
    {
        public ShellViewDragDrop()
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LVHITTESTINFO
        {
            public POINT pt;
            public uint flags;
            public int iItem;
            public int iSubItem;
        }

        bool IsLeftClick = false;
        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref LVHITTESTINFO lParam);


        public void DragEnter(System.Runtime.InteropServices.ComTypes.IDataObject pDataObj, int grfKeyState, Point pt, ref int pdwEffect)
        {


            AsyncDataObject pADataObject = new AsyncDataObject(pDataObj);
            pADataObject.SetAsyncMode(true);
            ExplorerBrowser.SysListviewDT.DragEnter(pADataObject, grfKeyState, pt, ref pdwEffect);
            

        }
        int LastItemIndex = -1;
     
        public void DragOver(int grfKeyState, Point pt, ref int pdwEffect)
        {
            
            ExplorerBrowser.SysListviewDT.DragOver(grfKeyState, pt, ref pdwEffect);

        }

        public void DragLeave()
        {

            ExplorerBrowser.SysListviewDT.DragLeave();

        }
        


        ShellObject[] ParseDrop(System.Runtime.InteropServices.ComTypes.IDataObject pDataObj)
        {
            List<ShellObject> result = new List<ShellObject>();
            System.Runtime.InteropServices.ComTypes.FORMATETC format = new System.Runtime.InteropServices.ComTypes.FORMATETC();
            System.Runtime.InteropServices.ComTypes.STGMEDIUM medium = new System.Runtime.InteropServices.ComTypes.STGMEDIUM();

            format.cfFormat = 15;
            format.dwAspect = System.Runtime.InteropServices.ComTypes.DVASPECT.DVASPECT_CONTENT;
            format.lindex = 0;
            format.ptd = IntPtr.Zero;
            format.tymed = System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL;

            pDataObj.GetData(ref format, out medium);
            ExplorerBrowser.GlobalLock(medium.unionmember);

            try
            {
                ShellObject parentFolder = null;
                int count = Marshal.ReadInt32(medium.unionmember);
                int offset = 4;

                for (int n = 0; n <= count; ++n)
                {
                    int pidlOffset = Marshal.ReadInt32(medium.unionmember, offset);
                    int pidlAddress = (int)medium.unionmember + pidlOffset;

                    if (n == 0)
                    {
                        parentFolder = ShellObjectFactory.Create(new IntPtr(pidlAddress));
                    }
                    else
                    {
                        result.Add(ShellObjectFactory.Create(ExplorerBrowser.CreateItemWithParent(GetIShellFolder(parentFolder.NativeShellItem), new IntPtr(pidlAddress))));
                    }

                    offset += 4;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(medium.unionmember);
            }

            return result.ToArray();
        }

        private bool HitTest(IntPtr ShellViewHandle, Point hitPoint, out int row, out int column)
        {
            // clear the output values
            row = column = -1;

            // set up some win32 api constant values
            const int LVM_FIRST = 0x1000;
            //const int LVM_SUBITEMHITTEST = (LVM_FIRST + 57);
            const int LVM_HITTEST = (LVM_FIRST + 18);

            const int LVHT_NOWHERE = 0x1;
            const int LVHT_ONITEMICON = 0x2;
            const int LVHT_ONITEMLABEL = 0x4;
            const int LVHT_ONITEMSTATEICON = 0x8;
            const int LVHT_EX_ONCONTENTS = 0x04000000;
            const int LVHT_ONITEM = (LVHT_ONITEMICON | LVHT_ONITEMLABEL | LVHT_ONITEMSTATEICON | LVHT_EX_ONCONTENTS);

            // set up the return value
            bool hitLocationFound = false;

            // initialise a hittest information structure
            LVHITTESTINFO lvHitTestInfo = new LVHITTESTINFO();
            lvHitTestInfo.pt.x = hitPoint.X;
            lvHitTestInfo.pt.y = hitPoint.Y;

            // send the hittest message to find out where the click was
            if (SendMessage(ShellViewHandle, LVM_HITTEST, 0, ref lvHitTestInfo) != 0)
            {

                bool nowhere = ((lvHitTestInfo.flags & LVHT_NOWHERE) != 0);
                bool onItem = ((lvHitTestInfo.flags & LVHT_ONITEM) != 0);

                if (onItem && !nowhere)
                {
                    row = lvHitTestInfo.iItem;
                    column = lvHitTestInfo.iSubItem;
                    hitLocationFound = true;
                }


            }
            else if (SendMessage(ShellViewHandle, LVM_FIRST, 0, ref lvHitTestInfo) != 0)
            {
                row = 0;
                hitLocationFound = true;
            }

            return hitLocationFound;
        }
        private const int LVM_FIRST = 0x1000;
        private const int LVM_GETITEMCOUNT = LVM_FIRST + 4;
        private const int LVM_GETITEM = LVM_FIRST + 75;
        private const int LVIF_TEXT = 0x0001;
        private const int LVIS_DROPHILITED = 0x0008;
        private const int LVM_SETITEMSTATE = LVM_FIRST + 43;
        const int LVIF_STATE = 0x0008;


        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam,
                                                 IntPtr lParam);


        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsItemFolder(IFolderView2 view, int index);

        [DllImport("BEH.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetItemName(IFolderView2 view, int index);



        private DragDropEffects Effect;

        public void Drop(System.Runtime.InteropServices.ComTypes.IDataObject pDataObj, int grfKeyState, Point pt, ref int pdwEffect)
        {
            AsyncDataObject pADataObject = new AsyncDataObject(pDataObj);
            pADataObject.SetAsyncMode(true);
            ExplorerBrowser.SysListviewDT.Drop(pADataObject, grfKeyState, pt, ref pdwEffect);
        }

        void mi_Click(object sender, EventArgs e)
        {
            Effect = DragDropEffects.Copy;
        }


        public void DoCopy(object Data)
        {
            DropData DataDrop = (DropData)Data;

            if (DataDrop.DropList == null)
            {
                using (FileOperation fileOp = new FileOperation())
                {
                    foreach (ShellObject item in DataDrop.ItemsForDrop)
                    {

                        string New_Name = "";
                        if (Path.GetExtension(item.ParsingName) == "")
                        {

                            New_Name = item.GetDisplayName(DisplayNameType.Default);
                        }
                        else
                        {
                            New_Name = Path.GetFileName(item.ParsingName);
                        }
                        if (!File.Exists(DataDrop.PathForDrop))
                        {
                            fileOp.CopyItem(item.ParsingName, DataDrop.PathForDrop, New_Name);
                        }

                    }

                    fileOp.PerformOperations();
                }
            }
            else
            {
                using (FileOperation fileOp = new FileOperation())
                {
                    foreach (string item in DataDrop.DropList)
                    {

                        string New_Name = "";
                        if (Path.GetExtension(item) == "")
                        {
                            ShellObject shi = ShellObject.FromParsingName(item);
                            New_Name = shi.GetDisplayName(DisplayNameType.Default);
                        }
                        else
                        {
                            New_Name = Path.GetFileName(item);
                        }
                        if (!File.Exists(DataDrop.PathForDrop))
                        {
                            fileOp.CopyItem(item, DataDrop.PathForDrop, New_Name);
                        }

                    }

                    fileOp.PerformOperations();
                }
            }
        }

        public void DoMove(object Data)
        {
            DropData DataDrop = (DropData)Data;

            using (FileOperation fileOp = new FileOperation())
            {
                foreach (ShellObject item in DataDrop.ItemsForDrop)
                {

                    string New_Name = "";
                    if (Path.GetExtension(item.ParsingName) == "")
                    {

                        New_Name = item.GetDisplayName(DisplayNameType.Default);
                    }
                    else
                    {
                        New_Name = Path.GetFileName(item.ParsingName);
                    }
                    if (!File.Exists(DataDrop.PathForDrop))
                    {
                        fileOp.MoveItem(item.ParsingName, DataDrop.PathForDrop, New_Name);
                    }

                }

                fileOp.PerformOperations();
            }
        }

        public IShellFolder GetIShellFolder(IShellItem sitem)
        {
            IShellFolder result;
            ((IShellItem2)sitem).BindToHandler(IntPtr.Zero,
                                               BHID.SFObject, typeof(IShellFolder).GUID, out result);
            return result;
        }

        private ShellObject[] ParseShellIDListArray(System.Runtime.InteropServices.ComTypes.IDataObject pDataObj)
        {
            List<ShellObject> result = new List<ShellObject>();
            System.Runtime.InteropServices.ComTypes.FORMATETC format = new System.Runtime.InteropServices.ComTypes.FORMATETC();
            System.Runtime.InteropServices.ComTypes.STGMEDIUM medium = new System.Runtime.InteropServices.ComTypes.STGMEDIUM();

            format.cfFormat = (short)ExplorerBrowser.RegisterClipboardFormat("Shell IDList Array");
            format.dwAspect = System.Runtime.InteropServices.ComTypes.DVASPECT.DVASPECT_CONTENT;
            format.lindex = 0;
            format.ptd = IntPtr.Zero;
            format.tymed = System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL;

            pDataObj.GetData(ref format, out medium);
            ExplorerBrowser.GlobalLock(medium.unionmember);

            try
            {
                ShellObject parentFolder = null;
                int count = Marshal.ReadInt32(medium.unionmember);
                int offset = 4;

                for (int n = 0; n <= count; ++n)
                {
                    int pidlOffset = Marshal.ReadInt32(medium.unionmember, offset);
                    int pidlAddress = (int)medium.unionmember + pidlOffset;

                    if (n == 0)
                    {
                        parentFolder = ShellObjectFactory.Create(new IntPtr(pidlAddress));
                    }
                    else
                    {
                        result.Add(ShellObjectFactory.Create(ExplorerBrowser.CreateItemWithParent(GetIShellFolder(parentFolder.NativeShellItem), new IntPtr(pidlAddress))));
                    }

                    offset += 4;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(medium.unionmember);
            }

            return result.ToArray();
        }
    }

    /// <summary>
    /// Class containing definition for columns
    /// </summary>
    [Serializable]
    public class Collumns
    {
        public Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser.PROPERTYKEY pkey;
        public string Name;
        public int Width;
    }

    /// <summary>
    /// BHID class
    /// </summary>
    public class BHID
    {
        public static Guid SFObject
        {
            get { return m_SFObject; }
        }

        public static Guid SFUIObject
        {
            get { return m_SFUIObject; }
        }

        static Guid m_SFObject = new Guid("3981e224-f559-11d3-8e3a-00c04f6837d5");
        static Guid m_SFUIObject = new Guid("3981e225-f559-11d3-8e3a-00c04f6837d5");
    }

    /// <summary>
    /// DropData class
    /// </summary>
    public class DropData
    {
        public ShellObject[] ItemsForDrop;
        public System.Collections.Specialized.StringCollection DropList;
        public string PathForDrop;
        public ShellObjectCollection Shellobjects;
    }
}
