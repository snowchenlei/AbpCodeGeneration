using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AbpCodeGeneration.VisualStudio.Shared
{
    public interface IViewFactory
    {
        T GetView<T>() where T : Control;

        T GetView<T>(object data) where T : Control;
    }
}