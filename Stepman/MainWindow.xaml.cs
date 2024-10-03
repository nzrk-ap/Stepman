using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using PowerApps.Samples.LoginUX;
using Stepman.Models;
using Stepman.Services;
using Microsoft.Extensions.DependencyInjection;

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
            LoadSteps();
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            var selected = StepsComboBox.SelectedItem;
            var stepId = (Guid)((FrameworkElement)selected).Tag;
            var attributes = _dynamicsComponentsService.GetStepAttributes(stepId);
            foreach (var item in attributes)
            {
                StepAttributesCollection.Add(item);
            }
        }

        private void Button_SolutionFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder";
                dialog.ShowNewFolderButton = true; // Optional: Show button to create a new folder

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
            var items = e.AddedItems;
            StepAttribute attribute;

            if (items.Count == 0)
            {
                if (StepData is not null)
                {
                    attribute = (StepAttribute)items[0];
                    StepData.Attributes.First(a => a.LogicalName == attribute.LogicalName).Checked = false;
                }
            }

            var selectedStep = StepsComboBox.SelectedItem;

            StepData ??= new StepData
            {
                StepId = (Guid)((FrameworkElement)selectedStep).Tag
            };

            attribute = (StepAttribute)items[0];
            StepData.Attributes.Add(attribute);
        }

        private void ImageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            var items = e.AddedItems;
            StepAttribute attribute;

            var selectedStep = StepsComboBox.SelectedItem;
            var stepId = (Guid)((FrameworkElement)selectedStep).Tag;
            var selectedImage = ImageComboBox.SelectedItem;
            var imageId = (Guid)((FrameworkElement)selectedImage).Tag;

            if (items.Count == 0)
            {
                if (StepData is not null)
                {
                    var image = StepData.Images.FirstOrDefault(i => i.ImageId == imageId);
                    if (image is null)
                    {
                        StepData.Images.Add(new ImageData
                        {
                            ImageId = imageId,
                            Attributes = new List<StepAttribute>()
                        });
                    }
                }
            }

            attribute = (StepAttribute)items[0];
            StepData.Images.First(i => i.ImageId == imageId).Attributes.Add(attribute);
        }

        public ObservableCollection<StepAttribute> StepAttributesCollection { get; set; }

        public ObservableCollection<StepAttribute> ImageAttributesCollection { get; set; }
    }
}