using AbpCodeGeneration.VisualStudio.Common;
using AbpCodeGeneration.VisualStudio.UI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Welcome.xaml 的交互逻辑
    /// </summary>
    public partial class Welcome : UserControl
    {
        public Dictionary<int, string> ValidationTypes;
        public Welcome()
        {
            InitializeComponent();
            ValidationTypes = EnumHelper.EnumToDictionary<ValidationType>(-1, "请选择验证类型");
            //ValidationTypes.ItemsSource = 
            //ValidationTypes.SelectedValuePath = "key";
            //ValidationTypes.DisplayMemberPath = "Value";
            //ValidationTypes.SelectedIndex = 0;

        }

        
    }
}
