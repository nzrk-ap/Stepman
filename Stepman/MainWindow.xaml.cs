using System;
using System.Collections.ObjectModel;
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
using Microsoft.Xrm.Sdk.Organization;
using PowerApps.Samples.LoginUX;
using Stepman.Services;

namespace Stepman
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DynamicsComponentsService _dynamicsComponentsService;

        public MainWindow()
        {
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
                MessageBox.Show("Connected to Dataverse version: " + ctrl.CrmConnectionMgr.CrmSvc.ConnectedOrgVersion.ToString() +
                    " Organization: " + ctrl.CrmConnectionMgr.CrmSvc.ConnectedOrgUniqueName, "Connection Status");
                // TODO Additional web service operations can be performed here
                _dynamicsComponentsService = new DynamicsComponentsService(ctrl.CrmConnectionMgr.CrmSvc);
                LoadSolutions();
            }
            else
            {
                MessageBox.Show("Cannot connect; try again!", "Connection Status");
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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSteps();
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            var selected = StepsComboBox.SelectedItem;
            var stepId = (Guid)((FrameworkElement)selected).Tag;
            var attributes = _dynamicsComponentsService.GetStepAttributes(stepId);
        }

        public class StepAttribute
        {
            public bool Checked { get; set; }
            public string LogicalName { get; set; }
            public string DisplayName { get; set; }

            public string Type { get; set; }
        }

        public ObservableCollection<StepAttribute> StepAttributesCollection { get; set; }
    }
}