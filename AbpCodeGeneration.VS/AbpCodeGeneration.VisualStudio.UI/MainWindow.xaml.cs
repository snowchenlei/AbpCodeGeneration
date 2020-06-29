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
            dataGrid.ItemsSource = DataList;
        }

        private void Query_Click(object sender, RoutedEventArgs e)
        {
            
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
                FirstUse = _setting.FirstUse,
                ValidationType = _setting.ValidationType,
                ApplicationService = _setting.ApplicationService,
                DomainService = _setting.DomainService,
                AuthorizationService = _setting.AuthorizationService,
                ExcelImportAndExport = _setting.ExcelImportAndExport,
                PictureUpload = _setting.PictureUpload
            });
            MessageBoxResult result = MessageBox.Show("代码生成成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
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
            if (GetCellXY(dataGrid, ref _rowIndex, ref _columnIndex))
            {
                DataList.RemoveAt(_rowIndex);
            }
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