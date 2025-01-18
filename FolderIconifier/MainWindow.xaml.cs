using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Vanara.PInvoke;
using Path = System.IO.Path;

namespace FolderIconifier;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        IconList.Items.Filter = ItemsFilter;
        
        var dataContext = new MainWindowViewModel();
        DataContext = dataContext;
        
        foreach (var file in Directory.GetFiles(
                     Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Icons"),
                     "*.ico",
                     SearchOption.AllDirectories))
        {
            dataContext.FolderIcons.Add(new FolderIcon(Path.GetFileNameWithoutExtension(file), file));
        }
    }

    private bool ItemsFilter(object item)
    {
        return (item as FolderIcon)?.Name.Contains(FilterText.Text, StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (((ListViewItem) sender).Content is FolderIcon folderIcon)
        {
            foreach (var folder in Environment.GetCommandLineArgs())
            {
                if (Directory.Exists(folder))
                {
                    Colorize(folder, folderIcon);
                }
            }

            Console.WriteLine(folderIcon.FilePath);
        }
    }
    
    private void Colorize(string folder, FolderIcon folderIcon)
    {
        // https://stackoverflow.com/a/78220597
        var folderSettings = new Shell32.SHFOLDERCUSTOMSETTINGS
        {
            dwMask = Shell32.FOLDERCUSTOMSETTINGSMASK.FCSM_ICONFILE,
            pszIconFile = folderIcon.FilePath,
            dwSize = (uint)Marshal.SizeOf(typeof(Shell32.SHFOLDERCUSTOMSETTINGS)),
            cchIconFile = 0
        };
        //FolderSettings.iIconIndex = 0;
        var result = Shell32.SHGetSetFolderCustomSettings(ref folderSettings, folder, Shell32.FCS.FCS_FORCEWRITE);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Shell32.SHGetSetFolderCustomSettings failed.");
        }
        
        Shell32.SHChangeNotify(Shell32.SHCNE.SHCNE_UPDATEDIR, Shell32.SHCNF.SHCNF_PATHW, folder);

        
//         var fileInfo = new FileInfo(Path.Combine(folder, "desktop.ini"));
//         if (fileInfo.Exists)
//         {
//             fileInfo.Attributes &= ~(FileAttributes.Hidden | FileAttributes.System); // HSA
//         }
//         using (var stream = fileInfo.CreateText())
//         {
//             stream.Write(
//                 $"""
//                 [.ShellClassInfo]
//                 IconResource={folderIcon.FilePath},0
//                 """);
//         }
//         fileInfo.Attributes |= FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive; // HSA
//         new DirectoryInfo(folder).Attributes |= FileAttributes.ReadOnly;
//         Thread.Sleep(1000);
//         SHChangeNotify(0x08000000, 0x0000, (IntPtr)null, (IntPtr)null);
    }

    private void FilterText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        IconList.Items.Filter = ItemsFilter;
    }

    private void Install_OnClick(object sender, RoutedEventArgs e)
    {
        if (Environment.ProcessPath == null) throw new InvalidOperationException("No process path provided.");
        
        using var superKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\Directory\shell\FolderIconifier", true);
        if (superKey == null) throw new InvalidOperationException();
        superKey.SetValue("", "Colorize Folder", RegistryValueKind.String);
        superKey.SetValue("Icon", $"\"{Environment.ProcessPath}\",0", RegistryValueKind.String);
        using var commandKey = superKey.CreateSubKey("command");
        if (commandKey == null) throw new InvalidOperationException();
        commandKey.SetValue("", $"{Environment.ProcessPath} \"%1\"", RegistryValueKind.String);
    }

    private void Uninstall_OnClick(object sender, RoutedEventArgs e)
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\Classes\Directory\shell\FolderIconifier", false);
    }
}

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] public partial FolderIcon? SelectedFolderIcon { get; set; }

    public ObservableCollection<FolderIcon> FolderIcons { get; } = new();

}

public partial class FolderIcon : ObservableObject
{
    public string FilePath { get; }

    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial ImageSource ImageSource { get; set; }

    public FolderIcon(string name, string filePath)
    {
        Name = name;

        FilePath = filePath;
        
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();

        using (var stream = File.OpenRead(filePath))
        {
            bitmapImage.StreamSource = stream;
            bitmapImage.DecodePixelHeight = 32;
            bitmapImage.DecodePixelWidth = 32;

            bitmapImage.EndInit();
        }

        ImageSource = bitmapImage;
    }
}