using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using PowerApps.Samples.LoginUX;
using Stepman.Models;
using Stepman.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Web.UI.WebControls;
using System;

namespace Stepman
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DynamicsComponentsService _dynamicsComponentsService;

        private readonly ExportService _exportService;
        private readonly SolutionPackService _solutionService;

        public MainWindow(IServiceProvider serviceProvider)
        {
            _exportService = serviceProvider.GetRequiredService<ExportService>();
            _solutionService = serviceProvider.GetRequiredService<SolutionPackService>();

            InitializeComponent();

            this.StepAttributesCollection = new ObservableCollection<StepAttribute>();
            StepAttributes.ItemsSource = StepAttributesCollection;
            this.ImageAttributesCollection = new ObservableCollection<StepAttribute>();
            ImageAttributes.ItemsSource = ImageAttributesCollection;
        }

        private void btnSignIn_Click(object sender, RoutedEventArgs e)
        {
            // Instantiate the login control.  
            ExampleLoginForm ctrl = new ExampleLoginForm();

            // Wire the button click event to the login response.   
            ctrl.ConnectionToCrmCompleted += ctrl_ConnectionToCrmCompleted;

            // Show the login control.   
            ctrl.ShowDialog();

            // Check that a web service connection is returned and the service is ready.     
            if (ctrl.CrmConnectionMgr != null && ctrl.CrmConnectionMgr.CrmSvc != null && ctrl.CrmConnectionMgr.CrmSvc.IsReady)
            {
                // Display the Dataverse version and connected environment name  
                System.Windows.MessageBox.Show("Connected to Dataverse version: " + ctrl.CrmConnectionMgr.CrmSvc.ConnectedOrgVersion.ToString() +
                    " Organization: " + ctrl.CrmConnectionMgr.CrmSvc.ConnectedOrgUniqueName, "Connection Status");
                // TODO Additional web service operations can be performed here
                _dynamicsComponentsService = new DynamicsComponentsService(ctrl.CrmConnectionMgr.CrmSvc);
                LoadSolutions();
            }
            else
            {
                System.Windows.MessageBox.Show("Cannot connect; try again!", "Connection Status");
            }
        }

        private void ctrl_ConnectionToCrmCompleted(object sender, EventArgs e)
        {
            if (sender is ExampleLoginForm)
            {
                this.Dispatcher.Invoke(() =>
                {
                    ((ExampleLoginForm)sender).Close();
                });
            }
        }

        private void LoadSolutions()
        {
            var solutions = _dynamicsComponentsService.GetStepSolutions();
            var cbItems = new ObservableCollection<ComboBoxItem>();
            SolutionsComboBox.ItemsSource = cbItems;
            foreach (var item in solutions)
            {
                cbItems.Add(new ComboBoxItem { Content = item.Value, Tag = item.Key });
            }
        }

        private void LoadSteps()
        {
            var selected = SolutionsComboBox.SelectedItem;
            var solutionId = (Guid)((FrameworkElement)selected).Tag;

            var steps = _dynamicsComponentsService.GetSolutionSteps(solutionId);
            var cbItems = new ObservableCollection<ComboBoxItem>();
            StepsComboBox.ItemsSource = cbItems;
            foreach (var item in steps)
            {
                cbItems.Add(new ComboBoxItem { Content = item.Value, Tag = item.Key });
            }
        }

        private void SolutionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StepAttributesCollection.Clear();
            ImageAttributesCollection.Clear();
            LoadSteps();
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            StepAttributesCollection.Clear();
            LoadAttributes();
        }

        private void LoadAttributes()
        {
            var selected = StepsComboBox.SelectedItem;
            var stepId = (Guid)((FrameworkElement)selected).Tag;
            var attributes = _dynamicsComponentsService.GetStepAttributes(stepId);
            foreach (var item in attributes)
            {
                StepAttributesCollection.Add(item);
            }

            var images = _dynamicsComponentsService.GetStepsImages(stepId);
            var cbItems = new ObservableCollection<ComboBoxItem>();
            ImageComboBox.ItemsSource = cbItems;
            foreach (var item in images)
            {
                cbItems.Add(new ComboBoxItem { Content = item.Value, Tag = item.Key });
            }
        }

        private void Button_SolutionFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder";
                dialog.ShowNewFolderButton = true; // Optional: Show button to create a new folder
                if (!string.IsNullOrEmpty(FolderPath))
                {
                    dialog.SelectedPath = TextBox_SelectedSolutionFolder.Text;
                }

                // Show the dialog and get the result
                var result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    // Display the selected folder path
                    TextBox_SelectedSolutionFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void Button_Export_Click(object sender, RoutedEventArgs e)
        {
            var taskInfo = TextBlock_TaskInfo.Text;
            var folder = TextBox_SelectedSolutionFolder.Text;
            _exportService.Export(StepData, folder, taskInfo);
            StepData = default;
            System.Windows.MessageBox.Show("Successfully exported");
        }

        private void Button_PackSolution_Click(object sender, RoutedEventArgs e)
        {
            var selected = SolutionsComboBox.SelectedItem;
            var solutionId = (Guid)((FrameworkElement)selected).Tag;
            var solutionName = _dynamicsComponentsService.GetSolutionLogicalName(solutionId);
            var path = TextBox_SelectedSolutionFolder.Text;
            var zipFile = solutionName + ".zip";
            var map = solutionName + "Map.xml";
            _solutionService.Pack(path, zipFile, map);
        }

        private StepData StepData { get; set; }

        private void StepAttributes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void StepAttribute_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            var row = DataGridRow.GetRowContainingElement(checkBox);
            var selectedStep = StepsComboBox.SelectedItem;
            StepAttribute attribute;

            StepData ??= new StepData
            {
                StepId = (Guid)((FrameworkElement)selectedStep).Tag
            };

            if (row != null)
            {
                // Get the selected item from the row
                attribute = (StepAttribute)row.Item;
                if (!StepData.Attributes.Contains(attribute))
                {
                    StepData.Attributes.Add(attribute);
                }
                HandleChanges();
            }
        }

        private void StepAttribute_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            var row = DataGridRow.GetRowContainingElement(checkBox);
            StepAttribute attribute;

            if (row != null)
            {
                // Get the selected item from the row
                attribute = (StepAttribute)row.Item;
                if (StepData.Attributes.Contains(attribute))
                {
                    StepData.Attributes.Remove(attribute);
                }
                HandleChanges();
            }
        }

        private void ImageAttribute_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            var row = DataGridRow.GetRowContainingElement(checkBox);

            var selectedStep = GetSelectedStepId();
            var imageId = GetSelectedImageId();
            StepAttribute attribute;

            StepData ??= new StepData
            {
                StepId = (Guid)((FrameworkElement)selectedStep).Tag
            };

            if (row != null)
            {
                // Get the selected item from the row
                attribute = (StepAttribute)row.Item;
                var image = StepData.Images.FirstOrDefault(i => i.ImageId == imageId);
                if (image is null)
                {
                    StepData.Images.Add(new ImageData
                    {
                        ImageId = imageId,
                        Attributes = new List<StepAttribute>()
                    });
                }

                StepData.Images.First(i => i.ImageId == imageId).Attributes.Add(attribute);
                HandleChanges();
            }
        }

        private void ImageAttribute_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            var row = DataGridRow.GetRowContainingElement(checkBox);

            var selectedStep = GetSelectedStepId();
            var imageId = GetSelectedImageId();
            StepAttribute attribute;

            if (row != null)
            {
                // Get the selected item from the row
                attribute = (StepAttribute)row.Item;
                var image = StepData.Images.FirstOrDefault(i => i.ImageId == imageId);
                if (image.Attributes.Contains(attribute))
                {
                    image.Attributes.Remove(attribute);
                }
                HandleChanges();
            }
        }

        private Guid GetSelectedImageId()
        {
            var selectedImage = ImageComboBox.SelectedItem;
            var imageId = (Guid)((FrameworkElement)selectedImage).Tag;
            return imageId;
        }

        private object GetSelectedStepId()
        {
            var selectedStep = StepsComboBox.SelectedItem;
            var stepId = (Guid)((FrameworkElement)selectedStep).Tag;
            return selectedStep;
        }

        private void ImageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageAttributesCollection.Clear();
            LoadImageAttributes();
        }

        private void LoadImageAttributes()
        {
            var selectedImage = ImageComboBox.SelectedItem;
            var imageId = (Guid)((FrameworkElement)selectedImage).Tag;
            var selectedStep = StepsComboBox.SelectedItem;
            var stepId = (Guid)((FrameworkElement)selectedStep).Tag;
            var attributes = _dynamicsComponentsService.GetImageAttributes(imageId, stepId);
            foreach (var item in attributes)
            {
                ImageAttributesCollection.Add(item);
            }
        }

        private void ImageAttributes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void HandleChanges()
        {
            var changesAdded = StepData.Attributes.Any() ||
                                  StepData.Images.Any(i => i.Attributes.Any());

            var ticketAdded = !string.IsNullOrEmpty(TicketInfo);
            var folderSelected = !string.IsNullOrEmpty(FolderPath);

            Button_Export.IsEnabled = changesAdded && ticketAdded && folderSelected;
        }

        public ObservableCollection<StepAttribute> StepAttributesCollection { get; set; }

        public ObservableCollection<StepAttribute> ImageAttributesCollection { get; set; }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            StepAttributesCollection.Clear();
            ImageAttributesCollection.Clear();
            LoadAttributes();
            LoadImageAttributes();
        }

        private void TextBlock_TaskInfo_TextChanged(object sender, TextChangedEventArgs e)
        {
            TicketInfo = TextBlock_TaskInfo.Text;
        }

        private string TicketInfo { get; set; }

        private string FolderPath { get; set; }

        private void TextBox_SelectedSolutionFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            FolderPath = TextBox_SelectedSolutionFolder.Text;
        }

        private void StepAttributes_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            StepAttributes.Columns[1].Visibility = Visibility.Collapsed;
            StepAttributes.Columns[2].Visibility = Visibility.Collapsed;
        }

        private void ImageAttributes_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            ImageAttributes.Columns[1].Visibility = Visibility.Collapsed;
            ImageAttributes.Columns[2].Visibility = Visibility.Collapsed;
        }
    }
}