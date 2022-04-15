using AbpCodeGeneration.VisualStudio.Common;
using AbpCodeGeneration.VisualStudio.Common.Model;
using AbpCodeGeneration.VisualStudio.UI.ViewModels;
using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AbpCodeGeneration.VisualStudio.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : UserControl
    {
        private ObservableCollection<DtoPropertyInfo> DataList = new ObservableCollection<DtoPropertyInfo>();
        private readonly Setting _setting;
        private readonly ProjectHelper projectHelper;
        public MainWindow(DTE2 _dte, Setting setting)
        {
            InitializeComponent();
            _setting = setting;
            projectHelper = new ProjectHelper(_dte);
            DtoFileModel dto = projectHelper.GetDtoModel();
            foreach (var item in dto.ClassPropertys)
            {
                if ("Id".Equals(item.Name) || (dto.Name + "Id").Equals(item.Name))
                {
                    ClassKeyType.Text = item.PropertyType;
                    continue;
                }
                DataList.Add(new DtoPropertyInfo
                {
                    PropertyName = item.Name,
                    PropertyType = item.PropertyType,
                    IsEdit = true,
                    IsList = true,
                    Local = item.Name
                });
            }
            PropertyGrid.ItemsSource = DataList;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(ClassKeyType.Text))
            {
                MessageBox.Show(Properties.Resources.PrimaryKeyIsRequired, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                return;
            }

            DtoFileModel dto = projectHelper.GetDtoModel();
            projectHelper.CreateFile(new CreateFileInput()
            {
                AbsoluteNamespace = dto.Namespace.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last(),
                Namespace = dto.Namespace,
                ClassName = dto.Name,
                KeyType = ClassKeyType.Text,
                LocalName = ClassLocalName.Text,
                DirectoryName = dto.DirName,
                PropertyInfos = DataList,
                Prefix = _setting.NamespacePrefix,// NamespacePrefix.Text,
                Setting = _setting,
                ValidationType = _setting.ValidationType,
                Controller = _setting.Controller,
                ApplicationService = _setting.ApplicationService,
                DomainService = _setting.DomainService,
                AuthorizationService = _setting.AuthorizationService,
                ExcelImportAndExport = _setting.ExcelImportAndExport,
                IsStandardProject = _setting.IsStandardProject
            });
            MessageBoxResult result = MessageBox.Show(Properties.Resources.Succeeded, Properties.Resources.Tips, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            if (result == MessageBoxResult.OK)
            {
                //获取父窗体并关闭
                System.Windows.Window parentWindow = System.Windows.Window.GetWindow(this);
                parentWindow.Close();
                return;
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            //增加行号
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            int _rowIndex = 0;
            int _columnIndex = 0;
            if (GetCellXY(PropertyGrid, ref _rowIndex, ref _columnIndex))
            {
                DataList.RemoveAt(_rowIndex);
            }
        }

        private void PropertyGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            // 模拟双击
            Util.Mouse.DoubleClick(MouseButton.Left);
        }

        //---取得选中 Cell 所在的行列
        private bool GetCellXY(DataGrid dg, ref int rowIndex, ref int columnIndex)
        {
            var cells = dg.SelectedCells;
            if (cells.Any())
            {
                rowIndex = dg.Items.IndexOf(cells.First().Item);
                columnIndex = cells.First().Column.DisplayIndex;
                return true;
            }
            return false;
        }

    }
}