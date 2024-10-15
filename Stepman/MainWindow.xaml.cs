using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using PowerApps.Samples.LoginUX;
using Stepman.Models;
using Stepman.Services;

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
            StepsCollection = new ObservableCollection<ComboBoxItem>();
            StepsComboBox.ItemsSource = StepsCollection;
            foreach (var item in steps)
            {
                StepsCollection.Add(new ComboBoxItem { Content = item.Name, Tag = item });
            }
        }

        private void SolutionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProgressRingLoader.Visibility = Visibility.Visible;
            StepAttributesCollection.Clear();
            ImageAttributesCollection.Clear();
            LoadSteps();
            ProgressRingLoader.Visibility = Visibility.Collapsed;
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            StepAttributesCollection.Clear();
            LoadAttributes();
        }

        private void LoadAttributes()
        {
            var step = GetSelectedStep();
            if (step is null)
                return;

            var attributes = _dynamicsComponentsService.GetStepAttributes(step.StepId);
            foreach (var item in attributes)
            {
                StepAttributesCollection.Add(item);
            }

            var images = _dynamicsComponentsService.GetStepsImages(step.StepId);
            var cbItems = new ObservableCollection<ComboBoxItem>();
            ImageComboBox.ItemsSource = cbItems;
            foreach (var item in images)
            {
                cbItems.Add(new ComboBoxItem { Content = item.Value, Tag = item.Key });
            }
        }

        private StepItem GetSelectedStep()
        {
            var selected = StepsComboBox.SelectedItem;
            if (selected is null)
                return default;
            return (StepItem)((FrameworkElement)selected).Tag;
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
            Button_Export.IsEnabled = false;
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
            var selectedStep = GetSelectedStep();
            StepAttribute attribute;

            if (selectedStep is null)
                return;

            StepData ??= new StepData
            {
                StepId = selectedStep.StepId
            };

            if (row != null)
            {
                // Get the selected item from the row
                attribute = (StepAttribute)row.Item;
                if (!StepData?.Attributes?.Contains(attribute) ?? false)
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

            if (row != null)
            {
                // Get the selected item from the row
                if (row.Item is StepAttribute attribute)
                {
                    if (StepData?.Attributes?.Contains(attribute) ?? false)
                    {
                        StepData.Attributes.Remove(attribute);
                    }
                    HandleChanges();
                }
            }
        }

        private void ImageAttribute_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            var row = DataGridRow.GetRowContainingElement(checkBox);

            var selectedStep = GetSelectedStepId();
            var imageId = GetSelectedImageId();

            StepData ??= new StepData
            {
                StepId = GetSelectedStepId()
            };

            if (row != null)
            {
                // Get the selected item from the row
                //attribute = (StepAttribute)row.Item;
                if (row.Item is StepAttribute attribute)
                {
                    var image = StepData?.Images?.FirstOrDefault(i => i.ImageId == imageId);
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
        }

        private void ImageAttribute_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            var row = DataGridRow.GetRowContainingElement(checkBox);

            var selectedStep = GetSelectedStepId();
            var imageId = GetSelectedImageId();

            if (row != null)
            {
                // Get the selected item from the row
                if (row.Item is StepAttribute attribute)
                {
                    var image = StepData?.Images?.FirstOrDefault(i => i.ImageId == imageId);
                    if (image?.Attributes?.Contains(attribute) ?? false)
                    {
                        image.Attributes.Remove(attribute);
                    }
                    HandleChanges();
                }
            }
        }

        private Guid GetSelectedImageId()
        {
            var selectedImage = ImageComboBox.SelectedItem;
            if (selectedImage is null) return default;
            return (Guid)((FrameworkElement)selectedImage).Tag;
        }

        private Guid GetSelectedStepId()
        {
            var selectedStep = StepsComboBox.SelectedItem;
            if (selectedStep is null) return default;
            var step = (StepItem)((FrameworkElement)selectedStep).Tag;
            return step.StepId;
        }

        private void ImageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageAttributesCollection.Clear();
            LoadImageAttributes();
        }

        private void LoadImageAttributes()
        {
            var imageId = GetSelectedImageId();
            var stepId = GetSelectedStepId();

            if (imageId == default || stepId == default)
                return;

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
            if (StepData is null)
                return;

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

        private void Button_FilterStepsByEntity_Click(object sender, RoutedEventArgs e)
        {
            var entityName = TextBox_TypeEntity.Text;
            if (!string.IsNullOrEmpty(entityName))
            {
                StepsComboBox.ItemsSource = StepsCollection.Where(step => (step.Tag as StepItem).RelatedEntity.Contains(entityName));
            }
            else
            {
                StepsComboBox.ItemsSource = StepsCollection;
            }
        }

        private ObservableCollection<ComboBoxItem> StepsCollection { get; set; }

        private void Button_ClearEntity_Click(object sender, RoutedEventArgs e)
        {
            StepsComboBox.ItemsSource = StepsCollection;
            TextBox_TypeEntity.Text = null;
        }
    }
}