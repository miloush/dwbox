#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using MS.Win32;

namespace System.Windows
{
    public sealed partial class TaskDialog
    {
        private const TaskDialogOptions DefaultOptions = TaskDialogOptions.CenterOwner | TaskDialogOptions.UseCommandLinks;
        private const TaskDialogButtons DefaultButtons = TaskDialogButtons.OK;
        private const TaskDialogImage DefaultImage = TaskDialogImage.None;

        private TaskDialog() { }
        public static TaskDialogButtonResult               Show(string content, string caption = null, string mainInstruction = null, TaskDialogButtons buttons = DefaultButtons, TaskDialogImage icon = DefaultImage)
        {
            return ShowCore(IntPtr.Zero, content, caption, mainInstruction, buttons, icon);
        }
        public static TaskDialogButtonResult Show(Window owner, string content, string caption = null, string mainInstruction = null, TaskDialogButtons buttons = DefaultButtons, TaskDialogImage icon = DefaultImage)
        {
            return ShowCore(new WindowInteropHelper(owner).Handle, content, caption, mainInstruction, buttons, icon);
        }

        public static TaskDialogResult               Show(string content, string caption = null, string mainInstruction = null, string verificationText = null, TaskDialogButtons buttons = DefaultButtons, TaskDialogImage icon = DefaultImage, string[] customButtons = null, TaskDialogOptions options = DefaultOptions)
        {
            return ShowIndirectCore(IntPtr.Zero, content, caption, mainInstruction, verificationText, buttons, icon, customButtons, options);
        }
        public static TaskDialogResult Show(Window owner, string content, string caption = null, string mainInstruction = null, string verificationText = null, TaskDialogButtons buttons = DefaultButtons, TaskDialogImage icon = DefaultImage, string[] customButtons = null, TaskDialogOptions options = DefaultOptions)
        {
            return ShowIndirectCore(new WindowInteropHelper(owner).Handle, content, caption, mainInstruction, verificationText, buttons, icon, customButtons, options);
        }

        internal static TaskDialogButtonResult ShowCore(
            IntPtr owner = default,
            string content = null,
            string caption = null,
            string mainInstruction = null,
            TaskDialogButtons buttons = TaskDialogButtons.OK,
            TaskDialogImage icon = TaskDialogImage.None)
        {
            int result = NativeMethods.TaskDialog(owner, IntPtr.Zero, caption, mainInstruction, content, buttons, (IntPtr)icon, out int button);
            if (result != 0)
            {
                throw new Win32Exception(result);
            }

            return (TaskDialogButtonResult)button;
        }

        internal static TaskDialogResult ShowIndirectCore(
            IntPtr owner = default,
            string content = null,
            string caption = null,
            string mainInstruction = null,
            string verificationText = null,
            TaskDialogButtons buttons = TaskDialogButtons.OK,
            TaskDialogImage icon = TaskDialogImage.None,
            string[] customButtons = null,
            TaskDialogOptions options = TaskDialogOptions.UseCommandLinks | TaskDialogOptions.CenterOwner)
        {
            const int commandButtonBase = 1000;

            int buttonSize = Marshal.SizeOf<NativeMethods.TASKDIALOG_BUTTON>();
            const int buttonTextOffset = sizeof(int);

            NativeMethods.TASKDIALOGCONFIG config = new NativeMethods.TASKDIALOGCONFIG();
            config.cbSize = Marshal.SizeOf<NativeMethods.TASKDIALOGCONFIG>();
            config.hwndParent = owner;
            config.pszWindowTitle = caption;
            config.pszContent = content;
            config.pszMainInstruction = mainInstruction;
            config.pszVerificationText = verificationText;
            config.dwCommonButtons = buttons;
            config.hMainIcon = (IntPtr)icon;
            config.dwFlags = options;

            try
            {
                if (customButtons != null && customButtons.Length > 0)
                {
                    config.cButtons = customButtons.Length;
                    config.pButtons = Marshal.AllocHGlobal(buttonSize * customButtons.Length);

                    for (int i = 0; i < customButtons.Length; i++)
                    {
                        Marshal.WriteInt32(config.pButtons + buttonSize * i, commandButtonBase + i);
                        if (customButtons[i] != null)
                        {
                            IntPtr pszButtonText = Marshal.StringToHGlobalUni(customButtons[i]);
                            Marshal.WriteIntPtr(config.pButtons + buttonSize * i + buttonTextOffset, pszButtonText);
                        }
                    }
                }
                else
                {
                    config.dwFlags &= ~(TaskDialogOptions.UseCommandLinks | TaskDialogOptions.UseCommandLinksNoIcon); // otherwise we get parameter incorrect
                }

                int result = NativeMethods.TaskDialogIndirect(config, out int button, out int radioButton, out bool isChecked);
                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                if (button >= commandButtonBase)
                {
                    return new TaskDialogResult((button - commandButtonBase), isChecked);
                }
                else
                {
                    return new TaskDialogResult((TaskDialogButtonResult)button, isChecked);
                }
            }
            finally
            {
                if (config.pButtons != IntPtr.Zero)
                {
                    for (int i = 0; i < customButtons.Length; i++)
                    {
                        IntPtr pszButtonText = Marshal.ReadIntPtr(config.pButtons + buttonSize * i + buttonTextOffset);
                        if (pszButtonText != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(pszButtonText);
                        }
                    }

                    Marshal.FreeHGlobal(config.pButtons);
                }
            }
        }
    }

    public struct TaskDialogResult
    {
        public TaskDialogButtonResult? Button { get; }
        public int? CustomButtonIndex { get; }
        public bool? IsVerificationChecked { get; }

        internal TaskDialogResult(TaskDialogButtonResult button, bool? isChecked = null)
        {
            Button = button;
            CustomButtonIndex = null;
            IsVerificationChecked = isChecked;
        }

        internal TaskDialogResult(int buttonIndex, bool? isChecked = null)
        {
            Button = null;
            CustomButtonIndex = buttonIndex;
            IsVerificationChecked = isChecked;
        }
    }

    public enum TaskDialogButtonResult
    {
        Cancel = 2,
        No = 7,
        OK = 1,
        Retry = 4,
        Yes = 6,
    }

    [Flags]
    public enum TaskDialogButtons
    {
        OK = 0x0001,
        Yes = 0x0002,
        No = 0x0004,
        Cancel = 0x0008,
        Retry = 0x0010,
        Close = 0x0020,

        OKCancel = OK | Cancel,
        YesNoCancel = Yes | No | Cancel,
        YesNo = Yes | No

        // NOTE: if you add or remove any values in this enum, be sure to update MessageBox.IsValidTaskDialogButtons()
    }

    public enum TaskDialogImage
    {
        /// <summary>
        ///   The task dialog does not display an icon.
        /// </summary>
        None = 0,

        /// <summary>
        ///   The task dialog contains a symbol consisting of a lowercase letter "i" in a circle.
        /// </summary>
        Information = ushort.MaxValue - 2, // TD_INFORMATION_ICON

        /// <summary>
        ///   The task dialog contains an icon consisting of an exclamation point in a triangle with a yellow background.
        /// </summary>
        Warning = ushort.MaxValue, // TD_WARNING_ICON

        /// <summary>
        ///   The task dialog contains an icon consisting of white "x" in a circle with a red background.
        /// </summary>
        Error = ushort.MaxValue - 1, // TD_ERROR_ICON

        /// <summary>
        ///   The task dialog contains an icon consisting of an user account control (UAC) shield.
        /// </summary>
        Shield = ushort.MaxValue - 3, // TD_SHIELD_ICON

        /// <summary>
        ///   The task dialog contains an icon consisting of an user account control (UAC) shield and shows a blue bar around the icon.
        /// </summary>
        ShieldBlueBar = ushort.MaxValue - 4,

        /// <summary>
        ///   The task dialog contains an icon consisting of an user account control (UAC) shield and shows a gray bar around the icon.
        /// </summary>
        ShieldGrayBar = ushort.MaxValue - 8,

        /// <summary>
        ///   The task dialog contains an icon consisting of an exclamation point in a yellow shield and shows a yellow bar around the icon.
        /// </summary>
        ShieldWarningYellowBar = ushort.MaxValue - 5,

        /// <summary>
        ///   The task dialog contains an icon consisting of white "x" in a red shield and shows a red bar around the icon.
        /// </summary>
        ShieldErrorRedBar = ushort.MaxValue - 6,

        /// <summary>
        ///   The task dialog contains an icon consisting of white tick in a green shield and shows a green bar around the icon.
        /// </summary>
        ShieldSuccessGreenBar = ushort.MaxValue - 7,

        // NOTE: if you add or remove any values in this enum, be sure to update TaskDialog.IsValidTaskDialogIcon()    
    }

    /// <summary>
    /// Specifies the behavior of the task dialog. 
    /// </summary>
    [Flags]
    public enum TaskDialogOptions
    {
        None = 0,

        /// <summary>
        /// Indicates that the buttons specified in the pButtons member are to be displayed as command links (using a standard task dialog glyph) instead of push buttons. 
        /// When using command links, all characters up to the first new line character in the pszButtonText member will be treated as the command link's main text, and the remainder will be treated as the command link's note. 
        /// This flag is ignored if the cButtons member is zero.
        /// </summary>
        UseCommandLinks = NativeMethods.TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS,

        /// <summary>
        /// Indicates that the buttons specified in the pButtons member are to be displayed as command links (without a glyph) instead of push buttons. 
        /// When using command links, all characters up to the first new line character in the pszButtonText member will be treated as the command link's main text, and the remainder will be treated as the command link's note. 
        /// This flag is ignored if the cButtons member is zero.
        /// </summary>
        UseCommandLinksNoIcon = NativeMethods.TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS_NO_ICON,

        /// <summary>
        /// Indicates that the verification checkbox in the dialog is checked when the dialog is initially displayed. 
        /// This flag is ignored if the pszVerificationText parameter is NULL.
        /// </summary>
        VerificationIsCheckedByDefault = NativeMethods.TASKDIALOG_FLAGS.TDF_VERIFICATION_FLAG_CHECKED,

        /// <summary>
        /// Indicates that the task dialog is positioned (centered) relative to the window specified by hwndParent. 
        /// If the flag is not supplied (or no hwndParent member is specified), the task dialog is positioned (centered) relative to the monitor.
        /// </summary>
        CenterOwner = NativeMethods.TASKDIALOG_FLAGS.TDF_POSITION_RELATIVE_TO_WINDOW,

        /// <summary>
        /// Indicates that the task dialog can be minimized.
        /// </summary>
        CanBeMinimzed = NativeMethods.TASKDIALOG_FLAGS.TDF_CAN_BE_MINIMIZED,

        /// <summary>
        /// Indicates that the width of the task dialog is determined by the width of its content area.
        /// </summary>
        SizeToContent = NativeMethods.TASKDIALOG_FLAGS.TDF_SIZE_TO_CONTENT,

        /// <summary>
        /// Indicates that the dialog should be able to be closed using Alt-F4, Escape, and the title bar's close button even if no cancel button is specified in either the dwCommonButtons or pButtons members.
        /// </summary>
        AllowCancellation = NativeMethods.TASKDIALOG_FLAGS.TDF_ALLOW_DIALOG_CANCELLATION,

        /// <summary>
        /// Indicates that text is displayed reading right to left.
        /// </summary>
        RightToLeft = NativeMethods.TASKDIALOG_FLAGS.TDF_RTL_LAYOUT
    }
}



namespace MS.Win32
{
    internal static class ExternDll
    {
        public const string Comctl32 = "comctl32.dll";
    }

    internal partial class NativeMethods
    {
        [DllImport(ExternDll.Comctl32)]
        internal static extern int TaskDialog(IntPtr hwndOwner, IntPtr hInstance,
          [MarshalAs(UnmanagedType.LPWStr)] string pszWindowTitle,
          [MarshalAs(UnmanagedType.LPWStr)] string pszMainInstruction,
          [MarshalAs(UnmanagedType.LPWStr)] string pszContent,
          TaskDialogButtons dwCommonButtons,
          IntPtr pszIcon,
          out int pnButton);

        [DllImport(ExternDll.Comctl32)]
        internal static extern int TaskDialogIndirect(
            in TASKDIALOGCONFIG pTaskConfig,
            out int pnButton,
            out int pnRadioButton,
            out bool pfVerificationFlagChecked);

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        internal struct TASKDIALOGCONFIG
        {
            public int cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public TaskDialogOptions dwFlags;
            public TaskDialogButtons dwCommonButtons;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszWindowTitle;
            public IntPtr hMainIcon;
            [MarshalAs(UnmanagedType.LPWStr)] 
            public string pszMainInstruction;
            [MarshalAs(UnmanagedType.LPWStr)] 
            public string pszContent;
            public int cButtons;
            public IntPtr pButtons;
            public int nDefaultButton;
            public int cRadioButtons;
            public IntPtr pRadioButtons;
            public int nDefaultRadioButton;
            [MarshalAs(UnmanagedType.LPWStr)] 
            public string pszVerificationText;
            [MarshalAs(UnmanagedType.LPWStr)] 
            public string pszExpandedInformation;
            [MarshalAs(UnmanagedType.LPWStr)] 
            public string pszExpandedControlText;
            [MarshalAs(UnmanagedType.LPWStr)] 
            public string pszCollapsedControlText;
            public IntPtr hFooterIcon;
            public IntPtr pszFooter;
            public IntPtr pfCallback;
            public IntPtr lpCallbackData;
            public int cxWidth;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TASKDIALOG_BUTTON
        {
            public int nButtonID;
            [MarshalAs(UnmanagedType.LPWStr)] 
            public string pszButtonText;
        }

        internal enum TASKDIALOG_FLAGS
        {
            TDF_ENABLE_HYPERLINKS = 0x0001,
            TDF_USE_HICON_MAIN = 0x0002,
            TDF_USE_HICON_FOOTER = 0x0004,
            TDF_ALLOW_DIALOG_CANCELLATION = 0x0008,
            TDF_USE_COMMAND_LINKS = 0x0010,
            TDF_USE_COMMAND_LINKS_NO_ICON = 0x0020,
            TDF_EXPAND_FOOTER_AREA = 0x0040,
            TDF_EXPANDED_BY_DEFAULT = 0x0080,
            TDF_VERIFICATION_FLAG_CHECKED = 0x0100,
            TDF_SHOW_PROGRESS_BAR = 0x0200,
            TDF_SHOW_MARQUEE_PROGRESS_BAR = 0x0400,
            TDF_CALLBACK_TIMER = 0x0800,
            TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000,
            TDF_RTL_LAYOUT = 0x2000,
            TDF_NO_DEFAULT_RADIO_BUTTON = 0x4000,
            TDF_CAN_BE_MINIMIZED = 0x8000,
            TDF_NO_SET_FOREGROUND = 0x00010000,
            TDF_SIZE_TO_CONTENT = 0x01000000
        }

        internal const int TDF_USE_HICON_MAIN = 0x2;
    }
}