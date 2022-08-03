using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DuplicationPlugin.Windows
{
    /// <summary>
    /// Interaction logic for DuplicateAssetWindow.xaml
    /// </summary>
    public partial class DuplicateAssetWindow : FrostyDockableWindow
    {
        public string SelectedPath { get; private set; } = "";
        public string SelectedName { get; private set; } = "";
        public Type SelectedType { get; private set; } = null;
        public Guid SelectedInstanceGUID { get; private set; } = new Guid();
        public Guid SelectedFileGUID { get; private set; } = Guid.NewGuid();
        private EbxAssetEntry entry;

        public DuplicateAssetWindow(EbxAssetEntry currentEntry)
        {
            InitializeComponent();

            pathSelector.ItemsSource = App.AssetManager.EnumerateEbx();
            entry = currentEntry;
            EbxAsset asset = App.AssetManager.GetEbx(entry);
            SelectedInstanceGUID = Utils.GenerateDeterministicGuid(asset.Objects, asset.GetType(), SelectedFileGUID);

            assetNameTextBox.Text = currentEntry.Filename;
            assetTypeTextBox.Text = entry.Type;
            assetInstanceGuidTextBox.Text = SelectedInstanceGUID.ToString();
            assetFileGuidTextBox.Text = SelectedFileGUID.ToString();
        }

        private void AssetNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void AssetGuidTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox source = e.OriginalSource as TextBox;
            try
            {
                new Guid(source.Text);
            }
            catch
            {
                if (source == assetInstanceGuidTextBox)
                    source.Text = SelectedInstanceGUID.ToString();
                else if (source == assetFileGuidTextBox)
                    source.Text = SelectedFileGUID.ToString();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string tmp = assetNameTextBox.Text.Replace('\\', '/').Trim('/');
            string fullName = pathSelector.SelectedPath + "/" + tmp;
            Guid instanceGuid = new Guid(assetInstanceGuidTextBox.Text);
            Guid fileGuid = new Guid(assetFileGuidTextBox.Text);

            if (!string.IsNullOrEmpty(assetNameTextBox.Text) && !entry.Name.Equals(fullName, StringComparison.OrdinalIgnoreCase))
            {
                if (!tmp.Contains("//"))
                {
                    SelectedName = tmp;
                    SelectedPath = pathSelector.SelectedPath;
                    SelectedInstanceGUID = instanceGuid;
                    SelectedFileGUID = fileGuid;

                    DialogResult = true;
                    Close();
                }
                else
                {
                    FrostyMessageBox.Show("Name of asset is invalid", "Frosty Editor");
                }
            }
            else
            {
                FrostyMessageBox.Show("Name of asset must be unique", "Frosty Editor");
            }
        }

        private void FrostyDockableWindow_FrostyLoaded(object sender, EventArgs e)
        {
            pathSelector.SelectAsset(entry);
        }

        private void ClassSelector_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
        }

        private void typeButton_Click(object sender, RoutedEventArgs e)
        {
            ClassSelector win = new ClassSelector(TypeLibrary.GetTypes("Asset"), allowAssets: true);
            if (win.ShowDialog() == true)
            {
                Type selectedType = win.SelectedClass;
                if (selectedType != null)
                {
                    SelectedType = selectedType;
                    assetTypeTextBox.Text = selectedType.Name;
                }
            }
        }

        
    }
}
