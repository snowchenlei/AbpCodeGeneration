using AbpCodeGeneration.VisualStudio.Common;
using AbpCodeGeneration.VisualStudio.Common.Enums;
using AbpCodeGeneration.VisualStudio.Common.Model;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AbpCodeGeneration.VisualStudio.UI
{
    /// <summary>
    /// Welcome.xaml 的交互逻辑
    /// </summary>
    public partial class Welcome : Window
    {
        public Dictionary<int, string> ValidationTypes;
        private readonly DTE2 _dte;
        public Welcome(DTE2 dte)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _dte = dte;
            InitializeComponent();
            // TODO:枚举的本地化
            ValidationTypes = EnumHelper.EnumToDictionary<ValidationType>(null, null);
            Validations.ItemsSource = ValidationTypes;
            Validations.SelectedIndex = 0;

        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            Setting setting = new Setting
            {
                ValidationType = (ValidationType)Validations.SelectedValue,
                SharedPermission = SharedPermission.IsChecked ?? false,
                ApplicationService = ApplicationService.IsChecked ?? false,
                DomainService = DomainService.IsChecked ?? false,
                Controller = Controller.IsChecked ?? false,
                AuthorizationService = AuthorizationService.IsChecked ?? false,
                ExcelImportAndExport = ExcelImportAndExport.IsChecked ?? false,
                IsStandardProject = StandardDDD.IsChecked ?? false,
                NamespacePrefix = NamespacePrefix.Text,
                Repository = Repository.IsChecked ?? false,
            };

            Window.GetWindow(this).Close();
            new MainWindow(_dte, setting).ShowDialog();
        }
    }
}
