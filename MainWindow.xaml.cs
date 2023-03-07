using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace NVShadowPlayHelper;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string ShadowPlayKey = @"SOFTWARE\NVIDIA Corporation\Global\ShadowPlay\NVSPCAPS";
    private const string DefaultPathW = "DefaultPathW";

    public MainWindow()
    {
        InitializeComponent();
        videoLocation.Text = VideoSavePath;
    }

    private void VideoChoose_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true
        };
        if (dialog.ShowDialog(this) != CommonFileDialogResult.Ok || dialog.FileName == null) return;

        string path = dialog.FileName;
        videoLocation.Text = path;

        var bytes = new List<byte>();
        foreach (byte b in Encoding.Default.GetBytes(path))
        {
            bytes.Add(b);
            bytes.Add(0x00);
        }
        bytes.Add(0x00);
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ShadowPlayKey, true);
        key?.SetValue(DefaultPathW, bytes.ToArray(), RegistryValueKind.Binary);

        RestartShadowPlay();
    }

    private static void RestartShadowPlay()
    {
        const string strCmdText = "/C sc stop NvContainerLocalSystem&timeout /T 5&sc start NvContainerLocalSystem";
        Process.Start(new ProcessStartInfo("CMD.exe", strCmdText)
        {
            UseShellExecute = true, // Needed for UAC to appear
            Verb = "runas",
            // CreateNoWindow = true, // Creates window anyway, due to UseShellExecute
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private void DeleteEmpty_OnClick(object sender, RoutedEventArgs e)
    {
        string? path = VideoSavePath;
        if (path == null) return;

        Directory.GetDirectories(path)
            .Where(dir => Directory.GetFiles(dir).Length == 0).ToList()
            .ForEach(Directory.Delete);
        MessageBox.Show(this, $"Deleted empty directories from {path}...", "Success", MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private string? VideoSavePath
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ShadowPlayKey);
            var type = key?.GetValueKind(DefaultPathW);
            if (type != RegistryValueKind.Binary)
            {
                MessageBox.Show(this, $"Nvidia ShadowPlay registry key invalid: {type}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
            return Encoding.Default.GetString((byte[]) key!.GetValue(DefaultPathW)!).Replace("\0", "");
        }
    }
}
